using BladeSpinners.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BladeSpinners.Editor
{
    [InitializeOnLoad]
    public static class RuntimeMenuHierarchyGenerator
    {
        private const string RuntimeUiObjectName = "RuntimeGameUiController";

        static RuntimeMenuHierarchyGenerator()
        {
            EditorApplication.delayCall += () => EnsureRuntimeMenuInHierarchy();
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        [MenuItem("Blade Spinners/Setup/Create Runtime Menu In Hierarchy")]
        public static void CreateRuntimeMenuInHierarchy()
        {
            EnsureRuntimeMenuInHierarchy(markSceneDirty: true, selectCreatedObject: true);
        }

        [MenuItem("Blade Spinners/Setup/Create Runtime Menu In Hierarchy", true)]
        private static bool CreateRuntimeMenuInHierarchyValidate()
        {
            return !Application.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            EnsureRuntimeMenuInHierarchy();
        }

        private static void EnsureRuntimeMenuInHierarchy(bool markSceneDirty = false, bool selectCreatedObject = false)
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            RuntimeGameUiController existing = Object.FindFirstObjectByType<RuntimeGameUiController>();
            if (existing != null)
                return;

            GameObject go = new GameObject(RuntimeUiObjectName);
            Undo.RegisterCreatedObjectUndo(go, "Create RuntimeGameUiController");
            go.AddComponent<RuntimeGameUiController>();

            if (markSceneDirty)
                EditorSceneManager.MarkSceneDirty(scene);

            if (selectCreatedObject)
                Selection.activeGameObject = go;

            Debug.Log("[RuntimeMenuHierarchyGenerator] Created RuntimeGameUiController in hierarchy.");
        }
    }
}
