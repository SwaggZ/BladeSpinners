using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BladeSpinners.Gameplay.Shrine
{
    /// <summary>
    /// Manages persistent meta-progression unlocks for the 335 Blader Shrine blessings.
    /// Starter blessings are available immediately; winning full runs (9 arenas) unlocks new blessings.
    /// </summary>
    public static class ShrineBlessingsUnlockManager
    {
        private const string PREFS_KEY = "BLADE_SPINNERS_UNLOCKED_BLESSINGS_V1";
        private static HashSet<ShrinePerkType> unlockedPerks;
        private static bool isInitialized = false;

        public static event Action<List<ShrinePerkType>> OnBlessingsUnlocked;

        // ── 30 STARTER BLESSINGS UNLOCKED BY DEFAULT ──────────────────────────
        private static readonly ShrinePerkType[] StarterBlessings = new ShrinePerkType[]
        {
            // 16 Common
            ShrinePerkType.TitaniumTip,
            ShrinePerkType.HeavyweightCore,
            ShrinePerkType.BladeSharpening,
            ShrinePerkType.RubberDampener,
            ShrinePerkType.ManaSiphon,
            ShrinePerkType.ApexTraction,
            ShrinePerkType.HyperDrift,
            ShrinePerkType.QuickstepLaunch,
            ShrinePerkType.StaminaConservation,
            ShrinePerkType.SpikeTread,
            ShrinePerkType.KineticBattery,
            ShrinePerkType.BalancedGyro,
            ShrinePerkType.StreamlinedChassis,
            ShrinePerkType.SparkIgnition,
            ShrinePerkType.LightAlloyWeight,
            ShrinePerkType.SteelBumper,

            // 8 Uncommon
            ShrinePerkType.TurboRip,
            ShrinePerkType.MagnetoRing,
            ShrinePerkType.ImpactAbsorber,
            ShrinePerkType.CentrifugalClutch,
            ShrinePerkType.FlankStriker,
            ShrinePerkType.IronPlating,
            ShrinePerkType.SlipstreamSurge,
            ShrinePerkType.OverchargeCapacitor,

            // 4 Rare
            ShrinePerkType.VampiricSpin,
            ShrinePerkType.StaticOverload,
            ShrinePerkType.IronFortress,
            ShrinePerkType.SpiritSurge,

            // 2 Epic
            ShrinePerkType.OverdriveCore,
            ShrinePerkType.AegisBarrier
        };

        static ShrineBlessingsUnlockManager()
        {
            EnsureInitialized();
        }

        public static void EnsureInitialized()
        {
            if (isInitialized && unlockedPerks != null)
                return;

            unlockedPerks = new HashSet<ShrinePerkType>();

            if (PlayerPrefs.HasKey(PREFS_KEY))
            {
                string savedData = PlayerPrefs.GetString(PREFS_KEY, string.Empty);
                if (!string.IsNullOrEmpty(savedData))
                {
                    string[] tokens = savedData.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string token in tokens)
                    {
                        if (Enum.TryParse(token.Trim(), out ShrinePerkType perkType))
                        {
                            unlockedPerks.Add(perkType);
                        }
                    }
                }
            }

            // Always ensure all starter blessings are unlocked
            foreach (var starter in StarterBlessings)
            {
                unlockedPerks.Add(starter);
            }

            isInitialized = true;
        }

        public static bool IsUnlocked(ShrinePerkType perk)
        {
            EnsureInitialized();
            return unlockedPerks.Contains(perk);
        }

        public static bool UnlockPerk(ShrinePerkType perk)
        {
            EnsureInitialized();
            if (unlockedPerks.Add(perk))
            {
                Save();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Called when the player wins the entire run (clearing all 9 arenas).
        /// Rolls and unlocks 3 to 6 new locked blessings.
        /// </summary>
        public static List<ShrinePerkType> OnRunWon(int minUnlocks = 3, int maxUnlocks = 6)
        {
            EnsureInitialized();

            List<ShrinePerkData> lockedPerks = ShrinePerkCatalog.GetAllPerks()
                .Where(p => !unlockedPerks.Contains(p.Type))
                .ToList();

            List<ShrinePerkType> newlyUnlocked = new List<ShrinePerkType>();
            if (lockedPerks.Count == 0)
                return newlyUnlocked;

            int countToUnlock = UnityEngine.Random.Range(minUnlocks, maxUnlocks + 1);
            countToUnlock = Mathf.Min(countToUnlock, lockedPerks.Count);

            // Group locked perks by rarity
            var lockedCommons = lockedPerks.Where(p => p.Rarity == PerkRarity.Common).ToList();
            var lockedUncommons = lockedPerks.Where(p => p.Rarity == PerkRarity.Uncommon).ToList();
            var lockedRares = lockedPerks.Where(p => p.Rarity == PerkRarity.Rare).ToList();
            var lockedEpics = lockedPerks.Where(p => p.Rarity == PerkRarity.Epic).ToList();
            var lockedLegendaries = lockedPerks.Where(p => p.Rarity == PerkRarity.Legendary).ToList();

            for (int i = 0; i < countToUnlock; i++)
            {
                if (lockedPerks.Count == 0)
                    break;

                float roll = UnityEngine.Random.value;
                ShrinePerkData chosen = null;

                // Weighted tier selection (Legendary 3%, Epic 8%, Rare 18%, Uncommon 31%, Common 40%)
                if (roll < 0.03f && lockedLegendaries.Count > 0)
                {
                    chosen = lockedLegendaries[UnityEngine.Random.Range(0, lockedLegendaries.Count)];
                }
                else if (roll < 0.11f && lockedEpics.Count > 0)
                {
                    chosen = lockedEpics[UnityEngine.Random.Range(0, lockedEpics.Count)];
                }
                else if (roll < 0.29f && lockedRares.Count > 0)
                {
                    chosen = lockedRares[UnityEngine.Random.Range(0, lockedRares.Count)];
                }
                else if (roll < 0.60f && lockedUncommons.Count > 0)
                {
                    chosen = lockedUncommons[UnityEngine.Random.Range(0, lockedUncommons.Count)];
                }
                else if (lockedCommons.Count > 0)
                {
                    chosen = lockedCommons[UnityEngine.Random.Range(0, lockedCommons.Count)];
                }
                else
                {
                    chosen = lockedPerks[UnityEngine.Random.Range(0, lockedPerks.Count)];
                }

                if (chosen != null)
                {
                    unlockedPerks.Add(chosen.Type);
                    newlyUnlocked.Add(chosen.Type);
                    lockedPerks.Remove(chosen);
                    lockedCommons.Remove(chosen);
                    lockedUncommons.Remove(chosen);
                    lockedRares.Remove(chosen);
                    lockedEpics.Remove(chosen);
                    lockedLegendaries.Remove(chosen);
                }
            }

            if (newlyUnlocked.Count > 0)
            {
                Save();
                OnBlessingsUnlocked?.Invoke(newlyUnlocked);
                Debug.Log($"[ShrineUnlocks] Run Victory! Unlocked {newlyUnlocked.Count} new blessings: {string.Join(", ", newlyUnlocked)}");
            }

            return newlyUnlocked;
        }

        public static int GetUnlockedCount()
        {
            EnsureInitialized();
            return unlockedPerks.Count;
        }

        public static int GetTotalCount()
        {
            return ShrinePerkCatalog.GetAllPerks().Count();
        }

        public static int GetUnlockedCountForRarity(PerkRarity rarity)
        {
            EnsureInitialized();
            return ShrinePerkCatalog.GetAllPerks()
                .Count(p => p.Rarity == rarity && unlockedPerks.Contains(p.Type));
        }

        public static int GetTotalCountForRarity(PerkRarity rarity)
        {
            return ShrinePerkCatalog.GetAllPerks()
                .Count(p => p.Rarity == rarity);
        }

        public static void Save()
        {
            EnsureInitialized();
            string data = string.Join(",", unlockedPerks.Select(p => p.ToString()));
            PlayerPrefs.SetString(PREFS_KEY, data);
            PlayerPrefs.Save();
        }

        public static void ResetToStarterPool()
        {
            unlockedPerks = new HashSet<ShrinePerkType>(StarterBlessings);
            Save();
        }

        public static void UnlockAllForDebug()
        {
            EnsureInitialized();
            foreach (var perk in ShrinePerkCatalog.GetAllPerks())
            {
                unlockedPerks.Add(perk.Type);
            }
            Save();
        }
    }
}
