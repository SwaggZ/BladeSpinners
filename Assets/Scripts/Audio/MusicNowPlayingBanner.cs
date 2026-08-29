using UnityEngine;

namespace BladeSpinners.Audio
{
    /// <summary>
    /// Lightweight Rocket League-style track credit shown whenever music changes.
    /// Uses unscaled time so it remains smooth in paused inventory screens.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class MusicNowPlayingBanner : MonoBehaviour
    {
        private const float HoldDuration = 4.4f;
        private const float FadeDuration = 0.30f;

        private static MusicNowPlayingBanner instance;

        private MusicTrackInfo track;
        private float shownAt = float.NegativeInfinity;
        private uint lastHandledMusicStartId;
        private uint lastRenderedMusicStartId;
        private bool waitingForFirstDraw;
        private bool waitingForUiRepaint;
        private int requestedAtFrame;
        private int lastGameUiDrawFrame = -1;
        private GUIStyle kickerStyle;
        private GUIStyle titleStyle;
        private GUIStyle authorStyle;
        private GUIStyle noteStyle;

        public static bool IsShowing =>
            instance != null
            && (instance.waitingForFirstDraw
                || instance.GetAlpha() > 0f);
        public static MusicTrackInfo DisplayedTrack =>
            instance != null ? instance.track : default;
        public static bool HasRenderedCurrentStart =>
            instance != null
            && instance.lastHandledMusicStartId != 0u
            && instance.lastRenderedMusicStartId
                == instance.lastHandledMusicStartId;

        public static void NotifyUiRepaintComplete()
        {
            if (instance != null)
                instance.waitingForUiRepaint = false;
        }

        public static void DrawAfterGameUi()
        {
            instance?.DrawOverlay(true);
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || instance != null)
                return;

            MusicNowPlayingBanner existing =
                FindFirstObjectByType<MusicNowPlayingBanner>();
            if (existing != null)
            {
                instance = existing;
                return;
            }

            GameObject host =
                new GameObject("MusicNowPlayingBanner");
            instance = host.AddComponent<MusicNowPlayingBanner>();
            DontDestroyOnLoad(host);
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
        }

        private void OnEnable()
        {
            SoundManager.NowPlayingChanged += HandleNowPlayingChanged;
            ShowCurrentPlaybackIfNew();
        }

        private void Start()
        {
            ShowCurrentPlaybackIfNew();
        }

