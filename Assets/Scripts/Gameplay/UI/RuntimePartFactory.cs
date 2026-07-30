using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Gameplay.UI
{
    public static class RuntimePartFactory
    {
        private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static List<BeyPart> CreateStarterCatalog(int partsPerType = 8, int seed = 123456)
        {
            List<BeyPart> parts = new List<BeyPart>();
            System.Random rng = new System.Random(seed);

            foreach (PartType type in Enum.GetValues(typeof(PartType)))
            {
                for (int i = 0; i < partsPerType; i++)
                {
                    int meshSeed = rng.Next(1000, int.MaxValue);
                    parts.Add(CreateTemporaryPart(type, meshSeed));
                }
            }

            return parts;
        }

        public static BeyPart CreateTemporaryPart(PartType type, int meshSeed)
        {
            BeyPart part = ScriptableObject.CreateInstance<BeyPart>();
            System.Random rng = new System.Random(meshSeed ^ (int)type * 93);

            string setName = $"Run_{type}_{Math.Abs(meshSeed % 10000):0000}";
            string partName = $"{setName}_{type}";
            RarityTier rarity = RollRarity(rng);
            Color primary = RandomColor(rng);
            Color secondary = Color.Lerp(primary, Color.white, 0.35f);

            SetField(part, "partName", partName);
            SetField(part, "partType", type);
            SetField(part, "partID", Guid.NewGuid().ToString("N"));
            SetField(part, "occupiesSlots", new List<PartType> { type });
            SetField(part, "rarity", rarity);
            SetField(part, "description", $"Temporary {rarity} {type} for run-only use.");
            SetField(part, "meshSeed", meshSeed);
            SetField(part, "primaryColor", primary);
            SetField(part, "secondaryColor", secondary);

            switch (type)
            {
                case PartType.Tip:
                    // (23/3/2026): Only assign basic tip types to random parts, never Orbit
                    // Orbit is a high-skill-ceiling behavior meant for curated parts only
                    TipBehaviorType[] randomTipTypes = new TipBehaviorType[]
                    {
                        TipBehaviorType.Flat, TipBehaviorType.Sharp, TipBehaviorType.Round,
                        TipBehaviorType.RubberFlat, TipBehaviorType.Ball, TipBehaviorType.Spike
                    };
                    SetField(part, "tipBehavior", randomTipTypes[rng.Next(randomTipTypes.Length)]);
                    SetField(part, "behaviorBasedStaminaDrainModifier", 0.6f + (float)rng.NextDouble() * 1.4f);
                    SetField(part, "uphillResistanceMultiplier", 0.5f + (float)rng.NextDouble() * 1.2f);
                    SetField(part, "slopeMultiplier", 0.7f + (float)rng.NextDouble() * 1.1f);
                    SetField(part, "spinThreshold", -1f);
                    break;

                case PartType.Track:
                    SetField(part, "trackHeight", 0.8f + (float)rng.NextDouble() * 1.2f);
                    SetField(part, "jumpArcModifier", 0.75f + (float)rng.NextDouble() * 0.6f);
                    break;

                case PartType.FusionWheel:
                    SetField(part, "weight", 15f + (float)rng.NextDouble() * 30f);
                    SetField(part, "massBasedStaminaDrainRate", 0.2f + (float)rng.NextDouble() * 1.3f);
                    break;

                case PartType.EnergyRing:
                    SetField(part, "manaPoolSize", 80f + (float)rng.NextDouble() * 160f);
                    SetField(part, "manaRegenRate", 12f + (float)rng.NextDouble() * 22f);
                    break;

                case PartType.FaceBolt:
                    break;
            }

            return part;
        }

        private static RarityTier RollRarity(System.Random rng)
        {
            int roll = rng.Next(0, 100);
            if (roll < 40) return RarityTier.Common;
            if (roll < 67) return RarityTier.Uncommon;
            if (roll < 85) return RarityTier.Rare;
            if (roll < 95) return RarityTier.Epic;
            return RarityTier.Legendary;
        }

        private static Color RandomColor(System.Random rng)
        {
            float h = (float)rng.NextDouble();
            float s = 0.55f + (float)rng.NextDouble() * 0.4f;
            float v = 0.55f + (float)rng.NextDouble() * 0.4f;
            Color c = Color.HSVToRGB(h, s, v);
            c.a = 1f;
            return c;
        }

        private static void SetField(BeyPart part, string fieldName, object value)
        {
            part.GetType().GetField(fieldName, Flags)?.SetValue(part, value);
        }
    }
}
