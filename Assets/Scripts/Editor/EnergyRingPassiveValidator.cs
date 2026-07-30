using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BladeSpinners.Abilities;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BladeSpinners.Editor
{
    public sealed class EnergyRingPassiveValidator :
        IPreprocessBuildWithReport
    {
        private const int ExpectedEnergyRingCount = 150;

        public int callbackOrder => -610;

        [MenuItem(
            "Blade Spinners/Validation/Energy Ring Passives")]
        public static void ValidateFromMenu()
        {
            ValidationSummary summary = ValidateAll(true);
            Debug.Log(summary.ToLogLine());
        }

        public static void RunFromCommandLine()
        {
            try
            {
                ValidationSummary summary = ValidateAll(true);
                Debug.Log(summary.ToLogLine());
                if (Application.isBatchMode)
                    EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                else
                    throw;
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            ValidationSummary summary = ValidateAll(false);
            Debug.Log(summary.ToLogLine());
        }

        private static ValidationSummary ValidateAll(
            bool includeBehaviorChecks)
        {
            IReadOnlyList<BeyPassive> definitions =
                EnergyRingPassiveResolver.AllPassives;
            if (definitions.Count != 10)
            {
                throw new BuildFailedException(
                    "Expected 10 Energy Ring passive definitions, " +
                    $"found {definitions.Count}.");
            }

            HashSet<Type> definitionTypes = new HashSet<Type>();
            for (int i = 0; i < definitions.Count; i++)
            {
                BeyPassive definition = definitions[i];
                if (definition == null)
                {
                    throw new BuildFailedException(
                        $"Energy Ring passive slot {i} is null.");
                }

                if (!definitionTypes.Add(definition.GetType()))
                {
                    throw new BuildFailedException(
                        "Duplicate Energy Ring passive type: " +
                        definition.GetType().Name);
                }

                if (string.IsNullOrWhiteSpace(
                        definition.PassiveName)
                    || string.IsNullOrWhiteSpace(
                        definition.Description))
                {
                    throw new BuildFailedException(
                        $"{definition.GetType().Name} has incomplete metadata.");
                }
            }

            string[] guids = AssetDatabase.FindAssets(
                "t:BeyPart",
                new[] { "Assets/Parts/Energy Rings" });
            List<BeyPart> rings = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<BeyPart>)
                .Where(part =>
                    part != null
                    && part.PartType == PartType.EnergyRing)
                .ToList();
            if (rings.Count != ExpectedEnergyRingCount)
            {
                throw new BuildFailedException(
                    "Energy Ring passive coverage expected " +
                    $"{ExpectedEnergyRingCount} authored rings, " +
                    $"found {rings.Count}.");
            }

            Dictionary<Type, int> distribution =
                definitionTypes.ToDictionary(type => type, _ => 0);
            for (int i = 0; i < rings.Count; i++)
            {
                BeyPart ring = rings[i];
                BeyPassive passive =
                    EnergyRingPassiveResolver.Resolve(ring);
                if (passive == null)
                {
                    throw new BuildFailedException(
                        $"Energy Ring '{ring.PartName}' did not resolve a passive.");
                }

                if (!distribution.ContainsKey(passive.GetType()))
                {
                    throw new BuildFailedException(
                        $"Energy Ring '{ring.PartName}' resolved unexpected " +
                        $"passive type {passive.GetType().Name}.");
                }
                distribution[passive.GetType()]++;
            }

            foreach (KeyValuePair<Type, int> pair in distribution)
            {
                if (pair.Value <= 0)
                {
                    throw new BuildFailedException(
                        $"{pair.Key.Name} is unreachable from the authored " +
                        "Energy Ring collection.");
                }
            }

            if (includeBehaviorChecks)
                ValidateBehavior();

            return new ValidationSummary(
                rings.Count,
                definitions.Count,
                distribution,
                includeBehaviorChecks ? 10 : 0);
        }

        private static void ValidateBehavior()
        {
            ValidateImpactGuard();
            ValidateLowSpinSurge();
            ValidateKineticBattery();
            ValidateRecoilRecovery();
            ValidateArcConversion();
            ValidateManaConduit();
            ValidateEnduranceMatrix();
            ValidateSecondWind();
            ValidatePickupAmplifier();
            ValidateSpinRecovery();
        }

        private static void ValidateImpactGuard()
        {
            BeyConfiguration config = CreateConfiguration(
                Find<ImpactGuardPassive>());
            config.SetSpin(100f);
            float taken =
                config.ApplyCollisionSpinDamage(null, 20f);
            AssertApproximately(
                16f, taken, "Impact Guard damage");
        }

        private static void ValidateLowSpinSurge()
        {
            BeyConfiguration config = CreateConfiguration(
                Find<LowSpinSurgePassive>());
            config.SetSpin(20f);
            float damage =
                config.ModifyOutgoingCollisionDamage(null, 20f);
            AssertApproximately(
                25f, damage, "Low Spin Surge damage");
        }

        private static void ValidateKineticBattery()
        {
            BeyConfiguration config = CreateConfiguration(
                Find<KineticBatteryPassive>(), 200f);
            config.SetMana(50f);
            config.NotifyBeyCollision(null, 0f, 0f);
            AssertApproximately(
                60f, config.CurrentMana, "Kinetic Battery mana");
        }

        private static void ValidateRecoilRecovery()
        {
            BeyConfiguration config = CreateConfiguration(
                Find<RecoilRecoveryPassive>());
            config.SetSpin(100f);
            config.ApplyCollisionSpinDamage(null, 20f);
            AssertApproximately(
                85f, config.CurrentSpin, "Recoil Recovery spin");
        }

        private static void ValidateArcConversion()
        {
            BeyConfiguration config = CreateConfiguration(
                Find<ArcConversionPassive>());
            config.SetSpin(50f);
            config.SetMana(80f);
            AssertApproximately(
                54f, config.CurrentSpin, "Arc Conversion spin");
        }

        private static void ValidateManaConduit()
        {
            BeyConfiguration config = CreateConfiguration(
                Find<ManaConduitPassive>());
            float regeneration = config.EnergyRingPassive
                .ModifyManaRegeneration(10f);
            AssertApproximately(
                13.5f, regeneration, "Mana Conduit regeneration");
        }

        private static void ValidateEnduranceMatrix()
        {
            BeyConfiguration config = CreateConfiguration(
                Find<EnduranceMatrixPassive>());
            float drain = config.EnergyRingPassive
                .ModifyPassiveSpinDrain(10f);
            AssertApproximately(
                8f, drain, "Endurance Matrix drain");
        }

        private static void ValidateSecondWind()
        {
            BeyConfiguration config = CreateConfiguration(
                Find<SecondWindPassive>());
            config.SetSpin(50f);
            config.ApplyCollisionSpinDamage(null, 100f);
            AssertApproximately(
                12f, config.CurrentSpin, "Second Wind saved spin");
            config.ApplyCollisionSpinDamage(null, 100f);
            AssertApproximately(
                0f, config.CurrentSpin, "Second Wind one-use limit");
        }

        private static void ValidatePickupAmplifier()
        {
            BeyConfiguration config = CreateConfiguration(
                Find<PickupAmplifierPassive>());
            float amount = config.ModifyPickupAmount(10f);
            AssertApproximately(
                15f, amount, "Collector's Prism pickup");
        }

        private static void ValidateSpinRecovery()
        {
            BeyConfiguration config = CreateConfiguration(
                Find<SpinRecoveryPassive>());
            config.SetSpin(50f);
            config.TickEnergyRingPassive(2.9f);
            AssertApproximately(
                50f, config.CurrentSpin, "Spin Recovery delay");
            config.TickEnergyRingPassive(0.2f);
            if (config.CurrentSpin <= 50f)
            {
                throw new BuildFailedException(
                    "Spin Recovery did not restore spin after its delay.");
            }
        }

        private static BeyConfiguration CreateConfiguration(
            BeyPassive passive,
            float manaPool = 100f)
        {
            BeyPart ring =
                ScriptableObject.CreateInstance<BeyPart>();
            SetField(ring, "partName", "Passive Test Ring");
            SetField(ring, "partID", "passive_test_ring");
            SetField(ring, "partType", PartType.EnergyRing);
            SetField(
                ring,
                "occupiesSlots",
                new List<PartType> { PartType.EnergyRing });
            SetField(ring, "manaPoolSize", manaPool);
            SetField(ring, "equippedPassive", passive);

            BeyConfiguration configuration =
                new BeyConfiguration();
            configuration.EquipPart(ring);
            configuration.ResetResourcesForMatch();
            return configuration;
        }

        private static T Find<T>() where T : BeyPassive
        {
            T passive = EnergyRingPassiveResolver.AllPassives
                .OfType<T>()
                .FirstOrDefault();
            if (passive == null)
            {
                throw new BuildFailedException(
                    $"Missing passive definition {typeof(T).Name}.");
            }
            return passive;
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = typeof(BeyPart).GetField(
                fieldName,
                BindingFlags.Instance
                | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new BuildFailedException(
                    $"BeyPart.{fieldName} was not found.");
            }
            field.SetValue(target, value);
        }

        private static void AssertApproximately(
            float expected,
            float actual,
            string label)
        {
            if (!Mathf.Approximately(expected, actual))
            {
                throw new BuildFailedException(
                    $"{label}: expected {expected:0.###}, " +
                    $"received {actual:0.###}.");
            }
        }

        private readonly struct ValidationSummary
        {
            private readonly int ringCount;
            private readonly int passiveCount;
            private readonly Dictionary<Type, int> distribution;
            private readonly int behaviorCheckCount;

            public ValidationSummary(
                int rings,
                int passives,
                Dictionary<Type, int> counts,
                int behaviorChecks)
            {
                ringCount = rings;
                passiveCount = passives;
                distribution = counts;
                behaviorCheckCount = behaviorChecks;
            }

            public string ToLogLine()
            {
                string counts = string.Join(
                    ", ",
                    distribution
                        .OrderBy(pair => pair.Key.Name)
                        .Select(pair =>
                            $"{pair.Key.Name}={pair.Value}"));
                return
                    "[EnergyRingPassives] PASS: " +
                    $"{ringCount} rings, " +
                    $"{passiveCount} definitions, " +
                    $"{behaviorCheckCount} behavior checks. " +
                    counts;
            }
        }
    }
}
