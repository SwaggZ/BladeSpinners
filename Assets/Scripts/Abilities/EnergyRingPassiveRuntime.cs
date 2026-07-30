using System;
using System.Collections.Generic;
using BladeSpinners.Gameplay.Parts;
using UnityEngine;

namespace BladeSpinners.Abilities
{
    public readonly struct EnergyRingCollisionInfo
    {
        public BeyConfiguration Other { get; }
        public float DamageDealt { get; }
        public float DamageTaken { get; }

        public EnergyRingCollisionInfo(
            BeyConfiguration other,
            float damageDealt,
            float damageTaken)
        {
            Other = other;
            DamageDealt = Mathf.Max(0f, damageDealt);
            DamageTaken = Mathf.Max(0f, damageTaken);
        }
    }

    /// <summary>
    /// Per-bey state and lifecycle bridge for the passive supplied by the equipped
    /// Energy Ring. Definitions remain stateless; cooldowns and one-use flags live here.
    /// </summary>
    public sealed class EnergyRingPassiveRuntime
    {
        private readonly BeyConfiguration owner;
        private readonly Dictionary<string, float> values =
            new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly HashSet<string> flags =
            new HashSet<string>(StringComparer.Ordinal);

        private BeyPassive activePassive;
        private float elapsedTime;
        private float lastCollisionTime;
        private string lastFeedbackMessage = string.Empty;
        private float lastFeedbackTime = float.NegativeInfinity;
        private int triggerCount;

        public EnergyRingPassiveRuntime(BeyConfiguration ownerConfiguration)
        {
            owner = ownerConfiguration;
            lastCollisionTime = 0f;
        }

        public BeyConfiguration Owner => owner;
        public BeyPassive ActivePassive => activePassive;
        public float ElapsedTime => elapsedTime;
        public float TimeSinceCollision =>
            Mathf.Max(0f, elapsedTime - lastCollisionTime);
        public string LastFeedbackMessage => lastFeedbackMessage;
        public float LastFeedbackTime => lastFeedbackTime;
        public int TriggerCount => triggerCount;

        public void Refresh(BeyPart energyRing)
        {
            BeyPassive next = EnergyRingPassiveResolver.Resolve(energyRing);
            if (next == activePassive)
                return;

            activePassive?.OnUnequipped(this);
            activePassive = next;
            ResetState();
            activePassive?.OnEquipped(this);
        }

        public void ResetForMatch()
        {
            activePassive?.OnUnequipped(this);
            ResetState();
            activePassive?.OnEquipped(this);
        }

        public void Tick(float deltaTime)
        {
            if (activePassive == null || deltaTime <= 0f)
                return;

            elapsedTime += deltaTime;
            activePassive.Tick(this, deltaTime);
        }

        public float ModifyOutgoingCollisionDamage(
            BeyConfiguration target,
            float damage)
        {
            float result = activePassive != null
                ? activePassive.ModifyOutgoingCollisionDamage(
                    this, target, Mathf.Max(0f, damage))
                : damage;
            return Mathf.Max(0f, result);
        }

        public float ModifyIncomingCollisionDamage(
            BeyConfiguration source,
            float damage)
        {
            float result = activePassive != null
                ? activePassive.ModifyIncomingCollisionDamage(
                    this, source, Mathf.Max(0f, damage))
                : damage;
            return Mathf.Max(0f, result);
        }

        public float ModifyPassiveSpinDrain(float drain)
        {
            float result = activePassive != null
                ? activePassive.ModifyPassiveSpinDrain(
                    this, Mathf.Max(0f, drain))
                : drain;
            return Mathf.Max(0f, result);
        }

        public float ModifyManaRegeneration(float regeneration)
        {
            float result = activePassive != null
                ? activePassive.ModifyManaRegeneration(
                    this, Mathf.Max(0f, regeneration))
                : regeneration;
            return Mathf.Max(0f, result);
        }

        public float ModifyPickupAmount(float amount)
        {
            float result = activePassive != null
                ? activePassive.ModifyPickupAmount(
                    this, Mathf.Max(0f, amount))
                : amount;
            return Mathf.Max(0f, result);
        }

        public void NotifyCollision(
            BeyConfiguration other,
            float damageDealt,
            float damageTaken)
        {
            lastCollisionTime = elapsedTime;
            activePassive?.OnCollisionWithBey(
                this,
                new EnergyRingCollisionInfo(
                    other, damageDealt, damageTaken));
        }

        public void NotifyCollisionDamageTaken(
            BeyConfiguration source,
            float damageTaken)
        {
            activePassive?.OnCollisionDamageTaken(
                this, source, Mathf.Max(0f, damageTaken));
        }

        public void NotifySpinChanged(float previousSpin, float newSpin)
        {
            activePassive?.OnSpinChanged(
                this, previousSpin, newSpin);
        }

        public void NotifyManaSpent(float amount)
        {
            if (amount > 0f)
                activePassive?.OnManaSpent(this, amount);
        }

        public float GetValue(string key, float fallback = 0f)
        {
            return !string.IsNullOrEmpty(key)
                && values.TryGetValue(key, out float value)
                ? value
                : fallback;
        }

        public void SetValue(string key, float value)
        {
            if (!string.IsNullOrEmpty(key))
                values[key] = value;
        }

        public bool HasFlag(string key)
        {
            return !string.IsNullOrEmpty(key) && flags.Contains(key);
        }

        public void SetFlag(string key, bool enabled = true)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (enabled)
                flags.Add(key);
            else
                flags.Remove(key);
        }

        public void TriggerFeedback(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            lastFeedbackMessage = message;
            lastFeedbackTime = Time.unscaledTime;
            triggerCount++;
        }

        private void ResetState()
        {
            values.Clear();
            flags.Clear();
            elapsedTime = 0f;
            lastCollisionTime = 0f;
            lastFeedbackMessage = string.Empty;
            lastFeedbackTime = float.NegativeInfinity;
            triggerCount = 0;
        }
    }
}
