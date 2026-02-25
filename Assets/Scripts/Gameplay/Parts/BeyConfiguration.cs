using UnityEngine;
using System;
using System.Collections.Generic;
using BladeSpinners.Core;
using BladeSpinners.Abilities;

namespace BladeSpinners.Gameplay.Parts
{
    /// <summary>
    /// Runtime class that holds the current 5 equipped parts and calculates the combined
    /// stat block from all equipped parts. Supports hybrid parts that lock multiple slots.
    /// Listens to spin changes and notifies parts of spin threshold crossings.
    /// </summary>
    public class BeyConfiguration
    {
        private Dictionary<PartType, BeyPart> equippedParts = new Dictionary<PartType, BeyPart>();
        private BeyStatBlock cachedStats;
        private bool statsDirty = true;

        private float currentSpin;
        private float currentMana;

        /// <summary>
        /// Event fired when spin value changes. Subscribers check spin thresholds.
        /// </summary>
        public event Action<float> OnSpinChanged;

        /// <summary>
        /// Event fired when mana value changes.
        /// </summary>
        public event Action<float> OnManaChanged;

        /// <summary>
        /// Event fired when a part is swapped.
        /// </summary>
        public event Action<PartType, BeyPart> OnPartSwapped;

        public BeyConfiguration()
        {
            // Initialize empty configuration
            InitializeEmptySlots();
            currentSpin = GameConstants.DEFAULT_STARTING_SPIN;
            currentMana = GameConstants.DEFAULT_MANA_POOL;
        }

        /// <summary>
        /// Set to true for enemy beys so GameManager enemy multipliers stack on top of global ones.
        /// </summary>
        public bool IsEnemy { get; set; } = false;

        private void InitializeEmptySlots()
        {
            equippedParts.Clear();
            foreach (PartType slotType in System.Enum.GetValues(typeof(PartType)))
            {
                equippedParts[slotType] = null;
            }
        }

        /// <summary>
        /// Equips a part into its slots. For hybrid parts, locks multiple slots.
        /// </summary>
        public void EquipPart(BeyPart part)
        {
            if (part == null)
                return;

            // Remove any existing parts that would conflict with hybrid slots
            foreach (PartType slot in part.OccupiesSlots)
            {
                equippedParts[slot] = null;
            }

            // Equip the new part to all its slots
            foreach (PartType slot in part.OccupiesSlots)
            {
                equippedParts[slot] = part;
            }

            statsDirty = true;
            OnPartSwapped?.Invoke(part.PartType, part);
        }

        /// <summary>
        /// Removes a part from its slots. Used when swapping parts.
        /// </summary>
        public void UnequipPart(PartType slotType)
        {
            if (equippedParts.ContainsKey(slotType) && equippedParts[slotType] != null)
            {
                BeyPart part = equippedParts[slotType];
                foreach (PartType slot in part.OccupiesSlots)
                {
                    equippedParts[slot] = null;
                }
                statsDirty = true;
            }
        }

        /// <summary>
        /// Gets the part equipped in a specific slot (null if empty).
        /// </summary>
        public BeyPart GetEquippedPart(PartType slotType)
        {
            return equippedParts.ContainsKey(slotType) ? equippedParts[slotType] : null;
        }

        /// <summary>
        /// Checks if a slot is occupied (including by hybrid parts).
        /// </summary>
        public bool IsSlotOccupied(PartType slotType)
        {
            return equippedParts.ContainsKey(slotType) && equippedParts[slotType] != null;
        }

        /// <summary>
        /// Gets all currently equipped parts (may contain duplicates for hybrid parts).
        /// </summary>
        public List<BeyPart> GetAllEquippedParts()
        {
            List<BeyPart> result = new List<BeyPart>();
            HashSet<BeyPart> seen = new HashSet<BeyPart>();

            foreach (var part in equippedParts.Values)
            {
                if (part != null && !seen.Contains(part))
                {
                    result.Add(part);
                    seen.Add(part);
                }
            }

            return result;
        }

        /// <summary>
        /// Calculates and caches the combined stat block from all equipped parts.
        /// Stats are only recalculated if something changed (statsDirty flag).
        /// </summary>
        public BeyStatBlock GetStatBlock()
        {
            if (statsDirty)
            {
                RecalculateStats();
                statsDirty = false;
            }

            return cachedStats;
        }

