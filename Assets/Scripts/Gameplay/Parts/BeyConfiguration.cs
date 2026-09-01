using UnityEngine;
using System;
using System.Collections.Generic;
using BladeSpinners.Core;
using BladeSpinners.Abilities;
using BladeSpinners.Gameplay.Shrine;

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
        private bool spinDrainPaused;
        private float manaRegenDelayRemaining;
        private float abilityCooldownRemaining;
        private float lastAbilityCooldownDuration;
        private int lifeStealUsesThisMatch;
        private readonly EnergyRingPassiveRuntime energyRingPassiveRuntime;

        public const float LifeStealDecayPerUse = 0.65f;
        public const float MaximumLifeStealBaseRatio = 0.50f;

        public BladerShrineRunState ShrineState { get; set; }
        public Transform OwnerTransform { get; set; }

        public bool HasShrinePerk(ShrinePerkType perk) => ShrineState != null && ShrineState.HasPerk(perk);

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
            energyRingPassiveRuntime =
                new EnergyRingPassiveRuntime(this);
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
            if (part.OccupiesSlots.Contains(PartType.EnergyRing))
            {
                energyRingPassiveRuntime.Refresh(
                    GetEquippedPart(PartType.EnergyRing));
            }
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
                if (part.OccupiesSlots.Contains(
                        PartType.EnergyRing))
                {
                    energyRingPassiveRuntime.Refresh(
                        GetEquippedPart(PartType.EnergyRing));
                }
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
            float tipSpinRetention = 50f;
            float wheelSpinRetention = 50f;

            BeyPart tipPart = GetEquippedPart(PartType.Tip);
            if (tipPart != null)
            {
                cachedStats.TipBehavior = tipPart.TipBehavior;
                cachedStats.BehaviorBasedStaminaDrainModifier = tipPart.BehaviorBasedStaminaDrainModifier;
                cachedStats.UphillResistanceMultiplier = tipPart.UphillResistanceMultiplier;
                cachedStats.SlopeMultiplier = tipPart.SlopeMultiplier;
                cachedStats.SpinThreshold = tipPart.SpinThreshold;
                cachedStats.AltTipBehavior = tipPart.AltTipBehavior;
                tipSpinRetention =
                    BeyCombatStatCalculator.GetTipSpinRetention(tipPart);
            }

            BeyPart trackPart = GetEquippedPart(PartType.Track);
            if (trackPart != null)
            {
                cachedStats.TrackHeight = trackPart.TrackHeight;
                cachedStats.JumpArcModifier = trackPart.JumpArcModifier;
            }

            BeyPart wheelPart = GetEquippedPart(PartType.FusionWheel);
            if (wheelPart != null)
            {
                cachedStats.Weight = wheelPart.Weight;
                cachedStats.MassBasedStaminaDrainRate = wheelPart.MassBasedStaminaDrainRate;
                FusionWheelCombatProfile wheelProfile =
                    FusionWheelCombatProfile.FromPart(wheelPart);
                cachedStats.Attack = wheelProfile.Attack;
                cachedStats.Defense = wheelProfile.Defense;
                wheelSpinRetention = wheelProfile.SpinRetention;
            }

            BeyPart ringPart = GetEquippedPart(PartType.EnergyRing);
            if (ringPart != null)
            {
                cachedStats.ManaPoolSize = ringPart.ManaPoolSize;
                cachedStats.ManaRegenRate = ringPart.ManaRegenRate;
                cachedStats.EquippedPassive =
                    EnergyRingPassiveResolver.Resolve(ringPart);
            }

            BeyPart faceBoltPart = GetEquippedPart(PartType.FaceBolt);
            if (faceBoltPart != null)
            {
                cachedStats.EquippedAbility = FaceBoltAbilityResolver.Resolve(faceBoltPart);
            }

            cachedStats.SpinRetention =
                BeyCombatStatCalculator.CombineSpinRetention(
                    wheelSpinRetention,
                    tipSpinRetention);

            // The authored wheel drain now matters. Retention then modifies the
            // combined wheel + Tip drain while remaining centered at 1x for 50.
            float baseDrain =
                cachedStats.MassBasedStaminaDrainRate
                + GameConstants.BASE_BEHAVIOR_DRAIN_RATE
                * cachedStats.BehaviorBasedStaminaDrainModifier;
            cachedStats.TotalStaminaDrainRate =
                baseDrain
                * BeyCombatStatCalculator.GetRetentionDrainMultiplier(
                    cachedStats.SpinRetention);

            if (HasShrinePerk(ShrinePerkType.HeavyweightCore))
            {
                cachedStats.Weight += 25f;
            }

            if (HasShrinePerk(ShrinePerkType.IronPlating))
            {
                cachedStats.Defense += 20f;
            }

            if (HasShrinePerk(ShrinePerkType.IronFortress))
            {
                cachedStats.Defense += 35f;
            }
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
            // Phoenix Rebirth Resurrection Check
            if (value <= 0f && HasShrinePerk(ShrinePerkType.PhoenixRebirth) && !IsEnemy && !ShrineState.PhoenixRebirthUsed)
            {
                ShrineState.ConsumePhoenixRebirth();
                float reviveSpin = StartingSpin * 0.60f;
                currentSpin = reviveSpin;
                Vector3 spawnPos = OwnerTransform != null ? OwnerTransform.position : Vector3.zero;
                BladeSpinners.Abilities.EpicAbilityVFXHelper.SpawnInfernoPillar(spawnPos, 2.0f);
                BladeSpinners.Gameplay.ThirdPersonCameraController.TriggerScreenShake(0.7f, 0.5f);
                Debug.Log("🔥 [BladerShrine] PHOENIX REBIRTH ACTIVATED! Resurrected with 60% Spin!");
                OnSpinChanged?.Invoke(currentSpin);
                return;
            }

            float oldSpin = currentSpin;
            currentSpin = Mathf.Clamp(value, GameConstants.MIN_SPIN, GameConstants.MAX_SPIN);

            // Check if we crossed a threshold
            if (oldSpin != currentSpin)
            {
                energyRingPassiveRuntime.NotifySpinChanged(
                    oldSpin, currentSpin);
                OnSpinChanged?.Invoke(currentSpin);
            }
        }

        /// <summary>
        /// Applies delta to current spin.
        /// </summary>
        public void ModifySpin(float delta)
        {
            SetSpin(currentSpin + delta);
        }

        /// <summary>
        /// Drains spin based on elapsed time and current stat configuration.
        /// </summary>
        public void DrainSpin(float deltaTime, float boostMultiplier = 1f)
        {
            if (spinDrainPaused)
                return;

            BeyStatBlock stats = GetStatBlock();            
            float gmDrain = GameManager.GetForBey(IsEnemy, g => g.spinDrainMultiplier, g => g.enemySpinDrainMultiplier);
            float drain = stats.TotalStaminaDrainRate * deltaTime * boostMultiplier * gmDrain;
            drain =
                energyRingPassiveRuntime.ModifyPassiveSpinDrain(
                    drain);
            if (HasShrinePerk(ShrinePerkType.StaminaConservation) && !IsEnemy)
                drain *= 0.80f;
            SetSpin(currentSpin - drain);
        }

        public float CurrentSpin => currentSpin;
        public float StartingSpin
        {
            get
            {
                float multiplier = GameManager.GetForBey(
                    IsEnemy,
                    g => g.startingSpinMultiplier,
                    g => g.enemyStartingSpinMultiplier);
                return Mathf.Clamp(
                    GameConstants.DEFAULT_STARTING_SPIN
                    * Mathf.Max(0f, multiplier),
                    GameConstants.MIN_SPIN,
                    GameConstants.MAX_SPIN);
            }
        }

        public float MaxSpin => GameConstants.MAX_SPIN;

        public bool IsBurst => currentSpin <= 0;
        public bool IsSpinDrainPaused => spinDrainPaused;

        public void SetSpinDrainPaused(bool paused)
        {
            spinDrainPaused = paused;
        }

        /// <summary>
        /// Sets the current mana value.
        /// </summary>
        public void SetMana(float value)
        {
            float oldMana = currentMana;
            currentMana = Mathf.Clamp(
                value,
                GameConstants.MIN_MANA,
                MaxMana);

            // Any mana spend (ability, boost upkeep, etc.) resets regen delay.
            if (currentMana < oldMana)
            {
                manaRegenDelayRemaining = GameConstants.MANA_REGEN_DELAY_AFTER_USE;
                energyRingPassiveRuntime.NotifyManaSpent(
                    oldMana - currentMana);
            }

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
            UpdateManaRegen(deltaTime);
        }

        public void UpdateManaRegen(float deltaTime)
        {
            if (manaRegenDelayRemaining > 0f)
            {
                manaRegenDelayRemaining = Mathf.Max(0f, manaRegenDelayRemaining - deltaTime);
                return;
            }

            BeyStatBlock stats = GetStatBlock();
            float gmRegen = GameManager.GetForBey(IsEnemy, g => g.manaRegenMultiplier, g => g.enemyManaRegenMultiplier);
            float regen = stats.ManaRegenRate * deltaTime * gmRegen * GameConstants.BASE_MANA_REGEN_SCALAR;
            regen =
                energyRingPassiveRuntime.ModifyManaRegeneration(
                    regen);
            if (HasShrinePerk(ShrinePerkType.OverdriveCore))
                regen *= 1.25f;
            if (HasShrinePerk(ShrinePerkType.ManaSiphon) && !IsEnemy)
                regen += 3f * deltaTime;
            if (HasShrinePerk(ShrinePerkType.CentrifugalClutch) && (currentSpin / MaxSpin) >= 0.70f)
                regen *= 1.35f;
            if (HasShrinePerk(ShrinePerkType.DragonHeart) && !IsEnemy)
                regen *= 2.0f;
            SetMana(currentMana + regen);
        }

        public float CurrentMana => currentMana;
        public float MaxMana
        {
            get
            {
                float multiplier = GameManager.GetForBey(
                    IsEnemy,
                    g => g.manaPoolMultiplier,
                    g => g.enemyManaPoolMultiplier);
                float basePool = Mathf.Max(
                    GameConstants.MIN_MANA,
                    GetStatBlock().ManaPoolSize
                    * Mathf.Max(0f, multiplier));
                if (HasShrinePerk(ShrinePerkType.OverchargeCapacitor) && !IsEnemy)
                    basePool += 25f;
                return basePool;
            }
        }

        /// <summary>
        /// Restores faction-aware starting resources and transient ability state.
        /// Call after the loadout and IsEnemy flag have both been assigned.
        /// </summary>
        public void ResetResourcesForMatch()
        {
            SetSpin(StartingSpin);
            SetMana(MaxMana);
            SetSpinDrainPaused(false);
            ResetAbilityCooldown();
            lifeStealUsesThisMatch = 0;
            manaRegenDelayRemaining = 0f;
            energyRingPassiveRuntime.ResetForMatch();
        }

        public void ModifyMana(float amount)
        {
            SetMana(currentMana + amount);
        }

        /// <summary>
        /// Scales the starting spin based on rip-cord minigame timing (e.g. 1.25x for perfect launch).
        /// </summary>
        public void ApplyLaunchRipMultiplier(float multiplier)
        {
            float baseSpin = StartingSpin;
            float finalSpin = Mathf.Clamp(baseSpin * multiplier, GameConstants.MIN_SPIN, GameConstants.MAX_SPIN);
            SetSpin(finalSpin);
        }

        /// <summary>
        /// Returns this cast's effective restore ratio and advances the shared
        /// per-match diminishing-return counter. Target damage is intentionally
        /// unaffected; only spin returned to the caster is reduced.
        /// </summary>
        public float ConsumeLifeStealRatio(float baseRatio)
        {
            float ratio = Mathf.Clamp(
                    baseRatio,
                    0f,
                    MaximumLifeStealBaseRatio)
                * GetLifeStealEfficiency(lifeStealUsesThisMatch);
            lifeStealUsesThisMatch++;
            return ratio;
        }

        public static float GetLifeStealEfficiency(int previousUses)
        {
            return Mathf.Pow(
                LifeStealDecayPerUse,
                Mathf.Max(0, previousUses));
        }

        public int LifeStealUsesThisMatch => lifeStealUsesThisMatch;
        public float NextLifeStealEfficiency =>
            GetLifeStealEfficiency(lifeStealUsesThisMatch);

        public void TickEnergyRingPassive(float deltaTime)
        {
            energyRingPassiveRuntime.Tick(deltaTime);
        }

        public float ModifyOutgoingCollisionDamage(
            BeyConfiguration target,
            float damage)
        {
            return energyRingPassiveRuntime
                .ModifyOutgoingCollisionDamage(target, damage);
        }

        public float ApplyCollisionSpinDamage(
            BeyConfiguration source,
            float damage)
        {
            float modifiedDamage = energyRingPassiveRuntime
                .ModifyIncomingCollisionDamage(source, damage);
            float before = currentSpin;
            SetSpin(before - modifiedDamage);
            float damageTaken = before - currentSpin;
            energyRingPassiveRuntime.NotifyCollisionDamageTaken(
                source, damageTaken);
            return damageTaken;
        }

        public void NotifyBeyCollision(
            BeyConfiguration other,
            float damageDealt,
            float damageTaken)
        {
            energyRingPassiveRuntime.NotifyCollision(
                other, damageDealt, damageTaken);

            if (HasShrinePerk(ShrinePerkType.VampiricSpin) && damageDealt > 0f && !IsEnemy)
            {
                float siphoned = damageDealt * 0.15f;
                SetSpin(Mathf.Min(currentSpin + siphoned, GameConstants.MAX_SPIN));
                Vector3 spawnPos = OwnerTransform != null ? OwnerTransform.position : Vector3.zero;
                BladeSpinners.Abilities.EpicAbilityVFXHelper.SpawnSparkBurst(spawnPos, new Color(0.95f, 0.22f, 0.35f, 1f), 12);
            }
        }

        public float ModifyPickupAmount(float amount)
        {
            return energyRingPassiveRuntime
                .ModifyPickupAmount(amount);
        }

        public EnergyRingPassiveRuntime EnergyRingPassive =>
            energyRingPassiveRuntime;
        public BeyPassive ActivePassive =>
            energyRingPassiveRuntime.ActivePassive;

        /// <summary>
        /// Returns the final cost after global and faction-specific modifiers.
        /// </summary>
        public float GetEffectiveAbilityCost(BeyAbility ability)
        {
            return ability != null
                ? GetEffectiveAbilityCost(ability.ManaCost)
                : 0f;
        }

        /// <summary>
        /// Returns the final cost for a base amount after all current modifiers.
        /// </summary>
        public float GetEffectiveAbilityCost(float baseManaCost)
        {
            float multiplier = GameManager.GetForBey(
                IsEnemy,
                g => g.abilityCostMultiplier,
                g => g.enemyAbilityCostMultiplier);
            return CalculateEffectiveAbilityCost(baseManaCost, multiplier);
        }

        public static float CalculateEffectiveAbilityCost(
            float baseManaCost, float multiplier)
        {
            return Mathf.Max(0f, baseManaCost) * Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// Checks the shared cooldown and effective, modified mana cost.
        /// </summary>
        public bool CanUseAbility(BeyAbility ability)
        {
            if (ability == null || !IsAbilityReady)
                return false;

            float effectiveCost = GetEffectiveAbilityCost(ability);
            return currentMana + Mathf.Epsilon >= effectiveCost;
        }

        /// <summary>
        /// Backward-compatible affordability check for callers that only have a base cost.
        /// The shared cooldown and the same effective-cost calculation still apply.
        /// </summary>
        public bool CanUseAbility(float baseManaCost)
        {
            if (!IsAbilityReady)
                return false;

            float effectiveCost = GetEffectiveAbilityCost(baseManaCost);
            return currentMana + Mathf.Epsilon >= effectiveCost;
        }

        /// <summary>
        /// Atomically validates cooldown and affordability, spends the exact cost that
        /// was checked, and starts the equipped ability's cooldown.
        /// </summary>
        public bool TryCommitAbilityUse(
            BeyAbility ability, out float effectiveCost)
        {
            effectiveCost = GetEffectiveAbilityCost(ability);
            if (ability == null
                || !IsAbilityReady
                || currentMana + Mathf.Epsilon < effectiveCost)
            {
                return false;
            }

            SetMana(currentMana - effectiveCost);
            lastAbilityCooldownDuration = Mathf.Max(
                0f, ability.CooldownDuration);
            abilityCooldownRemaining = lastAbilityCooldownDuration;
            return true;
        }

        public void TickAbilityCooldown(float deltaTime)
        {
            if (abilityCooldownRemaining <= 0f || deltaTime <= 0f)
                return;

            float dt = HasShrinePerk(ShrinePerkType.OverdriveCore) ? deltaTime * 1.25f : deltaTime;
            abilityCooldownRemaining = Mathf.Max(
                0f, abilityCooldownRemaining - dt);
        }

        public void ResetAbilityCooldown()
        {
            abilityCooldownRemaining = 0f;
            lastAbilityCooldownDuration = 0f;
        }

        /// <summary>
        /// Consumes an effective, modified mana cost. New ability activations should use
        /// TryCommitAbilityUse so affordability and cooldown are handled atomically.
        /// </summary>
        public void SpendMana(float baseAmount)
        {
            SetMana(currentMana - GetEffectiveAbilityCost(baseAmount));
        }

        public float AbilityCooldownRemaining => abilityCooldownRemaining;
        public float AbilityCooldownDuration => lastAbilityCooldownDuration;
        public float AbilityCooldownNormalized =>
            lastAbilityCooldownDuration > 0f
                ? Mathf.Clamp01(
                    abilityCooldownRemaining / lastAbilityCooldownDuration)
                : 0f;
        public bool IsAbilityReady => abilityCooldownRemaining <= 0f;
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
        public float Attack = 50f;
        public float Defense = 50f;
        public float SpinRetention = 50f;

        public float ManaPoolSize = 100f;
        public float ManaRegenRate = 20f;

        public Abilities.BeyAbility EquippedAbility;
        public Abilities.BeyPassive EquippedPassive;

        // Calculated values
        public float TotalStaminaDrainRate = 0.8f; // Sum of mass + behavior drains
    }
}
