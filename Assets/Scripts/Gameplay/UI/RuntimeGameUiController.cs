using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using BladeSpinners.Core;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Gameplay.UI
{
    public class RuntimeGameUiController : MonoBehaviour
    {
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

        private Vector2 ownedScroll;
        private Vector2 runScroll;

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
        private static readonly Color PANEL_DARK = new Color(0.09f, 0.09f, 0.11f, 0.97f);
        private static readonly Color ACCENT_YEL = new Color(1f, 0.87f, 0.00f, 1f);
        private static readonly Color BTN_DARK   = new Color(0.12f, 0.13f, 0.16f, 1f);
        private static readonly Color LIST_BG    = new Color(0.11f, 0.12f, 0.15f, 1f);
        private static readonly Color OVERLAY    = new Color(0f, 0f, 0f, 0.76f);
        private static readonly Color RED_DANGER = new Color(0.65f, 0.07f, 0.07f, 1f);

        private const string StarterConfigResourcePath = "StarterPartsConfig";
        private readonly BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        // ══════════════════════════════════════════════════════════════════════════
        //  BOOTSTRAP
        // ══════════════════════════════════════════════════════════════════════════

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
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

            DrawRect(new Rect(badge.x - 2f, badge.y - 2f, badge.width + 4f, badge.height + 4f), Color.black);
            DrawRect(badge, new Color(0f, 0f, 0f, 0.55f));
            DrawRect(new Rect(badge.x, badge.y, badge.width, 3f), ACCENT_YEL);

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

            DrawRect(new Rect(panel.x - 3f, panel.y - 3f, panel.width + 6f, panel.height + 6f), Color.black);
            DrawRect(panel, new Color(0.08f, 0.08f, 0.1f, 0.92f));
            DrawRect(new Rect(panel.x, panel.y, panel.width, 5f), ACCENT_YEL);

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
            countdownSubStyle.normal.textColor = ACCENT_YEL;

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

            DrawRect(new Rect(panel.x - 4f, panel.y - 4f, panel.width + 8f, panel.height + 8f), Color.black);
            DrawRect(panel, PANEL_DARK);
            DrawRect(new Rect(panel.x, panel.y, panel.width, 8f), RED_DANGER);

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
            GUIStyle deathTitleStyle = new GUIStyle(titleBarStyle);
            deathTitleStyle.normal.textColor = Color.white;
            deathTitleStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(panel.height * 0.08f, 28f, 84f));
            deathTitleStyle.wordWrap = false;
            GUIStyle deathReasonStyle = new GUIStyle(sectionLabelStyle)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(panel.height * 0.036f, 14f, 38f)),
                wordWrap = true
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

            float lootH = hasLoot ? Mathf.Clamp(remainingH * (showKillerBuild ? 0.32f : 0.46f), 90f, 230f) : 0f;
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
                    HandlePreviewDragInput(previewRect);
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
                GUILayout.Label(PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant(), resolvedItemStyle, GUILayout.ExpandWidth(true));
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

            float scrollH = Mathf.Max(40f, sectionH - sectionLabelStyle.fontSize * 2f - 10f);
            lootScroll = GUILayout.BeginScrollView(lootScroll, GUILayout.Height(scrollH));
            GUIStyle rowLabel = new GUIStyle(bodyLabelStyle) { alignment = TextAnchor.MiddleLeft };
            for (int i = 0; i < lootEligibleParts.Count; i++)
            {
                BeyPart part       = lootEligibleParts[i];
                bool    isSelected = lootSelectedFlags[i];
                bool    canToggle  = isSelected || sel < lootMaxTransferCount;

                GUILayout.BeginHorizontal();
                string partLabel = $"{PartDisplayNameFormatter.ToShortDisplayName(part).ToUpperInvariant()}  [{part.Rarity.ToString().ToUpper()}]";
                GUILayout.Label(partLabel, rowLabel, GUILayout.ExpandWidth(true));
                string toggleLabel = isSelected ? "\u2713 KEEP" : (canToggle ? "KEEP" : "\u2014");
                if (InlineBtn(toggleLabel, btnW, btnH, isSelected) && canToggle)
                    lootSelectedFlags[i] = !lootSelectedFlags[i];
                GUILayout.EndHorizontal();
                GUILayout.Space(2f);
            }
            GUILayout.EndScrollView();
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
            float gutter = Mathf.Clamp(sw * 0.004f, 4f, 12f);
            float border = Mathf.Clamp(sw * 0.0032f, 3f, 8f);
            float pad = Mathf.Clamp(sw * 0.009f, 8f, 22f);

            // ── Full black background ────────────────────────────────────────────
            DrawRect(new Rect(0, 0, sw, sh), BG_BLACK);

            // ── Diagonal yellow accent stripes (right quarter decoration) ────────
            DrawDiagonalStripe(sw * 0.68f,        24f, ACCENT_YEL,                                    -13f);
            DrawDiagonalStripe(sw * 0.68f + 36f,   8f, new Color(1f, 0.87f, 0f, 0.28f),               -13f);
            DrawDiagonalStripe(sw * 0.68f + 50f,   4f, new Color(1f, 1f, 1f, 0.10f),                  -13f);

            // ── Title bar (full-width yellow) ────────────────────────────────────
            float titleH = Mathf.Clamp(64f * uiScale, 58f, 110f);
            DrawRect(new Rect(0, 0, sw, titleH), ACCENT_YEL);
            DrawRect(new Rect(0, titleH, sw, border), Color.black);
            GUILayout.BeginArea(new Rect(pad, 0f, sw - pad * 2f, titleH));
            GUILayout.Label("BLADE SPINNERS", titleBarStyle);
            GUILayout.EndArea();

            // ── Left nav panel ───────────────────────────────────────────────────
            float navY  = titleH + border;
            float botH  = Mathf.Clamp(62f * uiScale, 58f, 108f);
            float itemH = Mathf.Clamp(48f * uiScale, 44f, 84f);
            float navWMin = Mathf.Clamp(sw * 0.24f, 240f, 460f);
            float navWMax = Mathf.Clamp(sw * 0.34f, 420f, 920f);
            float navWTarget = sw * 0.285f;
            float navW = Mathf.Clamp(navWTarget, navWMin, navWMax);
            float prevX = navW + gutter;
            float prevW = sw - prevX;

            // If preview area gets too small, shrink nav and reflow
            float minPreviewW = Mathf.Clamp(sw * 0.40f, 520f, 1700f);
            if (prevW < minPreviewW)
            {
                navW = Mathf.Max(navWMin, sw - minPreviewW - gutter);
                prevX = navW + gutter;
                prevW = sw - prevX;
            }

            DrawRect(new Rect(0, navY, navW, sh - navY), PANEL_DARK);
            DrawRect(new Rect(0, navY, border, sh - navY), ACCENT_YEL);    // yellow left rail
            DrawRect(new Rect(navW, navY, border, sh - navY), Color.black); // right border

            float iy = navY + gutter;
            float navInnerX = border;
            float navInnerW = navW - border;
            float navGap = Mathf.Clamp(sh * 0.004f, 2f, 6f);
            if (NavBtn("▸  INVENTORY", new Rect(navInnerX, iy, navInnerW, itemH), mainMenuPanel == MenuPanel.Inventory)) SetMainMenuPanel(MenuPanel.Inventory);
            iy += itemH + navGap;
            if (NavBtn("▸  SETTINGS",  new Rect(navInnerX, iy, navInnerW, itemH), mainMenuPanel == MenuPanel.Settings))  SetMainMenuPanel(MenuPanel.Settings);
            iy += itemH + navGap;
            if (NavBtn("▸  KEYBINDS",  new Rect(navInnerX, iy, navInnerW, itemH), mainMenuPanel == MenuPanel.Keybinds))  SetMainMenuPanel(MenuPanel.Keybinds);
            iy += itemH + navGap;
            if (NavBtn("▸  LOADOUT",   new Rect(navInnerX, iy, navInnerW, itemH), mainMenuPanel == MenuPanel.Home))       SetMainMenuPanel(MenuPanel.Home);
            iy += itemH + gutter;

            float subH = sh - iy - botH - gutter;
            if (subH > 30f)
            {
                GUILayout.BeginArea(new Rect(border + pad * 0.3f, iy, navW - border - pad * 0.6f, subH));
                switch (mainMenuPanel)
                {
                    case MenuPanel.Home:      DrawSelectedLoadoutSummary(); break;
                    case MenuPanel.Inventory: DrawInventoryPanel(false);    break;
                    case MenuPanel.Settings:  DrawSettingsPanel();          break;
                    case MenuPanel.Keybinds:  DrawKeybindPanel();           break;
                }
                GUILayout.EndArea();
            }

            // ── START RUN — big yellow button at bottom of nav panel ─────────────
            Rect startR = new Rect(navInnerX, sh - botH, navInnerW, botH);
            DrawRect(new Rect(startR.x - border, startR.y - border, startR.width + border * 2f, startR.height + border * 2f), Color.black);
            DrawRect(startR, ACCENT_YEL);
            GUILayout.BeginArea(startR);
            GUILayout.Label("▶  START RUN", startButtonStyle);
            GUILayout.EndArea();
            if (GUI.Button(startR, GUIContent.none, GUIStyle.none)) StartRun();

            // ── Right preview + stats panel ──────────────────────────────────────
            DrawPreviewAndStats(new Rect(prevX, navY, prevW, sh - navY), null);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  PAUSE MENU
        // ══════════════════════════════════════════════════════════════════════════

        private void DrawPauseMenu()
        {
            int sw = Screen.width, sh = Screen.height;
            float uiScale = GetUiScale();
            float gutter = Mathf.Clamp(sw * 0.004f, 4f, 12f);
            float border = Mathf.Clamp(sw * 0.0032f, 3f, 8f);
            float pad = Mathf.Clamp(sw * 0.009f, 8f, 22f);

            // Dark overlay over the game
            DrawRect(new Rect(0, 0, sw, sh), OVERLAY);

            float pWMin = Mathf.Clamp(400f * uiScale, 380f, 820f);
            float pWMax = Mathf.Clamp(sw * 0.40f, 640f, 1220f);
            float pW  = Mathf.Clamp(sw * 0.31f, pWMin, pWMax);
            float pY  = Mathf.Max(gutter * 3f, sh * 0.035f);
            float pH  = sh - pY * 2f;
            float px  = Mathf.Max(gutter * 3f, sw * 0.012f);

            // Black outer border + dark panel + yellow left rail
            DrawRect(new Rect(px - border, pY - border, pW + border * 2f, pH + border * 2f), Color.black);
            DrawRect(new Rect(px, pY, pW, pH), PANEL_DARK);
            DrawRect(new Rect(px, pY, border, pH), ACCENT_YEL);

            // PAUSED header (yellow bar)
            float hdrH = Mathf.Clamp(58f * uiScale, 52f, 94f);
            DrawRect(new Rect(px, pY, pW, hdrH), ACCENT_YEL);
            DrawRect(new Rect(px, pY + hdrH, pW, border), Color.black);
            GUILayout.BeginArea(new Rect(px + pad, pY, pW - pad * 2f, hdrH));
            GUILayout.Label("PAUSED", titleBarStyle);
            GUILayout.EndArea();

            float btnH = Mathf.Clamp(50f * uiScale, 46f, 80f);
            float bw   = pW - border;
            float by   = pY + hdrH + gutter;
            float bx   = px + border;
            float navGap = Mathf.Clamp(sh * 0.004f, 2f, 6f);

            if (NavBtn("▶  RESUME",          new Rect(bx, by, bw, btnH - navGap), false)) TogglePause();
            by += btnH + navGap;
            if (NavBtn("▸  RUN INVENTORY",   new Rect(bx, by, bw, btnH - navGap), pausePanel == MenuPanel.Inventory)) SetPausePanel(MenuPanel.Inventory);
            by += btnH + navGap;
            if (NavBtn("▸  SETTINGS",        new Rect(bx, by, bw, btnH - navGap), pausePanel == MenuPanel.Settings))  SetPausePanel(MenuPanel.Settings);
            by += btnH + navGap;
            if (NavBtn("▸  KEYBINDS",        new Rect(bx, by, bw, btnH - navGap), pausePanel == MenuPanel.Keybinds))  SetPausePanel(MenuPanel.Keybinds);
            by += btnH + gutter;

            // Thin yellow separator
            DrawRect(new Rect(px + pad, by + border, pW - pad * 2f, border), new Color(1f, 0.87f, 0f, 0.45f));
            by += border + gutter;

            // Return to Main Menu — red danger button
            Rect retR = new Rect(bx, by, bw, btnH - navGap);
            DrawRect(retR, RED_DANGER);
            DrawRect(new Rect(retR.x, retR.yMax, retR.width, border), Color.black);
            GUILayout.BeginArea(retR);
            GUI.contentColor = Color.white;
            GUILayout.Label("✕  RETURN TO MAIN MENU", navButtonStyle);
            GUI.contentColor = Color.white;
            GUILayout.EndArea();
            if (GUI.Button(retR, GUIContent.none, GUIStyle.none)) ReturnToMainMenu();
            by += btnH + gutter;

            float subH = pH - (by - pY) - gutter;
            if (subH > 30f)
            {
                GUILayout.BeginArea(new Rect(px + border + pad * 0.3f, by, pW - border - pad * 0.6f, subH));
                switch (pausePanel)
                {
                    case MenuPanel.Inventory: DrawInventoryPanel(true);  break;
                    case MenuPanel.Settings:  DrawSettingsPanel();       break;
                    case MenuPanel.Keybinds:  DrawKeybindPanel();        break;
                    default: break;
                }
                GUILayout.EndArea();
            }

            // Right preview + stats panel
            float prevX = px + pW + gutter;
            float prevW = sw - prevX - gutter;
            if (prevX + 200f < sw)
            {
                DrawPreviewAndStats(new Rect(prevX, pY, prevW, pH), runContext.Player);
            }
        }

        private void DrawArenaIntermissionMenu()
        {
            int sw = Screen.width, sh = Screen.height;
            float uiScale = GetUiScale();
            float gutter = Mathf.Clamp(sw * 0.004f, 4f, 12f);
            float border = Mathf.Clamp(sw * 0.0032f, 3f, 8f);
            float pad = Mathf.Clamp(sw * 0.009f, 8f, 22f);

            DrawRect(new Rect(0, 0, sw, sh), OVERLAY);

            float pWMin = Mathf.Clamp(420f * uiScale, 400f, 860f);
            float pWMax = Mathf.Clamp(sw * 0.40f, 680f, 1260f);
            float pW = Mathf.Clamp(sw * 0.31f, pWMin, pWMax);
            float pY = Mathf.Max(gutter * 3f, sh * 0.035f);
            float pH = sh - pY * 2f;
            float px = Mathf.Max(gutter * 3f, sw * 0.012f);

            DrawRect(new Rect(px - border, pY - border, pW + border * 2f, pH + border * 2f), Color.black);
            DrawRect(new Rect(px, pY, pW, pH), PANEL_DARK);
            DrawRect(new Rect(px, pY, border, pH), ACCENT_YEL);

            float hdrH = Mathf.Clamp(58f * uiScale, 52f, 94f);
            DrawRect(new Rect(px, pY, pW, hdrH), ACCENT_YEL);
            DrawRect(new Rect(px, pY + hdrH, pW, border), Color.black);
            GUILayout.BeginArea(new Rect(px + pad, pY, pW - pad * 2f, hdrH));
            GUILayout.Label("ARENA CLEAR", titleBarStyle);
            GUILayout.EndArea();

            float btnH = Mathf.Clamp(50f * uiScale, 46f, 80f);
            float bw = pW - border;
            float by = pY + hdrH + gutter;
            float bx = px + border;
            float navGap = Mathf.Clamp(sh * 0.004f, 2f, 6f);

            string advanceLabel = runContext.Progression != null && runContext.Progression.IsLastArena
                ? "▶  FINISH RUN"
                : "▶  NEXT ARENA";
            if (NavBtn(advanceLabel, new Rect(bx, by, bw, btnH - navGap), false)) AdvanceToNextArenaOrFinishRun();
            by += btnH + navGap;
            if (NavBtn("▸  RUN INVENTORY", new Rect(bx, by, bw, btnH - navGap), pausePanel == MenuPanel.Inventory)) SetPausePanel(MenuPanel.Inventory);
            by += btnH + navGap;
            if (NavBtn("▸  SETTINGS", new Rect(bx, by, bw, btnH - navGap), pausePanel == MenuPanel.Settings)) SetPausePanel(MenuPanel.Settings);
            by += btnH + navGap;
            if (NavBtn("▸  KEYBINDS", new Rect(bx, by, bw, btnH - navGap), pausePanel == MenuPanel.Keybinds)) SetPausePanel(MenuPanel.Keybinds);
            by += btnH + gutter;

            Rect retR = new Rect(bx, by, bw, btnH - navGap);
            DrawRect(retR, RED_DANGER);
            DrawRect(new Rect(retR.x, retR.yMax, retR.width, border), Color.black);
            GUILayout.BeginArea(retR);
            GUI.contentColor = Color.white;
            GUILayout.Label("✕  RETURN TO MAIN MENU", navButtonStyle);
            GUI.contentColor = Color.white;
            GUILayout.EndArea();
            if (GUI.Button(retR, GUIContent.none, GUIStyle.none)) ReturnToMainMenu();
            by += btnH + gutter;

            float subH = pH - (by - pY) - gutter;
            if (subH > 30f)
            {
                GUILayout.BeginArea(new Rect(px + border + pad * 0.3f, by, pW - border - pad * 0.6f, subH));
                switch (pausePanel)
                {
                    case MenuPanel.Inventory: DrawInventoryPanel(true); break;
                    case MenuPanel.Settings: DrawSettingsPanel(); break;
                    case MenuPanel.Keybinds: DrawKeybindPanel(); break;
                    default: break;
                }
                GUILayout.EndArea();
            }

            float prevX = px + pW + gutter;
            float prevW = sw - prevX - gutter;
            if (prevX + 200f < sw)
                DrawPreviewAndStats(new Rect(prevX, pY, prevW, pH), runContext.Player);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  SHARED SUB-PANELS
        // ══════════════════════════════════════════════════════════════════════════

        private void DrawInventoryPanel(bool isRunInventory)
        {
            float uiScale = GetUiScale();
            float inlineButtonW = Mathf.Clamp(88f * uiScale, 88f, 170f);
            float inlineButtonH = Mathf.Clamp(28f * uiScale, 28f, 42f);
            float rowMinH = Mathf.Clamp(34f * uiScale, 34f, 66f);
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

            foreach (PartType type in Enum.GetValues(typeof(PartType)))
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
            GUILayout.Space(Mathf.Clamp(8f * uiScale, 8f, 16f));
            GUILayout.Label($"SELECT {selectedType.ToString().ToUpper()}", sectionLabelStyle);

            if (parts.Count == 0)
            {
                GUILayout.Label("NO PARTS IN THIS SLOT TYPE.", bodyLabelStyle);
                return;
            }

            if (isRunInventory)
                runScroll = GUILayout.BeginScrollView(runScroll, GUILayout.MinHeight(Mathf.Clamp(180f * uiScale, 180f, 320f)), GUILayout.ExpandHeight(true));
            else
                ownedScroll = GUILayout.BeginScrollView(ownedScroll, GUILayout.MinHeight(Mathf.Clamp(180f * uiScale, 180f, 320f)), GUILayout.ExpandHeight(true));

            for (int i = 0; i < parts.Count; i++)
            {
                BeyPart part = parts[i];
                if (part == null) continue;

                GUILayout.BeginHorizontal(listItemStyle, GUILayout.MinHeight(rowMinH));
                GUILayout.Label($"{PartDisplayNameFormatter.ToShortDisplayName(part)}  [{part.Rarity.ToString().ToUpper()}]", rowLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(rowMinH - 6f));
                GUILayout.FlexibleSpace();
                if (InlineBtn("EQUIP", Mathf.Clamp(78f * uiScale, 78f, 148f), inlineButtonH))
                {
                    if (isRunInventory)
                    {
                        runContext.Player?.EquipPart(part);
                        RefreshPreviewFromLoadout(GetCurrentRunLoadout(runContext.Player));
                    }
                    else
                    {
                        selectedMainMenuLoadout[selectedType] = part;
                        RefreshPreviewFromLoadout(selectedMainMenuLoadout);
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
            // Panel background
            DrawRect(area, new Color(0.05f, 0.05f, 0.07f, 1f));
            DrawRect(new Rect(area.x, area.y, area.width, 4f), Color.black);

            // Preview label bar (black bg, yellow text)
            float barH = Mathf.Clamp(34f * uiScale, 30f, 58f);
            DrawRect(new Rect(area.x, area.y, area.width, barH), Color.black);
            GUILayout.BeginArea(new Rect(area.x + innerPad, area.y + 2f, area.width - innerPad * 2f, barH - 4f));
            GUILayout.Label("BEY PREVIEW", sectionLabelStyle);
            GUILayout.EndArea();

            // Preview texture
            float prevTexH = area.height * 0.50f;
            Rect texRect = new Rect(area.x + innerPad * 0.7f, area.y + barH + 6f, area.width - innerPad * 1.4f, prevTexH - barH - 10f);
            if (previewTexture != null)
            {
                GUI.DrawTexture(texRect, previewTexture, ScaleMode.ScaleToFit, true);
                HandlePreviewDragInput(texRect);
            }

            // Stats section — yellow bar separator + black header
            float statsY = area.y + prevTexH;
            DrawRect(new Rect(area.x, statsY, area.width, 4f), ACCENT_YEL);
            DrawRect(new Rect(area.x, statsY + 4f, area.width, barH), Color.black);
            GUILayout.BeginArea(new Rect(area.x + innerPad, statsY + 6f, area.width - innerPad * 2f, barH - 4f));
            GUILayout.Label("STATS", sectionLabelStyle);
            GUILayout.EndArea();
            DrawRect(new Rect(area.x, statsY + 4f + barH, area.width, 3f), Color.black);

            BeyStatBlock stats = GetStatsForDisplay(runPlayer);
            if (stats != null)
            {
                float spin  = GetCurrentSpinForDisplay(runPlayer);
                float mana  = GetCurrentManaForDisplay(runPlayer);
                float rowY  = statsY + 4f + barH + 6f;
                float rowH  = area.yMax - rowY - 8f;
                GUIStyle fittedStatStyle = FitLabelStyle(statRowStyle, $"MANA     {mana:0.0} / {stats.ManaPoolSize:0.0}", area.width - innerPad * 2f, 10);
                GUILayout.BeginArea(new Rect(area.x + innerPad, rowY, area.width - innerPad * 2f, rowH));
                GUILayout.Label($"SPIN     {spin:0.0} / {GameConstants.MAX_SPIN:0}",   fittedStatStyle);
                GUILayout.Label($"MANA     {mana:0.0} / {stats.ManaPoolSize:0.0}",     fittedStatStyle);
                GUILayout.Label($"WEIGHT   {stats.Weight:0.0}",                        fittedStatStyle);
                GUILayout.Label($"TIP      {stats.TipBehavior.ToString().ToUpper()}",  fittedStatStyle);
                GUILayout.Label($"DRAIN    {stats.TotalStaminaDrainRate:0.00}",        fittedStatStyle);
                GUILayout.Label($"REGEN    {stats.ManaRegenRate:0.0}",                 fittedStatStyle);
                GUILayout.EndArea();
            }
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
        }

        private void DrawKeybindPanel()
        {
            GUIStyle keybindStyle = new GUIStyle(bodyLabelStyle)
            {
                wordWrap = true,
                clipping = TextClipping.Clip
            };

            GUILayout.Label("KEYBINDS", sectionLabelStyle);
            GUILayout.Space(4);
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

            GUILayout.Label("CURRENT LOADOUT", sectionLabelStyle);
            GUILayout.Space(4);
            foreach (PartType type in Enum.GetValues(typeof(PartType)))
            {
                selectedMainMenuLoadout.TryGetValue(type, out BeyPart part);
                string name = part != null ? PartDisplayNameFormatter.ToShortDisplayName(part).ToUpper() : "NONE";
                GUILayout.Label($"{type.ToString().ToUpper()}   {name}", summaryStyle);
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  STATE MANAGEMENT
        // ══════════════════════════════════════════════════════════════════════════

        private void StartRun()
        {
            Debug.Log("[BladeSpinners] StartRun() called");
            int seed       = UnityEngine.Random.Range(1000, int.MaxValue);
            int enemyCount = UnityEngine.Random.Range(2, 5);
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
            ResetPreviewRotationState();
        }

        private void SetPausePanel(MenuPanel panel)
        {
            if (pausePanel == panel)
                return;

            pausePanel = panel;
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
            pausePanel = MenuPanel.Inventory;
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
            previewCamera.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
            previewCamera.enabled        = false;

            previewTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            previewCamera.targetTexture = previewTexture;
            ApplyPreviewManualRotation();
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

        private static GUIStyle FitLabelStyle(GUIStyle source, string text, float maxWidth, int minFontSize)
        {
            GUIStyle fitted = new GUIStyle(source);
            if (string.IsNullOrEmpty(text) || maxWidth <= 0f)
                return fitted;

            while (fitted.fontSize > minFontSize && fitted.CalcSize(new GUIContent(text)).x > maxWidth)
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

        /// <summary>
        /// Draws a nav-style button. Active buttons use yellow bg + black text.
        /// Returns true when clicked.
        /// </summary>
        /// <summary>Themed inline button drawn inline in GUILayout flow (black bg, yellow text/border). Returns true when clicked.</summary>
        private bool InlineBtn(string label, float width, float height, bool active = false)
        {
            Rect r = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            Color bg = active ? ACCENT_YEL : BTN_DARK;
            Color fg = active ? Color.black : ACCENT_YEL;
            DrawRect(r, bg);
            DrawRect(new Rect(r.x, r.yMax, r.width, Mathf.Max(2f, r.height * 0.05f)), Color.black);
            if (active) DrawRect(new Rect(r.x, r.y, Mathf.Max(3f, r.width * 0.03f), r.height), Color.black);
            GUI.contentColor = fg;
            GUI.Label(r, label, inlineActionButtonStyle);
            GUI.contentColor = Color.white;
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        private bool NavBtn(string label, Rect rect, bool active)
        {
            Color bg = active ? ACCENT_YEL : BTN_DARK;
            Color fg = active ? Color.black : Color.white;

            DrawRect(rect, bg);
            DrawRect(new Rect(rect.x, rect.yMax, rect.width, 3f), Color.black);
            if (active) DrawRect(new Rect(rect.x, rect.y, 5f, rect.height), Color.black);

            GUILayout.BeginArea(rect);
            GUI.contentColor = fg;
            GUILayout.Label(label, navButtonStyle);
            GUI.contentColor = Color.white;
            GUILayout.EndArea();

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
            int navSize  = Mathf.Clamp(Mathf.RoundToInt(27f * uiScale), 16, 50);
            int bodySize = Mathf.Clamp(Mathf.RoundToInt(18f * uiScale), 12, 32);
            int statSize = Mathf.Clamp(Mathf.RoundToInt(17f * uiScale), 11, 30);
            int inlineButtonSize = Mathf.Clamp(Mathf.RoundToInt(16f * uiScale), 12, 28);
            int scaledHorizontalPadding = Mathf.RoundToInt(Mathf.Clamp(8f * uiScale, 8f, 18f));
            int scaledVerticalPadding = Mathf.RoundToInt(Mathf.Clamp(4f * uiScale, 4f, 10f));

            listTex = MakeTex(LIST_BG);
            sliderTrackTex = MakeTex(new Color(1f, 1f, 1f, 0f));
            sliderThumbTex = MakeTex(ACCENT_YEL);

            titleBarStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = bigSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = Color.black }
            };

            navButtonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = navSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(Mathf.RoundToInt(Mathf.Clamp(14f * uiScale, 12f, 26f)), scaledHorizontalPadding, 0, 0),
                clipping  = TextClipping.Clip,
                normal    = { textColor = Color.white },
                hover     = { textColor = Color.black },
                active    = { textColor = Color.black }
            };

            startButtonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = navSize + 2,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                clipping  = TextClipping.Clip,
                normal    = { textColor = Color.black }
            };

            inlineActionButtonStyle = new GUIStyle(navButtonStyle)
            {
                fontSize  = inlineButtonSize,
                alignment = TextAnchor.MiddleCenter,
                padding   = new RectOffset(scaledHorizontalPadding, scaledHorizontalPadding, scaledVerticalPadding, scaledVerticalPadding),
                clipping  = TextClipping.Clip,
                wordWrap  = false,
                normal    = { textColor = ACCENT_YEL, background = null },
                hover     = { textColor = Color.white, background = null },
                active    = { textColor = Color.white, background = null }
            };

            sectionLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = bodySize + 1,
                fontStyle = FontStyle.Bold,
                clipping  = TextClipping.Clip,
                normal    = { textColor = ACCENT_YEL }
            };

            bodyLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = bodySize,
                clipping = TextClipping.Clip,
                wordWrap = true,
                normal   = { textColor = new Color(0.88f, 0.90f, 0.95f, 1f) }
            };

            statRowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = statSize,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal   = { textColor = Color.white }
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
