using BladeSpinners.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Edit-mode window for spawning named bowl prototype galleries before entering play mode.
    /// </summary>
    public sealed class BowlPrototypeWindow : EditorWindow
    {
        private const string RootObjectName = "BowlPrototypeGallery";

        private bool clearPrevious = true;
        private int columns = 4;
        private float spacing = 34f;
        private int angularSegments = 96;
        private int radialSegments = 36;
        private string layerName = "Default";

        [MenuItem("Blade Spinners/Test Bowl Window")]
        public static void ShowWindow()
        {
            BowlPrototypeWindow window = GetWindow<BowlPrototypeWindow>("Test Bowls");
            window.minSize = new Vector2(360f, 300f);
        }

        private void OnGUI()
        {
            GUILayout.Label("Bowl Prototype Gallery", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generates 12 named bowl examples in the current scene without entering play mode.",
                MessageType.Info);

            EditorGUILayout.Space(6f);
            clearPrevious = EditorGUILayout.Toggle("Clear Previous", clearPrevious);
            columns = EditorGUILayout.IntSlider("Columns", columns, 1, 6);
            spacing = EditorGUILayout.Slider("Spacing", spacing, 12f, 80f);

            EditorGUILayout.Space(4f);
            GUILayout.Label("Mesh Detail", EditorStyles.boldLabel);
            angularSegments = EditorGUILayout.IntSlider("Angular Segments", angularSegments, 24, 192);
            radialSegments = EditorGUILayout.IntSlider("Radial Segments", radialSegments, 10, 80);
            layerName = EditorGUILayout.TextField("Arena Layer", layerName);

            EditorGUILayout.Space(12f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate 12 Bowls", GUILayout.Height(30f)))
                    Generate();

                if (GUILayout.Button("Clear", GUILayout.Height(30f)))
                    Clear();
            }

            EditorGUILayout.Space(8f);
            BowlPrototypeGalleryGenerator generator = FindGenerator();
            string status = generator == null
                ? "No gallery object in scene yet."
                : $"Ready: {generator.PrototypeCount} prototype presets available.";
            EditorGUILayout.LabelField(status, EditorStyles.miniLabel);
        }

        private static BowlPrototypeGalleryGenerator FindGenerator()
        {
            return Object.FindFirstObjectByType<BowlPrototypeGalleryGenerator>();
        }

        private BowlPrototypeGalleryGenerator GetOrCreateGenerator()
        {
            BowlPrototypeGalleryGenerator existing = FindGenerator();
            if (existing != null)
                return existing;

            GameObject root = new GameObject(RootObjectName);
            Undo.RegisterCreatedObjectUndo(root, "Create Bowl Prototype Gallery");
            BowlPrototypeGalleryGenerator generator = root.AddComponent<BowlPrototypeGalleryGenerator>();
            Selection.activeGameObject = root;
            return generator;
        }

        private void Generate()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[BowlPrototypeWindow] Generation is edit-mode only.");
                return;
            }

            BowlPrototypeGalleryGenerator generator = GetOrCreateGenerator();
            Undo.RegisterCompleteObjectUndo(generator.gameObject, "Generate Bowl Prototypes");

            generator.ConfigureGeneration(clearPrevious, columns, spacing, angularSegments, radialSegments, layerName);
            generator.GeneratePrototypes();

            EditorUtility.SetDirty(generator);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private void Clear()
        {
            if (Application.isPlaying)
                return;

            BowlPrototypeGalleryGenerator generator = FindGenerator();
            if (generator == null)
                return;

            Undo.RegisterCompleteObjectUndo(generator.gameObject, "Clear Bowl Prototypes");
            generator.ClearGenerated();
            EditorUtility.SetDirty(generator);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
