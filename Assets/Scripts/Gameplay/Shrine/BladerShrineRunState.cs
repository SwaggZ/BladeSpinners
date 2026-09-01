using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BladeSpinners.Gameplay.Shrine
{
    [Serializable]
    public class BladerShrineRunState
    {
        private int bladerPoints = 50; // Starting bonus points
        private readonly HashSet<ShrinePerkType> activePerks = new HashSet<ShrinePerkType>();
        private readonly List<ShrinePerkData> currentOfferings = new List<ShrinePerkData>();
        private bool phoenixRebirthUsed = false;
        private int currentArenaOfferingSeed = -1;

        public int BladerPoints => bladerPoints;
        public IReadOnlyCollection<ShrinePerkType> ActivePerks => activePerks;
        public IReadOnlyList<ShrinePerkData> CurrentOfferings => currentOfferings;
        public bool PhoenixRebirthUsed => phoenixRebirthUsed;

        public event Action<int> OnPointsChanged;
        public event Action<ShrinePerkType> OnPerkAcquired;

        public void AddPoints(int amount)
        {
            if (amount <= 0) return;
            bladerPoints += amount;
            OnPointsChanged?.Invoke(bladerPoints);
        }

        public bool HasPerk(ShrinePerkType perk)
        {
            return activePerks.Contains(perk);
        }

        public void GrantPerk(ShrinePerkType perk)
        {
            activePerks.Add(perk);
        }

        public void ConsumePhoenixRebirth()
        {
            phoenixRebirthUsed = true;
            activePerks.Remove(ShrinePerkType.PhoenixRebirth);
        }

        public void RefreshOfferingsForArena(int arenaIndex, int runSeed)
        {
            int seed = runSeed * 10007 + arenaIndex * 733;
            if (seed == currentArenaOfferingSeed && currentOfferings.Count > 0)
                return;

            currentArenaOfferingSeed = seed;
            GenerateOfferings(seed);
        }

        public bool TryReroll(int cost = 100)
        {
            if (bladerPoints < cost)
                return false;

            bladerPoints -= cost;
            OnPointsChanged?.Invoke(bladerPoints);
            currentArenaOfferingSeed += 77;
            GenerateOfferings(currentArenaOfferingSeed);
            return true;
        }

        public bool TryPurchasePerk(ShrinePerkType type)
        {
            ShrinePerkData perkData = ShrinePerkCatalog.GetPerk(type);
            if (perkData == null || activePerks.Contains(type))
                return false;

            if (bladerPoints < perkData.BaseCost)
                return false;

            bladerPoints -= perkData.BaseCost;
            activePerks.Add(type);
            OnPointsChanged?.Invoke(bladerPoints);
            OnPerkAcquired?.Invoke(type);
            return true;
        }

        private void GenerateOfferings(int seed)
        {
            currentOfferings.Clear();
            List<ShrinePerkData> pool = ShrinePerkCatalog.GetAllPerks()
                .Where(p => ShrineBlessingsUnlockManager.IsUnlocked(p.Type) && !activePerks.Contains(p.Type) && (p.Type != ShrinePerkType.PhoenixRebirth || !phoenixRebirthUsed))
                .ToList();

            if (pool.Count == 0)
                return;

            System.Random rng = new System.Random(seed);
            List<ShrinePerkData> available = new List<ShrinePerkData>(pool);
            int countToOffer = Mathf.Min(3, available.Count);

            // Slot 1: Prefer Common / Uncommon for accessible early upgrades
            var lowTier = available.Where(p => p.Rarity == PerkRarity.Common || p.Rarity == PerkRarity.Uncommon).ToList();
            if (lowTier.Count > 0)
            {
                var pick = lowTier[rng.Next(lowTier.Count)];
                currentOfferings.Add(pick);
                available.Remove(pick);
            }

            // Fill remaining slots from available pool with Fisher-Yates shuffle
            for (int i = available.Count - 1; i > 0; i--)
            {
                int k = rng.Next(i + 1);
                var temp = available[i];
                available[i] = available[k];
                available[k] = temp;
            }

            while (currentOfferings.Count < countToOffer && available.Count > 0)
            {
                currentOfferings.Add(available[0]);
                available.RemoveAt(0);
            }
        }
    }
}
