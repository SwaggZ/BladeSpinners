using System;
using System.Collections.Generic;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;
using UnityEngine;

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
        [SerializeField] private List<BeyPart> enemyPartPool = new List<BeyPart>();
        [SerializeField] private bool useStarterOwnedPartsForEnemyPool = true;

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
    }
}
