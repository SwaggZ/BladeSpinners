using System;
using BladeSpinners.Gameplay.Combat;
using BladeSpinners.Gameplay.Parts;
using UnityEditor;
using UnityEngine;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Deterministic regression checks for collision speed, approach, and facing.
    /// </summary>
    public static class SpinExchangeImpactValidator
    {
        private const float Tolerance = 0.001f;

        [MenuItem("Blade Spinners/Validation/Test Spin Exchange Impacts")]
        public static void Validate()
        {
            BeyStatBlock attackerStats = new BeyStatBlock { Weight = 30f };
            BeyStatBlock defenderStats = new BeyStatBlock { Weight = 30f };
            Vector3 contactNormal = Vector3.right;

            CollisionImpactProfile headOn =
                SpinExchangeHandler.EvaluateImpact(
                    new Vector3(20f, 0f, 0f),
                    new Vector3(-15f, 0f, 0f),
                    contactNormal,
                    Vector3.left);
            CollisionImpactProfile slowGraze =
                SpinExchangeHandler.EvaluateImpact(
                    new Vector3(3f, 0f, 12f),
                    Vector3.zero,
                    contactNormal,
                    Vector3.left);
            CollisionImpactProfile parallelGraze =
                SpinExchangeHandler.EvaluateImpact(
                    new Vector3(0f, 0f, 20f),
                    Vector3.zero,
                    contactNormal,
                    Vector3.left);

            float headOnDamage = SpinExchangeHandler.CalculateSpinDamage(
                attackerStats,
                defenderStats,
                headOn.CollisionMagnitude,
                20f,
                headOn.DefenderFacingMultiplier);
            float grazingDamage = SpinExchangeHandler.CalculateSpinDamage(
                attackerStats,
                defenderStats,
                slowGraze.CollisionMagnitude,
                new Vector3(3f, 0f, 12f).magnitude,
                slowGraze.DefenderFacingMultiplier);

            if (headOn.ClosingSpeed <= slowGraze.ClosingSpeed
                || headOn.ApproachAlignment <= slowGraze.ApproachAlignment
                || headOnDamage <= grazingDamage * 3f)
            {
                throw new InvalidOperationException(
                    $"Head-on impact was not decisively stronger than a slow graze. " +
                    $"Head={headOnDamage:F2}, graze={grazingDamage:F2}.");
            }
            if (SpinExchangeHandler.ShouldExchangeSpin(
                    parallelGraze.ClosingSpeed))
            {
                throw new InvalidOperationException(
                    "Fast parallel motion incorrectly counted as a closing impact.");
            }

            CollisionImpactProfile frontHit =
                SpinExchangeHandler.EvaluateImpact(
                    new Vector3(20f, 0f, 0f),
                    Vector3.zero,
                    contactNormal,
                    Vector3.left);
            CollisionImpactProfile sideHit =
                SpinExchangeHandler.EvaluateImpact(
                    new Vector3(20f, 0f, 0f),
                    Vector3.zero,
                    contactNormal,
                    Vector3.forward);
            CollisionImpactProfile rearHit =
                SpinExchangeHandler.EvaluateImpact(
                    new Vector3(20f, 0f, 0f),
                    Vector3.zero,
                    contactNormal,
                    Vector3.right);

            if (!(frontHit.DefenderFacingMultiplier
                    < sideHit.DefenderFacingMultiplier
                && sideHit.DefenderFacingMultiplier
                    < rearHit.DefenderFacingMultiplier))
            {
                throw new InvalidOperationException(
                    "Front, side, and rear hit exposure multipliers are not ordered.");
            }

            BeyConfiguration attacker = new BeyConfiguration();
            BeyConfiguration defender =
                new BeyConfiguration { IsEnemy = true };
            SpinExchangeHandler.HandleCollision(
                attacker,
                defender,
                attackerStats,
                defenderStats,
                new Vector3(20f, 0f, 0f),
                new Vector3(-15f, 0f, 0f),
                contactNormal,
                Vector3.right,
                Vector3.left,
                1f);

            float appliedDamage = 100f - defender.CurrentSpin;
            AssertApproximately(
                appliedDamage, headOnDamage, "applied head-on damage");
            if (appliedDamage <= 0f
                || attacker.CurrentSpin >= 100f)
            {
                throw new InvalidOperationException(
                    "Full collision exchange did not damage both participants.");
            }

            Debug.Log(
                $"[SpinExchangeImpact] Passed: head-on={headOnDamage:F2}, " +
                $"slowGraze={grazingDamage:F2}, closing={headOn.ClosingSpeed:F1}/" +
                $"{slowGraze.ClosingSpeed:F1}, facing front/side/rear=" +
                $"{frontHit.DefenderFacingMultiplier:F3}/" +
                $"{sideHit.DefenderFacingMultiplier:F3}/" +
                $"{rearHit.DefenderFacingMultiplier:F3}.");
        }

        public static void ValidateFromCommandLine()
        {
            Validate();
        }

        private static void AssertApproximately(
            float actual, float expected, string scenario)
        {
            if (Mathf.Abs(actual - expected) > Tolerance)
            {
                throw new InvalidOperationException(
                    $"{scenario}: expected {expected:F3}, got {actual:F3}.");
            }
        }
    }
}
