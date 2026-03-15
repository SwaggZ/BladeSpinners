using UnityEngine;
using UnityEditor;
using BladeSpinners.Core;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Dockable editor window that mirrors all GameManager balance sliders.
    /// Open via: Blade Spinners → Game Manager
    /// Works in Play mode — changes apply instantly.
    /// </summary>
    public class GameManagerWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private bool showGlobal = true;
        private bool showEnemy = true;

        [MenuItem("Blade Spinners/Game Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<GameManagerWindow>("Game Manager");
            window.minSize = new Vector2(320, 400);
        }

        private void OnGUI()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
                gm = Object.FindFirstObjectByType<GameManager>();

            if (gm == null)
            {
                EditorGUILayout.HelpBox(
                    "No GameManager found.\nEnter Play mode or create a Test Arena first.",
                    MessageType.Warning);
                return;
            }

            Undo.RecordObject(gm, "GameManager Tweak");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // ═══════════════════════════════════════════════════════
            //  GLOBAL (affects ALL beys)
            // ═══════════════════════════════════════════════════════
            showGlobal = EditorGUILayout.Foldout(showGlobal, "Global (all beys)", true, EditorStyles.foldoutHeader);
            if (showGlobal)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
                gm.speedMultiplier        = EditorGUILayout.Slider("Speed",        gm.speedMultiplier,        0f, 3f);
                gm.accelerationMultiplier = EditorGUILayout.Slider("Acceleration", gm.accelerationMultiplier, 0f, 3f);
                gm.turnSpeedMultiplier    = EditorGUILayout.Slider("Turn Speed",   gm.turnSpeedMultiplier,    0f, 3f);
                gm.jumpMultiplier         = EditorGUILayout.Slider("Jump",         gm.jumpMultiplier,         0f, 3f);
                gm.boostMultiplier        = EditorGUILayout.Slider("Boost",        gm.boostMultiplier,        0f, 3f);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Combat", EditorStyles.boldLabel);
                gm.knockbackMultiplier    = EditorGUILayout.Slider("Knockback",     gm.knockbackMultiplier,    0f, 3f);
                gm.spinExchangeMultiplier = EditorGUILayout.Slider("Spin Exchange", gm.spinExchangeMultiplier, 0f, 3f);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Stamina / Spin", EditorStyles.boldLabel);
                gm.spinDrainMultiplier    = EditorGUILayout.Slider("Spin Drain",    gm.spinDrainMultiplier,    0f, 3f);
                gm.startingSpinMultiplier = EditorGUILayout.Slider("Starting Spin", gm.startingSpinMultiplier, 0f, 3f);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Mana", EditorStyles.boldLabel);
                gm.manaRegenMultiplier    = EditorGUILayout.Slider("Mana Regen",   gm.manaRegenMultiplier,    0f, 3f);
                gm.manaPoolMultiplier     = EditorGUILayout.Slider("Mana Pool",    gm.manaPoolMultiplier,     0f, 3f);
                gm.abilityCostMultiplier  = EditorGUILayout.Slider("Ability Cost", gm.abilityCostMultiplier,  0f, 3f);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
                gm.visualSpinMultiplier   = EditorGUILayout.Slider("Visual Spin",  gm.visualSpinMultiplier,   0f, 3f);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8);

            // ═══════════════════════════════════════════════════════
            //  ENEMY (stacks on top of global: final = global × enemy)
            // ═══════════════════════════════════════════════════════
            showEnemy = EditorGUILayout.Foldout(showEnemy, "Enemy (stacks × global)", true, EditorStyles.foldoutHeader);
            if (showEnemy)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
                DrawStackedSlider("Speed",        ref gm.enemySpeedMultiplier,        gm.speedMultiplier);
                DrawStackedSlider("Acceleration", ref gm.enemyAccelerationMultiplier, gm.accelerationMultiplier);
                DrawStackedSlider("Turn Speed",   ref gm.enemyTurnSpeedMultiplier,    gm.turnSpeedMultiplier);
                DrawStackedSlider("Jump",         ref gm.enemyJumpMultiplier,         gm.jumpMultiplier);
                DrawStackedSlider("Boost",        ref gm.enemyBoostMultiplier,        gm.boostMultiplier);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Combat", EditorStyles.boldLabel);
                DrawStackedSlider("Knockback",     ref gm.enemyKnockbackMultiplier,    gm.knockbackMultiplier);
                DrawStackedSlider("Spin Exchange", ref gm.enemySpinExchangeMultiplier, gm.spinExchangeMultiplier);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Stamina / Spin", EditorStyles.boldLabel);
                DrawStackedSlider("Spin Drain",    ref gm.enemySpinDrainMultiplier,    gm.spinDrainMultiplier);
                DrawStackedSlider("Starting Spin", ref gm.enemyStartingSpinMultiplier, gm.startingSpinMultiplier);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Mana", EditorStyles.boldLabel);
                DrawStackedSlider("Mana Regen",   ref gm.enemyManaRegenMultiplier,    gm.manaRegenMultiplier);
                DrawStackedSlider("Mana Pool",    ref gm.enemyManaPoolMultiplier,     gm.manaPoolMultiplier);
                DrawStackedSlider("Ability Cost", ref gm.enemyAbilityCostMultiplier,  gm.abilityCostMultiplier);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
                DrawStackedSlider("Visual Spin",  ref gm.enemyVisualSpinMultiplier,   gm.visualSpinMultiplier);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(12);

            // ── Reset buttons ────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Global → 100%"))
            {
                gm.speedMultiplier = 1f; gm.accelerationMultiplier = 1f;
                gm.turnSpeedMultiplier = 1f; gm.jumpMultiplier = 1f;
                gm.boostMultiplier = 1f; gm.knockbackMultiplier = 1f;
                gm.spinExchangeMultiplier = 1f; gm.spinDrainMultiplier = 1f;
                gm.startingSpinMultiplier = 1f; gm.manaRegenMultiplier = 1f;
                gm.manaPoolMultiplier = 1f; gm.abilityCostMultiplier = 1f;
                gm.visualSpinMultiplier = 1f;
            }
            if (GUILayout.Button("Reset Enemy → 100%"))
            {
                gm.enemySpeedMultiplier = 1f; gm.enemyAccelerationMultiplier = 1f;
                gm.enemyTurnSpeedMultiplier = 1f; gm.enemyJumpMultiplier = 1f;
                gm.enemyBoostMultiplier = 1f; gm.enemyKnockbackMultiplier = 1f;
                gm.enemySpinExchangeMultiplier = 1f; gm.enemySpinDrainMultiplier = 1f;
                gm.enemyStartingSpinMultiplier = 1f; gm.enemyManaRegenMultiplier = 1f;
                gm.enemyManaPoolMultiplier = 1f; gm.enemyAbilityCostMultiplier = 1f;
                gm.enemyVisualSpinMultiplier = 1f;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            if (Application.isPlaying)
                Repaint();
        }

        /// <summary>
        /// Draws a slider for an enemy multiplier with a label showing the effective (stacked) value.
        /// </summary>
        private void DrawStackedSlider(string label, ref float enemyValue, float globalValue)
        {
            EditorGUILayout.BeginHorizontal();
            enemyValue = EditorGUILayout.Slider(label, enemyValue, 0f, 3f);
            float effective = globalValue * enemyValue;
            GUIStyle miniStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = effective < 0.99f ? new Color(1f, 0.5f, 0.3f) :
                           effective > 1.01f ? new Color(0.3f, 1f, 0.5f) :
                           Color.gray }
            };
            EditorGUILayout.LabelField($"= {effective:P0}", miniStyle, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
        }
    }
}
