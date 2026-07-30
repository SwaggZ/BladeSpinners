using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BladeSpinners.Audio;
using BladeSpinners.Gameplay.UI;
using UnityEditor;
using UnityEngine;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Validates the JSON-to-runtime music pipeline and every situation route.
    /// </summary>
    public static class MusicSystemValidator
    {
        private const string CatalogAssetPath =
            "Assets/Resources/RuntimeSoundCatalog.asset";

        [MenuItem("Blade Spinners/Validation/Test Situation Music")]
        public static void Validate()
        {
            SoundCatalogBuildProcessor.SyncCatalog(false);
            RuntimeSoundCatalog catalog =
                AssetDatabase.LoadAssetAtPath<RuntimeSoundCatalog>(
                    CatalogAssetPath);
            if (catalog == null)
                throw new InvalidOperationException("Runtime sound catalog is missing.");

            string[] mp3Files = Directory.GetFiles(
                MusicMetadataAuthoring.MusicFolder,
                "*.mp3",
                SearchOption.TopDirectoryOnly);
            if (catalog.MusicTrackCount != mp3Files.Length)
            {
                throw new InvalidOperationException(
                    $"Music catalog coverage mismatch. MP3s={mp3Files.Length}, " +
                    $"tracks={catalog.MusicTrackCount}.");
            }

            HashSet<string> files =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<MusicSituation, int> counts =
                new Dictionary<MusicSituation, int>();
            for (int i = 0; i < catalog.MusicTracks.Count; i++)
            {
                RuntimeSoundCatalog.MusicTrackEntry track =
                    catalog.MusicTracks[i];
                if (track == null
                    || track.Clip == null
                    || track.Logo == null
                    || string.IsNullOrWhiteSpace(track.Title)
                    || string.IsNullOrWhiteSpace(track.Author)
                    || !files.Add(track.SourceFile))
                {
                    throw new InvalidOperationException(
                        $"Music track {i} is incomplete or duplicated.");
                }

                string expectedLogoName =
                    Path.GetFileNameWithoutExtension(
                        track.SourceFile);
                string actualLogoName =
                    Path.GetFileNameWithoutExtension(
                        AssetDatabase.GetAssetPath(
                            track.Logo));
                if (!string.Equals(
                        expectedLogoName,
                        actualLogoName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Music track '{track.SourceFile}' uses logo " +
                        $"'{actualLogoName}' instead of '{expectedLogoName}'.");
                }

                counts.TryGetValue(
                    track.Situation,
                    out int count);
                counts[track.Situation] = count + 1;

                string assetPath =
                    MusicMetadataAuthoring.MusicFolder
                    + "/"
                    + track.SourceFile;
                AudioImporter importer =
                    AssetImporter.GetAtPath(assetPath)
                        as AudioImporter;
                if (importer == null
                    || importer.defaultSampleSettings.loadType
                        != AudioClipLoadType.Streaming
                    || !importer.defaultSampleSettings.preloadAudioData)
                {
                    throw new InvalidOperationException(
                        $"Background music '{track.SourceFile}' must use " +
                        "Streaming load type with Preload Audio Data enabled.");
                }
            }

            foreach (MusicSituation situation in
                     Enum.GetValues(typeof(MusicSituation)))
            {
                counts.TryGetValue(
                    situation,
                    out int count);
                if (situation
                    == MusicSituation.StartScreen)
                {
                    if (count > 1)
                    {
                        throw new InvalidOperationException(
                            "StartScreen must have zero or one theme track.");
                    }
                    continue;
                }

                if (count <= 0)
                {
                    throw new InvalidOperationException(
                        $"Situation '{situation}' has no music.");
                }
            }
            if (counts[MusicSituation.Battle] < 2)
            {
                throw new InvalidOperationException(
                    "Battle should have at least two tracks for non-repeating selection.");
            }
            ValidateShuffleBag();

            AssertRoute(
                MusicSituation.MainMenu,
                true,
                false,
                false,
                false,
                -1);
            AssertRoute(
                MusicSituation.Inventory,
                false,
                true,
                false,
                false,
                0);
            AssertRoute(
                MusicSituation.Battle,
                false,
                false,
                false,
                false,
                0);
            AssertRoute(
                MusicSituation.BossBattle,
                false,
                false,
                false,
                false,
                Core.GameConstants.BOSS_MAP_DEPTH - 1);
            AssertRoute(
                MusicSituation.Victory,
                false,
                false,
                true,
                false,
                0);
            AssertRoute(
                MusicSituation.Lose,
                false,
                false,
                false,
                true,
                0);

            BindingFlags fields =
                BindingFlags.Instance | BindingFlags.NonPublic;
            if (typeof(SoundManager).GetField(
                    "primaryMusicSource",
                    fields)?.FieldType
                    != typeof(AudioSource)
                || typeof(SoundManager).GetField(
                    "secondaryMusicSource",
                    fields)?.FieldType
                    != typeof(AudioSource)
                || typeof(SoundManager).GetField(
                    "musicCrossfadeDuration",
                    fields)?.FieldType
                    != typeof(float)
                || typeof(SoundManager).GetField(
                    "lastStartedMusicClip",
                    BindingFlags.Static
                        | BindingFlags.NonPublic)?.FieldType
                    != typeof(AudioClip)
                || typeof(SoundManager).GetField(
                    "musicShuffleBags",
                    fields) == null
                || typeof(SoundManager).GetField(
                    "musicSessionSeed",
                    fields)?.FieldType
                    != typeof(int)
                || typeof(SoundManager).GetField(
                    "queuedMusicSituations",
                    fields)?.FieldType
                    != typeof(MusicSituationQueue)
                || typeof(SoundManager).GetProperty(
                    nameof(SoundManager.IsMusicPlaying),
                    BindingFlags.Static | BindingFlags.Public)?.PropertyType
                    != typeof(bool)
                || typeof(SoundManager).GetProperty(
                    nameof(SoundManager.CurrentMusicStartId),
                    BindingFlags.Static | BindingFlags.Public)?.PropertyType
                    != typeof(uint)
                || typeof(SoundManager).GetMethod(
                    nameof(SoundManager.QueueMusicSituation),
                    BindingFlags.Static | BindingFlags.Public)
                    == null
                || typeof(SoundManager).GetMethod(
                    nameof(SoundManager.SkipToNextMusic),
                    BindingFlags.Static | BindingFlags.Public)
                    == null)
            {
                throw new InvalidOperationException(
                    "SoundManager is missing crossfade, queue, recovery, " +
                    "or no-repeat controls.");
            }
            if (typeof(MusicNowPlayingBanner).GetProperty(
                    nameof(MusicNowPlayingBanner.IsShowing),
                    BindingFlags.Static | BindingFlags.Public)?.PropertyType
                    != typeof(bool)
                || typeof(MusicNowPlayingBanner).GetProperty(
                    nameof(MusicNowPlayingBanner.DisplayedTrack),
                    BindingFlags.Static | BindingFlags.Public)?.PropertyType
                    != typeof(MusicTrackInfo)
                || typeof(MusicNowPlayingBanner).GetProperty(
                    nameof(MusicNowPlayingBanner.HasRenderedCurrentStart),
                    BindingFlags.Static | BindingFlags.Public)?.PropertyType
                    != typeof(bool))
            {
                throw new InvalidOperationException(
                    "The now-playing banner is missing its runtime diagnostics.");
            }
            ValidateSituationQueue();
            ValidateStartScreenSurface();

            Debug.Log(
                $"[SituationMusic] Passed: {catalog.MusicTrackCount} tracks, " +
                $"{counts[MusicSituation.Battle]} Battle choices, all " +
                "six required situations covered, optional single-track Start " +
                "Screen theme, dual-source crossfade, menu queue and skip controls, " +
                "randomized per-situation shuffle bags, no-repeat playback, " +
                "streaming import, recovery, and now-playing metadata validated.");
        }

        public static void ValidateFromCommandLine()
        {
            Validate();
        }

        private static void AssertRoute(
            MusicSituation expected,
            bool mainMenu,
            bool inventory,
            bool won,
            bool lost,
            int depthIndex)
        {
            MusicSituation actual =
                RuntimeGameUiController.DetermineMusicSituation(
                    mainMenu,
                    inventory,
                    won,
                    lost,
                    depthIndex);
            if (actual != expected)
            {
                throw new InvalidOperationException(
                    $"Music route expected {expected}, got {actual}.");
            }
        }

        private static void ValidateShuffleBag()
        {
            List<AudioClip> clips = new List<AudioClip>();
            try
            {
                List<RuntimeSoundCatalog.MusicTrackEntry> tracks =
                    new List<
                        RuntimeSoundCatalog.MusicTrackEntry>();
                for (int i = 0; i < 5; i++)
                {
                    AudioClip clip = AudioClip.Create(
                        $"Shuffle Validation {i}",
                        128,
                        1,
                        44100,
                        false);
                    clips.Add(clip);
                    tracks.Add(
                        new RuntimeSoundCatalog.MusicTrackEntry(
                            clip,
                            null,
                            $"{clip.name}.mp3",
                            clip.name,
                            "Validator",
                            MusicSituation.Battle));
                }

                MusicShuffleBag bag =
                    new MusicShuffleBag(24681357);
                AudioClip previous = null;
                for (int cycle = 0; cycle < 3; cycle++)
                {
                    HashSet<AudioClip> cycleClips =
                        new HashSet<AudioClip>();
                    for (int i = 0; i < tracks.Count; i++)
                    {
                        RuntimeSoundCatalog.MusicTrackEntry selected =
                            bag.Take(tracks, previous);
                        if (selected == null
                            || selected.Clip == null
                            || selected.Clip == previous
                            || !cycleClips.Add(selected.Clip))
                        {
                            throw new InvalidOperationException(
                                "Music shuffle bag repeated, skipped, or " +
                                "returned an invalid track.");
                        }
                        previous = selected.Clip;
                    }

                    if (cycleClips.Count != tracks.Count)
                    {
                        throw new InvalidOperationException(
                            "Music shuffle bag did not consume every " +
                            "category track before refilling.");
                    }
                }

                MusicShuffleBag singleTrackBag =
                    new MusicShuffleBag(97531);
                RuntimeSoundCatalog.MusicTrackEntry only =
                    singleTrackBag.Take(tracks.GetRange(0, 1), null);
                if (only == null
                    || singleTrackBag.Take(
                            tracks.GetRange(0, 1),
                            only.Clip) != null)
                {
                    throw new InvalidOperationException(
                        "A one-track category violated no-repeat behavior.");
                }
            }
            finally
            {
                for (int i = 0; i < clips.Count; i++)
                UnityEngine.Object.DestroyImmediate(clips[i]);
            }
        }

        private static void ValidateSituationQueue()
        {
            MusicSituationQueue queue =
                new MusicSituationQueue();
            if (!queue.Enqueue(
                    MusicSituation.Inventory)
                || queue.Enqueue(
                    MusicSituation.Inventory)
                || !queue.Enqueue(
                    MusicSituation.MainMenu)
                || queue.Count != 2
                || !queue.Contains(
                    MusicSituation.Inventory)
                || !queue.TryDequeue(
                    out MusicSituation first)
                || first != MusicSituation.Inventory
                || !queue.Enqueue(
                    MusicSituation.Inventory)
                || !queue.TryDequeue(
                    out MusicSituation second)
                || second != MusicSituation.MainMenu
                || !queue.TryDequeue(
                    out MusicSituation third)
                || third != MusicSituation.Inventory
                || queue.TryDequeue(out _)
                || queue.Count != 0)
            {
                throw new InvalidOperationException(
                    "The menu music situation queue is not unique FIFO.");
            }
        }

        private static void ValidateStartScreenSurface()
        {
            Type uiType =
                typeof(RuntimeGameUiController);
            BindingFlags privateInstance =
                BindingFlags.Instance
                | BindingFlags.NonPublic;
            Type rootStateType =
                uiType.GetNestedType(
                    "RootUiState",
                    BindingFlags.NonPublic);
            if (rootStateType == null
                || !Enum.IsDefined(
                    rootStateType,
                    "StartScreen")
                || uiType.GetField(
                    "startScreenLogo",
                    privateInstance)?.FieldType
                    != typeof(Texture2D)
                || uiType.GetField(
                    "startScreenInputSubscription",
                    privateInstance)?.FieldType
                    != typeof(IDisposable)
                || uiType.GetMethod(
                    "DrawStartScreen",
                    privateInstance) == null
                || uiType.GetMethod(
                    "TryBeginStartScreenExit",
                    privateInstance) == null
                || uiType.GetMethod(
                    "UpdateStartScreenTransition",
                    privateInstance) == null)
            {
                throw new InvalidOperationException(
                    "The Start Screen is missing its logo, any-input, " +
                    "render, or transition surface.");
            }

            bool hasTransitionFolder = false;
            for (int i = 0;
                 i < SoundPaths.RequiredFolderKeys.Count;
                 i++)
            {
                if (string.Equals(
                        SoundPaths.RequiredFolderKeys[i],
                        SoundPaths.GuiStartScreenTransition,
                        StringComparison.Ordinal))
                {
                    hasTransitionFolder = true;
                    break;
                }
            }
            if (!hasTransitionFolder)
            {
                throw new InvalidOperationException(
                    "The Start Screen transition sound folder is not required " +
                    "by the runtime catalog.");
            }
        }
    }
}
