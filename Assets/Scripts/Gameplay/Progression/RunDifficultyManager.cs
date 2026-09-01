using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.Shrine;

namespace BladeSpinners.Gameplay.Progression
{
    /// <summary>
    /// Manages persistent run win counts and meta difficulty scaling.
    /// Every 5 run wins permanently unlocks an additive difficulty modifier
    /// (rotating between Part Tiers, Enemy Blessings, and Boss Buffs).
    /// Bosses have innate blessings, size scaling, and stat buffs from 0 wins.
    /// </summary>
    public static class RunDifficultyManager
    {
        private const string PREFS_RUN_WINS_KEY = "BLADE_SPINNERS_TOTAL_RUN_WINS";

        public struct BossScalingData
        {
            public float Scale;
            public float MassMultiplier;
            public float AttackMultiplier;
            public float SpinMultiplier;
            public float SpeedMultiplier;
            public int BlessingCount;
        }

        public static int GetTotalRunWins()
        {
            return PlayerPrefs.GetInt(PREFS_RUN_WINS_KEY, 0);
        }

        public static int RecordRunWin()
        {
            int current = GetTotalRunWins() + 1;
            PlayerPrefs.SetInt(PREFS_RUN_WINS_KEY, current);
            PlayerPrefs.Save();
            Debug.Log($"[RunDifficultyManager] Run won! Total Run Wins: {current} (Difficulty Milestone: {current / 5})");
            return current;
        }

        public static int GetMilestoneLevel()
        {
            return GetTotalRunWins() / 5;
        }

        public static string GetDifficultyName(int wins)
        {
            int milestone = wins / 5;
            switch (milestone)
            {
                case 0: return "RECRUIT // 新兵";
                case 1: return "ADEPT // 熟練";
                case 2: return "VETERAN // 歴戦";
                case 3: return "ELITE // 精鋭";
                case 4: return "CHAMPION // 闘士";
                case 5: return "MASTER // 達人";
                case 6: return "GRANDMASTER // 宗師";
                case 7: return "WARLORD // 覇王";
                case 8: return "OVERLORD // 冥王";
                case 9: return "APEX // 頂点";
                case 10: return "MYTHIC // 神話";
                case 11: return "NIGHTMARE // 悪夢";
                default: return "TORMENT // 煉獄";
            }
        }

        /// <summary>
        /// Rolls part rarity for enemies based on completed run wins and arena depth.
        /// Regular enemies start with 100% Common at 0 wins and scale to Legendary.
        /// Bosses roll 1-2 tiers higher from the start.
        /// </summary>
        public static RarityTier RollEnemyPartRarity(int wins, float depth01, System.Random rng, bool isBoss)
        {
            int milestone = wins / 5;

            // Base weights [Common, Uncommon, Rare, Epic, Legendary]
            float wCommon = 100f;
            float wUncommon = 0f;
            float wRare = 0f;
            float wEpic = 0f;
            float wLegendary = 0f;

            if (milestone >= 11 || wins >= 55)
            {
                wCommon = 0f; wUncommon = 0f; wRare = 0f; wEpic = 30f; wLegendary = 70f;
            }
            else if (milestone >= 8 || wins >= 40)
            {
                wCommon = 0f; wUncommon = 10f; wRare = 40f; wEpic = 40f; wLegendary = 10f;
            }
            else if (milestone >= 5 || wins >= 25)
            {
                wCommon = 15f; wUncommon = 45f; wRare = 35f; wEpic = 5f; wLegendary = 0f;
            }
            else if (milestone >= 2 || wins >= 10)
            {
                wCommon = 70f; wUncommon = 30f; wRare = 0f; wEpic = 0f; wLegendary = 0f;
            }
            else
            {
                // Milestone 0-1 (0-9 wins): 100% Common for regular enemies
                wCommon = 100f; wUncommon = 0f; wRare = 0f; wEpic = 0f; wLegendary = 0f;
            }

            // Bosses get +1 to +2 tier rarity boost from the start
            if (isBoss)
            {
                if (milestone >= 8)
                {
                    wCommon = 0f; wUncommon = 0f; wRare = 0f; wEpic = 20f; wLegendary = 80f;
                }
                else if (milestone >= 4)
                {
                    wCommon = 0f; wUncommon = 0f; wRare = 25f; wEpic = 55f; wLegendary = 20f;
                }
                else
                {
                    // 0-19 wins boss: Uncommon / Rare parts
                    wCommon = 0f; wUncommon = 60f; wRare = 40f; wEpic = 0f; wLegendary = 0f;
                }
            }

            // Depth late-run modifier (+10% bump toward higher tiers)
            if (depth01 > 0.6f && !isBoss && milestone >= 2)
            {
                wCommon = Mathf.Max(0f, wCommon - 15f);
                wUncommon += 10f;
                wRare += 5f;
            }

            float total = wCommon + wUncommon + wRare + wEpic + wLegendary;
            float roll = (float)rng.NextDouble() * total;

            if (roll < wCommon) return RarityTier.Common;
            roll -= wCommon;
            if (roll < wUncommon) return RarityTier.Uncommon;
            roll -= wUncommon;
            if (roll < wRare) return RarityTier.Rare;
            roll -= wRare;
            if (roll < wEpic) return RarityTier.Epic;
            return RarityTier.Legendary;
        }

