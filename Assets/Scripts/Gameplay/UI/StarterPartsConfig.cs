using System;
using System.Collections.Generic;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BladeSpinners.Gameplay.UI
{
    [CreateAssetMenu(fileName = "StarterPartsConfig", menuName = "Blade Spinners/Starter Parts Config")]
    public class StarterPartsConfig : ScriptableObject
    {
        [Serializable]
        public struct StarterLoadoutSlot
        {
            public PartType slot;
            public BeyPart part;
        }

        [Header("Starter Ownership")]
        [SerializeField] private List<BeyPart> starterOwnedParts = new List<BeyPart>();

        [Header("Starter Base Set (Drag part assets by slot)")]
        [SerializeField] private BeyPart starterBaseTip;
        [SerializeField] private BeyPart starterBaseTrack;
        [SerializeField] private BeyPart starterBaseFusionWheel;
        [SerializeField] private BeyPart starterBaseEnergyRing;
        [SerializeField] private BeyPart starterBaseFaceBolt;

        [SerializeField] private List<BeyPart> starterBaseSetParts = new List<BeyPart>();
        [SerializeField] private List<StarterLoadoutSlot> starterLoadout = new List<StarterLoadoutSlot>();
        [SerializeField] private string starterBaseSetName = "Arctic Fox";
        [Header("Build-safe Runtime Catalog (all project parts)")]
        [SerializeField] private List<BeyPart> runtimePartCatalog = new List<BeyPart>();
        [SerializeField] private List<BeyPart> enemyPartPool = new List<BeyPart>();
        [SerializeField] private bool useStarterOwnedPartsForEnemyPool = true;
        [SerializeField, Range(0f, 2f)] private float enemyDepthRarityScale = 0f;

        public float EnemyDepthRarityScale => enemyDepthRarityScale;

        public List<BeyPart> GetOwnedStarterParts()
        {
            List<BeyPart> result = new List<BeyPart>();
            for (int i = 0; i < starterOwnedParts.Count; i++)
            {
                BeyPart part = starterOwnedParts[i];
                if (part != null && !result.Contains(part))
                {
                    result.Add(part);
                }
            }

            return result;
        }

        public BeyPart GetStarterLoadoutPart(PartType type)
        {
            for (int i = 0; i < starterLoadout.Count; i++)
            {
                StarterLoadoutSlot slot = starterLoadout[i];
                if (slot.slot == type && slot.part != null)
                {
                    return slot.part;
                }
            }

            return null;
        }

        public Dictionary<PartType, BeyPart> GetExplicitStarterBaseLoadout()
        {
            Dictionary<PartType, BeyPart> result = new Dictionary<PartType, BeyPart>();

            AddIfValid(result, PartType.Tip, starterBaseTip);
            AddIfValid(result, PartType.Track, starterBaseTrack);
            AddIfValid(result, PartType.FusionWheel, starterBaseFusionWheel);
            AddIfValid(result, PartType.EnergyRing, starterBaseEnergyRing);
            AddIfValid(result, PartType.FaceBolt, starterBaseFaceBolt);

            for (int i = 0; i < starterBaseSetParts.Count; i++)
            {
                BeyPart part = starterBaseSetParts[i];
                if (part == null)
                    continue;

                if (!result.ContainsKey(part.PartType))
                    result[part.PartType] = part;
            }

            return result;
        }

        private static void AddIfValid(Dictionary<PartType, BeyPart> result, PartType slot, BeyPart part)
        {
            if (part == null)
                return;

            if (part.PartType != slot)
                return;

            if (!result.ContainsKey(slot))
                result[slot] = part;
        }

        public Dictionary<PartType, BeyPart> GetPreferredStarterBaseLoadout(List<BeyPart> owned)
        {
            Dictionary<PartType, BeyPart> result = new Dictionary<PartType, BeyPart>();
            if (owned == null || owned.Count == 0)
                return result;

            string targetSet = NormalizeSetToken(starterBaseSetName);
            if (string.IsNullOrEmpty(targetSet))
                return result;

            for (int i = 0; i < owned.Count; i++)
            {
                BeyPart part = owned[i];
                if (part == null)
                    continue;

                if (result.ContainsKey(part.PartType))
                    continue;

                string partToken = NormalizeSetToken(part.PartName);
                if (!string.IsNullOrEmpty(partToken) && partToken.StartsWith(targetSet, StringComparison.Ordinal))
                {
                    result[part.PartType] = part;
                }
            }

            return result;
        }

        private static string NormalizeSetToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        public List<BeyPart> GetEnemyPartPool(List<BeyPart> fallbackOwned)
        {
            List<BeyPart> result = new List<BeyPart>();

            for (int i = 0; i < enemyPartPool.Count; i++)
            {
                BeyPart part = enemyPartPool[i];
                if (part != null && !result.Contains(part))
                    result.Add(part);
            }

            if (useStarterOwnedPartsForEnemyPool && fallbackOwned != null)
            {
                for (int i = 0; i < fallbackOwned.Count; i++)
                {
                    BeyPart part = fallbackOwned[i];
                    if (part != null && !result.Contains(part))
                        result.Add(part);
                }
            }

            return result;
        }

        public List<BeyPart> GetRuntimePartCatalog()
        {
            List<BeyPart> result = new List<BeyPart>();
            for (int i = 0; i < runtimePartCatalog.Count; i++)
            {
                BeyPart part = runtimePartCatalog[i];
                if (part != null && !result.Contains(part))
                    result.Add(part);
            }

            return result;
        }

        public bool HasCompleteExplicitBaseLoadout()
        {
            Dictionary<PartType, BeyPart> explicitBase = GetExplicitStarterBaseLoadout();
            return explicitBase.ContainsKey(PartType.Tip)
                && explicitBase.ContainsKey(PartType.Track)
                && explicitBase.ContainsKey(PartType.FusionWheel)
                && explicitBase.ContainsKey(PartType.EnergyRing)
                && explicitBase.ContainsKey(PartType.FaceBolt);
        }

        public bool HasRuntimePartCatalog()
        {
            return runtimePartCatalog != null && runtimePartCatalog.Count > 0;
        }

#if UNITY_EDITOR
        public static bool TryEnsureResourcesConfig(out StarterPartsConfig config)
        {
            const string resourcesAssetPath = "Assets/Resources/StarterPartsConfig.asset";

            config = AssetDatabase.LoadAssetAtPath<StarterPartsConfig>(resourcesAssetPath);
            bool created = false;

            if (config == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");

                config = CreateInstance<StarterPartsConfig>();
                AssetDatabase.CreateAsset(config, resourcesAssetPath);
                created = true;
            }

            bool changed = AutoPopulateBaseParts(config);
            if (created || changed)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return config != null;
        }

        private static bool AutoPopulateBaseParts(StarterPartsConfig config)
        {
            if (config == null)
                return false;

            SerializedObject so = new SerializedObject(config);
            SerializedProperty setNameProp = so.FindProperty("starterBaseSetName");

            string setName = setNameProp != null ? setNameProp.stringValue : string.Empty;
            if (string.IsNullOrWhiteSpace(setName))
                setName = "Arctic Fox";

            string targetToken = NormalizeSetToken(setName);
            if (string.IsNullOrEmpty(targetToken))
                return false;

            Dictionary<PartType, BeyPart> matches = new Dictionary<PartType, BeyPart>();
            string[] guids = AssetDatabase.FindAssets("t:BeyPart");
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                BeyPart part = AssetDatabase.LoadAssetAtPath<BeyPart>(assetPath);
                if (part == null)
                    continue;

                string token = NormalizeSetToken(part.PartName);
                if (string.IsNullOrEmpty(token) || !token.StartsWith(targetToken, StringComparison.Ordinal))
                    continue;

                if (!matches.ContainsKey(part.PartType))
                    matches[part.PartType] = part;
            }

            bool changed = false;
            changed |= SetObjectReference(so, "starterBaseTip", GetMatch(matches, PartType.Tip));
            changed |= SetObjectReference(so, "starterBaseTrack", GetMatch(matches, PartType.Track));
            changed |= SetObjectReference(so, "starterBaseFusionWheel", GetMatch(matches, PartType.FusionWheel));
            changed |= SetObjectReference(so, "starterBaseEnergyRing", GetMatch(matches, PartType.EnergyRing));
            changed |= SetObjectReference(so, "starterBaseFaceBolt", GetMatch(matches, PartType.FaceBolt));

            changed |= PopulatePartList(so, "starterBaseSetParts", matches);
            changed |= PopulatePartList(so, "starterOwnedParts", matches);
            changed |= PopulateLoadout(so, matches);
            changed |= PopulateRuntimeCatalog(so);

            if (changed)
                so.ApplyModifiedPropertiesWithoutUndo();

            return changed;
        }

        private static BeyPart GetMatch(Dictionary<PartType, BeyPart> matches, PartType type)
        {
            BeyPart part;
            return matches.TryGetValue(type, out part) ? part : null;
        }

        private static bool SetObjectReference(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
                return false;

            if (prop.objectReferenceValue == value)
                return false;

            prop.objectReferenceValue = value;
            return true;
        }

        private static bool PopulatePartList(SerializedObject so, string listPropertyName, Dictionary<PartType, BeyPart> matches)
        {
            SerializedProperty listProp = so.FindProperty(listPropertyName);
            if (listProp == null || !listProp.isArray)
                return false;

            bool changed = false;
            foreach (PartType type in CoreSlots())
            {
                BeyPart part = GetMatch(matches, type);
                if (part == null)
                    continue;

                if (ListContainsPart(listProp, part))
                    continue;

                int idx = listProp.arraySize;
                listProp.InsertArrayElementAtIndex(idx);
                SerializedProperty elem = listProp.GetArrayElementAtIndex(idx);
                elem.objectReferenceValue = part;
                changed = true;
            }

            return changed;
        }

        private static bool PopulateLoadout(SerializedObject so, Dictionary<PartType, BeyPart> matches)
        {
            SerializedProperty loadout = so.FindProperty("starterLoadout");
            if (loadout == null || !loadout.isArray)
                return false;

            bool changed = false;
            foreach (PartType type in CoreSlots())
            {
                BeyPart part = GetMatch(matches, type);
                if (part == null)
                    continue;

                if (LoadoutContainsSlot(loadout, type))
                    continue;

                int idx = loadout.arraySize;
                loadout.InsertArrayElementAtIndex(idx);
                SerializedProperty slot = loadout.GetArrayElementAtIndex(idx);
                SerializedProperty slotType = slot.FindPropertyRelative("slot");
                SerializedProperty slotPart = slot.FindPropertyRelative("part");
                if (slotType != null)
                    slotType.enumValueIndex = (int)type;
                if (slotPart != null)
                    slotPart.objectReferenceValue = part;
                changed = true;
            }

            return changed;
        }

        private static bool PopulateRuntimeCatalog(SerializedObject so)
        {
            SerializedProperty listProp = so.FindProperty("runtimePartCatalog");
            if (listProp == null || !listProp.isArray)
                return false;

            string[] guids = AssetDatabase.FindAssets("t:BeyPart");
            Array.Sort(guids, (left, right) =>
                string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(left), AssetDatabase.GUIDToAssetPath(right)));

            List<BeyPart> authoredParts = new List<BeyPart>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                BeyPart part = AssetDatabase.LoadAssetAtPath<BeyPart>(path);
                if (part != null && !authoredParts.Contains(part))
                    authoredParts.Add(part);
            }

            bool alreadySynchronized = listProp.arraySize == authoredParts.Count;
            if (alreadySynchronized)
            {
                for (int i = 0; i < authoredParts.Count; i++)
                {
                    if (listProp.GetArrayElementAtIndex(i).objectReferenceValue != authoredParts[i])
                    {
                        alreadySynchronized = false;
                        break;
                    }
                }
            }

            if (alreadySynchronized)
                return false;

            listProp.ClearArray();
            for (int i = 0; i < authoredParts.Count; i++)
            {
                int index = listProp.arraySize;
                listProp.InsertArrayElementAtIndex(index);
                listProp.GetArrayElementAtIndex(index).objectReferenceValue = authoredParts[i];
            }

            return true;
        }
        private static bool ListContainsPart(SerializedProperty listProp, BeyPart part)
        {
            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty elem = listProp.GetArrayElementAtIndex(i);
                if (elem.objectReferenceValue == part)
                    return true;
            }

            return false;
        }

        private static bool LoadoutContainsSlot(SerializedProperty loadout, PartType type)
        {
            for (int i = 0; i < loadout.arraySize; i++)
            {
                SerializedProperty slot = loadout.GetArrayElementAtIndex(i);
                SerializedProperty slotType = slot.FindPropertyRelative("slot");
                if (slotType != null && slotType.enumValueIndex == (int)type)
                    return true;
            }

            return false;
        }

        private static IEnumerable<PartType> CoreSlots()
        {
            yield return PartType.Tip;
            yield return PartType.Track;
            yield return PartType.FusionWheel;
            yield return PartType.EnergyRing;
            yield return PartType.FaceBolt;
        }
#endif
    }
}
