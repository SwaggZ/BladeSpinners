using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;
using UnityEditor;
using UnityEngine;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Verifies that every exposed balance field has a gameplay consumer and
    /// exercises the previously disconnected starting-resource and wheel-drain paths.
    /// </summary>
    public static class BalanceControlsValidator
    {
        private const BindingFlags PrivateInstance =
            BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly string[] BalanceFields =
        {
            "speedMultiplier",
            "accelerationMultiplier",
            "turnSpeedMultiplier",
            "jumpMultiplier",
            "boostMultiplier",
            "knockbackMultiplier",
            "spinExchangeMultiplier",
            "spinDrainMultiplier",
            "startingSpinMultiplier",
            "manaRegenMultiplier",
            "manaPoolMultiplier",
            "abilityCostMultiplier",
            "visualSpinMultiplier",
            "enemySpeedMultiplier",
            "enemyAccelerationMultiplier",
            "enemyTurnSpeedMultiplier",
            "enemyJumpMultiplier",
            "enemyBoostMultiplier",
            "enemyKnockbackMultiplier",
            "enemySpinExchangeMultiplier",
            "enemySpinDrainMultiplier",
            "enemyStartingSpinMultiplier",
            "enemyManaRegenMultiplier",
            "enemyManaPoolMultiplier",
            "enemyAbilityCostMultiplier",
            "enemyVisualSpinMultiplier"
        };

        [MenuItem("Blade Spinners/Validation/Test Balance Controls")]
        public static void Validate()
        {
            ValidateEveryControlHasGameplayConsumer();

            GameManager previousManager = GameManager.Instance;
            GameObject temporaryManagerObject = null;
            GameManager manager = previousManager;
            Dictionary<string, float> previousValues = null;
            BeyPart ring = null;
            BeyPart tip = null;
            BeyPart lowDrainWheel = null;
            BeyPart highDrainWheel = null;

            try
            {
                if (manager == null)
                {
                    temporaryManagerObject =
                        new GameObject("BalanceControlsValidator_GameManager");
                    manager = temporaryManagerObject.AddComponent<GameManager>();
                    SetGameManagerInstance(manager);
                }

                previousValues = CaptureBalanceValues(manager);
                SetAllBalanceValues(manager, 1f);
                manager.startingSpinMultiplier = 1.2f;
                manager.enemyStartingSpinMultiplier = 0.5f;
                manager.manaPoolMultiplier = 1.5f;
                manager.enemyManaPoolMultiplier = 0.4f;

                ring = CreatePart(PartType.EnergyRing);
                SetPartField(ring, "manaPoolSize", 200f);

                BeyConfiguration player = new BeyConfiguration();
                player.EquipPart(ring);
                player.ResetResourcesForMatch();
                BeyConfiguration enemy =
                    new BeyConfiguration { IsEnemy = true };
                enemy.EquipPart(ring);
                enemy.ResetResourcesForMatch();

                AssertApproximately(
                    player.CurrentSpin,
                    120f,
                    "player starting spin");
                AssertApproximately(
                    enemy.CurrentSpin,
                    60f,
                    "enemy starting spin");
                AssertApproximately(
                    player.MaxMana,
                    300f,
                    "player maximum mana");
                AssertApproximately(
                    enemy.MaxMana,
                    120f,
                    "enemy maximum mana");
                AssertApproximately(
                    player.CurrentMana,
                    player.MaxMana,
                    "player reset mana");
                AssertApproximately(
                    enemy.CurrentMana,
                    enemy.MaxMana,
                    "enemy reset mana");

                enemy.SetMana(9999f);
                AssertApproximately(
                    enemy.CurrentMana,
                    enemy.MaxMana,
                    "enemy mana clamp");

                manager.startingSpinMultiplier = 1f;
                manager.enemyStartingSpinMultiplier = 1f;
                manager.spinDrainMultiplier = 1f;
                manager.enemySpinDrainMultiplier = 1f;

                tip = CreatePart(PartType.Tip);
                SetPartField(
                    tip,
                    "tipBehavior",
                    TipBehaviorType.Ball);
                SetPartField(
                    tip,
                    "behaviorBasedStaminaDrainModifier",
                    1f);
                lowDrainWheel = CreateWheel(12345, 0.2f);
                highDrainWheel = CreateWheel(12345, 1.8f);

                float lowDrain = MeasureOneSecondDrain(
                    lowDrainWheel,
                    tip);
                float highDrain = MeasureOneSecondDrain(
                    highDrainWheel,
                    tip);
                if (highDrain <= lowDrain * 2f)
                {
                    throw new InvalidOperationException(
                        "Authored Fusion Wheel drain did not materially affect " +
                        $"passive spin loss. Low={lowDrain:F3}, high={highDrain:F3}.");
                }

                Debug.Log(
                    $"[BalanceControls] Passed: all {BalanceFields.Length} exposed " +
                    "controls have gameplay consumers; starting spin player/enemy=" +
                    $"{player.CurrentSpin:F0}/{enemy.CurrentSpin:F0}, max mana=" +
                    $"{player.MaxMana:F0}/{enemy.MaxMana:F0}, authored wheel drain=" +
                    $"{lowDrain:F3}/{highDrain:F3}.");
            }
            finally
            {
                Destroy(ring);
                Destroy(tip);
                Destroy(lowDrainWheel);
                Destroy(highDrainWheel);

                if (manager != null && previousValues != null)
                    RestoreBalanceValues(manager, previousValues);
                if (temporaryManagerObject != null)
                    UnityEngine.Object.DestroyImmediate(
                        temporaryManagerObject);
                SetGameManagerInstance(previousManager);
            }
        }

        public static void ValidateFromCommandLine()
        {
            Validate();
        }

        private static void ValidateEveryControlHasGameplayConsumer()
        {
            string scriptsRoot = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets",
                "Scripts");
            string[] files = Directory.GetFiles(
                scriptsRoot,
                "*.cs",
                SearchOption.AllDirectories);
            List<string> sources = new List<string>();
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileName(files[i]);
                if (fileName == "GameManager.cs"
                    || fileName == "GameManagerWindow.cs"
                    || fileName == "BalanceControlsValidator.cs")
                {
                    continue;
                }
                sources.Add(File.ReadAllText(files[i]));
            }

            List<string> missing = new List<string>();
            for (int i = 0; i < BalanceFields.Length; i++)
            {
                string expectedAccess = "g => g."
                    + BalanceFields[i];
                bool found = false;
                for (int sourceIndex = 0;
                    sourceIndex < sources.Count;
                    sourceIndex++)
                {
                    if (sources[sourceIndex].Contains(expectedAccess))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    missing.Add(BalanceFields[i]);
            }

            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    "Balance controls without gameplay consumers: "
                    + string.Join(", ", missing));
            }
        }

        private static float MeasureOneSecondDrain(
            BeyPart wheel,
            BeyPart tip)
        {
            BeyConfiguration config = new BeyConfiguration();
            config.EquipPart(wheel);
            config.EquipPart(tip);
            config.SetSpin(GameConstants.MAX_SPIN);
            float before = config.CurrentSpin;
            config.DrainSpin(1f);
            return before - config.CurrentSpin;
        }

        private static BeyPart CreateWheel(int seed, float massDrain)
        {
            BeyPart part = CreatePart(PartType.FusionWheel);
            SetPartField(part, "meshSeed", seed);
            SetPartField(part, "weight", 30f);
            SetPartField(
                part,
                "massBasedStaminaDrainRate",
                massDrain);
            return part;
        }

        private static BeyPart CreatePart(PartType type)
        {
            BeyPart part = ScriptableObject.CreateInstance<BeyPart>();
            part.name = $"BalanceValidation_{type}";
            SetPartField(part, "partType", type);
            SetPartField(
                part,
                "occupiesSlots",
                new List<PartType> { type });
            return part;
        }

        private static void SetPartField<T>(
            BeyPart part,
            string fieldName,
            T value)
        {
            FieldInfo field = typeof(BeyPart).GetField(
                fieldName,
                PrivateInstance);
            if (field == null)
            {
                throw new MissingFieldException(
                    typeof(BeyPart).FullName,
                    fieldName);
            }
            field.SetValue(part, value);
        }

        private static Dictionary<string, float> CaptureBalanceValues(
            GameManager manager)
        {
            Dictionary<string, float> values =
                new Dictionary<string, float>();
            for (int i = 0; i < BalanceFields.Length; i++)
            {
                FieldInfo field = typeof(GameManager).GetField(
                    BalanceFields[i],
                    BindingFlags.Instance | BindingFlags.Public);
                values[BalanceFields[i]] = (float)field.GetValue(manager);
            }
            return values;
        }

        private static void SetGameManagerInstance(GameManager manager)
        {
            typeof(GameManager)
                .GetProperty(
                    "Instance",
                    BindingFlags.Static | BindingFlags.Public)
                ?.GetSetMethod(true)
                ?.Invoke(null, new object[] { manager });
        }

        private static void SetAllBalanceValues(
            GameManager manager,
            float value)
        {
            for (int i = 0; i < BalanceFields.Length; i++)
            {
                typeof(GameManager).GetField(
                    BalanceFields[i],
                    BindingFlags.Instance | BindingFlags.Public)
                    ?.SetValue(manager, value);
            }
        }

        private static void RestoreBalanceValues(
            GameManager manager,
            Dictionary<string, float> values)
        {
            foreach (KeyValuePair<string, float> pair in values)
            {
                typeof(GameManager).GetField(
                    pair.Key,
                    BindingFlags.Instance | BindingFlags.Public)
                    ?.SetValue(manager, pair.Value);
            }
        }

        private static void AssertApproximately(
            float actual,
            float expected,
            string scenario)
        {
            if (Mathf.Abs(actual - expected) > 0.001f)
            {
                throw new InvalidOperationException(
                    $"{scenario}: expected {expected:F3}, got {actual:F3}.");
            }
        }

        private static void Destroy(UnityEngine.Object value)
        {
            if (value != null)
                UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
