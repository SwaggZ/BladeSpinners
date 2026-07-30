using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BladeSpinners.Audio
{
    /// <summary>
    /// Persistent, folder-driven audio service. Interaction code requests a folder key;
    /// the manager chooses a clip from the generated runtime catalog.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class SoundManager : MonoBehaviour
    {
        private const string CatalogResourceName = "RuntimeSoundCatalog";
        private const int SpatialVoiceCount = 16;

        private static SoundManager instance;
        private static uint nextMusicStartId;
        private static AudioClip lastStartedMusicClip;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)]
        private float masterVolume = AudioMixLevels.DefaultMaster;
        [SerializeField, Range(0f, 1f)]
        private float sfxVolume = AudioMixLevels.DefaultSoundEffects;
        [SerializeField, Range(0f, 1f)]
        private float uiVolume = AudioMixLevels.DefaultGui;
        [SerializeField, Range(0f, 1f)]
        private float musicVolume = AudioMixLevels.DefaultMusic;
        [SerializeField, Range(0.1f, 5f)]
        private float musicCrossfadeDuration = 1.25f;

        private readonly List<AudioSource> spatialVoices = new List<AudioSource>();
        private readonly Dictionary<string, int> lastClipByKey =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> lastPlayTimeByKey =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> warnedKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<
            MusicSituation,
            MusicShuffleBag> musicShuffleBags =
                new Dictionary<
                    MusicSituation,
                    MusicShuffleBag>();
        private readonly MusicSituationQueue
            queuedMusicSituations =
                new MusicSituationQueue();

        private RuntimeSoundCatalog catalog;
        private AudioSource uiSource;
        private AudioSource primaryMusicSource;
        private AudioSource secondaryMusicSource;
        private AudioSource activeMusicSource;
        private Coroutine musicTransition;
        private MusicTrackInfo currentMusicTrack;
        private MusicSituation? currentMusicSituation;
        private uint musicStartId;
        private float nextMusicRecoveryAt;
        private int fallbackVoiceIndex;
        private int musicSessionSeed;
        private System.Random musicSeedGenerator;
        private MusicShuffleBag fallbackMusicShuffleBag;

        public static event Action<MusicTrackInfo> NowPlayingChanged;

        public static bool IsReady => instance != null && instance.catalog != null;
        public static int CatalogFolderCount => instance != null && instance.catalog != null
            ? instance.catalog.FolderCount
            : 0;
        public static int CatalogClipCount => instance != null && instance.catalog != null
            ? instance.catalog.ClipCount
            : 0;
        public static int CatalogMusicTrackCount =>
            instance != null && instance.catalog != null
                ? instance.catalog.MusicTrackCount
                : 0;
        public static MusicTrackInfo CurrentMusicTrack =>
            instance != null ? instance.currentMusicTrack : default;
        public static MusicSituation? CurrentMusicSituation =>
            instance != null ? instance.currentMusicSituation : null;
        public static float MusicCrossfadeDuration =>
            instance != null ? instance.musicCrossfadeDuration : 0f;
        public static bool IsMusicPlaying =>
            instance != null && instance.IsMusicOutputActive();
        public static uint CurrentMusicStartId =>
            instance != null ? instance.musicStartId : 0u;
        public static int QueuedMusicSituationCount =>
            instance != null
                ? instance.queuedMusicSituations.Count
                : 0;
        public static AudioMixLevels CurrentMix =>
            instance != null
                ? instance.GetCurrentMix()
                : AudioMixPreferences.Load();
        public static float MasterVolume => CurrentMix.Master;
        public static float SoundEffectsVolume =>
            CurrentMix.SoundEffects;
        public static float MusicVolume => CurrentMix.Music;
        public static float GuiVolume => CurrentMix.Gui;
        public static float EffectiveSoundEffectsVolume =>
            CurrentMix.EffectiveSoundEffects;
        public static float EffectiveMusicVolume =>
            CurrentMix.EffectiveMusic;
        public static float EffectiveGuiVolume =>
            CurrentMix.EffectiveGui;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            nextMusicStartId = 0u;
            lastStartedMusicClip = null;
            NowPlayingChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || instance != null)
                return;

            SoundManager existing = FindFirstObjectByType<SoundManager>();
            if (existing != null)
            {
                instance = existing;
                return;
            }

            GameObject host = new GameObject("SoundManager");
            instance = host.AddComponent<SoundManager>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMusicRandomizer();
            catalog = Resources.Load<RuntimeSoundCatalog>(CatalogResourceName);
            ApplyMix(AudioMixPreferences.Load());
            CreateSources();

            if (catalog == null)
            {
                Debug.LogError(
                    "[SoundManager] RuntimeSoundCatalog is missing. Run " +
                    "'Blade Spinners/Audio/Sync Runtime Sound Catalog' before building.");
            }
        }

        private void Start()
        {
            // RuntimeGameUiController owns the initial Start Screen situation.
            // Keeping startup routing there prevents debug-only scenes from
            // unexpectedly starting front-end music.
        }

        private void Update()
        {
            if (musicTransition != null
                || !currentMusicSituation.HasValue
                || !currentMusicTrack.IsValid)
            {
                return;
            }

            if (!IsMusicOutputActive())
            {
                if (Time.unscaledTime >= nextMusicRecoveryAt)
                {
                    nextMusicRecoveryAt = Time.unscaledTime + 1f;
                    RequestMusicSituation(
                        currentMusicSituation.Value,
                        true);
                }
                return;
            }

            AudioClip clip = activeMusicSource.clip;
            if (activeMusicSource.loop
                || clip == null
                || clip.length <= 0f)
            {
                return;
            }

            float remaining = clip.length - activeMusicSource.time;
            if (remaining <= musicCrossfadeDuration + 0.1f)
            {
                AdvanceMusicQueue();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public static void PlayAbility(string abilityName, Vector3 position)
        {
            GetInstance()?.PlaySpatialFolder(
                SoundPaths.Ability(abilityName), position, 1f, 0.96f, 1.04f, 0.08f);
        }

        public static void PlayBeyHit(Vector3 position, float intensity)
        {
            GetInstance()?.PlaySpatialFolder(
                SoundPaths.HitBeyAgainstBey,
                position,
                Mathf.Clamp01(intensity),
                0.94f,
                1.06f,
                0.06f);
        }

        public static void PlayWallHit(Vector3 position, float intensity)
        {
            GetInstance()?.PlaySpatialFolder(
                SoundPaths.HitBeyAgainstWall,
                position,
                Mathf.Clamp01(intensity),
                0.95f,
                1.05f,
                0.1f);
        }

        public static void PlayUi(string folderKey, float volumeScale = 1f)
        {
            SoundManager manager = GetInstance();
            if (manager == null || manager.uiSource == null)
                return;

            string key = SoundPaths.Normalize(folderKey);
            if (!manager.TrySelectClip(key, out AudioClip clip) || !manager.CanPlay(key, 0.035f))
                return;

            manager.uiSource.pitch = 1f;
            manager.uiSource.PlayOneShot(
                clip,
                Mathf.Clamp01(volumeScale));
        }

        public static void PlayMusic(string folderKey, bool restartIfPlaying = false)
        {
            SoundManager manager = GetInstance();
            if (manager == null)
                return;

            string key = SoundPaths.Normalize(folderKey);
            if (!restartIfPlaying
                && manager.IsMusicOutputActive())
                return;

            if (!manager.TrySelectClip(key, out AudioClip clip))
                return;
            if (clip == lastStartedMusicClip
                && manager.IsMusicOutputActive())
            {
                return;
            }

            manager.BeginMusicTransition(
                new MusicTrackInfo(
                    clip,
                    null,
                    clip.name,
                    "Unknown Artist",
                    MusicSituation.MainMenu),
                null,
                true);
        }

        public static void PlayMusicSituation(
            MusicSituation situation,
            bool forceNewTrack = false)
        {
            SoundManager manager = GetInstance();
            if (manager == null)
                return;

            manager.queuedMusicSituations.Clear();
            manager.RequestMusicSituation(
                situation,
                forceNewTrack);
        }

        /// <summary>
        /// Adds a category to the upcoming music rotation without interrupting
        /// the current track. Repeated menu navigation cannot add duplicate
        /// categories to the pending queue.
        /// </summary>
        public static void QueueMusicSituation(
            MusicSituation situation)
        {
            SoundManager manager = GetInstance();
            if (manager == null)
                return;

            if (!manager.IsMusicOutputActive()
                && manager.musicTransition == null)
            {
                manager.RequestMusicSituation(
                    situation,
                    false);
                return;
            }

            if (!manager.queuedMusicSituations.Enqueue(
                    situation))
                return;

            Debug.Log(
                $"[Music] Queued {situation} " +
                $"({manager.queuedMusicSituations.Count} pending).");
        }

        public static bool IsMusicSituationQueued(
            MusicSituation situation)
        {
            return instance != null
                && instance.ContainsQueuedMusicSituation(situation);
        }

        /// <summary>
        /// Softly advances to the next queued category, or another song from
        /// the current category when the queue is empty.
        /// </summary>
        public static void SkipToNextMusic()
        {
            GetInstance()?.AdvanceMusicQueue();
        }

        public static void StopMusic()
        {
            instance?.BeginMusicStop();
        }

        /// <summary>
        /// Applies the user mix immediately. The listener is the master bus while
        /// SoundManager sources are the category buses.
        /// </summary>
        public static void SetMixVolumes(
            float master,
            float soundEffects,
            float music,
            float gui,
            bool persist = true)
        {
            AudioMixLevels levels = new AudioMixLevels(
                master,
                soundEffects,
                music,
                gui)
                .Clamped();

            SoundManager manager = GetInstance();
            if (manager != null)
                manager.ApplyMix(levels);
            else
                AudioListener.volume = levels.Master;

            if (persist)
                AudioMixPreferences.Save(levels);
        }

        public static float CalculateEffectiveVolume(
            float master,
            float category)
        {
            return Mathf.Clamp01(master)
                * Mathf.Clamp01(category);
        }

        private static SoundManager GetInstance()
        {
            if (!Application.isPlaying)
                return null;

            if (instance == null)
                Bootstrap();

            return instance;
        }

        private void CreateSources()
        {
            uiSource = gameObject.AddComponent<AudioSource>();
            ConfigureTwoDimensionalSource(uiSource);
            uiSource.volume = uiVolume;

            primaryMusicSource = CreateMusicSource("Music A");
            secondaryMusicSource = CreateMusicSource("Music B");

            for (int i = 0; i < SpatialVoiceCount; i++)
            {
                GameObject voiceObject = new GameObject($"Spatial Voice {i + 1}");
                voiceObject.transform.SetParent(transform, false);
                AudioSource source = voiceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.dopplerLevel = 0f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = 3f;
                source.maxDistance = 40f;
                source.volume = sfxVolume;
                spatialVoices.Add(source);
            }
        }

        private AudioMixLevels GetCurrentMix()
        {
            return new AudioMixLevels(
                masterVolume,
                sfxVolume,
                musicVolume,
                uiVolume);
        }

        private void ApplyMix(AudioMixLevels levels)
        {
            AudioMixLevels clamped = levels.Clamped();
            float previousMusicVolume = musicVolume;

            masterVolume = clamped.Master;
            sfxVolume = clamped.SoundEffects;
            musicVolume = clamped.Music;
            uiVolume = clamped.Gui;

            // AudioListener is the global master bus. Category volumes live on
            // their sources, producing Master x Category output.
            AudioListener.volume = masterVolume;

            if (uiSource != null)
                uiSource.volume = uiVolume;

            for (int i = 0; i < spatialVoices.Count; i++)
            {
                if (spatialVoices[i] != null)
                    spatialVoices[i].volume = sfxVolume;
            }

            RescaleMusicSources(
                previousMusicVolume,
                musicVolume);
        }

        private void RescaleMusicSources(
            float previousVolume,
            float nextVolume)
        {
            RescaleMusicSource(
                primaryMusicSource,
                previousVolume,
                nextVolume);
            RescaleMusicSource(
                secondaryMusicSource,
                previousVolume,
                nextVolume);
        }

        private void RescaleMusicSource(
            AudioSource source,
            float previousVolume,
            float nextVolume)
        {
            if (source == null)
                return;

            if (previousVolume > 0.0001f)
            {
                float fadeWeight = Mathf.Clamp01(
                    source.volume / previousVolume);
                source.volume = nextVolume * fadeWeight;
                return;
            }

            // A running transition reapplies its fade weight next frame. Outside
            // a transition, raising Music from zero restores the active track now.
            if (musicTransition == null
                && source == activeMusicSource
                && source.isPlaying)
            {
                source.volume = nextVolume;
            }
        }

        private static void ConfigureTwoDimensionalSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
        }

        private AudioSource CreateMusicSource(string sourceName)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            ConfigureTwoDimensionalSource(source);
            source.loop = true;
            source.volume = 0f;
            return source;
        }

        private void RequestMusicSituation(
            MusicSituation situation,
            bool forceNewTrack)
        {
            if (!forceNewTrack
                && currentMusicSituation == situation
                && currentMusicTrack.IsValid
                && IsMusicOutputActive())
            {
                return;
            }
            if (catalog == null)
            {
                WarnOnce(
                    $"Music/{situation}",
                    "runtime catalog is unavailable");
                return;
            }

            IReadOnlyList<
                RuntimeSoundCatalog.MusicTrackEntry> tracks;
            bool hasSituationTracks =
                catalog.TryGetMusicTracks(
                    situation,
                    out tracks)
                && tracks.Count > 0;

            // The authored Start Screen theme is intentionally optional until
            // its final track is supplied. A Main Menu song keeps first launch
            // audible in the meantime, but still loops as a title theme.
            if (!hasSituationTracks
                && situation == MusicSituation.StartScreen)
            {
                hasSituationTracks =
                    catalog.TryGetMusicTracks(
                        MusicSituation.MainMenu,
                        out tracks)
                    && tracks.Count > 0;
            }
            if (!hasSituationTracks)
            {
                WarnOnce(
                    $"Music/{situation}",
                    "situation has no configured tracks");
                return;
            }

            RuntimeSoundCatalog.MusicTrackEntry selected =
                SelectMusicTrack(
                    tracks,
                    lastStartedMusicClip,
                    situation);
            if (selected == null || selected.Clip == null)
            {
                // A one-song situation may receive a redundant forced request while
                // that song is already playing. Keeping it running avoids both a
                // restart and a repeated banner.
                if (IsMusicOutputActive()
                    && currentMusicTrack.Clip
                        == lastStartedMusicClip)
                {
                    return;
                }

                // If the only situation track is the last song and playback was lost,
                // use another authored track rather than leaving the game silent or
                // immediately replaying the same song.
                selected = SelectMusicTrack(
                    catalog.MusicTracks,
                    lastStartedMusicClip,
                    null);
                if (selected == null || selected.Clip == null)
                {
                    WarnOnce(
                        $"Music/{situation}",
                        "situation contains no playable non-repeating clips");
                    return;
                }
            }

            bool loopTrack =
                situation == MusicSituation.StartScreen
                || CountPlayableTracks(tracks) <= 1;
            BeginMusicTransition(
                selected.ToTrackInfo(),
                situation,
                loopTrack);
        }

        private void AdvanceMusicQueue()
        {
            MusicSituation situation;
            if (queuedMusicSituations.TryDequeue(
                    out MusicSituation queued))
            {
                situation = queued;
            }
            else if (currentMusicSituation.HasValue)
            {
                situation =
                    currentMusicSituation.Value;
            }
            else
            {
                situation = MusicSituation.MainMenu;
            }

            RequestMusicSituation(
                situation,
                true);
        }

        private bool ContainsQueuedMusicSituation(
            MusicSituation situation)
        {
            return queuedMusicSituations.Contains(
                situation);
        }

        private RuntimeSoundCatalog.MusicTrackEntry SelectMusicTrack(
            IReadOnlyList<RuntimeSoundCatalog.MusicTrackEntry> tracks,
            AudioClip excludedClip,
            MusicSituation? situation)
        {
            InitializeMusicRandomizer();

            MusicShuffleBag shuffleBag;
            if (situation.HasValue)
            {
                if (!musicShuffleBags.TryGetValue(
                        situation.Value,
                        out shuffleBag))
                {
                    shuffleBag = new MusicShuffleBag(
                        NextMusicShuffleSeed());
                    musicShuffleBags.Add(
                        situation.Value,
                        shuffleBag);
                }
            }
            else
            {
                if (fallbackMusicShuffleBag == null)
                {
                    fallbackMusicShuffleBag =
                        new MusicShuffleBag(
                            NextMusicShuffleSeed());
                }
                shuffleBag = fallbackMusicShuffleBag;
            }

            return shuffleBag.Take(
                tracks,
                excludedClip);
        }

        private void InitializeMusicRandomizer()
        {
            if (musicSeedGenerator != null)
                return;

            unchecked
            {
                musicSessionSeed =
                    Environment.TickCount
                    ^ (int)DateTime.UtcNow.Ticks
                    ^ Guid.NewGuid().GetHashCode();
            }
            musicSeedGenerator =
                new System.Random(musicSessionSeed);
            Debug.Log(
                $"[Music] Randomized playlist session seed: " +
                $"{musicSessionSeed}.");
        }

        private int NextMusicShuffleSeed()
        {
            int seed = musicSeedGenerator.Next();
            return musicSeedGenerator.Next(0, 2) == 0
                ? seed
                : ~seed;
        }

        private static int CountPlayableTracks(
            IReadOnlyList<RuntimeSoundCatalog.MusicTrackEntry> tracks)
        {
            int count = 0;
            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks[i] != null && tracks[i].Clip != null)
                    count++;
            }
            return count;
        }

        private void BeginMusicTransition(
            MusicTrackInfo track,
            MusicSituation? situation,
            bool loopTrack)
        {
            if (!track.IsValid)
                return;
            if (track.Clip == lastStartedMusicClip
                && IsMusicOutputActive())
            {
                return;
            }

            if (musicTransition != null)
                StopCoroutine(musicTransition);

            AudioSource outgoing = GetDominantMusicSource();
            AudioSource incoming =
                outgoing == primaryMusicSource
                    ? secondaryMusicSource
                    : primaryMusicSource;
            if (incoming == null)
                return;

            incoming.Stop();
            incoming.clip = track.Clip;
            incoming.pitch = 1f;
            incoming.loop = loopTrack;
            incoming.volume = 0f;
            if (track.Clip.loadState
                == AudioDataLoadState.Unloaded)
            {
                track.Clip.LoadAudioData();
            }
            incoming.Play();

            activeMusicSource = incoming;
            currentMusicTrack = track;
            currentMusicSituation = situation ?? track.Situation;
            lastStartedMusicClip = track.Clip;
            musicStartId = ++nextMusicStartId;
            if (musicStartId == 0u)
                musicStartId = ++nextMusicStartId;
            nextMusicRecoveryAt = Time.unscaledTime + 1f;
            Debug.Log(
                $"[Music] {currentMusicSituation}: \"{track.Title}\" by {track.Author} " +
                $"(start {musicStartId}, crossfade {musicCrossfadeDuration:F2}s, " +
                $"loop={loopTrack}).");
            NowPlayingChanged?.Invoke(track);

            musicTransition = StartCoroutine(
                CrossfadeMusic(outgoing, incoming));
        }

        private void BeginMusicStop()
        {
            if (musicTransition != null)
                StopCoroutine(musicTransition);

            musicTransition = StartCoroutine(
                FadeOutMusic(
                    primaryMusicSource,
                    secondaryMusicSource));
            currentMusicTrack = default;
            currentMusicSituation = null;
            activeMusicSource = null;
        }

        private bool IsMusicOutputActive()
        {
            return activeMusicSource != null
                && activeMusicSource.clip != null
                && activeMusicSource.isPlaying;
        }

        private IEnumerator CrossfadeMusic(
            AudioSource outgoing,
            AudioSource incoming)
        {
            float duration = Mathf.Max(
                0.01f,
                musicCrossfadeDuration);
            float elapsed = 0f;
            float outgoingStartWeight =
                GetMusicFadeWeight(outgoing);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                incoming.volume = musicVolume * eased;
                if (outgoing != null && outgoing != incoming)
                {
                    outgoing.volume =
                        musicVolume
                        * outgoingStartWeight
                        * (1f - eased);
                }
                yield return null;
            }

            incoming.volume = musicVolume;
            if (outgoing != null && outgoing != incoming)
            {
                outgoing.Stop();
                outgoing.clip = null;
                outgoing.volume = 0f;
            }
            musicTransition = null;
        }

        private IEnumerator FadeOutMusic(
            AudioSource first,
            AudioSource second)
        {
            if (first == null && second == null)
            {
                musicTransition = null;
                yield break;
            }

            float duration = Mathf.Max(
                0.01f,
                musicCrossfadeDuration);
            float firstStartWeight =
                GetMusicFadeWeight(first);
            float secondStartWeight =
                GetMusicFadeWeight(second);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float remaining =
                    1f - Mathf.Clamp01(elapsed / duration);
                if (first != null)
                {
                    first.volume =
                        musicVolume
                        * firstStartWeight
                        * remaining;
                }
                if (second != null)
                {
                    second.volume =
                        musicVolume
                        * secondStartWeight
                        * remaining;
                }
                yield return null;
            }
            StopAndClearMusicSource(first);
            StopAndClearMusicSource(second);
            musicTransition = null;
        }

        private float GetMusicFadeWeight(AudioSource source)
        {
            if (source == null || !source.isPlaying)
                return 0f;
            if (musicVolume > 0.0001f)
            {
                return Mathf.Clamp01(
                    source.volume / musicVolume);
            }

            return source == activeMusicSource ? 1f : 0f;
        }

        private static void StopAndClearMusicSource(
            AudioSource source)
        {
            if (source == null)
                return;
            source.Stop();
            source.clip = null;
            source.volume = 0f;
        }

        private AudioSource GetDominantMusicSource()
        {
            bool primaryPlaying =
                primaryMusicSource != null
                && primaryMusicSource.isPlaying;
            bool secondaryPlaying =
                secondaryMusicSource != null
                && secondaryMusicSource.isPlaying;

            if (primaryPlaying && secondaryPlaying)
            {
                return primaryMusicSource.volume
                    >= secondaryMusicSource.volume
                    ? primaryMusicSource
                    : secondaryMusicSource;
            }
            if (primaryPlaying)
                return primaryMusicSource;
            if (secondaryPlaying)
                return secondaryMusicSource;
            return activeMusicSource;
        }

        private void PlaySpatialFolder(
            string folderKey,
            Vector3 position,
            float volumeScale,
            float minPitch,
            float maxPitch,
            float minimumInterval)
        {
            string key = SoundPaths.Normalize(folderKey);
            if (!TrySelectClip(key, out AudioClip clip) || !CanPlay(key, minimumInterval))
                return;

            AudioSource voice = GetAvailableSpatialVoice();
            if (voice == null)
                return;

            voice.transform.position = position;
            voice.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
            voice.PlayOneShot(
                clip,
                Mathf.Clamp01(volumeScale));
        }

        private AudioSource GetAvailableSpatialVoice()
        {
            for (int i = 0; i < spatialVoices.Count; i++)
            {
                if (!spatialVoices[i].isPlaying)
                    return spatialVoices[i];
            }

            if (spatialVoices.Count == 0)
                return null;

            AudioSource fallback = spatialVoices[fallbackVoiceIndex % spatialVoices.Count];
            fallbackVoiceIndex = (fallbackVoiceIndex + 1) % spatialVoices.Count;
            fallback.Stop();
            return fallback;
        }

        private bool TrySelectClip(string key, out AudioClip clip)
        {
            clip = null;
            if (catalog == null || !catalog.TryGetClips(key, out IReadOnlyList<AudioClip> clips))
            {
                WarnOnce(key, "folder is not present in the runtime catalog");
                return false;
            }

            if (clips.Count == 0)
            {
                WarnOnce(key, "folder has no AudioClips yet");
                return false;
            }

            int startIndex = UnityEngine.Random.Range(0, clips.Count);
            if (clips.Count > 1
                && lastClipByKey.TryGetValue(key, out int previousIndex)
                && startIndex == previousIndex)
            {
                startIndex = (startIndex + UnityEngine.Random.Range(1, clips.Count)) % clips.Count;
            }

            for (int offset = 0; offset < clips.Count; offset++)
            {
                int index = (startIndex + offset) % clips.Count;
                if (clips[index] == null)
                    continue;

                clip = clips[index];
                lastClipByKey[key] = index;
                return true;
            }

            WarnOnce(key, "folder contains only missing clip references");
            return false;
        }

        private bool CanPlay(string key, float minimumInterval)
        {
            float now = Time.unscaledTime;
            if (lastPlayTimeByKey.TryGetValue(key, out float previousTime)
                && now - previousTime < minimumInterval)
            {
                return false;
            }

            lastPlayTimeByKey[key] = now;
            return true;
        }

        private void WarnOnce(string key, string reason)
        {
            if ((!Application.isEditor && !Debug.isDebugBuild) || !warnedKeys.Add(key))
                return;

            Debug.LogWarning($"[SoundManager] '{key}' is silent because its {reason}.");
        }
    }
}
