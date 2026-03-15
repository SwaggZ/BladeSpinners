using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Gameplay.UI
{
    public class RuntimeGameUiController : MonoBehaviour
    {
        // ── Enum types ───────────────────────────────────────────────────────────
        private enum RootUiState { MainMenu, InRun, Paused }
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
        private Transform     previewSpinChild;
        private BeyAssembler  previewAssembler;
        private BeyConfiguration previewConfig;

        // ── Settings sliders ────────────────────────────────────────────────────
        private float settingsVolume      = 1f;
        private float settingsSensitivity = 1f;

        // ── GUI styles ───────────────────────────────────────────────────────────
        private GUIStyle titleBarStyle;
        private GUIStyle navButtonStyle;
        private GUIStyle startButtonStyle;
        private GUIStyle sectionLabelStyle;
        private GUIStyle bodyLabelStyle;
        private GUIStyle statRowStyle;
        private GUIStyle listItemStyle;
        private Texture2D listTex;
        private int styleScreenW = -1;
        private int styleScreenH = -1;
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
            if (rootState == RootUiState.InRun || rootState == RootUiState.Paused)
            {
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame)
                    TogglePause();
            }

            // Spin the preview bey model
            if (previewSpinChild != null)
                previewSpinChild.Rotate(Vector3.up, 240f * Time.unscaledDeltaTime, Space.Self);
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
                }

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

        // ══════════════════════════════════════════════════════════════════════════
        //  MAIN MENU
        // ══════════════════════════════════════════════════════════════════════════

        private void DrawMainMenu()
        {
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
            float titleH = Mathf.Clamp(sh * 0.115f, 58f * uiScale, 88f * uiScale);
            DrawRect(new Rect(0, 0, sw, titleH), ACCENT_YEL);
            DrawRect(new Rect(0, titleH, sw, border), Color.black);
            GUILayout.BeginArea(new Rect(pad, 0f, sw - pad * 2f, titleH));
            GUILayout.Label("BLADE SPINNERS", titleBarStyle);
            GUILayout.EndArea();

            // ── Left nav panel ───────────────────────────────────────────────────
            float navY  = titleH + border;
            float botH  = Mathf.Clamp(sh * 0.105f, 64f * uiScale, 92f * uiScale);
            float itemH = Mathf.Clamp(sh * 0.086f, 48f * uiScale, 70f * uiScale);
            float navWMin = Mathf.Clamp(sw * 0.22f, 220f, 360f);
            float navWMax = Mathf.Clamp(sw * 0.37f, 360f, 540f);
            float navWTarget = sw * 0.34f;
            float navW = Mathf.Clamp(navWTarget, navWMin, navWMax);
            float prevX = navW + gutter;
            float prevW = sw - prevX;

            // If preview area gets too small, shrink nav and reflow
            float minPreviewW = Mathf.Clamp(sw * 0.46f, 520f, 880f);
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
            if (NavBtn("▸  INVENTORY", new Rect(navInnerX, iy, navInnerW, itemH), mainMenuPanel == MenuPanel.Inventory)) mainMenuPanel = MenuPanel.Inventory;
            iy += itemH + navGap;
            if (NavBtn("▸  SETTINGS",  new Rect(navInnerX, iy, navInnerW, itemH), mainMenuPanel == MenuPanel.Settings))  mainMenuPanel = MenuPanel.Settings;
            iy += itemH + navGap;
            if (NavBtn("▸  KEYBINDS",  new Rect(navInnerX, iy, navInnerW, itemH), mainMenuPanel == MenuPanel.Keybinds))  mainMenuPanel = MenuPanel.Keybinds;
            iy += itemH + navGap;
            if (NavBtn("▸  LOADOUT",   new Rect(navInnerX, iy, navInnerW, itemH), mainMenuPanel == MenuPanel.Home))       mainMenuPanel = MenuPanel.Home;
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

            float pW  = Mathf.Clamp(sw * 0.34f, 300f, 520f);
            float pY  = Mathf.Max(gutter * 3f, sh * 0.035f);
            float pH  = sh - pY * 2f;
            float px  = Mathf.Max(gutter * 3f, sw * 0.012f);

            // Black outer border + dark panel + yellow left rail
            DrawRect(new Rect(px - border, pY - border, pW + border * 2f, pH + border * 2f), Color.black);
            DrawRect(new Rect(px, pY, pW, pH), PANEL_DARK);
            DrawRect(new Rect(px, pY, border, pH), ACCENT_YEL);

            // PAUSED header (yellow bar)
            float hdrH = Mathf.Clamp(sh * 0.105f, 54f * uiScale, 84f * uiScale);
            DrawRect(new Rect(px, pY, pW, hdrH), ACCENT_YEL);
            DrawRect(new Rect(px, pY + hdrH, pW, border), Color.black);
            GUILayout.BeginArea(new Rect(px + pad, pY, pW - pad * 2f, hdrH));
            GUILayout.Label("PAUSED", titleBarStyle);
            GUILayout.EndArea();

            float btnH = Mathf.Clamp(sh * 0.088f, 50f * uiScale, 72f * uiScale);
            float bw   = pW - border;
            float by   = pY + hdrH + gutter;
            float bx   = px + border;
            float navGap = Mathf.Clamp(sh * 0.004f, 2f, 6f);

            if (NavBtn("▶  RESUME",          new Rect(bx, by, bw, btnH - navGap), false)) TogglePause();
            by += btnH + navGap;
            if (NavBtn("▸  RUN INVENTORY",   new Rect(bx, by, bw, btnH - navGap), pausePanel == MenuPanel.Inventory)) pausePanel = MenuPanel.Inventory;
            by += btnH + navGap;
            if (NavBtn("▸  SETTINGS",        new Rect(bx, by, bw, btnH - navGap), pausePanel == MenuPanel.Settings))  pausePanel = MenuPanel.Settings;
            by += btnH + navGap;
            if (NavBtn("▸  KEYBINDS",        new Rect(bx, by, bw, btnH - navGap), pausePanel == MenuPanel.Keybinds))  pausePanel = MenuPanel.Keybinds;
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

        // ══════════════════════════════════════════════════════════════════════════
        //  SHARED SUB-PANELS
        // ══════════════════════════════════════════════════════════════════════════

        private void DrawInventoryPanel(bool isRunInventory)
        {
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

            GUILayout.Space(6f);
            GUILayout.Label("CLICK A SLOT TO OPEN ITS PART LIST", bodyLabelStyle);
            GUILayout.Space(8f);

            foreach (PartType type in Enum.GetValues(typeof(PartType)))
            {
                currentLoadout.TryGetValue(type, out BeyPart equippedPart);
                string slotLabel = type.ToString().ToUpper();
                string equippedLabel = equippedPart != null
                    ? PartDisplayNameFormatter.ToShortDisplayName(equippedPart)
                    : "NONE";

                GUILayout.BeginHorizontal(listItemStyle);
                GUILayout.Label($"{slotLabel} : {equippedLabel}", bodyLabelStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("CHOOSE", navButtonStyle, GUILayout.Width(90f), GUILayout.Height(30f)))
                {
                    selectedInventorySlot = type;
                }
                GUILayout.EndHorizontal();
            }

            if (!selectedInventorySlot.HasValue)
                return;

            PartType selectedType = selectedInventorySlot.Value;
            List<BeyPart> parts = GetPartsByType(sourceParts, selectedType);
            GUILayout.Space(10f);
            GUILayout.Label($"SELECT {selectedType.ToString().ToUpper()}", sectionLabelStyle);

            if (parts.Count == 0)
            {
                GUILayout.Label("NO PARTS IN THIS SLOT TYPE.", bodyLabelStyle);
                return;
            }

            if (isRunInventory)
                runScroll = GUILayout.BeginScrollView(runScroll, GUILayout.MinHeight(180f));
            else
                ownedScroll = GUILayout.BeginScrollView(ownedScroll, GUILayout.MinHeight(180f));

            for (int i = 0; i < parts.Count; i++)
            {
                BeyPart part = parts[i];
                if (part == null) continue;

                GUILayout.BeginHorizontal(listItemStyle);
                GUILayout.Label($"{PartDisplayNameFormatter.ToShortDisplayName(part)}  [{part.Rarity.ToString().ToUpper()}]", bodyLabelStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("EQUIP", navButtonStyle, GUILayout.Width(72f), GUILayout.Height(28f)))
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
            // Panel background
            DrawRect(area, new Color(0.05f, 0.05f, 0.07f, 1f));
            DrawRect(new Rect(area.x, area.y, area.width, 4f), Color.black);

            // Preview label bar (black bg, yellow text)
            float barH = Mathf.Clamp(area.height * 0.072f, 32f * uiScale, 48f * uiScale);
            DrawRect(new Rect(area.x, area.y, area.width, barH), Color.black);
            GUILayout.BeginArea(new Rect(area.x + 12f, area.y + 2f, area.width - 24f, barH - 4f));
            GUILayout.Label("BEY PREVIEW", sectionLabelStyle);
            GUILayout.EndArea();

            // Preview texture
            float prevTexH = area.height * 0.50f;
            Rect texRect = new Rect(area.x + 8f, area.y + barH + 6f, area.width - 16f, prevTexH - barH - 10f);
            if (previewTexture != null)
                GUI.DrawTexture(texRect, previewTexture, ScaleMode.ScaleToFit, true);

            // Stats section — yellow bar separator + black header
            float statsY = area.y + prevTexH;
            DrawRect(new Rect(area.x, statsY, area.width, 4f), ACCENT_YEL);
            DrawRect(new Rect(area.x, statsY + 4f, area.width, barH), Color.black);
            GUILayout.BeginArea(new Rect(area.x + 12f, statsY + 6f, area.width - 24f, barH - 4f));
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
                GUILayout.BeginArea(new Rect(area.x + 12f, rowY, area.width - 24f, rowH));
                GUILayout.Label($"SPIN     {spin:0.0} / {GameConstants.MAX_SPIN:0}",   statRowStyle);
                GUILayout.Label($"MANA     {mana:0.0} / {stats.ManaPoolSize:0.0}",     statRowStyle);
                GUILayout.Label($"WEIGHT   {stats.Weight:0.0}",                        statRowStyle);
                GUILayout.Label($"TIP      {stats.TipBehavior.ToString().ToUpper()}",  statRowStyle);
                GUILayout.Label($"DRAIN    {stats.TotalStaminaDrainRate:0.00}",        statRowStyle);
                GUILayout.Label($"REGEN    {stats.ManaRegenRate:0.0}",                 statRowStyle);
                GUILayout.EndArea();
            }
        }

        private void DrawSettingsPanel()
        {
            GUILayout.Label("SETTINGS", sectionLabelStyle);
            GUILayout.Space(6);
            GUILayout.Label($"MASTER VOLUME   {settingsVolume:0.00}", bodyLabelStyle);
            settingsVolume = GUILayout.HorizontalSlider(settingsVolume, 0f, 1f);
            GUILayout.Space(4);
            GUILayout.Label($"CAM SENSITIVITY  {settingsSensitivity:0.00}", bodyLabelStyle);
            settingsSensitivity = GUILayout.HorizontalSlider(settingsSensitivity, 0.25f, 2f);
        }

        private void DrawKeybindPanel()
        {
            GUILayout.Label("KEYBINDS", sectionLabelStyle);
            GUILayout.Space(4);
            GUILayout.Label("MOVE         WASD",            bodyLabelStyle);
            GUILayout.Label("BOOST        LEFT SHIFT",      bodyLabelStyle);
            GUILayout.Label("JUMP         SPACE",           bodyLabelStyle);
            GUILayout.Label("ABILITY      E",               bodyLabelStyle);
            GUILayout.Label("LOCK-ON      MIDDLE MOUSE",    bodyLabelStyle);
            GUILayout.Label("CYCLE TGT    SCROLL WHEEL",    bodyLabelStyle);
            GUILayout.Label("PAUSE        ESC",             bodyLabelStyle);
        }

        private void DrawSelectedLoadoutSummary()
        {
            GUILayout.Label("CURRENT LOADOUT", sectionLabelStyle);
            GUILayout.Space(4);
            foreach (PartType type in Enum.GetValues(typeof(PartType)))
            {
                selectedMainMenuLoadout.TryGetValue(type, out BeyPart part);
                string name = part != null ? PartDisplayNameFormatter.ToShortDisplayName(part).ToUpper() : "NONE";
                GUILayout.Label($"{type.ToString().ToUpper()}   {name}", bodyLabelStyle);
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

            if (fallbackMenuCamera != null)
            {
                Destroy(fallbackMenuCamera.gameObject);
                fallbackMenuCamera = null;
            }

            try
            {
                runContext  = RuntimeRunBuilder.BuildRandomTestRun(selectedMainMenuLoadout, ownedParts, enemyParts, seed, enemyCount);
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
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            rootState     = RootUiState.MainMenu;
            mainMenuPanel = MenuPanel.Home;
            pausePanel    = MenuPanel.Home;
            selectedInventorySlot = null;
            EnsureFallbackMenuCamera();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        private void TogglePause()
        {
            if (rootState == RootUiState.InRun)
            {
                rootState      = RootUiState.Paused;
                Time.timeScale = 0f;
                pausePanel     = MenuPanel.Home;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
            else if (rootState == RootUiState.Paused)
            {
                rootState      = RootUiState.InRun;
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
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
            StarterPartsConfig starterConfig = Resources.Load<StarterPartsConfig>(StarterConfigResourcePath);
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
            }

            if (ownedParts == null || ownedParts.Count == 0)
                ownedParts = RuntimePartFactory.CreateStarterCatalog(1, Environment.TickCount);

            if (enemyParts == null || enemyParts.Count == 0)
                enemyParts = new List<BeyPart>(ownedParts);

            BuildDefaultLoadout();
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
            foreach (PartType type in Enum.GetValues(typeof(PartType)))
                previewConfig.UnequipPart(type);
            foreach (KeyValuePair<PartType, BeyPart> kv in loadout)
                if (kv.Value != null) previewAssembler.EquipPart(kv.Value);
            previewCamera?.Render();
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

            int bigSize  = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.047f), Mathf.RoundToInt(28f * uiScale), Mathf.RoundToInt(56f * uiScale));
            int navSize  = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.031f), Mathf.RoundToInt(16f * uiScale), Mathf.RoundToInt(32f * uiScale));
            int bodySize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.021f), Mathf.RoundToInt(12f * uiScale), Mathf.RoundToInt(22f * uiScale));
            int statSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.019f), Mathf.RoundToInt(11f * uiScale), Mathf.RoundToInt(20f * uiScale));

            listTex = MakeTex(LIST_BG);

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
                padding   = new RectOffset(14, 6, 0, 0),
                normal    = { textColor = Color.white },
                hover     = { textColor = Color.black },
                active    = { textColor = Color.black }
            };

            startButtonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = navSize + 2,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.black }
            };

            sectionLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = bodySize + 1,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = ACCENT_YEL }
            };

            bodyLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = bodySize,
                normal   = { textColor = new Color(0.88f, 0.90f, 0.95f, 1f) }
            };

            statRowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = statSize,
                normal   = { textColor = Color.white }
            };

            listItemStyle = new GUIStyle(GUI.skin.box)
            {
                margin  = new RectOffset(1, 1, 2, 2),
                padding = new RectOffset(8, 8, 4, 4),
                normal  = { background = listTex, textColor = Color.white }
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
            return Mathf.Clamp(Mathf.Min(widthScale, heightScale), 0.85f, 2.25f);
        }
    }
}
