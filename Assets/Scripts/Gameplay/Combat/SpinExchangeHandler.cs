using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Gameplay.Combat
{
    /// <summary>
    /// Handles spin exchange calculations during collisions between two Beyblades.
    /// Spin is exchanged based on Fusion Wheel weight differential and attack contribution.
    /// Heavier Beys deal more spin damage and take less spin damage.
    /// </summary>
    public class SpinExchangeHandler
    {
        /// <summary>
        /// Calculates spin damage dealt by attacker to defender during a collision.
        /// Returns the amount of spin the defender loses (and attacker might gain).
        /// </summary>
        public static float CalculateSpinDamage(
            BeyStatBlock attackerStats,
            BeyStatBlock defenderStats,
            float collisionMagnitude,
            float attackerVelocityMagnitude)
        {
            // Base spin damage from collision
            float spinDamage = GameConstants.COLLISION_SPIN_EXCHANGE_BASE;

            // Weight differential multiplier
            float weightDifference = attackerStats.Weight - defenderStats.Weight;
            float weightMultiplier = 1f + (weightDifference / 100f) * GameConstants.WEIGHT_KNOCKBACK_MULTIPLIER;
            weightMultiplier = Mathf.Clamp(weightMultiplier, 0.5f, 2f); // Cap at 0.5x to 2x

            // Velocity contribution - faster collision deals more spin damage
            float velocityFactor = Mathf.Clamp01(attackerVelocityMagnitude / 50f); // Scales to max at 50 m/s

            spinDamage *= weightMultiplier * (1f + velocityFactor);

            return spinDamage;
        }

        /// <summary>
        /// Handles a full collision exchange between two Beys.
        /// Updates both Bey configurations' spin values.
        /// </summary>
        public static void HandleCollision(
            BeyConfiguration attackerConfig,
            BeyConfiguration defenderConfig,
            BeyStatBlock attackerStats,
            BeyStatBlock defenderStats,
            Vector3 attackerVelocity,
            Vector3 defenderVelocity,
            float spinExchangeMultiplier = 1f)
        {
            // Calculate spin damage based on attacker's stats and velocity
            float spinDamageToDefender = CalculateSpinDamage(
                attackerStats,
                defenderStats,
                1f, // Collision magnitude (could be enhanced with contact impact data)
                attackerVelocity.magnitude
            ) * spinExchangeMultiplier;

            // Calculate spin damage based on defender's counter-force
            float spinDamageToAttacker = CalculateSpinDamage(
                defenderStats,
                attackerStats,
                1f,
                defenderVelocity.magnitude * 0.3f // Defender's contribution is lower
            ) * spinExchangeMultiplier;

            // Apply spin damage
            defenderConfig.SetSpin(defenderConfig.CurrentSpin - spinDamageToDefender);
            attackerConfig.SetSpin(attackerConfig.CurrentSpin - spinDamageToAttacker);

            // Debug logging — collision damage
            string defName = defenderConfig.IsEnemy ? "Enemy" : "Player";
            string atkName = attackerConfig.IsEnemy ? "Enemy" : "Player";
            Debug.Log($"[Damage] {defName} took <color=red>-{spinDamageToDefender:F1}</color> spin → {defenderConfig.CurrentSpin:F1} remaining");
            Debug.Log($"[Damage] {atkName} took <color=red>-{spinDamageToAttacker:F1}</color> spin → {attackerConfig.CurrentSpin:F1} remaining");

            // TODO: Trigger collision effects (sound, particles, knockback)
        }

        /// <summary>
        /// Determines if a collision should trigger spin exchange.
        /// Low-speed collisions (rolling contact) don't exchange as much spin.
        /// </summary>
        public static bool ShouldExchangeSpin(float collisionRelativeVelocity)
        {
            // Only exchange spin on meaningful collisions (> 5 m/s relative velocity)
            return collisionRelativeVelocity > 5f;
        }
    }
}