        private void RecalculateStats()
        {
            cachedStats = new BeyStatBlock();

            // Collect all unique equipment
            HashSet<BeyPart> uniqueParts = new HashSet<BeyPart>();
            foreach (var part in equippedParts.Values)
            {
                if (part != null)
                    uniqueParts.Add(part);
            }

            // Aggregate stats from all parts
            foreach (BeyPart part in uniqueParts)
            {
                // Tip stats
                cachedStats.TipBehavior = part.TipBehavior;
                cachedStats.BehaviorBasedStaminaDrainModifier = part.BehaviorBasedStaminaDrainModifier;
                cachedStats.UphillResistanceMultiplier = part.UphillResistanceMultiplier;
                cachedStats.SlopeMultiplier = part.SlopeMultiplier;
                cachedStats.SpinThreshold = part.SpinThreshold;
                cachedStats.AltTipBehavior = part.AltTipBehavior;

                // Track stats
                cachedStats.TrackHeight = part.TrackHeight;
                cachedStats.JumpArcModifier = part.JumpArcModifier;

                // Fusion Wheel stats
                cachedStats.Weight = part.Weight;
                cachedStats.MassBasedStaminaDrainRate = part.MassBasedStaminaDrainRate;

                // Energy Ring stats
                cachedStats.ManaPoolSize = part.ManaPoolSize;
                cachedStats.ManaRegenRate = part.ManaRegenRate;

                // Face Bolt stats
                cachedStats.EquippedAbility = part.EquippedAbility;
            }

            // Calculate total stamina drain as Fusion Wheel mass drain + Tip behavior drain modifier
            cachedStats.TotalStaminaDrainRate = 
                (GameConstants.BASE_MASS_DRAIN_RATE * (cachedStats.Weight / 25f)) + 
                (GameConstants.BASE_BEHAVIOR_DRAIN_RATE * cachedStats.BehaviorBasedStaminaDrainModifier);
        }

        /// <summary>
        /// Gets the active Tip behavior based on current spin vs spin threshold.
        /// </summary>
        public Core.TipBehaviorType GetActiveTipBehavior()
        {
            BeyStatBlock stats = GetStatBlock();
            
            // If no threshold is set, use primary behavior
            if (stats.SpinThreshold < 0)
                return stats.TipBehavior;

            // If spin is above threshold, use primary
            if (currentSpin > stats.SpinThreshold)
                return stats.TipBehavior;

            // If spin is below threshold, use alternative
            return stats.AltTipBehavior;
        }

        /// <summary>
        /// Sets the current spin value and notifies listeners of threshold crossings.
        /// </summary>
        public void SetSpin(float value)
        {
            float oldSpin = currentSpin;
            currentSpin = Mathf.Clamp(value, GameConstants.MIN_SPIN, GameConstants.MAX_SPIN);

            // Check if we crossed a threshold
            if (oldSpin != currentSpin)
            {
                OnSpinChanged?.Invoke(currentSpin);
            }
        }

        /// <summary>
        /// Drains spin based on elapsed time and current stat configuration.
        /// </summary>
        public void DrainSpin(float deltaTime, float boostMultiplier = 1f)
        {
            BeyStatBlock stats = GetStatBlock();
            float gmDrain = GameManager.GetForBey(IsEnemy, g => g.spinDrainMultiplier, g => g.enemySpinDrainMultiplier);
            float drain = stats.TotalStaminaDrainRate * deltaTime * boostMultiplier * gmDrain;
            SetSpin(currentSpin - drain);
        }

        public float CurrentSpin => currentSpin;
        public bool IsBurst => currentSpin <= 0;

        /// <summary>
        /// Sets the current mana value.
        /// </summary>
        public void SetMana(float value)
        {
            float oldMana = currentMana;
            float gmPool = GameManager.Get(g => g.manaPoolMultiplier);
            currentMana = Mathf.Clamp(value, GameConstants.MIN_MANA, GetStatBlock().ManaPoolSize * gmPool);

            if (oldMana != currentMana)
            {
                OnManaChanged?.Invoke(currentMana);
            }
        }

        /// <summary>
        /// Regenerates mana based on elapsed time and current stat configuration.
        /// </summary>
        public void RegenMana(float deltaTime)
        {
            BeyStatBlock stats = GetStatBlock();
            float gmRegen = GameManager.GetForBey(IsEnemy, g => g.manaRegenMultiplier, g => g.enemyManaRegenMultiplier);
            float regen = stats.ManaRegenRate * deltaTime * gmRegen;
            SetMana(currentMana + regen);
        }

        public float CurrentMana => currentMana;

        /// <summary>
        /// Checks if the player has enough mana to use an ability.
        /// </summary>
        public bool CanUseAbility(float manaCost)
        {
            return currentMana >= manaCost;
        }

        /// <summary>
        /// Consumes mana for ability usage.
        /// </summary>
        public void SpendMana(float amount)
        {
            float gmCost = GameManager.GetForBey(IsEnemy, g => g.abilityCostMultiplier, g => g.enemyAbilityCostMultiplier);
            SetMana(currentMana - amount * gmCost);
        }
    }

    /// <summary>
    /// Data class that holds the final combined stat block from all equipped parts.
    /// </summary>
    public class BeyStatBlock
    {
        public Core.TipBehaviorType TipBehavior = Core.TipBehaviorType.Ball;
        public float BehaviorBasedStaminaDrainModifier = 1f;
        public float UphillResistanceMultiplier = 1f;
        public float SlopeMultiplier = 1f;
        public float SpinThreshold = -1f;
        public Core.TipBehaviorType AltTipBehavior = Core.TipBehaviorType.Ball;

        public float TrackHeight = 1f;
        public float JumpArcModifier = 1f;

        public float Weight = 25f;
        public float MassBasedStaminaDrainRate = 0.5f;

        public float ManaPoolSize = 100f;
        public float ManaRegenRate = 20f;

        public Abilities.BeyAbility EquippedAbility;

        // Calculated values
        public float TotalStaminaDrainRate = 0.8f; // Sum of mass + behavior drains
    }
}
