using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BladeSpinners.Gameplay.Shrine
{
    [Serializable]
    public class BladerShrineRunState
    {
        private int bladerPoints = 150; // Starting bonus points
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

        public bool TryReroll(int cost = 50)
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
                .Where(p => !activePerks.Contains(p.Type) && (p.Type != ShrinePerkType.PhoenixRebirth || !phoenixRebirthUsed))
                .ToList();

            if (pool.Count == 0)
                return;

            System.Random rng = new System.Random(seed);
            int countToOffer = Mathf.Min(3, pool.Count);

            // Fisher-Yates shuffle
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int k = rng.Next(i + 1);
                var temp = pool[i];
                pool[i] = pool[k];
                pool[k] = temp;
            }

            for (int i = 0; i < countToOffer; i++)
            {
                currentOfferings.Add(pool[i]);
            }
        }
    }
}
