using System;
using System.Collections.Generic;
using System.Reflection;
using BladeSpinners.Abilities;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;
using UnityEditor;
using UnityEngine;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Regression coverage for atomic effective-cost spending and shared cooldowns.
    /// </summary>
    public static class AbilityActivationValidator
    {
        private const float Tolerance = 0.001f;

        private static readonly PropertyInfo GameManagerInstanceProperty =
            typeof(GameManager).GetProperty(
                "Instance", BindingFlags.Static | BindingFlags.Public);

        [MenuItem("Blade Spinners/Validation/Test Ability Costs And Cooldowns")]
        public static void Validate()
        {
            GameManager previousManager = GameManager.Instance;
            GameObject temporaryManagerObject = null;
            GameManager manager = previousManager;
            float previousGlobalCost = 1f;
            float previousEnemyCost = 1f;
            float previousManaPool = 1f;
            DashAbility dash = null;
            List<BeyAbility> abilityPool = null;

            try
            {
                if (manager == null)
                {
                    temporaryManagerObject =
                        new GameObject("AbilityActivationValidator_GameManager");
                    manager = temporaryManagerObject.AddComponent<GameManager>();
                    SetGameManagerInstance(manager);
                }

                previousGlobalCost = manager.abilityCostMultiplier;
                previousEnemyCost = manager.enemyAbilityCostMultiplier;
                previousManaPool = manager.manaPoolMultiplier;
                manager.manaPoolMultiplier = 1f;

                dash = ScriptableObject.CreateInstance<DashAbility>();
                if (dash == null)
                    throw new InvalidOperationException("Could not create Dash ability.");

                ValidatePlayerEffectiveCost(manager, dash);
                ValidateDiscount(manager, dash);
                ValidateEnemySurcharge(manager, dash);
                abilityPool = ValidateAbilityCooldownMetadata();

                Debug.Log(
                    $"[AbilityActivation] Passed: base cost {dash.ManaCost:F1}, " +
                    $"automatic cooldown {dash.CooldownDuration:F2}s; atomic player, " +
                    "discount, enemy-surcharge, repeated-cast, and all 51 metadata " +
                    "checks succeeded.");
            }
            finally
            {
                if (manager != null)
                {
                    manager.abilityCostMultiplier = previousGlobalCost;
                    manager.enemyAbilityCostMultiplier = previousEnemyCost;
                    manager.manaPoolMultiplier = previousManaPool;
                }

                if (abilityPool != null)
                {
                    for (int i = 0; i < abilityPool.Count; i++)
                    {
                        if (abilityPool[i] != null)
                            UnityEngine.Object.DestroyImmediate(abilityPool[i]);
                    }
                }

                if (dash != null)
                    UnityEngine.Object.DestroyImmediate(dash);

                if (temporaryManagerObject != null)
                    UnityEngine.Object.DestroyImmediate(temporaryManagerObject);

                SetGameManagerInstance(previousManager);
            }
        }

        public static void ValidateFromCommandLine()
        {
            Validate();
        }

        private static void ValidatePlayerEffectiveCost(
            GameManager manager, BeyAbility ability)
        {
            manager.abilityCostMultiplier = 1.5f;
            manager.enemyAbilityCostMultiplier = 2f;
            BeyConfiguration configuration = new BeyConfiguration();
            float expectedCost = ability.ManaCost * 1.5f;
            AssertApproximately(
                configuration.GetEffectiveAbilityCost(ability),
                expectedCost,
                "player effective cost");

            configuration.SetMana(expectedCost - 0.01f);
            float manaBeforeRejectedCast = configuration.CurrentMana;
            if (configuration.TryCommitAbilityUse(ability, out float rejectedCost)
                || !Mathf.Approximately(
                    configuration.CurrentMana, manaBeforeRejectedCast)
                || configuration.AbilityCooldownRemaining > 0f)
            {
                throw new InvalidOperationException(
                    "An unaffordable modified player cost was committed.");
            }
            AssertApproximately(rejectedCost, expectedCost, "rejected effective cost");

            configuration.SetMana(100f);
            if (!configuration.TryCommitAbilityUse(ability, out float committedCost))
                throw new InvalidOperationException("Affordable player cast was rejected.");
            AssertApproximately(committedCost, expectedCost, "committed effective cost");
            AssertApproximately(
                configuration.CurrentMana,
                100f - expectedCost,
                "post-cast player mana");
            AssertApproximately(
                configuration.AbilityCooldownRemaining,
                ability.CooldownDuration,
                "initial cooldown");

            float manaAfterFirstCast = configuration.CurrentMana;
            if (configuration.TryCommitAbilityUse(ability, out _)
                || !Mathf.Approximately(
                    configuration.CurrentMana, manaAfterFirstCast))
            {
                throw new InvalidOperationException(
                    "A second same-frame cast bypassed the shared cooldown.");
            }

            configuration.TickAbilityCooldown(ability.CooldownDuration * 0.5f);
            AssertApproximately(
                configuration.AbilityCooldownNormalized,
                0.5f,
                "half cooldown");
            configuration.TickAbilityCooldown(ability.CooldownDuration);
            if (!configuration.IsAbilityReady
                || configuration.AbilityCooldownRemaining != 0f)
            {
                throw new InvalidOperationException(
                    "Cooldown did not return to a ready state.");
            }
        }

        private static void ValidateDiscount(
            GameManager manager, BeyAbility ability)
        {
            manager.abilityCostMultiplier = 0.5f;
            BeyConfiguration configuration = new BeyConfiguration();
            configuration.SetMana(100f);
            float expectedCost = ability.ManaCost * 0.5f;

            if (!configuration.TryCommitAbilityUse(ability, out float committedCost))
                throw new InvalidOperationException("Discounted cast was rejected.");

            AssertApproximately(committedCost, expectedCost, "discounted cost");
            AssertApproximately(
                configuration.CurrentMana,
                100f - expectedCost,
                "discounted post-cast mana");
        }

        private static void ValidateEnemySurcharge(
            GameManager manager, BeyAbility ability)
        {
            manager.abilityCostMultiplier = 1.5f;
            manager.enemyAbilityCostMultiplier = 2f;
            BeyConfiguration configuration =
                new BeyConfiguration { IsEnemy = true };
            float expectedCost = ability.ManaCost * 3f;
            AssertApproximately(
                configuration.GetEffectiveAbilityCost(ability),
                expectedCost,
                "enemy effective cost");

            configuration.SetMana(expectedCost - 0.01f);
            if (configuration.TryCommitAbilityUse(ability, out _))
            {
                throw new InvalidOperationException(
                    "Enemy cast passed affordability using the unmodified base cost.");
            }

            configuration.SetMana(expectedCost);
            if (!configuration.TryCommitAbilityUse(ability, out float committedCost))
                throw new InvalidOperationException(
                    "Enemy cast with exactly enough modified mana was rejected.");
            AssertApproximately(committedCost, expectedCost, "enemy committed cost");
            AssertApproximately(
                configuration.CurrentMana, 0f, "enemy post-cast mana");
        }

        private static List<BeyAbility> ValidateAbilityCooldownMetadata()
        {
            List<BeyAbility> abilities = AbilityFactory.CreateRuntimeAbilityPool();
            HashSet<int> distinctCooldownMilliseconds = new HashSet<int>();

            for (int i = 0; i < abilities.Count; i++)
            {
                BeyAbility ability = abilities[i];
                if (ability == null
                    || !float.IsFinite(ability.CooldownDuration)
                    || ability.CooldownDuration <= 0f)
                {
                    throw new InvalidOperationException(
                        $"Invalid cooldown metadata at ability index {i}.");
                }

                distinctCooldownMilliseconds.Add(
                    Mathf.RoundToInt(ability.CooldownDuration * 1000f));
            }

            if (abilities.Count != 51)
            {
                throw new InvalidOperationException(
                    $"Expected 51 runtime abilities, found {abilities.Count}.");
            }
            if (distinctCooldownMilliseconds.Count < 4)
            {
                throw new InvalidOperationException(
                    "Automatic cooldown tuning did not vary meaningfully by ability.");
            }

            return abilities;
        }

        private static void SetGameManagerInstance(GameManager manager)
        {
            if (GameManagerInstanceProperty == null)
            {
                throw new MissingMemberException(
                    typeof(GameManager).FullName, "Instance");
            }

            GameManagerInstanceProperty.SetValue(null, manager);
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
