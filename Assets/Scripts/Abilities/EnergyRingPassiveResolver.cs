using System;
using System.Collections.Generic;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;
using UnityEngine;

namespace BladeSpinners.Abilities
{
    /// <summary>
    /// Build-safe passive assignment for the authored Energy Ring catalog. Explicit
    /// references win; otherwise a stable part ID hash selects one of ten definitions.
    /// </summary>
    public static class EnergyRingPassiveResolver
    {
        private static readonly BeyPassive[] PassivePool =
        {
            Create<SpinRecoveryPassive>(
                "Spin Recovery",
                "After 3 seconds without a Bey collision, recover 2.5 spin per second.",
                AbilityRarity.Common),
            Create<LowSpinSurgePassive>(
                "Low Spin Surge",
                "Deal 25% more collision damage while below 30% starting spin.",
                AbilityRarity.Uncommon),
            Create<ImpactGuardPassive>(
                "Impact Guard",
                "Reduce incoming collision spin damage by 20%.",
                AbilityRarity.Common),
            Create<KineticBatteryPassive>(
                "Kinetic Battery",
                "Bey collisions restore up to 10 mana, with a 1.25 second cooldown.",
                AbilityRarity.Uncommon),
            Create<RecoilRecoveryPassive>(
                "Recoil Recovery",
                "Recover 25% of collision spin damage taken, up to 8 spin per hit.",
                AbilityRarity.Rare),
            Create<ArcConversionPassive>(
                "Arc Conversion",
                "Every 20 mana spent restores 4 spin.",
                AbilityRarity.Rare),
            Create<ManaConduitPassive>(
                "Mana Conduit",
                "Increase passive mana regeneration by 35%.",
                AbilityRarity.Common),
            Create<EnduranceMatrixPassive>(
                "Endurance Matrix",
                "Reduce passive stamina drain by 20%.",
                AbilityRarity.Uncommon),
            Create<SecondWindPassive>(
                "Second Wind",
                "Once per match, survive a bursting collision with 12 spin.",
                AbilityRarity.Legendary),
            Create<PickupAmplifierPassive>(
                "Collector's Prism",
                "Resource pickups restore 50% more spin or mana.",
                AbilityRarity.Rare)
        };

        public static IReadOnlyList<BeyPassive> AllPassives =>
            PassivePool;

        public static BeyPassive Resolve(BeyPart energyRing)
        {
            if (energyRing == null
                || energyRing.PartType != PartType.EnergyRing)
            {
                return null;
            }

            if (energyRing.EquippedPassive != null)
                return energyRing.EquippedPassive;

            string key = !string.IsNullOrWhiteSpace(
                energyRing.PartID)
                ? energyRing.PartID
                : energyRing.PartName;
            uint hash = StableHash(key);
            return PassivePool[
                (int)(hash % (uint)PassivePool.Length)];
        }

        public static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                string text = value ?? string.Empty;
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= char.ToUpperInvariant(text[i]);
                    hash *= 16777619u;
                }
                return hash;
            }
        }

        private static T Create<T>(
            string displayName,
            string description,
            AbilityRarity rarity)
            where T : BeyPassive
        {
            T passive = ScriptableObject.CreateInstance<T>();
            passive.ConfigureRuntimeMetadata(
                displayName, description, rarity);
            return passive;
        }
    }
}