        /// <summary>
        /// Rolls blessings for enemies. Regular enemies start with 0 blessings at 0 wins.
        /// Bosses have 1-2 blessings from the start, scaling up to 5.
        /// </summary>
        public static List<ShrinePerkType> RollEnemyBlessings(int wins, float depth01, System.Random rng, bool isSemiBoss, bool isFinalBoss)
        {
            int milestone = wins / 5;
            int count = 0;

            if (isFinalBoss)
            {
                // Final Boss blessing count: 2 baseline, scaling up to 5
                if (milestone >= 9) count = 5;
                else if (milestone >= 6) count = 4;
                else if (milestone >= 3) count = 3;
                else count = 2;
            }
            else if (isSemiBoss)
            {
                // Semi Boss blessing count: 1 baseline, scaling up to 4
                if (milestone >= 9) count = 4;
                else if (milestone >= 6) count = 3;
                else if (milestone >= 3) count = 2;
                else count = 1;
            }
            else
            {
                // Regular enemies:
                // 0 wins: 0% chance
                // 5 wins (Milestone 1): 15% chance for 1
                // 20 wins (Milestone 4): 35% chance for 1 (10% for 2)
                // 35 wins (Milestone 7): 65% chance for 1-2
                // 50 wins (Milestone 10): 100% chance for 2
                // 60+ wins (Milestone 12): 100% chance for 3
                if (milestone >= 12 || wins >= 60)
                {
                    count = rng.NextDouble() < 0.5f ? 3 : 2;
                }
                else if (milestone >= 10 || wins >= 50)
                {
                    count = 2;
                }
                else if (milestone >= 7 || wins >= 35)
                {
                    count = rng.NextDouble() < 0.4f ? 2 : 1;
                }
                else if (milestone >= 4 || wins >= 20)
                {
                    double r = rng.NextDouble();
                    if (r < 0.10f) count = 2;
                    else if (r < 0.40f) count = 1;
                    else count = 0;
                }
                else if (milestone >= 1 || wins >= 5)
                {
                    if (rng.NextDouble() < 0.15f) count = 1;
                    else count = 0;
                }
                else
                {
                    count = 0;
                }
            }

            List<ShrinePerkType> blessings = new List<ShrinePerkType>();
            if (count <= 0) return blessings;

            var allPerks = ShrinePerkCatalog.GetAllPerks().ToList();
            if (allPerks == null || allPerks.Count == 0) return blessings;

            // Pick distinct blessings
            HashSet<ShrinePerkType> picked = new HashSet<ShrinePerkType>();
            int attempts = 0;
            while (picked.Count < count && attempts < 40)
            {
                attempts++;
                int idx = rng.Next(allPerks.Count);
                ShrinePerkData p = allPerks[idx];
                if (p != null && !picked.Contains(p.Type))
                {
                    picked.Add(p.Type);
                    blessings.Add(p.Type);
                }
            }

            return blessings;
        }

        /// <summary>
        /// Computes boss stat buffs, size scaling, and mass multipliers.
        /// </summary>
        public static BossScalingData GetBossScaling(int wins, bool isFinalBoss)
        {
            int milestone = wins / 5;
            int bossMilestone = milestone / 3; // Milestone 0, 3, 6, 9 (wins 0, 15, 30, 45)

            if (isFinalBoss)
            {
                float scale = 1.55f + Mathf.Clamp(bossMilestone * 0.08f, 0f, 0.35f);
                float mass = 1.65f + bossMilestone * 0.20f;
                float attack = 1.35f + bossMilestone * 0.18f;
                float spin = 1.40f + bossMilestone * 0.20f;
                float speed = 1.15f + bossMilestone * 0.08f;
                int blessings = Mathf.Clamp(2 + bossMilestone, 2, 5);

                return new BossScalingData
                {
                    Scale = scale,
                    MassMultiplier = mass,
                    AttackMultiplier = attack,
                    SpinMultiplier = spin,
                    SpeedMultiplier = speed,
                    BlessingCount = blessings
                };
            }
            else
            {
                // Semi-Boss
                float scale = 1.30f + Mathf.Clamp(bossMilestone * 0.06f, 0f, 0.25f);
                float mass = 1.35f + bossMilestone * 0.15f;
                float attack = 1.20f + bossMilestone * 0.14f;
                float spin = 1.25f + bossMilestone * 0.15f;
                float speed = 1.10f + bossMilestone * 0.06f;
                int blessings = Mathf.Clamp(1 + bossMilestone, 1, 4);

                return new BossScalingData
                {
                    Scale = scale,
                    MassMultiplier = mass,
                    AttackMultiplier = attack,
                    SpinMultiplier = spin,
                    SpeedMultiplier = speed,
                    BlessingCount = blessings
                };
            }
        }
    }
}
