using BladeSpinners.Gameplay.PartDebugging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BladeSpinners.Editor
{
    public static class PartsDebugSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/PartsDebugScene.unity";

        [MenuItem("GameObject/Blade Spinners/Create Parts Debug Scene")]
        public static void CreatePartsDebugScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject cameraObj = new GameObject("Debug Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.11f, 1f);
            cameraObj.transform.position = new Vector3(10f, 8f, -12f);
            cameraObj.transform.rotation = Quaternion.Euler(18f, 35f, 0f);
            cameraObj.AddComponent<DebugFlyCameraController>();

            GameObject worldBuilderObj = new GameObject("PartDebugWorldBuilder");
            worldBuilderObj.AddComponent<PartDebugWorldBuilder>();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[PartsDebugSceneSetup] Failed to save scene to {ScenePath}");
                return;
            }

            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Selection.activeGameObject = worldBuilderObj;
            Debug.Log("[PartsDebugSceneSetup] PartsDebugScene created. Press Play to spawn the full part debug world.");
        }
    }
}