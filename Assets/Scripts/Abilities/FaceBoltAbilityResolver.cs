using System;
using System.Collections.Generic;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    public static class FaceBoltAbilityResolver
    {
        private static readonly Dictionary<string, BeyAbility> AssignedByFaceBoltId = new Dictionary<string, BeyAbility>();
        private static readonly List<BeyAbility> AbilityPool = AbilityFactory.CreateRuntimeAbilityPool();
        private static readonly Dictionary<Type, BeyAbility> InstanceMap = AbilityFactory.CreateAbilityInstanceMap();

        /// <summary>
        /// Maps face bolt names (case-insensitive) to specific ability types.
        /// Thematically matched so each bey's identity reflects its ability.
        /// </summary>
        private static readonly Dictionary<string, Type> NameToAbilityType = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "Arctic Fox", typeof(FreezeAbility) },
            { "Ashen Wolf", typeof(FireBoltAbility) },
            { "Barrier", typeof(ShieldAbility) },
            { "Berserker", typeof(BerserkAbility) },
            { "Blizzard", typeof(FreezeAbility) },
            { "Blood Moon", typeof(VampireDrainAbility) },
            { "Buzz Blade", typeof(RicochetShotAbility) },
            { "Chain Lightning", typeof(ChainLightningAbility) },
            { "Crimson Slash", typeof(FireBoltAbility) },
            { "Death Dealer", typeof(SpinDrainAbility) },
            { "Deflector", typeof(ThornsAbility) },
            { "Dragonheart", typeof(DragonBurstAbility) },
            { "Eclipse", typeof(MirageCloneAbility) },
            { "Electra", typeof(ChainLightningAbility) },
            { "Emerald Storm", typeof(TidalWaveAbility) },
            { "Frost Bite", typeof(FreezeAbility) },
            { "Gilded Wrath", typeof(BerserkAbility) },
            { "Glacier", typeof(FreezeAbility) },
            { "Hexblade", typeof(SpinDrainAbility) },
            { "Inferno", typeof(FireBoltAbility) },
            { "Iron Claw", typeof(GroundPoundAbility) },
            { "Lucky Star", typeof(LuckyStarAbility) },
            { "Magma Core", typeof(SolarFlareAbility) },
            { "Mirage", typeof(MirageCloneAbility) },
            { "Multishot", typeof(RicochetShotAbility) },
            { "Night Stalker", typeof(FlashStepAbility) },
            { "Obsidian", typeof(GravityClashAbility) },
            { "Phantom Arrow", typeof(RicochetShotAbility) },
            { "Phantom", typeof(MirageCloneAbility) },
            { "Plague Doctor", typeof(PoisonCloudAbility) },
            { "Rampager", typeof(BerserkAbility) },
            { "Ricochet", typeof(RicochetShotAbility) },
            { "Sapphire Edge", typeof(FreezeAbility) },
            { "Scorpion", typeof(PoisonCloudAbility) },
            { "Serpent King", typeof(SerpentCoilAbility) },
            { "Shadow Wyrm", typeof(DragonBurstAbility) },
            { "Solar Flare", typeof(SolarFlareAbility) },
            { "Soul Reaver", typeof(VampireDrainAbility) },
            { "Supernova", typeof(SolarFlareAbility) },
            { "Swiftfoot", typeof(DashAbility) },
            { "Tempest", typeof(ChainLightningAbility) },
            { "Thornback", typeof(ThornsAbility) },
            { "Thunder Bolt", typeof(ChainLightningAbility) },
            { "Tidal Wave", typeof(TidalWaveAbility) },
            { "Venom Fang", typeof(PoisonCloudAbility) },
            { "Void Reaper", typeof(SpinDrainAbility) },
            { "Warden", typeof(ShieldAbility) },
            { "Wildfire", typeof(FireBoltAbility) },
        };

        public static BeyAbility Resolve(BeyPart faceBolt)
        {
            if (faceBolt == null || faceBolt.PartType != PartType.FaceBolt)
                return null;

            if (faceBolt.EquippedAbility != null)
                return faceBolt.EquippedAbility;

            // 1. Try exact name match
            string partName = NormalizeFaceBoltName(faceBolt.PartName, faceBolt.PartID);
            if (!string.IsNullOrEmpty(partName) && NameToAbilityType.TryGetValue(partName, out Type abilityType))
            {
                if (InstanceMap.TryGetValue(abilityType, out BeyAbility namedAbility))
                    return namedAbility;
            }

            if (AbilityPool.Count == 0)
                return null;

            // 2. Fall back to deterministic hash assignment (unique per bolt where possible)
            string key = BuildFaceBoltKey(faceBolt);
            if (AssignedByFaceBoltId.TryGetValue(key, out BeyAbility existing) && existing != null)
                return existing;

            int startIndex = Math.Abs(key.GetHashCode()) % AbilityPool.Count;
            HashSet<BeyAbility> used = new HashSet<BeyAbility>(AssignedByFaceBoltId.Values);
            for (int offset = 0; offset < AbilityPool.Count; offset++)
            {
                int index = (startIndex + offset) % AbilityPool.Count;
                BeyAbility candidate = AbilityPool[index];
                if (!used.Contains(candidate))
                {
                    AssignedByFaceBoltId[key] = candidate;
                    return candidate;
                }
            }

            BeyAbility fallback = AbilityPool[startIndex];
            AssignedByFaceBoltId[key] = fallback;
            return fallback;
        }

        private static string BuildFaceBoltKey(BeyPart faceBolt)
        {
            string id = string.IsNullOrWhiteSpace(faceBolt.PartID) ? "NO_ID" : faceBolt.PartID.Trim();
            string name = string.IsNullOrWhiteSpace(faceBolt.PartName) ? "NO_NAME" : faceBolt.PartName.Trim();
            return id + "|" + name;
        }

        private static string NormalizeFaceBoltName(string partName, string partId)
        {
            string raw = string.IsNullOrWhiteSpace(partName) ? string.Empty : partName.Trim();
            if (string.IsNullOrWhiteSpace(raw) && !string.IsNullOrWhiteSpace(partId))
                raw = partId.Trim();

            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            raw = raw.Replace("_", " ");

            if (raw.EndsWith(" FaceBolt", StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(0, raw.Length - " FaceBolt".Length);

            if (raw.EndsWith(" Face Bolt", StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(0, raw.Length - " Face Bolt".Length);

            int suffixIndex = raw.IndexOf(" facebolt ", StringComparison.OrdinalIgnoreCase);
            if (suffixIndex >= 0)
                raw = raw.Substring(0, suffixIndex);

            int idSuffixIndex = raw.IndexOf(" face bolt ", StringComparison.OrdinalIgnoreCase);
            if (idSuffixIndex >= 0)
                raw = raw.Substring(0, idSuffixIndex);

            return raw.Trim();
        }
    }
}
