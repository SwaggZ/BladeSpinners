using UnityEngine;
using UnityEditor;
using BladeSpinners.Core;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Editor window with sliders for configuring and generating test arenas.
    /// Open via: Blade Spinners → Test Arena Window
    /// </summary>
    public class ArenaEditorWindow : EditorWindow
    {
        private int seed = 42;
        private int outerWalls = 4;
        private int innerWalls = 2;
        private int staminaPickups = 2;
        private int manaPickups = 2;
        private int enemyCount = 2;

        [MenuItem("Blade Spinners/Test Arena Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<ArenaEditorWindow>("Test Arena");
            window.minSize = new Vector2(320, 280);
        }

        private void OnGUI()
        {
            GUILayout.Label("Test Arena Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            seed = EditorGUILayout.IntField("Seed", seed);

            EditorGUILayout.Space(4);
            GUILayout.Label("Arena Features", EditorStyles.boldLabel);

            outerWalls = EditorGUILayout.IntSlider("Outer Walls", outerWalls,
                0, GameConstants.ARENA_MAX_RIM_WALLS);

            innerWalls = EditorGUILayout.IntSlider("Inner Walls", innerWalls,
                0, GameConstants.ARENA_MAX_INNER_WALLS);

            EditorGUILayout.Space(4);
            GUILayout.Label("Pickup Placeholders", EditorStyles.boldLabel);

            staminaPickups = EditorGUILayout.IntSlider("Stamina Pickups", staminaPickups,
                0, GameConstants.ARENA_MAX_PICKUPS);

            manaPickups = EditorGUILayout.IntSlider("Mana Pickups", manaPickups,
                0, GameConstants.ARENA_MAX_PICKUPS);

            EditorGUILayout.Space(4);
            GUILayout.Label("Enemies", EditorStyles.boldLabel);

            enemyCount = EditorGUILayout.IntSlider("Enemy Count", enemyCount,
                0, GameConstants.ENEMY_MAX_PER_COMBAT_ROOM);

            EditorGUILayout.Space(12);

            if (GUILayout.Button("Generate Test Arena", GUILayout.Height(32)))
            {
                TestSceneSetup.CreateTestArena(seed, outerWalls, innerWalls, staminaPickups, manaPickups, enemyCount);
            }

            EditorGUILayout.Space(4);

            if (GUILayout.Button("Randomize Seed"))
            {
                seed = Random.Range(1, 99999);
            }
        }
    }
}
