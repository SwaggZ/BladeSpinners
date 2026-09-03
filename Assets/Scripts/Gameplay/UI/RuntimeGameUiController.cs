using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using BladeSpinners.Core;
using BladeSpinners.Gameplay;
using BladeSpinners.Abilities;
using BladeSpinners.Audio;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.Shrine;
using BladeSpinners.Gameplay.Combat;
using BladeSpinners.Gameplay.Progression;

namespace BladeSpinners.Gameplay.UI
{
    public class RuntimeGameUiController : MonoBehaviour
    {
        private const string PartsDebugSceneName = "PartsDebugScene";

        // ── Enum types ───────────────────────────────────────────────────────────
        private enum RootUiState
        {
            StartScreen,
            MainMenu,
            InRun,
            Paused,
            BetweenArenas
        }
        private enum MenuPanel
        {
            Home,
            Inventory,
            ShrineCompendium,
            Minigames,
            Records,
            Settings,
            Keybinds,
            Shrine
        }

        // ── Singleton ────────────────────────────────────────────────────────────
        private static RuntimeGameUiController instance;

        // ── State ────────────────────────────────────────────────────────────────
        private RootUiState rootState     = RootUiState.StartScreen;
        private MenuPanel   mainMenuPanel = MenuPanel.Home;
        private MenuPanel   pausePanel    = MenuPanel.Home;
        private Vector2     compendiumScrollPos;
        private int         compendiumRarityFilter = -1;
        private int         compendiumCategoryFilter = -1;
        private string      compendiumSearchQuery = string.Empty;
        private List<ShrinePerkType> lastRunUnlockedBlessings = new List<ShrinePerkType>();
        private bool        showRunVictoryModal = false;
        private MusicSituation? requestedMusicSituation;
        private const float StartScreenInputDelay = 0.45f;
        private const float StartScreenExitDuration = 1.15f;
        private const string StartScreenCatchphrase =
            "BUILD YOUR BLADE. CLAIM THE ARENA.";
        private float startScreenEnteredAt;
        private float startScreenExitStartedAt = -1f;
        private Texture2D startScreenLogo;
        private IDisposable startScreenInputSubscription;

        private RuntimeRunBuilder.RunContext runContext;
        private Camera fallbackMenuCamera;
        private readonly Dictionary<PartType, BeyPart> selectedMainMenuLoadout =
            new Dictionary<PartType, BeyPart>();
        private List<BeyPart> ownedParts = new List<BeyPart>();
        private List<BeyPart> enemyParts = new List<BeyPart>();
        private PartType? selectedInventorySlot;
        private BeyPart selectedInventoryPart;
        private BeyPart selectedLootPart;
        private float runElapsedSeconds;
        private float arenaElapsedSeconds;
        private int arenasClearedThisRun;
        private bool hasActiveRun;
        private bool runRecordSubmitted;

        private Vector2 ownedScroll;
        private Vector2 runScroll;
        private Vector2 garageSwapScroll;
        private Vector2 settingsScroll;
        private PartType? garageInspectSlot = PartType.EnergyRing;
        private bool buildSlotPickerOpen;
        private readonly Dictionary<PartType, BeyPart>[] savedBuildSlots = new Dictionary<PartType, BeyPart>[3];
        private readonly string[] savedBuildNames = new string[3];
        private string transientUiMessage = string.Empty;
        private float transientUiMessageUntil;

        // ── Preview bey ──────────────────────────────────────────────────────────
        private RenderTexture previewTexture;
        private Camera        previewCamera;
        private Transform     previewTiltPivot;
        private Transform     previewSpinChild;
        private BeyAssembler  previewAssembler;
        private BeyConfiguration previewConfig;
        private bool previewRenderQueued;
        private int previewLoadoutHash = int.MinValue;
        private bool previewIsDragging;
        private int previewDragPointerId = -1;
        private Vector2 previewLastPointerPos;
        private float previewManualPitch;

        // Per-part 3D preview system
        private Camera partPreviewCamera;
        private Transform partPreviewRoot;
        private Dictionary<PartType, RenderTexture> partPreviewTextures = new Dictionary<PartType, RenderTexture>();
        private Dictionary<PartType, GameObject> partPreviewObjects = new Dictionary<PartType, GameObject>();
        private bool partPreviewsDirty;

        // Swap-modal per-part preview cache
        private Dictionary<int, RenderTexture> swapPartPreviewCache = new Dictionary<int, RenderTexture>();
        private PartType? lastRenderedSwapSlot;
        private bool swapPreviewsDirty;
        private List<BeyPart> swapPreviewQueue;

        // ── Settings sliders ────────────────────────────────────────────────────
        private float settingsMasterVolume =
            AudioMixLevels.DefaultMaster;
        private float settingsSoundEffectsVolume =
            AudioMixLevels.DefaultSoundEffects;
        private float settingsMusicVolume =
            AudioMixLevels.DefaultMusic;
        private float settingsGuiVolume =
            AudioMixLevels.DefaultGui;
        private float settingsSensitivity = 1f;
        private float settingsClippingOpacity = 0.2f;
        private float settingsRingsOpacity = 1f;
        private bool showResetConfirm = false;

        // ── Resolution dropdown state ─────────────────────────────────────────────
        private static readonly (string label, int w, int h)[] ResolutionPresets = new[]
        {
            ("4K  —  3840 x 2160",  3840, 2160),
            ("1440p  —  2560 x 1440", 2560, 1440),
            ("1080p  —  1920 x 1080", 1920, 1080),
            ("900p  —  1600 x 900",  1600, 900),
            ("720p  —  1280 x 720",  1280, 720),
        };
        private bool resolutionDropdownOpen = false;
        private int pendingResolutionIndex = -1;   // which preset was applied but not confirmed
        private int confirmedResolutionIndex = -1; // last confirmed preset (-1 = original)
        private int prevResW = 0;
        private int prevResH = 0;
        private float revertResolutionTimer = 0f;
        private const float RevertResolutionSeconds = 15f;

        // ── Loot transfer state ──────────────────────────────────────────────────
        private List<BeyPart> lootEligibleParts;
        private List<bool>    lootSelectedFlags;
        private int           lootMaxTransferCount;
        private RarityTier    lootMaxRarityTier;
        private bool          lootTransferInitialized;
        private Vector2       lootScroll;

        // ── GUI styles ───────────────────────────────────────────────────────────
        private GUIStyle titleBarStyle;
        private GUIStyle navButtonStyle;
        private GUIStyle startButtonStyle;
        private GUIStyle inlineActionButtonStyle;
        private GUIStyle sectionLabelStyle;
        private GUIStyle bodyLabelStyle;
        private GUIStyle statRowStyle;
        private GUIStyle listItemStyle;
        private GUIStyle sliderTrackStyle;
        private GUIStyle sliderThumbStyle;
        private Texture2D listTex;
        private Texture2D sliderTrackTex;
        private Texture2D sliderThumbTex;
        private int styleScreenW = -1;
        private int styleScreenH = -1;
        private bool deathOverlayPreviewPrepared;

        // ── Stardew Valley-Style Fishing Rip Launcher State ─────────────────────
        private float ripBobberPos = 0f;
        private float ripBobberVel = 0f;
        private float ripTargetPos = 0.5f;
        private float ripTensionCharge = 0f;
        private float ripTargetTimer = 0f;
        private MatchManager lastCountdownMatch;

        // ── Build safety ─────────────────────────────────────────────────────────
        private bool   initFailed;
        private string initErrorMsg = "";
        // ── Palette ──────────────────────────────────────────────────────────────
        private static readonly Color BG_BLACK   = new Color(0.06f, 0.06f, 0.07f, 1f);
        private static readonly Color BG_NAVY    = new Color(0.03f, 0.05f, 0.10f, 1f);
        private static readonly Color PANEL_DARK = new Color(0.09f, 0.09f, 0.11f, 0.97f);
        private static readonly Color PANEL_GLASS = new Color(0.06f, 0.07f, 0.10f, 0.86f);
        private static readonly Color PANEL_STEEL = new Color(0.10f, 0.11f, 0.15f, 0.95f);
        private static readonly Color ACCENT_YEL = new Color(1f, 0.87f, 0.00f, 1f);
        private static readonly Color ACCENT_GOLD = new Color(1f, 0.85f, 0.20f, 1f);
        private static readonly Color ACCENT_ORANGE = new Color(1f, 0.44f, 0.12f, 1f);
        private static readonly Color ACCENT_CYAN = new Color(0.12f, 0.82f, 1f, 1f);
        private static readonly Color ACCENT_MAGENTA = new Color(1f, 0.23f, 0.56f, 1f);
        private static readonly Color ACCENT_RED = new Color(1f, 0.16f, 0.08f, 1f);
        private static readonly Color BTN_DARK   = new Color(0.12f, 0.13f, 0.16f, 1f);
        private static readonly Color LIST_BG    = new Color(0.11f, 0.12f, 0.15f, 1f);
        private static readonly Color OVERLAY    = new Color(0f, 0f, 0f, 0.76f);
        private static readonly Color RED_DANGER = new Color(0.65f, 0.07f, 0.07f, 1f);

        private static readonly PartType[] PART_DISPLAY_ORDER = { PartType.FaceBolt, PartType.EnergyRing, PartType.FusionWheel, PartType.Track, PartType.Tip };

        private const string StarterConfigResourcePath = "StarterPartsConfig";
        private readonly BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        // ══════════════════════════════════════════════════════════════════════════
        //  BOOTSTRAP
        // ══════════════════════════════════════════════════════════════════════════

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (IsPartsDebugSceneActive())
                return;

            Debug.Log("[BladeSpinners] Bootstrap() called");
            if (instance != null) return;

            RuntimeGameUiController existing = FindFirstObjectByType<RuntimeGameUiController>();
            if (existing != null)
            {
                instance = existing;
                Debug.Log("[BladeSpinners] Bootstrap: found existing scene instance");
                return;
            }

            Debug.Log("[BladeSpinners] Bootstrap: creating new RuntimeGameUiController");
            GameObject go = new GameObject("RuntimeGameUiController");
            instance = go.AddComponent<RuntimeGameUiController>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (IsPartsDebugSceneActive())
            {
                Destroy(gameObject);
                return;
            }

            Debug.Log("[BladeSpinners] Awake() called");
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAudioSettings();
            startScreenEnteredAt = Time.unscaledTime;
            startScreenLogo =
                Resources.Load<Texture2D>("UI/GameLogo");
            startScreenInputSubscription =
                InputSystem.onAnyButtonPress.Call(
                    _ => TryBeginStartScreenExit());

            // Always start with cursor free (main menu)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            try
            {
                EnsureFallbackMenuCamera();
                Debug.Log("[BladeSpinners] Fallback camera ready");
            }
            catch (Exception e)
            {
                Debug.LogError($"[BladeSpinners] Camera init failed: {e}");
            }

            try
            {
                BuildStarterData();
                Debug.Log($"[BladeSpinners] Starter data: owned={ownedParts?.Count}, enemy={enemyParts?.Count}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[BladeSpinners] BuildStarterData failed: {e}");
                initFailed   = true;
                initErrorMsg = $"BuildStarterData: {e.Message}";
                // Ensure minimum data for rendering
                if (ownedParts == null || ownedParts.Count == 0)
                {
                    ownedParts = RuntimePartFactory.CreateStarterCatalog(1, Environment.TickCount);
                    enemyParts = new List<BeyPart>(ownedParts);
                    BuildDefaultLoadout();
                }
            }

            try
            {
                EnsurePreviewSetup();
                RefreshPreviewFromLoadout(selectedMainMenuLoadout);
                Debug.Log("[BladeSpinners] Preview setup complete");
            }
            catch (Exception e)
            {
                Debug.LogError($"[BladeSpinners] Preview init failed: {e}");
                // Non-fatal: menu still works without preview
            }

            Debug.Log("[BladeSpinners] Awake() finished successfully");
        }

        private void OnDestroy()
        {
            startScreenInputSubscription?.Dispose();
            startScreenInputSubscription = null;
            if (instance == this)
                instance = null;
        }

        private static bool IsPartsDebugSceneActive()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            return activeScene.IsValid() && string.Equals(activeScene.name, PartsDebugSceneName, StringComparison.Ordinal);
        }

        private void Update()
        {
            if (rootState == RootUiState.StartScreen)
            {
                UpdateStartScreenTransition();
                if (rootState == RootUiState.StartScreen)
                {
                    UpdateCursorState();
                    UpdateMusicSituation();
                    return;
                }
            }

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.f11Key.wasPressedThisFrame || (kb.altKey.isPressed && kb.enterKey.wasPressedThisFrame))
                {
                    Screen.fullScreen = !Screen.fullScreen;
                }
            }

            if (rootState == RootUiState.InRun || rootState == RootUiState.Paused || rootState == RootUiState.BetweenArenas)
            {
                Gamepad gp = Gamepad.current;
                bool pausePressed = false;
                if (kb != null && kb.escapeKey.wasPressedThisFrame) pausePressed = true;
                if (gp != null && (gp.startButton.wasPressedThisFrame || gp.selectButton.wasPressedThisFrame)) pausePressed = true;

                if (pausePressed)
                    TogglePause();
            }

            UpdateCursorState();
            UpdateRunTimersAndRecords();

            // Resolution revert countdown
            if (pendingResolutionIndex >= 0 && revertResolutionTimer > 0f)
            {
                revertResolutionTimer -= Time.unscaledDeltaTime;
                if (revertResolutionTimer <= 0f)
                {
                    // Revert
                    Screen.SetResolution(prevResW, prevResH, Screen.fullScreenMode);
                    pendingResolutionIndex = -1;
                    revertResolutionTimer = 0f;
                }
            }

            // Minigame Arcade Session Update
            if (rootState == RootUiState.MainMenu && mainMenuPanel == MenuPanel.Minigames && MinigameArcadeManager.State == ArcadeState.Playing)
            {
                Mouse mouse = Mouse.current;
                bool lmbPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
                bool lmbHeld = mouse != null && mouse.leftButton.isPressed;
                MinigameArcadeManager.UpdateSession(lmbPressed, lmbHeld, Time.unscaledDeltaTime);
            }


            if (rootState != RootUiState.InRun
                || runContext.Match == null
                || runContext.Match.CurrentState != MatchManager.MatchState.PlayerLost)
            {
                deathOverlayPreviewPrepared = false;
                lootTransferInitialized = false;
            }

            HandleRunProgressionAdvance();
            UpdateMusicSituation();

            // Spin the preview bey model
            if (previewSpinChild != null)
                previewSpinChild.Rotate(Vector3.up, 240f * Time.unscaledDeltaTime, Space.Self);

            if (ShouldRenderPreviewThisFrame())
                previewRenderQueued = true;
        }

        private void LateUpdate()
        {
            if (!previewRenderQueued || previewCamera == null || previewTexture == null)
                return;

            previewCamera.Render();

            if (partPreviewsDirty)
                RenderPartPreviews();

            if (swapPreviewsDirty)
                RenderSwapPreviews();

            previewRenderQueued = false;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  ONGUI DISPATCH
        // ══════════════════════════════════════════════════════════════════════════

        public static float UiWidth => 1080f * ((float)Screen.width / Mathf.Max(1, Screen.height));
        public static float UiHeight => 1080f;

        private void OnGUI()
        {
            try
            {
                float scale = Screen.height / 1080f;
                Matrix4x4 origMatrix = GUI.matrix;
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1.0f));

                EnsureStyles();
                switch (rootState)
                {
                    case RootUiState.StartScreen: DrawStartScreen(); break;
                    case RootUiState.MainMenu: DrawMainMenu(); break;
                    case RootUiState.Paused:   DrawPauseMenu(); break;
                    case RootUiState.BetweenArenas: DrawArenaIntermissionMenu(); break;
                }

                if (rootState == RootUiState.InRun)
                    DrawInRunOverlays();

                if (initFailed)
                {
                    GUI.color = Color.red;
                    GUI.Label(new Rect(10, UiHeight - 30, UiWidth, 24),
                        $"[Init Warning] {initErrorMsg}");
                    GUI.color = Color.white;
                }

                GUI.matrix = origMatrix;
            }
            catch (Exception e)
            {
                // Emergency fallback — render error on screen so builds aren't silent
                GUI.color = Color.red;
                GUI.Label(new Rect(10, 10, UiWidth - 20, 60),
                    $"[UI Error] {e.Message}");
                GUI.color = Color.white;
                Debug.LogError($"[BladeSpinners] OnGUI exception: {e}");
            }
            finally
            {
                if (Event.current != null
                    && Event.current.type
                        == EventType.Repaint)
                {
                    MusicNowPlayingBanner
                        .NotifyUiRepaintComplete();
                }
                MusicNowPlayingBanner.DrawAfterGameUi();
            }
        }

        private void DrawStartScreen()
        {
            Rect screen = new Rect(0f, 0f, UiWidth, UiHeight);
            // Deep gradient from dark sci-fi navy to obsidian
            DrawVerticalGradient(screen, new Color(0.006f, 0.018f, 0.055f, 1f), new Color(0.001f, 0.003f, 0.010f, 1f), 32);

            float now = Time.unscaledTime;
            float exitProgress = startScreenExitStartedAt < 0f
                ? 0f
                : Mathf.Clamp01((now - startScreenExitStartedAt) / StartScreenExitDuration);
            float easedExit = exitProgress * exitProgress * (3f - 2f * exitProgress);

            // 1. Perspective Cyber-Grid on the lower arena floor
            DrawStartScreenGrid(screen, now, easedExit);

            // 2. Ambient Rising Stardust & Anime Embers
            DrawStartScreenStars(screen, now, easedExit);

            // 3. Central Arena Stadium Hologram Ring
            DrawArenaBurstMotif(new Rect(screen.x, screen.y + screen.height * 0.10f, screen.width, screen.height * 0.58f));

            // 4. Top Tech Metadata Headers
            float metaAlpha = (1f - easedExit) * 0.85f;
            GUIStyle techMetaStyle = CreateStaticStyle(
                bodyLabelStyle,
                new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, metaAlpha),
                ScaleFont(11f),
                TextAnchor.MiddleLeft,
                FontStyle.Bold);
            GUI.Label(new Rect(24f, 16f, 420f, 22f), "⬢ SYSTEM: ONLINE // HYPER-SPIN PROTOCOL", techMetaStyle);

            GUIStyle techRightStyle = CreateStaticStyle(
                bodyLabelStyle,
                new Color(ACCENT_YEL.r, ACCENT_YEL.g, ACCENT_YEL.b, metaAlpha),
                ScaleFont(11f),
                TextAnchor.MiddleRight,
                FontStyle.Bold);
            GUI.Label(new Rect(screen.width - 444f, 16f, 420f, 22f), "ENGINE: UNIVERSAL URP // 60 FPS HI-FI", techRightStyle);

            // 5. Hero Logo
            float logoWidth = Mathf.Clamp(screen.width * 0.58f, 440f, 1020f);
            float logoHeight = Mathf.Clamp(screen.height * 0.32f, 190f, 390f);
            float logoScale = 1f + easedExit * 0.22f;
            Rect logoRect = new Rect(
                screen.center.x - logoWidth * logoScale * 0.5f,
                screen.height * 0.22f - logoHeight * (logoScale - 1f) * 0.5f,
                logoWidth * logoScale,
                logoHeight * logoScale);

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 1f - easedExit);
            if (startScreenLogo != null)
            {
                GUI.DrawTexture(logoRect, startScreenLogo, ScaleMode.ScaleToFit, true);
            }
            else
            {
                DrawPlaceholderStartLogo(logoRect, now);
            }

            // 6. Anime Subtitle Badge
            GUIStyle catchphraseStyle = CreateStaticStyle(
                sectionLabelStyle,
                new Color(0.78f, 0.92f, 1f, 1f - easedExit),
                ScaleFont(22f),
                TextAnchor.MiddleCenter,
                FontStyle.Bold);
            Rect phraseRect = new Rect(screen.width * 0.20f, screen.height * 0.58f, screen.width * 0.60f, Mathf.Clamp(screen.height * 0.06f, 36f, 60f));
            DrawPanelFrame(phraseRect, new Color(0.02f, 0.06f, 0.12f, 0.85f * (1f - easedExit)), new Color(0.04f, 0.10f, 0.20f, 0.90f * (1f - easedExit)), new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.75f * (1f - easedExit)), 2f);
            DrawFrameCorners(phraseRect, new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 1f - easedExit), 14f, 1.5f);
            DrawFittedLabel(phraseRect, "爆転ブレード // EXTREME BURST ARENA", catchphraseStyle, new Color(0.78f, 0.92f, 1f, 1f - easedExit), 12);

            // 7. Interactive Breathing "PRESS TO START" Prompt
            if (startScreenExitStartedAt < 0f)
            {
                float breath = 0.60f + 0.40f * (0.5f + 0.5f * Mathf.Sin(now * 3.2f));
                float promptW = Mathf.Clamp(screen.width * 0.44f, 380f, 640f);
                float promptH = Mathf.Clamp(screen.height * 0.08f, 52f, 74f);
                Rect promptRect = new Rect((screen.width - promptW) * 0.5f, screen.height * 0.74f, promptW, promptH);

                Color promptBorder = new Color(ACCENT_YEL.r, ACCENT_YEL.g, ACCENT_YEL.b, breath);
                DrawPanelFrame(promptRect, new Color(0.02f, 0.05f, 0.10f, 0.92f * breath), new Color(0.05f, 0.09f, 0.18f, 0.95f * breath), promptBorder, 2.5f);
                DrawFrameCorners(promptRect, promptBorder, 20f, 2f);
                DrawMotionBandClipped(new Rect(promptRect.x + promptRect.width * 0.55f, promptRect.y, promptRect.width * 0.35f, promptRect.height), promptBorder, 8f, 12f, 0.10f * breath);

                GUIStyle promptStyle = CreateStaticStyle(
                    bodyLabelStyle,
                    new Color(1f, 0.92f, 0.35f, breath),
                    ScaleFont(20f),
                    TextAnchor.MiddleCenter,
                    FontStyle.Bold);
                GUI.Label(promptRect, "PRESS ANY KEY OR CLICK TO ENTER", promptStyle);
            }
            GUI.color = previousColor;

            // 8. Bottom Cyber Footer
            GUIStyle footerStyle = CreateStaticStyle(
                bodyLabelStyle,
                new Color(0.45f, 0.60f, 0.75f, (1f - easedExit) * 0.75f),
                ScaleFont(11f),
                TextAnchor.MiddleCenter);
            GUI.Label(new Rect(0f, screen.height - 32f, screen.width, 24f), "© 2026 BLADE SPINNERS ARCADE // ALL SYSTEMS OPERATIONAL", footerStyle);

            if (exitProgress > 0f)
            {
                float ringSize = Mathf.Lerp(screen.height * 0.12f, screen.width * 1.10f, easedExit);
                Rect ring = new Rect(screen.center.x - ringSize * 0.5f, screen.center.y - ringSize * 0.5f, ringSize, ringSize);
                DrawFrameCorners(ring, new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, (1f - easedExit) * 0.85f), ringSize * 0.12f, Mathf.Clamp(5f * (1f - easedExit), 1f, 5f));
                float flashAlpha = Mathf.Clamp01((exitProgress - 0.68f) / 0.32f);
                DrawRect(screen, new Color(0.55f, 0.88f, 1f, flashAlpha));
            }
        }

        private static void DrawStartScreenGrid(Rect screen, float time, float launchProgress)
        {
            float horizonY = screen.height * 0.52f;
            float gridAlpha = (1f - launchProgress) * 0.35f;
            if (gridAlpha <= 0.01f) return;

            // Radiating perspective lines toward vanishing point at (screen.center.x, horizonY)
            Vector2 vanishPoint = new Vector2(screen.center.x, horizonY);
            const int lineCount = 18;
            for (int i = 0; i <= lineCount; i++)
            {
                float bottomX = Mathf.Lerp(-screen.width * 0.2f, screen.width * 1.2f, (float)i / lineCount);
                DrawLine(vanishPoint, new Vector2(bottomX, screen.height), new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, gridAlpha * 0.6f), 1.2f);
            }

            // Scrolling horizontal perspective grid lines
            const int horizCount = 9;
            float scroll = Mathf.Repeat(time * 0.45f, 1f / horizCount);
            for (int i = 0; i < horizCount; i++)
            {
                float t = Mathf.Pow((float)i / horizCount + scroll, 2.2f);
                if (t > 1f) continue;
                float lineY = Mathf.Lerp(horizonY, screen.height, t);
                float lineAlpha = gridAlpha * t;
                DrawRect(new Rect(screen.x, lineY, screen.width, 1.5f), new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, lineAlpha));
            }

            // Horizon Glow Line
            DrawRect(new Rect(screen.x, horizonY - 1f, screen.width, 2.5f), new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, gridAlpha * 1.5f));
        }

        private static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)
        {
            Vector2 delta = pointB - pointA;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            float length = delta.magnitude;

            GUIUtility.RotateAroundPivot(angle, pointA);
            DrawRect(new Rect(pointA.x, pointA.y - width * 0.5f, length, width), color);
            GUIUtility.RotateAroundPivot(-angle, pointA);
        }

        private static void DrawStartScreenStars(Rect screen, float time, float launchProgress)
        {
            const int StarCount = 110;
            for (int i = 0; i < StarCount; i++)
            {
                float x = StartScreenHash01(i * 92821 + 17);
                float ySeed = StartScreenHash01(i * 68917 + 71);
                float speed = Mathf.Lerp(4f, 18f, StartScreenHash01(i * 31337 + 29));
                float y = Mathf.Repeat(ySeed * screen.height + time * speed, screen.height);
                float twinkle = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(time * (1.1f + speed * 0.08f) + i));
                float size = Mathf.Lerp(1.2f, 3.8f, StartScreenHash01(i * 47293 + 43));
                float streak = launchProgress * Mathf.Lerp(18f, 105f, speed / 18f);

                Color starColor = i % 3 == 0
                    ? new Color(1f, 0.85f, 0.3f, twinkle * (1f - launchProgress * 0.55f))
                    : new Color(0.48f, 0.88f, 1f, twinkle * (1f - launchProgress * 0.55f));

                DrawRect(new Rect(screen.x + x * screen.width, screen.y + y, size, size + streak), starColor);
            }
        }

        private void DrawPlaceholderStartLogo(Rect rect, float time)
        {
            float pulse = 0.78f + 0.22f * (0.5f + 0.5f * Mathf.Sin(time * 1.4f));

            // Stylized Cyber Blade Emblem Icon
            float emblemSize = Mathf.Min(rect.height * 0.48f, 130f);
            Rect emblemRect = new Rect(rect.center.x - emblemSize * 0.5f, rect.y + 4f, emblemSize, emblemSize);

            // Rotating cyber blade icon
            DrawFrameCorners(emblemRect, new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, pulse), emblemSize * 0.35f, 3f);
            DrawRect(new Rect(emblemRect.x + emblemSize * 0.15f, emblemRect.center.y - 2f, emblemSize * 0.70f, 4f), ACCENT_CYAN);
            DrawRect(new Rect(emblemRect.center.x - 2f, emblemRect.y + emblemSize * 0.15f, 4f, emblemSize * 0.70f), ACCENT_ORANGE);
            DrawRect(new Rect(emblemRect.center.x - 8f, emblemRect.center.y - 8f, 16f, 16f), ACCENT_YEL);

            // Double-Layered 3D Metallic Title
            Rect titleRect = new Rect(rect.x, rect.y + emblemSize + 8f, rect.width, rect.height - emblemSize - 8f);

            GUIStyle shadowStyle = CreateStaticStyle(
                titleBarStyle,
                new Color(0f, 0.05f, 0.12f, 0.95f),
                Mathf.Clamp(Mathf.RoundToInt(60f * GetUiScale()), 32, 108),
                TextAnchor.MiddleCenter,
                FontStyle.Bold);

            GUIStyle titleStyle = CreateStaticStyle(
                titleBarStyle,
                Color.white,
                Mathf.Clamp(Mathf.RoundToInt(60f * GetUiScale()), 32, 108),
                TextAnchor.MiddleCenter,
                FontStyle.Bold);

            // Drop shadow offsets
            GUI.Label(new Rect(titleRect.x + 4f, titleRect.y + 4f, titleRect.width, titleRect.height), "BLADE SPINNERS", shadowStyle);
            GUI.Label(new Rect(titleRect.x - 2f, titleRect.y, titleRect.width, titleRect.height), "BLADE SPINNERS", shadowStyle);
            GUI.Label(titleRect, "BLADE SPINNERS", titleStyle);

            // Sliding laser gleam sweep
            float gleamCycle = Mathf.Repeat(time * 0.35f, 1f);
            if (gleamCycle < 0.35f)
            {
                float gleam01 = gleamCycle / 0.35f;
                float gleamX = Mathf.Lerp(titleRect.x + titleRect.width * 0.15f, titleRect.x + titleRect.width * 0.85f, gleam01);
                DrawRect(new Rect(gleamX, titleRect.y + titleRect.height * 0.25f, 18f, titleRect.height * 0.55f), new Color(1f, 1f, 1f, 0.35f * Mathf.Sin(gleam01 * Mathf.PI)));
            }
        }

        private static float StartScreenHash01(int value)
        {
            unchecked
            {
                uint hash = (uint)value;
                hash ^= hash >> 16;
                hash *= 0x7feb352d;
                hash ^= hash >> 15;
                hash *= 0x846ca68b;
                hash ^= hash >> 16;
                return (hash & 0x00ffffff)
                    / 16777215f;
            }
        }

        private void TryBeginStartScreenExit()
        {
            if (rootState != RootUiState.StartScreen
                || startScreenExitStartedAt >= 0f
                || Time.unscaledTime
                    - startScreenEnteredAt
                    < StartScreenInputDelay)
            {
                return;
            }

            startScreenExitStartedAt =
                Time.unscaledTime;
            startScreenInputSubscription?.Dispose();
            startScreenInputSubscription = null;
            SoundManager.PlayUi(
                SoundPaths.GuiStartScreenTransition);
            Debug.Log(
                "[BladeSpinners] Start Screen transition began.");
        }

        private void UpdateStartScreenTransition()
        {
            if (startScreenExitStartedAt < 0f
                || Time.unscaledTime
                    - startScreenExitStartedAt
                    < StartScreenExitDuration)
            {
                return;
            }

            rootState = RootUiState.MainMenu;
            mainMenuPanel = MenuPanel.Home;
            requestedMusicSituation =
                MusicSituation.MainMenu;
            SoundManager.PlayMusicSituation(
                MusicSituation.MainMenu,
                true);
            EnsureFallbackMenuCamera();
            UpdateCursorState();
            Debug.Log(
                "[BladeSpinners] Start Screen transition completed.");
        }

        private void DrawInRunOverlays()
        {
            MatchManager match = runContext.Match;
            if (match == null)
                return;

            if (match.CurrentState == MatchManager.MatchState.PlayerWon)
            {
                DrawVictoryCelebrationOverlay(match);
                return;
            }

            if (match.CurrentState == MatchManager.MatchState.PlayerLost)
            {
                if (match.StateTimer > 0.6f)
                    DrawDramaticDefeatOverlay(match);
                else
                    DrawDeathOverlay(match);
                return;
            }

            DrawRunDepthOverlay();
            DrawEnergyRingPassiveOverlay();
            DrawActiveShrinePerksOverlay();
            DrawBladeLockOverlay();
            DrawInGameCombatHud();

            if (match.CurrentState == MatchManager.MatchState.WaitingToStart)
                DrawStartCountdownOverlay(match);

            DrawComicPopups();
        }

        private void DrawEnergyRingPassiveOverlay()
        {
            BeyConfiguration configuration =
                runContext.Player?.BeyConfiguration;
            EnergyRingPassiveRuntime passiveRuntime =
                configuration?.EnergyRingPassive;
            BeyPassive passive = passiveRuntime?.ActivePassive;
            if (passive == null)
                return;

            int sw = Mathf.RoundToInt(UiWidth);
            int sh = Mathf.RoundToInt(UiHeight);
            float panelW = Mathf.Clamp(
                sw * 0.26f, 300f, 520f);
            float panelH = Mathf.Clamp(
                sh * 0.072f, 66f, 102f);
            Rect panel = new Rect(
                Mathf.Clamp(sw * 0.008f, 14f, 28f),
                Mathf.Clamp(sh * 0.085f, 76f, 132f),
                panelW,
                panelH);

            bool showProc =
                Time.unscaledTime
                    - passiveRuntime.LastFeedbackTime
                <= 1.8f;
            Color border = showProc
                ? ACCENT_YEL
                : new Color(
                    ACCENT_CYAN.r,
                    ACCENT_CYAN.g,
                    ACCENT_CYAN.b,
                    0.75f);
            DrawPanelFrame(
                panel,
                new Color(0.02f, 0.06f, 0.12f, 0.86f),
                new Color(0.03f, 0.09f, 0.16f, 0.90f),
                border,
                showProc ? 3f : 2f);

            GUIStyle passiveNameStyle =
                new GUIStyle(sectionLabelStyle)
                {
                    fontSize = Mathf.RoundToInt(
                        Mathf.Clamp(
                            sh * 0.017f, 14f, 27f)),
                    alignment = TextAnchor.MiddleLeft
                };
            GUIStyle passiveProcStyle =
                new GUIStyle(bodyLabelStyle)
                {
                    fontSize = Mathf.RoundToInt(
                        Mathf.Clamp(
                            sh * 0.015f, 12f, 23f)),
                    alignment = TextAnchor.MiddleLeft
                };
            passiveProcStyle.normal.textColor =
                showProc ? ACCENT_YEL : ACCENT_CYAN;

            float pad = Mathf.Clamp(
                panel.height * 0.15f, 9f, 15f);
            GUI.Label(
                new Rect(
                    panel.x + pad,
                    panel.y + 5f,
                    panel.width - pad * 2f,
                    panel.height * 0.44f),
                $"PASSIVE  //  {passive.PassiveName.ToUpperInvariant()}",
                passiveNameStyle);
            GUI.Label(
                new Rect(
                    panel.x + pad,
                    panel.y + panel.height * 0.45f,
                    panel.width - pad * 2f,
                    panel.height * 0.42f),
                showProc
                    ? passiveRuntime.LastFeedbackMessage.ToUpperInvariant()
                    : "ENERGY RING ONLINE",
                passiveProcStyle);
        }

        private void DrawActiveShrinePerksOverlay()
        {
            BladerShrineRunState shrine = runContext.ShrineState;
            if (shrine == null || shrine.ActivePerks.Count == 0)
                return;

            float uiScale = GetUiScale();
            float startX = Mathf.Clamp(UiWidth * 0.008f, 14f, 28f);
            float startY = Mathf.Clamp(UiHeight * 0.170f, 155f, 250f);
            float badgeH = Mathf.Clamp(24f * uiScale, 20f, 32f);
            float badgeW = Mathf.Clamp(180f * uiScale, 150f, 230f);

            int idx = 0;
            foreach (ShrinePerkType perkType in shrine.ActivePerks)
            {
                ShrinePerkData data = ShrinePerkCatalog.GetPerk(perkType);
                if (data == null) continue;

                Color perkRarityColor = GetRarityColor(data.Rarity);
                Rect bRect = new Rect(startX, startY + (badgeH + 4f) * idx, badgeW, badgeH);
                DrawRect(bRect, new Color(0.02f, 0.05f, 0.10f, 0.85f));
                DrawFrameCorners(bRect, perkRarityColor, 6f, 1.2f);
                DrawFittedLabel(new Rect(bRect.x + 6f, bRect.y + 2f, bRect.width - 12f, bRect.height - 4f), $"{data.IconSymbol} {data.Name}", bodyLabelStyle, perkRarityColor, 9);
                idx++;
            }
        }

        private void DrawBladeLockOverlay()
        {
            BladeLockDuelManager duel = BladeLockDuelManager.Instance;
            if (duel == null || !duel.IsInBladeLock)
                return;

            float uiScale = GetUiScale();
            float meter = duel.ClashMeter; // 0 (Enemy) to 1 (Player)
            float timeLeft = Mathf.Max(0f, duel.DurationRemaining);
            float totalTime = Mathf.Max(0.1f, duel.TotalDuration);
            float timeRatio = Mathf.Clamp01(timeLeft / totalTime);

            int sw = Mathf.RoundToInt(UiWidth);
            int sh = Mathf.RoundToInt(UiHeight);

            bool isOrbital = duel.CurrentMinigame == ClashMinigameType.OrbitalCrosshair;
            float panelW = Mathf.Clamp(620f * uiScale, 500f, 920f);
            float panelH = isOrbital
                ? Mathf.Clamp(290f * uiScale, 260f, 380f)
                : Mathf.Clamp(200f * uiScale, 170f, 290f);
            Rect panel = new Rect(sw * 0.5f - panelW * 0.5f, sh * 0.70f - panelH * 0.5f, panelW, panelH);

            Color frameColor = meter >= 0.5f
                ? Color.Lerp(ACCENT_GOLD, ACCENT_CYAN, (meter - 0.5f) * 2f)
                : Color.Lerp(ACCENT_GOLD, ACCENT_RED, (0.5f - meter) * 2f);

            // Background & Border
            DrawPanelFrame(panel, new Color(0.02f, 0.04f, 0.09f, 0.95f), new Color(0.04f, 0.08f, 0.16f, 0.98f), frameColor, 3f);
            DrawFrameCorners(panel, frameColor, 20f, 3f);

            float pad = Mathf.Clamp(12f * uiScale, 10f, 20f);

            // 1. Header Title by Minigame Type
            string minigameTitle;
            string minigamePrompt;
            switch (duel.CurrentMinigame)
            {
                case ClashMinigameType.PrecisionTiming:
                    minigameTitle = "BLADE LOCK CLASH // PRECISION STRIKE [一閃]";
                    minigamePrompt = "CLICK [LEFT MOUSE BUTTON] IN THE CRITICAL TARGET ZONE!";
                    break;
                case ClashMinigameType.RhythmBeat:
                    minigameTitle = "BLADE LOCK CLASH // RHYTHM COMBO [連続撃]";
                    minigamePrompt = "CLICK [LEFT MOUSE BUTTON] AS ORBS PASS OVER THE TARGET CIRCLE!";
                    break;
                case ClashMinigameType.TensionBalance:
                    minigameTitle = "BLADE LOCK CLASH // TENSION BALANCE [拮抗維持]";
                    minigamePrompt = "HOLD [LMB] TO THRUST UP // RELEASE [LMB] TO FALL";
                    break;
                case ClashMinigameType.OrbitalCrosshair:
                    minigameTitle = "BLADE LOCK CLASH // ORBITAL LOCK [旋風追尾]";
                    minigamePrompt = "CLICK [LMB] WHEN ORBITING SPARK OVERLAPS CIRCULAR RETICLE!";
                    break;
                case ClashMinigameType.ReflexTrigger:
                    minigameTitle = "BLADE LOCK CLASH // QUICK-DRAW REFLEX [瞬撃拔刀]";
                    if (duel.FalseStart)
                        minigamePrompt = "FALSE START! CLICKED PREMATURELY!";
                    else if (duel.ReflexSignalActive)
                        minigamePrompt = ">>> STRIKE NOW! CLICK [LEFT MOUSE BUTTON]! <<<";
                    else
                        minigamePrompt = "STAND BY... WAIT FOR THE STRIKE SIGNAL (DO NOT CLICK!)";
                    break;
                default:
                    minigameTitle = "BLADE LOCK CLASH // OVERPOWER MASH [猛連打]";
                    minigamePrompt = "RAPIDLY CLICK [LEFT MOUSE BUTTON] TO OVERPOWER!";
                    break;
            }

            float headerH = Mathf.Clamp(34f * uiScale, 28f, 44f);
            Rect headerRect = new Rect(panel.x + pad, panel.y + 6f, panel.width - pad * 2f, headerH);
            DrawFittedLabel(headerRect, minigameTitle, titleBarStyle, frameColor, 13);

            // 2. Interactive Minigame Gauge Area
            float barY = headerRect.yMax + 6f;
            float barH = isOrbital
                ? Mathf.Clamp(154f * uiScale, 136f, 190f)
                : Mathf.Clamp(54f * uiScale, 48f, 68f);
            Rect gaugeRect = new Rect(panel.x + pad, barY, panel.width - pad * 2f, barH);

            DrawRect(gaugeRect, new Color(0.05f, 0.05f, 0.08f, 0.9f));
            DrawPanelFrame(gaugeRect, new Color(0f, 0f, 0f, 0.8f), new Color(0.05f, 0.05f, 0.08f, 0.9f), new Color(0.4f, 0.5f, 0.6f, 0.6f), 1.5f);

            // Render specific minigame controls inside gaugeRect
            switch (duel.CurrentMinigame)
            {
                case ClashMinigameType.PrecisionTiming:
                    // Draw Sweet Spot Zone
                    float sweetX1 = gaugeRect.x + gaugeRect.width * duel.SweetSpotMin;
                    float sweetW = gaugeRect.width * (duel.SweetSpotMax - duel.SweetSpotMin);
                    Rect sweetRect = new Rect(sweetX1, gaugeRect.y + 2f, sweetW, gaugeRect.height - 4f);
                    DrawRect(sweetRect, new Color(0.2f, 0.95f, 0.45f, 0.45f));
                    DrawFrameCorners(sweetRect, ACCENT_GOLD, 6f, 2f);
                    DrawSidewaysLabel(sweetRect, "TARGET ZONE", bodyLabelStyle, ACCENT_GOLD);

                    // Oscillating Needle
                    float needleX = gaugeRect.x + gaugeRect.width * duel.NeedlePos;
                    Rect needleRect = new Rect(needleX - 3f, gaugeRect.y - 4f, 6f, gaugeRect.height + 8f);
                    DrawRect(needleRect, ACCENT_RED);
                    DrawFrameCorners(needleRect, Color.white, 4f, 1.5f);
                    break;

                case ClashMinigameType.RhythmBeat:
                    // Taiko-no-Tatsujin Conveyor Track Line
                    float trackY = gaugeRect.center.y;
                    DrawRect(new Rect(gaugeRect.x + 8f, trackY - 2f, gaugeRect.width - 16f, 4f), new Color(0.25f, 0.45f, 0.7f, 0.7f));

                    // Target Drum Circle on Left
                    float targetX = gaugeRect.x + gaugeRect.width * BladeLockDuelManager.RhythmTargetX;
                    float hitRadius = Mathf.Clamp(gaugeRect.height * 0.38f, 18f, 26f);
                    Rect targetZoneRect = new Rect(targetX - hitRadius, trackY - hitRadius, hitRadius * 2f, hitRadius * 2f);
                    DrawPanelFrame(targetZoneRect, new Color(0.1f, 0.35f, 0.7f, 0.5f), Color.clear, ACCENT_GOLD, 3f);
                    DrawFrameCorners(targetZoneRect, ACCENT_GOLD, 10f, 2f);
                    DrawFittedLabel(targetZoneRect, "HIT", sectionLabelStyle, ACCENT_GOLD, 11);

                    // Moving Notes
                    for (int b = 0; b < duel.ActiveNotes.Count; b++)
                    {
                        float noteX = gaugeRect.x + gaugeRect.width * duel.ActiveNotes[b];
                        if (noteX >= gaugeRect.x - 20f && noteX <= gaugeRect.xMax + 20f)
                        {
                            float noteR = hitRadius * 0.75f;
                            Rect noteRect = new Rect(noteX - noteR, trackY - noteR, noteR * 2f, noteR * 2f);
                            DrawRect(noteRect, new Color(1f, 0.40f, 0.08f, 0.95f));
                            DrawFrameCorners(noteRect, Color.white, 6f, 2f);
                            DrawFittedLabel(noteRect, "BEAT", bodyLabelStyle, Color.white, 8);
                        }
                    }
                    break;

                case ClashMinigameType.TensionBalance:
                    float bTargetX = gaugeRect.x + gaugeRect.width * duel.BalanceTargetPos;
                    float bZoneW = gaugeRect.width * 0.44f;
                    Rect bZoneRect = new Rect(bTargetX - bZoneW * 0.5f, gaugeRect.y + 3f, bZoneW, gaugeRect.height - 6f);
                    DrawRect(bZoneRect, new Color(0.2f, 0.9f, 0.5f, 0.4f));
                    DrawFrameCorners(bZoneRect, ACCENT_GOLD, 6f, 2f);
                    DrawSidewaysLabel(bZoneRect, "BALANCE ZONE", bodyLabelStyle, ACCENT_GOLD);

                    float pBobX = gaugeRect.x + gaugeRect.width * duel.BalanceBobberPos;
                    Rect pBobRect = new Rect(pBobX - 10f, gaugeRect.y - 2f, 20f, gaugeRect.height + 4f);
                    DrawRect(pBobRect, ACCENT_CYAN);
                    DrawFrameCorners(pBobRect, Color.white, 4f, 1.5f);
                    break;

                case ClashMinigameType.OrbitalCrosshair:
                    Vector2 dialCenter = gaugeRect.center;
                    float dialRadius = gaugeRect.height * 0.40f;

                    // Circular Radar Track (crosshairs + radar frame)
                    DrawRect(new Rect(dialCenter.x - dialRadius - 14f, dialCenter.y - 1f, (dialRadius + 14f) * 2f, 2f), new Color(0.2f, 0.5f, 0.8f, 0.45f));
                    DrawRect(new Rect(dialCenter.x - 1f, dialCenter.y - dialRadius - 14f, 2f, (dialRadius + 14f) * 2f), new Color(0.2f, 0.5f, 0.8f, 0.45f));
                    
                    // Outer radar ring frame
                    Rect dialBounds = new Rect(dialCenter.x - dialRadius, dialCenter.y - dialRadius, dialRadius * 2f, dialRadius * 2f);
                    DrawPanelFrame(dialBounds, new Color(0.04f, 0.08f, 0.16f, 0.75f), Color.clear, new Color(0.3f, 0.65f, 0.95f, 0.7f), 2f);
                    DrawFrameCorners(dialBounds, ACCENT_CYAN, 16f, 2f);

                    // Inner decorative radar ring
                    float innerRadius = dialRadius * 0.55f;
                    Rect innerBounds = new Rect(dialCenter.x - innerRadius, dialCenter.y - innerRadius, innerRadius * 2f, innerRadius * 2f);
                    DrawPanelFrame(innerBounds, Color.clear, Color.clear, new Color(0.25f, 0.5f, 0.75f, 0.35f), 1f);

                    // Circular Lock Target Zone on the perimeter
                    float tRad = duel.TargetLockAngle * Mathf.Deg2Rad;
                    Vector2 tPos = dialCenter + new Vector2(Mathf.Cos(tRad), Mathf.Sin(tRad)) * dialRadius;
                    float lockSz = Mathf.Clamp(48f * uiScale, 42f, 56f);
                    Rect lockCircleRect = new Rect(tPos.x - lockSz * 0.5f, tPos.y - lockSz * 0.5f, lockSz, lockSz);
                    DrawPanelFrame(lockCircleRect, new Color(0.9f, 0.4f, 0.1f, 0.65f), new Color(0.18f, 0.09f, 0.02f, 0.9f), ACCENT_GOLD, 3f);
                    DrawFrameCorners(lockCircleRect, ACCENT_GOLD, 12f, 2f);
                    DrawFittedLabel(lockCircleRect, "LOCK", titleBarStyle, ACCENT_GOLD, 15);

                    // Orbiting Spark along the perimeter
                    float oRad = duel.OrbitAngle * Mathf.Deg2Rad;
                    Vector2 oPos = dialCenter + new Vector2(Mathf.Cos(oRad), Mathf.Sin(oRad)) * dialRadius;
                    float orbSz = Mathf.Clamp(24f * uiScale, 20f, 30f);
                    Rect orbCircleRect = new Rect(oPos.x - orbSz * 0.5f, oPos.y - orbSz * 0.5f, orbSz, orbSz);
                    DrawRect(orbCircleRect, ACCENT_CYAN);
                    DrawFrameCorners(orbCircleRect, Color.white, 6f, 2f);

                    // Alignment proximity glow
                    float angleDiff = Mathf.Abs(Mathf.DeltaAngle(duel.OrbitAngle, duel.TargetLockAngle));
                    if (angleDiff <= 32f)
                    {
                        DrawPanelFrame(lockCircleRect, new Color(1f, 0.8f, 0.2f, 0.4f), Color.clear, Color.white, 3f);
                    }
                    break;

                case ClashMinigameType.ReflexTrigger:
                    if (duel.ReflexSignalActive && !duel.FalseStart)
                    {
                        DrawRect(gaugeRect, new Color(1f, 0.85f, 0.1f, 0.85f));
                        DrawFittedLabel(gaugeRect, "[ STRIKE NOW! CLICK LEFT MOUSE BUTTON! ]", titleBarStyle, Color.black, 14);
                    }
                    else if (duel.FalseStart)
                    {
                        DrawRect(gaugeRect, new Color(0.8f, 0.1f, 0.1f, 0.85f));
                        DrawFittedLabel(gaugeRect, "FALSE START // RECOIL PENALTY", titleBarStyle, Color.white, 12);
                    }
                    else
                    {
                        float standbyPulse = 0.4f + 0.3f * Mathf.Sin(Time.unscaledTime * 12f);
                        DrawRect(gaugeRect, new Color(0.8f, 0.2f, 0.1f, standbyPulse));
                        DrawFittedLabel(gaugeRect, "STAND BY... DO NOT CLICK", sectionLabelStyle, ACCENT_GOLD, 12);
                    }
                    break;

                default:
                    // Rapid Mash (Tug of war fill)
                    float playerFillW = gaugeRect.width * meter;
                    if (playerFillW > 1f)
                    {
                        Rect playerFillRect = new Rect(gaugeRect.x, gaugeRect.y, playerFillW, gaugeRect.height);
                        Color pCol = Color.Lerp(new Color(0.1f, 0.6f, 1f, 0.85f), new Color(0.2f, 1f, 0.6f, 0.95f), meter);
                        DrawRect(playerFillRect, pCol);
                    }

                    float enemyFillW = gaugeRect.width * (1f - meter);
                    if (enemyFillW > 1f)
                    {
                        Rect enemyFillRect = new Rect(gaugeRect.x + playerFillW, gaugeRect.y, enemyFillW, gaugeRect.height);
                        Color eCol = Color.Lerp(new Color(0.9f, 0.2f, 0.2f, 0.85f), new Color(0.7f, 0.1f, 0.8f, 0.95f), 1f - meter);
                        DrawRect(enemyFillRect, eCol);
                    }

                    float reticleX = gaugeRect.x + gaugeRect.width * meter;
                    float reticleSize = Mathf.Clamp(36f * uiScale, 28f, 48f);
                    Rect reticleRect = new Rect(reticleX - reticleSize * 0.5f, gaugeRect.center.y - reticleSize * 0.5f, reticleSize, reticleSize);
                    DrawRect(reticleRect, new Color(0.05f, 0.05f, 0.08f, 0.85f));
                    DrawFrameCorners(reticleRect, frameColor, 8f, 2f);
                    GUIStyle diamondStyle = new GUIStyle(titleBarStyle)
                    {
                        fontSize = Mathf.RoundToInt(18f * uiScale),
                        alignment = TextAnchor.MiddleCenter
                    };
                    GUI.Label(reticleRect, "◆", diamondStyle);
                    break;
            }

            // 3. Mini Overall Dominance Bar below minigame gauge
            float domY = gaugeRect.yMax + 4f;
            float domH = 6f;
            Rect domRect = new Rect(panel.x + pad, domY, panel.width - pad * 2f, domH);
            DrawRect(domRect, new Color(0.1f, 0.1f, 0.15f, 0.9f));
            DrawRect(new Rect(domRect.x, domRect.y, domRect.width * meter, domH), frameColor);

            // 4. Action Prompt (Pulsing callout)
            float promptY = domRect.yMax + 6f;
            float promptH = Mathf.Clamp(32f * uiScale, 26f, 40f);
            Rect promptRect = new Rect(panel.x + pad, promptY, panel.width - pad * 2f, promptH);

            float pulse = 0.85f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 10f)) * 0.35f;
            Color promptColor = new Color(frameColor.r, frameColor.g, frameColor.b, pulse);
            DrawFittedLabel(promptRect, minigamePrompt, sectionLabelStyle, promptColor, 12);

            // 5. Timer Depletion Bar
            float timerH = Mathf.Clamp(5f * uiScale, 4f, 7f);
            Rect timerRect = new Rect(panel.x + pad, panel.yMax - timerH - 6f, (panel.width - pad * 2f) * timeRatio, timerH);
            DrawRect(timerRect, frameColor);
        }

        private void DrawRunDepthOverlay()
        {
            RuntimeRunBuilder.RunProgression progression = runContext.Progression;
            if (progression == null)
                return;

            int sw = Mathf.RoundToInt(UiWidth);
            int sh = Mathf.RoundToInt(UiHeight);
            string label =
                $"LEVEL {progression.CurrentLevelOneBased}/{progression.TotalLevels}   " +
                $"ARENA {progression.CurrentArenaOneBased}/{progression.ArenasPerLevel}\n" +
                $"RUN {FormatRunTime(runElapsedSeconds)}   " +
                $"ARENA {FormatRunTime(arenaElapsedSeconds)}";

            GUIStyle infoStyle = new GUIStyle(bodyLabelStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(sh * 0.021f, 18f, 46f))
            };

            float padX = Mathf.Clamp(sw * 0.008f, 12f, 30f);
            float padY = Mathf.Clamp(sh * 0.008f, 8f, 18f);
            Vector2 textSize = infoStyle.CalcSize(new GUIContent(label));

            float badgeW = Mathf.Clamp(textSize.x + padX * 2f, 360f, sw * 0.58f);
            float badgeH = Mathf.Clamp(
                textSize.y + padY * 2f,
                64f,
                138f);
            Rect badge = new Rect(
                Mathf.Clamp(sw * 0.008f, 14f, 28f),
                Mathf.Clamp(sh * 0.01f, 12f, 26f),
                badgeW,
                badgeH);

            DrawPanelFrame(badge, new Color(0.02f, 0.06f, 0.12f, 0.90f), new Color(0.03f, 0.09f, 0.16f, 0.93f), ACCENT_CYAN, 2f);
            DrawFrameCorners(badge, new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.60f), badge.height * 0.28f, 1.5f);

            GUI.Label(
                new Rect(badge.x + padX, badge.y + padY * 0.5f, badge.width - padX * 2f, badge.height - padY),
                label,
                infoStyle);
        }

        public struct ComicPopup
        {
            public string text;
            public Color color;
            public float startTime;
            public float duration;
            public Vector2 screenPos;
            public float scale;
        }
        private static readonly List<ComicPopup> activeComicPopups = new List<ComicPopup>();

        public static void SpawnComicPopup(string text, Color color, float scale = 1f)
        {
            activeComicPopups.Add(new ComicPopup
            {
                text = text,
                color = color,
                startTime = Time.unscaledTime,
                duration = 1.15f * scale,
                screenPos = new Vector2(UiWidth * 0.5f + UnityEngine.Random.Range(-120f, 120f), UiHeight * 0.38f + UnityEngine.Random.Range(-40f, 40f)),
                scale = scale
            });
        }

        public static void SpawnGlobalComicPopup(string text, Color color, float scale = 1f) => SpawnComicPopup(text, color, scale);

        private void DrawComicPopups()
        {
            if (activeComicPopups.Count == 0) return;

            float now = Time.unscaledTime;
            for (int i = activeComicPopups.Count - 1; i >= 0; i--)
            {
                ComicPopup popup = activeComicPopups[i];
                float elapsed = now - popup.startTime;
                if (elapsed >= popup.duration)
                {
                    activeComicPopups.RemoveAt(i);
                    continue;
                }

                float norm = elapsed / popup.duration;
                float alpha = 1f - Mathf.Pow(norm, 2.5f);
                float bounce = Mathf.Sin(norm * Mathf.PI * 0.5f) * 45f;
                float popScale = (1f + Mathf.Sin(norm * Mathf.PI) * 0.35f) * popup.scale;

                float uiScale = GetUiScale();
                int fontSize = Mathf.RoundToInt(Mathf.Clamp(17f * uiScale * popScale, 12f, 32f));
                GUIStyle style = new GUIStyle(titleBarStyle)
                {
                    fontSize = fontSize,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    wordWrap = false,
                    clipping = TextClipping.Overflow
                };

                Vector2 textSize = style.CalcSize(new GUIContent(popup.text));
                float boxW = textSize.x + 18f * uiScale;
                float boxH = textSize.y + 8f * uiScale;
                Vector2 pos = popup.screenPos - new Vector2(0f, bounce);
                Rect r = new Rect(pos.x - boxW * 0.5f, pos.y - boxH * 0.5f, boxW, boxH);

                // Backdrop dark cyber card with vibrant border
                Color bgCard = new Color(0.02f, 0.04f, 0.08f, alpha * 0.90f);
                Color borderCard = new Color(popup.color.r, popup.color.g, popup.color.b, alpha * 0.95f);
                DrawPanelFrame(r, bgCard, bgCard, borderCard, 2f);
                DrawFrameCorners(r, borderCard, 10f, 1.5f);

                // Shadow
                style.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.95f);
                GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), popup.text, style);

                // Main text
                Color c = popup.color;
                c.a *= alpha;
                style.normal.textColor = c;
                GUI.Label(r, popup.text, style);
            }
        }

        private void DrawStartCountdownOverlay(MatchManager match)
        {
            int sw = Mathf.RoundToInt(UiWidth);
            int sh = Mathf.RoundToInt(UiHeight);

            float remaining = match.CountdownRemaining;
            float total = Mathf.Max(0.1f, match.CountdownDuration);
            float elapsed = total - remaining;
            float showcaseDuration = Mathf.Max(0.1f, total - 3.0f);

            // Reset minigame parameters to 0 at the start of every match countdown
            if (lastCountdownMatch != match || elapsed <= 0.05f)
            {
                ripBobberPos = 0f;
                ripBobberVel = 0f;
                ripTargetPos = 0.5f;
                ripTensionCharge = 0f;
                ripTargetTimer = 0f;
                lastCountdownMatch = match;
            }

            // Stardew Valley-Style Buoyant Physics Simulation for the Launch Bobber
            if (!match.HasPlayerRipped)
            {
                bool isHolding = false;
                if (Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed)) isHolding = true;
                if (Keyboard.current != null && (Keyboard.current.spaceKey.isPressed || Keyboard.current.enterKey.isPressed)) isHolding = true;
                if (Gamepad.current != null && (Gamepad.current.buttonSouth.isPressed || Gamepad.current.buttonWest.isPressed || Gamepad.current.rightTrigger.isPressed || Gamepad.current.leftTrigger.isPressed || Gamepad.current.rightShoulder.isPressed)) isHolding = true;

                // Physics update: smooth buoyant acceleration and gentle gravity
                float dt = Time.unscaledDeltaTime;
                float targetVel = isHolding ? 0.45f : -0.38f;
                float accelRate = isHolding ? 1.6f : 1.3f;
                ripBobberVel = Mathf.MoveTowards(ripBobberVel, targetVel, dt * accelRate);
                ripBobberVel = Mathf.Clamp(ripBobberVel, -0.45f, 0.45f);
                ripBobberPos = Mathf.Clamp01(ripBobberPos + ripBobberVel * dt);
                if (ripBobberPos <= 0f || ripBobberPos >= 1f) ripBobberVel *= -0.15f;

                // Smooth organic sweet-spot wander
                ripTargetTimer += dt * 1.2f;
                ripTargetPos = 0.50f + 0.33f * Mathf.Sin(ripTargetTimer) + 0.08f * Mathf.Sin(ripTargetTimer * 2.5f);

                // Overlap calculation: sweet-spot zone is 0.28 wide
                float sweetZoneHalfH = 0.14f;
                bool insideSweetSpot = Mathf.Abs(ripBobberPos - ripTargetPos) <= sweetZoneHalfH;

                if (insideSweetSpot)
                    ripTensionCharge = Mathf.Clamp01(ripTensionCharge + 0.20f * dt);
                else
                    ripTensionCharge = Mathf.Clamp01(ripTensionCharge - 0.12f * dt);

                // If countdown reaches 0, trigger launch with current charged tension
                if (remaining <= 0.08f)
                {
                    match.ExecuteRipCord(ripTensionCharge);
                }
            }

            // ── TOP ANIME COUNTDOWN & SHOWCASE BANNER ───────────────────────────────
            string mainText;
            string subText;
            Color textColor;
            Color borderColor;

            if (elapsed < showcaseDuration)
            {
                float shotDuration = showcaseDuration / 4f;
                int shotIndex = Mathf.Clamp(Mathf.FloorToInt(elapsed / shotDuration), 0, 3);
                switch (shotIndex)
                {
                    case 0:
                        mainText = "INSPECTION 01";
                        subText = "CHASSIS & FUSION WHEEL // PRE-FLIGHT SCAN";
                        textColor = ACCENT_CYAN;
                        borderColor = ACCENT_CYAN;
                        break;
                    case 1:
                        mainText = "INSPECTION 02";
                        subText = "FACE BOLT & ENERGY CORE // RESONANCE SYNC";
                        textColor = ACCENT_MAGENTA;
                        borderColor = ACCENT_MAGENTA;
                        break;
                    case 2:
                        mainText = "INSPECTION 03";
                        subText = "LOCK SYSTEM // TENSION CALIBRATION";
                        textColor = ACCENT_GOLD;
                        borderColor = ACCENT_GOLD;
                        break;
                    default:
                        mainText = "INSPECTION 04";
                        subText = "ARENA SECTOR SCAN // ENGAGE LAUNCHER";
                        textColor = ACCENT_ORANGE;
                        borderColor = ACCENT_ORANGE;
                        break;
                }
            }
            else if (remaining > 2.0f)
            {
                mainText = "3";
                subText = "READY... THREE!";
                textColor = new Color(1f, 0.9f, 0.2f, 1f);
                borderColor = ACCENT_YEL;
            }
            else if (remaining > 1.0f)
            {
                mainText = "2";
                subText = "SET... TWO!";
                textColor = ACCENT_CYAN;
                borderColor = ACCENT_CYAN;
            }
            else if (remaining > 0.08f)
            {
                mainText = "1";
                subText = "撃ち込め！ LET IT RIP!!!";
                textColor = new Color(1f, 0.4f, 0.1f, 1f);
                borderColor = new Color(1f, 0.5f, 0.1f, 1f);
            }
            else
            {
                mainText = "CLASH!!";
                subText = "BURST DRIVE ENGAGED";
                textColor = Color.white;
                borderColor = ACCENT_MAGENTA;
            }

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 14f) * 0.03f;
            float topW = Mathf.Clamp(sw * 0.38f, 360f, 580f) * pulse;
            float topH = Mathf.Clamp(sh * 0.14f, 100f, 170f) * pulse;
            Rect topPanel = new Rect((sw - topW) * 0.5f, sh * 0.05f, topW, topH);

            DrawPanelFrame(topPanel, new Color(0.02f, 0.04f, 0.08f, 0.95f), new Color(0.04f, 0.07f, 0.14f, 0.98f), borderColor, 3.5f);
            DrawFrameCorners(topPanel, borderColor, topPanel.width * 0.16f, 2f);
            DrawMotionBandClipped(new Rect(topPanel.x + topPanel.width * 0.5f, topPanel.y, topPanel.width * 0.45f, topPanel.height), borderColor, 8f, 14f, 0.12f);

            GUIStyle countdownStyle = CreateStaticStyle(titleBarStyle, textColor, Mathf.RoundToInt(Mathf.Clamp(UiHeight * 0.065f, 36f, 84f)), TextAnchor.MiddleCenter, FontStyle.Bold);
            GUIStyle countdownSubStyle = CreateStaticStyle(bodyLabelStyle, borderColor, Mathf.RoundToInt(Mathf.Clamp(UiHeight * 0.019f, 12f, 22f)), TextAnchor.MiddleCenter, FontStyle.Bold);

            GUILayout.BeginArea(topPanel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(mainText, countdownStyle);
            GUILayout.Label(subText, countdownSubStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();

            // ── STARDEW VALLEY-STYLE LAUNCH TENSION GAUGE ───────────────────────────
            float meterW = Mathf.Clamp(sw * 0.38f, 380f, 620f);
            float meterH = Mathf.Clamp(sh * 0.32f, 250f, 370f);
            Rect meterPanel = new Rect((sw - meterW) * 0.5f, sh * 0.58f, meterW, meterH);

            Color meterBorder = match.HasPlayerRipped
                ? (match.RipRating == BladeSpinners.Abilities.LaunchRating.Perfect ? ACCENT_YEL : (match.RipRating == BladeSpinners.Abilities.LaunchRating.Great ? ACCENT_CYAN : ACCENT_ORANGE))
                : (ripTensionCharge >= 0.85f ? ACCENT_GOLD : ACCENT_CYAN);

            DrawPanelFrame(meterPanel, new Color(0.02f, 0.05f, 0.10f, 0.95f), new Color(0.04f, 0.08f, 0.16f, 0.98f), meterBorder, 3f);
            DrawFrameCorners(meterPanel, meterBorder, 28f, 2f);

            // Title Header with generous padding
            float headerH = Mathf.Clamp(sh * 0.030f, 24f, 34f);
            Rect titleRect = new Rect(meterPanel.x + 14f, meterPanel.y + 12f, meterPanel.width - 28f, headerH);
            DrawFittedLabel(titleRect, "RIP LAUNCHER // POWER TENSION", sectionLabelStyle, ACCENT_CYAN, 12);

            // Inner Track Areas
            float innerPad = 16f;
            float trackAreaX = meterPanel.x + innerPad + 10f;
            float trackAreaY = titleRect.yMax + 8f;
            float trackAreaH = meterPanel.yMax - trackAreaY - 62f;

            // 1. Left Vertical Track: Bobber & Moving Sweet-Spot Target Zone
            float bobberTrackW = meterPanel.width * 0.48f;
            Rect bobberTrackRect = new Rect(trackAreaX, trackAreaY, bobberTrackW, trackAreaH);
            DrawPanelFrame(bobberTrackRect, new Color(0.04f, 0.06f, 0.11f, 0.95f), new Color(0.02f, 0.04f, 0.08f, 0.98f), new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.4f), 1.5f);

            // Sweet Spot Zone in Vertical Track (0 at bottom, 1 at top)
            float sweetH = trackAreaH * 0.28f;
            float sweetCenterY = trackAreaY + trackAreaH * (1f - ripTargetPos);
            Rect sweetZoneRect = new Rect(bobberTrackRect.x + 3f, sweetCenterY - sweetH * 0.5f, bobberTrackW - 6f, sweetH);
            sweetZoneRect.y = Mathf.Clamp(sweetZoneRect.y, trackAreaY + 2f, trackAreaY + trackAreaH - sweetH - 2f);

            float sweetPulse = 0.70f + 0.30f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f));
            Color sweetColor = new Color(0.20f, 0.95f, 0.45f, 0.40f * sweetPulse);
            DrawRect(sweetZoneRect, sweetColor);
            DrawFrameCorners(sweetZoneRect, new Color(0.35f, 1f, 0.55f, 0.95f), 10f, 2f);
            DrawFittedLabel(sweetZoneRect, "SWEET SPOT ZONE", bodyLabelStyle, new Color(0.85f, 1f, 0.90f, 0.9f), 10);

            // Player Bobber Handle (Ascends with Left-Click, falls with gravity)
            float bobberH = 26f;
            float bobberY = trackAreaY + trackAreaH * (1f - ripBobberPos) - bobberH * 0.5f;
            bobberY = Mathf.Clamp(bobberY, trackAreaY + 2f, trackAreaY + trackAreaH - bobberH - 2f);
            Rect bobberRect = new Rect(bobberTrackRect.x + 6f, bobberY, bobberTrackW - 12f, bobberH);

            bool isInsideSweet = Mathf.Abs(ripBobberPos - ripTargetPos) <= 0.14f;
            Color bobberColor = isInsideSweet ? ACCENT_GOLD : new Color(0.40f, 0.85f, 1f, 1f);
            DrawPanelFrame(bobberRect, new Color(bobberColor.r * 0.3f, bobberColor.g * 0.3f, bobberColor.b * 0.3f, 0.9f), new Color(0.08f, 0.08f, 0.12f, 0.98f), bobberColor, 2f);
            DrawFrameCorners(bobberRect, bobberColor, 8f, 1.5f);
            DrawFittedLabel(bobberRect, "▲ LAUNCH BOBBER ▲", sectionLabelStyle, bobberColor, 10);

            // 2. Right Vertical Bar: Tension Power Charge Meter (0%..100%)
            float chargeBarX = bobberTrackRect.xMax + 18f;
            float chargeBarW = meterPanel.width - chargeBarX + meterPanel.x - innerPad - 10f;
            Rect chargeTrackRect = new Rect(chargeBarX, trackAreaY, chargeBarW, trackAreaH);
            DrawPanelFrame(chargeTrackRect, new Color(0.04f, 0.06f, 0.11f, 0.95f), new Color(0.02f, 0.04f, 0.08f, 0.98f), new Color(ACCENT_GOLD.r, ACCENT_GOLD.g, ACCENT_GOLD.b, 0.5f), 1.5f);

            // Filled Tension Level
            float filledH = trackAreaH * ripTensionCharge;
            Rect filledRect = new Rect(chargeTrackRect.x + 3f, chargeTrackRect.yMax - filledH, chargeBarW - 6f, filledH);
            Color fillGrad = Color.Lerp(ACCENT_ORANGE, ACCENT_GOLD, ripTensionCharge);
            DrawRect(filledRect, fillGrad);

            // Tension Percent Text
            GUIStyle chargeStyle = CreateStaticStyle(titleBarStyle, Color.white, Mathf.RoundToInt(Mathf.Clamp(sh * 0.022f, 14f, 24f)), TextAnchor.MiddleCenter, FontStyle.Bold);
            GUI.Label(chargeTrackRect, $"{Mathf.RoundToInt(ripTensionCharge * 100f)}%\nPOWER", chargeStyle);

            // Bottom Instructions & Result Prompt
            GUIStyle promptStyle = new GUIStyle(bodyLabelStyle)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(sh * 0.016f, 11f, 18f)),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            Rect promptRect = new Rect(meterPanel.x + 12f, meterPanel.yMax - 54f, meterPanel.width - 24f, 46f);

            if (!match.HasPlayerRipped)
            {
                promptStyle.normal.textColor = isInsideSweet ? ACCENT_GOLD : ACCENT_CYAN;
                string hint = isInsideSweet
                    ? ">> PERFECT TENSION LOCK! CHARGING FAST! <<"
                    : "HOLD [LEFT CLICK / SPACE / A / X] TO RISE ▲\nRELEASE TO FALL ▼";
                GUI.Label(promptRect, hint, promptStyle);
            }
            else
            {
                string ratingStr = match.RipRating switch
                {
                    BladeSpinners.Abilities.LaunchRating.Perfect => "★ SUPER PERFECT RIP! (+140% SPIN +50 PTS) ★",
                    BladeSpinners.Abilities.LaunchRating.Great => "GREAT RIP! (+118% SPIN POWER)",
                    BladeSpinners.Abilities.LaunchRating.Good => "GOOD RIP (+95% SPIN)",
                    _ => "MISHAP RIP (+75% SPIN)"
                };
                Color ratingCol = match.RipRating switch
                {
                    BladeSpinners.Abilities.LaunchRating.Perfect => ACCENT_GOLD,
                    BladeSpinners.Abilities.LaunchRating.Great => ACCENT_CYAN,
                    _ => ACCENT_ORANGE
                };
                promptStyle.normal.textColor = ratingCol;
                GUI.Label(promptRect, ratingStr, promptStyle);
            }
        }

        private void DrawInGameCombatHud()
        {
            PlayerManager player = runContext.Player;
            if (player == null || player.BeyConfiguration == null)
                return;

            BeyConfiguration config = player.BeyConfiguration;
            int sw = Mathf.RoundToInt(UiWidth);
            int sh = Mathf.RoundToInt(UiHeight);

            // ── LEAGUE OF LEGENDS / MOBA STYLE ABILITY HUD (BOTTOM-RIGHT) ──────────
            BladeSpinners.Abilities.BeyAbility ability = config.GetStatBlock().EquippedAbility;
            BeyPart faceBolt = config.GetEquippedPart(PartType.FaceBolt);
            if (faceBolt == null && runContext.Player != null)
            {
                BeyAssembler assembler = runContext.Player.GetComponent<BeyAssembler>();
                if (assembler != null)
                    faceBolt = assembler.GetEquippedPart(PartType.FaceBolt);
            }
            if (faceBolt == null && runContext.Player != null)
            {
                var loadout = GetCurrentRunLoadout(runContext.Player);
                if (loadout != null && loadout.TryGetValue(PartType.FaceBolt, out BeyPart fb))
                    faceBolt = fb;
            }
            if (faceBolt == null && selectedMainMenuLoadout != null && selectedMainMenuLoadout.TryGetValue(PartType.FaceBolt, out BeyPart mainFb))
            {
                faceBolt = mainFb;
            }
            if (faceBolt == null && ability != null)
            {
                faceBolt = FindFaceBoltForAbility(ability);
            }

            float size = Mathf.Clamp(sh * 0.12f, 85f, 130f);
            float marginX = 36f;
            float marginY = 36f;
            Rect hudRect = new Rect(sw - size - marginX, sh - size - marginY, size, size);

            // Outer Tech Chassis Frame
            DrawPanelFrame(new Rect(hudRect.x - 6f, hudRect.y - 6f, hudRect.width + 12f, hudRect.height + 12f),
                new Color(0.01f, 0.03f, 0.07f, 0.92f),
                new Color(0.03f, 0.07f, 0.14f, 0.96f),
                new Color(0.15f, 0.35f, 0.55f, 0.6f), 2f);

            float effectiveCost = ability != null ? config.GetEffectiveAbilityCost(ability) : 0f;
            bool hasMana = config.CurrentMana >= effectiveCost;
            bool isOnCooldown = !config.IsAbilityReady;
            bool isReady = config.IsAbilityReady && hasMana;

            // Face Bolt Emblem Icon / Visual (Dynamic equipped part icon)
            Rect iconRect = new Rect(hudRect.x, hudRect.y, hudRect.width, hudRect.height);
            DrawRect(iconRect, new Color(0.02f, 0.05f, 0.10f, 1f));

            float pad = 6f;
            Rect innerRect = new Rect(iconRect.x + pad, iconRect.y + pad, iconRect.width - pad * 2f, iconRect.height - pad * 2f);

            Sprite emblem = null;
            if (faceBolt != null)
            {
                emblem = faceBolt.FaceBoltEmblem != null ? faceBolt.FaceBoltEmblem : faceBolt.Icon;
            }
            if (emblem == null && ability != null)
            {
                emblem = ability.Icon;
            }
            if (emblem == null && ability != null)
            {
                BeyPart matchedBolt = FindFaceBoltForAbility(ability);
                if (matchedBolt != null)
                {
                    emblem = matchedBolt.FaceBoltEmblem != null ? matchedBolt.FaceBoltEmblem : matchedBolt.Icon;
                }
            }

            if (emblem != null && emblem.texture != null)
            {
                DrawSprite(innerRect, emblem);
            }
            else if (faceBolt != null)
            {
                DrawPartSprite(innerRect, faceBolt);
            }
            else
            {
                // Stylized Fallback: Glowing Cyber Ability Hexagon / Glyph with FaceBolt initials
                DrawRect(innerRect, new Color(0.04f, 0.10f, 0.20f, 0.85f));
                DrawFrameCorners(innerRect, ACCENT_CYAN, 12f, 2f);
                string glyph = ability != null ? (ability.AbilityName.Length > 2 ? ability.AbilityName.Substring(0, 2).ToUpperInvariant() : "AB") : "FB";
                GUIStyle glyphStyle = CreateStaticStyle(titleBarStyle, ACCENT_CYAN, Mathf.RoundToInt(size * 0.36f), TextAnchor.MiddleCenter, FontStyle.Bold);
                GUI.Label(innerRect, glyph, glyphStyle);
            }

            // Cooldown Wipe Overlay
            if (isOnCooldown)
            {
                float cdRemaining = config.AbilityCooldownRemaining;
                float cdTotal = Mathf.Max(0.1f, ability != null ? ability.CooldownDuration : 5f);
                float cdNorm = Mathf.Clamp01(cdRemaining / cdTotal);

                // Dark semi-transparent wipe
                Rect cdWipeRect = new Rect(iconRect.x, iconRect.y + iconRect.height * (1f - cdNorm), iconRect.width, iconRect.height * cdNorm);
                DrawRect(new Rect(iconRect.x, iconRect.y, iconRect.width, iconRect.height), new Color(0f, 0f, 0f, 0.70f));
                DrawRect(cdWipeRect, new Color(0.05f, 0.15f, 0.30f, 0.40f));

                // Bold Centered Cooldown Number
                GUIStyle cdNumStyle = new GUIStyle(titleBarStyle)
                {
                    fontSize = Mathf.RoundToInt(size * 0.38f),
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                cdNumStyle.normal.textColor = new Color(1f, 1f, 1f, 0.95f);
                GUI.Label(new Rect(iconRect.x + 2f, iconRect.y + 2f, iconRect.width, iconRect.height), $"{cdRemaining:0.0}", new GUIStyle(cdNumStyle) { normal = { textColor = Color.black } });
                GUI.Label(iconRect, $"{cdRemaining:0.0}", cdNumStyle);
            }
            else if (!hasMana)
            {
                DrawRect(iconRect, new Color(0.1f, 0.25f, 0.6f, 0.45f));
            }
            else
            {
                float pulse = 0.7f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 0.3f;
                Color readyBorder = new Color(ACCENT_GOLD.r, ACCENT_GOLD.g, ACCENT_GOLD.b, pulse);
                DrawFrameCorners(iconRect, readyBorder, 18f, 3f);
            }

            // Outer Border Frame
            Color frameBorder = isReady ? ACCENT_GOLD : (isOnCooldown ? new Color(0.3f, 0.3f, 0.4f, 0.6f) : ACCENT_CYAN);
            DrawBorderOnly(iconRect, frameBorder, 2f);

            // Top-Right Mana Cost Badge
            if (effectiveCost > 0f)
            {
                float manaTagW = size * 0.42f;
                float manaTagH = size * 0.24f;
                Rect manaTagRect = new Rect(iconRect.xMax - manaTagW + 4f, iconRect.y - 6f, manaTagW, manaTagH);
                DrawPanelFrame(manaTagRect, new Color(0.04f, 0.14f, 0.30f, 0.95f), new Color(0.02f, 0.08f, 0.18f, 0.95f), ACCENT_CYAN, 1.5f);

                GUIStyle manaCostStyle = new GUIStyle(sectionLabelStyle)
                {
                    fontSize = Mathf.RoundToInt(size * 0.16f),
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                manaCostStyle.normal.textColor = ACCENT_CYAN;
                GUI.Label(manaTagRect, $"{Mathf.RoundToInt(effectiveCost)}", manaCostStyle);
            }

            // Bottom Keybind Badge Pill: [E]
            float keyW = size * 0.36f;
            float keyH = size * 0.24f;
            Rect keyRect = new Rect(iconRect.center.x - keyW * 0.5f, iconRect.yMax - keyH * 0.5f, keyW, keyH);
            DrawPanelFrame(keyRect, new Color(0.08f, 0.08f, 0.12f, 0.98f), new Color(0.12f, 0.12f, 0.18f, 0.98f), isReady ? ACCENT_GOLD : Color.white, 1.5f);

            GUIStyle keyStyle = new GUIStyle(sectionLabelStyle)
            {
                fontSize = Mathf.RoundToInt(size * 0.17f),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            keyStyle.normal.textColor = isReady ? ACCENT_GOLD : Color.white;
            GUI.Label(keyRect, "E", keyStyle);

            // Top Ability Name Banner
            string abilityName = ability != null ? ability.AbilityName.ToUpperInvariant() : "NO ABILITY";
            float nameW = size * 1.8f;
            float nameH = 22f;
            Rect nameRect = new Rect(iconRect.xMax - nameW, iconRect.y - nameH - 10f, nameW, nameH);
            GUIStyle nameStyle = new GUIStyle(sectionLabelStyle)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(size * 0.14f, 11f, 16f)),
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold
            };
            nameStyle.normal.textColor = isReady ? ACCENT_GOLD : (isOnCooldown ? Color.gray : ACCENT_CYAN);
            GUI.Label(new Rect(nameRect.x + 1f, nameRect.y + 1f, nameRect.width, nameRect.height), abilityName, new GUIStyle(nameStyle) { normal = { textColor = Color.black } });
            GUI.Label(nameRect, abilityName, nameStyle);
        }

        private void DrawDramaticDefeatOverlay(MatchManager match)
        {
            int sw = Mathf.RoundToInt(UiWidth);
            int sh = Mathf.RoundToInt(UiHeight);

            float totalDefeatTime = 3.2f;
            float elapsed = totalDefeatTime - match.StateTimer;
            float alpha = Mathf.Clamp01(elapsed * 2.5f);

            // Fullscreen dark vignette + grayscale backdrop tint
            DrawRect(new Rect(0, 0, sw, sh), new Color(0.04f, 0.01f, 0.02f, 0.70f * alpha));

            // Dramatic letterbox cinematic bars
            float barH = Mathf.Clamp(sh * 0.12f, 70f, 130f);
            DrawRect(new Rect(0, 0, sw, barH), new Color(0f, 0f, 0f, 0.95f * alpha));
            DrawRect(new Rect(0, sh - barH, sw, barH), new Color(0f, 0f, 0f, 0.95f * alpha));

            string mainDefeatTitle;
            string subDefeatTitle;
            Color themeColor;

            switch (match.LastPlayerDefeatReason)
            {
                case MatchManager.PlayerDefeatReason.BurstedByEnemy:
                    mainDefeatTitle = "BURST FINISH";
                    subDefeatTitle = "PARTS DISASSEMBLED // CRITICAL SPIN BREACH";
                    themeColor = RED_DANGER;
                    break;
                case MatchManager.PlayerDefeatReason.KnockedOutByEnemy:
                case MatchManager.PlayerDefeatReason.JumpedOut:
                    mainDefeatTitle = "OVER FINISH";
                    subDefeatTitle = "EJECTED FROM STADIUM // ARENA BOUNDARY EXIT";
                    themeColor = ACCENT_ORANGE;
                    break;
                default:
                    mainDefeatTitle = "SPIN FINISH";
                    subDefeatTitle = "ROTATIONAL MOMENTUM DEPLETED // STAMINA ZERO";
                    themeColor = ACCENT_CYAN;
                    break;
            }

            float bannerH = Mathf.Clamp(sh * 0.18f, 110f, 200f);
            float bannerW = Mathf.Clamp(sw * 0.72f, 600f, 1300f);
            Rect bannerRect = new Rect((sw - bannerW) * 0.5f, (sh - bannerH) * 0.5f, bannerW, bannerH);

            DrawPanelFrame(bannerRect, new Color(0.05f, 0.01f, 0.02f, 0.92f * alpha), new Color(0.12f, 0.02f, 0.04f, 0.96f * alpha), themeColor, 3f);
            DrawFrameCorners(bannerRect, themeColor, 28f, 3f);
            DrawMotionBandClipped(bannerRect, themeColor, 12f, 18f, 0.12f);

            GUIStyle defeatTitleStyle = new GUIStyle(titleBarStyle)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(bannerH * 0.44f, 32f, 72f)),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            defeatTitleStyle.normal.textColor = Color.white;

            GUIStyle defeatSubStyle = new GUIStyle(sectionLabelStyle)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(bannerH * 0.18f, 12f, 24f)),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            defeatSubStyle.normal.textColor = themeColor;

            GUI.Label(new Rect(bannerRect.x, bannerRect.y + bannerH * 0.14f, bannerRect.width, bannerH * 0.48f), mainDefeatTitle, defeatTitleStyle);
            GUI.Label(new Rect(bannerRect.x, bannerRect.y + bannerH * 0.62f, bannerRect.width, bannerH * 0.28f), subDefeatTitle, defeatSubStyle);
        }

        private void DrawVictoryCelebrationOverlay(MatchManager match)
        {
            int sw = Mathf.RoundToInt(UiWidth);
            int sh = Mathf.RoundToInt(UiHeight);

            float totalWinTime = 5.2f;
            float elapsed = totalWinTime - match.StateTimer;
            float alpha = Mathf.Clamp01(elapsed * 2.5f);

            // Cinematic letterbox bars
            float barH = Mathf.Clamp(sh * 0.10f, 60f, 110f);
            DrawRect(new Rect(0, 0, sw, barH), new Color(0f, 0f, 0f, 0.85f * alpha));
            DrawRect(new Rect(0, sh - barH, sw, barH), new Color(0f, 0f, 0f, 0.85f * alpha));

            float bannerH = Mathf.Clamp(sh * 0.16f, 100f, 180f);
            float bannerW = Mathf.Clamp(sw * 0.65f, 540f, 1150f);
            Rect bannerRect = new Rect((sw - bannerW) * 0.5f, (sh - bannerH) * 0.46f, bannerW, bannerH);

            DrawPanelFrame(bannerRect, new Color(0.02f, 0.05f, 0.10f, 0.90f * alpha), new Color(0.04f, 0.10f, 0.20f, 0.95f * alpha), ACCENT_GOLD, 3f);
            DrawFrameCorners(bannerRect, ACCENT_GOLD, 24f, 2.5f);

            GUIStyle winTitleStyle = new GUIStyle(titleBarStyle)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(bannerH * 0.45f, 30f, 68f)),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            winTitleStyle.normal.textColor = ACCENT_GOLD;

            GUIStyle winSubStyle = new GUIStyle(sectionLabelStyle)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(bannerH * 0.18f, 12f, 22f)),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            winSubStyle.normal.textColor = ACCENT_CYAN;

            GUI.Label(new Rect(bannerRect.x, bannerRect.y + bannerH * 0.12f, bannerRect.width, bannerH * 0.48f), "★ ARENA CLEARED! ★", winTitleStyle);
            GUI.Label(new Rect(bannerRect.x, bannerRect.y + bannerH * 0.60f, bannerRect.width, bannerH * 0.28f), "ALL OPPONENTS BURSTED // VICTORY SECURED (+150 PTS)", winSubStyle);

            // Prompt after 1.2s to continue or wait
            if (elapsed > 1.2f)
            {
                float promptAlpha = Mathf.Clamp01((elapsed - 1.2f) * 2f) * (0.6f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 0.4f);
                GUIStyle promptStyle = new GUIStyle(sectionLabelStyle)
                {
                    fontSize = Mathf.RoundToInt(Mathf.Clamp(bannerH * 0.15f, 11f, 17f)),
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                promptStyle.normal.textColor = new Color(1f, 1f, 1f, promptAlpha);
                GUI.Label(new Rect(0, sh - barH + (barH - 24f) * 0.5f, sw, 24f), "[ SPACE / CLICK ] TO ADVANCE TO SHRINE", promptStyle);
            }
        }

        private void DrawDeathOverlay(MatchManager match)
        {
            int sw = Mathf.RoundToInt(UiWidth);
            int sh = Mathf.RoundToInt(UiHeight);
            float uiScale = GetUiScale();

            if (!lootTransferInitialized)
                InitLootTransferState();

            DrawRect(new Rect(0, 0, sw, sh), OVERLAY);

            float panelW = Mathf.Clamp(sw * 0.90f, 950f, 1600f);
            float panelH = Mathf.Clamp(sh * 0.88f, 620f, 940f);
            Rect panel = new Rect((sw - panelW) * 0.5f, (sh - panelH) * 0.5f, panelW, panelH);

            DrawPanelFrame(panel, new Color(0.06f, 0.02f, 0.03f, 0.97f), new Color(0.10f, 0.03f, 0.04f, 0.99f), RED_DANGER, 4f);
            DrawFrameCorners(panel, new Color(RED_DANGER.r, RED_DANGER.g * 0.6f, RED_DANGER.b * 0.6f, 0.70f), panel.width * 0.06f, 2f);
            DrawMotionBandClipped(new Rect(panel.x + panel.width * 0.60f, panel.y, panel.width * 0.32f, panel.height), RED_DANGER, 10f, 16f, 0.09f);

            string reasonText = string.IsNullOrWhiteSpace(match.LastPlayerDefeatMessage)
                ? "You were defeated."
                : match.LastPlayerDefeatMessage;
            IReadOnlyList<BeyPart> killerParts = match.LastKillerParts;
            bool showKillerBuild = match.LastPlayerDefeatReason == MatchManager.PlayerDefeatReason.BurstedByEnemy
                || match.LastPlayerDefeatReason == MatchManager.PlayerDefeatReason.KnockedOutByEnemy;

            if (showKillerBuild)
            {
                if (!deathOverlayPreviewPrepared)
                {
                    Dictionary<PartType, BeyPart> killerLoadout = BuildLoadoutFromParts(killerParts);
                    if (killerLoadout.Count > 0)
                        RefreshPreviewFromLoadout(killerLoadout);
                    deathOverlayPreviewPrepared = true;
                }
            }
            else
            {
                deathOverlayPreviewPrepared = false;
            }

            float outerPad = Mathf.Clamp(panel.width * 0.022f, 16f, 32f);
            float buttonH = Mathf.Clamp(46f * uiScale, 42f, 60f);
            Rect buttonRect = new Rect(panel.x + outerPad, panel.yMax - outerPad - buttonH, panel.width - outerPad * 2f, buttonH);
            Rect content = new Rect(panel.x + outerPad, panel.y + outerPad, panel.width - outerPad * 2f, buttonRect.y - panel.y - outerPad * 1.5f);

            // ── TOP HEADER ──────────────────────────────────────────────────────────
            GUIStyle deathTitleStyle = new GUIStyle(titleBarStyle)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(panel.height * 0.062f, 26f, 52f)),
                wordWrap = false,
                normal = { textColor = Color.white }
            };
            GUIStyle deathReasonStyle = new GUIStyle(sectionLabelStyle)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(panel.height * 0.026f, 13f, 24f)),
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = new Color(1f, 0.45f, 0.45f, 1f) }
            };

            float titleH = Mathf.Clamp(42f * uiScale, 34f, 54f);
            float reasonH = Mathf.Clamp(26f * uiScale, 20f, 34f);
            float timingH = Mathf.Clamp(22f * uiScale, 18f, 28f);

            DrawFittedLabel(new Rect(content.x, content.y, content.width, titleH), "YOU BURSTED", deathTitleStyle, Color.white, 24);
            DrawFittedLabel(new Rect(content.x, content.y + titleH + 4f, content.width, reasonH), reasonText.ToUpperInvariant(), sectionLabelStyle, new Color(1f, 0.45f, 0.45f, 1f), 13);

            GUIStyle timingStyle = CreateStaticStyle(bodyLabelStyle, ACCENT_CYAN, 12, TextAnchor.MiddleLeft, FontStyle.Bold);
            GUI.Label(new Rect(content.x, content.y + titleH + reasonH + 8f, content.width, timingH),
                $"RUN {FormatRunTime(runElapsedSeconds)}   •   ARENA {FormatRunTime(arenaElapsedSeconds)}   •   {arenasClearedThisRun} ARENAS CLEARED", timingStyle);

            // ── MAIN BODY: 2 EQUAL COLUMNS ──────────────────────────────────────────
            float bodyY = content.y + titleH + reasonH + timingH + 16f;
            float bodyH = content.yMax - bodyY;
            float colGap = 16f;
            float colW = (content.width - colGap) * 0.5f;

            Rect leftCol = new Rect(content.x, bodyY, colW, bodyH);
            Rect rightCol = new Rect(content.x + colW + colGap, bodyY, colW, bodyH);

            // ── LEFT COLUMN: KILLER PREVIEW & BUILD ─────────────────────────────────
            DrawPanelFrame(leftCol, new Color(0.02f, 0.04f, 0.08f, 0.94f), new Color(0.04f, 0.07f, 0.14f, 0.98f), ACCENT_GOLD, 2f);
            DrawFrameCorners(leftCol, ACCENT_GOLD, 14f, 2f);

            float colHeaderH = 34f;
            Rect leftHeaderRect = new Rect(leftCol.x, leftCol.y, leftCol.width, colHeaderH);
            DrawRect(leftHeaderRect, new Color(0.03f, 0.06f, 0.12f, 0.9f));
            DrawRect(new Rect(leftHeaderRect.x, leftHeaderRect.yMax - 2f, leftHeaderRect.width, 2f), ACCENT_GOLD);
            DrawFittedLabel(new Rect(leftHeaderRect.x + 12f, leftHeaderRect.y, leftHeaderRect.width - 24f, leftHeaderRect.height),
                "KILLER PROFILE & LOADOUT", sectionLabelStyle, ACCENT_GOLD, 11);

            float previewH = Mathf.Clamp(leftCol.height * 0.44f, 150f, 260f);
            Rect previewRect = new Rect(leftCol.x + 12f, leftHeaderRect.yMax + 8f, leftCol.width - 24f, previewH);
            DrawRect(previewRect, new Color(0f, 0f, 0f, 0.45f));
            if (previewTexture != null)
            {
                GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit, true);
            }

            Rect killerPartsArea = new Rect(leftCol.x + 12f, previewRect.yMax + 8f, leftCol.width - 24f, leftCol.yMax - previewRect.yMax - 16f);
            DrawKillerPartsSection(killerPartsArea, killerParts, uiScale);

            // ── RIGHT COLUMN: LOOT SALVAGE & DETAIL ──────────────────────────────────
            DrawPanelFrame(rightCol, new Color(0.02f, 0.04f, 0.08f, 0.94f), new Color(0.04f, 0.07f, 0.14f, 0.98f), ACCENT_CYAN, 2f);
            DrawFrameCorners(rightCol, ACCENT_CYAN, 14f, 2f);

            Rect rightHeaderRect = new Rect(rightCol.x, rightCol.y, rightCol.width, colHeaderH);
            DrawRect(rightHeaderRect, new Color(0.03f, 0.06f, 0.12f, 0.9f));
            DrawRect(new Rect(rightHeaderRect.x, rightHeaderRect.yMax - 2f, rightHeaderRect.width, 2f), ACCENT_CYAN);

            int selLoot = CountSelectedLoot();
            string lootHeader = lootEligibleParts != null && lootEligibleParts.Count > 0
                ? $"LOOT SALVAGE  —  {selLoot}/{lootMaxTransferCount}  ▸  MAX: {lootMaxRarityTier.ToString().ToUpper()}"
                : "LOOT SALVAGE  —  NO NEW PARTS TO SALVAGE";
            DrawFittedLabel(new Rect(rightHeaderRect.x + 12f, rightHeaderRect.y, rightHeaderRect.width - 24f, rightHeaderRect.height),
                lootHeader, sectionLabelStyle, ACCENT_CYAN, 11);

            if (lootEligibleParts != null && lootEligibleParts.Count > 0)
            {
                if (selectedLootPart == null || !lootEligibleParts.Contains(selectedLootPart))
                    selectedLootPart = lootEligibleParts[0];

                float listH = Mathf.Clamp((rightCol.height - colHeaderH - 24f) * 0.48f, 110f, 220f);
                Rect listRect = new Rect(rightCol.x + 10f, rightHeaderRect.yMax + 8f, rightCol.width - 20f, listH);
                DrawLootList(listRect, uiScale);

                Rect detailRect = new Rect(rightCol.x + 10f, listRect.yMax + 8f, rightCol.width - 20f, rightCol.yMax - listRect.yMax - 16f);
                DrawLootPartDetailCard(detailRect, selectedLootPart, uiScale);
            }
            else
            {
                GUIStyle noLootStyle = CreateStaticStyle(bodyLabelStyle, Color.gray, 14, TextAnchor.MiddleCenter, FontStyle.Italic);
                GUI.Label(new Rect(rightCol.x + 12f, rightHeaderRect.yMax + 20f, rightCol.width - 24f, rightCol.height - colHeaderH - 40f),
                    "NO TRANSFERABLE PARTS WERE SALVAGED THIS RUN.\nCLEAR MORE ARENAS ON YOUR NEXT ATTEMPT TO UNLOCK HIGHER-TIER SPOILS!", noLootStyle);
            }

            // ── BOTTOM BUTTON ───────────────────────────────────────────────────────
            string btnLabel = lootEligibleParts != null && selLoot > 0
                ? $"TRANSFER LOOT ({selLoot}) & RETURN TO MAIN MENU"
                : "LEAVE RUN & RETURN TO MAIN MENU";
            Color btnColor = selLoot > 0 ? new Color(0.18f, 0.85f, 0.55f, 1f) : ACCENT_RED;
            if (ActionBtn(btnLabel, buttonRect, btnColor, false))
            {
                CommitTransferLootAndReturnToMenu();
            }
        }

        private void DrawKillerPartsSection(Rect area, IReadOnlyList<BeyPart> parts, float uiScale)
        {
            if (parts == null || parts.Count == 0)
            {
                GUIStyle emptyStyle = CreateStaticStyle(bodyLabelStyle, Color.gray, 12, TextAnchor.MiddleCenter, FontStyle.Italic);
                GUI.Label(area, "NO KILLER LOADOUT DATA RECORDED", emptyStyle);
                return;
            }

            float rowH = (area.height - 4f) / Mathf.Max(1, parts.Count);
            rowH = Mathf.Clamp(rowH, 22f, 34f);

            for (int i = 0; i < parts.Count; i++)
            {
                BeyPart part = parts[i];
                if (part == null) continue;

                Rect rowRect = new Rect(area.x, area.y + rowH * i, area.width, rowH - 2f);
                DrawRect(rowRect, new Color(0f, 0f, 0f, 0.35f));

                Color rarityColor = GetRarityColor(part.Rarity);
                DrawRect(new Rect(rowRect.x + 6f, rowRect.center.y - 4f, 8f, 8f), rarityColor);

                int score = Mathf.RoundToInt(GetPartPowerScore(part));
                string partName = PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant();
                string line = $"{partName}  [{part.PartType.ToString().ToUpper()}]";

                GUIStyle nameStyle = CreateStaticStyle(bodyLabelStyle, Color.white, 12, TextAnchor.MiddleLeft, FontStyle.Bold);
                GUIStyle scoreStyle = CreateStaticStyle(bodyLabelStyle, ACCENT_GOLD, 12, TextAnchor.MiddleRight, FontStyle.Bold);

                GUI.Label(new Rect(rowRect.x + 20f, rowRect.y, rowRect.width - 110f, rowRect.height), line, nameStyle);
                GUI.Label(new Rect(rowRect.xMax - 95f, rowRect.y, 90f, rowRect.height), $"SCORE {score}", scoreStyle);
            }
        }

        private void DrawLootList(Rect listArea, float uiScale)
        {
            int sel = CountSelectedLoot();
            float rowH = Mathf.Clamp(36f * uiScale, 32f, 44f);
            float btnW = Mathf.Clamp(76f * uiScale, 68f, 90f);
            float btnH = rowH - 6f;

            GUILayout.BeginArea(listArea);
            lootScroll = GUILayout.BeginScrollView(lootScroll, false, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            for (int i = 0; i < lootEligibleParts.Count; i++)
            {
                BeyPart part = lootEligibleParts[i];
                if (part == null) continue;

                bool isSelected = lootSelectedFlags != null && i < lootSelectedFlags.Count && lootSelectedFlags[i];
                bool canToggle = isSelected || sel < lootMaxTransferCount;
                bool isFocused = selectedLootPart == part;

                Color rarityColor = GetRarityColor(part.Rarity);
                int partScore = Mathf.RoundToInt(GetPartPowerScore(part));

                Rect rowRect = GUILayoutUtility.GetRect(listArea.width - 24f, rowH, GUILayout.ExpandWidth(true), GUILayout.Height(rowH));

                // Row background
                Color rowBg = isFocused
                    ? new Color(0.04f, 0.12f, 0.22f, 0.90f)
                    : (isSelected ? new Color(0.03f, 0.10f, 0.08f, 0.75f) : new Color(0.02f, 0.04f, 0.07f, 0.65f));
                DrawRect(rowRect, rowBg);
                if (isFocused)
                    DrawFrameCorners(rowRect, ACCENT_CYAN, 8f, 1.5f);

                // Rarity Indicator dot
                float dotSize = 8f;
                DrawRect(new Rect(rowRect.x + 8f, rowRect.center.y - dotSize * 0.5f, dotSize, dotSize), rarityColor);

                // Labels
                float labelX = rowRect.x + 22f;
                float labelW = rowRect.width - labelX - (btnW * 2f + 16f);
                string partText = $"{PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant()}  [{part.Rarity.ToString().ToUpper()}]  •  {part.PartType.ToString().ToUpper()}  •  SCORE {partScore}";
                GUIStyle rowStyle = CreateStaticStyle(bodyLabelStyle, isFocused ? Color.white : new Color(0.85f, 0.88f, 0.92f, 0.95f), 12, TextAnchor.MiddleLeft, isFocused ? FontStyle.Bold : FontStyle.Normal);
                GUI.Label(new Rect(labelX, rowRect.y, labelW, rowRect.height), partText, rowStyle);

                // [VIEW] button
                Rect viewBtnRect = new Rect(rowRect.xMax - btnW * 2f - 10f, rowRect.y + 3f, btnW, btnH);
                if (ActionBtn(isFocused ? "VIEWING" : "VIEW", viewBtnRect, isFocused ? ACCENT_CYAN : new Color(0.3f, 0.6f, 0.8f, 0.8f), false))
                {
                    selectedLootPart = part;
                }

                // [KEEP] button
                Rect keepBtnRect = new Rect(rowRect.xMax - btnW - 4f, rowRect.y + 3f, btnW, btnH);
                string keepLabel = isSelected ? "✓ KEEP" : (canToggle ? "KEEP" : "—");
                Color keepColor = isSelected ? new Color(0.2f, 0.9f, 0.45f, 1f) : (canToggle ? ACCENT_GOLD : Color.gray);
                if (ActionBtn(keepLabel, keepBtnRect, keepColor, false, canToggle))
                {
                    if (lootSelectedFlags != null && i < lootSelectedFlags.Count)
                    {
                        lootSelectedFlags[i] = !lootSelectedFlags[i];
                        selectedLootPart = part;
                    }
                }

                GUILayout.Space(3f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawLootPartDetailCard(Rect area, BeyPart part, float uiScale)
        {
            DrawPanelFrame(area, new Color(0.01f, 0.03f, 0.07f, 0.92f), new Color(0.02f, 0.06f, 0.12f, 0.96f), ACCENT_CYAN, 1.5f);
            DrawFrameCorners(area, ACCENT_CYAN, 10f, 1.5f);

            float pad = 10f;
            Rect content = new Rect(area.x + pad, area.y + pad, area.width - pad * 2f, area.height - pad * 2f);

            if (part == null)
            {
                GUIStyle emptyStyle = CreateStaticStyle(bodyLabelStyle, Color.gray, 13, TextAnchor.MiddleCenter, FontStyle.Italic);
                GUI.Label(content, "SELECT A SALVAGED PART TO INSPECT ITS STATS & ABILITY", emptyStyle);
                return;
            }

            Color rarityColor = GetRarityColor(part.Rarity);
            float lineH = 20f;

            // Header: Name + Rarity + Type
            GUIStyle nameStyle = CreateStaticStyle(titleBarStyle, Color.white, 15, TextAnchor.MiddleLeft, FontStyle.Bold);
            GUIStyle badgeStyle = CreateStaticStyle(bodyLabelStyle, rarityColor, 12, TextAnchor.MiddleRight, FontStyle.Bold);
            GUI.Label(new Rect(content.x, content.y, content.width * 0.60f, lineH), PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant(), nameStyle);
            GUI.Label(new Rect(content.x + content.width * 0.60f, content.y, content.width * 0.40f, lineH), $"[{part.Rarity.ToString().ToUpper()}] {part.PartType.ToString().ToUpper()}", badgeStyle);

            // Row 2: Stats summary (Power Score, Weight, and comparison)
            float statsY = content.y + lineH + 4f;
            int score = Mathf.RoundToInt(GetPartPowerScore(part));
            string statSummary = $"POWER SCORE: {score}    |    WEIGHT: {part.Weight:0.0}g";

            Dictionary<PartType, BeyPart> currentLoadout = hasActiveRun && runContext.Player != null
                ? GetCurrentRunLoadout(runContext.Player)
                : selectedMainMenuLoadout;
            if (currentLoadout != null && currentLoadout.TryGetValue(part.PartType, out BeyPart equipped) && equipped != null && equipped != part)
            {
                float diffScore = GetPartPowerScore(part) - GetPartPowerScore(equipped);
                string diffStr = diffScore >= 0 ? $"+{diffScore:0}" : $"{diffScore:0}";
                statSummary += $"  ({diffStr} vs Equipped)";
            }
            GUIStyle statSummaryStyle = CreateStaticStyle(bodyLabelStyle, ACCENT_GOLD, 12, TextAnchor.MiddleLeft, FontStyle.Bold);
            GUI.Label(new Rect(content.x, statsY, content.width, lineH), statSummary, statSummaryStyle);

            // Row 3: Passive or Ability or Description
            float descY = statsY + lineH + 4f;
            Rect descBox = new Rect(content.x, descY, content.width, content.yMax - descY);
            DrawRect(descBox, new Color(0f, 0f, 0f, 0.45f));
            DrawFrameCorners(descBox, new Color(0.2f, 0.5f, 0.8f, 0.5f), 6f, 1f);

            string abilityTitle = "";
            string abilityDesc = "";

            if (part.PartType == PartType.EnergyRing)
            {
                BeyPassive passive = EnergyRingPassiveResolver.Resolve(part);
                if (passive != null)
                {
                    abilityTitle = $"PASSIVE: {passive.PassiveName.ToUpperInvariant()} [{passive.Rarity.ToString().ToUpper()}]";
                    abilityDesc = passive.Description.ToUpperInvariant();
                }
            }
            else if (part.PartType == PartType.FaceBolt)
            {
                BeyAbility ability = ResolveAbilityForPart(part);
                if (ability != null)
                {
                    abilityTitle = $"ABILITY: {ability.AbilityName.ToUpperInvariant()} (MANA: {ability.ManaCost:0.#})";
                    abilityDesc = ability.Description.ToUpperInvariant();
                }
            }

            if (string.IsNullOrEmpty(abilityTitle) && !string.IsNullOrWhiteSpace(part.Description))
            {
                abilityTitle = "DESCRIPTION";
                abilityDesc = part.Description.ToUpperInvariant();
            }

            if (!string.IsNullOrEmpty(abilityTitle))
            {
                GUIStyle aTitleStyle = CreateStaticStyle(bodyLabelStyle, ACCENT_CYAN, 11, TextAnchor.MiddleLeft, FontStyle.Bold);
                GUIStyle aDescStyle = new GUIStyle(bodyLabelStyle)
                {
                    fontSize = 11,
                    wordWrap = true,
                    alignment = TextAnchor.UpperLeft,
                    normal = { textColor = new Color(0.88f, 0.93f, 1f, 0.95f) }
                };
                GUI.Label(new Rect(descBox.x + 6f, descBox.y + 3f, descBox.width - 12f, 16f), abilityTitle, aTitleStyle);
                GUI.Label(new Rect(descBox.x + 6f, descBox.y + 20f, descBox.width - 12f, descBox.height - 22f), abilityDesc, aDescStyle);
            }
            else
            {
                GUIStyle aDescStyle = CreateStaticStyle(bodyLabelStyle, Color.gray, 11, TextAnchor.MiddleCenter, FontStyle.Italic);
                GUI.Label(descBox, "STANDARD PART WITH BALANCED CHARACTERISTICS", aDescStyle);
            }
        }

        // ── Loot transfer helpers ────────────────────────────────────────────────

        private void InitLootTransferState()
        {
            lootTransferInitialized = true;
            int depthIndex = runContext.Progression?.DepthIndex ?? 0;
            int totalArenas = runContext.Progression?.TotalArenaCount ?? 1;
            GetLootTransferRules(depthIndex, totalArenas, out lootMaxTransferCount, out lootMaxRarityTier);

            List<BeyPart> allRun = runContext.Player?.GetRunInventory()?.GetAllParts() ?? new List<BeyPart>();
            lootEligibleParts = new List<BeyPart>();
            foreach (BeyPart p in allRun)
            {
                if (p == null) continue;
                if ((int)p.Rarity > (int)lootMaxRarityTier) continue;
                if (ownedParts.Contains(p)) continue;
                lootEligibleParts.Add(p);
            }
            // highest rarity first
            lootEligibleParts.Sort((a, b) => ((int)b.Rarity).CompareTo((int)a.Rarity));
            lootSelectedFlags = new List<bool>(new bool[lootEligibleParts.Count]);
            lootScroll = Vector2.zero;
        }

        private static void GetLootTransferRules(int depthIndex, int totalArenas,
            out int maxCount, out RarityTier maxRarity)
        {
            float t = totalArenas > 1 ? Mathf.Clamp01((float)depthIndex / (totalArenas - 1)) : 1f;
            maxCount = t < 0.34f ? 1 : t < 0.67f ? 2 : 3;
            maxRarity = t < 0.34f ? RarityTier.Common
                      : t < 0.67f ? RarityTier.Uncommon
                      :             RarityTier.Rare;
        }

        private int CountSelectedLoot()
        {
            if (lootSelectedFlags == null) return 0;
            int n = 0;
            foreach (bool b in lootSelectedFlags) if (b) n++;
            return n;
        }

        private void CommitTransferLootAndReturnToMenu()
        {
            if (lootEligibleParts != null && lootSelectedFlags != null)
            {
                for (int i = 0; i < lootEligibleParts.Count && i < lootSelectedFlags.Count; i++)
                    if (lootSelectedFlags[i])
                        TransferPartsToMainInventory(new[] { lootEligibleParts[i] });
            }
            lootTransferInitialized = false;
            lootEligibleParts       = null;
            lootSelectedFlags       = null;
            ReturnToMainMenu();
        }

        private void TransferPartsToMainInventory(IEnumerable<BeyPart> parts)
        {
            foreach (BeyPart p in parts)
                if (p != null && !ownedParts.Contains(p))
                    ownedParts.Add(p);
            AutoSave();
        }

        private static Dictionary<PartType, BeyPart> BuildLoadoutFromParts(IReadOnlyList<BeyPart> parts)
        {
            Dictionary<PartType, BeyPart> map = new Dictionary<PartType, BeyPart>();
            if (parts == null)
                return map;

            for (int i = 0; i < parts.Count; i++)
            {
                BeyPart part = parts[i];
                if (part == null)
                    continue;

                if (!map.ContainsKey(part.PartType))
                    map[part.PartType] = part;
            }

            return map;
        }

        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null)
                return;

            Texture tex = sprite.texture;
            if (tex == null)
                return;

            Color prevColor = GUI.color;
            GUI.color = Color.white;

            // Direct standard IMGUI rendering for full texture sprites (our icons/emblems)
            if (sprite.rect.width >= tex.width - 1f && sprite.rect.height >= tex.height - 1f)
            {
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true);
                GUI.color = prevColor;
                return;
            }

            // Sub-rect sprite in atlas or sprite sheet
            try
            {
                Rect tr = sprite.rect;
                // Unity IMGUI expects top-left origin UVs, whereas Sprite.rect has bottom-left origin
                Rect uv = new Rect(
                    tr.x / (float)tex.width,
                    (tex.height - tr.yMax) / (float)tex.height,
                    tr.width / (float)tex.width,
                    tr.height / (float)tex.height);
                GUI.DrawTextureWithTexCoords(rect, tex, uv, true);
            }
            catch
            {
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true);
            }
            GUI.color = prevColor;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  MAIN MENU
        // ══════════════════════════════════════════════════════════════════════════

        private void DrawMainMenu()
        {
            RefreshPreviewFromLoadout(selectedMainMenuLoadout);

            int sw = Mathf.RoundToInt(UiWidth), sh = Mathf.RoundToInt(UiHeight);
            float gutter = 10f;
            float topH = 78f;
            float bottomH = 90f;

            Rect screenRect = new Rect(0, 0, sw, sh);
            DrawConceptBackdrop(screenRect);
            DrawRect(screenRect, new Color(0f, 0.01f, 0.03f, 0.18f));

            Rect topRect = new Rect(gutter, gutter, sw - gutter * 2f, topH);
            DrawMainTopBar(topRect);

            Rect bottomRect = new Rect(gutter, sh - bottomH - gutter, sw - gutter * 2f, bottomH);
            Rect contentRect = new Rect(gutter, topRect.yMax + gutter, sw - gutter * 2f, bottomRect.y - topRect.yMax - gutter * 2f);

            switch (mainMenuPanel)
            {
                case MenuPanel.Inventory:
                    DrawInventoryWorkspace(
                        contentRect,
                        false);
                    break;

                case MenuPanel.ShrineCompendium:
                    DrawShrineBlessingsCompendium(contentRect);
                    break;

                case MenuPanel.Minigames:
                    DrawMinigamesArcade(contentRect);
                    break;

                case MenuPanel.Records:
                    DrawPersonalBestPanel(contentRect);
                    break;

                case MenuPanel.Settings:
                    DrawFramedContentPanel(contentRect, "SETTINGS", delegate
                    {
                        DrawSettingsPanel();
                    });
                    break;

                default:
                    DrawGarageOverview(contentRect, null, true);
                    break;
            }

            DrawMainBottomBar(bottomRect);
            DrawTransientUiMessage(new Rect(gutter, bottomRect.y - 42f, sw - gutter * 2f, 32f));

            if (showRunVictoryModal && lastRunUnlockedBlessings != null && lastRunUnlockedBlessings.Count > 0)
            {
                DrawRunVictoryUnlocksModal();
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  PAUSE MENU
        // ══════════════════════════════════════════════════════════════════════════

        private void DrawPauseMenu()
        {
            DrawRunMenuShell("PAUSED", "RESUME", TogglePause);
        }

        private void DrawArenaIntermissionMenu()
        {
            string advanceLabel = runContext.Progression != null && runContext.Progression.IsLastArena
                ? "FINISH RUN"
                : "NEXT ARENA";
            DrawRunMenuShell("ARENA CLEAR", advanceLabel, AdvanceToNextArenaOrFinishRun);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  SHARED SUB-PANELS
        // ══════════════════════════════════════════════════════════════════════════

        private void DrawInventoryPanel(bool isRunInventory)
        {
            float uiScale = GetUiScale();
            float tabH = Mathf.Clamp(42f * uiScale, 38f, 54f);
            float rowMinH = Mathf.Clamp(70f * uiScale, 64f, 90f);

            Dictionary<PartType, BeyPart> currentLoadout;
            List<BeyPart> sourceParts;

            if (isRunInventory)
            {
                PlayerManager player = runContext.Player;
                if (player == null) { GUILayout.Label("NO ACTIVE RUN.", bodyLabelStyle); return; }
                currentLoadout = GetCurrentRunLoadout(player);
                sourceParts = player.GetRunInventory().GetAllParts();
            }
            else
            {
                currentLoadout = selectedMainMenuLoadout;
                sourceParts = ownedParts;
            }

            if (!selectedInventorySlot.HasValue)
                selectedInventorySlot = PartType.FaceBolt;

            PartType activeSlot = selectedInventorySlot.Value;

            // ── 5 CATEGORY TABS (EXPLICIT PIXEL-SNAPPED LAYOUT) ─────────────────
            int tabCount = PART_DISPLAY_ORDER.Length;
            float tabGap = Mathf.Round(4f * uiScale);
            float availW = Mathf.Max(300f, UiWidth * 0.50f);
            float tabWidth = Mathf.Floor((availW - (tabCount - 1) * tabGap) / tabCount);

            Rect tabsBarRect = GUILayoutUtility.GetRect(availW, tabH, GUILayout.Width(availW), GUILayout.Height(tabH));
            int barX = Mathf.RoundToInt(tabsBarRect.x);
            int barY = Mathf.RoundToInt(tabsBarRect.y);
            int barH = Mathf.RoundToInt(tabH);
            int tabW = Mathf.RoundToInt(tabWidth);
            int gapI = Mathf.RoundToInt(tabGap);

            for (int t = 0; t < tabCount; t++)
            {
                PartType slot = PART_DISPLAY_ORDER[t];
                bool isActive = activeSlot == slot;
                string tabLabel = slot switch
                {
                    PartType.FaceBolt => "FACE BOLT",
                    PartType.EnergyRing => "ENERGY RING",
                    PartType.FusionWheel => "FUSION WHEEL",
                    PartType.Track => "SPIN TRACK",
                    PartType.Tip => "PERF. TIP",
                    _ => slot.ToString().ToUpper()
                };

                Rect tabRect = new Rect(barX + t * (tabW + gapI), barY, tabW, barH);
                if (DrawCategoryTabBtn(tabLabel, tabRect, isActive))
                {
                    selectedInventorySlot = slot;
                    activeSlot = slot;
                }
            }

            GUILayout.Space(Mathf.Clamp(8f * uiScale, 6f, 14f));

            List<BeyPart> parts = GetPartsByType(sourceParts, activeSlot);
            if (selectedInventoryPart == null || selectedInventoryPart.PartType != activeSlot || !parts.Contains(selectedInventoryPart))
                selectedInventoryPart = parts.Count > 0 ? parts[0] : null;

            currentLoadout.TryGetValue(activeSlot, out BeyPart equippedPart);

            if (parts.Count == 0)
            {
                GUILayout.Label($"NO {activeSlot.ToString().ToUpper()} PARTS OWNED.", bodyLabelStyle);
                return;
            }

            // ── SCROLLABLE PART LIST ─────────────────────────────────────────────
            if (isRunInventory)
                runScroll = GUILayout.BeginScrollView(runScroll, GUILayout.ExpandHeight(true));
            else
                ownedScroll = GUILayout.BeginScrollView(ownedScroll, GUILayout.ExpandHeight(true));

            for (int i = 0; i < parts.Count; i++)
            {
                BeyPart part = parts[i];
                if (part == null) continue;

                bool isSelected = selectedInventoryPart == part;
                bool isEquipped = equippedPart == part;

                Rect rowRaw = GUILayoutUtility.GetRect(10f, rowMinH, GUILayout.ExpandWidth(true));
                Rect row = new Rect(Mathf.Round(rowRaw.x), Mathf.Round(rowRaw.y), Mathf.Round(rowRaw.width), Mathf.Round(rowRaw.height));

                Color rowBg = isSelected
                    ? new Color(0.08f, 0.22f, 0.38f, 0.96f)
                    : (isEquipped ? new Color(0.05f, 0.15f, 0.24f, 0.90f) : new Color(0.03f, 0.07f, 0.13f, 0.85f));
                DrawRect(row, rowBg);
                DrawRect(new Rect(row.x, row.yMax - 2f, row.width, 2f), isSelected ? ACCENT_CYAN : (isEquipped ? ACCENT_YEL : new Color(1f, 1f, 1f, 0.06f)));

                if (isSelected || isEquipped)
                    DrawFrameCorners(row, isSelected ? ACCENT_CYAN : ACCENT_YEL, 16f, 1.5f);

                // Part icon
                float iconSz = Mathf.Min(row.height - 16f, 52f);
                Rect iconRect = new Rect(row.x + 8f, row.center.y - iconSz * 0.5f, iconSz, iconSz);
                if (activeSlot != PartType.FaceBolt && swapPartPreviewCache.TryGetValue(part.GetInstanceID(), out RenderTexture swapRT) && swapRT != null && swapRT.IsCreated())
                    GUI.DrawTexture(iconRect, swapRT, ScaleMode.ScaleToFit, true);
                else
                    DrawPartSprite(iconRect, part);

                // Labels
                float labelX = iconRect.xMax + 12f;
                float labelW = row.width * 0.44f;
                string partDisplayName = PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant();
                DrawFittedLabel(new Rect(labelX, row.y + 8f, labelW, 24f), partDisplayName, sectionLabelStyle, isEquipped ? ACCENT_YEL : Color.white, 10);
                DrawFittedLabel(new Rect(labelX, row.y + 32f, labelW, 20f), $"POWER {Mathf.RoundToInt(GetPartPowerScore(part))}  //  WT {part.Weight:0.0}G", bodyLabelStyle, new Color(0.70f, 0.88f, 1f, 0.85f), 10);

                // Rarity Pill
                DrawRarityPill(new Rect(row.xMax - 196f, row.y + 12f, 86f, row.height - 24f), part.Rarity, part.Rarity.ToString().ToUpperInvariant());

                // Equip / Status button
                Rect btnRect = new Rect(row.xMax - 100f, row.y + 12f, 92f, row.height - 24f);
                if (isEquipped)
                {
                    DrawRect(btnRect, new Color(0.12f, 0.35f, 0.22f, 0.90f));
                    DrawRect(new Rect(btnRect.x, btnRect.yMax - 2f, btnRect.width, 2f), new Color(0.25f, 0.95f, 0.45f, 1f));
                    DrawFittedLabel(btnRect, "EQUIPPED", bodyLabelStyle, new Color(0.4f, 1f, 0.55f, 1f), 9);
                }
                else
                {
                    if (ActionBtn("EQUIP", btnRect, ACCENT_CYAN, false))
                    {
                        selectedInventoryPart = part;
                        if (isRunInventory)
                        {
                            runContext.Player?.EquipPart(part);
                            RefreshPreviewFromLoadout(GetCurrentRunLoadout(runContext.Player));
                        }
                        else
                        {
                            selectedMainMenuLoadout[activeSlot] = part;
                            RefreshPreviewFromLoadout(selectedMainMenuLoadout);
                            AutoSave();
                        }
                    }
                }

                // Clicking anywhere on row selects it
                if (WithButtonSound(GUI.Button(new Rect(row.x, row.y, row.width - 100f, row.height), GUIContent.none, GUIStyle.none)))
                {
                    selectedInventoryPart = part;
                }
            }

            if (isRunInventory)
                GUILayout.EndScrollView();
            else
                GUILayout.EndScrollView();
        }

        private bool DrawCategoryTabBtn(string label, Rect rect, bool active)
        {
            int rx = Mathf.RoundToInt(rect.x);
            int ry = Mathf.RoundToInt(rect.y);
            int rw = Mathf.RoundToInt(rect.width);
            int rh = Mathf.RoundToInt(rect.height);
            Rect r = new Rect(rx, ry, rw, rh);

            Color bg = active ? ACCENT_YEL : PANEL_STEEL;
            Color fg = active ? Color.black : Color.white;
            int bottomBorderH = Mathf.Max(2, Mathf.RoundToInt(rh * 0.06f));
            int leftAccentW = active ? Mathf.Max(3, Mathf.RoundToInt(rw * 0.03f)) : 0;

            DrawRect(new Rect(rx - 1, ry - 1, rw + 2, rh + 2), Color.black);
            DrawRect(r, bg);
            if (!active)
            {
                DrawRect(new Rect(rx, ry, 4, rh), ACCENT_CYAN);
                DrawRect(new Rect(rx + 4, ry, Mathf.RoundToInt(rw * 0.18f), rh), new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.10f));
            }
            DrawRect(new Rect(rx, ry + rh - bottomBorderH, rw, bottomBorderH), Color.black);
            if (active) DrawRect(new Rect(rx, ry, leftAccentW, rh), Color.black);

            Rect labelRect = new Rect(rx + leftAccentW, ry, rw - leftAccentW, rh - bottomBorderH);
            GUIStyle fittedStyle = FitLabelStyle(inlineActionButtonStyle, label, labelRect.width - 4f, 12, labelRect.height - 4f);

            GUI.contentColor = fg;
            GUI.Label(labelRect, label, fittedStyle);
            GUI.contentColor = Color.white;
            return WithButtonSound(GUI.Button(r, GUIContent.none, GUIStyle.none));
        }

        private void DrawInventoryWorkspace(
            Rect area,
            bool isRunInventory)
        {
            DrawPanelFrame(
                area,
                new Color(0.03f, 0.06f, 0.11f, 0.94f),
                new Color(0.05f, 0.10f, 0.16f, 0.95f),
                ACCENT_CYAN,
                2f);

            float uiScale = GetUiScale();
            float pad = Mathf.Clamp(12f * uiScale, 10f, 20f);
            float headerH = Mathf.Clamp(34f * uiScale, 30f, 48f);
            float gap = Mathf.Clamp(12f * uiScale, 10f, 20f);

            DrawFittedLabel(
                new Rect(area.x + pad, area.y + 6f, area.width - pad * 2f, headerH),
                isRunInventory ? "RUN INVENTORY // PART VAULT" : "INVENTORY // PART VAULT",
                sectionLabelStyle,
                ACCENT_CYAN,
                10);

            Rect content = new Rect(
                Mathf.Round(area.x + pad),
                Mathf.Round(area.y + headerH + 8f),
                Mathf.Round(area.width - pad * 2f),
                Mathf.Round(area.height - headerH - pad - 8f));

            float detailW = Mathf.Round(Mathf.Clamp(content.width * 0.36f, 320f, 540f));
            Rect listRect = new Rect(
                content.x,
                content.y,
                Mathf.Round(Mathf.Max(260f, content.width - detailW - gap)),
                content.height);
            Rect detailRect = new Rect(
                Mathf.Round(listRect.xMax + gap),
                content.y,
                detailW,
                content.height);

            GUILayout.BeginArea(listRect);
            DrawInventoryPanel(isRunInventory);
            GUILayout.EndArea();

            DrawSelectedPartCardInRect(
                detailRect,
                selectedInventoryPart,
                "PART SPECIFICATIONS");
        }

        private void DrawMinigamesArcade(Rect area)
        {
            float uiScale = GetUiScale();
            float pad = Mathf.Clamp(14f * uiScale, 12f, 22f);

            DrawPanelFrame(area, new Color(0.015f, 0.035f, 0.07f, 0.96f), new Color(0.03f, 0.07f, 0.14f, 0.98f), ACCENT_CYAN, 2f);
            DrawFrameCorners(area, ACCENT_CYAN, 24f, 2f);

            if (MinigameArcadeManager.State == ArcadeState.Browser)
            {
                // ── HEADER ────────────────────────────────────────────────────────
                float headerH = 54f;
                Rect headerRect = new Rect(area.x + pad, area.y + 10f, area.width - pad * 2f, headerH);
                DrawFittedLabel(new Rect(headerRect.x, headerRect.y, headerRect.width, 28f),
                    "CLASH ARCADE // MINIGAME DOJO [試練道場]", titleBarStyle, ACCENT_CYAN, 18);
                DrawFittedLabel(new Rect(headerRect.x, headerRect.y + 28f, headerRect.width, 22f),
                    "TRAIN CLASH DUEL MECHANICS STANDALONE • EARN HIGH SCORES • EXCLUSIVE LEFT MOUSE BUTTON CONTROLS",
                    sectionLabelStyle, ACCENT_GOLD, 12);

                // ── GRID (3 columns x 2 rows) ─────────────────────────────────────
                float topY = headerRect.yMax + 10f;
                Rect gridArea = new Rect(area.x + pad, topY, area.width - pad * 2f, area.yMax - topY - pad);
                float cardGap = 14f;
                float cardW = (gridArea.width - cardGap * 2f) / 3f;
                float cardH = (gridArea.height - cardGap) / 2f;

                for (int i = 0; i < 6; i++)
                {
                    ClashMinigameType type = (ClashMinigameType)i;
                    var info = MinigameArcadeManager.GetMinigameInfo(type);
                    int highScore = MinigameArcadeManager.GetHighScore(type);

                    int col = i % 3;
                    int row = i / 3;
                    Rect cRect = new Rect(gridArea.x + col * (cardW + cardGap), gridArea.y + row * (cardH + cardGap), cardW, cardH);

                    Color cardAccent = highScore > 0 ? ACCENT_GOLD : ACCENT_CYAN;
                    DrawPanelFrame(cRect, new Color(0.025f, 0.055f, 0.11f, 0.92f), new Color(0.04f, 0.08f, 0.16f, 0.95f), cardAccent, 1.5f);
                    DrawFrameCorners(cRect, cardAccent, 12f, 1.5f);

                    float cPad = 12f;
                    // Tag
                    DrawFittedLabel(new Rect(cRect.x + cPad, cRect.y + 8f, cRect.width - cPad * 2f, 18f),
                        info.category, sectionLabelStyle, new Color(0.40f, 0.85f, 1f, 0.95f), 12);

                    // Title
                    DrawFittedLabel(new Rect(cRect.x + cPad, cRect.y + 28f, cRect.width - cPad * 2f, 26f),
                        $"{info.title}  {info.jp}", titleBarStyle, Color.white, 16);

                    // Description
                    float btnH = 38f;
                    float scoreH = 26f;
                    float descTop = cRect.y + 56f;
                    float descH = cRect.yMax - btnH - scoreH - 24f - descTop;
                    Rect descRect = new Rect(cRect.x + cPad, descTop, cRect.width - cPad * 2f, descH);
                    GUIStyle descStyle = new GUIStyle(bodyLabelStyle)
                    {
                        wordWrap = true,
                        clipping = TextClipping.Clip,
                        fontSize = 13
                    };
                    GUI.Label(descRect, info.desc, descStyle);

                    // High score bar
                    Rect scoreRect = new Rect(cRect.x + cPad, cRect.yMax - btnH - scoreH - 12f, cRect.width - cPad * 2f, scoreH);
                    DrawRect(scoreRect, new Color(0f, 0f, 0f, 0.45f));
                    string scoreText = highScore > 0
                        ? $"TOP RECORD: {highScore:N0} PTS"
                        : "NO RECORD SET YET";
                    Color scoreColor = highScore > 0 ? ACCENT_GOLD : new Color(0.6f, 0.7f, 0.8f, 0.75f);
                    DrawFittedLabel(scoreRect, scoreText, sectionLabelStyle, scoreColor, 12);

                    // Play button
                    Rect btnRect = new Rect(cRect.x + cPad, cRect.yMax - btnH - 8f, cRect.width - cPad * 2f, btnH);
                    if (ActionBtn("START PRACTICE DRILL", btnRect, ACCENT_CYAN, false))
                    {
                        MinigameArcadeManager.StartSession(type);
                    }
                }
            }
            else if (MinigameArcadeManager.State == ArcadeState.Playing)
            {
                var info = MinigameArcadeManager.GetMinigameInfo(MinigameArcadeManager.SelectedMinigame);

                // ── PLAYING HUD HEADER ────────────────────────────────────────────
                float headH = 46f;
                Rect topHead = new Rect(area.x + pad, area.y + 12f, area.width - pad * 2f, headH);
                DrawFittedLabel(new Rect(topHead.x, topHead.y, topHead.width * 0.45f, 26f),
                    $"{info.title}  {info.jp}", titleBarStyle, ACCENT_CYAN, 14);

                // Score + Streak badge
                string scoreStr = $"SCORE: {MinigameArcadeManager.CurrentScore:N0} PTS";
                if (MinigameArcadeManager.Streak > 1)
                    scoreStr += $"  [STREAK x{MinigameArcadeManager.Streak}]";
                DrawFittedLabel(new Rect(topHead.x + topHead.width * 0.45f, topHead.y, topHead.width * 0.55f, 26f),
                    scoreStr, titleBarStyle, ACCENT_GOLD, 14);

                // Timer Bar
                float timePct = Mathf.Clamp01(MinigameArcadeManager.TimeRemaining / MinigameArcadeManager.TotalDuration);
                Rect timeBarBg = new Rect(topHead.x, topHead.y + 30f, topHead.width, 14f);
                DrawRect(timeBarBg, new Color(0.04f, 0.08f, 0.15f, 0.9f));
                DrawRect(new Rect(timeBarBg.x, timeBarBg.y, timeBarBg.width * timePct, timeBarBg.height),
                    timePct > 0.25f ? ACCENT_CYAN : ACCENT_RED);
                DrawFittedLabel(timeBarBg, $"TIME REMAINING: {MinigameArcadeManager.TimeRemaining:0.0}s", bodyLabelStyle, Color.white, 9);

                // ── INTERACTIVE MINIGAME GAUGE AREA ──────────────────────────────
                bool isOrbital = MinigameArcadeManager.SelectedMinigame == ClashMinigameType.OrbitalCrosshair;
                float gaugeW = isOrbital ? 340f : Mathf.Clamp(area.width * 0.72f, 480f, 780f);
                float gaugeH = isOrbital ? 250f : 64f;
                Rect gaugeRect = new Rect(area.center.x - gaugeW * 0.5f, area.center.y - gaugeH * 0.5f, gaugeW, gaugeH);

                DrawRect(gaugeRect, new Color(0.05f, 0.05f, 0.08f, 0.92f));
                DrawPanelFrame(gaugeRect, new Color(0f, 0f, 0f, 0.8f), new Color(0.05f, 0.05f, 0.08f, 0.9f), new Color(0.4f, 0.5f, 0.6f, 0.6f), 1.5f);

                switch (MinigameArcadeManager.SelectedMinigame)
                {
                    case ClashMinigameType.RapidMash:
                        // Fill meter
                        Rect fillRect = new Rect(gaugeRect.x + 3f, gaugeRect.y + 3f, (gaugeRect.width - 6f) * MinigameArcadeManager.ClashMeter, gaugeRect.height - 6f);
                        Color fillCol = Color.Lerp(ACCENT_RED, ACCENT_CYAN, MinigameArcadeManager.ClashMeter);
                        DrawRect(fillRect, fillCol);
                        DrawFittedLabel(gaugeRect, $"RAPID MASH // CLICKS: {MinigameArcadeManager.MashCount}", bodyLabelStyle, Color.white, 11);
                        break;

                    case ClashMinigameType.PrecisionTiming:
                        float sweetX1 = gaugeRect.x + gaugeRect.width * MinigameArcadeManager.SweetSpotMin;
                        float sweetW = gaugeRect.width * (MinigameArcadeManager.SweetSpotMax - MinigameArcadeManager.SweetSpotMin);
                        Rect sweetRect = new Rect(sweetX1, gaugeRect.y + 2f, sweetW, gaugeRect.height - 4f);
                        DrawRect(sweetRect, new Color(0.2f, 0.95f, 0.45f, 0.45f));
                        DrawFrameCorners(sweetRect, ACCENT_GOLD, 6f, 2f);
                        DrawSidewaysLabel(sweetRect, "TARGET ZONE", bodyLabelStyle, ACCENT_GOLD);

                        float needleX = gaugeRect.x + gaugeRect.width * MinigameArcadeManager.NeedlePos;
                        Rect needleRect = new Rect(needleX - 3f, gaugeRect.y - 4f, 6f, gaugeRect.height + 8f);
                        DrawRect(needleRect, ACCENT_RED);
                        DrawFrameCorners(needleRect, Color.white, 4f, 1.5f);
                        break;

                    case ClashMinigameType.RhythmBeat:
                        // Taiko Conveyor Track Line
                        float trackY = gaugeRect.center.y;
                        DrawRect(new Rect(gaugeRect.x + 8f, trackY - 2f, gaugeRect.width - 16f, 4f), new Color(0.25f, 0.45f, 0.7f, 0.7f));

                        // Target drum circle on left
                        float targetX = gaugeRect.x + gaugeRect.width * MinigameArcadeManager.RhythmTargetX;
                        float hitRadius = Mathf.Clamp(gaugeRect.height * 0.38f, 20f, 30f);
                        Rect targetZoneRect = new Rect(targetX - hitRadius, trackY - hitRadius, hitRadius * 2f, hitRadius * 2f);
                        DrawPanelFrame(targetZoneRect, new Color(0.1f, 0.35f, 0.7f, 0.5f), Color.clear, ACCENT_GOLD, 3f);
                        DrawFrameCorners(targetZoneRect, ACCENT_GOLD, 10f, 2f);
                        DrawFittedLabel(targetZoneRect, "HIT", sectionLabelStyle, ACCENT_GOLD, 12);

                        // Moving rhythm notes
                        for (int n = 0; n < MinigameArcadeManager.ActiveNotes.Count; n++)
                        {
                            float noteX = gaugeRect.x + gaugeRect.width * MinigameArcadeManager.ActiveNotes[n];
                            if (noteX >= gaugeRect.x - 20f && noteX <= gaugeRect.xMax + 20f)
                            {
                                float noteR = hitRadius * 0.75f;
                                Rect noteRect = new Rect(noteX - noteR, trackY - noteR, noteR * 2f, noteR * 2f);
                                DrawRect(noteRect, new Color(1f, 0.40f, 0.08f, 0.95f));
                                DrawFrameCorners(noteRect, Color.white, 6f, 2f);
                                DrawFittedLabel(noteRect, "BEAT", bodyLabelStyle, Color.white, 9);
                            }
                        }
                        break;

                    case ClashMinigameType.TensionBalance:
                        float bTargetX = gaugeRect.x + gaugeRect.width * MinigameArcadeManager.BalanceTargetPos;
                        float bZoneW = gaugeRect.width * 0.44f;
                        Rect bZoneRect = new Rect(bTargetX - bZoneW * 0.5f, gaugeRect.y + 3f, bZoneW, gaugeRect.height - 6f);
                        DrawRect(bZoneRect, new Color(0.2f, 0.9f, 0.5f, 0.4f));
                        DrawFrameCorners(bZoneRect, ACCENT_GOLD, 6f, 2f);
                        DrawSidewaysLabel(bZoneRect, "BALANCE ZONE", bodyLabelStyle, ACCENT_GOLD);

                        float pBobX = gaugeRect.x + gaugeRect.width * MinigameArcadeManager.BalanceBobberPos;
                        Rect pBobRect = new Rect(pBobX - 10f, gaugeRect.y - 2f, 20f, gaugeRect.height + 4f);
                        DrawRect(pBobRect, ACCENT_CYAN);
                        DrawFrameCorners(pBobRect, Color.white, 4f, 1.5f);
                        break;

                    case ClashMinigameType.OrbitalCrosshair:
                        Vector2 dialCenter = gaugeRect.center;
                        float dialRadius = gaugeH * 0.40f;

                        // Circular Radar Track (crosshairs + radar frame)
                        DrawRect(new Rect(dialCenter.x - dialRadius - 16f, dialCenter.y - 1f, (dialRadius + 16f) * 2f, 2f), new Color(0.2f, 0.5f, 0.8f, 0.45f));
                        DrawRect(new Rect(dialCenter.x - 1f, dialCenter.y - dialRadius - 16f, 2f, (dialRadius + 16f) * 2f), new Color(0.2f, 0.5f, 0.8f, 0.45f));
                        
                        // Outer radar frame
                        Rect dialBounds = new Rect(dialCenter.x - dialRadius, dialCenter.y - dialRadius, dialRadius * 2f, dialRadius * 2f);
                        DrawPanelFrame(dialBounds, new Color(0.04f, 0.08f, 0.16f, 0.75f), Color.clear, new Color(0.3f, 0.65f, 0.95f, 0.7f), 2f);
                        DrawFrameCorners(dialBounds, ACCENT_CYAN, 18f, 2f);

                        // Inner decorative radar ring
                        float innerRadius = dialRadius * 0.55f;
                        Rect innerBounds = new Rect(dialCenter.x - innerRadius, dialCenter.y - innerRadius, innerRadius * 2f, innerRadius * 2f);
                        DrawPanelFrame(innerBounds, Color.clear, Color.clear, new Color(0.25f, 0.5f, 0.75f, 0.35f), 1f);

                        // Circular Lock Target Zone on the perimeter
                        float tRad = MinigameArcadeManager.TargetLockAngle * Mathf.Deg2Rad;
                        Vector2 tPos = dialCenter + new Vector2(Mathf.Cos(tRad), Mathf.Sin(tRad)) * dialRadius;
                        float lockSz = 52f;
                        Rect lockCircleRect = new Rect(tPos.x - lockSz * 0.5f, tPos.y - lockSz * 0.5f, lockSz, lockSz);
                        DrawPanelFrame(lockCircleRect, new Color(0.9f, 0.4f, 0.1f, 0.65f), new Color(0.18f, 0.09f, 0.02f, 0.9f), ACCENT_GOLD, 3f);
                        DrawFrameCorners(lockCircleRect, ACCENT_GOLD, 12f, 2f);
                        DrawFittedLabel(lockCircleRect, "LOCK", titleBarStyle, ACCENT_GOLD, 15);

                        // Orbiting Spark along the perimeter
                        float oRad = MinigameArcadeManager.OrbitAngle * Mathf.Deg2Rad;
                        Vector2 oPos = dialCenter + new Vector2(Mathf.Cos(oRad), Mathf.Sin(oRad)) * dialRadius;
                        float orbSz = 26f;
                        Rect orbCircleRect = new Rect(oPos.x - orbSz * 0.5f, oPos.y - orbSz * 0.5f, orbSz, orbSz);
                        DrawRect(orbCircleRect, ACCENT_CYAN);
                        DrawFrameCorners(orbCircleRect, Color.white, 6f, 2f);

                        // Proximity glow
                        float angleDiffArcade = Mathf.Abs(Mathf.DeltaAngle(MinigameArcadeManager.OrbitAngle, MinigameArcadeManager.TargetLockAngle));
                        if (angleDiffArcade <= 32f)
                        {
                            DrawPanelFrame(lockCircleRect, new Color(1f, 0.8f, 0.2f, 0.4f), Color.clear, Color.white, 3f);
                        }
                        break;

                    case ClashMinigameType.ReflexTrigger:
                        if (MinigameArcadeManager.ReflexSignalActive && !MinigameArcadeManager.FalseStart)
                        {
                            DrawRect(gaugeRect, new Color(1f, 0.85f, 0.1f, 0.85f));
                            DrawFittedLabel(gaugeRect, "[ STRIKE NOW! CLICK LEFT MOUSE BUTTON! ]", titleBarStyle, Color.black, 15);
                        }
                        else if (MinigameArcadeManager.FalseStart)
                        {
                            DrawRect(gaugeRect, new Color(0.8f, 0.1f, 0.1f, 0.85f));
                            DrawFittedLabel(gaugeRect, "FALSE START // PREMATURE CLICK!", titleBarStyle, Color.white, 13);
                        }
                        else
                        {
                            float standbyPulse = 0.4f + 0.3f * Mathf.Sin(Time.unscaledTime * 12f);
                            DrawRect(gaugeRect, new Color(0.8f, 0.2f, 0.1f, standbyPulse));
                            DrawFittedLabel(gaugeRect, "STAND BY... DO NOT CLICK!", sectionLabelStyle, Color.white, 13);
                        }
                        break;
                }

                // ── POPUP FEEDBACK ────────────────────────────────────────────────
                if (MinigameArcadeManager.FeedbackTimer > 0f && !string.IsNullOrEmpty(MinigameArcadeManager.FeedbackText))
                {
                    Rect feedbackRect = new Rect(gaugeRect.x, gaugeRect.y - 40f, gaugeRect.width, 32f);
                    DrawFittedLabel(feedbackRect, MinigameArcadeManager.FeedbackText, titleBarStyle, MinigameArcadeManager.FeedbackColor, 15);
                }

                // Instructions prompt
                Rect promptRect = new Rect(gaugeRect.x, gaugeRect.yMax + 14f, gaugeRect.width, 24f);
                DrawFittedLabel(promptRect, info.desc, bodyLabelStyle, new Color(0.8f, 0.9f, 1f, 0.9f), 11);

                // Abort Button
                Rect abortRect = new Rect(area.center.x - 120f, area.yMax - 48f, 240f, 36f);
                if (ActionBtn("ABORT DRILL & RETURN", abortRect, ACCENT_RED, false))
                {
                    MinigameArcadeManager.AbortSession();
                }
            }
            else if (MinigameArcadeManager.State == ArcadeState.RoundOver)
            {
                var info = MinigameArcadeManager.GetMinigameInfo(MinigameArcadeManager.SelectedMinigame);
                float modalW = Mathf.Clamp(area.width * 0.52f, 440f, 560f);
                float modalH = 340f;
                Rect modalRect = new Rect(area.center.x - modalW * 0.5f, area.center.y - modalH * 0.5f, modalW, modalH);

                DrawPanelFrame(modalRect, new Color(0.02f, 0.05f, 0.10f, 0.98f), new Color(0.04f, 0.09f, 0.18f, 0.98f), ACCENT_GOLD, 3f);
                DrawFrameCorners(modalRect, ACCENT_GOLD, 20f, 2f);

                DrawFittedLabel(new Rect(modalRect.x, modalRect.y + 16f, modalRect.width, 28f),
                    $"DRILL COMPLETED // {info.title}", titleBarStyle, ACCENT_CYAN, 14);

                if (MinigameArcadeManager.IsNewHighScore)
                {
                    DrawRect(new Rect(modalRect.x + 30f, modalRect.y + 52f, modalRect.width - 60f, 32f), new Color(0.8f, 0.6f, 0.1f, 0.35f));
                    DrawFittedLabel(new Rect(modalRect.x, modalRect.y + 54f, modalRect.width, 28f),
                        "*** NEW TOP RECORD SET! ***", titleBarStyle, ACCENT_GOLD, 15);
                }
                else
                {
                    DrawFittedLabel(new Rect(modalRect.x, modalRect.y + 54f, modalRect.width, 24f),
                        $"PERSONAL BEST: {MinigameArcadeManager.BestScore:N0} PTS", bodyLabelStyle, new Color(0.7f, 0.85f, 1f, 0.85f), 12);
                }

                // Giant Final Score
                Rect scoreRect = new Rect(modalRect.x, modalRect.y + 104f, modalRect.width, 60f);
                GUIStyle giantScoreStyle = new GUIStyle(titleBarStyle)
                {
                    fontSize = 36,
                    alignment = TextAnchor.MiddleCenter
                };
                giantScoreStyle.normal.textColor = ACCENT_GOLD;
                GUI.Label(scoreRect, $"{MinigameArcadeManager.CurrentScore:N0} PTS", giantScoreStyle);

                // Buttons
                float btnW = (modalRect.width - 40f) * 0.48f;
                float btnY = modalRect.yMax - 54f;
                Rect playAgainRect = new Rect(modalRect.x + 16f, btnY, btnW, 40f);
                Rect backRect = new Rect(modalRect.xMax - 16f - btnW, btnY, btnW, 40f);

                if (ActionBtn("PLAY AGAIN", playAgainRect, ACCENT_CYAN, false))
                {
                    MinigameArcadeManager.StartSession(MinigameArcadeManager.SelectedMinigame);
                }
                if (ActionBtn("RETURN TO ARCADE", backRect, PANEL_STEEL, false))
                {
                    MinigameArcadeManager.AbortSession();
                }
            }
        }

        private void DrawPersonalBestPanel(Rect area)
        {
            DrawPanelFrame(
                area,
                new Color(0.03f, 0.06f, 0.11f, 0.94f),
                new Color(0.05f, 0.10f, 0.16f, 0.95f),
                ACCENT_CYAN,
                2f);

            float uiScale = GetUiScale();
            float pad = Mathf.Clamp(12f * uiScale, 10f, 20f);
            float headerH = Mathf.Clamp(34f * uiScale, 30f, 48f);
            float colGap = Mathf.Clamp(16f * uiScale, 12f, 24f);

            DrawFittedLabel(
                new Rect(area.x + pad, area.y + 6f, area.width - pad * 2f, headerH),
                "TOURNAMENT RECORDS // 大会戦績",
                sectionLabelStyle,
                ACCENT_CYAN,
                10);

            Rect content = new Rect(
                Mathf.Round(area.x + pad),
                Mathf.Round(area.y + headerH + 8f),
                Mathf.Round(area.width - pad * 2f),
                Mathf.Round(area.height - headerH - pad - 8f));

            IReadOnlyList<RunRecord> fastest = RunRecordStore.GetFastestCompleted();
            IReadOnlyList<RunRecord> deepest = RunRecordStore.GetDeepest();

            int colW = Mathf.RoundToInt((content.width - colGap) * 0.5f);
            int colH = Mathf.RoundToInt(content.height);

            Rect leftCol = new Rect(content.x, content.y, colW, colH);
            Rect rightCol = new Rect(content.x + colW + Mathf.RoundToInt(colGap), content.y, colW, colH);

            DrawRunRecordColumnStyled(leftCol, "FASTEST COMPLETED RUNS // 最速走破記録", fastest, true, ACCENT_CYAN, uiScale);
            DrawRunRecordColumnStyled(rightCol, "MOST ARENAS CLEARED // 最高到達記録", deepest, false, ACCENT_GOLD, uiScale);
        }

        private void DrawRunRecordColumnStyled(
            Rect colRect,
            string header,
            IReadOnlyList<RunRecord> records,
            bool fastestColumn,
            Color themeColor,
            float uiScale)
        {
            DrawPanelFrame(colRect, new Color(0.02f, 0.04f, 0.09f, 0.94f), new Color(0.04f, 0.08f, 0.15f, 0.96f), themeColor, 2f);
            DrawFrameCorners(colRect, themeColor, 12f, 2f);

            float pad = Mathf.Clamp(10f * uiScale, 8f, 16f);
            float headerH = Mathf.Clamp(36f * uiScale, 30f, 48f);
            Rect headerRect = new Rect(colRect.x + pad, colRect.y + 6f, colRect.width - pad * 2f, headerH);
            DrawRect(headerRect, new Color(themeColor.r * 0.15f, themeColor.g * 0.15f, themeColor.b * 0.15f, 0.6f));
            DrawFittedLabel(headerRect, header, sectionLabelStyle, themeColor, 11);

            float listY = headerRect.yMax + 8f;
            float rowH = Mathf.Clamp(44f * uiScale, 38f, 58f);
            float rowGap = Mathf.Clamp(6f * uiScale, 4f, 10f);

            if (records == null || records.Count == 0)
            {
                Rect emptyRect = new Rect(colRect.x + pad, listY, colRect.width - pad * 2f, 80f);
                DrawRect(emptyRect, new Color(0f, 0f, 0f, 0.4f));
                DrawFittedLabel(
                    emptyRect,
                    fastestColumn
                        ? "COMPLETE A RUN TO REGISTER YOUR FIRST TIME"
                        : "CLEAR ARENAS TO RECORD YOUR DEEPEST RUN",
                    bodyLabelStyle,
                    new Color(0.6f, 0.7f, 0.8f, 0.7f),
                    11);
                return;
            }

            int displayCount = Mathf.Min(records.Count, 8);
            for (int i = 0; i < displayCount; i++)
            {
                RunRecord record = records[i];
                int ry = Mathf.RoundToInt(listY + i * (rowH + rowGap));
                Rect rowRect = new Rect(colRect.x + pad, ry, colRect.width - pad * 2f, rowH);

                bool isRank1 = i == 0;
                bool isRank2 = i == 1;
                bool isRank3 = i == 2;

                Color rankColor = isRank1 ? ACCENT_GOLD : (isRank2 ? new Color(0.85f, 0.88f, 0.95f, 1f) : (isRank3 ? new Color(0.90f, 0.60f, 0.35f, 1f) : new Color(0.3f, 0.5f, 0.7f, 0.8f)));
                Color rowBg = isRank1
                    ? new Color(0.12f, 0.09f, 0.02f, 0.92f)
                    : (isRank2 ? new Color(0.06f, 0.08f, 0.12f, 0.88f) : (isRank3 ? new Color(0.08f, 0.05f, 0.03f, 0.88f) : new Color(0.02f, 0.04f, 0.08f, 0.85f)));

                DrawRect(rowRect, rowBg);
                DrawRect(new Rect(rowRect.x, rowRect.y, 4f, rowRect.height), rankColor);
                DrawRect(new Rect(rowRect.x, rowRect.yMax - 1f, rowRect.width, 1f), new Color(1f, 1f, 1f, 0.08f));

                // Rank badge / medal
                string rankTag = isRank1 ? "1ST" : (isRank2 ? "2ND" : (isRank3 ? "3RD" : $"{i + 1:D2}."));
                float rankW = Mathf.Clamp(46f * uiScale, 38f, 60f);
                Rect rankRect = new Rect(rowRect.x + 8f, rowRect.y, rankW, rowRect.height);
                DrawFittedLabel(rankRect, rankTag, titleBarStyle, rankColor, 12);

                // Arenas cleared badge
                float arenaW = Mathf.Clamp(130f * uiScale, 110f, 160f);
                Rect arenaRect = new Rect(rankRect.xMax + 6f, rowRect.y, arenaW, rowRect.height);
                string arenaStr = $"{record.arenasCleared}/{record.totalArenas} ARENAS";
                DrawFittedLabel(arenaRect, arenaStr, sectionLabelStyle, Color.white, 11);

                // Time Duration
                float timeW = Mathf.Clamp(100f * uiScale, 90f, 130f);
                Rect timeRect = new Rect(arenaRect.xMax + 6f, rowRect.y, timeW, rowRect.height);
                DrawFittedLabel(timeRect, FormatRunTime(record.durationSeconds), bodyLabelStyle, ACCENT_CYAN, 11);

                // Status Badge
                float statusW = Mathf.Clamp(100f * uiScale, 85f, 120f);
                Rect statusRect = new Rect(rowRect.xMax - statusW - 8f, rowRect.center.y - 12f * uiScale, statusW, 24f * uiScale);
                bool won = record.completed;
                Color statusBg = won ? new Color(0.10f, 0.32f, 0.18f, 0.85f) : new Color(0.35f, 0.08f, 0.10f, 0.85f);
                Color statusFg = won ? new Color(0.35f, 0.95f, 0.50f, 1f) : new Color(1f, 0.40f, 0.40f, 1f);
                DrawRect(statusRect, statusBg);
                DrawFittedLabel(statusRect, won ? "VICTORY" : "DEFEATED", bodyLabelStyle, statusFg, 9);
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  SHRINE BLESSINGS COMPENDIUM
        // ══════════════════════════════════════════════════════════════════════════

        private void DrawShrineBlessingsCompendium(Rect area)
        {
            float uiScale = GetUiScale();
            float pad = Mathf.Clamp(12f * uiScale, 10f, 20f);

            DrawPanelFrame(
                area,
                new Color(0.02f, 0.04f, 0.08f, 0.96f),
                new Color(0.04f, 0.08f, 0.14f, 0.98f),
                ACCENT_CYAN,
                2.5f);
            DrawFrameCorners(area, ACCENT_CYAN, 20f, 2f);

            // ── TOP HEADER & META PROGRESS ───────────────────────────
            float headerH = Mathf.Clamp(72f * uiScale, 64f, 96f);
            Rect headerRect = new Rect(area.x + pad, area.y + pad * 0.6f, area.width - pad * 2f, headerH);
            DrawPanelFrame(headerRect, new Color(0.03f, 0.06f, 0.12f, 0.92f), new Color(0.05f, 0.10f, 0.18f, 0.95f), ACCENT_CYAN, 1.5f);

            int unlockedCount = ShrineBlessingsUnlockManager.GetUnlockedCount();
            int totalCount = ShrineBlessingsUnlockManager.GetTotalCount();
            float unlockPct = totalCount > 0 ? (float)unlockedCount / totalCount : 0f;

            // Title & Subtitle
            float titleW = headerRect.width * 0.48f;
            DrawFittedLabel(new Rect(headerRect.x + 14f, headerRect.y + 6f, titleW, headerH * 0.48f), "SHRINE BLESSINGS // COMPENDIUM", titleBarStyle, ACCENT_CYAN, 13);
            DrawFittedLabel(new Rect(headerRect.x + 14f, headerRect.y + headerH * 0.50f, titleW, headerH * 0.42f), "WIN FULL RUNS (9 ARENAS) TO UNLOCK NEW SHRINE BLESSINGS", bodyLabelStyle, new Color(0.65f, 0.85f, 1f, 0.80f), 10);

            // Progress Meter on right
            float progW = headerRect.width * 0.46f;
            Rect progArea = new Rect(headerRect.xMax - progW - 14f, headerRect.y + 6f, progW, headerH - 12f);
            string progStr = $"UNLOCKED: {unlockedCount} / {totalCount} ({unlockPct * 100f:F1}%)";
            DrawFittedLabel(new Rect(progArea.x, progArea.y + 2f, progArea.width, progArea.height * 0.42f), progStr, sectionLabelStyle, ACCENT_GOLD, 11);

            Rect barRect = new Rect(progArea.x, progArea.y + progArea.height * 0.48f, progArea.width, Mathf.Clamp(14f * uiScale, 10f, 18f));
            DrawRect(barRect, new Color(0f, 0f, 0f, 0.6f));
            DrawPanelFrame(barRect, new Color(0f, 0f, 0f, 0.6f), new Color(0f, 0f, 0f, 0.6f), new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.4f), 1f);
            if (unlockPct > 0f)
            {
                Rect fillRect = new Rect(barRect.x + 1f, barRect.y + 1f, (barRect.width - 2f) * Mathf.Clamp01(unlockPct), barRect.height - 2f);
                DrawRect(fillRect, ACCENT_CYAN);
            }

            // ── FILTER TOOLBAR ──────────────────────────────────────────
            float filterY = headerRect.yMax + 8f;
            float filterH = Mathf.Clamp(34f * uiScale, 30f, 44f);
            Rect filterBar = new Rect(area.x + pad, filterY, area.width - pad * 2f, filterH);

            // Rarity Tabs: ALL, COMMON, UNCOMMON, RARE, EPIC, LEGENDARY
            int totalRarities = 6;
            float rTabW = (filterBar.width * 0.60f) / totalRarities;
            string[] rNames = { "ALL", "COMMON", "UNCOMMON", "RARE", "EPIC", "LEGENDARY" };
            Color[] rColors = { ACCENT_CYAN, GetRarityColor(PerkRarity.Common), GetRarityColor(PerkRarity.Uncommon), GetRarityColor(PerkRarity.Rare), GetRarityColor(PerkRarity.Epic), GetRarityColor(PerkRarity.Legendary) };

            for (int r = 0; r < totalRarities; r++)
            {
                int rIndex = r - 1;
                bool isSelected = compendiumRarityFilter == rIndex;
                Rect rRect = new Rect(filterBar.x + r * rTabW, filterBar.y, rTabW - 4f, filterH);

                int rUnl = r == 0 ? unlockedCount : ShrineBlessingsUnlockManager.GetUnlockedCountForRarity((PerkRarity)rIndex);
                int rTot = r == 0 ? totalCount : ShrineBlessingsUnlockManager.GetTotalCountForRarity((PerkRarity)rIndex);
                string tabLabel = $"{rNames[r]} [{rUnl}/{rTot}]";

                Color bg = isSelected ? new Color(rColors[r].r * 0.3f, rColors[r].g * 0.3f, rColors[r].b * 0.3f, 0.9f) : new Color(0.03f, 0.06f, 0.10f, 0.85f);
                DrawPanelFrame(rRect, bg, bg, isSelected ? rColors[r] : new Color(0.15f, 0.25f, 0.35f, 0.6f), isSelected ? 2f : 1f);
                if (GUI.Button(rRect, GUIContent.none, GUIStyle.none))
                {
                    compendiumRarityFilter = rIndex;
                }
                DrawFittedLabel(rRect, tabLabel, sectionLabelStyle, isSelected ? rColors[r] : new Color(0.7f, 0.8f, 0.9f, 0.8f), 9);
            }

            // Category Tabs on right: ALL, COMBAT, MOBILITY, ENERGY, DEFENSE
            float cStartX = filterBar.x + filterBar.width * 0.62f;
            float cAvailW = filterBar.xMax - cStartX;
            int totalCats = 5;
            float cTabW = cAvailW / totalCats;
            string[] cNames = { "ALL", "COMBAT", "MOBILITY", "ENERGY", "DEFENSE" };

            for (int c = 0; c < totalCats; c++)
            {
                int cIndex = c - 1;
                bool isSelected = compendiumCategoryFilter == cIndex;
                Rect cRect = new Rect(cStartX + c * cTabW, filterBar.y, cTabW - 4f, filterH);

                Color bg = isSelected ? new Color(0.08f, 0.22f, 0.32f, 0.9f) : new Color(0.02f, 0.05f, 0.09f, 0.85f);
                DrawPanelFrame(cRect, bg, bg, isSelected ? ACCENT_CYAN : new Color(0.15f, 0.25f, 0.35f, 0.6f), isSelected ? 2f : 1f);
                if (GUI.Button(cRect, GUIContent.none, GUIStyle.none))
                {
                    compendiumCategoryFilter = cIndex;
                }
                DrawFittedLabel(cRect, cNames[c], sectionLabelStyle, isSelected ? ACCENT_CYAN : new Color(0.6f, 0.7f, 0.8f, 0.8f), 9);
            }

            // ── MAIN SCROLLABLE GRID ────────────────────────────────────
            float gridY = filterBar.yMax + 8f;
            Rect gridArea = new Rect(area.x + pad, gridY, area.width - pad * 2f, area.yMax - gridY - pad);
            DrawPanelFrame(gridArea, new Color(0.015f, 0.035f, 0.07f, 0.95f), new Color(0.02f, 0.05f, 0.09f, 0.96f), new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.35f), 1f);

            var filteredList = ShrinePerkCatalog.GetAllPerks().ToList();
            if (compendiumRarityFilter >= 0)
                filteredList = filteredList.Where(p => (int)p.Rarity == compendiumRarityFilter).ToList();
            if (compendiumCategoryFilter >= 0)
                filteredList = filteredList.Where(p => (int)p.Category == compendiumCategoryFilter).ToList();

            float scrollAreaW = gridArea.width - 12f;
            float scrollAreaH = gridArea.height - 8f;
            int cols = Mathf.Max(2, Mathf.FloorToInt((scrollAreaW - 16f) / Mathf.Clamp(260f * uiScale, 220f, 340f)));
            int rows = Mathf.CeilToInt((float)filteredList.Count / cols);
            float cardH = Mathf.Clamp(148f * uiScale, 136f, 175f);
            float gap = Mathf.Clamp(6f * uiScale, 5f, 10f);
            float totalContentH = rows * (cardH + gap) + 12f;

            float innerW = scrollAreaW - 14f;
            float cardW = (innerW - gap * (cols - 1)) / cols;

            Rect viewRect = new Rect(0, 0, innerW, totalContentH);
            compendiumScrollPos = GUI.BeginScrollView(
                new Rect(gridArea.x + 4f, gridArea.y + 4f, scrollAreaW, scrollAreaH),
                compendiumScrollPos,
                viewRect,
                false,
                false,
                GUIStyle.none,
                GUIStyle.none);

            for (int i = 0; i < filteredList.Count; i++)
            {
                int r = i / cols;
                float cy = 6f + r * (cardH + gap);

                // Viewport Culling: Skip offscreen cards for buttery-smooth 144+ FPS scrolling
                if (cy + cardH < compendiumScrollPos.y - 15f || cy > compendiumScrollPos.y + scrollAreaH + 15f)
                    continue;

                int c = i % cols;
                float cx = c * (cardW + gap);
                Rect cardRect = new Rect(cx, cy, cardW, cardH);

                ShrinePerkData perk = filteredList[i];
                DrawCompendiumPerkCard(cardRect, perk, uiScale);
            }

            GUI.EndScrollView();

            // Custom Sleek Vertical Scrollbar Indicator
            if (totalContentH > scrollAreaH)
            {
                Rect scrollTrack = new Rect(gridArea.xMax - 7f, gridArea.y + 6f, 4f, gridArea.height - 12f);
                DrawRect(scrollTrack, new Color(0.04f, 0.08f, 0.15f, 0.85f));

                float maxScroll = totalContentH - scrollAreaH;
                float thumbH = Mathf.Max(26f, (scrollAreaH / totalContentH) * scrollTrack.height);
                float thumbY = scrollTrack.y + Mathf.Clamp01(compendiumScrollPos.y / maxScroll) * (scrollTrack.height - thumbH);
                Rect thumbRect = new Rect(scrollTrack.x - 1f, thumbY, 6f, thumbH);
                DrawRect(thumbRect, ACCENT_CYAN);
                DrawFrameCorners(thumbRect, ACCENT_CYAN, 3f, 1f);
            }
        }

        private void DrawCompendiumPerkCard(Rect card, ShrinePerkData perk, float uiScale)
        {
            bool isUnlocked = ShrineBlessingsUnlockManager.IsUnlocked(perk.Type);
            Color rarityColor = GetRarityColor(perk.Rarity);

            if (isUnlocked)
            {
                // Unlocked Blessing Card
                DrawPanelFrame(card, new Color(0.03f, 0.06f, 0.12f, 0.94f), new Color(0.04f, 0.08f, 0.16f, 0.96f), rarityColor, 2f);
                DrawFrameCorners(card, rarityColor, 8f, 1.5f);

                // Top Accent Line
                DrawRect(new Rect(card.x + 2f, card.y + 2f, card.width - 4f, 3f), rarityColor);

                // Icon Symbol Box (Centered Symbol)
                float iconBoxSize = Mathf.Clamp(34f * uiScale, 30f, 42f);
                Rect iconBox = new Rect(card.x + 8f, card.y + 8f, iconBoxSize, iconBoxSize);
                DrawPanelFrame(iconBox, new Color(rarityColor.r * 0.25f, rarityColor.g * 0.25f, rarityColor.b * 0.25f, 0.90f), new Color(0.02f, 0.04f, 0.08f, 0.95f), rarityColor, 1.5f);

                GUIStyle iconSymbolStyle = CreateStaticStyle(
                    sectionLabelStyle,
                    rarityColor,
                    Mathf.RoundToInt(Mathf.Clamp(iconBoxSize * 0.44f, 11f, 18f)),
                    TextAnchor.MiddleCenter,
                    FontStyle.Bold);
                GUI.Label(iconBox, perk.IconSymbol, iconSymbolStyle);

                // Name & Japanese Name
                float textX = iconBox.xMax + 8f;
                float textW = card.width - iconBoxSize - 96f * uiScale;
                Rect nameRect = new Rect(textX, card.y + 6f, textW, 18f * uiScale);
                DrawFittedLabel(nameRect, perk.Name, sectionLabelStyle, Color.white, 11);

                Rect jpRect = new Rect(textX, nameRect.yMax - 2f, textW, 14f * uiScale);
                DrawFittedLabel(jpRect, perk.JapaneseName, bodyLabelStyle, new Color(0.60f, 0.80f, 1f, 0.75f), 9);

                // Category & Rarity Badge on top right
                float badgeW = Mathf.Clamp(76f * uiScale, 68f, 90f);
                Rect catBadge = new Rect(card.xMax - badgeW - 6f, card.y + 8f, badgeW, 18f * uiScale);
                DrawRect(catBadge, new Color(rarityColor.r * 0.20f, rarityColor.g * 0.20f, rarityColor.b * 0.20f, 0.85f));
                DrawFittedLabel(catBadge, perk.Category.ToString().ToUpperInvariant(), bodyLabelStyle, rarityColor, 8);

                // Description Body (Larger crisp font)
                float descY = iconBox.yMax + 4f;
                float descH = (card.yMax - 22f * uiScale) - descY;
                Rect descRect = new Rect(card.x + 6f, descY, card.width - 12f, Mathf.Max(16f, descH));
                DrawRect(descRect, new Color(0f, 0f, 0f, 0.25f));
                DrawFittedLabel(new Rect(descRect.x + 4f, descRect.y + 2f, descRect.width - 8f, descRect.height - 4f), perk.Description, bodyLabelStyle, new Color(0.90f, 0.95f, 1f, 0.95f), 12);

                // Footer Bar (Cost & Discovered Tag)
                float footerY = card.yMax - 20f * uiScale;
                Rect costRect = new Rect(card.x + 8f, footerY, card.width * 0.50f, 16f * uiScale);
                DrawFittedLabel(costRect, $"COST: {perk.BaseCost} PTS", bodyLabelStyle, ACCENT_GOLD, 9);

                Rect discRect = new Rect(card.xMax - 95f * uiScale - 6f, footerY, 95f * uiScale, 16f * uiScale);
                DrawFittedLabel(discRect, "✓ DISCOVERED", bodyLabelStyle, new Color(0.35f, 0.95f, 0.55f, 0.90f), 9);
            }
            else
            {
                // Locked Blessing Card (Risk of Rain style)
                DrawPanelFrame(card, new Color(0.015f, 0.025f, 0.045f, 0.96f), new Color(0.012f, 0.020f, 0.035f, 0.98f), new Color(0.12f, 0.20f, 0.32f, 0.45f), 1.5f);

                // Top Accent Line (Dimmed)
                DrawRect(new Rect(card.x + 2f, card.y + 2f, card.width - 4f, 2f), new Color(0.12f, 0.20f, 0.30f, 0.50f));

                // Icon Box (Centered ??)
                float iconBoxSize = Mathf.Clamp(34f * uiScale, 30f, 42f);
                Rect iconBox = new Rect(card.x + 8f, card.y + 8f, iconBoxSize, iconBoxSize);
                DrawPanelFrame(iconBox, new Color(0.01f, 0.02f, 0.03f, 0.95f), new Color(0.01f, 0.02f, 0.03f, 0.95f), new Color(0.2f, 0.3f, 0.4f, 0.4f), 1f);

                GUIStyle lockSymbolStyle = CreateStaticStyle(
                    sectionLabelStyle,
                    new Color(0.35f, 0.45f, 0.55f, 0.6f),
                    Mathf.RoundToInt(Mathf.Clamp(iconBoxSize * 0.44f, 11f, 18f)),
                    TextAnchor.MiddleCenter,
                    FontStyle.Bold);
                GUI.Label(iconBox, "??", lockSymbolStyle);

                // Name & Japanese Name (Hidden ?????)
                float textX = iconBox.xMax + 8f;
                float textW = card.width - iconBoxSize - 96f * uiScale;
                Rect nameRect = new Rect(textX, card.y + 6f, textW, 18f * uiScale);
                DrawFittedLabel(nameRect, "? ? ? ? ? ?", sectionLabelStyle, new Color(0.40f, 0.50f, 0.65f, 0.65f), 11);

                Rect jpRect = new Rect(textX, nameRect.yMax - 2f, textW, 14f * uiScale);
                DrawFittedLabel(jpRect, "未知の加護", bodyLabelStyle, new Color(0.25f, 0.35f, 0.45f, 0.5f), 9);

                // Category Tag
                float badgeW = Mathf.Clamp(76f * uiScale, 68f, 90f);
                Rect catBadge = new Rect(card.xMax - badgeW - 6f, card.y + 8f, badgeW, 18f * uiScale);
                DrawRect(catBadge, new Color(0f, 0f, 0f, 0.5f));
                DrawFittedLabel(catBadge, "LOCKED", bodyLabelStyle, new Color(0.45f, 0.55f, 0.65f, 0.6f), 8);

                // Description Body (Sealed ????? marks)
                float descY = iconBox.yMax + 4f;
                float descH = (card.yMax - 22f * uiScale) - descY;
                Rect descRect = new Rect(card.x + 6f, descY, card.width - 12f, Mathf.Max(16f, descH));
                DrawRect(descRect, new Color(0f, 0f, 0f, 0.35f));
                DrawFittedLabel(new Rect(descRect.x + 4f, descRect.y + 2f, descRect.width - 8f, descRect.height - 4f), "? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ?", bodyLabelStyle, new Color(0.30f, 0.40f, 0.50f, 0.60f), 12);

                // Footer Bar
                float footerY = card.yMax - 20f * uiScale;
                Rect costRect = new Rect(card.x + 8f, footerY, card.width * 0.50f, 16f * uiScale);
                DrawFittedLabel(costRect, "COST: ??? PTS", bodyLabelStyle, new Color(0.35f, 0.45f, 0.55f, 0.6f), 9);

                Rect discRect = new Rect(card.xMax - 95f * uiScale - 6f, footerY, 95f * uiScale, 16f * uiScale);
                DrawFittedLabel(discRect, "SEALED", bodyLabelStyle, new Color(0.70f, 0.35f, 0.20f, 0.75f), 9);
            }
        }

        private void DrawRunVictoryUnlocksModal()
        {
            int sw = Mathf.RoundToInt(UiWidth);
            int sh = Mathf.RoundToInt(UiHeight);
            float uiScale = GetUiScale();

            DrawRect(new Rect(0, 0, sw, sh), new Color(0f, 0.02f, 0.05f, 0.88f));

            float modalW = Mathf.Clamp(sw * 0.85f, 780f, 1300f);
            float modalH = Mathf.Clamp(sh * 0.82f, 520f, 850f);
            Rect modal = new Rect((sw - modalW) * 0.5f, (sh - modalH) * 0.5f, modalW, modalH);

            DrawPanelFrame(modal, new Color(0.02f, 0.05f, 0.10f, 0.98f), new Color(0.04f, 0.10f, 0.18f, 0.99f), ACCENT_GOLD, 3.5f);
            DrawFrameCorners(modal, ACCENT_GOLD, 28f, 3f);

            float pad = Mathf.Clamp(16f * uiScale, 12f, 24f);

            // Title & Banner
            float bannerH = Mathf.Clamp(75f * uiScale, 65f, 95f);
            Rect bannerRect = new Rect(modal.x + pad, modal.y + pad, modal.width - pad * 2f, bannerH);
            DrawPanelFrame(bannerRect, new Color(0.12f, 0.09f, 0.02f, 0.92f), new Color(0.18f, 0.14f, 0.03f, 0.95f), ACCENT_GOLD, 2f);

            DrawFittedLabel(new Rect(bannerRect.x, bannerRect.y + 6f, bannerRect.width, bannerH * 0.50f), "★ RUN COMPLETE // GRAND VICTORY! ★", titleBarStyle, ACCENT_GOLD, 16);
            DrawFittedLabel(new Rect(bannerRect.x, bannerRect.y + bannerH * 0.52f, bannerRect.width, bannerH * 0.40f), "ALL 9 ARENAS CLEARED! THE SHRINE SPIRITS HAVE REVEALED NEW BLESSINGS:", sectionLabelStyle, ACCENT_CYAN, 11);

            // Cards container
            float cardsY = bannerRect.yMax + 12f;
            float cardsH = modal.yMax - cardsY - 65f * uiScale;
            Rect cardsArea = new Rect(modal.x + pad, cardsY, modal.width - pad * 2f, cardsH);
            DrawPanelFrame(cardsArea, new Color(0.015f, 0.03f, 0.06f, 0.90f), new Color(0.02f, 0.05f, 0.09f, 0.95f), new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.4f), 1f);

            int count = lastRunUnlockedBlessings.Count;
            int cols = Mathf.Min(count, 3);
            int rows = Mathf.CeilToInt((float)count / cols);
            float cardW = (cardsArea.width - 24f - 12f * (cols - 1)) / cols;
            float cardH = (cardsArea.height - 24f - 12f * (rows - 1)) / rows;

            for (int i = 0; i < count; i++)
            {
                int r = i / cols;
                int c = i % cols;
                float cx = cardsArea.x + 12f + c * (cardW + 12f);
                float cy = cardsArea.y + 12f + r * (cardH + 12f);
                Rect cRect = new Rect(cx, cy, cardW, cardH);

                ShrinePerkData perk = ShrinePerkCatalog.GetPerk(lastRunUnlockedBlessings[i]);
                if (perk != null)
                {
                    DrawCompendiumPerkCard(cRect, perk, uiScale);
                }
            }

            // Claim button
            float btnW = Mathf.Clamp(300f * uiScale, 240f, 400f);
            float btnH = Mathf.Clamp(46f * uiScale, 40f, 60f);
            Rect claimRect = new Rect(modal.center.x - btnW * 0.5f, modal.yMax - btnH - pad * 0.8f, btnW, btnH);
            if (ActionBtn("CLAIM & CONTINUE // 承知", claimRect, ACCENT_GOLD, false))
            {
                showRunVictoryModal = false;
            }
        }

        private void DrawPreviewAndStats(Rect area, PlayerManager runPlayer)
        {
            float uiScale = GetUiScale();
            float innerPad = Mathf.Clamp(10f * uiScale, 10f, 24f);
            float frame = Mathf.Max(3f, Mathf.Clamp(UiWidth * 0.0028f, 3f, 7f));
            DrawPanelFrame(area, new Color(0.05f, 0.06f, 0.10f, 0.92f), new Color(0.09f, 0.06f, 0.02f, 0.96f), ACCENT_YEL, frame);
            DrawHorizontalGradient(new Rect(area.x, area.y + area.height * 0.72f, area.width, area.height * 0.28f), new Color(0f, 0f, 0f, 0f), new Color(1f, 0.64f, 0.06f, 0.12f), 18);

            // Preview label bar (black bg, yellow text)
            float barH = Mathf.Clamp(34f * uiScale, 30f, 58f);
            DrawHorizontalGradient(new Rect(area.x, area.y, area.width, barH), new Color(0f, 0f, 0f, 0.95f), new Color(0.18f, 0.08f, 0.02f, 0.95f), 12);
            GUILayout.BeginArea(new Rect(area.x + innerPad, area.y + 2f, area.width - innerPad * 2f, barH - 4f));
            GUILayout.Label("BEY PREVIEW", sectionLabelStyle);
            GUILayout.EndArea();

            BeyStatBlock stats = GetStatsForDisplay(runPlayer);
            float spin = stats != null ? GetCurrentSpinForDisplay(runPlayer) : 0f;
            float mana = stats != null ? GetCurrentManaForDisplay(runPlayer) : 0f;
            float maxMana =
                stats != null ? GetMaxManaForDisplay(runPlayer) : 0f;

            // Preview row
            float prevTexH = area.height * 0.50f;
            float previewRowY = area.y + barH + 6f;
            float previewRowH = prevTexH - barH - 10f;
            float previewGap = Mathf.Clamp(10f * uiScale, 10f, 18f);
            bool showOverallCard = stats != null && area.width >= 620f;
            float cardWidth = showOverallCard ? Mathf.Clamp(area.width * 0.34f, 180f, 300f) : 0f;
            float texWidth = area.width - innerPad * 1.4f - (showOverallCard ? cardWidth + previewGap : 0f);
            Rect texRect = new Rect(area.x + innerPad * 0.7f, previewRowY, texWidth, previewRowH);
            DrawRect(new Rect(texRect.x - 2f, texRect.y - 2f, texRect.width + 4f, texRect.height + 4f), new Color(0f, 0f, 0f, 0.82f));
            DrawVerticalGradient(texRect, new Color(0.02f, 0.04f, 0.08f, 0.88f), new Color(0.08f, 0.05f, 0.02f, 0.64f), 10);
            DrawRect(new Rect(texRect.x, texRect.y, texRect.width, 2f), new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.70f));
            if (previewTexture != null)
            {
                GUI.DrawTexture(texRect, previewTexture, ScaleMode.ScaleToFit, true);
                HandlePreviewDragInput(texRect);
            }

            if (showOverallCard)
            {
                Rect overallRect = new Rect(texRect.xMax + previewGap, previewRowY, cardWidth, previewRowH);
                DrawOverallStatsCard(
                    overallRect,
                    stats,
                    spin,
                    mana,
                    maxMana);
            }

            // Stats section — yellow bar separator + black header
            float statsY = area.y + prevTexH;
            DrawHorizontalGradient(new Rect(area.x, statsY, area.width, 4f), ACCENT_YEL, ACCENT_ORANGE, 18);
            DrawHorizontalGradient(new Rect(area.x, statsY + 4f, area.width, barH), new Color(0f, 0f, 0f, 0.95f), new Color(0.16f, 0.06f, 0.01f, 0.95f), 12);
            GUILayout.BeginArea(new Rect(area.x + innerPad, statsY + 6f, area.width - innerPad * 2f, barH - 4f));
            GUILayout.Label("STATS / PART DATA", sectionLabelStyle);
            GUILayout.EndArea();
            DrawRect(new Rect(area.x, statsY + 4f + barH, area.width, 3f), Color.black);

            if (stats != null)
            {
                float rowY  = statsY + 4f + barH + 6f;
                float rowH  = area.yMax - rowY - 8f;
                GUIStyle fittedStatStyle = FitLabelStyle(statRowStyle, $"MANA     {mana:0.0} / {maxMana:0.0}", area.width - innerPad * 2f, 10);
                BeyPart selectedPart = GetFocusedInventoryPart();
                bool showPart = selectedPart != null;
                float fullW = area.width - innerPad * 2f;
                float leftW = showPart ? Mathf.Max(180f, fullW * 0.44f) : fullW;
                float rightX = area.x + innerPad + leftW + (showPart ? Mathf.Clamp(10f * uiScale, 10f, 18f) : 0f);
                float rightW = showPart ? area.x + area.width - innerPad - rightX : 0f;

                GUILayout.BeginArea(new Rect(area.x + innerPad, rowY, leftW, rowH));
                GUILayout.Label($"SPIN     {spin:0.0} / {GameConstants.MAX_SPIN:0}",   fittedStatStyle);
                GUILayout.Label($"MANA     {mana:0.0} / {maxMana:0.0}",                fittedStatStyle);
                GUILayout.Label($"ATK / DEF {stats.Attack:0} / {stats.Defense:0}",      fittedStatStyle);
                GUILayout.Label($"RETAIN   {stats.SpinRetention:0}",                   fittedStatStyle);
                GUILayout.Label($"WEIGHT   {stats.Weight:0.0}",                        fittedStatStyle);
                GUILayout.Label($"TIP      {stats.TipBehavior.ToString().ToUpper()}",  fittedStatStyle);
                GUILayout.Label($"DRAIN    {stats.TotalStaminaDrainRate:0.00}",        fittedStatStyle);
                GUILayout.Label($"REGEN    {stats.ManaRegenRate:0.0}",                 fittedStatStyle);
                GUILayout.EndArea();

                if (showPart && rightW > 120f)
                {
                    GUILayout.BeginArea(new Rect(rightX, rowY, rightW, rowH));
                    DrawSelectedPartCard(selectedPart, "SELECTED PART", false);
                    GUILayout.EndArea();
                }
            }
        }

        private void DrawOverallStatsCard(
            Rect area,
            BeyStatBlock stats,
            float spin,
            float mana,
            float maxMana)
        {
            if (stats == null)
                return;

            DrawPanelFrame(area, new Color(0.03f, 0.05f, 0.10f, 0.82f), new Color(0.10f, 0.05f, 0.03f, 0.88f), ACCENT_CYAN, 2f);
            DrawHorizontalGradient(new Rect(area.x, area.y + area.height * 0.68f, area.width, area.height * 0.32f), new Color(0f, 0f, 0f, 0f), new Color(1f, 0.58f, 0.12f, 0.10f), 10);

            float pad = Mathf.Clamp(10f * GetUiScale(), 10f, 18f);
            Rect content = new Rect(area.x + pad, area.y + pad * 0.75f, area.width - pad * 2f, area.height - pad * 1.4f);
            GUILayout.BeginArea(content);

            GUIStyle headerStyle = FitLabelStyle(sectionLabelStyle, "OVERALL STATS", content.width, 10);
            GUIStyle statStyle = FitLabelStyle(statRowStyle, "ABILITY  LEGENDARY BURST", content.width, 10);
            GUIStyle detailStyle = FitLabelStyle(bodyLabelStyle, "CURRENT BUILD OVERVIEW", content.width, 10);

            GUILayout.Label("OVERALL STATS", headerStyle);
            GUILayout.Space(4f);
            GUILayout.Label($"SPIN     {spin:0.0} / {GameConstants.MAX_SPIN:0}", statStyle);
            GUILayout.Label($"MANA     {mana:0.0} / {maxMana:0.0}", statStyle);
            GUILayout.Label($"ATK / DEF {stats.Attack:0} / {stats.Defense:0}", statStyle);
            GUILayout.Label($"RETAIN   {stats.SpinRetention:0}", statStyle);
            GUILayout.Label($"WEIGHT   {stats.Weight:0.0}", statStyle);
            GUILayout.Label($"TIP      {stats.TipBehavior.ToString().ToUpper()}", statStyle);
            GUILayout.Label($"HEIGHT   {stats.TrackHeight:0.00}", statStyle);
            GUILayout.Label($"JUMP ARC {stats.JumpArcModifier:0.00}", statStyle);
            GUILayout.Label($"DRAIN    {stats.TotalStaminaDrainRate:0.00}", statStyle);
            GUILayout.Label($"REGEN    {stats.ManaRegenRate:0.0}", statStyle);

            GUILayout.Space(6f);
            GUILayout.Label("ACTIVE ABILITY", headerStyle);
            if (stats.EquippedAbility == null)
            {
                GUILayout.Label("NONE", detailStyle);
            }
            else
            {
                GUILayout.Label(stats.EquippedAbility.AbilityName.ToUpperInvariant(), statStyle);
                GUILayout.Label($"COST     {stats.EquippedAbility.ManaCost:0.#}", statStyle);
                GUILayout.Label(stats.EquippedAbility.Rarity.ToString().ToUpperInvariant(), detailStyle);
            }

            GUILayout.EndArea();
        }

        private BeyPart GetFocusedInventoryPart()
        {
            bool mainInventoryOpen = rootState == RootUiState.MainMenu && mainMenuPanel == MenuPanel.Inventory;
            bool runInventoryOpen = (rootState == RootUiState.Paused || rootState == RootUiState.BetweenArenas) && pausePanel == MenuPanel.Inventory;
            return (mainInventoryOpen || runInventoryOpen) ? selectedInventoryPart : null;
        }

        private void DrawSelectedPartCard(BeyPart part, string header, bool drawBackground)
        {
            if (drawBackground)
                GUILayout.BeginVertical(listItemStyle);

            if (part == null)
            {
                GUILayout.Label("SELECT A PART TO VIEW ITS STATS AND ABILITY.", bodyLabelStyle);
                if (drawBackground)
                    GUILayout.EndVertical();
                return;
            }

            GUIStyle headerStyle = FitLabelStyle(sectionLabelStyle, header, 420f, 10);
            GUIStyle detailStyle = FitLabelStyle(bodyLabelStyle, "ABILITY RARITY  LEGENDARY", 420f, 10);
            GUIStyle statStyle = FitLabelStyle(statRowStyle, "MANA REGEN  100.0", 420f, 10);

            GUILayout.Label(header, headerStyle);
            GUILayout.Space(4f);
            GUILayout.Label(PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant(), statStyle);
            GUILayout.Label($"TYPE      {part.PartType.ToString().ToUpper()}", statStyle);
            GUIStyle rarityRowStyle = new GUIStyle(statStyle) { normal = { textColor = GetRarityColor(part.Rarity) } };
            GUILayout.Label($"RARITY    {part.Rarity.ToString().ToUpper()}", rarityRowStyle);

            List<string> partLines = BuildPartDetailLines(part);
            for (int i = 0; i < partLines.Count; i++)
                GUILayout.Label(partLines[i], statStyle);

            Dictionary<PartType, BeyPart> currentLoadout = hasActiveRun && runContext.Player != null
                ? GetCurrentRunLoadout(runContext.Player)
                : selectedMainMenuLoadout;

            if (currentLoadout != null && currentLoadout.TryGetValue(part.PartType, out BeyPart equipped) && equipped != null && equipped != part)
            {
                GUILayout.Space(6f);
                GUILayout.Label($"EQUIPPED COMPARISON ({PartDisplayNameFormatter.ToShortDisplayName(equipped).ToUpperInvariant()})", headerStyle);
                float diffScore = GetPartPowerScore(part) - GetPartPowerScore(equipped);
                string scoreDiffStr = diffScore >= 0 ? $"+{diffScore:0}" : $"{diffScore:0}";
                GUILayout.Label($"POWER SCORE   {GetPartPowerScore(part):0} ({scoreDiffStr})", statStyle);
                float diffWeight = part.Weight - equipped.Weight;
                string weightDiffStr = diffWeight >= 0 ? $"+{diffWeight:0.0}" : $"{diffWeight:0.0}";
                GUILayout.Label($"WEIGHT        {part.Weight:0.0}g ({weightDiffStr}g)", statStyle);
            }

            if (part.PartType == PartType.EnergyRing)
            {
                BeyPassive passive =
                    EnergyRingPassiveResolver.Resolve(part);
                GUILayout.Space(6f);
                GUILayout.Label("PASSIVE", headerStyle);
                if (passive == null)
                {
                    GUILayout.Label("NONE", detailStyle);
                }
                else
                {
                    GUILayout.Label(
                        passive.PassiveName.ToUpperInvariant(),
                        statStyle);
                    GUILayout.Label(
                        $"PASSIVE RARITY  {passive.Rarity.ToString().ToUpperInvariant()}",
                        statStyle);
                    GUILayout.Label(
                        passive.Description.ToUpperInvariant(),
                        detailStyle);
                }
            }
            else if (part.PartType == PartType.FaceBolt)
            {
                BeyAbility ability = ResolveAbilityForPart(part);
                GUILayout.Space(6f);
                GUILayout.Label("ABILITY", headerStyle);
                if (ability == null)
                {
                    GUILayout.Label("NONE", detailStyle);
                }
                else
                {
                    GUILayout.Label(
                        ability.AbilityName.ToUpperInvariant(),
                        statStyle);
                    GUILayout.Label(
                        $"ABILITY RARITY  {ability.Rarity.ToString().ToUpper()}",
                        statStyle);
                    GUILayout.Label(
                        $"MANA COST       {ability.ManaCost:0.#}",
                        statStyle);
                    if (!string.IsNullOrWhiteSpace(
                            ability.Description))
                    {
                        GUILayout.Label(
                            ability.Description.ToUpperInvariant(),
                            detailStyle);
                    }
                }
            }

            if (drawBackground)
                GUILayout.EndVertical();
        }

        private void DrawSelectedPartCardInRect(
            Rect area,
            BeyPart part,
            string header)
        {
            DrawPanelFrame(
                area,
                new Color(0.015f, 0.035f, 0.065f, 0.96f),
                new Color(0.04f, 0.08f, 0.13f, 0.97f),
                ACCENT_CYAN,
                2f);
            float pad = Mathf.Clamp(12f * GetUiScale(), 10f, 18f);
            float btnH = Mathf.Clamp(44f * GetUiScale(), 38f, 56f);
            bool canEquip = part != null;

            Rect content = new Rect(
                area.x + pad,
                area.y + pad,
                Mathf.Max(1f, area.width - pad * 2f),
                Mathf.Max(1f, area.height - pad * 2f - (canEquip ? btnH + 10f : 0f)));
            GUILayout.BeginArea(content);
            DrawSelectedPartCard(part, header, false);
            GUILayout.EndArea();

            if (canEquip)
            {
                Dictionary<PartType, BeyPart> currentLoadout = hasActiveRun && runContext.Player != null
                    ? GetCurrentRunLoadout(runContext.Player)
                    : selectedMainMenuLoadout;

                currentLoadout.TryGetValue(part.PartType, out BeyPart equipped);
                bool isEquipped = equipped == part;

                Rect equipBtnRect = new Rect(area.x + pad, area.yMax - pad - btnH, area.width - pad * 2f, btnH);
                if (isEquipped)
                {
                    DrawRect(equipBtnRect, new Color(0.10f, 0.32f, 0.20f, 0.92f));
                    DrawRect(new Rect(equipBtnRect.x, equipBtnRect.yMax - 2f, equipBtnRect.width, 2f), new Color(0.25f, 0.95f, 0.45f, 1f));
                    DrawFittedLabel(equipBtnRect, "CURRENTLY EQUIPPED", bodyLabelStyle, new Color(0.4f, 1f, 0.55f, 1f), 11);
                }
                else
                {
                    if (ActionBtn("EQUIP PART TO CURRENT BUILD", equipBtnRect, ACCENT_CYAN, false))
                    {
                        if (hasActiveRun && runContext.Player != null)
                        {
                            runContext.Player.EquipPart(part);
                            RefreshPreviewFromLoadout(GetCurrentRunLoadout(runContext.Player));
                        }
                        else
                        {
                            selectedMainMenuLoadout[part.PartType] = part;
                            RefreshPreviewFromLoadout(selectedMainMenuLoadout);
                            AutoSave();
                        }
                    }
                }
            }
        }

        private BeyPart FindFaceBoltForAbility(BladeSpinners.Abilities.BeyAbility ability)
        {
            if (ability == null) return null;
            if (ownedParts != null)
            {
                for (int i = 0; i < ownedParts.Count; i++)
                {
                    BeyPart p = ownedParts[i];
                    if (p != null && p.PartType == PartType.FaceBolt)
                    {
                        var res = BladeSpinners.Abilities.FaceBoltAbilityResolver.Resolve(p);
                        if (res != null && (res.GetType() == ability.GetType() || res.AbilityName == ability.AbilityName))
                            return p;
                    }
                }
            }
            if (enemyParts != null)
            {
                for (int i = 0; i < enemyParts.Count; i++)
                {
                    BeyPart p = enemyParts[i];
                    if (p != null && p.PartType == PartType.FaceBolt)
                    {
                        var res = BladeSpinners.Abilities.FaceBoltAbilityResolver.Resolve(p);
                        if (res != null && (res.GetType() == ability.GetType() || res.AbilityName == ability.AbilityName))
                            return p;
                    }
                }
            }
            StarterPartsConfig starterCfg = LoadStarterConfig();
            if (starterCfg != null)
            {
                var starterOwned = starterCfg.GetOwnedStarterParts();
                if (starterOwned != null)
                {
                    for (int i = 0; i < starterOwned.Count; i++)
                    {
                        BeyPart p = starterOwned[i];
                        if (p != null && p.PartType == PartType.FaceBolt)
                        {
                            var res = BladeSpinners.Abilities.FaceBoltAbilityResolver.Resolve(p);
                            if (res != null && (res.GetType() == ability.GetType() || res.AbilityName == ability.AbilityName))
                                return p;
                        }
                    }
                }
            }
            return null;
        }

        private static BeyAbility ResolveAbilityForPart(BeyPart part)
        {
            if (part == null || part.PartType != PartType.FaceBolt)
                return null;

            return part.EquippedAbility != null ? part.EquippedAbility : FaceBoltAbilityResolver.Resolve(part);
        }

        private static List<string> BuildPartDetailLines(BeyPart part)
        {
            List<string> lines = new List<string>();
            if (part == null)
                return lines;

            switch (part.PartType)
            {
                case PartType.Tip:
                    lines.Add($"TIP       {part.TipBehavior.ToString().ToUpper()}");
                    lines.Add($"RETENTION {BeyCombatStatCalculator.GetTipSpinRetention(part):0}");
                    lines.Add($"DRAIN MOD {part.BehaviorBasedStaminaDrainModifier:0.00}");
                    lines.Add($"UPHILL    {part.UphillResistanceMultiplier:0.00}");
                    lines.Add($"SLOPE     {part.SlopeMultiplier:0.00}");
                    break;

                case PartType.Track:
                    lines.Add($"HEIGHT    {part.TrackHeight:0.00}");
                    lines.Add($"JUMP ARC  {part.JumpArcModifier:0.00}");
                    break;

                case PartType.FusionWheel:
                    FusionWheelCombatProfile profile =
                        FusionWheelCombatProfile.FromPart(part);
                    lines.Add($"ATTACK    {profile.Attack:0}");
                    lines.Add($"DEFENSE   {profile.Defense:0}");
                    lines.Add($"CONTACT   {profile.ContactStyle} / {profile.ShapeDescription}");
                    lines.Add($"WEIGHT    {part.Weight:0.0}");
                    lines.Add($"RETENTION {profile.SpinRetention:0}");
                    lines.Add($"MASS DRAIN {part.MassBasedStaminaDrainRate:0.00}");
                    break;

                case PartType.EnergyRing:
                    lines.Add($"MANA POOL {part.ManaPoolSize:0.0}");
                    lines.Add($"MANA REGEN {part.ManaRegenRate:0.0}");
                    BeyPassive passive =
                        EnergyRingPassiveResolver.Resolve(part);
                    lines.Add(
                        $"PASSIVE   {(passive != null ? passive.PassiveName.ToUpperInvariant() : "NONE")}");
                    break;

                case PartType.FaceBolt:
                    break;
            }

            if (!string.IsNullOrWhiteSpace(part.Description))
                lines.Add(part.Description.ToUpperInvariant());

            return lines;
        }

        private void DrawSettingsPanel()
        {
            settingsScroll = GUILayout.BeginScrollView(settingsScroll, false, false, GUIStyle.none, GUIStyle.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            GUILayout.Label("SETTINGS", sectionLabelStyle);
            GUILayout.Space(6);
            GUILayout.Label("AUDIO", sectionLabelStyle);
            GUILayout.Space(2);

            float nextMaster = DrawThemedSlider(
                "MASTER VOLUME",
                settingsMasterVolume,
                0f,
                1f);
            GUILayout.Space(4);
            float nextSoundEffects = DrawThemedSlider(
                "SOUND EFFECTS VOLUME",
                settingsSoundEffectsVolume,
                0f,
                1f);
            GUILayout.Space(4);
            float nextMusic = DrawThemedSlider(
                "MUSIC VOLUME",
                settingsMusicVolume,
                0f,
                1f);
            GUILayout.Space(4);
            float nextGui = DrawThemedSlider(
                "GUI VOLUME",
                settingsGuiVolume,
                0f,
                1f);
            if (!Mathf.Approximately(
                    nextMaster,
                    settingsMasterVolume)
                || !Mathf.Approximately(
                    nextSoundEffects,
                    settingsSoundEffectsVolume)
                || !Mathf.Approximately(
                    nextMusic,
                    settingsMusicVolume)
                || !Mathf.Approximately(
                    nextGui,
                    settingsGuiVolume))
            {
                settingsMasterVolume = nextMaster;
                settingsSoundEffectsVolume = nextSoundEffects;
                settingsMusicVolume = nextMusic;
                settingsGuiVolume = nextGui;
                ApplyAudioSettings(true);
            }

            GUILayout.Space(10);
            GUILayout.Label("GAMEPLAY", sectionLabelStyle);
            GUILayout.Space(2);
            settingsSensitivity = DrawThemedSlider("CAM SENSITIVITY", settingsSensitivity, 0.25f, 2f);
            GUILayout.Space(4);
            float newRingsOpacity = DrawThemedSlider("RINGS UI OPACITY", settingsRingsOpacity, 0f, 1f);
            if (!Mathf.Approximately(newRingsOpacity, settingsRingsOpacity))
            {
                settingsRingsOpacity = newRingsOpacity;
                ApplySettingsToPlayer(runContext.Player);
            }
            GUILayout.Space(4);
            float newOpacity = DrawThemedSlider("CLIPPING OPACITY", settingsClippingOpacity, 0.1f, 0.6f);
            if (!Mathf.Approximately(newOpacity, settingsClippingOpacity))
            {
                settingsClippingOpacity = newOpacity;
                ApplySettingsToCameraController(runContext.CameraController);
            }

            GUILayout.Space(10);
            GUILayout.Label("DISPLAY & RESOLUTION", sectionLabelStyle);
            GUILayout.Space(4);

            string windowModeText = Screen.fullScreen
                ? "WINDOW MODE: FULLSCREEN  [CLICK TO SWITCH TO WINDOWED]"
                : "WINDOW MODE: WINDOWED  [CLICK TO SWITCH TO FULLSCREEN]";
            if (GUILayout.Button(windowModeText, inlineActionButtonStyle, GUILayout.Height(36)))
            {
                Screen.fullScreen = !Screen.fullScreen;
            }

            GUILayout.Space(8);

            // ── Resolution dropdown ──────────────────────────────────────────────
            // Determine current display label
            int currentDispIdx = confirmedResolutionIndex >= 0 ? confirmedResolutionIndex : -1;
            string currentLabel = currentDispIdx >= 0
                ? ResolutionPresets[currentDispIdx].label
                : $"CURRENT  —  {Screen.width} x {Screen.height}";
            if (pendingResolutionIndex >= 0)
                currentLabel = ResolutionPresets[pendingResolutionIndex].label + $"  (REVERTS IN {Mathf.CeilToInt(revertResolutionTimer)}s)";

            // Dropdown trigger button
            Color dropdownColor = pendingResolutionIndex >= 0 ? ACCENT_GOLD : ACCENT_CYAN;
            Rect dropdownBtnRect = GUILayoutUtility.GetRect(0f, 36f, GUILayout.ExpandWidth(true));
            if (ActionBtn(currentLabel + "  [ CHANGE RESOLUTION ]", dropdownBtnRect, dropdownColor, resolutionDropdownOpen))
            {
                resolutionDropdownOpen = !resolutionDropdownOpen;
            }

            // Dropdown list
            if (resolutionDropdownOpen)
            {
                GUILayout.Space(2);
                for (int ri = 0; ri < ResolutionPresets.Length; ri++)
                {
                    int rIdx = ri; // capture
                    var preset = ResolutionPresets[rIdx];
                    bool isSelected = rIdx == (pendingResolutionIndex >= 0 ? pendingResolutionIndex : confirmedResolutionIndex);
                    Color itemColor = isSelected ? ACCENT_GOLD : new Color(0.7f, 0.85f, 1f, 1f);
                    Rect itemRect = GUILayoutUtility.GetRect(0f, 32f, GUILayout.ExpandWidth(true));
                    DrawRect(itemRect, isSelected ? new Color(0.12f, 0.18f, 0.28f, 0.95f) : new Color(0.04f, 0.08f, 0.14f, 0.9f));
                    DrawRect(new Rect(itemRect.x, itemRect.yMax - 1f, itemRect.width, 1f), new Color(1f, 1f, 1f, 0.06f));
                    DrawFittedLabel(new Rect(itemRect.x + 14f, itemRect.y, itemRect.width - 28f, itemRect.height),
                        preset.label, bodyLabelStyle, itemColor, 12);
                    if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
                    {
                        // Apply preview
                        prevResW = Screen.width;
                        prevResH = Screen.height;
                        Screen.SetResolution(preset.w, preset.h, Screen.fullScreenMode);
                        pendingResolutionIndex = rIdx;
                        revertResolutionTimer = RevertResolutionSeconds;
                        resolutionDropdownOpen = false;
                        Event.current.Use();
                    }
                }
                GUILayout.Space(4);
            }

            // Confirm / revert bar (shows only while there's a pending resolution)
            if (pendingResolutionIndex >= 0)
            {
                GUILayout.Space(4);
                float barH = 38f;
                Rect barRect = GUILayoutUtility.GetRect(0f, barH, GUILayout.ExpandWidth(true));
                float half = (barRect.width - 12f) * 0.5f;
                Rect confRect = new Rect(barRect.x, barRect.y, half, barH);
                Rect revertRect = new Rect(barRect.x + half + 12f, barRect.y, half, barH);

                if (ActionBtn($"CONFIRM RESOLUTION ({ResolutionPresets[pendingResolutionIndex].label})", confRect, new Color(0.2f, 0.9f, 0.45f, 1f), false))
                {
                    confirmedResolutionIndex = pendingResolutionIndex;
                    pendingResolutionIndex = -1;
                    revertResolutionTimer = 0f;
                }
                if (ActionBtn($"REVERT  (REVERTS IN {Mathf.CeilToInt(revertResolutionTimer)}s)", revertRect, ACCENT_RED, false))
                {
                    Screen.SetResolution(prevResW, prevResH, Screen.fullScreenMode);
                    pendingResolutionIndex = -1;
                    revertResolutionTimer = 0f;
                }
            }


            GUILayout.Space(12);
            GUILayout.Label("KEYBINDS", sectionLabelStyle);
            GUILayout.Space(4);
            DrawKeybindPanel();

            GUILayout.Space(22);
            GUILayout.Label("DATA MANAGEMENT // DANGER ZONE", sectionLabelStyle);
            GUILayout.Space(4);
            GUILayout.Label("Wipe all local save files, unlocked shrine blessings, and tournament records to return profile to fresh starter state.", bodyLabelStyle);
            GUILayout.Space(8);

            Rect dangerBtnRect = GUILayoutUtility.GetRect(0f, 44f, GUILayout.ExpandWidth(true));
            if (!showResetConfirm)
            {
                if (ActionBtn("RESET ALL PROGRESS & SAVE DATA", dangerBtnRect, ACCENT_RED, false))
                {
                    showResetConfirm = true;
                }
            }
            else
            {
                float btnHalf = (dangerBtnRect.width - 12f) * 0.5f;
                Rect confirmRect = new Rect(dangerBtnRect.x, dangerBtnRect.y, btnHalf, dangerBtnRect.height);
                Rect cancelRect = new Rect(dangerBtnRect.x + btnHalf + 12f, dangerBtnRect.y, btnHalf, dangerBtnRect.height);

                if (ActionBtn("CONFIRM WIPE ALL PROGRESS", confirmRect, Color.red, true))
                {
                    ExecuteFullReset();
                    showResetConfirm = false;
                }
                if (ActionBtn("CANCEL", cancelRect, ACCENT_CYAN, false))
                {
                    showResetConfirm = false;
                }
            }

            GUILayout.Space(16);
            GUILayout.EndScrollView();
        }

        private void ExecuteFullReset()
        {
            // 1. Delete EVERYTHING in the persistent data folder
            string dataDir = Application.persistentDataPath;
            if (System.IO.Directory.Exists(dataDir))
            {
                foreach (string file in System.IO.Directory.GetFiles(dataDir, "*", System.IO.SearchOption.AllDirectories))
                {
                    try { System.IO.File.Delete(file); }
                    catch (System.Exception e) { Debug.LogWarning($"[Reset] Could not delete {file}: {e.Message}"); }
                }
            }

            // 2. Clear all PlayerPrefs
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            // 3. Reset shrine blessings to starter pool
            ShrineBlessingsUnlockManager.ResetToStarterPool();

            // 4. Reset arcade high scores
            MinigameArcadeManager.ResetAllHighScores();

            // 5. Reset resolution dropdown state
            pendingResolutionIndex = -1;
            confirmedResolutionIndex = -1;
            revertResolutionTimer = 0f;

            // 5. Rebuild starter inventory & loadout
            BuildStarterData();
            BuildDefaultLoadout();
            RefreshPreviewFromLoadout(selectedMainMenuLoadout);
            SetMainMenuPanel(MenuPanel.Home);

            Debug.Log("[RuntimeGameUiController] Full player progress reset executed — persistent data folder wiped.");
        }


        private void DrawKeybindPanel()
        {
            float totalW = UiWidth * 0.48f;
            float col1W = Mathf.Round(totalW * 0.25f);
            float col2W = Mathf.Round(totalW * 0.25f);
            float col3W = Mathf.Round(totalW * 0.25f);
            float col4W = Mathf.Max(60f, totalW - col1W - col2W - col3W);

            // Table Header Bar
            GUILayout.BeginHorizontal();
            GUIStyle thStyle1 = CreateStaticStyle(sectionLabelStyle, ACCENT_GOLD, 11, TextAnchor.MiddleLeft, FontStyle.Bold);
            GUIStyle thStyle2 = CreateStaticStyle(sectionLabelStyle, ACCENT_CYAN, 11, TextAnchor.MiddleLeft, FontStyle.Bold);
            GUIStyle thStyle3 = CreateStaticStyle(sectionLabelStyle, new Color(0.25f, 0.90f, 0.35f, 1f), 11, TextAnchor.MiddleLeft, FontStyle.Bold);
            GUIStyle thStyle4 = CreateStaticStyle(sectionLabelStyle, new Color(0.35f, 0.65f, 1f, 1f), 11, TextAnchor.MiddleLeft, FontStyle.Bold);

            GUILayout.Label("ACTION", thStyle1, GUILayout.Width(col1W));
            GUILayout.Label("KEYBOARD & MOUSE", thStyle2, GUILayout.Width(col2W));
            GUILayout.Label("SYMMETRIC CONTROLLER (L/R-OFFSET)", thStyle3, GUILayout.Width(col3W));
            GUILayout.Label("ASYMMETRIC CONTROLLER (SOUTH-FACE)", thStyle4, GUILayout.Width(col4W));
            GUILayout.EndHorizontal();

            GUILayout.Space(3f);

            var keybinds = new (string action, string kbm, string symCtrl, string asymCtrl)[]
            {
                // ── In-game ──
                ("MOVEMENT",         "W / A / S / D",        "Left Stick / D-Pad",       "Left Stick / D-Pad"),
                ("BOOST ACCEL",      "LEFT SHIFT",            "Right Trigger",             "Right Trigger"),
                ("JUMP HOP",         "SPACEBAR",              "Bottom Face Button",        "Bottom Face Button"),
                ("SPECIAL ABILITY",  "E KEY",                 "Top Face Button",           "Top Face Button"),
                ("LOCK-ON TARGET",   "MIDDLE MOUSE",          "Right Stick Click",         "Right Stick Click"),
                ("CYCLE TARGET",     "SCROLL WHEEL",          "D-Pad Left / Right",        "D-Pad Left / Right"),
                ("CLASH DUEL",       "LEFT CLICK (MASH)",     "Bottom Face / Trigger",     "Bottom Face / Trigger"),
                ("LAUNCH RIP-CORD",  "HOLD LEFT CLICK",       "HOLD Bottom Face / Trigger","HOLD Bottom Face / Trigger"),
                ("BRAKE / DRIFT",    "C KEY",                 "Left Trigger",              "Left Trigger"),
                ("CAMERA LOOK",      "MOUSE MOVE",            "Right Stick",               "Right Stick"),
                ("PAUSE / MENU",     "ESC",                   "Start / Menu",              "Start / Options"),
                ("FULLSCREEN",       "F11 / ALT + ENTER",     "—",                         "—"),
                // ── Menu navigation ──
                ("NAVIGATE MENU",    "MOUSE",                 "Left Stick / D-Pad",        "Left Stick / D-Pad"),
                ("SELECT / CONFIRM", "LEFT CLICK",            "Bottom Face Button",        "Bottom Face Button"),
                ("BACK / CANCEL",    "ESC / RIGHT CLICK",     "Right Face Button",         "Right Face Button"),
                ("SCROLL LISTS",     "SCROLL WHEEL",          "Left Stick Up / Down",      "Left Stick Up / Down"),
            };

            // Section separator index
            int menuNavStartIdx = System.Array.FindIndex(keybinds, r => r.action == "NAVIGATE MENU");

            GUIStyle rowActionStyle = CreateStaticStyle(bodyLabelStyle, Color.white, 11, TextAnchor.MiddleLeft, FontStyle.Bold);
            GUIStyle rowKbmStyle = CreateStaticStyle(bodyLabelStyle, new Color(0.85f, 0.92f, 1f, 0.95f), 11, TextAnchor.MiddleLeft, FontStyle.Normal);
            GUIStyle rowSymStyle = CreateStaticStyle(bodyLabelStyle, new Color(0.75f, 1f, 0.8f, 0.95f), 11, TextAnchor.MiddleLeft, FontStyle.Normal);
            GUIStyle rowAsymStyle = CreateStaticStyle(bodyLabelStyle, new Color(0.8f, 0.9f, 1f, 0.95f), 11, TextAnchor.MiddleLeft, FontStyle.Normal);

            for (int i = 0; i < keybinds.Length; i++)
            {
                // Section header divider before menu navigation rows
                if (i == menuNavStartIdx)
                {
                    Rect divRect = GUILayoutUtility.GetRect(totalW, 20f, GUILayout.ExpandWidth(true), GUILayout.Height(20f));
                    DrawRect(divRect, new Color(0.02f, 0.05f, 0.10f, 0.8f));
                    DrawRect(new Rect(divRect.x, divRect.yMax - 1f, divRect.width, 1f), new Color(ACCENT_GOLD.r, ACCENT_GOLD.g, ACCENT_GOLD.b, 0.4f));
                    DrawFittedLabel(new Rect(divRect.x + 4f, divRect.y, divRect.width, divRect.height),
                        "MENU NAVIGATION", thStyle1, ACCENT_GOLD, 10);
                }

                var row = keybinds[i];
                Rect rowRect = GUILayoutUtility.GetRect(totalW, 22f, GUILayout.ExpandWidth(true), GUILayout.Height(22f));
                Color rowBg = (i % 2 == 0) ? new Color(0.04f, 0.08f, 0.14f, 0.55f) : new Color(0.02f, 0.04f, 0.08f, 0.35f);
                DrawRect(rowRect, rowBg);

                GUI.Label(new Rect(rowRect.x + 4f, rowRect.y, col1W - 6f, rowRect.height), row.action, rowActionStyle);
                GUI.Label(new Rect(rowRect.x + col1W, rowRect.y, col2W - 6f, rowRect.height), row.kbm, rowKbmStyle);
                GUI.Label(new Rect(rowRect.x + col1W + col2W, rowRect.y, col3W - 6f, rowRect.height), row.symCtrl, rowSymStyle);
                GUI.Label(new Rect(rowRect.x + col1W + col2W + col3W, rowRect.y, col4W - 6f, rowRect.height), row.asymCtrl, rowAsymStyle);
            }
        }


        private void DrawSelectedLoadoutSummary()
        {
            GUIStyle summaryStyle = new GUIStyle(bodyLabelStyle)
            {
                wordWrap = true,
                clipping = TextClipping.Clip
            };

            Rect bgRect = GUILayoutUtility.GetRect(10f, 10f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawRect(bgRect, new Color(0f, 0f, 0f, 0.42f));
            DrawRect(new Rect(bgRect.x, bgRect.y, bgRect.width, 3f), new Color(ACCENT_YEL.r, ACCENT_YEL.g, ACCENT_YEL.b, 0.85f));

            float innerPadX = 10f;
            float innerPadY = 8f;
            GUILayout.BeginArea(new Rect(bgRect.x + innerPadX, bgRect.y + innerPadY, bgRect.width - innerPadX * 2f, bgRect.height - innerPadY * 2f));
            GUILayout.Label("CURRENT LOADOUT", sectionLabelStyle);
            GUILayout.Space(4);
            foreach (PartType type in PART_DISPLAY_ORDER)
            {
                selectedMainMenuLoadout.TryGetValue(type, out BeyPart part);
                string name = part != null ? PartDisplayNameFormatter.ToShortDisplayName(part).ToUpper() : "NONE";

                Rect lineRect = GUILayoutUtility.GetRect(10f, Mathf.Max(summaryStyle.lineHeight + 6f, summaryStyle.fontSize + 8f), GUILayout.ExpandWidth(true));
                DrawRect(lineRect, new Color(0f, 0f, 0f, 0.36f));
                DrawRect(new Rect(lineRect.x, lineRect.yMax - 1f, lineRect.width, 1f), new Color(1f, 1f, 1f, 0.08f));
                GUI.Label(new Rect(lineRect.x + 6f, lineRect.y + 2f, lineRect.width - 12f, lineRect.height - 4f), $"{type.ToString().ToUpper()}   {name}", summaryStyle);
            }
            GUILayout.EndArea();
        }

        private void DrawMainTopBar(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.02f, 0.05f, 0.10f, 0.94f), new Color(0.04f, 0.08f, 0.16f, 0.96f), ACCENT_CYAN, 3f);
            DrawFrameCorners(rect, ACCENT_CYAN, 24f, 2f);
            DrawMotionBandClipped(new Rect(rect.x + rect.width * 0.60f, rect.y, rect.width * 0.32f, rect.height), ACCENT_CYAN, 8f, 16f, 0.08f);

            float pad = Mathf.Clamp(12f * GetUiScale(), 12f, 24f);
            float logoW = Mathf.Clamp(rect.width * 0.26f, 240f, 400f);
            float tabsW = Mathf.Clamp(rect.width * 0.52f, 480f, 860f);
            float badgeW = Mathf.Clamp(rect.width * 0.20f, 160f, 260f);

            Rect brandRect = new Rect(rect.x + pad, rect.y + pad * 0.5f, logoW, rect.height - pad);
            Rect tabsRect = new Rect(brandRect.xMax + pad, rect.y + pad * 0.5f, tabsW, rect.height - pad);
            Rect badgeRect = new Rect(rect.xMax - pad - badgeW, rect.y + pad * 0.5f, badgeW, rect.height - pad);

            DrawBrandLockup(brandRect);

            float gap = Mathf.Clamp(6f * GetUiScale(), 5f, 10f);
            float tabW = (tabsRect.width - gap * 5f) / 6f;
            if (TopTabBtn("GARAGE", new Rect(tabsRect.x, tabsRect.y, tabW, tabsRect.height), mainMenuPanel == MenuPanel.Home))
                SetMainMenuPanel(MenuPanel.Home);
            if (TopTabBtn("INVENTORY", new Rect(tabsRect.x + tabW + gap, tabsRect.y, tabW, tabsRect.height), mainMenuPanel == MenuPanel.Inventory))
                SetMainMenuPanel(MenuPanel.Inventory);
            if (TopTabBtn("SHRINE BLESSINGS", new Rect(tabsRect.x + (tabW + gap) * 2f, tabsRect.y, tabW, tabsRect.height), mainMenuPanel == MenuPanel.ShrineCompendium))
                SetMainMenuPanel(MenuPanel.ShrineCompendium);
            if (TopTabBtn("MINIGAMES", new Rect(tabsRect.x + (tabW + gap) * 3f, tabsRect.y, tabW, tabsRect.height), mainMenuPanel == MenuPanel.Minigames))
                SetMainMenuPanel(MenuPanel.Minigames);
            if (TopTabBtn("RECORDS", new Rect(tabsRect.x + (tabW + gap) * 4f, tabsRect.y, tabW, tabsRect.height), mainMenuPanel == MenuPanel.Records))
                SetMainMenuPanel(MenuPanel.Records);
            if (TopTabBtn("SETTINGS", new Rect(tabsRect.x + (tabW + gap) * 5f, tabsRect.y, tabW, tabsRect.height), mainMenuPanel == MenuPanel.Settings))
                SetMainMenuPanel(MenuPanel.Settings);

            // Blader Threat Tier / Status badge on the right
            int wins = RunDifficultyManager.GetTotalRunWins();
            string diffName = RunDifficultyManager.GetDifficultyName(wins);

            DrawPanelFrame(badgeRect, new Color(0.03f, 0.07f, 0.14f, 0.85f), new Color(0.04f, 0.10f, 0.20f, 0.90f), new Color(ACCENT_YEL.r, ACCENT_YEL.g, ACCENT_YEL.b, 0.6f), 1.5f);
            DrawFrameCorners(badgeRect, ACCENT_YEL, 10f, 1f);

            GUIStyle rankStyle = CreateStaticStyle(
                sectionLabelStyle,
                ACCENT_YEL,
                ScaleFont(12f),
                TextAnchor.MiddleLeft,
                FontStyle.Bold);
            GUI.Label(new Rect(badgeRect.x + 10f, badgeRect.y + 2f, badgeRect.width - 20f, badgeRect.height * 0.5f), $"THREAT: {diffName}", rankStyle);

            GUIStyle ptsStyle = CreateStaticStyle(
                bodyLabelStyle,
                ACCENT_CYAN,
                ScaleFont(11f),
                TextAnchor.MiddleLeft);
            GUI.Label(new Rect(badgeRect.x + 10f, badgeRect.y + badgeRect.height * 0.48f, badgeRect.width - 20f, badgeRect.height * 0.48f), $"{wins} RUN WINS // ACTIVE", ptsStyle);
        }

        private void DrawBrandLockup(Rect rect)
        {
            float iconSize = rect.height * 0.85f;
            Rect iconRect = new Rect(rect.x, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);
            DrawRect(iconRect, new Color(0.03f, 0.09f, 0.18f, 0.92f));
            DrawFrameCorners(iconRect, ACCENT_CYAN, iconRect.width * 0.35f, 2f);

            // Rotating Blade Cross
            float angle = Time.unscaledTime * 45f;
            GUIUtility.RotateAroundPivot(angle, iconRect.center);
            DrawRect(new Rect(iconRect.x + iconRect.width * 0.15f, iconRect.center.y - 2f, iconRect.width * 0.70f, 4f), ACCENT_CYAN);
            DrawRect(new Rect(iconRect.center.x - 2f, iconRect.y + iconRect.height * 0.15f, 4f, iconRect.height * 0.70f), ACCENT_ORANGE);
            GUIUtility.RotateAroundPivot(-angle, iconRect.center);

            DrawRect(new Rect(iconRect.center.x - 4f, iconRect.center.y - 4f, 8f, 8f), ACCENT_YEL);

            Rect labelRect = new Rect(iconRect.xMax + 10f, rect.y, rect.width - iconRect.width - 10f, rect.height);
            GUIStyle brandStyle = CreateStaticStyle(
                titleBarStyle,
                Color.white,
                ScaleFont(18f),
                TextAnchor.MiddleLeft,
                FontStyle.Bold);
            GUI.Label(new Rect(labelRect.x, labelRect.y + 2f, labelRect.width, labelRect.height * 0.55f), "BLADE SPINNERS", brandStyle);

            GUIStyle subStyle = CreateStaticStyle(
                bodyLabelStyle,
                new Color(0.55f, 0.82f, 1f, 0.85f),
                ScaleFont(10f),
                TextAnchor.MiddleLeft);
            GUI.Label(new Rect(labelRect.x, labelRect.y + labelRect.height * 0.52f, labelRect.width, labelRect.height * 0.45f), "NEXT-GEN BEYBLADE GARAGE", subStyle);
        }

        private void DrawMainBottomBar(Rect rect)
        {
            rect = new Rect(Mathf.Round(rect.x), Mathf.Round(rect.y), Mathf.Round(rect.width), Mathf.Round(rect.height));
            DrawPanelFrame(rect, new Color(0.02f, 0.05f, 0.10f, 0.94f), new Color(0.03f, 0.08f, 0.15f, 0.96f), ACCENT_CYAN, 3f);
            DrawFrameCorners(rect, ACCENT_CYAN, 24f, 2f);

            float pad = Mathf.Round(Mathf.Clamp(10f * GetUiScale(), 10f, 18f));
            float gap = Mathf.Round(Mathf.Clamp(12f * GetUiScale(), 10f, 18f));
            float buttonH = Mathf.Round(rect.height - pad * 2f);
            float autoW = Mathf.Round(Mathf.Clamp(rect.width * 0.18f, 160f, 260f));
            float saveW = Mathf.Round(Mathf.Clamp(rect.width * 0.18f, 160f, 260f));
            float nextSongW = Mathf.Round(Mathf.Clamp(rect.width * 0.15f, 130f, 200f));
            float startW = Mathf.Round(Mathf.Clamp(rect.width * 0.32f, 260f, 440f));

            Rect autoRect = new Rect(Mathf.Round(rect.x + pad), Mathf.Round(rect.y + pad), autoW, buttonH);
            Rect saveRect = new Rect(Mathf.Round(autoRect.xMax + gap), Mathf.Round(rect.y + pad), saveW, buttonH);
            Rect startRect = new Rect(Mathf.Round(rect.xMax - pad - startW), Mathf.Round(rect.y + pad), startW, buttonH);
            Rect nextSongRect = new Rect(Mathf.Round(startRect.x - gap - nextSongW), Mathf.Round(rect.y + pad), nextSongW, buttonH);

            if (ActionBtn("AUTO OPTIMIZE", autoRect, ACCENT_CYAN, false))
                AutoOptimizeCurrentBuild();
            if (ActionBtn("SAVE BUILD", saveRect, new Color(0.18f, 0.62f, 1f, 1f), false))
                buildSlotPickerOpen = !buildSlotPickerOpen;
            if (ActionBtn("NEXT SONG", nextSongRect, ACCENT_MAGENTA, false))
                SoundManager.SkipToNextMusic();
            if (ActionBtn("START RUN // 出撃", startRect, ACCENT_ORANGE, false))
                StartRun();

            if (!buildSlotPickerOpen)
                return;

            float modalW = Mathf.Clamp(rect.width * 0.34f, 320f, 480f);
            float modalH = Mathf.Clamp(172f * GetUiScale(), 162f, 220f);
            Rect modal = new Rect(saveRect.x, rect.y - modalH - gap * 0.7f, modalW, modalH);
            DrawPanelFrame(modal, new Color(0.04f, 0.08f, 0.14f, 0.98f), new Color(0.05f, 0.10f, 0.18f, 0.98f), ACCENT_CYAN, 2f);
            DrawFittedLabel(new Rect(modal.x + 12f, modal.y + 8f, modal.width - 24f, 28f), "BUILD SLOTS", sectionLabelStyle, ACCENT_CYAN, 10);

            float rowY = modal.y + 42f;
            float rowH = (modal.height - 54f) / 3f;
            for (int i = 0; i < 3; i++)
            {
                Rect row = new Rect(modal.x + 10f, rowY + rowH * i, modal.width - 20f, rowH - 6f);
                DrawRect(row, new Color(0f, 0f, 0f, 0.28f));
                DrawRect(new Rect(row.x, row.yMax - 1f, row.width, 1f), new Color(1f, 1f, 1f, 0.08f));
                string slotName = string.IsNullOrWhiteSpace(savedBuildNames[i]) ? "EMPTY SLOT" : savedBuildNames[i].ToUpperInvariant();
                DrawFittedLabel(new Rect(row.x + 8f, row.y + 2f, row.width * 0.52f, row.height - 4f), $"SLOT {i + 1}   {slotName}", bodyLabelStyle, Color.white, 10);

                float miniW = Mathf.Clamp(row.width * 0.18f, 78f, 110f);
                Rect saveMini = new Rect(row.xMax - miniW * 2f - 8f, row.y + 5f, miniW, row.height - 10f);
                Rect loadMini = new Rect(row.xMax - miniW, row.y + 5f, miniW, row.height - 10f);
                if (ActionBtn("SAVE", saveMini, ACCENT_CYAN, false))
                    SaveCurrentBuildToSlot(i);
                if (ActionBtn("LOAD", loadMini, savedBuildSlots[i] != null ? new Color(0.19f, 0.80f, 0.55f, 1f) : new Color(0.19f, 0.80f, 0.55f, 0.35f), false, savedBuildSlots[i] != null))
                    LoadBuildFromSlot(i);
            }
        }

        private void DrawRunMenuShell(string title, string primaryLabel, Action primaryAction)
        {
            int sw = Mathf.RoundToInt(UiWidth);
            int sh = Mathf.RoundToInt(UiHeight);
            float uiScale = GetUiScale();
            float gutter = Mathf.Clamp(sw * 0.006f, 8f, 18f);

            DrawRect(new Rect(0, 0, sw, sh), new Color(0f, 0f, 0f, 0.60f));
            DrawConceptBackdrop(new Rect(0, 0, sw, sh));
            DrawRect(new Rect(0, 0, sw, sh), new Color(0f, 0.01f, 0.03f, 0.40f));

            Rect shell = new Rect(gutter * 2f, gutter * 2f, sw - gutter * 4f, sh - gutter * 4f);
            DrawPanelFrame(shell, new Color(0.02f, 0.05f, 0.11f, 0.96f), new Color(0.03f, 0.08f, 0.15f, 0.98f), ACCENT_CYAN, 3f);

            float topH = Mathf.Clamp(82f * uiScale, 74f, 118f);
            Rect topRect = new Rect(shell.x + gutter, shell.y + gutter, shell.width - gutter * 2f, topH);
            DrawPanelFrame(topRect, new Color(0.03f, 0.07f, 0.14f, 0.96f), new Color(0.04f, 0.10f, 0.18f, 0.96f), ACCENT_CYAN, 2f);
            DrawFittedLabel(new Rect(topRect.x + 16f, topRect.y + 8f, topRect.width * 0.18f, topRect.height - 16f), title, titleBarStyle, Color.white, 12);

            // Isaac-Style Floor Progression Node Map
            float mapX = topRect.x + topRect.width * 0.19f;
            float mapW = topRect.width * 0.45f;
            Rect mapRect = new Rect(mapX, topRect.y + 6f, mapW, topRect.height - 12f);
            DrawFloorProgressionMap(mapRect, uiScale);

            float actionH = Mathf.Clamp(44f * uiScale, 40f, 58f);
            float actionW = Mathf.Clamp(156f * uiScale, 140f, 220f);
            Rect primaryRect = new Rect(topRect.xMax - actionW * 2f - 18f, topRect.center.y - actionH * 0.5f, actionW, actionH);
            Rect returnRect = new Rect(topRect.xMax - actionW, topRect.center.y - actionH * 0.5f, actionW, actionH);
            if (ActionBtn(primaryLabel, primaryRect, new Color(0.18f, 0.72f, 1f, 1f), false))
                primaryAction?.Invoke();
            if (ActionBtn("RETURN MENU", returnRect, ACCENT_RED, true))
                ReturnToMainMenu();

            float tabY = topRect.yMax + gutter;
            float tabH = Mathf.Clamp(50f * uiScale, 44f, 64f);
            float tabW = Mathf.Clamp(170f * uiScale, 140f, 220f);

            bool isIntermission = rootState == RootUiState.BetweenArenas;
            float curX = shell.x + gutter;

            if (isIntermission)
            {
                Rect shrineTab = new Rect(curX, tabY, tabW, tabH);
                if (TopTabBtn("BLADER SHRINE", shrineTab, pausePanel == MenuPanel.Shrine))
                    SetPausePanel(MenuPanel.Shrine);
                curX = shrineTab.xMax + gutter;
            }

            Rect garageTab = new Rect(curX, tabY, tabW, tabH);
            curX = garageTab.xMax + gutter;
            Rect inventoryTab = new Rect(curX, tabY, tabW, tabH);
            curX = inventoryTab.xMax + gutter;
            Rect settingsTab = new Rect(curX, tabY, tabW, tabH);

            if (TopTabBtn("GARAGE", garageTab, pausePanel == MenuPanel.Home))
                SetPausePanel(MenuPanel.Home);
            if (TopTabBtn("INVENTORY", inventoryTab, pausePanel == MenuPanel.Inventory))
                SetPausePanel(MenuPanel.Inventory);
            if (TopTabBtn("SETTINGS", settingsTab, pausePanel == MenuPanel.Settings))
                SetPausePanel(MenuPanel.Settings);

            Rect contentRect = new Rect(shell.x + gutter, garageTab.yMax + gutter, shell.width - gutter * 2f, shell.yMax - garageTab.yMax - gutter * 2f);
            switch (pausePanel)
            {
                case MenuPanel.Shrine:
                    DrawBladerShrinePanel(contentRect);
                    break;

                case MenuPanel.Inventory:
                    DrawInventoryWorkspace(
                        contentRect,
                        true);
                    break;

                case MenuPanel.Settings:
                    DrawFramedContentPanel(contentRect, "SETTINGS", delegate
                    {
                        DrawSettingsPanel();
                    });
                    break;

                default:
                    DrawGarageOverview(contentRect, runContext.Player, false);
                    break;
            }
        }

        private void DrawFloorProgressionMap(Rect area, float uiScale)
        {
            int totalArenas = runContext.Progression != null ? runContext.Progression.TotalArenaCount : 8;
            if (totalArenas <= 0) totalArenas = 8;

            DrawPanelFrame(area, new Color(0.015f, 0.04f, 0.08f, 0.85f), new Color(0.03f, 0.06f, 0.12f, 0.90f), ACCENT_CYAN, 1.5f);

            float pad = Mathf.Clamp(10f * uiScale, 8f, 18f);
            float nodeSize = Mathf.Clamp(area.height - pad * 1.5f, 26f, 50f);
            float availableW = area.width - pad * 2f;
            float gap = (availableW - nodeSize * totalArenas) / Mathf.Max(1, totalArenas - 1);

            for (int i = 0; i < totalArenas; i++)
            {
                float x = area.x + pad + i * (nodeSize + gap);
                float y = area.center.y - nodeSize * 0.5f;
                Rect nodeRect = new Rect(x, y, nodeSize, nodeSize);

                // Connector line to next node
                if (i < totalArenas - 1)
                {
                    float lineX = nodeRect.xMax;
                    float lineW = gap;
                    float lineH = 3f;
                    float lineY = area.center.y - lineH * 0.5f;
                    Color lineColor = (i < arenasClearedThisRun) ? ACCENT_CYAN : new Color(1f, 1f, 1f, 0.12f);
                    DrawRect(new Rect(lineX, lineY, lineW, lineH), lineColor);
                }

                bool isCleared = i < arenasClearedThisRun;
                bool isCurrent = i == arenasClearedThisRun;
                bool isSemiBoss = i == 3;
                bool isFinalBoss = i == totalArenas - 1;

                Color nodeBorder = isCurrent
                    ? ACCENT_GOLD
                    : (isFinalBoss ? ACCENT_RED : (isSemiBoss ? ACCENT_ORANGE : (isCleared ? ACCENT_CYAN : new Color(0.3f, 0.4f, 0.5f, 0.5f))));
                Color nodeBg = isCurrent
                    ? new Color(0.18f, 0.14f, 0.02f, 0.95f)
                    : (isCleared ? new Color(0.03f, 0.12f, 0.20f, 0.90f) : new Color(0.02f, 0.04f, 0.08f, 0.85f));

                DrawPanelFrame(nodeRect, nodeBg, nodeBg, nodeBorder, 2f);
                if (isCurrent)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
                    DrawFrameCorners(nodeRect, Color.Lerp(ACCENT_GOLD, Color.white, pulse), 8f, 2f);
                }

                string nodeLabel = isCleared ? "✓" : (isFinalBoss ? "BOSS" : (isSemiBoss ? "MID" : $"F{i + 1}"));
                Color textColor = isCurrent
                    ? ACCENT_GOLD
                    : (isFinalBoss ? ACCENT_RED : (isSemiBoss ? ACCENT_ORANGE : (isCleared ? ACCENT_CYAN : new Color(0.6f, 0.7f, 0.8f, 0.7f))));
                DrawFittedLabel(nodeRect, nodeLabel, sectionLabelStyle, textColor, 10);
            }
        }

        private void DrawFramedContentPanel(Rect area, string label, Action drawContent)
        {
            DrawPanelFrame(area, new Color(0.03f, 0.06f, 0.11f, 0.94f), new Color(0.05f, 0.10f, 0.16f, 0.95f), ACCENT_CYAN, 2f);
            float pad = Mathf.Clamp(12f * GetUiScale(), 12f, 20f);
            float headerH = Mathf.Clamp(34f * GetUiScale(), 30f, 48f);
            DrawFittedLabel(new Rect(area.x + pad, area.y + 6f, area.width - pad * 2f, headerH), label, sectionLabelStyle, ACCENT_CYAN, 10);
            GUILayout.BeginArea(new Rect(area.x + pad, area.y + headerH + 8f, area.width - pad * 2f, area.height - headerH - pad - 8f));
            drawContent?.Invoke();
            GUILayout.EndArea();
        }

        private void DrawBladerShrinePanel(Rect area)
        {
            float uiScale = GetUiScale();
            BladerShrineRunState shrine = runContext.ShrineState;
            if (shrine == null)
            {
                DrawFramedContentPanel(area, "BLADER SHRINE", () =>
                {
                    GUILayout.Label("No active shrine available for this run.", bodyLabelStyle);
                });
                return;
            }

            DrawPanelFrame(area, new Color(0.02f, 0.04f, 0.09f, 0.96f), new Color(0.04f, 0.07f, 0.14f, 0.98f), ACCENT_GOLD, 2.5f);

            float pad = Mathf.Clamp(14f * uiScale, 12f, 24f);

            // 1. Shrine Header Bar
            float headerH = Mathf.Clamp(56f * uiScale, 50f, 74f);
            Rect headerRect = new Rect(area.x + pad, area.y + pad * 0.7f, area.width - pad * 2f, headerH);
            DrawRect(headerRect, new Color(0.03f, 0.06f, 0.12f, 0.85f));
            DrawFrameCorners(headerRect, ACCENT_GOLD, 12f, 2f);

            DrawFittedLabel(
                new Rect(headerRect.x + 16f, headerRect.y + 4f, headerRect.width * 0.55f, headerRect.height * 0.52f),
                "THE BLADER SHRINE // 試練の社",
                titleBarStyle,
                ACCENT_GOLD,
                14);

            DrawFittedLabel(
                new Rect(headerRect.x + 16f, headerRect.y + headerRect.height * 0.50f, headerRect.width * 0.65f, headerRect.height * 0.44f),
                "Spend combat-earned Blader Points to invoke powerful spirit blessings for your run.",
                bodyLabelStyle,
                new Color(0.85f, 0.92f, 1f, 0.95f),
                16);

            // Points Balance Badge on right
            float balanceW = Mathf.Clamp(230f * uiScale, 200f, 320f);
            Rect balanceRect = new Rect(headerRect.xMax - balanceW - 10f, headerRect.center.y - (headerH * 0.38f), balanceW, headerH * 0.76f);
            DrawPanelFrame(balanceRect, new Color(0.08f, 0.06f, 0.02f, 0.95f), new Color(0.16f, 0.12f, 0.03f, 0.98f), ACCENT_GOLD, 2f);
            DrawFittedLabel(
                new Rect(balanceRect.x + 8f, balanceRect.y + 2f, balanceRect.width - 16f, balanceRect.height - 4f),
                $"{shrine.BladerPoints:N0} BLADER PTS",
                sectionLabelStyle,
                ACCENT_GOLD,
                12);

            // 2. Offerings Section (3 cards)
            float contentTop = headerRect.yMax + pad * 0.7f;
            float activeShelfH = Mathf.Clamp(95f * uiScale, 85f, 130f);
            float rerollH = Mathf.Clamp(38f * uiScale, 34f, 48f);
            float cardsH = area.yMax - contentTop - activeShelfH - rerollH - pad * 2.2f;

            IReadOnlyList<ShrinePerkData> offerings = shrine.CurrentOfferings;
            if (offerings == null || offerings.Count == 0)
            {
                Rect emptyRect = new Rect(area.x + pad, contentTop, area.width - pad * 2f, cardsH);
                DrawRect(emptyRect, new Color(0f, 0f, 0f, 0.4f));
                DrawFittedLabel(emptyRect, "ALL SHRINE BLESSINGS IN THE REALM HAVE BEEN CLAIMED", sectionLabelStyle, ACCENT_GOLD, 14);
            }
            else
            {
                int count = offerings.Count;
                float gap = Mathf.Clamp(12f * uiScale, 10f, 20f);
                float cardW = (area.width - pad * 2f - gap * (count - 1)) / count;

                for (int i = 0; i < count; i++)
                {
                    ShrinePerkData perk = offerings[i];
                    Rect cardRect = new Rect(area.x + pad + (cardW + gap) * i, contentTop, cardW, cardsH);
                    DrawOfferingCard(cardRect, perk, shrine, uiScale);
                }
            }

            // 3. Reroll & Utilities Row
            float rerollY = contentTop + cardsH + pad * 0.5f;
            float rerollW = Mathf.Clamp(280f * uiScale, 240f, 360f);
            Rect rerollRect = new Rect(area.center.x - rerollW * 0.5f, rerollY, rerollW, rerollH);
            bool canReroll = shrine.BladerPoints >= 100;
            if (ActionBtn("REROLL OFFERINGS (100 PTS)", rerollRect, canReroll ? ACCENT_CYAN : new Color(0.4f, 0.4f, 0.4f, 0.6f), false, canReroll))
            {
                shrine.TryReroll(100);
            }

            // 4. Active Run Perks Shelf
            float shelfY = rerollY + rerollH + pad * 0.5f;
            Rect shelfRect = new Rect(area.x + pad, shelfY, area.width - pad * 2f, activeShelfH);
            DrawActivePerksShelf(shelfRect, shrine, uiScale);
        }

        private void DrawOfferingCard(Rect rect, ShrinePerkData perk, BladerShrineRunState shrine, float uiScale)
        {
            Color rarityColor = GetRarityColor(perk.Rarity);

            bool isOwned = shrine.HasPerk(perk.Type);
            bool canAfford = shrine.BladerPoints >= perk.BaseCost;

            DrawPanelFrame(rect, new Color(0.02f, 0.04f, 0.09f, 0.95f), new Color(0.04f, 0.07f, 0.14f, 0.98f), rarityColor, 2f);
            DrawFrameCorners(rect, rarityColor, 16f, 2.5f);

            float pad = 16f;

            // 1. Rarity / Category Tag Header
            Rect tagRect = new Rect(rect.x + pad + 2f, rect.y + 14f, rect.width - (pad + 2f) * 2f, 26f);
            DrawRect(tagRect, new Color(rarityColor.r * 0.2f, rarityColor.g * 0.2f, rarityColor.b * 0.2f, 0.75f));
            DrawFrameCorners(tagRect, rarityColor, 6f, 1f);
            DrawFittedLabel(tagRect, $"{perk.Rarity.ToString().ToUpperInvariant()} BLESSING // {perk.Category.ToString().ToUpperInvariant()}", sectionLabelStyle, rarityColor, 12);

            // 2. Icon + Japanese Subtitle + Bold Name
            float iconSize = 60f;
            Rect iconRect = new Rect(rect.x + pad + 2f, tagRect.yMax + 12f, iconSize, iconSize);
            DrawRect(iconRect, new Color(0f, 0f, 0f, 0.55f));
            DrawFrameCorners(iconRect, rarityColor, 8f, 2f);
            GUIStyle iconStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                contentOffset = Vector2.zero
            };
            iconStyle.normal.textColor = rarityColor;
            GUI.Label(iconRect, perk.IconSymbol, iconStyle);

            Rect titleRect = new Rect(iconRect.xMax + 14f, iconRect.y, rect.width - iconSize - (pad + 2f) * 2f - 14f, iconSize);
            DrawFittedLabel(new Rect(titleRect.x, titleRect.y, titleRect.width, 22f), perk.JapaneseName, bodyLabelStyle, new Color(0.75f, 0.85f, 0.95f, 0.80f), 12);
            DrawFittedLabel(new Rect(titleRect.x, titleRect.y + 22f, titleRect.width, 38f), perk.Name, titleBarStyle, Color.white, 18);

            // 3. Buy Button
            float btnH = 48f;
            Rect btnRect = new Rect(rect.x + pad, rect.yMax - btnH - pad, rect.width - pad * 2f, btnH);

            // 4. Description & Feature Details Box
            float descY = iconRect.yMax + 14f;
            float descH = btnRect.y - descY - 12f;
            Rect descRect = new Rect(rect.x + pad, descY, rect.width - pad * 2f, descH);
            DrawPanelFrame(descRect, new Color(0.015f, 0.03f, 0.07f, 0.85f), new Color(0.02f, 0.04f, 0.10f, 0.90f), new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.35f), 1.2f);

            GUIStyle descStyle = new GUIStyle(bodyLabelStyle)
            {
                wordWrap = true,
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            descStyle.normal.textColor = new Color(0.92f, 0.96f, 1f, 0.98f);
            GUI.Label(new Rect(descRect.x + 14f, descRect.y + 14f, descRect.width - 28f, descRect.height - 28f), perk.Description, descStyle);

            if (isOwned)
            {
                ActionBtn("ACQUIRED", btnRect, new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.5f), false, false);
            }
            else
            {
                string btnLabel = canAfford ? $"INVOKE ({perk.BaseCost} PTS)" : $"LOCKED ({perk.BaseCost} PTS)";
                Color btnColor = canAfford ? rarityColor : new Color(0.4f, 0.4f, 0.4f, 0.5f);
                if (ActionBtn(btnLabel, btnRect, btnColor, false, canAfford))
                {
                    if (shrine.TryPurchasePerk(perk.Type))
                    {
                        SpawnComicPopup($"{perk.Name} ACQUIRED!", rarityColor, 1.4f);
                        SoundManager.PlayUiConfirm();
                    }
                }
            }
        }

        private void DrawActivePerksShelf(Rect rect, BladerShrineRunState shrine, float uiScale)
        {
            DrawPanelFrame(rect, new Color(0.02f, 0.04f, 0.08f, 0.9f), new Color(0.03f, 0.06f, 0.12f, 0.95f), new Color(0.00f, 0.490f, 0.800f, 0.5f), 1.5f);
            float pad = Mathf.Clamp(8f * uiScale, 6f, 14f);

            var active = shrine.ActivePerks;
            string headerText = $"ACTIVE SPIRIT BLESSINGS ({active.Count})";
            DrawFittedLabel(new Rect(rect.x + pad, rect.y + 4f, rect.width - pad * 2f, 22f), headerText, sectionLabelStyle, new Color(0.00f, 0.490f, 0.800f, 1f), 10);

            if (active.Count == 0)
            {
                DrawFittedLabel(new Rect(rect.x + pad, rect.y + 28f, rect.width - pad * 2f, rect.height - 34f), "No shrine blessings acquired yet. Select an offering above to empower your Bey for the run.", bodyLabelStyle, new Color(0.6f, 0.7f, 0.8f, 0.6f), 10);
                return;
            }

            float badgeW = Mathf.Clamp(190f * uiScale, 160f, 260f);
            float badgeH = rect.height - 36f;
            int idx = 0;
            foreach (ShrinePerkType perkType in active)
            {
                ShrinePerkData data = ShrinePerkCatalog.GetPerk(perkType);
                if (data == null) continue;
                Rect bRect = new Rect(rect.x + pad + (badgeW + 8f) * idx, rect.y + 28f, badgeW, badgeH);
                if (bRect.xMax > rect.xMax - pad) break;

                Color perkRarityColor = GetRarityColor(data.Rarity);
                DrawRect(bRect, new Color(0.04f, 0.08f, 0.15f, 0.8f));
                DrawFrameCorners(bRect, perkRarityColor, 8f, 1.2f);

                DrawFittedLabel(new Rect(bRect.x + 6f, bRect.y + 4f, bRect.width - 12f, bRect.height * 0.45f), $"{data.IconSymbol} {data.Name}", sectionLabelStyle, perkRarityColor, 9);
                DrawFittedLabel(new Rect(bRect.x + 6f, bRect.y + bRect.height * 0.45f, bRect.width - 12f, bRect.height * 0.50f), data.JapaneseName, bodyLabelStyle, new Color(0.7f, 0.8f, 0.9f, 0.7f), 8);
                idx++;
            }
        }

        private void DrawGarageOverview(Rect area, PlayerManager runPlayer, bool showBuildManagement)
        {
            Dictionary<PartType, BeyPart> loadout = runPlayer != null
                ? GetCurrentRunLoadout(runPlayer)
                : selectedMainMenuLoadout;

            if (runPlayer != null)
                RefreshPreviewFromLoadout(loadout);

            float gap = Mathf.Clamp(12f * GetUiScale(), 10f, 18f);
            float leftW = Mathf.Clamp(area.width * 0.28f, 260f, 360f);
            float rightW = Mathf.Clamp(area.width * 0.22f, 220f, 320f);
            Rect leftRect = new Rect(area.x, area.y, leftW, area.height);
            Rect centerRect = new Rect(leftRect.xMax + gap, area.y, area.width - leftW - rightW - gap * 2f, area.height);
            Rect rightRect = new Rect(centerRect.xMax + gap, area.y, rightW, area.height);

            BeyStatBlock stats = GetStatsForDisplay(runPlayer);
            DrawGarageLoadoutPanel(leftRect, loadout, stats, showBuildManagement);
            DrawGarageStagePanel(centerRect, loadout, runPlayer != null, runPlayer);

            BeyPart detailPart = null;
            if (garageInspectSlot.HasValue)
                loadout.TryGetValue(garageInspectSlot.Value, out detailPart);
            if (detailPart == null)
                loadout.TryGetValue(PartType.FaceBolt, out detailPart);
            if (detailPart == null)
                loadout.TryGetValue(PartType.EnergyRing, out detailPart);

            DrawGarageInfoPanel(rightRect, loadout, detailPart, stats);
        }

        private void DrawGarageLoadoutPanel(Rect area, Dictionary<PartType, BeyPart> loadout, BeyStatBlock stats, bool showBuildManagement)
        {
            DrawPanelFrame(area, new Color(0.03f, 0.07f, 0.12f, 0.94f), new Color(0.05f, 0.10f, 0.18f, 0.95f), ACCENT_CYAN, 2f);

            float pad = Mathf.Clamp(12f * GetUiScale(), 10f, 18f);
            float headerH = Mathf.Clamp(70f * GetUiScale(), 64f, 96f);
            Rect headerRect = new Rect(area.x + pad, area.y + pad, area.width - pad * 2f, headerH);
            DrawRect(headerRect, new Color(0f, 0f, 0f, 0.22f));
            DrawRect(new Rect(headerRect.x, headerRect.y, headerRect.width, 2f), ACCENT_CYAN);

            string buildName = GetBuildDisplayName(loadout).ToUpperInvariant();
            string faceBoltName = GetFaceBoltDisplayName(loadout).ToUpperInvariant();
            int totalScore = Mathf.RoundToInt(GetBuildPowerScore(loadout));
            DrawFittedLabel(new Rect(headerRect.x + 8f, headerRect.y + 4f, headerRect.width - 16f, headerRect.height * 0.36f), "CURRENT BUILD", sectionLabelStyle, ACCENT_CYAN, 10);
            DrawFittedLabel(new Rect(headerRect.x + 8f, headerRect.y + headerRect.height * 0.26f, headerRect.width - 110f, headerRect.height * 0.34f), buildName, bodyLabelStyle, Color.white, 10);
            DrawFittedLabel(new Rect(headerRect.x + 8f, headerRect.y + headerRect.height * 0.58f, headerRect.width * 0.58f, headerRect.height * 0.24f), $"FACE BOLT  {faceBoltName}", bodyLabelStyle, new Color(0.72f, 0.88f, 1f, 0.9f), 10);

            Rect scoreRect = new Rect(headerRect.xMax - 92f, headerRect.y + 10f, 84f, headerRect.height - 20f);
            DrawRect(scoreRect, new Color(0.02f, 0.10f, 0.18f, 0.9f));
            DrawRect(new Rect(scoreRect.x, scoreRect.yMax - 3f, scoreRect.width, 3f), ACCENT_CYAN);
            DrawFittedLabel(new Rect(scoreRect.x, scoreRect.y + 6f, scoreRect.width, 18f), "SCORE", sectionLabelStyle, ACCENT_CYAN, 9);
            DrawFittedLabel(new Rect(scoreRect.x, scoreRect.y + 26f, scoreRect.width, scoreRect.height - 30f), totalScore.ToString(), titleBarStyle, Color.white, 12);

            float listY = headerRect.yMax + 10f;
            float listH = showBuildManagement ? area.height * 0.46f : area.height * 0.50f;
            Rect listRect = new Rect(area.x + pad, listY, area.width - pad * 2f, listH);
            DrawRect(listRect, new Color(0f, 0f, 0f, 0.18f));

            float rowH = Mathf.Clamp(58f * GetUiScale(), 50f, 72f);
            float currentY = listRect.y + 6f;
            foreach (PartType type in PART_DISPLAY_ORDER)
            {
                Rect row = new Rect(listRect.x + 4f, currentY, listRect.width - 8f, rowH);
                currentY += rowH + 6f;
                loadout.TryGetValue(type, out BeyPart part);
                bool isSelected = garageInspectSlot == type;
                bool hovered = row.Contains(Event.current.mousePosition);
                DrawRect(row, isSelected ? new Color(0.10f, 0.25f, 0.40f, 0.95f) : (hovered ? new Color(0.08f, 0.18f, 0.28f, 0.92f) : new Color(0.04f, 0.08f, 0.14f, 0.88f)));
                DrawRect(new Rect(row.x, row.yMax - 2f, row.width, 2f), (isSelected || hovered) ? ACCENT_CYAN : new Color(1f, 1f, 1f, 0.06f));

                Rect iconRect = new Rect(row.x + 8f, row.y + 7f, row.height - 14f, row.height - 14f);
                DrawRect(iconRect, new Color(0f, 0f, 0f, 0.26f));
                if (type != PartType.FaceBolt && partPreviewTextures.TryGetValue(type, out RenderTexture loadoutRT) && loadoutRT != null && loadoutRT.IsCreated())
                    GUI.DrawTexture(iconRect, loadoutRT, ScaleMode.ScaleToFit, true);
                else
                    DrawPartSprite(iconRect, part);

                Rect labelRect = new Rect(iconRect.xMax + 8f, row.y + 6f, row.width * 0.48f, row.height * 0.42f);
                Rect subRect = new Rect(iconRect.xMax + 8f, row.y + row.height * 0.48f, row.width * 0.48f, row.height * 0.26f);
                Rect rarityRect = new Rect(row.xMax - 86f, row.y + 8f, 74f, row.height - 16f);

                string name = part != null ? PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant() : "EMPTY";
                DrawFittedLabel(labelRect, name, bodyLabelStyle, Color.white, 10);
                DrawFittedLabel(subRect, type.ToString().ToUpperInvariant(), bodyLabelStyle, new Color(0.65f, 0.83f, 1f, 0.9f), 10);
                DrawRarityPill(rarityRect, part != null ? part.Rarity : RarityTier.Common, part != null ? part.Rarity.ToString().ToUpperInvariant() : "NONE");

                if (WithButtonSound(GUI.Button(row, GUIContent.none, GUIStyle.none)))
                    garageInspectSlot = type;
            }

            float statsY = Mathf.Min(currentY + 10f, area.yMax - area.height * 0.28f);
            Rect statsRect = new Rect(area.x + pad, statsY, area.width - pad * 2f, area.yMax - statsY - pad);
            DrawOverallStatBars(statsRect, stats);
        }

        private void DrawGarageStagePanel(Rect area, Dictionary<PartType, BeyPart> loadout, bool useRunInventory, PlayerManager runPlayer)
        {
            DrawPanelFrame(area, new Color(0.02f, 0.06f, 0.11f, 0.94f), new Color(0.05f, 0.10f, 0.16f, 0.95f), ACCENT_CYAN, 2f);

            float pad = Mathf.Clamp(14f * GetUiScale(), 12f, 22f);
            Rect inner = new Rect(area.x + pad, area.y + pad, area.width - pad * 2f, area.height - pad * 2f);
            DrawFittedLabel(new Rect(inner.x, inner.y, inner.width, 28f), useRunInventory ? "RUN GARAGE // INSPECTION" : "GARAGE // 3D INSPECTION", sectionLabelStyle, ACCENT_CYAN, 10);

            Rect previewRect = new Rect(inner.x + inner.width * 0.12f, inner.y + inner.height * 0.15f, inner.width * 0.76f, inner.height * 0.60f);
            DrawRect(new Rect(previewRect.x, previewRect.center.y - 2f, previewRect.width, 4f), new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.18f));
            DrawFrameCorners(previewRect, new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.40f), Mathf.Clamp(previewRect.width * 0.12f, 22f, 48f), 2f);
            DrawMotionBandClipped(new Rect(previewRect.x + previewRect.width * 0.62f, previewRect.y, previewRect.width * 0.24f, previewRect.height), ACCENT_CYAN, 6f, 12f, 0.08f);
            if (previewTexture != null)
                GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit, true);

            float nodeSize = Mathf.Clamp(130f * GetUiScale(), 110f, 160f);
            float smallYOffset = Mathf.Clamp(10f * GetUiScale(), 10f, 18f);
            Rect faceBoltRect = new Rect(previewRect.x - nodeSize * 0.72f, previewRect.y + previewRect.height * 0.12f, nodeSize, nodeSize);
            Rect energyRingRect = new Rect(previewRect.xMax - nodeSize * 0.28f, previewRect.y + previewRect.height * 0.12f, nodeSize, nodeSize);
            Rect fusionRect = new Rect(previewRect.x - nodeSize * 0.40f, previewRect.yMax - nodeSize * 0.92f, nodeSize, nodeSize);
            Rect trackRect = new Rect(previewRect.xMax - nodeSize * 0.60f, previewRect.yMax - nodeSize * 0.92f, nodeSize, nodeSize);
            Rect tipRect = new Rect(previewRect.center.x - nodeSize * 0.5f, previewRect.yMax - smallYOffset, nodeSize, nodeSize);

            Rect hintRect = new Rect(inner.x, inner.yMax - 42f, inner.width, 28f);
            DrawFittedLabel(hintRect, "CLICK ANY PART NODE TO INSPECT DETAILS // GO TO INVENTORY TO EQUIP PARTS", bodyLabelStyle, new Color(0.74f, 0.90f, 1f, 0.82f), 10);

            DrawOrbitSlot(faceBoltRect, PartType.FaceBolt, loadout, "FACE BOLT");
            DrawOrbitSlot(energyRingRect, PartType.EnergyRing, loadout, "ENERGY RING");
            DrawOrbitSlot(fusionRect, PartType.FusionWheel, loadout, "FUSION WHEEL");
            DrawOrbitSlot(trackRect, PartType.Track, loadout, "TRACK");
            DrawOrbitSlot(tipRect, PartType.Tip, loadout, "TIP");

            if (previewTexture != null)
                HandlePreviewDragInput(previewRect);
        }

        private void DrawOrbitSlot(Rect rect, PartType type, Dictionary<PartType, BeyPart> loadout, string label)
        {
            loadout.TryGetValue(type, out BeyPart part);
            bool isSelected = garageInspectSlot == type;
            DrawRect(rect, isSelected ? new Color(0.10f, 0.26f, 0.42f, 0.96f) : new Color(0.04f, 0.09f, 0.15f, 0.92f));
            DrawFrameCorners(rect, isSelected ? ACCENT_CYAN : new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.45f), rect.width * 0.24f, 2f);
            DrawRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), isSelected ? ACCENT_CYAN : new Color(1f, 1f, 1f, 0.08f));

            Rect labelRect = new Rect(rect.x - 8f, rect.y - 26f, rect.width + 16f, 20f);
            DrawFittedLabel(labelRect, label, sectionLabelStyle, isSelected ? ACCENT_CYAN : Color.white, 9);

            float iconSize = Mathf.Min(rect.width * 0.68f, rect.height - 38f);
            float availableH = rect.height - 26f - 26f;
            Rect iconRect = new Rect(rect.center.x - iconSize * 0.5f, rect.y + 8f + (availableH - iconSize) * 0.5f, iconSize, iconSize);
            if (type != PartType.FaceBolt && partPreviewTextures.TryGetValue(type, out RenderTexture partRT) && partRT != null && partRT.IsCreated())
                GUI.DrawTexture(iconRect, partRT, ScaleMode.ScaleToFit, true);
            else
                DrawPartSprite(iconRect, part);
            DrawFittedLabel(new Rect(rect.x + 6f, rect.yMax - 26f, rect.width - 12f, 18f), part != null ? PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant() : "EMPTY", bodyLabelStyle, Color.white, 9);

            if (WithButtonSound(GUI.Button(rect, GUIContent.none, GUIStyle.none)))
                garageInspectSlot = type;
        }

        private void DrawGarageInfoPanel(Rect area, Dictionary<PartType, BeyPart> loadout, BeyPart detailPart, BeyStatBlock stats)
        {
            DrawPanelFrame(area, new Color(0.03f, 0.07f, 0.13f, 0.94f), new Color(0.05f, 0.11f, 0.18f, 0.95f), ACCENT_CYAN, 2f);
            float pad = Mathf.Clamp(12f * GetUiScale(), 10f, 18f);
            Rect content = new Rect(area.x + pad, area.y + pad, area.width - pad * 2f, area.height - pad * 2f);
            GUILayout.BeginArea(content);

            if (detailPart != null)
            {
                DrawSelectedPartCard(
                    detailPart,
                    "PART DATA",
                    false);
            }
            else
            {
                GUILayout.Label("SYSTEM OVERVIEW", sectionLabelStyle);
                GUILayout.Space(6f);
                GUILayout.Label("HOVER A PART ON THE LEFT OR CLICK A NODE AROUND THE BEY TO INSPECT AND SWAP IT.", bodyLabelStyle);
                GUILayout.Space(10f);

                loadout.TryGetValue(PartType.FaceBolt, out BeyPart faceBolt);
                BeyAbility ability = ResolveAbilityForPart(faceBolt);
                GUILayout.Label("ACTIVE ABILITY", sectionLabelStyle);
                if (ability == null)
                {
                    GUILayout.Label("NONE", bodyLabelStyle);
                }
                else
                {
                    GUILayout.Label(ability.AbilityName.ToUpperInvariant(), statRowStyle);
                    GUILayout.Label($"COST     {ability.ManaCost:0.#}", statRowStyle);
                    GUILayout.Label(ability.Rarity.ToString().ToUpperInvariant(), bodyLabelStyle);
                    if (!string.IsNullOrWhiteSpace(ability.Description))
                        GUILayout.Label(ability.Description.ToUpperInvariant(), bodyLabelStyle);
                }

                loadout.TryGetValue(
                    PartType.EnergyRing,
                    out BeyPart energyRing);
                BeyPassive passive =
                    EnergyRingPassiveResolver.Resolve(energyRing);
                GUILayout.Space(10f);
                GUILayout.Label(
                    "ACTIVE PASSIVE",
                    sectionLabelStyle);
                if (passive == null)
                {
                    GUILayout.Label("NONE", bodyLabelStyle);
                }
                else
                {
                    GUILayout.Label(
                        passive.PassiveName.ToUpperInvariant(),
                        statRowStyle);
                    GUILayout.Label(
                        passive.Description.ToUpperInvariant(),
                        bodyLabelStyle);
                }

                if (stats != null)
                {
                    GUILayout.Space(10f);
                    GUILayout.Label("BUILD PROFILE", sectionLabelStyle);
                    GUILayout.Label($"POWER   {Mathf.RoundToInt(GetBuildPowerScore(loadout))}", statRowStyle);
                    GUILayout.Label($"DRAIN   {stats.TotalStaminaDrainRate:0.00}", statRowStyle);
                    GUILayout.Label($"MANA    {stats.ManaPoolSize:0} / {stats.ManaRegenRate:0.0}", statRowStyle);
                }
            }

            GUILayout.EndArea();
        }

        private void DrawOverallStatBars(Rect area, BeyStatBlock stats)
        {
            DrawRect(area, new Color(0f, 0f, 0f, 0.20f));
            DrawRect(new Rect(area.x, area.y, area.width, 2f), ACCENT_CYAN);
            DrawFittedLabel(new Rect(area.x + 8f, area.y + 6f, area.width - 16f, 22f), "OVERALL STATS", sectionLabelStyle, ACCENT_CYAN, 10);

            if (stats == null)
                return;

            string[] labels = { "ATTACK", "DEFENSE", "STAMINA", "AGILITY", "WEIGHT", "CONTROL", "ENERGY" };
            float[] values =
            {
                Mathf.Clamp01(stats.Attack / 100f),
                Mathf.Clamp01(stats.Defense / 100f),
                Mathf.Clamp01(stats.SpinRetention / 100f),
                Mathf.Clamp01((1.5f - Mathf.Clamp(stats.Weight / 55f, 0f, 1.5f)) * 0.35f + stats.JumpArcModifier / 1.5f * 0.25f + GetTipAgilityFactor(stats.TipBehavior) * 0.40f),
                Mathf.Clamp01(stats.Weight / 55f),
                Mathf.Clamp01((2.0f - stats.SlopeMultiplier) / 1.5f * 0.45f + (2.0f - stats.UphillResistanceMultiplier) / 1.7f * 0.35f + GetTipControlFactor(stats.TipBehavior) * 0.20f),
                Mathf.Clamp01(stats.ManaPoolSize / 150f * 0.65f + stats.ManaRegenRate / 32f * 0.35f)
            };

            float rowY = area.y + 34f;
            float rowH = Mathf.Clamp(24f * GetUiScale(), 22f, 30f);
            for (int i = 0; i < labels.Length; i++)
            {
                Rect row = new Rect(area.x + 8f, rowY + i * (rowH + 8f), area.width - 16f, rowH);
                DrawRect(new Rect(row.x, row.y + row.height * 0.55f, row.width, 6f), new Color(0.02f, 0.04f, 0.08f, 0.9f));
                DrawRect(new Rect(row.x, row.y + row.height * 0.55f, row.width * values[i], 6f), ACCENT_CYAN);
                DrawFittedLabel(new Rect(row.x, row.y, row.width * 0.55f, row.height * 0.7f), labels[i], bodyLabelStyle, Color.white, 10);
                DrawFittedLabel(new Rect(row.xMax - 56f, row.y, 56f, row.height * 0.7f), Mathf.RoundToInt(values[i] * 100f).ToString(), bodyLabelStyle, new Color(0.72f, 0.90f, 1f, 0.95f), 10);
            }
        }

        private static float GetTipAgilityFactor(TipBehaviorType tipBehavior)
        {
            switch (tipBehavior)
            {
                case TipBehaviorType.Flat:
                case TipBehaviorType.RubberFlat:
                    return 1f;
                case TipBehaviorType.Orbit:
                    return 0.82f;
                case TipBehaviorType.Round:
                case TipBehaviorType.Ball:
                    return 0.66f;
                case TipBehaviorType.Spike:
                case TipBehaviorType.Sharp:
                    return 0.42f;
                default:
                    return 0.55f;
            }
        }

        private static float GetTipControlFactor(TipBehaviorType tipBehavior)
        {
            switch (tipBehavior)
            {
                case TipBehaviorType.Spike:
                case TipBehaviorType.Sharp:
                    return 1f;
                case TipBehaviorType.Ball:
                case TipBehaviorType.Orbit:
                    return 0.72f;
                case TipBehaviorType.Round:
                    return 0.58f;
                case TipBehaviorType.Flat:
                case TipBehaviorType.RubberFlat:
                    return 0.40f;
                default:
                    return 0.55f;
            }
        }

        private void DrawPartSprite(Rect rect, BeyPart part)
        {
            if (part == null)
                return;

            Sprite sprite = part.Icon != null ? part.Icon : part.FaceBoltEmblem;
            if (sprite != null)
            {
                DrawSprite(rect, sprite);
                return;
            }

            DrawRect(rect, new Color(part.PrimaryColor.r, part.PrimaryColor.g, part.PrimaryColor.b, 0.60f));
            DrawFrameCorners(rect, new Color(part.SecondaryColor.r, part.SecondaryColor.g, part.SecondaryColor.b, 0.80f), rect.width * 0.34f, 2f);
        }

        private void DrawRarityPill(Rect rect, RarityTier rarity, string label)
        {
            Color rarityColor = GetRarityColor(rarity);
            DrawRect(rect, new Color(0.02f, 0.05f, 0.10f, 0.85f));
            DrawFrameCorners(rect, rarityColor, rect.width * 0.28f, 1.5f);
            DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), rarityColor);
            DrawFittedLabel(rect, label, bodyLabelStyle, rarityColor, 8);
        }

        private static Color GetRarityColor(RarityTier rarity)
        {
            switch (rarity)
            {
                case RarityTier.Common:
                    return new Color(0.000f, 0.490f, 0.800f, 1f); // #007DCC Bright Teal Blue
                case RarityTier.Uncommon:
                    return new Color(0.294f, 0.800f, 0.000f, 1f); // #4BCC00 Lime Green
                case RarityTier.Rare:
                    return new Color(0.875f, 0.000f, 0.000f, 1f); // #DF0000 Racing Red
                case RarityTier.Epic:
                    return new Color(0.690f, 0.000f, 0.541f, 1f); // #B0008A Raspberry Plum
                case RarityTier.Legendary:
                    return new Color(1.000f, 0.725f, 0.000f, 1f); // #FFB900 Amber Flame
                default:
                    return new Color(0.000f, 0.490f, 0.800f, 1f);
            }
        }

        private static Color GetRarityColor(BladeSpinners.Core.AbilityRarity rarity)
        {
            switch (rarity)
            {
                case BladeSpinners.Core.AbilityRarity.Common:
                    return new Color(0.000f, 0.490f, 0.800f, 1f); // #007DCC Bright Teal Blue
                case BladeSpinners.Core.AbilityRarity.Uncommon:
                    return new Color(0.294f, 0.800f, 0.000f, 1f); // #4BCC00 Lime Green
                case BladeSpinners.Core.AbilityRarity.Rare:
                    return new Color(0.875f, 0.000f, 0.000f, 1f); // #DF0000 Racing Red
                case BladeSpinners.Core.AbilityRarity.Legendary:
                    return new Color(1.000f, 0.725f, 0.000f, 1f); // #FFB900 Amber Flame
                default:
                    return new Color(0.000f, 0.490f, 0.800f, 1f);
            }
        }

        private static Color GetRarityColor(PerkRarity rarity)
        {
            switch (rarity)
            {
                case PerkRarity.Common:
                    return new Color(0.000f, 0.490f, 0.800f, 1f); // #007DCC Bright Teal Blue
                case PerkRarity.Uncommon:
                    return new Color(0.294f, 0.800f, 0.000f, 1f); // #4BCC00 Lime Green
                case PerkRarity.Rare:
                    return new Color(0.875f, 0.000f, 0.000f, 1f); // #DF0000 Racing Red
                case PerkRarity.Epic:
                    return new Color(0.690f, 0.000f, 0.541f, 1f); // #B0008A Raspberry Plum
                case PerkRarity.Legendary:
                    return new Color(1.000f, 0.725f, 0.000f, 1f); // #FFB900 Amber Flame
                default:
                    return new Color(0.000f, 0.490f, 0.800f, 1f);
            }
        }

        private bool TopTabBtn(string label, Rect rect, bool active)
        {
            Color fill = active ? new Color(0.10f, 0.34f, 0.56f, 0.96f) : new Color(0.03f, 0.08f, 0.14f, 0.96f);
            DrawRect(rect, fill);
            DrawRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), active ? ACCENT_CYAN : new Color(1f, 1f, 1f, 0.08f));
            if (active)
                DrawMotionBandClipped(new Rect(rect.x + rect.width * 0.56f, rect.y, rect.width * 0.32f, rect.height), ACCENT_CYAN, 8f, 14f, 0.10f);
            DrawFrameCorners(rect, new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, active ? 0.60f : 0.25f), rect.width * 0.22f, 2f);
            DrawFittedLabel(rect, label, navButtonStyle, Color.white, 10);
            return WithButtonSound(GUI.Button(rect, GUIContent.none, GUIStyle.none));
        }

        private bool ActionBtn(string label, Rect rect, Color accent, bool hot, bool enabled = true)
        {
            DrawRect(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), Color.black);
            
            // For START RUN button, detect hover dynamically
            bool isStartRunHovering = label == "START RUN" && rect.Contains(Event.current.mousePosition);
            bool isHot = label == "START RUN" ? isStartRunHovering : hot;
            
            if (isHot && enabled && label == "START RUN")
            {
                // Draw smooth spectrum gradient from red → orange → yellow/orange
                float w = rect.width;
                float h = rect.height;
                int gradientSteps = 20;  // Smooth spectrum with many steps
                
                for (int i = 0; i < gradientSteps; i++)
                {
                    float t = i / (float)gradientSteps;  // 0 to 1
                    // Smooth spectrum: red (0.8, 0.1, 0) → orange (1, 0.4, 0.1) → yellow/orange (1, 0.5, 0.2)
                    float r = 0.8f + t * 0.2f;  // 0.8 to 1.0
                    float g = 0.1f + t * 0.4f;  // 0.1 to 0.5
                    float b = 0f + t * 0.2f;    // 0 to 0.2
                    
                    float stripX = rect.x + (w / gradientSteps) * i;
                    float stripW = w / gradientSteps;
                    DrawRect(new Rect(stripX, rect.y, stripW, h), new Color(r, g, b, 0.98f));
                }
            }
            else if (label == "START RUN")
            {
                // Normal state for START RUN: solid flame red background
                DrawRect(rect, new Color(0.8f, 0.15f, 0.0f, 0.96f)); // Flame red
            }
            else
            {
                Color fill = hot
                    ? new Color(accent.r * 0.85f, accent.g * 0.45f, accent.b * 0.20f, enabled ? 0.98f : 0.42f)
                    : new Color(0.04f, 0.08f, 0.14f, enabled ? 0.96f : 0.45f);
                DrawRect(rect, fill);
            }
            
            if (isHot && label == "START RUN")
                DrawMotionBandClipped(new Rect(rect.x + rect.width * 0.58f, rect.y, rect.width * 0.34f, rect.height), ACCENT_RED, 8f, 10f, 0.12f);
            else if (label == "START RUN")
                DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), new Color(1f, 0.3f, 0f, 0.98f)); // Flame red accent
            else
                DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), accent);
            DrawRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), enabled ? accent : new Color(accent.r, accent.g, accent.b, 0.30f));
            
            // Use consistent style for text to prevent movement, center both horizontally and vertically
            Color textColor = isHot ? new Color(1f, 1f, 0.25f, 1f) : Color.white;  // Flame yellow on hover, white in normal
            
            // Create a centered text rect - use startButtonStyle for START RUN (has MiddleCenter alignment)
            Rect textRect = rect;
            GUIStyle textStyle = label == "START RUN" ? startButtonStyle : navButtonStyle;
            DrawFittedLabel(textRect, label, textStyle, textColor, 10);
            return WithButtonSound(
                enabled && GUI.Button(rect, GUIContent.none, GUIStyle.none));
        }

        private static bool WithButtonSound(bool clicked)
        {
            if (clicked)
                SoundManager.PlayUi(SoundPaths.GuiButton);

            return clicked;
        }

        private void SaveCurrentBuildToSlot(int slotIndex)
        {
            savedBuildSlots[slotIndex] = CloneLoadout(selectedMainMenuLoadout);
            savedBuildNames[slotIndex] = GetBuildDisplayName(savedBuildSlots[slotIndex]);
            buildSlotPickerOpen = false;
            ShowTransientUiMessage($"Saved {savedBuildNames[slotIndex]} to slot {slotIndex + 1}.");
        }

        private void LoadBuildFromSlot(int slotIndex)
        {
            if (savedBuildSlots[slotIndex] == null)
            {
                ShowTransientUiMessage($"Slot {slotIndex + 1} is empty.");
                return;
            }

            ApplyLoadoutToMainMenu(savedBuildSlots[slotIndex]);
            buildSlotPickerOpen = false;
            ShowTransientUiMessage($"Loaded {savedBuildNames[slotIndex]}.");
        }

        private void AutoOptimizeCurrentBuild()
        {
            bool equippedAny = false;
            foreach (PartType type in Enum.GetValues(typeof(PartType)))
            {
                List<BeyPart> typeParts = GetOwnedParts(type);
                BeyPart bestPart = null;
                float bestScore = float.MinValue;
                for (int i = 0; i < typeParts.Count; i++)
                {
                    BeyPart candidate = typeParts[i];
                    float score = GetPartPowerScore(candidate);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPart = candidate;
                    }
                }

                if (bestPart != null)
                {
                    selectedMainMenuLoadout[type] = bestPart;
                    equippedAny = true;
                }
            }

            RefreshPreviewFromLoadout(selectedMainMenuLoadout);
            ShowTransientUiMessage("Auto optimize equipped the highest rated owned parts.");
            AutoSave();
            if (equippedAny)
                SoundManager.PlayUi(SoundPaths.GuiEquipPart);
        }

        private void EquipPartFromGarage(PartType slot, BeyPart part, bool useRunInventory, PlayerManager runPlayer)
        {
            if (part == null)
                return;

            if (useRunInventory && runPlayer != null)
            {
                runPlayer.EquipPart(part);
                RefreshPreviewFromLoadout(GetCurrentRunLoadout(runPlayer));
            }
            else
            {
                selectedMainMenuLoadout[slot] = part;
                RefreshPreviewFromLoadout(selectedMainMenuLoadout);
                AutoSave();
            }

            SoundManager.PlayUi(SoundPaths.GuiEquipPart);
        }

        private Dictionary<PartType, BeyPart> CloneLoadout(Dictionary<PartType, BeyPart> source)
        {
            Dictionary<PartType, BeyPart> clone = new Dictionary<PartType, BeyPart>();
            if (source == null)
                return clone;

            foreach (KeyValuePair<PartType, BeyPart> kv in source)
            {
                if (kv.Value != null)
                    clone[kv.Key] = kv.Value;
            }

            return clone;
        }

        private void ApplyLoadoutToMainMenu(Dictionary<PartType, BeyPart> source)
        {
            selectedMainMenuLoadout.Clear();
            foreach (KeyValuePair<PartType, BeyPart> kv in source)
            {
                if (kv.Value != null)
                    selectedMainMenuLoadout[kv.Key] = kv.Value;
            }

            BuildDefaultLoadout();
            RefreshPreviewFromLoadout(selectedMainMenuLoadout);
            AutoSave();
        }

        private float GetBuildPowerScore(Dictionary<PartType, BeyPart> loadout)
        {
            float total = 0f;
            if (loadout == null)
                return total;

            foreach (KeyValuePair<PartType, BeyPart> kv in loadout)
                total += GetPartPowerScore(kv.Value);
            return total;
        }

        private float GetPartPowerScore(BeyPart part)
        {
            if (part == null)
                return 0f;

            float rarityBoost = ((int)part.Rarity + 1) * 8f;
            switch (part.PartType)
            {
                case PartType.Tip:
                    return rarityBoost + (2.5f - part.BehaviorBasedStaminaDrainModifier) * 16f + part.UphillResistanceMultiplier * 10f + part.SlopeMultiplier * 8f;
                case PartType.Track:
                    return rarityBoost + part.TrackHeight * 20f + part.JumpArcModifier * 18f;
                case PartType.FusionWheel:
                    FusionWheelCombatProfile profile =
                        FusionWheelCombatProfile.FromPart(part);
                    return rarityBoost
                        + profile.Attack * 0.55f
                        + profile.Defense * 0.40f
                        + profile.SpinRetention * 0.25f;
                case PartType.EnergyRing:
                    BeyPassive passive =
                        EnergyRingPassiveResolver.Resolve(part);
                    float passiveScore = passive != null
                        ? ((int)passive.Rarity + 1) * 5f
                        : 0f;
                    return rarityBoost
                        + part.ManaPoolSize * 0.36f
                        + part.ManaRegenRate * 1.1f
                        + passiveScore;
                case PartType.FaceBolt:
                    return rarityBoost + (part.EquippedAbility != null ? part.EquippedAbility.ManaCost * 3f + ((int)part.EquippedAbility.Rarity + 1) * 10f : 18f);
                default:
                    return rarityBoost;
            }
        }

        private string GetBuildDisplayName(Dictionary<PartType, BeyPart> loadout)
        {
            if (loadout != null && loadout.TryGetValue(PartType.FaceBolt, out BeyPart faceBolt) && faceBolt != null)
                return PartDisplayNameFormatter.ToShortDisplayName(faceBolt);

            return "Starter Build";
        }

        private string GetFaceBoltDisplayName(Dictionary<PartType, BeyPart> loadout)
        {
            if (loadout != null && loadout.TryGetValue(PartType.FaceBolt, out BeyPart faceBolt) && faceBolt != null)
                return PartDisplayNameFormatter.ToShortDisplayName(faceBolt);

            return "None";
        }

        private void ShowTransientUiMessage(string message)
        {
            transientUiMessage = message ?? string.Empty;
            transientUiMessageUntil = Time.unscaledTime + 3f;
        }

        private void DrawTransientUiMessage(Rect rect)
        {
            if (string.IsNullOrWhiteSpace(transientUiMessage) || Time.unscaledTime > transientUiMessageUntil)
                return;

            float width = Mathf.Min(rect.width, 520f);
            Rect bubble = new Rect(rect.center.x - width * 0.5f, rect.y, width, rect.height);
            DrawRect(bubble, new Color(0.02f, 0.08f, 0.14f, 0.84f));
            DrawRect(new Rect(bubble.x, bubble.yMax - 2f, bubble.width, 2f), ACCENT_CYAN);
            DrawFittedLabel(bubble, transientUiMessage.ToUpperInvariant(), bodyLabelStyle, Color.white, 10);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  STATE MANAGEMENT
        // ══════════════════════════════════════════════════════════════════════════

        private void StartRun()
        {
            Debug.Log("[BladeSpinners] StartRun() called");
            int seed       = UnityEngine.Random.Range(1000, int.MaxValue);
            int enemyCount = UnityEngine.Random.Range(1, 3);  // First stage: 1-2 enemies
            RuntimeRunBuilder.RunProgression progression = RuntimeRunBuilder.CreateRunProgression(seed, 3, 3);

            if (fallbackMenuCamera != null)
            {
                Destroy(fallbackMenuCamera.gameObject);
                fallbackMenuCamera = null;
            }

            try
            {
                runContext  = RuntimeRunBuilder.BuildRandomTestRun(
                    selectedMainMenuLoadout,
                    ownedParts,
                    enemyParts,
                    seed,
                    enemyCount,
                    progression,
                    null);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BladeSpinners] StartRun failed: {e}");
                initFailed   = true;
                initErrorMsg = $"StartRun: {e.Message}";
                EnsureFallbackMenuCamera();
                return;
            }

            Debug.Log("[BladeSpinners] Run started successfully");
            runElapsedSeconds = 0f;
            arenaElapsedSeconds = 0f;
            arenasClearedThisRun = 0;
            hasActiveRun = true;
            runRecordSubmitted = false;
            rootState   = RootUiState.InRun;
            mainMenuPanel = MenuPanel.Home;
            pausePanel    = MenuPanel.Home;
            selectedInventorySlot = null;
            garageInspectSlot = PartType.EnergyRing;
            buildSlotPickerOpen = false;
            ResetPreviewRotationState();
            ApplySettingsToCameraController(runContext.CameraController);
            ApplySettingsToPlayer(runContext.Player);
            Time.timeScale = 1f;
            UpdateCursorState();
        }

        private void ReturnToMainMenu()
        {
            if (hasActiveRun && !runRecordSubmitted)
                RecordCurrentRun(false);

            Time.timeScale = 1f;
            RuntimeRunBuilder.ClearRunObjectsForMainMenu();
            hasActiveRun = false;
            rootState     = RootUiState.MainMenu;
            mainMenuPanel = MenuPanel.Home;
            pausePanel    = MenuPanel.Home;
            selectedInventorySlot = null;
            garageInspectSlot = PartType.EnergyRing;
            buildSlotPickerOpen = false;
            deathOverlayPreviewPrepared = false;
            lootTransferInitialized = false;
            lootEligibleParts       = null;
            lootSelectedFlags       = null;
            ResetPreviewRotationState();
            RefreshPreviewFromLoadout(selectedMainMenuLoadout);
            EnsureFallbackMenuCamera();
            UpdateCursorState();
        }

        private void TogglePause()
        {
            if (rootState == RootUiState.InRun)
            {
                rootState      = RootUiState.Paused;
                Time.timeScale = 0f;
                pausePanel     = MenuPanel.Home;
                ResetPreviewRotationState();
                UpdateCursorState();
            }
            else if (rootState == RootUiState.Paused)
            {
                rootState      = RootUiState.InRun;
                Time.timeScale = 1f;
                ResetPreviewRotationState();
                UpdateCursorState();
            }
            else if (rootState == RootUiState.BetweenArenas)
            {
                rootState = RootUiState.InRun;
                Time.timeScale = 1f;
                ResetPreviewRotationState();
                UpdateCursorState();
            }
        }

        private void SetMainMenuPanel(MenuPanel panel)
        {
            if (mainMenuPanel == panel)
                return;

            mainMenuPanel = panel;
            garageInspectSlot = PartType.EnergyRing;
            buildSlotPickerOpen = false;
            ResetPreviewRotationState();
        }

        private void SetPausePanel(MenuPanel panel)
        {
            if (pausePanel == panel)
                return;

            pausePanel = panel;
            garageInspectSlot = PartType.EnergyRing;
            ResetPreviewRotationState();
        }

        private void UpdateMusicSituation()
        {
            MusicSituation desired = ResolveMusicSituation();
            if (rootState == RootUiState.StartScreen)
            {
                if (requestedMusicSituation
                        == MusicSituation.StartScreen
                    && SoundManager.CurrentMusicSituation
                        == MusicSituation.StartScreen
                    && SoundManager.IsMusicPlaying)
                {
                    return;
                }

                requestedMusicSituation =
                    MusicSituation.StartScreen;
                SoundManager.PlayMusicSituation(
                    MusicSituation.StartScreen);
                return;
            }

            bool menuBrowsing =
                rootState == RootUiState.MainMenu
                && (desired == MusicSituation.MainMenu
                    || desired
                        == MusicSituation.Inventory);
            if (menuBrowsing)
            {
                bool requestChanged =
                    requestedMusicSituation != desired;
                requestedMusicSituation = desired;

                MusicSituation? playing =
                    SoundManager.CurrentMusicSituation;
                bool playingMenuMusic =
                    playing == MusicSituation.MainMenu
                    || playing
                        == MusicSituation.Inventory;
                if (!SoundManager.IsMusicPlaying
                    || !playingMenuMusic)
                {
                    SoundManager.PlayMusicSituation(
                        desired);
                    return;
                }

                if (requestChanged
                    || playing != desired)
                {
                    SoundManager.QueueMusicSituation(
                        desired);
                }
                return;
            }

            if (requestedMusicSituation == desired
                && SoundManager.CurrentMusicSituation == desired
                && SoundManager.IsMusicPlaying)
            {
                return;
            }

            requestedMusicSituation = desired;
            SoundManager.PlayMusicSituation(desired);
        }

        private MusicSituation ResolveMusicSituation()
        {
            if (rootState == RootUiState.StartScreen)
                return MusicSituation.StartScreen;

            bool inventoryOpen =
                (rootState == RootUiState.MainMenu
                    && mainMenuPanel == MenuPanel.Inventory)
                || ((rootState == RootUiState.Paused
                        || rootState == RootUiState.BetweenArenas)
                    && pausePanel == MenuPanel.Inventory);
            MatchManager match = runContext.Match;
            RuntimeRunBuilder.RunProgression progression =
                runContext.Progression;
            return DetermineMusicSituation(
                rootState == RootUiState.MainMenu,
                inventoryOpen,
                match != null
                    && match.CurrentState
                        == MatchManager.MatchState.PlayerWon,
                match != null
                    && match.CurrentState
                        == MatchManager.MatchState.PlayerLost,
                progression != null
                    ? progression.DepthIndex
                    : -1);
        }

        public static MusicSituation DetermineMusicSituation(
            bool isMainMenu,
            bool inventoryOpen,
            bool playerWon,
            bool playerLost,
            int depthIndex)
        {
            if (inventoryOpen)
                return MusicSituation.Inventory;
            if (isMainMenu)
                return MusicSituation.MainMenu;
            if (playerLost)
                return MusicSituation.Lose;
            if (playerWon)
                return MusicSituation.Victory;
            return depthIndex + 1
                    == GameConstants.BOSS_MAP_DEPTH
                ? MusicSituation.BossBattle
                : MusicSituation.Battle;
        }

        private void ResetPreviewRotationState()
        {
            previewManualPitch = 0f;
            previewIsDragging = false;
            previewDragPointerId = -1;
            ApplyPreviewManualRotation();
            previewRenderQueued = true;
        }

        private void UpdateCursorState()
        {
            bool lockCursor = rootState == RootUiState.InRun
                && runContext.Match != null
                && runContext.Match.CurrentState == MatchManager.MatchState.InProgress;

            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
        }

        private void HandleRunProgressionAdvance()
        {
            if (rootState != RootUiState.InRun || runContext.Match == null)
                return;

            if (runContext.Match.CurrentState != MatchManager.MatchState.PlayerWon)
                return;

            // Check if player wants to skip early after having watched the initial victory burst
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            var gp = UnityEngine.InputSystem.Gamepad.current;
            bool skipPressed = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                || (mouse != null && mouse.leftButton.wasPressedThisFrame)
                || (gp != null && (gp.aButton.wasPressedThisFrame || gp.buttonSouth.wasPressedThisFrame));

            // Victory celebration runs for 5.2s, or can be advanced after 1.5s if skip input is pressed
            if (runContext.Match.StateTimer > 0f)
            {
                if (!skipPressed || runContext.Match.StateTimer > 3.7f)
                    return;
            }

            RuntimeRunBuilder.RunProgression progression =
                runContext.Progression;
            if (progression != null)
            {
                arenasClearedThisRun = Mathf.Max(
                    arenasClearedThisRun,
                    progression.DepthIndex + 1);
                if (progression.IsLastArena)
                    RecordCurrentRun(true);
            }

            rootState = RootUiState.BetweenArenas;
            pausePanel = MenuPanel.Shrine;
            Time.timeScale = 0f;
            ResetPreviewRotationState();
            UpdateCursorState();
        }

        private void AdvanceToNextArenaOrFinishRun()
        {
            RuntimeRunBuilder.RunProgression progression = runContext.Progression;
            if (progression == null)
            {
                ReturnToMainMenu();
                return;
            }

            if (!progression.TryAdvance())
            {
                Debug.Log("[BladeSpinners] Run complete. Transferring all loot and unlocking blessings.");
                List<BeyPart> allRunParts = runContext.Player?.GetRunInventory()?.GetAllParts() ?? new List<BeyPart>();
                TransferPartsToMainInventory(allRunParts);

                // Record run win to increase difficulty progression milestones
                RunDifficultyManager.RecordRunWin();

                // Roll 3 to 6 new shrine blessings on winning the entire run!
                lastRunUnlockedBlessings = ShrineBlessingsUnlockManager.OnRunWon(3, 6);
                showRunVictoryModal = lastRunUnlockedBlessings != null && lastRunUnlockedBlessings.Count > 0;

                ReturnToMainMenu();
                return;
            }

            Dictionary<PartType, BeyPart> nextLoadout = GetCurrentRunLoadout(runContext.Player);
            List<BeyPart> carriedInventory = runContext.Player?.GetRunInventory()?.GetAllParts() ?? new List<BeyPart>();
            BladerShrineRunState carriedShrine = runContext.ShrineState;
            int enemyCount = UnityEngine.Random.Range(2, 5);
            Time.timeScale = 1f;

            runContext = RuntimeRunBuilder.BuildRandomTestRun(
                nextLoadout,
                ownedParts,
                enemyParts,
                progression.RunSeed,
                enemyCount,
                progression,
                carriedInventory,
                carriedShrine);

            arenaElapsedSeconds = 0f;
            rootState = RootUiState.InRun;
            pausePanel = MenuPanel.Home;
            selectedInventorySlot = null;
            deathOverlayPreviewPrepared = false;
            ApplySettingsToCameraController(runContext.CameraController);
            ApplySettingsToPlayer(runContext.Player);
            UpdateCursorState();
        }

        private void UpdateRunTimersAndRecords()
        {
            if (!hasActiveRun || runContext.Match == null)
                return;

            MatchManager.MatchState state =
                runContext.Match.CurrentState;
            if (rootState == RootUiState.InRun
                && state == MatchManager.MatchState.InProgress)
            {
                float delta = Mathf.Max(
                    0f,
                    Time.unscaledDeltaTime);
                runElapsedSeconds += delta;
                arenaElapsedSeconds += delta;
            }

            if (state == MatchManager.MatchState.PlayerLost
                && !runRecordSubmitted)
            {
                RecordCurrentRun(false);
            }
        }

        private void RecordCurrentRun(bool completed)
        {
            if (!hasActiveRun || runRecordSubmitted)
                return;

            RuntimeRunBuilder.RunProgression progression =
                runContext.Progression;
            if (progression == null)
                return;

            if (completed)
            {
                arenasClearedThisRun = Mathf.Max(
                    arenasClearedThisRun,
                    progression.TotalArenaCount);
            }

            RunRecordStore.Record(
                runElapsedSeconds,
                arenasClearedThisRun,
                progression.TotalArenaCount,
                progression.RunSeed,
                completed);
            runRecordSubmitted = true;
            Debug.Log(
                $"[RunRecords] Recorded {(completed ? "completed" : "ended")} " +
                $"run: {arenasClearedThisRun}/{progression.TotalArenaCount} arenas, " +
                $"{FormatRunTime(runElapsedSeconds)}.");
        }

        private static string FormatRunTime(float seconds)
        {
            int totalCentiseconds = Mathf.Max(
                0,
                Mathf.FloorToInt(seconds * 100f));
            int hours = totalCentiseconds / 360000;
            int minutes =
                totalCentiseconds / 6000 % 60;
            int wholeSeconds =
                totalCentiseconds / 100 % 60;
            int centiseconds = totalCentiseconds % 100;
            return hours > 0
                ? $"{hours:00}:{minutes:00}:{wholeSeconds:00}.{centiseconds:00}"
                : $"{minutes:00}:{wholeSeconds:00}.{centiseconds:00}";
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  DATA HELPERS
        // ══════════════════════════════════════════════════════════════════════════

        private void AutoSave()
        {
            SaveManager.Save(ownedParts, selectedMainMenuLoadout);
        }

        private void BuildDefaultLoadout()
        {
            foreach (PartType type in Enum.GetValues(typeof(PartType)))
            {
                if (selectedMainMenuLoadout.TryGetValue(type, out BeyPart existing) && existing != null)
                    continue;
                List<BeyPart> typeParts = GetOwnedParts(type);
                selectedMainMenuLoadout[type] = typeParts.Count > 0
                    ? typeParts[0]
                    : RuntimePartFactory.CreateTemporaryPart(type, Environment.TickCount + (int)type * 777);
            }
        }

        private void BuildStarterData()
        {
            StarterPartsConfig starterConfig = LoadStarterConfig();
            selectedMainMenuLoadout.Clear();

            if (starterConfig != null)
            {
                ownedParts = starterConfig.GetOwnedStarterParts();

                Dictionary<PartType, BeyPart> explicitBase = starterConfig.GetExplicitStarterBaseLoadout();
                foreach (KeyValuePair<PartType, BeyPart> kv in explicitBase)
                {
                    if (kv.Value == null)
                        continue;

                    selectedMainMenuLoadout[kv.Key] = kv.Value;
                    if (!ownedParts.Contains(kv.Value))
                        ownedParts.Add(kv.Value);
                }

                Dictionary<PartType, BeyPart> preferredBase = starterConfig.GetPreferredStarterBaseLoadout(ownedParts);
                foreach (KeyValuePair<PartType, BeyPart> kv in preferredBase)
                {
                    if (kv.Value != null && !selectedMainMenuLoadout.ContainsKey(kv.Key))
                        selectedMainMenuLoadout[kv.Key] = kv.Value;
                }

                foreach (PartType type in Enum.GetValues(typeof(PartType)))
                {
                    BeyPart configured = starterConfig.GetStarterLoadoutPart(type);
                    if (configured != null && !explicitBase.ContainsKey(type))
                    {
                        selectedMainMenuLoadout[type] = configured;
                        if (!ownedParts.Contains(configured))
                            ownedParts.Add(configured);
                    }
                }

                enemyParts = starterConfig.GetEnemyPartPool(ownedParts);
                List<BeyPart> fullCatalog = starterConfig.GetRuntimePartCatalog();
                for (int i = 0; i < fullCatalog.Count; i++)
                {
                    BeyPart part = fullCatalog[i];
                    if (part != null && !enemyParts.Contains(part))
                        enemyParts.Add(part);
                }
            }
            else
            {
                Debug.LogWarning("[BladeSpinners] StarterPartsConfig not found at Resources/StarterPartsConfig.asset. Falling back to generated starter catalog.");
            }

            if (ownedParts == null || ownedParts.Count == 0)
                ownedParts = RuntimePartFactory.CreateStarterCatalog(1, Environment.TickCount);

            if (enemyParts == null || enemyParts.Count == 0)
                enemyParts = new List<BeyPart>(ownedParts);

            // Load saved data (owned parts + loadout) from disk
            List<BeyPart> allKnown = new List<BeyPart>(ownedParts);
            for (int i = 0; i < enemyParts.Count; i++)
                if (enemyParts[i] != null && !allKnown.Contains(enemyParts[i]))
                    allKnown.Add(enemyParts[i]);

            if (SaveManager.TryLoad(allKnown, out List<BeyPart> savedOwned, out Dictionary<PartType, BeyPart> savedLoadout))
            {
                // Merge: start from saved parts, add any starter parts not already present
                List<BeyPart> starterOwned = ownedParts;
                ownedParts = savedOwned;
                for (int i = 0; i < starterOwned.Count; i++)
                    if (starterOwned[i] != null && !ownedParts.Contains(starterOwned[i]))
                        ownedParts.Add(starterOwned[i]);

                // Apply saved loadout
                selectedMainMenuLoadout.Clear();
                foreach (KeyValuePair<PartType, BeyPart> kv in savedLoadout)
                    if (kv.Value != null)
                        selectedMainMenuLoadout[kv.Key] = kv.Value;

                Debug.Log($"[SaveManager] Loaded save: {savedOwned.Count} parts, loadout restored.");
            }

            BuildDefaultLoadout();
        }

        private StarterPartsConfig LoadStarterConfig()
        {
            StarterPartsConfig starterConfig = Resources.Load<StarterPartsConfig>(StarterConfigResourcePath);

#if UNITY_EDITOR
            bool needsAutoRepair = starterConfig == null
                || !starterConfig.HasCompleteExplicitBaseLoadout()
                || !starterConfig.HasRuntimePartCatalog();
            if (needsAutoRepair)
            {
                StarterPartsConfig repaired;
                if (StarterPartsConfig.TryEnsureResourcesConfig(out repaired) && repaired != null)
                {
                    starterConfig = repaired;
                    Debug.Log("[BladeSpinners] Auto-created or repaired Resources/StarterPartsConfig.asset from project part assets.");
                }
            }
#endif

            return starterConfig;
        }

        private List<BeyPart> GetOwnedParts(PartType type)
        {
            List<BeyPart> list = new List<BeyPart>();
            for (int i = 0; i < ownedParts.Count; i++)
            {
                BeyPart part = ownedParts[i];
                if (part != null && part.PartType == type) list.Add(part);
            }
            return list;
        }

        private static List<BeyPart> GetPartsByType(List<BeyPart> source, PartType type)
        {
            List<BeyPart> list = new List<BeyPart>();
            if (source == null)
                return list;

            for (int i = 0; i < source.Count; i++)
            {
                BeyPart part = source[i];
                if (part != null && part.PartType == type)
                    list.Add(part);
            }

            return list;
        }

        private Dictionary<PartType, BeyPart> GetCurrentRunLoadout(PlayerManager player)
        {
            Dictionary<PartType, BeyPart> map = new Dictionary<PartType, BeyPart>();
            if (player == null || player.BeyConfiguration == null) return map;
            foreach (PartType type in Enum.GetValues(typeof(PartType)))
                map[type] = player.BeyConfiguration.GetEquippedPart(type);
            return map;
        }

        private BeyStatBlock GetStatsForDisplay(PlayerManager player)
        {
            if (player?.BeyConfiguration != null) return player.BeyConfiguration.GetStatBlock();
            return previewConfig?.GetStatBlock();
        }

        private float GetCurrentSpinForDisplay(PlayerManager player)
        {
            if (player?.BeyConfiguration != null) return player.BeyConfiguration.CurrentSpin;
            return previewConfig?.CurrentSpin ?? 0f;
        }

        private float GetCurrentManaForDisplay(PlayerManager player)
        {
            if (player?.BeyConfiguration != null) return player.BeyConfiguration.CurrentMana;
            return previewConfig?.CurrentMana ?? 0f;
        }

        private float GetMaxManaForDisplay(PlayerManager player)
        {
            if (player?.BeyConfiguration != null)
                return player.BeyConfiguration.MaxMana;
            return previewConfig?.MaxMana ?? 0f;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  PREVIEW BEY
        // ══════════════════════════════════════════════════════════════════════════

        private void EnsurePreviewSetup()
        {
            if (previewCamera != null) return;

            GameObject root = new GameObject("__PreviewRoot");
            DontDestroyOnLoad(root);
            root.transform.position = new Vector3(5000f, 5000f, 5000f);

            GameObject tiltPivot = new GameObject("TiltPivot");
            tiltPivot.transform.SetParent(root.transform, false);
            previewTiltPivot = tiltPivot.transform;
            previewSpinChild = new GameObject("SpinChild").transform;
            previewSpinChild.SetParent(tiltPivot.transform, false);
            previewSpinChild.localScale = Vector3.one * 2.75f;

            previewConfig    = new BeyConfiguration();
            previewAssembler = root.AddComponent<BeyAssembler>();
            typeof(BeyAssembler).GetField("beyModelTransform", flags)?.SetValue(previewAssembler, previewSpinChild);
            previewAssembler.SetConfiguration(previewConfig);
            previewConfig.SetSpin(GameConstants.MAX_SPIN);

            GameObject cameraObj = new GameObject("__PreviewCamera");
            DontDestroyOnLoad(cameraObj);
            previewCamera = cameraObj.AddComponent<Camera>();
            previewCamera.transform.position = root.transform.position + new Vector3(0f, 0.56f, -0.92f);
            previewCamera.transform.LookAt(root.transform.position + new Vector3(0f, 0.18f, 0f));
            previewCamera.clearFlags     = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            previewCamera.enabled        = false;

            previewTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            previewCamera.targetTexture = previewTexture;
            ApplyPreviewManualRotation();

            // Per-part preview setup
            EnsurePartPreviewSetup();
        }

        private void EnsurePartPreviewSetup()
        {
            if (partPreviewCamera != null) return;

            // Root placed far away from main scene and bey preview
            GameObject root = new GameObject("__PartPreviewRoot");
            DontDestroyOnLoad(root);
            root.transform.position = new Vector3(6000f, 5000f, 5000f);
            partPreviewRoot = root.transform;

            // One camera, positioned to see a single part clearly
            GameObject camObj = new GameObject("__PartPreviewCamera");
            DontDestroyOnLoad(camObj);
            partPreviewCamera = camObj.AddComponent<Camera>();
            partPreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            partPreviewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            partPreviewCamera.enabled = false;
            partPreviewCamera.nearClipPlane = 0.01f;
            partPreviewCamera.farClipPlane = 10f;

            // Create a RenderTexture and mesh holder per part type
            foreach (PartType type in Enum.GetValues(typeof(PartType)))
            {
                RenderTexture rt = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
                partPreviewTextures[type] = rt;

                GameObject holder = new GameObject($"PartPreview_{type}");
                holder.transform.SetParent(root.transform, false);
                holder.AddComponent<MeshFilter>();
                MeshRenderer mr = holder.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                holder.SetActive(false);
                partPreviewObjects[type] = holder;
            }
        }

        private void RenderPartPreviews()
        {
            if (partPreviewCamera == null) return;

            foreach (PartType type in Enum.GetValues(typeof(PartType)))
            {
                if (!partPreviewObjects.ContainsKey(type) || !partPreviewTextures.ContainsKey(type))
                    continue;
                if (type == PartType.FaceBolt) continue;

                GameObject holder = partPreviewObjects[type];
                MeshFilter mf = holder.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                {
                    holder.SetActive(false);
                    continue;
                }

                // Activate only this holder
                foreach (var kvp in partPreviewObjects)
                    kvp.Value.SetActive(kvp.Key == type);

                // Position camera to frame this part's mesh
                Bounds bounds = mf.sharedMesh.bounds;
                float meshHeight = bounds.size.y;
                float meshWidth = Mathf.Max(bounds.size.x, bounds.size.z);
                float extent = Mathf.Max(meshHeight, meshWidth) * 0.5f;
                if (extent < 0.01f) extent = 0.1f;

                Vector3 meshCenter = partPreviewRoot.position + holder.transform.localPosition + bounds.center;
                float cameraDistance = extent * 2.0f;

                partPreviewCamera.transform.position = meshCenter + new Vector3(0f, extent * 0.2f, -cameraDistance);
                partPreviewCamera.transform.LookAt(meshCenter);
                partPreviewCamera.targetTexture = partPreviewTextures[type];
                partPreviewCamera.Render();
            }

            // Deactivate all after rendering
            foreach (var kvp in partPreviewObjects)
                kvp.Value.SetActive(false);

            partPreviewsDirty = false;
        }

        private void RenderSwapPreviews()
        {
            if (partPreviewCamera == null || swapPreviewQueue == null) return;

            PartType slot = lastRenderedSwapSlot ?? PartType.Tip;
            if (slot == PartType.FaceBolt) { swapPreviewsDirty = false; return; }
            if (!partPreviewObjects.ContainsKey(slot)) { swapPreviewsDirty = false; return; }

            GameObject holder = partPreviewObjects[slot];
            MeshFilter mf = holder.GetComponent<MeshFilter>();
            MeshRenderer mr = holder.GetComponent<MeshRenderer>();
            Shader shader = ShaderProvider.URPLit;

            foreach (var kvp in partPreviewObjects)
                kvp.Value.SetActive(false);
            holder.SetActive(true);

            foreach (BeyPart part in swapPreviewQueue)
            {
                if (part == null) continue;
                int id = part.GetInstanceID();
                if (swapPartPreviewCache.ContainsKey(id)) continue;

                Mesh mesh = ProceduralPartMeshGenerator.GenerateMesh(part);
                mf.sharedMesh = mesh;
                if (shader != null)
                {
                    Material mat = new Material(shader);
                    Color partColor = part.PrimaryColor;
                    if (part.PartType == PartType.EnergyRing)
                        partColor.a = 0.56f;
                    mat.color = partColor;
                    if (part.PartType == PartType.FusionWheel && mat.HasProperty("_Metallic"))
                        mat.SetFloat("_Metallic", 1f);
                    if (part.PartType == PartType.FusionWheel && mat.HasProperty("_Smoothness"))
                        mat.SetFloat("_Smoothness", 0.92f);
                    if (part.PartType == PartType.EnergyRing)
                    {
                        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
                        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
                        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    }
                    mr.sharedMaterial = mat;
                }

                RenderTexture rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
                swapPartPreviewCache[id] = rt;

                Bounds bounds = mesh.bounds;
                float extent = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) * 0.5f;
                if (extent < 0.01f) extent = 0.1f;
                Vector3 meshCenter = partPreviewRoot.position + bounds.center;
                float camDist = extent * 2.0f;

                partPreviewCamera.transform.position = meshCenter + new Vector3(0f, extent * 0.2f, -camDist);
                partPreviewCamera.transform.LookAt(meshCenter);
                partPreviewCamera.targetTexture = rt;
                partPreviewCamera.Render();
            }

            holder.SetActive(false);
            swapPreviewsDirty = false;
        }

        private void RefreshPartPreviewMeshes(Dictionary<PartType, BeyPart> loadout)
        {
            if (partPreviewRoot == null || loadout == null) return;

            Shader shader = ShaderProvider.URPLit;

            foreach (PartType type in Enum.GetValues(typeof(PartType)))
            {
                if (!partPreviewObjects.ContainsKey(type)) continue;
                if (type == PartType.FaceBolt) continue; // FaceBolt uses emblem sprite

                GameObject holder = partPreviewObjects[type];
                MeshFilter mf = holder.GetComponent<MeshFilter>();
                MeshRenderer mr = holder.GetComponent<MeshRenderer>();

                loadout.TryGetValue(type, out BeyPart part);
                if (part == null)
                {
                    mf.sharedMesh = null;
                    holder.SetActive(false);
                    continue;
                }

                Mesh mesh = ProceduralPartMeshGenerator.GenerateMesh(part);
                mf.sharedMesh = mesh;

                if (shader != null)
                {
                    Material mat = new Material(shader);
                    Color partColor = part.PrimaryColor;
                    if (type == PartType.EnergyRing)
                        partColor.a = 0.56f;
                    mat.color = partColor;
                    if (type == PartType.FusionWheel && mat.HasProperty("_Metallic"))
                        mat.SetFloat("_Metallic", 1f);
                    if (type == PartType.FusionWheel && mat.HasProperty("_Smoothness"))
                        mat.SetFloat("_Smoothness", 0.92f);
                    if (type == PartType.EnergyRing)
                    {
                        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
                        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
                        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    }
                    mr.sharedMaterial = mat;
                }

                holder.transform.localPosition = Vector3.zero;
                holder.transform.localRotation = Quaternion.identity;
                holder.transform.localScale = Vector3.one;
            }

            partPreviewsDirty = true;
        }

        private void EnsureFallbackMenuCamera()
        {
            if (rootState != RootUiState.MainMenu
                && rootState != RootUiState.StartScreen)
                return;

            Camera existingMain = Camera.main;
            if (existingMain != null)
            {
                fallbackMenuCamera = existingMain;
                EnsureMenuAudioListener(fallbackMenuCamera);
                // Ensure sensible clear settings for the menu background
                fallbackMenuCamera.clearFlags = CameraClearFlags.SolidColor;
                fallbackMenuCamera.backgroundColor = new Color(0.03f, 0.03f, 0.04f, 1f);
                return;
            }

            if (fallbackMenuCamera != null)
            {
                EnsureMenuAudioListener(fallbackMenuCamera);
                return;
            }

            Debug.Log("[BladeSpinners] No Camera.main found, creating fallback camera");
            GameObject cameraObject = new GameObject("__MenuCamera");
            DontDestroyOnLoad(cameraObject);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.03f, 0.04f, 1f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;
            camera.depth = -100f;
            EnsureMenuAudioListener(camera);

            fallbackMenuCamera = camera;
        }

        private static void EnsureMenuAudioListener(
            Camera camera)
        {
            if (camera == null
                || FindFirstObjectByType<AudioListener>() != null)
            {
                return;
            }

            camera.gameObject.AddComponent<AudioListener>();
        }

        private void RefreshPreviewFromLoadout(Dictionary<PartType, BeyPart> loadout)
        {
            if (previewAssembler == null || loadout == null) return;

            int loadoutHash = ComputeLoadoutHash(loadout);
            if (loadoutHash == previewLoadoutHash)
                return;

            foreach (PartType type in Enum.GetValues(typeof(PartType)))
                previewConfig.UnequipPart(type);
            foreach (KeyValuePair<PartType, BeyPart> kv in loadout)
                if (kv.Value != null) previewAssembler.EquipPart(kv.Value);

            previewLoadoutHash = loadoutHash;
            previewRenderQueued = true;
            RefreshPartPreviewMeshes(loadout);
        }

        private bool ShouldRenderPreviewThisFrame()
        {
            if (rootState == RootUiState.MainMenu
                || rootState == RootUiState.Paused
                || rootState == RootUiState.BetweenArenas)
                return true;

            if (rootState == RootUiState.InRun
                && runContext.Match != null
                && runContext.Match.CurrentState == MatchManager.MatchState.PlayerLost)
                return true;

            return false;
        }

        private static int ComputeLoadoutHash(Dictionary<PartType, BeyPart> loadout)
        {
            unchecked
            {
                int hash = 17;
                foreach (PartType type in Enum.GetValues(typeof(PartType)))
                {
                    hash = hash * 31 + (int)type;
                    loadout.TryGetValue(type, out BeyPart part);
                    hash = hash * 31 + (part != null ? part.GetInstanceID() : 0);
                }

                return hash;
            }
        }

        private void HandlePreviewDragInput(Rect previewRect)
        {
            Event currentEvent = Event.current;
            if (currentEvent == null)
                return;

            if (currentEvent.type == EventType.MouseDown
                && currentEvent.button == 0
                && previewRect.Contains(currentEvent.mousePosition))
            {
                previewIsDragging = true;
                previewDragPointerId = currentEvent.button;
                previewLastPointerPos = currentEvent.mousePosition;
                currentEvent.Use();
                return;
            }

            if (!previewIsDragging)
                return;

            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == previewDragPointerId)
            {
                Vector2 delta = currentEvent.mousePosition - previewLastPointerPos;
                previewLastPointerPos = currentEvent.mousePosition;

                // Lock rotation exclusively to pitch tilt (up and down)
                previewManualPitch = Mathf.Clamp(previewManualPitch - delta.y * 0.35f, -38f, 38f);

                ApplyPreviewManualRotation();
                previewRenderQueued = true;
                currentEvent.Use();
                return;
            }

            if (currentEvent.rawType == EventType.MouseUp || currentEvent.type == EventType.MouseLeaveWindow)
            {
                previewIsDragging = false;
                previewDragPointerId = -1;
            }
        }

        private void ApplyPreviewManualRotation()
        {
            if (previewTiltPivot == null)
                return;

            previewTiltPivot.localRotation = Quaternion.Euler(previewManualPitch, 0f, 0f);
        }

        private float DrawThemedSlider(string label, float value, float minValue, float maxValue)
        {
            GUILayout.Label($"{label}   {value:0.00}", bodyLabelStyle);
            Rect rect = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));
            DrawRect(new Rect(rect.x, rect.y + 8f, rect.width, 6f), new Color(0f, 0f, 0f, 0.85f));
            float normalized = Mathf.InverseLerp(minValue, maxValue, value);
            DrawRect(new Rect(rect.x, rect.y + 8f, rect.width * normalized, 6f), ACCENT_YEL);
            return GUI.HorizontalSlider(rect, value, minValue, maxValue, sliderTrackStyle, sliderThumbStyle);
        }

        private void LoadAudioSettings()
        {
            AudioMixLevels levels =
                AudioMixPreferences.Load();
            settingsMasterVolume = levels.Master;
            settingsSoundEffectsVolume = levels.SoundEffects;
            settingsMusicVolume = levels.Music;
            settingsGuiVolume = levels.Gui;
            ApplyAudioSettings(false);
        }

        private void ApplyAudioSettings(bool persist)
        {
            SoundManager.SetMixVolumes(
                settingsMasterVolume,
                settingsSoundEffectsVolume,
                settingsMusicVolume,
                settingsGuiVolume,
                persist);
        }

        private void ApplySettingsToCameraController(ThirdPersonCameraController cameraController)
        {
            if (cameraController == null)
                return;

            cameraController.SetOccluderOpacity(settingsClippingOpacity);
        }

        private void ApplySettingsToPlayer(PlayerManager player)
        {
            if (player == null || player.StatRingsUI == null)
                return;

            player.StatRingsUI.SetUiOpacity(settingsRingsOpacity);
        }

        private static GUIStyle FitLabelStyle(GUIStyle source, string text, float maxWidth, int minFontSize, float maxHeight = float.PositiveInfinity)
        {
            GUIStyle fitted = new GUIStyle(source)
            {
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            if (string.IsNullOrEmpty(text) || maxWidth <= 0f)
                return fitted;

            while (fitted.fontSize > minFontSize && fitted.CalcSize(new GUIContent(text)).x > maxWidth)
                fitted.fontSize--;

            while (fitted.fontSize > minFontSize && fitted.CalcHeight(new GUIContent(text), maxWidth) > maxHeight)
                fitted.fontSize--;

            return fitted;
        }

        private static string GetLongestPartLabel(IReadOnlyList<BeyPart> parts)
        {
            if (parts == null || parts.Count == 0)
                return "KILLER BUILD";

            string longest = "KILLER BUILD";
            for (int i = 0; i < parts.Count; i++)
            {
                BeyPart part = parts[i];
                if (part == null)
                    continue;

                string label = PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant();
                if (label.Length > longest.Length)
                    longest = label;
            }

            return longest;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  DRAW HELPERS
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>Draws a coloured rectangle using GUI.DrawTexture.</summary>
        private static void DrawRect(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        /// <summary>Draws a tall, thin rectangle rotated around its horizontal centre.</summary>
        private static void DrawDiagonalStripe(float xCenter, float width, Color color, float angleDeg)
        {
            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(angleDeg, new Vector2(xCenter, UiHeight * 0.5f));
            DrawRect(new Rect(xCenter - width * 0.5f, -200f, width, UiHeight + 400f), color);
            GUI.matrix = saved;
        }

        private static void DrawFrameCorners(Rect rect, Color color, float length, float thickness)
        {
            DrawRect(new Rect(rect.x, rect.y, length, thickness), color);
            DrawRect(new Rect(rect.x, rect.y, thickness, length), color);
            DrawRect(new Rect(rect.xMax - length, rect.y, length, thickness), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, length), color);
            DrawRect(new Rect(rect.x, rect.yMax - thickness, length, thickness), color);
            DrawRect(new Rect(rect.x, rect.yMax - length, thickness, length), color);
            DrawRect(new Rect(rect.xMax - length, rect.yMax - thickness, length, thickness), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.yMax - length, thickness, length), color);
        }

        private static void DrawMotionBand(Rect rect, Color lineColor, float stripeWidth, float gap, float alpha)
        {
            stripeWidth = Mathf.Max(3f, stripeWidth);
            gap = Mathf.Max(4f, gap);

            for (float x = rect.x - rect.height; x < rect.xMax + rect.height; x += stripeWidth + gap)
            {
                Matrix4x4 saved = GUI.matrix;
                GUIUtility.RotateAroundPivot(-22f, new Vector2(x, rect.center.y));
                DrawRect(new Rect(x, rect.y - rect.height, stripeWidth, rect.height * 3f), new Color(lineColor.r, lineColor.g, lineColor.b, alpha));
                GUI.matrix = saved;
            }
        }

        private static void DrawMotionBandClipped(Rect rect, Color lineColor, float stripeWidth, float gap, float alpha)
        {
            stripeWidth = Mathf.Max(3f, stripeWidth);
            gap = Mathf.Max(4f, gap);

            GUI.BeginGroup(rect);
            Rect localRect = new Rect(0f, 0f, rect.width, rect.height);
            for (float x = -localRect.height; x < localRect.xMax + localRect.height; x += stripeWidth + gap)
            {
                Matrix4x4 saved = GUI.matrix;
                GUIUtility.RotateAroundPivot(-22f, new Vector2(x, localRect.center.y));
                DrawRect(new Rect(x, -localRect.height, stripeWidth, localRect.height * 3f), new Color(lineColor.r, lineColor.g, lineColor.b, alpha));
                GUI.matrix = saved;
            }
            GUI.EndGroup();
        }

        private static void DrawArenaBurstMotif(Rect rect)
        {
            float motifSize = Mathf.Min(rect.width, rect.height);
            Rect core = new Rect(rect.x + rect.width * 0.36f, rect.y - motifSize * 0.08f, motifSize * 0.28f, motifSize * 0.28f);
            DrawRect(new Rect(core.x, core.center.y - 2f, core.width, 4f), new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.24f));
            DrawRect(new Rect(core.center.x - 2f, core.y, 4f, core.height), new Color(ACCENT_MAGENTA.r, ACCENT_MAGENTA.g, ACCENT_MAGENTA.b, 0.18f));

            for (int i = 0; i < 3; i++)
            {
                float inset = i * motifSize * 0.02f;
                Rect ring = new Rect(core.x - inset, core.y - inset, core.width + inset * 2f, core.height + inset * 2f);
                DrawFrameCorners(ring, i == 0 ? new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.45f) : new Color(ACCENT_ORANGE.r, ACCENT_ORANGE.g, ACCENT_ORANGE.b, 0.20f), ring.width * 0.18f, 2f);
            }

            DrawDiagonalStripe(rect.x + rect.width * 0.22f, 18f, new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.20f), 28f);
            DrawDiagonalStripe(rect.x + rect.width * 0.78f, 18f, new Color(ACCENT_MAGENTA.r, ACCENT_MAGENTA.g, ACCENT_MAGENTA.b, 0.18f), -26f);
        }

        private static void DrawHeaderBar(Rect rect, Color baseColor, string mode)
        {
            DrawRect(rect, baseColor);
            DrawRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), Color.black);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(1f, 1f, 1f, 0.12f));

            if (mode == "hot")
            {
                DrawMotionBand(new Rect(rect.x + rect.width * 0.50f, rect.y, rect.width * 0.50f, rect.height), ACCENT_ORANGE, 12f, 12f, 0.18f);
                DrawRect(new Rect(rect.x + rect.width * 0.72f, rect.y, rect.width * 0.28f, rect.height), new Color(ACCENT_RED.r, ACCENT_RED.g, ACCENT_RED.b, 0.10f));
            }
            else
            {
                DrawMotionBand(new Rect(rect.x, rect.y, rect.width * 0.48f, rect.height), ACCENT_CYAN, 10f, 14f, 0.16f);
                DrawRect(new Rect(rect.x, rect.y, rect.width * 0.22f, rect.height), new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.08f));
            }
        }

        private static void DrawMenuSideFlair(Rect rect, bool hot)
        {
            float flareW = Mathf.Clamp(rect.width * 0.07f, 18f, 44f);
            Color flare = hot ? ACCENT_RED : ACCENT_CYAN;
            DrawRect(new Rect(rect.xMax - flareW, rect.y, flareW, rect.height), new Color(flare.r, flare.g, flare.b, hot ? 0.07f : 0.06f));
            DrawMotionBand(new Rect(rect.xMax - flareW, rect.y, flareW, rect.height), flare, 8f, 10f, hot ? 0.12f : 0.14f);
        }

        private static void DrawVerticalGradient(Rect rect, Color top, Color bottom, int steps)
        {
            steps = Mathf.Max(1, steps);
            float stepHeight = rect.height / steps;
            for (int i = 0; i < steps; i++)
            {
                float t = steps == 1 ? 1f : i / (float)(steps - 1);
                DrawRect(new Rect(rect.x, rect.y + stepHeight * i, rect.width, stepHeight + 1f), Color.Lerp(top, bottom, t));
            }
        }

        private static void DrawHorizontalGradient(Rect rect, Color left, Color right, int steps)
        {
            steps = Mathf.Max(1, steps);
            float stepWidth = rect.width / steps;
            for (int i = 0; i < steps; i++)
            {
                float t = steps == 1 ? 1f : i / (float)(steps - 1);
                DrawRect(new Rect(rect.x + stepWidth * i, rect.y, stepWidth + 1f, rect.height), Color.Lerp(left, right, t));
            }
        }

        private static void DrawPanelFrame(Rect rect, Color fillTop, Color fillBottom, Color accent, float border)
        {
            DrawRect(new Rect(rect.x - border, rect.y - border, rect.width + border * 2f, rect.height + border * 2f), Color.black);
            DrawRect(rect, fillTop);
            DrawRect(new Rect(rect.x, rect.y + rect.height * 0.55f, rect.width, rect.height * 0.45f), new Color(fillBottom.r, fillBottom.g, fillBottom.b, 0.22f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, Mathf.Max(2f, border)), accent);
            DrawRect(new Rect(rect.x, rect.y, Mathf.Max(2f, border), rect.height), new Color(accent.r, accent.g, accent.b, 0.65f));
            DrawRect(new Rect(rect.x, rect.yMax - Mathf.Max(2f, border), rect.width, Mathf.Max(2f, border)), new Color(0f, 0f, 0f, 0.85f));
            DrawFrameCorners(rect, new Color(accent.r, accent.g, accent.b, 0.60f), Mathf.Clamp(rect.width * 0.09f, 16f, 42f), Mathf.Max(2f, border));
        }

        private static void DrawBorderOnly(Rect rect, Color accent, float border)
        {
            float b = Mathf.Max(1f, border);
            DrawRect(new Rect(rect.x, rect.y, rect.width, b), accent);
            DrawRect(new Rect(rect.x, rect.yMax - b, rect.width, b), new Color(accent.r * 0.6f, accent.g * 0.6f, accent.b * 0.6f, accent.a));
            DrawRect(new Rect(rect.x, rect.y, b, rect.height), new Color(accent.r * 0.8f, accent.g * 0.8f, accent.b * 0.8f, accent.a));
            DrawRect(new Rect(rect.xMax - b, rect.y, b, rect.height), new Color(accent.r * 0.8f, accent.g * 0.8f, accent.b * 0.8f, accent.a));
            DrawFrameCorners(rect, accent, Mathf.Clamp(rect.width * 0.18f, 10f, 24f), b);
        }

        private static void DrawConceptBackdrop(Rect rect)
        {
            DrawVerticalGradient(rect, BG_NAVY, BG_BLACK, 16);

            Rect upperField = new Rect(rect.x, rect.y, rect.width, rect.height * 0.48f);
            DrawHorizontalGradient(upperField, new Color(0.02f, 0.12f, 0.22f, 0.22f), new Color(0.18f, 0.04f, 0.16f, 0.12f), 24);

            Rect lowerField = new Rect(rect.x, rect.y + rect.height * 0.62f, rect.width, rect.height * 0.38f);
            DrawVerticalGradient(lowerField, new Color(1f, 0.68f, 0.08f, 0.02f), new Color(1f, 0.36f, 0.08f, 0.18f), 16);

            DrawMotionBand(new Rect(rect.x, rect.y + rect.height * 0.08f, rect.width * 0.38f, rect.height * 0.34f), ACCENT_CYAN, 14f, 16f, 0.16f);
            DrawMotionBand(new Rect(rect.x + rect.width * 0.68f, rect.y + rect.height * 0.54f, rect.width * 0.32f, rect.height * 0.34f), ACCENT_ORANGE, 16f, 18f, 0.18f);
            DrawMotionBand(new Rect(rect.x + rect.width * 0.74f, rect.y + rect.height * 0.14f, rect.width * 0.20f, rect.height * 0.24f), ACCENT_MAGENTA, 8f, 12f, 0.12f);

            DrawDiagonalStripe(rect.x + rect.width * 0.15f, 24f, new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.16f), 28f);
            DrawDiagonalStripe(rect.x + rect.width * 0.86f, 22f, new Color(ACCENT_ORANGE.r, ACCENT_ORANGE.g, ACCENT_ORANGE.b, 0.16f), -25f);
            DrawArenaBurstMotif(new Rect(rect.x, rect.y, rect.width, rect.height * 0.36f));

            DrawRect(new Rect(rect.x, rect.y + rect.height * 0.20f, rect.width, 2f), new Color(1f, 1f, 1f, 0.04f));
            DrawRect(new Rect(rect.x, rect.y + rect.height * 0.78f, rect.width, 2f), new Color(1f, 0.78f, 0.16f, 0.08f));
        }

        private void DrawFittedLabel(Rect rect, string label, GUIStyle source, Color textColor, int minFontSize = 10)
        {
            float usableWidth = Mathf.Max(1f, rect.width - source.padding.horizontal);
            float usableHeight = Mathf.Max(1f, rect.height - source.padding.vertical);
            GUIStyle fittedStyle = new GUIStyle(FitLabelStyle(source, label, usableWidth, minFontSize, usableHeight))
            {
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            fittedStyle.normal.textColor = textColor;
            fittedStyle.hover.textColor = textColor;
            fittedStyle.active.textColor = textColor;
            fittedStyle.focused.textColor = textColor;
            fittedStyle.onNormal.textColor = textColor;
            fittedStyle.onHover.textColor = textColor;
            fittedStyle.onActive.textColor = textColor;
            fittedStyle.onFocused.textColor = textColor;
            GUI.Label(rect, label, fittedStyle);
        }

        private static void DrawSidewaysLabel(Rect rect, string text, GUIStyle sourceStyle, Color color, float angle = -90f)
        {
            Matrix4x4 origMatrix = GUI.matrix;
            Vector2 pivot = rect.center;
            GUIUtility.RotateAroundPivot(angle, pivot);
            Rect rotatedRect = new Rect(pivot.x - rect.height * 0.5f, pivot.y - rect.width * 0.5f, rect.height, rect.width);
            GUIStyle style = new GUIStyle(sourceStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.width * 0.26f), 9, 13)
            };
            style.normal.textColor = color;
            GUI.Label(rotatedRect, text, style);
            GUI.matrix = origMatrix;
        }

        /// <summary>
        /// Draws a nav-style button. Active buttons use yellow bg + black text.
        /// Returns true when clicked.
        /// </summary>
        /// <summary>Themed inline button drawn inline in GUILayout flow (black bg, yellow text/border). Returns true when clicked.</summary>
        private bool InlineBtn(string label, float width, float height, bool active = false)
        {
            Rect r = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            Color bg = active ? ACCENT_YEL : PANEL_STEEL;
            Color fg = active ? Color.black : Color.white;
            float bottomBorderH = Mathf.Max(2f, r.height * 0.05f);
            float leftAccentW = active ? Mathf.Max(3f, r.width * 0.03f) : 0f;
            DrawRect(new Rect(r.x - 1f, r.y - 1f, r.width + 2f, r.height + 2f), Color.black);
            DrawRect(r, bg);
            if (!active)
            {
                DrawRect(new Rect(r.x, r.y, 4f, r.height), ACCENT_CYAN);
                DrawRect(new Rect(r.x + 4f, r.y, r.width * 0.18f, r.height), new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.10f));
            }
            DrawRect(new Rect(r.x, r.yMax, r.width, bottomBorderH), Color.black);
            if (active) DrawRect(new Rect(r.x, r.y, leftAccentW, r.height), Color.black);

            Rect labelRect = new Rect(r.x + leftAccentW, r.y, r.width - leftAccentW, r.height - bottomBorderH);
            float usableWidth = Mathf.Max(1f, labelRect.width - inlineActionButtonStyle.padding.horizontal);
            float usableHeight = Mathf.Max(1f, labelRect.height - inlineActionButtonStyle.padding.vertical);
            GUIStyle fittedStyle = FitLabelStyle(inlineActionButtonStyle, label, usableWidth, 12, usableHeight);

            GUI.contentColor = fg;
            GUI.Label(labelRect, label, fittedStyle);
            GUI.contentColor = Color.white;
            return WithButtonSound(GUI.Button(r, GUIContent.none, GUIStyle.none));
        }

        private bool NavBtn(string label, Rect rect, bool active)
        {
            Color bg = active ? ACCENT_YEL : PANEL_STEEL;
            Color fg = active ? Color.black : Color.white;
            float accentWidth = active ? Mathf.Max(4f, rect.width * 0.018f) : 0f;
            float borderHeight = Mathf.Max(2f, rect.height * 0.06f);

            DrawRect(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), Color.black);
            DrawRect(rect, bg);
            if (active)
            {
                DrawRect(new Rect(rect.x + rect.width * 0.68f, rect.y, rect.width * 0.32f, rect.height), new Color(ACCENT_ORANGE.r, ACCENT_ORANGE.g, ACCENT_ORANGE.b, 0.14f));
                DrawMotionBand(new Rect(rect.x + rect.width * 0.58f, rect.y, rect.width * 0.42f, rect.height), ACCENT_ORANGE, 10f, 11f, 0.12f);
            }
            else
            {
                DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), ACCENT_CYAN);
                DrawRect(new Rect(rect.x + 4f, rect.y, rect.width * 0.15f, rect.height), new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.08f));
            }
            DrawRect(new Rect(rect.x, rect.yMax - borderHeight, rect.width, borderHeight), Color.black);
            if (active)
                DrawRect(new Rect(rect.x, rect.y, accentWidth, rect.height), Color.black);

            Rect labelRect = new Rect(rect.x + accentWidth, rect.y, rect.width - accentWidth, rect.height - borderHeight);
            DrawFittedLabel(labelRect, label, navButtonStyle, fg, 12);

            return WithButtonSound(GUI.Button(rect, GUIContent.none, GUIStyle.none));
        }

        private static GUIStyle CreateStaticStyle(GUIStyle baseStyle, Color color, int fontSize, TextAnchor alignment, FontStyle fontStyle = FontStyle.Normal)
        {
            GUIStyle s = new GUIStyle(baseStyle)
            {
                fontSize = fontSize,
                alignment = alignment,
                fontStyle = fontStyle
            };
            s.normal.textColor = color;
            s.hover.textColor = color;
            s.active.textColor = color;
            s.focused.textColor = color;
            s.onNormal.textColor = color;
            s.onHover.textColor = color;
            s.onActive.textColor = color;
            s.onFocused.textColor = color;
            return s;
        }

        private static int ScaleFont(float base1080pSize)
        {
            return Mathf.RoundToInt(base1080pSize);
        }

        private void EnsureStyles()
        {
            if (titleBarStyle != null && styleScreenW == (int)UiWidth && styleScreenH == (int)UiHeight)
                return;

            styleScreenW = (int)UiWidth;
            styleScreenH = (int)UiHeight;
            float uiScale = 1.0f;

            int bigSize  = 42;
            int navSize  = 28;
            int bodySize = 18;
            int statSize = 17;
            int inlineButtonSize = 20;
            int scaledHorizontalPadding = 8;
            int scaledVerticalPadding = 3;

            listTex = MakeTex(LIST_BG);
            sliderTrackTex = MakeTex(new Color(1f, 1f, 1f, 0f));
            sliderThumbTex = MakeTex(ACCENT_YEL);

            titleBarStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = bigSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(10, 0, 0, 0),
                normal    = { textColor = Color.white },
                hover     = { textColor = Color.white },
                active    = { textColor = Color.white },
                focused   = { textColor = Color.white }
            };

            navButtonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = navSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(Mathf.RoundToInt(Mathf.Clamp(14f * uiScale, 12f, 26f)), scaledHorizontalPadding, 0, 0),
                clipping  = TextClipping.Clip,
                normal    = { textColor = Color.white },
                hover     = { textColor = new Color(0.55f, 0.88f, 1f, 1f) },
                active    = { textColor = new Color(0.40f, 0.75f, 1f, 1f) },
                focused   = { textColor = Color.white }
            };

            startButtonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = navSize + 2,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                clipping  = TextClipping.Clip,
                normal    = { textColor = Color.white },
                hover     = { textColor = Color.white },
                active    = { textColor = Color.white },
                focused   = { textColor = Color.white }
            };

            inlineActionButtonStyle = new GUIStyle(navButtonStyle)
            {
                fontSize  = inlineButtonSize,
                alignment = TextAnchor.MiddleCenter,
                padding   = new RectOffset(scaledHorizontalPadding, scaledHorizontalPadding, scaledVerticalPadding, scaledVerticalPadding),
                clipping  = TextClipping.Clip,
                wordWrap  = false,
                normal    = { textColor = Color.white, background = null },
                hover     = { textColor = Color.white, background = null },
                active    = { textColor = Color.black, background = null },
                focused   = { textColor = Color.white, background = null }
            };

            sectionLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = bodySize + 1,
                fontStyle = FontStyle.Bold,
                clipping  = TextClipping.Clip,
                normal    = { textColor = ACCENT_YEL },
                hover     = { textColor = ACCENT_YEL },
                active    = { textColor = ACCENT_YEL },
                focused   = { textColor = ACCENT_YEL }
            };

            bodyLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = bodySize,
                clipping = TextClipping.Clip,
                wordWrap = true,
                normal   = { textColor = new Color(0.88f, 0.90f, 0.95f, 1f) },
                hover    = { textColor = new Color(0.88f, 0.90f, 0.95f, 1f) },
                active   = { textColor = new Color(0.88f, 0.90f, 0.95f, 1f) },
                focused  = { textColor = new Color(0.88f, 0.90f, 0.95f, 1f) }
            };

            statRowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = statSize,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal   = { textColor = Color.white },
                hover    = { textColor = Color.white },
                active   = { textColor = Color.white },
                focused  = { textColor = Color.white }
            };

            listItemStyle = new GUIStyle(GUI.skin.box)
            {
                margin  = new RectOffset(1, 1, 2, 2),
                padding = new RectOffset(scaledHorizontalPadding, scaledHorizontalPadding, scaledVerticalPadding, scaledVerticalPadding),
                normal  = { background = listTex, textColor = Color.white }
            };

            sliderTrackStyle = new GUIStyle(GUI.skin.horizontalSlider)
            {
                fixedHeight = 22f,
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                normal = { background = sliderTrackTex },
                active = { background = sliderTrackTex },
                hover = { background = sliderTrackTex },
                focused = { background = sliderTrackTex }
            };

            sliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                fixedWidth = Mathf.Clamp(16f * uiScale, 16f, 28f),
                fixedHeight = Mathf.Clamp(18f * uiScale, 18f, 28f),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = sliderThumbTex },
                active = { background = sliderThumbTex },
                hover = { background = sliderThumbTex },
                focused = { background = sliderThumbTex }
            };
        }

        private static Texture2D MakeTex(Color c)
        {
            Texture2D t = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                wrapMode    = TextureWrapMode.Clamp,
                filterMode  = FilterMode.Point
            };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        private static float GetUiScale()
        {
            return 1.0f;
        }
    }
}
