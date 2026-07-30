using System;
using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Parts
{
    /// <summary>
    /// Deterministic geometry and combat identity for a Fusion Wheel. The same
    /// seed-derived values drive both its generated mesh and its combat stats.
    /// </summary>
    public readonly struct FusionWheelCombatProfile
    {
        public int SymmetryPlanes { get; }
        public int BladeCount { get; }
        public float BladeProtrusion { get; }
        public float BladeWidth { get; }
        public float BladeSweep { get; }
        public float Attack { get; }
        public float Defense { get; }
        public float SpinRetention { get; }

        private FusionWheelCombatProfile(
            int symmetryPlanes,
            int bladeCount,
            float bladeProtrusion,
            float bladeWidth,
            float bladeSweep,
            float attack,
            float defense,
            float spinRetention)
        {
            SymmetryPlanes = symmetryPlanes;
            BladeCount = bladeCount;
            BladeProtrusion = bladeProtrusion;
            BladeWidth = bladeWidth;
            BladeSweep = bladeSweep;
            Attack = attack;
            Defense = defense;
            SpinRetention = spinRetention;
        }

        public string ContactStyle
        {
            get
            {
                if (Attack >= Defense + 10f)
                    return "AGGRESSIVE";
                if (Defense >= Attack + 10f)
                    return "GUARD";
                return "BALANCED";
            }
        }

        public string ShapeDescription
        {
            get
            {
                float protrusion = Mathf.InverseLerp(
                    0.015f, 0.05f, BladeProtrusion);
                string edge = protrusion >= 0.67f
                    ? "JAGGED"
                    : protrusion <= 0.33f ? "ROUND" : "RIDGED";
                return $"{edge} {BladeCount}-BLADE";
            }
        }

        public static FusionWheelCombatProfile FromPart(BeyPart part)
        {
            if (part == null || part.PartType != PartType.FusionWheel)
                return CreateDefault();

            System.Random rng = new System.Random(part.MeshSeed);
            int symmetryPlanes = 1 + rng.Next(0, 2);
            int bladeCount = 3 + rng.Next(0, 6);
            float bladeProtrusion =
                0.015f + (float)rng.NextDouble() * 0.035f;
            float bladeWidth =
                0.15f + (float)rng.NextDouble() * 0.25f;
            float bladeSweep =
                -0.05f + (float)rng.NextDouble() * 0.1f;

            float weight = Mathf.InverseLerp(
                GameConstants.MIN_WEIGHT,
                GameConstants.MAX_WEIGHT,
                part.Weight);
            float protrusion = Mathf.InverseLerp(
                0.015f, 0.05f, bladeProtrusion);
            float width = Mathf.InverseLerp(
                0.15f, 0.4f, bladeWidth);
            float blades = Mathf.InverseLerp(
                3f, 8f, bladeCount);
            float symmetry = symmetryPlanes == 2 ? 1f : 0f;
            float drainQuality = 1f - Mathf.InverseLerp(
                0.1f, 2f, part.MassBasedStaminaDrainRate);

            // Projecting edges, narrow contact zones, and more contact points
            // make an offensive wheel. Weight matters, but does not define it.
            float attackShape =
                protrusion * 0.50f
                + (1f - width) * 0.25f
                + blades * 0.15f
                + (1f - symmetry) * 0.10f;
            float attack = Mathf.Clamp(
                20f + attackShape * 60f + weight * 20f,
                0f,
                100f);

            // Round, broad, symmetrical wheels distribute impacts. Weight gives
            // them stability without making every heavy wheel identical.
            float defenseShape =
                (1f - protrusion) * 0.45f
                + width * 0.25f
                + symmetry * 0.20f
                + (1f - blades) * 0.10f;
            float defense = Mathf.Clamp(
                20f + defenseShape * 45f + weight * 35f,
                0f,
                100f);

            float retentionShape =
                (1f - protrusion) * 0.35f
                + drainQuality * 0.35f
                + weight * 0.20f
                + symmetry * 0.10f;
            float spinRetention = Mathf.Clamp(
                20f + retentionShape * 80f,
                0f,
                100f);

            return new FusionWheelCombatProfile(
                symmetryPlanes,
                bladeCount,
                bladeProtrusion,
                bladeWidth,
                bladeSweep,
                attack,
                defense,
                spinRetention);
        }

        private static FusionWheelCombatProfile CreateDefault()
        {
            return new FusionWheelCombatProfile(
                2,
                5,
                0.025f,
                0.275f,
                0f,
                50f,
                50f,
                50f);
        }
    }

    /// <summary>
    /// Combines the wheel's rotational stability with the Tip's behavior and
    /// authored drain characteristics into a build-level retention stat.
    /// </summary>
    public static class BeyCombatStatCalculator
    {
        public static float GetTipSpinRetention(BeyPart tip)
        {
            if (tip == null || tip.PartType != PartType.Tip)
                return 50f;

            float drainQuality = 1f - Mathf.InverseLerp(
                0.5f,
                2.5f,
                tip.BehaviorBasedStaminaDrainModifier);
            float behaviorStability = GetBehaviorStability(tip.TipBehavior);
            return Mathf.Clamp(
                15f + drainQuality * 65f + behaviorStability * 20f,
                0f,
                100f);
        }

        public static float CombineSpinRetention(
            float wheelRetention,
            float tipRetention)
        {
            return Mathf.Clamp(
                wheelRetention * 0.65f + tipRetention * 0.35f,
                0f,
                100f);
        }

        public static float GetRetentionDrainMultiplier(float spinRetention)
        {
            return Mathf.Lerp(
                1.25f,
                0.75f,
                Mathf.Clamp01(spinRetention / 100f));
        }

        private static float GetBehaviorStability(TipBehaviorType behavior)
        {
            switch (behavior)
            {
                case TipBehaviorType.Sharp:
                case TipBehaviorType.Spike:
                    return 1f;
                case TipBehaviorType.Ball:
                    return 0.82f;
                case TipBehaviorType.Round:
                    return 0.72f;
                case TipBehaviorType.Orbit:
                    return 0.62f;
                case TipBehaviorType.Flat:
                    return 0.35f;
                case TipBehaviorType.RubberFlat:
                    return 0.18f;
                default:
                    return 0.55f;
            }
        }
    }
}
