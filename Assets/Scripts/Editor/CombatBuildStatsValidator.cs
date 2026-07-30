using System;
using System.Collections.Generic;
using System.Reflection;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Combat;
using BladeSpinners.Gameplay.Parts;
using UnityEditor;
using UnityEngine;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Regression coverage for geometry-derived wheel identity, Attack/Defense
    /// damage modifiers, and build-level spin retention.
    /// </summary>
    public static class CombatBuildStatsValidator
    {
        private const BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("Blade Spinners/Validation/Test Combat Build Stats")]
        public static void Validate()
        {
            ValidateAuthoredWheelProfiles(
                out int authoredCount,
                out int distinctAttacks,
                out int distinctDefenses);

            BeyPart lowAttackWheel = null;
            BeyPart highAttackWheel = null;
            BeyPart stableTip = null;
            BeyPart aggressiveTip = null;

            try
            {
                FindEqualWeightAttackExtremes(
                    out lowAttackWheel,
                    out highAttackWheel);

                FusionWheelCombatProfile lowProfile =
                    FusionWheelCombatProfile.FromPart(lowAttackWheel);
                FusionWheelCombatProfile highProfile =
                    FusionWheelCombatProfile.FromPart(highAttackWheel);
                if (highProfile.Attack - lowProfile.Attack < 20f)
                {
                    throw new InvalidOperationException(
                        "Equal-weight generated wheels do not have enough Attack diversity. " +
                        $"Low={lowProfile.Attack:F1}, high={highProfile.Attack:F1}.");
                }

                BeyStatBlock neutralDefender = new BeyStatBlock
                {
                    Weight = 30f,
                    Defense = 50f
                };
                float lowAttackDamage = SpinExchangeHandler.CalculateSpinDamage(
                    new BeyStatBlock
                    {
                        Weight = 30f,
                        Attack = lowProfile.Attack
                    },
                    neutralDefender,
                    1f,
                    20f);
                float highAttackDamage = SpinExchangeHandler.CalculateSpinDamage(
                    new BeyStatBlock
                    {
                        Weight = 30f,
                        Attack = highProfile.Attack
                    },
                    neutralDefender,
                    1f,
                    20f);
                if (highAttackDamage <= lowAttackDamage * 1.15f)
                {
                    throw new InvalidOperationException(
                        "Geometry-derived Attack did not materially change equal-weight " +
                        $"damage. Low={lowAttackDamage:F2}, high={highAttackDamage:F2}.");
                }

                float lowDefenseDamage = SpinExchangeHandler.CalculateSpinDamage(
                    new BeyStatBlock { Weight = 30f, Attack = 50f },
                    new BeyStatBlock { Weight = 30f, Defense = 20f },
                    1f,
                    20f);
                float highDefenseDamage = SpinExchangeHandler.CalculateSpinDamage(
                    new BeyStatBlock { Weight = 30f, Attack = 50f },
                    new BeyStatBlock { Weight = 30f, Defense = 90f },
                    1f,
                    20f);
                if (highDefenseDamage >= lowDefenseDamage * 0.75f)
                {
                    throw new InvalidOperationException(
                        "Defense did not materially reduce received spin damage. " +
                        $"Low defense={lowDefenseDamage:F2}, " +
                        $"high defense={highDefenseDamage:F2}.");
                }

                stableTip = CreateTip(
                    "StableValidationTip",
                    TipBehaviorType.Sharp,
                    0.5f);
                aggressiveTip = CreateTip(
                    "AggressiveValidationTip",
                    TipBehaviorType.RubberFlat,
                    2.5f);

                BeyConfiguration stableBuild = new BeyConfiguration();
                stableBuild.EquipPart(highAttackWheel);
                stableBuild.EquipPart(stableTip);
                BeyConfiguration aggressiveBuild = new BeyConfiguration();
                aggressiveBuild.EquipPart(highAttackWheel);
                aggressiveBuild.EquipPart(aggressiveTip);
                BeyStatBlock stableStats = stableBuild.GetStatBlock();
                BeyStatBlock aggressiveStats = aggressiveBuild.GetStatBlock();
                if (stableStats.SpinRetention <= aggressiveStats.SpinRetention
                    || stableStats.TotalStaminaDrainRate
                    >= aggressiveStats.TotalStaminaDrainRate)
                {
                    throw new InvalidOperationException(
                        "Tip retention does not improve build stamina drain. " +
                        $"Stable={stableStats.SpinRetention:F1}/" +
                        $"{stableStats.TotalStaminaDrainRate:F2}, aggressive=" +
                        $"{aggressiveStats.SpinRetention:F1}/" +
                        $"{aggressiveStats.TotalStaminaDrainRate:F2}.");
                }

                Debug.Log(
                    $"[CombatBuildStats] Passed: {authoredCount} wheels, " +
                    $"{distinctAttacks} attack bands, {distinctDefenses} defense bands; " +
                    $"equal-weight damage {lowAttackDamage:F2}->{highAttackDamage:F2}, " +
                    $"defense damage {lowDefenseDamage:F2}->{highDefenseDamage:F2}, " +
                    $"retention {aggressiveStats.SpinRetention:F1}->" +
                    $"{stableStats.SpinRetention:F1}.");
            }
            finally
            {
                Destroy(lowAttackWheel);
                Destroy(highAttackWheel);
                Destroy(stableTip);
                Destroy(aggressiveTip);
            }
        }

        public static void ValidateFromCommandLine()
        {
            Validate();
        }

        private static void ValidateAuthoredWheelProfiles(
            out int authoredCount,
            out int distinctAttacks,
            out int distinctDefenses)
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:BeyPart",
                new[] { "Assets/Parts/Fusion Wheels" });
            HashSet<int> attackBands = new HashSet<int>();
            HashSet<int> defenseBands = new HashSet<int>();
            authoredCount = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                BeyPart wheel = AssetDatabase.LoadAssetAtPath<BeyPart>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (wheel == null || wheel.PartType != PartType.FusionWheel)
                    continue;

                FusionWheelCombatProfile profile =
                    FusionWheelCombatProfile.FromPart(wheel);
                if (!IsValidStat(profile.Attack)
                    || !IsValidStat(profile.Defense)
                    || !IsValidStat(profile.SpinRetention))
                {
                    throw new InvalidOperationException(
                        $"Wheel '{wheel.PartID}' produced invalid combat stats.");
                }

                authoredCount++;
                attackBands.Add(Mathf.RoundToInt(profile.Attack));
                defenseBands.Add(Mathf.RoundToInt(profile.Defense));
            }

            distinctAttacks = attackBands.Count;
            distinctDefenses = defenseBands.Count;
            if (authoredCount != 150
                || distinctAttacks < 15
                || distinctDefenses < 15)
            {
                throw new InvalidOperationException(
                    $"Authored wheel profile coverage is too narrow. Wheels=" +
                    $"{authoredCount}, attack bands={distinctAttacks}, " +
                    $"defense bands={distinctDefenses}.");
            }
        }

        private static void FindEqualWeightAttackExtremes(
            out BeyPart lowAttackWheel,
            out BeyPart highAttackWheel)
        {
            lowAttackWheel = null;
            highAttackWheel = null;
            float lowestAttack = float.MaxValue;
            float highestAttack = float.MinValue;

            for (int seed = 0; seed < 1000; seed++)
            {
                BeyPart candidate = CreateWheel(
                    $"ValidationWheel_{seed}",
                    seed,
                    30f,
                    0.8f);
                float attack =
                    FusionWheelCombatProfile.FromPart(candidate).Attack;

                if (attack < lowestAttack)
                {
                    Destroy(lowAttackWheel);
                    lowAttackWheel = candidate;
                    lowestAttack = attack;
                    candidate = null;
                }
                if (candidate != null && attack > highestAttack)
                {
                    Destroy(highAttackWheel);
                    highAttackWheel = candidate;
                    highestAttack = attack;
                    candidate = null;
                }

                Destroy(candidate);
            }
        }

        private static BeyPart CreateWheel(
            string name,
            int seed,
            float weight,
            float massDrain)
        {
            BeyPart part = ScriptableObject.CreateInstance<BeyPart>();
            part.name = name;
            SetField(part, "partType", PartType.FusionWheel);
            SetField(
                part,
                "occupiesSlots",
                new List<PartType> { PartType.FusionWheel });
            SetField(part, "meshSeed", seed);
            SetField(part, "weight", weight);
            SetField(part, "massBasedStaminaDrainRate", massDrain);
            return part;
        }

        private static BeyPart CreateTip(
            string name,
            TipBehaviorType behavior,
            float behaviorDrain)
        {
            BeyPart part = ScriptableObject.CreateInstance<BeyPart>();
            part.name = name;
            SetField(part, "partType", PartType.Tip);
            SetField(
                part,
                "occupiesSlots",
                new List<PartType> { PartType.Tip });
            SetField(part, "tipBehavior", behavior);
            SetField(
                part,
                "behaviorBasedStaminaDrainModifier",
                behaviorDrain);
            return part;
        }

        private static void SetField<T>(
            BeyPart part,
            string fieldName,
            T value)
        {
            FieldInfo field = typeof(BeyPart).GetField(
                fieldName,
                FieldFlags);
            if (field == null)
            {
                throw new MissingFieldException(
                    typeof(BeyPart).FullName,
                    fieldName);
            }
            field.SetValue(part, value);
        }

        private static bool IsValidStat(float value)
        {
            return !float.IsNaN(value)
                && !float.IsInfinity(value)
                && value >= 0f
                && value <= 100f;
        }

        private static void Destroy(UnityEngine.Object value)
        {
            if (value != null)
                UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
