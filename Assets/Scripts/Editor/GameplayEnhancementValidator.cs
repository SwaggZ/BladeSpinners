using System;
using System.Reflection;
using BladeSpinners.Audio;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.UI;
using BladeSpinners.World;
using UnityEditor;
using UnityEngine;

namespace BladeSpinners.Editor
{
    public static class GameplayEnhancementValidator
    {
        private const BindingFlags InstancePrivate =
            BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticPrivate =
            BindingFlags.Static | BindingFlags.NonPublic;

        [MenuItem(
            "Blade Spinners/Validation/Gameplay Enhancement Batch")]
        public static void Validate()
        {
            ValidateLifeSteal();
            ValidatePickups();
            ValidateArenaPlacement();
            ValidateSurfaceMovement();
            ValidateLowBounceMaterial();
            ValidateTimerAndUiSurface();
            ValidateMusicBannerDuration();
            ValidateRecordOrdering();
            Debug.Log(
                "[GameplayEnhancements] Passed 9 lifesteal/reset checks, " +
                "spin and mana proportional pickup checks, recharge checks, " +
                "12 generated-arena placement checks, surface-tangent and " +
                "low-bounce checks, timer/records/UI wiring, and the 5-second banner.");
        }

        public static void ValidateFromCommandLine()
        {
            try
            {
                Validate();
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

        public static void ValidateRegressionSuiteFromCommandLine()
        {
            try
            {
                Validate();
                AbilityActivationValidator.Validate();
                OrbitTipPhysicsValidator.Validate();
                EnergyRingPassiveValidator.ValidateFromMenu();
                BalanceControlsValidator.Validate();
                SpinExchangeImpactValidator.Validate();
                MusicSystemValidator.Validate();
                Debug.Log(
                    "[GameplayEnhancements] Related regression suite passed.");
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

        private static void ValidateLifeSteal()
        {
            BeyConfiguration config = new BeyConfiguration();
            float[] expected =
            {
                0.50f,
                0.325f,
                0.21125f,
                0.1373125f,
                0.0892531f,
                0.0580145f
            };
            for (int i = 0; i < expected.Length; i++)
            {
                AssertApproximately(
                    expected[i],
                    config.ConsumeLifeStealRatio(1f),
                    $"life-steal use {i + 1}");
            }

            config.ResetResourcesForMatch();
            if (config.LifeStealUsesThisMatch != 0)
            {
                throw new InvalidOperationException(
                    "Life-steal usage did not reset with match resources.");
            }
            AssertApproximately(
                1f,
                config.NextLifeStealEfficiency,
                "life-steal reset efficiency");
            AssertApproximately(
                0.5f,
                config.ConsumeLifeStealRatio(10f),
                "life-steal first-use cap");
        }

        private static void ValidatePickups()
        {
            GameObject root = new GameObject(
                "GameplayEnhancementPickupTest");
            try
            {
                PickupPlaceholder pickup =
                    root.AddComponent<PickupPlaceholder>();
                MethodInfo apply = typeof(PickupPlaceholder).GetMethod(
                    "ApplyPickup",
                    InstancePrivate);
                FieldInfo chargeField =
                    typeof(PickupPlaceholder).GetField(
                        "charge",
                        InstancePrivate);
                if (apply == null || chargeField == null)
                {
                    throw new MissingMemberException(
                        "Pickup proportional-reward members are missing.");
                }

                BeyConfiguration config = new BeyConfiguration();
                config.SetSpin(50f);
                pickup.Initialize(PickupType.SpinMedium);
                apply.Invoke(pickup, new object[] { config, 0.5f });
                AssertApproximately(
                    65f,
                    config.CurrentSpin,
                    "half-charge spin pickup");

                config.SetMana(0f);
                pickup.Initialize(PickupType.Mana);
                apply.Invoke(pickup, new object[] { config, 0.5f });
                AssertApproximately(
                    config.MaxMana * 0.15f,
                    config.CurrentMana,
                    "half-charge mana pickup");

                chargeField.SetValue(pickup, 0.25f);
                pickup.AdvanceRecharge(6f);
                AssertApproximately(
                    0.75f,
                    pickup.Charge01,
                    "pickup recharge");
                pickup.AdvanceRecharge(30f);
                AssertApproximately(
                    1f,
                    pickup.Charge01,
                    "pickup recharge clamp");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ValidateArenaPlacement()
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0)
            {
                throw new InvalidOperationException(
                    "Ground layer is unavailable.");
            }

            for (int seed = 1; seed <= 12; seed++)
            {
                GameObject arena = ProceduralArenaGenerator.Generate(
                    seed,
                    RoomType.Combat,
                    0,
                    0,
                    2,
                    2);
                try
                {
                    Physics.SyncTransforms();
                    PickupPlaceholder[] pickups =
                        arena.GetComponentsInChildren<
                            PickupPlaceholder>(true);
                    if (pickups.Length != 4)
                    {
                        throw new InvalidOperationException(
                            $"Arena seed {seed} created {pickups.Length} " +
                            "pickups instead of four.");
                    }

                    for (int i = 0; i < pickups.Length; i++)
                    {
                        PickupPlaceholder pickup = pickups[i];
                        if (pickup.gameObject.layer == groundLayer)
                        {
                            throw new InvalidOperationException(
                                $"Arena seed {seed} pickup remained on Ground.");
                        }

                        Ray ray = new Ray(
                            pickup.transform.position
                                + Vector3.up * 0.2f,
                            Vector3.down);
                        if (!Physics.Raycast(
                                ray,
                                out RaycastHit hit,
                                4f,
                                1 << groundLayer,
                                QueryTriggerInteraction.Ignore))
                        {
                            throw new InvalidOperationException(
                                $"Arena seed {seed} pickup {i} has no ground below it.");
                        }

                        float height = pickup.transform.position.y
                            - hit.point.y;
                        if (Mathf.Abs(
                                height
                                - GameConstants.PICKUP_SPAWN_HEIGHT)
                            > 0.18f)
                        {
                            throw new InvalidOperationException(
                                $"Arena seed {seed} pickup {i} is {height:F3} m " +
                                "above ground instead of the configured " +
                                $"{GameConstants.PICKUP_SPAWN_HEIGHT:F3} m.");
                        }
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(arena);
                }
            }
        }

        private static void ValidateSurfaceMovement()
        {
            GameObject root = new GameObject(
                "GameplayEnhancementMovementTest");
            try
            {
                root.AddComponent<Rigidbody>().useGravity = false;
                BeyMovementController movement =
                    root.AddComponent<BeyMovementController>();
                FieldInfo grounded = typeof(BeyMovementController)
                    .GetField("isGrounded", InstancePrivate);
                FieldInfo groundNormal =
                    typeof(BeyMovementController).GetField(
                        "lastGroundNormal",
                        InstancePrivate);
                MethodInfo tangentMethod =
                    typeof(BeyMovementController).GetMethod(
                        "GetSurfaceTangent",
                        InstancePrivate);
                if (grounded == null
                    || groundNormal == null
                    || tangentMethod == null)
                {
                    throw new MissingMemberException(
                        "Surface-tangent movement members are missing.");
                }

                Vector3 normal =
                    new Vector3(0f, 1f, -1f).normalized;
                grounded.SetValue(movement, true);
                groundNormal.SetValue(movement, normal);
                Vector3 tangent = (Vector3)tangentMethod.Invoke(
                    movement,
                    new object[] { Vector3.forward });
                AssertApproximately(
                    0f,
                    Vector3.Dot(tangent, normal),
                    "surface tangent dot");
                if (tangent.y <= 0f)
                {
                    throw new InvalidOperationException(
                        "Outward bowl movement did not gain along-slope height.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ValidateLowBounceMaterial()
        {
            GameObject root = new GameObject(
                "GameplayEnhancementAssemblerTest");
            try
            {
                BeyAssembler assembler =
                    root.AddComponent<BeyAssembler>();
                MethodInfo ensure = typeof(BeyAssembler).GetMethod(
                    "EnsureInitialized",
                    InstancePrivate);
                FieldInfo materialField =
                    typeof(BeyAssembler).GetField(
                        "bouncyMaterial",
                        StaticPrivate);
                ensure?.Invoke(assembler, null);
                PhysicsMaterial material =
                    materialField?.GetValue(null) as PhysicsMaterial;
                if (material == null
                    || material.bounciness > 0.021f
                    || material.bounceCombine
                        != PhysicsMaterialCombine.Minimum)
                {
                    throw new InvalidOperationException(
                        "Bey mesh material is still configured to launch off ground.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ValidateTimerAndUiSurface()
        {
            Type uiType = typeof(RuntimeGameUiController);
            string[] methods =
            {
                "UpdateRunTimersAndRecords",
                "RecordCurrentRun",
                "DrawInventoryWorkspace",
                "DrawSelectedPartCardInRect",
                "DrawPersonalBestPanel",
                "FormatRunTime"
            };
            for (int i = 0; i < methods.Length; i++)
            {
                if (uiType.GetMethod(
                        methods[i],
                        InstancePrivate | BindingFlags.Static)
                    == null)
                {
                    throw new MissingMethodException(
                        uiType.Name,
                        methods[i]);
                }
            }

            if (typeof(RunRecordStore).GetMethod(
                    nameof(RunRecordStore.Record))
                == null)
            {
                throw new MissingMethodException(
                    nameof(RunRecordStore),
                    nameof(RunRecordStore.Record));
            }
        }

        private static void ValidateMusicBannerDuration()
        {
            FieldInfo hold = typeof(MusicNowPlayingBanner).GetField(
                "HoldDuration",
                StaticPrivate);
            FieldInfo fade = typeof(MusicNowPlayingBanner).GetField(
                "FadeDuration",
                StaticPrivate);
            float total = (float)hold.GetRawConstantValue()
                + (float)fade.GetRawConstantValue() * 2f;
            AssertApproximately(
                5f,
                total,
                "music banner duration");
        }

        private static void ValidateRecordOrdering()
        {
            MethodInfo fastest = typeof(RunRecordStore).GetMethod(
                "CompareFastest",
                StaticPrivate);
            MethodInfo deepest = typeof(RunRecordStore).GetMethod(
                "CompareDeepest",
                StaticPrivate);
            RunRecord shortRun = new RunRecord
            {
                durationSeconds = 60f,
                arenasCleared = 2,
                recordedUtc = "2026-01-01"
            };
            RunRecord longRun = new RunRecord
            {
                durationSeconds = 120f,
                arenasCleared = 5,
                recordedUtc = "2026-01-02"
            };
            int fastestOrder = (int)fastest.Invoke(
                null,
                new object[] { shortRun, longRun });
            int deepestOrder = (int)deepest.Invoke(
                null,
                new object[] { shortRun, longRun });
            if (fastestOrder >= 0 || deepestOrder <= 0)
            {
                throw new InvalidOperationException(
                    "Run-record leaderboard ordering is incorrect.");
            }
        }

        private static void AssertApproximately(
            float expected,
            float actual,
            string label)
        {
            if (Mathf.Abs(expected - actual) > 0.001f)
            {
                throw new InvalidOperationException(
                    $"{label}: expected {expected:F4}, got {actual:F4}.");
            }
        }
    }
}
