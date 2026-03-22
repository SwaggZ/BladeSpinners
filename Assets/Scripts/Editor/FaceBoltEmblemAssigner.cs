using UnityEngine;
using UnityEditor;
using BladeSpinners.Gameplay.Parts;
using System.Reflection;

namespace BladeSpinners.Editor
{
    public static class FaceBoltEmblemAssigner
    {
        private const string IconsFolder = "Assets/Parts/Icons";
        private const string FaceBoltFolder = "Assets/Parts/Face Bolts";

        [MenuItem("Blade Spinners/Assign Face Bolt Emblems")]
        public static void AssignAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:BeyPart", new[] { FaceBoltFolder });
            FieldInfo emblemField = typeof(BeyPart).GetField("faceBoltEmblem",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (emblemField == null)
            {
                Debug.LogError("[FaceBoltEmblemAssigner] Could not find faceBoltEmblem field on BeyPart.");
                return;
            }

            int assigned = 0;
            int skipped = 0;

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                BeyPart part = AssetDatabase.LoadAssetAtPath<BeyPart>(assetPath);
                if (part == null || part.PartType != BladeSpinners.Core.PartType.FaceBolt)
                    continue;

                // Derive icon filename: "Abyssal Tide_FaceBolt" → "abyssal_tide"
                string baseName = part.name;
                int suffixIdx = baseName.LastIndexOf("_FaceBolt");
                if (suffixIdx > 0)
                    baseName = baseName.Substring(0, suffixIdx);

                string iconName = baseName.Replace(" ", "_").ToLowerInvariant();
                string iconPath = $"{IconsFolder}/{iconName}.png";

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                if (sprite == null)
                {
                    Debug.LogWarning($"[FaceBoltEmblemAssigner] No icon found for '{part.name}' at '{iconPath}'");
                    skipped++;
                    continue;
                }

                emblemField.SetValue(part, sprite);
                EditorUtility.SetDirty(part);
                assigned++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[FaceBoltEmblemAssigner] Done — {assigned} assigned, {skipped} skipped.");
        }
    }
}
