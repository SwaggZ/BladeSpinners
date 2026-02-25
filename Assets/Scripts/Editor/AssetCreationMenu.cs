using UnityEditor;
using UnityEngine;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Core;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Editor menu items for creating Beyblade parts and other assets.
    /// </summary>
    public class AssetCreationMenu
    {
        [MenuItem("Assets/Create/Blade Spinners/Tip Part")]
        public static void CreateTipPart()
        {
            CreateBeyPart(PartType.Tip, "New Tip");
        }

        [MenuItem("Assets/Create/Blade Spinners/Track Part")]
        public static void CreateTrackPart()
        {
            CreateBeyPart(PartType.Track, "New Track");
        }

        [MenuItem("Assets/Create/Blade Spinners/Fusion Wheel Part")]
        public static void CreateFusionWheelPart()
        {
            CreateBeyPart(PartType.FusionWheel, "New Fusion Wheel");
        }

        [MenuItem("Assets/Create/Blade Spinners/Energy Ring Part")]
        public static void CreateEnergyRingPart()
        {
            CreateBeyPart(PartType.EnergyRing, "New Energy Ring");
        }

        [MenuItem("Assets/Create/Blade Spinners/Face Bolt Part")]
        public static void CreateFaceBoltPart()
        {
            CreateBeyPart(PartType.FaceBolt, "New Face Bolt");
        }

        private static void CreateBeyPart(PartType partType, string defaultName)
        {
            BeyPart part = ScriptableObject.CreateInstance<BeyPart>();
            
            // Set basic properties
            var nameField = typeof(BeyPart).GetField("partName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            nameField?.SetValue(part, defaultName);

            var typeField = typeof(BeyPart).GetField("partType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            typeField?.SetValue(part, partType);

            var occupiesField = typeof(BeyPart).GetField("occupiesSlots", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (occupiesField != null)
            {
                var list = new System.Collections.Generic.List<PartType> { partType };
                occupiesField.SetValue(part, list);
            }

            var idField = typeof(BeyPart).GetField("partID", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            idField?.SetValue(part, System.Guid.NewGuid().ToString());

            // Save
            string path = EditorUtility.SaveFilePanelInProject(
                $"Save {partType} Part", 
                defaultName, 
                "asset", 
                $"Save the {partType} part"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(part, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = part;
                Debug.Log($"✅ Created {partType} part at {path}");
            }
        }
    }
}
