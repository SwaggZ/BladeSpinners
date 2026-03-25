using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using BladeSpinners.Core;
using BladeSpinners.Gameplay;
using BladeSpinners.Abilities;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Gameplay.UI
{
    public class RuntimeGameUiController : MonoBehaviour
    {
        private const string PartsDebugSceneName = "PartsDebugScene";

        // ── Enum types ───────────────────────────────────────────────────────────
        private enum RootUiState { MainMenu, InRun, Paused, BetweenArenas }
        private enum MenuPanel   { Home, Inventory, Settings, Keybinds }

        // ── Singleton ────────────────────────────────────────────────────────────
        private static RuntimeGameUiController instance;

        // ── State ────────────────────────────────────────────────────────────────
        private RootUiState rootState     = RootUiState.MainMenu;
        private MenuPanel   mainMenuPanel = MenuPanel.Home;
        private MenuPanel   pausePanel    = MenuPanel.Home;

        private RuntimeRunBuilder.RunContext runContext;
        private Camera fallbackMenuCamera;
        private readonly Dictionary<PartType, BeyPart> selectedMainMenuLoadout =
            new Dictionary<PartType, BeyPart>();
        private List<BeyPart> ownedParts = new List<BeyPart>();
        private List<BeyPart> enemyParts = new List<BeyPart>();
        private PartType? selectedInventorySlot;
        private BeyPart selectedInventoryPart;
        private BeyPart selectedLootPart;

        private Vector2 ownedScroll;
        private Vector2 runScroll;
        private Vector2 garageSwapScroll;
        private PartType? garageSwapSlot;
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
        private float previewManualYaw;
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
        private float settingsVolume      = 1f;
        private float settingsSensitivity = 1f;
        private float settingsClippingOpacity = 0.2f;
        private float settingsRingsOpacity = 1f;

        // ── Loot transfer state ──────────────────────────────────────────────────
        private List<BeyPart> lootEligibleParts;
        private List<bool>    lootSelectedFlags;
        private int           lootMaxTransferCount;
        private RarityTier    lootMaxRarityTier;
        private bool          lootTransferInitialized;
        private Vector2       lootScroll;
        private Vector2       deathSalvageCardScroll;
        private Vector2       deathEquippedCardScroll;

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

        private static bool IsPartsDebugSceneActive()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            return activeScene.IsValid() && string.Equals(activeScene.name, PartsDebugSceneName, StringComparison.Ordinal);
        }

        private void Update()
        {
            if (rootState == RootUiState.InRun || rootState == RootUiState.Paused || rootState == RootUiState.BetweenArenas)
            {
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame)
                    TogglePause();
            }

            UpdateCursorState();

            if (rootState != RootUiState.InRun
                || runContext.Match == null
                || runContext.Match.CurrentState != MatchManager.MatchState.PlayerLost)
            {
                deathOverlayPreviewPrepared = false;
                lootTransferInitialized = false;
            }

            HandleRunProgressionAdvance();

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

        private void OnGUI()
        {
            try
            {
                EnsureStyles();
                switch (rootState)
                {
                    case RootUiState.MainMenu: DrawMainMenu(); break;
                    case RootUiState.Paused:   DrawPauseMenu(); break;
                    case RootUiState.BetweenArenas: DrawArenaIntermissionMenu(); break;
                }

                if (rootState == RootUiState.InRun)
                    DrawInRunOverlays();

                if (initFailed)
                {
                    GUI.color = Color.red;
                    GUI.Label(new Rect(10, Screen.height - 30, Screen.width, 24),
                        $"[Init Warning] {initErrorMsg}");
                    GUI.color = Color.white;
                }
            }
            catch (Exception e)
            {
                // Emergency fallback — render error on screen so builds aren't silent
                GUI.color = Color.red;
                GUI.Label(new Rect(10, 10, Screen.width - 20, 60),
                    $"[UI Error] {e.Message}");
                GUI.color = Color.white;
                Debug.LogError($"[BladeSpinners] OnGUI exception: {e}");
            }
        }

        private void DrawInRunOverlays()
        {
            MatchManager match = runContext.Match;
            if (match == null)
                return;

            DrawRunDepthOverlay();

            if (match.CurrentState == MatchManager.MatchState.WaitingToStart)
                DrawStartCountdownOverlay(match);

            if (match.CurrentState == MatchManager.MatchState.PlayerLost)
                DrawDeathOverlay(match);
        }

        private void DrawRunDepthOverlay()
        {
            RuntimeRunBuilder.RunProgression progression = runContext.Progression;
            if (progression == null)
                return;

            int sw = Screen.width;
            int sh = Screen.height;
            string label = $"LEVEL {progression.CurrentLevelOneBased}/{progression.TotalLevels}   ARENA {progression.CurrentArenaOneBased}/{progression.ArenasPerLevel}";

            GUIStyle infoStyle = new GUIStyle(bodyLabelStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(sh * 0.021f, 18f, 46f))
            };

            float padX = Mathf.Clamp(sw * 0.008f, 12f, 30f);
            float padY = Mathf.Clamp(sh * 0.008f, 8f, 18f);
            Vector2 textSize = infoStyle.CalcSize(new GUIContent(label));

            float badgeW = Mathf.Clamp(textSize.x + padX * 2f, 360f, sw * 0.58f);
            float badgeH = Mathf.Clamp(textSize.y + padY * 2f, 52f, 92f);
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

        private void DrawStartCountdownOverlay(MatchManager match)
        {
            int sw = Screen.width;
            int sh = Screen.height;

            float remaining = match.CountdownRemaining;
            int seconds = Mathf.Max(1, Mathf.CeilToInt(remaining));

            string label = remaining <= 0.05f ? "GO!" : seconds.ToString();

            float panelW = Mathf.Clamp(sw * 0.2f, 180f, 320f);
            float panelH = Mathf.Clamp(sh * 0.14f, 110f, 180f);
            Rect panel = new Rect((sw - panelW) * 0.5f, sh * 0.1f, panelW, panelH);

            DrawPanelFrame(panel, new Color(0.02f, 0.06f, 0.12f, 0.94f), new Color(0.03f, 0.09f, 0.17f, 0.97f), ACCENT_CYAN, 3f);
            DrawFrameCorners(panel, new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.55f), panel.width * 0.14f, 2f);
            DrawMotionBandClipped(new Rect(panel.x + panel.width * 0.55f, panel.y, panel.width * 0.38f, panel.height), ACCENT_CYAN, 8f, 14f, 0.08f);

            GUIStyle countdownStyle = new GUIStyle(titleBarStyle)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.085f, 48f, 96f)),
                alignment = TextAnchor.MiddleCenter
            };
            countdownStyle.normal.textColor = Color.white;

            GUIStyle countdownSubStyle = new GUIStyle(bodyLabelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.022f, 14f, 24f))
            };
            countdownSubStyle.normal.textColor = ACCENT_CYAN;

            GUILayout.BeginArea(panel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(label, countdownStyle);
            GUILayout.Label("MATCH START", countdownSubStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }

        private void DrawDeathOverlay(MatchManager match)
        {
            int sw = Screen.width;
            int sh = Screen.height;
            float uiScale = GetUiScale();

            if (!lootTransferInitialized)
                InitLootTransferState();

            DrawRect(new Rect(0, 0, sw, sh), OVERLAY);

            float panelW = Mathf.Clamp(sw * 0.82f, 640f, 3000f);
            float panelH = Mathf.Clamp(sh * 0.78f, 420f, 1500f);
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
                Dictionary<PartType, BeyPart> killerLoadout = BuildLoadoutFromParts(killerParts);
                if (killerLoadout.Count > 0)
                    RefreshPreviewFromLoadout(killerLoadout);
                deathOverlayPreviewPrepared = true;
            }
            else if (!showKillerBuild)
            {
                deathOverlayPreviewPrepared = false;
            }

            float outerPad = Mathf.Clamp(panel.width * 0.028f, 16f, 40f);
            float buttonH = Mathf.Clamp(40f * uiScale, 40f, 72f);
            Rect buttonRect = new Rect(panel.x + outerPad, panel.yMax - outerPad - buttonH, panel.width - outerPad * 2f, buttonH);
            Rect content = new Rect(panel.x + outerPad, panel.y + outerPad, panel.width - outerPad * 2f, buttonRect.y - panel.y - outerPad * 2f);
            GUIStyle deathTitleStyle = new GUIStyle(titleBarStyle)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(panel.height * 0.08f, 28f, 84f)),
                wordWrap = false,
                normal  = { textColor = Color.white },
                hover   = { textColor = Color.white },
                active  = { textColor = Color.white },
                focused = { textColor = Color.white }
            };
            GUIStyle deathReasonStyle = new GUIStyle(sectionLabelStyle)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(panel.height * 0.036f, 14f, 38f)),
                wordWrap = true,
                normal  = { textColor = new Color(1f, 0.55f, 0.55f, 1f) },
                hover   = { textColor = new Color(1f, 0.55f, 0.55f, 1f) },
                active  = { textColor = new Color(1f, 0.55f, 0.55f, 1f) },
                focused = { textColor = new Color(1f, 0.55f, 0.55f, 1f) }
            };
            GUIStyle buildHeaderStyle = new GUIStyle(sectionLabelStyle)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(panel.height * 0.032f, 13f, 30f)),
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            GUIStyle killerItemStyle = new GUIStyle(bodyLabelStyle)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(panel.height * 0.030f, 12f, 26f)),
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            bool hasLoot = lootEligibleParts != null;
            float topGap = Mathf.Clamp(8f * uiScale, 8f, 18f);
            float sectionGap = Mathf.Clamp(10f * uiScale, 10f, 22f);
            float titleHeight = deathTitleStyle.CalcHeight(new GUIContent("YOU BURSTED"), content.width);
            float reasonHeight = deathReasonStyle.CalcHeight(new GUIContent(reasonText.ToUpperInvariant()), content.width);
            Rect titleRect = new Rect(content.x, content.y, content.width, titleHeight);
            Rect reasonRect = new Rect(content.x, titleRect.yMax + topGap, content.width, reasonHeight);
            GUI.Label(titleRect, "YOU BURSTED", deathTitleStyle);
            GUI.Label(reasonRect, reasonText.ToUpperInvariant(), deathReasonStyle);

            float remainingY = reasonRect.yMax + sectionGap;
            float remainingH = Mathf.Max(60f, content.yMax - remainingY);

            float lootH = hasLoot ? Mathf.Max(90f, remainingH * (showKillerBuild ? 0.40f : 0.80f)) : 0f;
            float killerH = showKillerBuild ? Mathf.Max(120f, remainingH - lootH - (hasLoot ? sectionGap : 0f)) : 0f;

            if (showKillerBuild)
            {
                Rect rowRect = new Rect(content.x, remainingY, content.width, killerH);
                float columnGap = Mathf.Clamp(panel.width * 0.018f, 10f, 28f);
                float buildWidth = Mathf.Clamp(rowRect.width * 0.37f, 260f, rowRect.width * 0.52f);
                float previewWidth = rowRect.width - buildWidth - columnGap;

                float innerPad = Mathf.Clamp(panel.width * 0.012f, 10f, 20f);
                float headerH = Mathf.Clamp(28f * uiScale, 24f, 46f);

                Rect previewCard = new Rect(rowRect.x, rowRect.y, previewWidth, rowRect.height);
                Rect buildCard = new Rect(previewCard.xMax + columnGap, rowRect.y, buildWidth, rowRect.height);
                GUIStyle previewHeaderStyle = FitLabelStyle(buildHeaderStyle, "KILLER PREVIEW", previewWidth - innerPad * 2f, 12);
                GUIStyle buildAreaHeaderStyle = FitLabelStyle(buildHeaderStyle, "KILLER BUILD", buildWidth - innerPad * 2f, 12);
                GUIStyle buildItemStyle = FitLabelStyle(killerItemStyle, GetLongestPartLabel(killerParts), buildWidth - innerPad * 2f - 24f, 10);

                DrawRect(previewCard, new Color(0f, 0f, 0f, 0.38f));
                DrawRect(new Rect(previewCard.x, previewCard.y, previewCard.width, 3f), ACCENT_YEL);

                Rect previewLabel = new Rect(previewCard.x + innerPad, previewCard.y + innerPad * 0.45f, previewCard.width - innerPad * 2f, headerH);
                GUI.Label(previewLabel, "KILLER PREVIEW", previewHeaderStyle);

                Rect previewRect = new Rect(
                    previewCard.x + innerPad,
                    previewCard.y + innerPad * 0.45f + headerH + 6f,
                    previewCard.width - innerPad * 2f,
                    previewCard.height - (innerPad * 1.4f + headerH + 6f));
                if (previewTexture != null)
                {
                    GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit, true);
                }

                DrawRect(buildCard, new Color(0f, 0f, 0f, 0.30f));
                DrawRect(new Rect(buildCard.x, buildCard.y, buildCard.width, 3f), ACCENT_YEL);

                Rect buildArea = new Rect(buildCard.x + innerPad, buildCard.y + innerPad * 0.45f, buildCard.width - innerPad * 2f, buildCard.height - innerPad);
                GUILayout.BeginArea(buildArea);
                GUILayout.Label("KILLER BUILD", buildAreaHeaderStyle, GUILayout.Height(headerH));
                GUILayout.Space(Mathf.Clamp(4f * uiScale, 4f, 10f));
                if (killerParts != null && killerParts.Count > 0)
                    DrawKillerPartsRows(killerParts, buildItemStyle);
                GUILayout.EndArea();

                remainingY = rowRect.yMax + sectionGap;
            }

            if (hasLoot)
            {
                GUILayout.BeginArea(new Rect(content.x, remainingY, content.width, Mathf.Min(lootH, content.yMax - remainingY)));
                DrawLootSalvageSection(lootH, uiScale);
                GUILayout.EndArea();
            }

            int _lootSelected = CountSelectedLoot();
            string _btnLabel = hasLoot && _lootSelected > 0
                ? $"TAKE LOOT ({_lootSelected}) & LEAVE"
                : "LEAVE RUN";
            if (GUI.Button(buttonRect, _btnLabel, navButtonStyle))
            {
                CommitTransferLootAndReturnToMenu();
            }
        }

        private void DrawKillerPartsRows(IReadOnlyList<BeyPart> parts, GUIStyle itemStyle)
        {
            GUIStyle resolvedItemStyle = itemStyle ?? bodyLabelStyle;
            for (int i = 0; i < parts.Count; i++)
            {
                BeyPart part = parts[i];
                if (part == null)
                    continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label("■", sectionLabelStyle, GUILayout.Width(18f));
                int partScore = Mathf.RoundToInt(GetPartPowerScore(part));
                string partLabel = $"{PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant()}  —  SCORE {partScore}";
                GUILayout.Label(partLabel, resolvedItemStyle, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
                GUILayout.Space(2f);
            }
        }

        // ── Loot transfer helpers ────────────────────────────────────────────────

        private void InitLootTransferState()
        {
            lootTransferInitialized = true;

            int depthIndex  = runContext.Progression?.DepthIndex  ?? 0;
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
            lootScroll        = Vector2.zero;
            deathSalvageCardScroll = Vector2.zero;
            deathEquippedCardScroll = Vector2.zero;
        }

        private static void GetLootTransferRules(int depthIndex, int totalArenas,
            out int maxCount, out RarityTier maxRarity)
        {
            float t = totalArenas > 1 ? Mathf.Clamp01((float)depthIndex / (totalArenas - 1)) : 1f;
            maxCount  = t < 0.34f ? 1 : t < 0.67f ? 2 : 3;
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

        private void DrawLootSalvageSection(float sectionH, float uiScale)
        {
            float btnW = Mathf.Clamp(72f * uiScale, 70f, 120f);
            float btnH = Mathf.Clamp(26f * uiScale, 24f, 40f);
            int sel    = CountSelectedLoot();

            string headerText = lootEligibleParts.Count > 0
                ? $"LOOT SALVAGE  —  {sel}/{lootMaxTransferCount}  ▸  MAX: {lootMaxRarityTier.ToString().ToUpper()}"
                : "LOOT SALVAGE  —  NO NEW PARTS TO SALVAGE";
            GUILayout.Label(headerText, sectionLabelStyle);

            if (lootEligibleParts.Count == 0) return;

            if (selectedLootPart == null || !lootEligibleParts.Contains(selectedLootPart))
                selectedLootPart = lootEligibleParts[0];

            float scrollH = Mathf.Max(40f, sectionH - sectionLabelStyle.fontSize * 2f - 10f);
            GUIStyle rowLabel = new GUIStyle(bodyLabelStyle) { alignment = TextAnchor.MiddleLeft };
            GUILayout.BeginHorizontal(GUILayout.Height(scrollH));

            float detailWidth = Mathf.Clamp(560f * uiScale, 420f, 760f);
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.Height(scrollH));
            lootScroll = GUILayout.BeginScrollView(lootScroll, GUILayout.Height(scrollH));
            for (int i = 0; i < lootEligibleParts.Count; i++)
            {
                BeyPart part       = lootEligibleParts[i];
                bool    isSelected = lootSelectedFlags[i];
                bool    canToggle  = isSelected || sel < lootMaxTransferCount;
                bool    isFocused  = selectedLootPart == part;

                GUILayout.BeginHorizontal();
                int partScore = Mathf.RoundToInt(GetPartPowerScore(part));
                string partLabel = $"{PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant()}  [{part.Rarity.ToString().ToUpper()}]  —  {part.PartType.ToString().ToUpper()}  —  SCORE {partScore}";
                GUILayout.Label(partLabel, rowLabel, GUILayout.ExpandWidth(true));
                if (InlineBtn(isFocused ? "VIEWING" : "VIEW", btnW, btnH, isFocused))
                    selectedLootPart = part;
                string toggleLabel = isSelected ? "\u2713 KEEP" : (canToggle ? "KEEP" : "\u2014");
                if (InlineBtn(toggleLabel, btnW, btnH, isSelected) && canToggle)
                {
                    lootSelectedFlags[i] = !lootSelectedFlags[i];
                    selectedLootPart = part;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(2f);
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUILayout.Space(Mathf.Clamp(10f * uiScale, 10f, 18f));
            Rect comparisonRect = GUILayoutUtility.GetRect(detailWidth, scrollH, GUILayout.Width(detailWidth), GUILayout.Height(scrollH));
            DrawLootPartComparison(comparisonRect, selectedLootPart, uiScale);
            GUILayout.EndHorizontal();
        }

        private void DrawLootPartComparison(Rect area, BeyPart lootPart, float uiScale)
        {
            if (lootPart == null)
            {
                GUI.Label(area, "SELECT A PART TO VIEW COMPARISON.", bodyLabelStyle);
                return;
            }

            BeyPart equippedPart = GetEquippedPartForType(lootPart.PartType);

            float cardGap = Mathf.Clamp(10f * uiScale, 8f, 16f);
            float cardWidth = Mathf.Max(180f, (area.width - cardGap) * 0.5f);
            float cardHeight = Mathf.Max(180f, area.height);
            float contentWidthHint = Mathf.Max(150f, cardWidth - 20f);

            Rect leftRect = new Rect(area.x, area.y, cardWidth, cardHeight);
            Rect rightRect = new Rect(leftRect.xMax + cardGap, area.y, cardWidth, cardHeight);

            DrawSelectedPartCardInRect(leftRect, lootPart, "SALVAGE PART", contentWidthHint, false);
            DrawSelectedPartCardInRect(rightRect, equippedPart, "EQUIPPED PART", contentWidthHint, true);
        }

        private void DrawSelectedPartCardInRect(Rect cardRect, BeyPart part, string header, float widthHint, bool isEquippedCard)
        {
            DrawRect(cardRect, new Color(0f, 0f, 0f, 0.26f));
            DrawRect(new Rect(cardRect.x, cardRect.y, cardRect.width, 3f), ACCENT_YEL);

            GUIStyle headerStyle = FitLabelStyle(sectionLabelStyle, header, widthHint, 10);
            GUIStyle detailStyle = FitLabelStyle(bodyLabelStyle, "ABILITY RARITY  LEGENDARY", widthHint, 10);
            GUIStyle statStyle = FitLabelStyle(statRowStyle, "MANA REGEN  100.0", widthHint, 10);

            float contentPadX = 10f;
            float contentPadY = 8f;
            Rect contentRect = new Rect(cardRect.x + contentPadX, cardRect.y + contentPadY, cardRect.width - contentPadX * 2f, cardRect.height - contentPadY * 2f);
            float headerHeight = Mathf.Clamp(26f * GetUiScale(), 22f, 40f);

            Rect headerRect = new Rect(contentRect.x, contentRect.y, contentRect.width, headerHeight);
            GUI.Label(headerRect, header, headerStyle);

            Rect scrollRect = new Rect(contentRect.x, headerRect.yMax + 2f, contentRect.width, Mathf.Max(24f, contentRect.yMax - (headerRect.yMax + 2f)));
            GUILayout.BeginArea(scrollRect);

            Vector2 scroll = isEquippedCard ? deathEquippedCardScroll : deathSalvageCardScroll;
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (part == null)
            {
                GUILayout.Label("NO PART EQUIPPED IN THIS SLOT.", bodyLabelStyle);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                if (isEquippedCard) deathEquippedCardScroll = scroll; else deathSalvageCardScroll = scroll;
                return;
            }

            GUILayout.Label(PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant(), statStyle);
            GUILayout.Label($"TYPE      {part.PartType.ToString().ToUpper()}", statStyle);
            GUILayout.Label($"RARITY    {part.Rarity.ToString().ToUpper()}", statStyle);

            List<string> partLines = BuildPartDetailLines(part);
            for (int i = 0; i < partLines.Count; i++)
                GUILayout.Label(partLines[i], statStyle);

            BeyAbility ability = ResolveAbilityForPart(part);
            GUILayout.Space(6f);
            GUILayout.Label("ABILITY", headerStyle);
            if (ability == null)
            {
                GUILayout.Label("NONE", detailStyle);
            }
            else
            {
                GUILayout.Label(ability.AbilityName.ToUpperInvariant(), statStyle);
                GUILayout.Label($"ABILITY RARITY  {ability.Rarity.ToString().ToUpper()}", statStyle);
                GUILayout.Label($"MANA COST       {ability.ManaCost:0.#}", statStyle);
                if (!string.IsNullOrWhiteSpace(ability.Description))
                    GUILayout.Label(ability.Description.ToUpperInvariant(), detailStyle);
            }

            GUILayout.EndScrollView();
            if (isEquippedCard) deathEquippedCardScroll = scroll; else deathSalvageCardScroll = scroll;
            GUILayout.EndArea();
        }

        private BeyPart GetEquippedPartForType(PartType type)
        {
            if (runContext.Player != null && runContext.Player.BeyConfiguration != null)
                return runContext.Player.BeyConfiguration.GetEquippedPart(type);

            selectedMainMenuLoadout.TryGetValue(type, out BeyPart part);
            return part;
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
            if (sprite == null || sprite.texture == null)
                return;

            Rect tr = sprite.textureRect;
            Texture tex = sprite.texture;
            Rect uv = new Rect(
                tr.x / tex.width,
                tr.y / tex.height,
                tr.width / tex.width,
                tr.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv, true);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  MAIN MENU
        // ══════════════════════════════════════════════════════════════════════════

        private void DrawMainMenu()
        {
            RefreshPreviewFromLoadout(selectedMainMenuLoadout);

            int sw = Screen.width, sh = Screen.height;
            float uiScale = GetUiScale();
            float gutter = Mathf.Clamp(sw * 0.006f, 8f, 18f);
            float topH = Mathf.Clamp(78f * uiScale, 70f, 118f);
            float bottomH = Mathf.Clamp(90f * uiScale, 82f, 128f);

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
                    DrawFramedContentPanel(contentRect, "PART INVENTORY", delegate
                    {
                        DrawInventoryPanel(false);
                    });
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
            float inlineButtonW = Mathf.Clamp(110f * uiScale, 100f, 190f);
            float inlineButtonH = Mathf.Clamp(34f * uiScale, 32f, 50f);
            float rowMinH = Mathf.Clamp(42f * uiScale, 40f, 72f);
            GUIStyle rowLabelStyle = new GUIStyle(bodyLabelStyle)
            {
                wordWrap = true,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft
            };

            GUILayout.Label(isRunInventory ? "RUN INVENTORY" : "INVENTORY", sectionLabelStyle);

            Dictionary<PartType, BeyPart> currentLoadout;
            List<BeyPart> sourceParts;

            if (isRunInventory)
            {
                PlayerManager player = runContext.Player;
                if (player == null) { GUILayout.Label("No active run.", bodyLabelStyle); return; }

                currentLoadout = GetCurrentRunLoadout(player);
                sourceParts = player.GetRunInventory().GetAllParts();
            }
            else
            {
                currentLoadout = selectedMainMenuLoadout;
                sourceParts = ownedParts;
            }

            GUILayout.Space(Mathf.Clamp(4f * uiScale, 4f, 10f));
            GUILayout.Label("CLICK A SLOT TO OPEN ITS PART LIST", bodyLabelStyle);
            GUILayout.Space(Mathf.Clamp(6f * uiScale, 6f, 12f));

            foreach (PartType type in PART_DISPLAY_ORDER)
            {
                currentLoadout.TryGetValue(type, out BeyPart equippedPart);
                string slotLabel = type.ToString().ToUpper();
                string equippedLabel = equippedPart != null
                    ? PartDisplayNameFormatter.ToShortDisplayName(equippedPart)
                    : "NONE";

                GUILayout.BeginHorizontal(listItemStyle, GUILayout.MinHeight(rowMinH));
                GUILayout.Label($"{slotLabel} : {equippedLabel}", rowLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(rowMinH - 6f));
                GUILayout.FlexibleSpace();
                if (InlineBtn("CHOOSE", inlineButtonW, inlineButtonH))
                {
                    selectedInventorySlot = type;
                }
                GUILayout.EndHorizontal();
            }

            if (!selectedInventorySlot.HasValue)
                return;

            PartType selectedType = selectedInventorySlot.Value;
            List<BeyPart> parts = GetPartsByType(sourceParts, selectedType);
            if (selectedInventoryPart == null || selectedInventoryPart.PartType != selectedType || !parts.Contains(selectedInventoryPart))
                selectedInventoryPart = parts.Count > 0 ? parts[0] : null;

            GUILayout.Space(Mathf.Clamp(8f * uiScale, 8f, 16f));
            GUILayout.Label($"SELECT {selectedType.ToString().ToUpper()}", sectionLabelStyle);

            if (parts.Count == 0)
            {
                GUILayout.Label("NO PARTS IN THIS SLOT TYPE.", bodyLabelStyle);
                return;
            }

            if (isRunInventory)
                runScroll = GUILayout.BeginScrollView(runScroll, GUILayout.ExpandHeight(true));
            else
                ownedScroll = GUILayout.BeginScrollView(ownedScroll, GUILayout.ExpandHeight(true));

            for (int i = 0; i < parts.Count; i++)
            {
                BeyPart part = parts[i];
                if (part == null) continue;
                bool isFocused = selectedInventoryPart == part;

                GUILayout.BeginHorizontal(listItemStyle, GUILayout.MinHeight(rowMinH));
                GUILayout.Label($"{PartDisplayNameFormatter.ToShortDisplayName(part)}  [{part.Rarity.ToString().ToUpper()}]", rowLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(rowMinH - 6f));
                GUILayout.FlexibleSpace();
                if (InlineBtn(isFocused ? "VIEWING" : "VIEW", Mathf.Clamp(104f * uiScale, 96f, 170f), inlineButtonH, isFocused))
                    selectedInventoryPart = part;
                if (InlineBtn("EQUIP", Mathf.Clamp(98f * uiScale, 92f, 168f), inlineButtonH))
                {
                    selectedInventoryPart = part;
                    if (isRunInventory)
                    {
                        runContext.Player?.EquipPart(part);
                        RefreshPreviewFromLoadout(GetCurrentRunLoadout(runContext.Player));
                    }
                    else
                    {
                        selectedMainMenuLoadout[selectedType] = part;
                        RefreshPreviewFromLoadout(selectedMainMenuLoadout);
                        AutoSave();
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (isRunInventory)
                GUILayout.EndScrollView();
            else
                GUILayout.EndScrollView();
        }

        private void DrawPreviewAndStats(Rect area, PlayerManager runPlayer)
        {
            float uiScale = GetUiScale();
            float innerPad = Mathf.Clamp(10f * uiScale, 10f, 24f);
            float frame = Mathf.Max(3f, Mathf.Clamp(Screen.width * 0.0028f, 3f, 7f));
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
                DrawOverallStatsCard(overallRect, stats, spin, mana);
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
                GUIStyle fittedStatStyle = FitLabelStyle(statRowStyle, $"MANA     {mana:0.0} / {stats.ManaPoolSize:0.0}", area.width - innerPad * 2f, 10);
                BeyPart selectedPart = GetFocusedInventoryPart();
                bool showPart = selectedPart != null;
                float fullW = area.width - innerPad * 2f;
                float leftW = showPart ? Mathf.Max(180f, fullW * 0.44f) : fullW;
                float rightX = area.x + innerPad + leftW + (showPart ? Mathf.Clamp(10f * uiScale, 10f, 18f) : 0f);
                float rightW = showPart ? area.x + area.width - innerPad - rightX : 0f;

                GUILayout.BeginArea(new Rect(area.x + innerPad, rowY, leftW, rowH));
                GUILayout.Label($"SPIN     {spin:0.0} / {GameConstants.MAX_SPIN:0}",   fittedStatStyle);
                GUILayout.Label($"MANA     {mana:0.0} / {stats.ManaPoolSize:0.0}",     fittedStatStyle);
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

        private void DrawOverallStatsCard(Rect area, BeyStatBlock stats, float spin, float mana)
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
            GUILayout.Label($"MANA     {mana:0.0} / {stats.ManaPoolSize:0.0}", statStyle);
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

        private void DrawSelectedPartCard(BeyPart part, string header, bool drawBackground, float widthHint = 420f, float minHeight = 0f)
        {
            if (part == null)
            {
                if (drawBackground)
                {
                    Rect emptyBg = minHeight > 0f
                        ? GUILayoutUtility.GetRect(10f, minHeight, GUILayout.ExpandWidth(true), GUILayout.Height(minHeight))
                        : GUILayoutUtility.GetRect(10f, 10f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    DrawRect(emptyBg, new Color(0f, 0f, 0f, 0.26f));
                    DrawRect(new Rect(emptyBg.x, emptyBg.y, emptyBg.width, 3f), ACCENT_YEL);
                    GUILayout.BeginArea(new Rect(emptyBg.x + 10f, emptyBg.y + 8f, emptyBg.width - 20f, emptyBg.height - 16f));
                    GUIStyle headerStyleEmpty = FitLabelStyle(sectionLabelStyle, header, widthHint, 10);
                    GUILayout.Label(header, headerStyleEmpty);
                    GUILayout.Space(4f);
                    GUILayout.Label("NO PART EQUIPPED IN THIS SLOT.", bodyLabelStyle);
                    GUILayout.EndArea();
                }
                else
                {
                    GUILayout.Label("SELECT A PART TO VIEW ITS STATS AND ABILITY.", bodyLabelStyle);
                }
                return;
            }

            if (drawBackground)
            {
                Rect bg = minHeight > 0f
                    ? GUILayoutUtility.GetRect(10f, minHeight, GUILayout.ExpandWidth(true), GUILayout.Height(minHeight))
                    : GUILayoutUtility.GetRect(10f, 10f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                DrawRect(bg, new Color(0f, 0f, 0f, 0.26f));
                DrawRect(new Rect(bg.x, bg.y, bg.width, 3f), ACCENT_YEL);
                GUILayout.BeginArea(new Rect(bg.x + 10f, bg.y + 8f, bg.width - 20f, bg.height - 16f));
            }

            GUIStyle headerStyle = FitLabelStyle(sectionLabelStyle, header, widthHint, 10);
            GUIStyle detailStyle = FitLabelStyle(bodyLabelStyle, "ABILITY RARITY  LEGENDARY", widthHint, 10);
            GUIStyle statStyle = FitLabelStyle(statRowStyle, "MANA REGEN  100.0", widthHint, 10);

            GUILayout.Label(header, headerStyle);
            GUILayout.Space(4f);
            GUILayout.Label(PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant(), statStyle);
            GUILayout.Label($"TYPE      {part.PartType.ToString().ToUpper()}", statStyle);
            GUILayout.Label($"RARITY    {part.Rarity.ToString().ToUpper()}", statStyle);

            List<string> partLines = BuildPartDetailLines(part);
            for (int i = 0; i < partLines.Count; i++)
                GUILayout.Label(partLines[i], statStyle);

            BeyAbility ability = ResolveAbilityForPart(part);
            GUILayout.Space(6f);
            GUILayout.Label("ABILITY", headerStyle);
            if (ability == null)
            {
                GUILayout.Label("NONE", detailStyle);
            }
            else
            {
                GUILayout.Label(ability.AbilityName.ToUpperInvariant(), statStyle);
                GUILayout.Label($"ABILITY RARITY  {ability.Rarity.ToString().ToUpper()}", statStyle);
                GUILayout.Label($"MANA COST       {ability.ManaCost:0.#}", statStyle);
                if (!string.IsNullOrWhiteSpace(ability.Description))
                    GUILayout.Label(ability.Description.ToUpperInvariant(), detailStyle);
            }

            if (drawBackground)
                GUILayout.EndArea();
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
                    lines.Add($"DRAIN MOD {part.BehaviorBasedStaminaDrainModifier:0.00}");
                    lines.Add($"UPHILL    {part.UphillResistanceMultiplier:0.00}");
                    lines.Add($"SLOPE     {part.SlopeMultiplier:0.00}");
                    break;

                case PartType.Track:
                    lines.Add($"HEIGHT    {part.TrackHeight:0.00}");
                    lines.Add($"JUMP ARC  {part.JumpArcModifier:0.00}");
                    break;

                case PartType.FusionWheel:
                    lines.Add($"WEIGHT    {part.Weight:0.0}");
                    lines.Add($"MASS DRAIN {part.MassBasedStaminaDrainRate:0.00}");
                    break;

                case PartType.EnergyRing:
                    lines.Add($"MANA POOL {part.ManaPoolSize:0.0}");
                    lines.Add($"MANA REGEN {part.ManaRegenRate:0.0}");
                    break;

                case PartType.FaceBolt:
                    lines.Add($"EMBLEM    {(part.FaceBoltEmblem != null ? "YES" : "NO")}");
                    break;
            }

            if (!string.IsNullOrWhiteSpace(part.Description))
                lines.Add(part.Description.ToUpperInvariant());

            return lines;
        }

        private void DrawSettingsPanel()
        {
            GUILayout.Label("SETTINGS", sectionLabelStyle);
            GUILayout.Space(6);
            settingsVolume = DrawThemedSlider("MASTER VOLUME", settingsVolume, 0f, 1f);
            GUILayout.Space(4);
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

            GUILayout.Space(12);
            GUILayout.Label("KEYBINDS", sectionLabelStyle);
            GUILayout.Space(4);
            DrawKeybindPanel();
        }

        private void DrawKeybindPanel()
        {
            GUIStyle keybindStyle = new GUIStyle(bodyLabelStyle)
            {
                wordWrap = true,
                clipping = TextClipping.Clip
            };
            GUILayout.Label("MOVE         WASD",            keybindStyle);
            GUILayout.Label("BOOST        LEFT SHIFT",      keybindStyle);
            GUILayout.Label("JUMP         SPACE",           keybindStyle);
            GUILayout.Label("ABILITY      E",               keybindStyle);
            GUILayout.Label("LOCK-ON      MIDDLE MOUSE",    keybindStyle);
            GUILayout.Label("CYCLE TGT    SCROLL WHEEL",    keybindStyle);
            GUILayout.Label("PAUSE        ESC",             keybindStyle);
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
            DrawPanelFrame(rect, new Color(0.02f, 0.05f, 0.11f, 0.94f), new Color(0.03f, 0.09f, 0.16f, 0.96f), ACCENT_CYAN, 3f);
            DrawMotionBandClipped(new Rect(rect.x + rect.width * 0.62f, rect.y, rect.width * 0.30f, rect.height), ACCENT_CYAN, 8f, 16f, 0.10f);

            float pad = Mathf.Clamp(12f * GetUiScale(), 12f, 24f);
            float logoW = Mathf.Clamp(rect.width * 0.28f, 260f, 440f);
            float tabsW = Mathf.Clamp(rect.width * 0.36f, 320f, 560f);
            Rect brandRect = new Rect(rect.x + pad, rect.y + pad * 0.6f, logoW, rect.height - pad * 1.2f);
            Rect tabsRect = new Rect(rect.center.x - tabsW * 0.5f, rect.y + pad * 0.55f, tabsW, rect.height - pad * 1.1f);

            DrawBrandLockup(brandRect);

            float gap = Mathf.Clamp(10f * GetUiScale(), 8f, 18f);
            float tabW = (tabsRect.width - gap * 2f) / 3f;
            if (TopTabBtn("GARAGE", new Rect(tabsRect.x, tabsRect.y, tabW, tabsRect.height), mainMenuPanel == MenuPanel.Home))
                SetMainMenuPanel(MenuPanel.Home);
            if (TopTabBtn("INVENTORY", new Rect(tabsRect.x + tabW + gap, tabsRect.y, tabW, tabsRect.height), mainMenuPanel == MenuPanel.Inventory))
                SetMainMenuPanel(MenuPanel.Inventory);
            if (TopTabBtn("SETTINGS", new Rect(tabsRect.x + (tabW + gap) * 2f, tabsRect.y, tabW, tabsRect.height), mainMenuPanel == MenuPanel.Settings))
                SetMainMenuPanel(MenuPanel.Settings);
        }

        private void DrawBrandLockup(Rect rect)
        {
            Rect iconRect = new Rect(rect.x, rect.y + rect.height * 0.18f, rect.height * 0.64f, rect.height * 0.64f);
            DrawRect(iconRect, new Color(0.04f, 0.12f, 0.20f, 0.90f));
            DrawFrameCorners(iconRect, ACCENT_CYAN, iconRect.width * 0.40f, 2f);
            DrawRect(new Rect(iconRect.x + iconRect.width * 0.16f, iconRect.y + iconRect.height * 0.24f, iconRect.width * 0.52f, 3f), ACCENT_CYAN);
            DrawRect(new Rect(iconRect.x + iconRect.width * 0.24f, iconRect.y + iconRect.height * 0.43f, iconRect.width * 0.42f, 3f), ACCENT_CYAN);
            DrawRect(new Rect(iconRect.x + iconRect.width * 0.10f, iconRect.y + iconRect.height * 0.62f, iconRect.width * 0.60f, 3f), ACCENT_CYAN);

            Rect labelRect = new Rect(iconRect.xMax + 12f, rect.y, rect.width - iconRect.width - 12f, rect.height);
            DrawFittedLabel(new Rect(labelRect.x, labelRect.y + labelRect.height * 0.08f, labelRect.width, labelRect.height * 0.68f), "BLADE SPINNERS", titleBarStyle, Color.white, 14);
            GUIStyle subStyle = FitLabelStyle(bodyLabelStyle, "PLACEHOLDER LOGO / RUNTIME GARAGE", labelRect.width, 10, labelRect.height * 0.3f);
            Color prev = GUI.contentColor;
            GUI.contentColor = new Color(0.62f, 0.82f, 0.95f, 0.8f);
            GUI.Label(new Rect(labelRect.x, labelRect.y + labelRect.height * 0.62f, labelRect.width, labelRect.height * 0.3f), "PLACEHOLDER LOGO / RUNTIME GARAGE", subStyle);
            GUI.contentColor = prev;
        }

        private void DrawMainBottomBar(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.02f, 0.05f, 0.09f, 0.94f), new Color(0.03f, 0.08f, 0.14f, 0.96f), ACCENT_CYAN, 3f);
            float pad = Mathf.Clamp(10f * GetUiScale(), 10f, 18f);
            float gap = Mathf.Clamp(12f * GetUiScale(), 10f, 18f);
            float buttonH = rect.height - pad * 2f;
            float autoW = Mathf.Clamp(rect.width * 0.20f, 180f, 280f);
            float saveW = Mathf.Clamp(rect.width * 0.20f, 180f, 280f);
            float startW = Mathf.Clamp(rect.width * 0.28f, 220f, 360f);

            Rect autoRect = new Rect(rect.x + pad, rect.y + pad, autoW, buttonH);
            Rect saveRect = new Rect(autoRect.xMax + gap, rect.y + pad, saveW, buttonH);
            Rect startRect = new Rect(rect.xMax - pad - startW, rect.y + pad, startW, buttonH);

            if (ActionBtn("AUTO OPTIMIZE", autoRect, ACCENT_CYAN, false))
                AutoOptimizeCurrentBuild();
            if (ActionBtn("SAVE BUILD", saveRect, new Color(0.18f, 0.62f, 1f, 1f), false))
                buildSlotPickerOpen = !buildSlotPickerOpen;
            if (ActionBtn("START RUN", startRect, ACCENT_ORANGE, false))
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
            int sw = Screen.width;
            int sh = Screen.height;
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
            DrawFittedLabel(new Rect(topRect.x + 16f, topRect.y + 8f, topRect.width * 0.28f, topRect.height - 16f), title, titleBarStyle, Color.white, 12);

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
            Rect garageTab = new Rect(shell.x + gutter, tabY, 180f * uiScale, tabH);
            Rect inventoryTab = new Rect(garageTab.xMax + gutter, tabY, 180f * uiScale, tabH);
            Rect settingsTab = new Rect(inventoryTab.xMax + gutter, tabY, 180f * uiScale, tabH);
            if (TopTabBtn("GARAGE", garageTab, pausePanel == MenuPanel.Home))
                SetPausePanel(MenuPanel.Home);
            if (TopTabBtn("INVENTORY", inventoryTab, pausePanel == MenuPanel.Inventory))
                SetPausePanel(MenuPanel.Inventory);
            if (TopTabBtn("SETTINGS", settingsTab, pausePanel == MenuPanel.Settings))
                SetPausePanel(MenuPanel.Settings);

            Rect contentRect = new Rect(shell.x + gutter, garageTab.yMax + gutter, shell.width - gutter * 2f, shell.yMax - garageTab.yMax - gutter * 2f);
            switch (pausePanel)
            {
                case MenuPanel.Inventory:
                    DrawFramedContentPanel(contentRect, "RUN INVENTORY", delegate
                    {
                        DrawInventoryPanel(true);
                    });
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
            BeyPart hoveredPart = DrawGarageLoadoutPanel(leftRect, loadout, stats, showBuildManagement);
            DrawGarageStagePanel(centerRect, loadout, runPlayer != null, runPlayer);

            BeyPart detailPart = hoveredPart;
            if (detailPart == null && garageSwapSlot.HasValue)
                loadout.TryGetValue(garageSwapSlot.Value, out detailPart);
            DrawGarageInfoPanel(rightRect, loadout, detailPart, stats);
        }

        private BeyPart DrawGarageLoadoutPanel(Rect area, Dictionary<PartType, BeyPart> loadout, BeyStatBlock stats, bool showBuildManagement)
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
            BeyPart hoveredPart = null;
            Rect hoveredRow = Rect.zero;
            float currentY = listRect.y + 6f;
            foreach (PartType type in PART_DISPLAY_ORDER)
            {
                Rect row = new Rect(listRect.x + 4f, currentY, listRect.width - 8f, rowH);
                currentY += rowH + 6f;
                loadout.TryGetValue(type, out BeyPart part);
                bool hovered = row.Contains(Event.current.mousePosition);
                DrawRect(row, hovered ? new Color(0.08f, 0.18f, 0.28f, 0.92f) : new Color(0.04f, 0.08f, 0.14f, 0.88f));
                DrawRect(new Rect(row.x, row.yMax - 2f, row.width, 2f), hovered ? ACCENT_CYAN : new Color(1f, 1f, 1f, 0.06f));

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

                if (hovered)
                {
                    hoveredPart = part;
                    hoveredRow = row;
                }

                if (GUI.Button(row, GUIContent.none, GUIStyle.none))
                    garageSwapSlot = type;
            }

            float statsY = Mathf.Min(currentY + 10f, area.yMax - area.height * 0.28f);
            Rect statsRect = new Rect(area.x + pad, statsY, area.width - pad * 2f, area.yMax - statsY - pad);
            DrawOverallStatBars(statsRect, stats);

            // Draw tooltip last so it renders on top of the stats section
            if (hoveredPart != null)
            {
                float tooltipH = 122f;
                float tooltipY = hoveredRow.yMax + 4f;
                // If the tooltip would overlap the stats section, flip it above the hovered row
                if (tooltipY + tooltipH > statsY)
                    tooltipY = hoveredRow.y - tooltipH - 4f;
                tooltipY = Mathf.Clamp(tooltipY, area.y + 4f, area.yMax - tooltipH - 4f);
                Rect tooltip = new Rect(area.x + pad + 10f, tooltipY, area.width - pad * 2f - 20f, tooltipH);
                DrawCompactPartTooltip(tooltip, hoveredPart);
            }

            return hoveredPart;
        }

        private void DrawGarageStagePanel(Rect area, Dictionary<PartType, BeyPart> loadout, bool useRunInventory, PlayerManager runPlayer)
        {
            DrawPanelFrame(area, new Color(0.02f, 0.06f, 0.11f, 0.94f), new Color(0.05f, 0.10f, 0.16f, 0.95f), ACCENT_CYAN, 2f);

            float pad = Mathf.Clamp(14f * GetUiScale(), 12f, 22f);
            Rect inner = new Rect(area.x + pad, area.y + pad, area.width - pad * 2f, area.height - pad * 2f);
            DrawFittedLabel(new Rect(inner.x, inner.y, inner.width, 28f), useRunInventory ? "RUN GARAGE" : "GARAGE", sectionLabelStyle, ACCENT_CYAN, 10);

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
            DrawFittedLabel(hintRect, garageSwapSlot.HasValue ? $"SWAPPING {garageSwapSlot.Value.ToString().ToUpper()}" : "CLICK A PART NODE TO OPEN ITS SWAP MODAL", bodyLabelStyle, new Color(0.74f, 0.90f, 1f, 0.82f), 10);

            Rect swapModalRect = new Rect(area.xMax - Mathf.Clamp(area.width * 0.38f, 260f, 360f), area.y + 18f, Mathf.Clamp(area.width * 0.36f, 250f, 340f), area.height * 0.58f);

            bool modalOpen = garageSwapSlot.HasValue;
            bool suppressBackgroundInteraction = modalOpen
                && Event.current != null
                && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDrag)
                && swapModalRect.Contains(Event.current.mousePosition);

            // Re-draw orbit slots with click suppression when modal overlays this area.
            // (Keep visuals identical; only disable behind-modal interaction.)
            DrawOrbitSlot(faceBoltRect, PartType.FaceBolt, loadout, "FACE BOLT", suppressBackgroundInteraction);
            DrawOrbitSlot(energyRingRect, PartType.EnergyRing, loadout, "ENERGY RING", suppressBackgroundInteraction);
            DrawOrbitSlot(fusionRect, PartType.FusionWheel, loadout, "FUSION WHEEL", suppressBackgroundInteraction);
            DrawOrbitSlot(trackRect, PartType.Track, loadout, "TRACK", suppressBackgroundInteraction);
            DrawOrbitSlot(tipRect, PartType.Tip, loadout, "TIP", suppressBackgroundInteraction);

            // Preview drag handled after orbit buttons so orbit clicks are not consumed first.
            // Also suppress drag input while interacting with the modal.
            if (previewTexture != null && !suppressBackgroundInteraction)
                HandlePreviewDragInput(previewRect);

            DrawGarageSwapModal(swapModalRect, loadout, useRunInventory, runPlayer);

            if (suppressBackgroundInteraction)
                Event.current.Use();

            // Close modal when clicking anywhere outside it (orbit slot clicks are already consumed before this point)
            if (garageSwapSlot.HasValue
                && Event.current != null
                && Event.current.type == EventType.MouseDown
                && !swapModalRect.Contains(Event.current.mousePosition))
            {
                garageSwapSlot = null;
                Event.current.Use();
            }
        }

        private void DrawOrbitSlot(Rect rect, PartType type, Dictionary<PartType, BeyPart> loadout, string label, bool suppressInteraction = false)
        {
            loadout.TryGetValue(type, out BeyPart part);
            bool active = garageSwapSlot == type;
            DrawRect(rect, active ? new Color(0.08f, 0.20f, 0.32f, 0.96f) : new Color(0.04f, 0.09f, 0.15f, 0.92f));
            DrawFrameCorners(rect, active ? ACCENT_CYAN : new Color(ACCENT_CYAN.r, ACCENT_CYAN.g, ACCENT_CYAN.b, 0.45f), rect.width * 0.24f, 2f);
            DrawRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), active ? ACCENT_CYAN : new Color(1f, 1f, 1f, 0.08f));

            Rect labelRect = new Rect(rect.x - 8f, rect.y - 26f, rect.width + 16f, 20f);
            DrawFittedLabel(labelRect, label, sectionLabelStyle, Color.white, 9);

            float iconSize = Mathf.Min(rect.width * 0.68f, rect.height - 38f);
            float availableH = rect.height - 26f - 26f; // top padding to bottom label
            Rect iconRect = new Rect(rect.center.x - iconSize * 0.5f, rect.y + 8f + (availableH - iconSize) * 0.5f, iconSize, iconSize);
            if (type != PartType.FaceBolt && partPreviewTextures.TryGetValue(type, out RenderTexture partRT) && partRT != null && partRT.IsCreated())
                GUI.DrawTexture(iconRect, partRT, ScaleMode.ScaleToFit, true);
            else
                DrawPartSprite(iconRect, part);
            DrawFittedLabel(new Rect(rect.x + 6f, rect.yMax - 26f, rect.width - 12f, 18f), part != null ? PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant() : "EMPTY", bodyLabelStyle, Color.white, 9);

            if (!suppressInteraction && GUI.Button(rect, GUIContent.none, GUIStyle.none))
                garageSwapSlot = garageSwapSlot == type ? (PartType?)null : type;
        }

        private void DrawGarageSwapModal(Rect area, Dictionary<PartType, BeyPart> loadout, bool useRunInventory, PlayerManager runPlayer)
        {
            if (!garageSwapSlot.HasValue)
                return;

            PartType slot = garageSwapSlot.Value;
            List<BeyPart> sourceParts = useRunInventory
                ? runContext.Player?.GetRunInventory()?.GetAllParts() ?? new List<BeyPart>()
                : ownedParts;
            List<BeyPart> parts = GetPartsByType(sourceParts, slot);

            // Queue swap-part 3D previews when the slot changes
            if (lastRenderedSwapSlot != slot)
            {
                foreach (var rt in swapPartPreviewCache.Values)
                    if (rt != null) rt.Release();
                swapPartPreviewCache.Clear();
                lastRenderedSwapSlot = slot;
                swapPreviewQueue = new List<BeyPart>(parts);
                swapPreviewsDirty = true;
            }

            DrawPanelFrame(area, new Color(0.04f, 0.08f, 0.14f, 0.98f), new Color(0.06f, 0.12f, 0.18f, 0.99f), ACCENT_CYAN, 2f);
            DrawFittedLabel(new Rect(area.x + 10f, area.y + 8f, area.width - 46f, 24f), $"SWAP {slot.ToString().ToUpper()}", sectionLabelStyle, ACCENT_CYAN, 10);
            Rect closeRect = new Rect(area.xMax - 30f, area.y + 6f, 22f, 22f);
            if (ActionBtn("X", closeRect, ACCENT_RED, true))
            {
                garageSwapSlot = null;
                return;
            }

            Rect listRect = new Rect(area.x + 8f, area.y + 38f, area.width - 16f, area.height - 46f);
            GUILayout.BeginArea(listRect);
            garageSwapScroll = GUILayout.BeginScrollView(garageSwapScroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (parts.Count == 0)
            {
                GUILayout.Label("NO PARTS AVAILABLE FOR THIS SLOT.", bodyLabelStyle);
            }
            else
            {
                for (int i = 0; i < parts.Count; i++)
                {
                    BeyPart part = parts[i];
                    if (part == null)
                        continue;

                    Rect row = GUILayoutUtility.GetRect(10f, Mathf.Clamp(72f * GetUiScale(), 66f, 88f), GUILayout.ExpandWidth(true));
                    DrawRect(row, new Color(0f, 0f, 0f, 0.22f));
                    DrawRect(new Rect(row.x, row.yMax - 2f, row.width, 2f), new Color(1f, 1f, 1f, 0.06f));

                    float iconSz = Mathf.Min(row.height - 16f, 44f);
                    Rect iconRect = new Rect(row.x + 8f, row.center.y - iconSz * 0.5f, iconSz, iconSz);
                    if (slot != PartType.FaceBolt && swapPartPreviewCache.TryGetValue(part.GetInstanceID(), out RenderTexture swapRT) && swapRT != null && swapRT.IsCreated())
                        GUI.DrawTexture(iconRect, swapRT, ScaleMode.ScaleToFit, true);
                    else
                        DrawPartSprite(iconRect, part);
                    DrawFittedLabel(new Rect(iconRect.xMax + 8f, row.y + 8f, row.width * 0.48f, 22f), PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant(), bodyLabelStyle, Color.white, 10);
                    DrawFittedLabel(new Rect(iconRect.xMax + 8f, row.y + 32f, row.width * 0.38f, 18f), $"POWER {Mathf.RoundToInt(GetPartPowerScore(part))}", bodyLabelStyle, new Color(0.70f, 0.88f, 1f, 0.86f), 10);
                    DrawRarityPill(new Rect(row.xMax - 150f, row.y + 10f, 64f, row.height - 20f), part.Rarity, part.Rarity.ToString().ToUpperInvariant());

                    Rect equipRect = new Rect(row.xMax - 78f, row.y + 10f, 68f, row.height - 20f);
                    if (ActionBtn("EQUIP", equipRect, ACCENT_CYAN, false))
                    {
                        EquipPartFromGarage(slot, part, useRunInventory, runPlayer);
                        loadout[slot] = part;
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawGarageInfoPanel(Rect area, Dictionary<PartType, BeyPart> loadout, BeyPart detailPart, BeyStatBlock stats)
        {
            DrawPanelFrame(area, new Color(0.03f, 0.07f, 0.13f, 0.94f), new Color(0.05f, 0.11f, 0.18f, 0.95f), ACCENT_CYAN, 2f);
            float pad = Mathf.Clamp(12f * GetUiScale(), 10f, 18f);
            Rect content = new Rect(area.x + pad, area.y + pad, area.width - pad * 2f, area.height - pad * 2f);
            GUILayout.BeginArea(content);

            if (detailPart != null)
            {
                DrawSelectedPartCard(detailPart, "PART DATA", true);
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

        private void DrawCompactPartTooltip(Rect area, BeyPart part)
        {
            if (part == null)
                return;

            DrawPanelFrame(area, new Color(0.05f, 0.09f, 0.15f, 0.98f), new Color(0.06f, 0.12f, 0.18f, 0.98f), ACCENT_CYAN, 2f);
            float pad = 8f;
            DrawFittedLabel(new Rect(area.x + pad, area.y + 6f, area.width - pad * 2f, 20f), PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant(), sectionLabelStyle, Color.white, 10);
            DrawFittedLabel(new Rect(area.x + pad, area.y + 28f, area.width - pad * 2f, 18f), $"{part.PartType.ToString().ToUpper()}  |  {Mathf.RoundToInt(GetPartPowerScore(part))} PWR", bodyLabelStyle, new Color(0.72f, 0.88f, 1f, 0.85f), 10);

            List<string> lines = BuildPartDetailLines(part);
            float lineY = area.y + 48f;
            int count = Mathf.Min(lines.Count, 3);
            for (int i = 0; i < count; i++)
            {
                DrawFittedLabel(new Rect(area.x + pad, lineY + i * 18f, area.width - pad * 2f, 18f), lines[i], bodyLabelStyle, Color.white, 9);
            }
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
                Mathf.Clamp01(stats.Weight / 48f * 0.65f + stats.MassBasedStaminaDrainRate / 1.8f * 0.20f + (stats.EquippedAbility != null ? 0.15f : 0f)),
                Mathf.Clamp01(stats.Weight / 60f * 0.30f + (2.2f - stats.TotalStaminaDrainRate) / 2.2f * 0.40f + stats.UphillResistanceMultiplier / 1.8f * 0.30f),
                Mathf.Clamp01((2.4f - stats.TotalStaminaDrainRate) / 2.4f * 0.70f + (2.0f - stats.BehaviorBasedStaminaDrainModifier) / 2.0f * 0.30f),
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
            DrawRect(rect, new Color(rarityColor.r * 0.35f, rarityColor.g * 0.35f, rarityColor.b * 0.35f, 0.95f));
            DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), rarityColor);
            DrawFittedLabel(rect, label, bodyLabelStyle, Color.white, 8);
        }

        private static Color GetRarityColor(RarityTier rarity)
        {
            switch (rarity)
            {
                case RarityTier.Uncommon:
                    return new Color(0.20f, 0.82f, 0.38f, 1f);
                case RarityTier.Rare:
                    return new Color(0.18f, 0.58f, 1f, 1f);
                case RarityTier.Epic:
                    return new Color(0.68f, 0.35f, 0.98f, 1f);
                case RarityTier.Legendary:
                    return new Color(1f, 0.56f, 0.12f, 1f);
                default:
                    return new Color(0.62f, 0.66f, 0.72f, 1f);
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
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
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
            return enabled && GUI.Button(rect, GUIContent.none, GUIStyle.none);
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
                    selectedMainMenuLoadout[type] = bestPart;
            }

            RefreshPreviewFromLoadout(selectedMainMenuLoadout);
            ShowTransientUiMessage("Auto optimize equipped the highest rated owned parts.");
            AutoSave();
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
                    return rarityBoost + part.Weight * 1.7f + part.MassBasedStaminaDrainRate * 10f;
                case PartType.EnergyRing:
                    return rarityBoost + part.ManaPoolSize * 0.36f + part.ManaRegenRate * 1.1f;
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
            rootState   = RootUiState.InRun;
            mainMenuPanel = MenuPanel.Home;
            pausePanel    = MenuPanel.Home;
            selectedInventorySlot = null;
            garageSwapSlot = null;
            buildSlotPickerOpen = false;
            ResetPreviewRotationState();
            ApplySettingsToCameraController(runContext.CameraController);
            ApplySettingsToPlayer(runContext.Player);
            Time.timeScale = 1f;
            UpdateCursorState();
        }

        private void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            RuntimeRunBuilder.ClearRunObjectsForMainMenu();
            rootState     = RootUiState.MainMenu;
            mainMenuPanel = MenuPanel.Home;
            pausePanel    = MenuPanel.Home;
            selectedInventorySlot = null;
            garageSwapSlot = null;
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
            garageSwapSlot = null;
            buildSlotPickerOpen = false;
            ResetPreviewRotationState();
        }

        private void SetPausePanel(MenuPanel panel)
        {
            if (pausePanel == panel)
                return;

            pausePanel = panel;
            garageSwapSlot = null;
            ResetPreviewRotationState();
        }

        private void ResetPreviewRotationState()
        {
            previewManualPitch = 0f;
            previewManualYaw = 0f;
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

            rootState = RootUiState.BetweenArenas;
            pausePanel = MenuPanel.Home;
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
                Debug.Log("[BladeSpinners] Run complete. Transferring all loot and returning to main menu.");
                List<BeyPart> allRunParts = runContext.Player?.GetRunInventory()?.GetAllParts() ?? new List<BeyPart>();
                TransferPartsToMainInventory(allRunParts);
                ReturnToMainMenu();
                return;
            }

            Dictionary<PartType, BeyPart> nextLoadout = GetCurrentRunLoadout(runContext.Player);
            List<BeyPart> carriedInventory = runContext.Player?.GetRunInventory()?.GetAllParts() ?? new List<BeyPart>();
            int enemyCount = UnityEngine.Random.Range(2, 5);
            Time.timeScale = 1f;

            runContext = RuntimeRunBuilder.BuildRandomTestRun(
                nextLoadout,
                ownedParts,
                enemyParts,
                progression.RunSeed,
                enemyCount,
                progression,
                carriedInventory);

            rootState = RootUiState.InRun;
            pausePanel = MenuPanel.Home;
            selectedInventorySlot = null;
            deathOverlayPreviewPrepared = false;
            ApplySettingsToCameraController(runContext.CameraController);
            ApplySettingsToPlayer(runContext.Player);
            UpdateCursorState();
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
            if (rootState != RootUiState.MainMenu)
                return;

            Camera existingMain = Camera.main;
            if (existingMain != null)
            {
                fallbackMenuCamera = existingMain;
                // Ensure sensible clear settings for the menu background
                fallbackMenuCamera.clearFlags = CameraClearFlags.SolidColor;
                fallbackMenuCamera.backgroundColor = new Color(0.03f, 0.03f, 0.04f, 1f);
                return;
            }

            if (fallbackMenuCamera != null)
                return;

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

            fallbackMenuCamera = camera;
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
            if (rootState == RootUiState.MainMenu || rootState == RootUiState.Paused || rootState == RootUiState.BetweenArenas)
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

                previewManualPitch = Mathf.Clamp(previewManualPitch - delta.y * 0.30f, -40f, 40f);

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
            GUIStyle fitted = new GUIStyle(source);
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
            GUIUtility.RotateAroundPivot(angleDeg, new Vector2(xCenter, Screen.height * 0.5f));
            DrawRect(new Rect(xCenter - width * 0.5f, -200f, width, Screen.height + 400f), color);
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
            GUIStyle fittedStyle = FitLabelStyle(source, label, usableWidth, minFontSize, usableHeight);
            Color previousColor = GUI.contentColor;
            GUI.contentColor = textColor;
            GUI.Label(rect, label, fittedStyle);
            GUI.contentColor = previousColor;
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
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
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

            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  STYLE BUILDER
        // ══════════════════════════════════════════════════════════════════════════

        private void EnsureStyles()
        {
            if (titleBarStyle != null && styleScreenW == Screen.width && styleScreenH == Screen.height)
                return;

            styleScreenW = Screen.width;
            styleScreenH = Screen.height;
            float uiScale = GetUiScale();

            int bigSize  = Mathf.Clamp(Mathf.RoundToInt(42f * uiScale), 28, 82);
            int navSize  = Mathf.Clamp(Mathf.RoundToInt(28f * uiScale), 18, 52);
            int bodySize = Mathf.Clamp(Mathf.RoundToInt(18f * uiScale), 12, 32);
            int statSize = Mathf.Clamp(Mathf.RoundToInt(17f * uiScale), 11, 30);
            int inlineButtonSize = Mathf.Clamp(Mathf.RoundToInt(20f * uiScale), 14, 34);
            int scaledHorizontalPadding = Mathf.RoundToInt(Mathf.Clamp(8f * uiScale, 8f, 18f));
            int scaledVerticalPadding = Mathf.RoundToInt(Mathf.Clamp(3f * uiScale, 3f, 8f));

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
            float widthScale = Screen.width / 1920f;
            float heightScale = Screen.height / 1080f;
            float rawScale = Mathf.Min(widthScale, heightScale);

            if (rawScale > 1f)
                rawScale = 1f + (rawScale - 1f) * 0.72f;

            return Mathf.Clamp(rawScale, 0.85f, 1.8f);
        }
    }
}