        private void OnDisable()
        {
            SoundManager.NowPlayingChanged -= HandleNowPlayingChanged;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void HandleNowPlayingChanged(MusicTrackInfo nextTrack)
        {
            if (!nextTrack.IsValid)
                return;

            uint startId = SoundManager.CurrentMusicStartId;
            if (startId != 0u
                && startId == lastHandledMusicStartId)
            {
                return;
            }

            lastHandledMusicStartId = startId;
            track = nextTrack;
            shownAt = Time.unscaledTime;
            waitingForFirstDraw = true;
            waitingForUiRepaint = true;
            requestedAtFrame = Time.frameCount;
            Debug.Log(
                $"[MusicBanner] Showing \"{nextTrack.Title}\" by {nextTrack.Author} " +
                $"for {HoldDuration + FadeDuration * 2f:F2}s.");
        }

        private void ShowCurrentPlaybackIfNew()
        {
            uint startId = SoundManager.CurrentMusicStartId;
            MusicTrackInfo current =
                SoundManager.CurrentMusicTrack;
            if (startId == 0u
                || startId == lastHandledMusicStartId
                || !current.IsValid)
            {
                return;
            }

            HandleNowPlayingChanged(current);
        }

        private float GetAlpha()
        {
            if (!track.IsValid)
                return 0f;

            float elapsed = Time.unscaledTime - shownAt;
            if (elapsed < 0f
                || elapsed > HoldDuration + FadeDuration * 2f)
            {
                return 0f;
            }
            if (elapsed < FadeDuration)
                return Mathf.Clamp01(elapsed / FadeDuration);
            if (elapsed > HoldDuration + FadeDuration)
            {
                return 1f - Mathf.Clamp01(
                    (elapsed - HoldDuration - FadeDuration)
                    / FadeDuration);
            }
            return 1f;
        }

        private void OnGUI()
        {
            if (lastGameUiDrawFrame == Time.frameCount)
                return;

            DrawOverlay(false);
        }

        private void DrawOverlay(bool fromGameUi)
        {
            if (fromGameUi)
                lastGameUiDrawFrame = Time.frameCount;

            if (waitingForUiRepaint
                && Time.frameCount <= requestedAtFrame + 2)
            {
                return;
            }

            if (waitingForFirstDraw)
            {
                waitingForFirstDraw = false;
                shownAt = Time.unscaledTime - 0.01f;
            }

            float alpha = GetAlpha();
            if (alpha <= 0f)
                return;

            EnsureStyles();
            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;
            GUI.depth = -1000;

            float scale = Mathf.Clamp(
                Screen.height / 1080f,
                0.75f,
                1.5f);
            float width = Mathf.Clamp(390f * scale, 300f, 540f);
            float height = Mathf.Clamp(82f * scale, 68f, 112f);
            float margin = Mathf.Clamp(24f * scale, 14f, 34f);
            float easedAlpha = alpha * alpha * (3f - 2f * alpha);
            float x = Mathf.Lerp(
                -width - margin,
                margin,
                easedAlpha);
            float y = Screen.height - height - margin;
            Rect panel = new Rect(x, y, width, height);

            GUI.color = new Color(0.025f, 0.045f, 0.075f, 0.94f * alpha);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(0.12f, 0.82f, 1f, alpha);
            GUI.DrawTexture(
                new Rect(panel.x, panel.y, 5f * scale, panel.height),
                Texture2D.whiteTexture);

            float iconSize = panel.height - 18f * scale;
            Rect icon = new Rect(
                panel.x + 15f * scale,
                panel.y + 9f * scale,
                iconSize,
                iconSize);
            GUI.color = new Color(0.08f, 0.18f, 0.28f, 0.98f * alpha);
            GUI.DrawTexture(icon, Texture2D.whiteTexture);

            Color textColor = new Color(1f, 1f, 1f, alpha);
            GUI.color = textColor;
            if (track.Logo != null)
            {
                GUI.DrawTexture(
                    icon,
                    track.Logo,
                    ScaleMode.ScaleAndCrop,
                    true);
                GUI.color =
                    new Color(0.12f, 0.82f, 1f, alpha);
                DrawBorder(icon, Mathf.Max(1f, scale));
                GUI.color = textColor;
            }
            else
            {
                GUI.Label(icon, ">", noteStyle);
            }

            float textX = icon.xMax + 14f * scale;
            float textWidth = panel.xMax - textX - 12f * scale;
            GUI.Label(
                new Rect(
                    textX,
                    panel.y + 7f * scale,
                    textWidth,
                    18f * scale),
                "NOW PLAYING",
                kickerStyle);
            GUI.Label(
                new Rect(
                    textX,
                    panel.y + 25f * scale,
                    textWidth,
                    29f * scale),
                track.Title,
                titleStyle);
            GUI.Label(
                new Rect(
                    textX,
                    panel.y + 54f * scale,
                    textWidth,
                    19f * scale),
                "BY " + track.Author,
                authorStyle);

            GUI.color = previousColor;
            GUI.depth = previousDepth;
            if (Event.current.type == EventType.Repaint)
            {
                lastRenderedMusicStartId =
                    lastHandledMusicStartId;
            }
        }

        private static void DrawBorder(
            Rect rect,
            float thickness)
        {
            GUI.DrawTexture(
                new Rect(
                    rect.x,
                    rect.y,
                    rect.width,
                    thickness),
                Texture2D.whiteTexture);
            GUI.DrawTexture(
                new Rect(
                    rect.x,
                    rect.yMax - thickness,
                    rect.width,
                    thickness),
                Texture2D.whiteTexture);
            GUI.DrawTexture(
                new Rect(
                    rect.x,
                    rect.y,
                    thickness,
                    rect.height),
                Texture2D.whiteTexture);
            GUI.DrawTexture(
                new Rect(
                    rect.xMax - thickness,
                    rect.y,
                    thickness,
                    rect.height),
                Texture2D.whiteTexture);
        }

        private void EnsureStyles()
        {
            int baseSize = Mathf.RoundToInt(
                Mathf.Clamp(Screen.height * 0.016f, 12f, 22f));
            if (titleStyle != null
                && titleStyle.fontSize == baseSize + 3)
            {
                return;
            }

            kickerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.Max(10, baseSize - 3),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.12f, 0.82f, 1f) }
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = baseSize + 3,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
                normal = { textColor = Color.white }
            };
            authorStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.Max(10, baseSize - 2),
                clipping = TextClipping.Clip,
                normal =
                {
                    textColor =
                        new Color(0.72f, 0.82f, 0.90f)
                }
            };
            noteStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = baseSize + 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
    }
}
