using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Gameplay.Combat
{
    public readonly struct CollisionImpactProfile
    {
        public float RelativeSpeed { get; }
        public float ClosingSpeed { get; }
        public float ApproachAlignment { get; }
        public float CollisionMagnitude { get; }
        public float DefenderFacingMultiplier { get; }

        public CollisionImpactProfile(
            float relativeSpeed,
            float closingSpeed,
            float approachAlignment,
            float collisionMagnitude,
            float defenderFacingMultiplier)
        {
            RelativeSpeed = relativeSpeed;
            ClosingSpeed = closingSpeed;
            ApproachAlignment = approachAlignment;
            CollisionMagnitude = collisionMagnitude;
            DefenderFacingMultiplier = defenderFacingMultiplier;
        }
    }

    /// <summary>
    /// Handles spin exchange calculations during collisions between two Beyblades.
    /// Spin damage uses wheel Attack, defender Defense, weight, real relative
    /// contact motion, and hit orientation.
    /// </summary>
    public class SpinExchangeHandler
    {
        /// <summary>
        /// Calculates spin damage dealt by attacker to defender during a collision.
        /// collisionMagnitude comes from the real closing speed and approach angle.
        /// </summary>
        public static float CalculateSpinDamage(
            BeyStatBlock attackerStats,
            BeyStatBlock defenderStats,
            float collisionMagnitude,
            float attackerVelocityMagnitude,
            float defenderFacingMultiplier = 1f)
        {
            float spinDamage = GameConstants.COLLISION_SPIN_EXCHANGE_BASE;

            float weightDifference =
                attackerStats.Weight - defenderStats.Weight;
            float weightMultiplier =
                1f + (weightDifference / 100f)
                * GameConstants.WEIGHT_KNOCKBACK_MULTIPLIER;
            weightMultiplier = Mathf.Clamp(weightMultiplier, 0.5f, 2f);

            // Personal speed contributes modestly. Relative contact motion remains
            // authoritative through the collision-magnitude factor.
            float velocityFactor = Mathf.Clamp01(
                attackerVelocityMagnitude / 50f);
            float personalSpeedMultiplier = Mathf.Lerp(
                0.85f, 1.2f, velocityFactor);
            float impactMultiplier = Mathf.Clamp(
                collisionMagnitude, 0.15f, 2.25f);
            float facingMultiplier = Mathf.Clamp(
                defenderFacingMultiplier, 0.75f, 1.4f);
            float attackMultiplier = Mathf.Lerp(
                0.75f,
                1.35f,
                Mathf.Clamp01(attackerStats.Attack / 100f));
            float defenseMultiplier = Mathf.Lerp(
                1.25f,
                0.70f,
                Mathf.Clamp01(defenderStats.Defense / 100f));

            spinDamage *= weightMultiplier
                * personalSpeedMultiplier
                * impactMultiplier
                * facingMultiplier
                * attackMultiplier
                * defenseMultiplier;

            return spinDamage;
        }

        /// <summary>
        /// Evaluates the planar contact. Closing speed determines strength,
        /// relative-velocity alignment distinguishes grazing from direct impacts, and
        /// defender facing distinguishes front, side, and rear exposure.
        /// </summary>
        public static CollisionImpactProfile EvaluateImpact(
            Vector3 attackerVelocity,
            Vector3 defenderVelocity,
            Vector3 attackerToDefenderNormal,
            Vector3 defenderForward)
        {
            Vector3 contactNormal = attackerToDefenderNormal;
            contactNormal.y = 0f;
            if (contactNormal.sqrMagnitude < 0.0001f)
                contactNormal = Vector3.right;
            else
                contactNormal.Normalize();

            Vector3 relativeVelocity =
                attackerVelocity - defenderVelocity;
            relativeVelocity.y = 0f;
            float relativeSpeed = relativeVelocity.magnitude;
            float closingSpeed = Mathf.Max(
                0f, Vector3.Dot(relativeVelocity, contactNormal));
            float approachAlignment = relativeSpeed > 0.001f
                ? Mathf.Clamp01(closingSpeed / relativeSpeed)
                : 0f;

            float contactSpeedFactor = Mathf.InverseLerp(
                2f, 30f, closingSpeed);
            float speedMagnitude = Mathf.Lerp(
                0.35f, 1.8f, contactSpeedFactor);
            float approachMultiplier = Mathf.Lerp(
                0.55f, 1.2f, approachAlignment);
            float collisionMagnitude =
                speedMagnitude * approachMultiplier;

            Vector3 planarDefenderForward = defenderForward;
            planarDefenderForward.y = 0f;
            float facingMultiplier = 1f;
            if (planarDefenderForward.sqrMagnitude > 0.0001f)
            {
                planarDefenderForward.Normalize();
                Vector3 directionToAttacker = -contactNormal;
                float frontAlignment = Vector3.Dot(
                    planarDefenderForward, directionToAttacker);

                // Front = 0.85x, side = 1.075x, rear = 1.30x.
                facingMultiplier = Mathf.Lerp(
                    1.3f,
                    0.85f,
                    (frontAlignment + 1f) * 0.5f);
            }

            return new CollisionImpactProfile(
                relativeSpeed,
                closingSpeed,
                approachAlignment,
                collisionMagnitude,
                facingMultiplier);
        }

        /// <summary>
        /// Handles a full collision exchange between two Beys.
        /// </summary>
        public static void HandleCollision(
            BeyConfiguration attackerConfig,
            BeyConfiguration defenderConfig,
            BeyStatBlock attackerStats,
            BeyStatBlock defenderStats,
            Vector3 attackerVelocity,
            Vector3 defenderVelocity,
            Vector3 attackerToDefenderNormal,
            Vector3 attackerForward,
            Vector3 defenderForward,
            float spinExchangeMultiplier = 1f)
        {
            CollisionImpactProfile attackerImpact = EvaluateImpact(
                attackerVelocity,
                defenderVelocity,
                attackerToDefenderNormal,
                defenderForward);
            CollisionImpactProfile counterImpact = EvaluateImpact(
                defenderVelocity,
                attackerVelocity,
                -attackerToDefenderNormal,
                attackerForward);

            float spinDamageToDefender = CalculateSpinDamage(
                attackerStats,
                defenderStats,
                attackerImpact.CollisionMagnitude,
                attackerVelocity.magnitude,
                attackerImpact.DefenderFacingMultiplier)
                * spinExchangeMultiplier;

            float spinDamageToAttacker = CalculateSpinDamage(
                defenderStats,
                attackerStats,
                counterImpact.CollisionMagnitude,
                defenderVelocity.magnitude,
                counterImpact.DefenderFacingMultiplier)
                * 0.35f
                * spinExchangeMultiplier;

            spinDamageToDefender =
                attackerConfig.ModifyOutgoingCollisionDamage(
                    defenderConfig, spinDamageToDefender);
            spinDamageToAttacker =
                defenderConfig.ModifyOutgoingCollisionDamage(
                    attackerConfig, spinDamageToAttacker);

            spinDamageToDefender =
                defenderConfig.ApplyCollisionSpinDamage(
                    attackerConfig, spinDamageToDefender);
            spinDamageToAttacker =
                attackerConfig.ApplyCollisionSpinDamage(
                    defenderConfig, spinDamageToAttacker);

            string defenderName =
                defenderConfig.IsEnemy ? "Enemy" : "Player";
            string attackerName =
                attackerConfig.IsEnemy ? "Enemy" : "Player";
            Debug.Log(
                $"[Damage] {defenderName} took <color=red>" +
                $"-{spinDamageToDefender:F1}</color> spin -> " +
                $"{defenderConfig.CurrentSpin:F1} remaining");
            Debug.Log(
                $"[Damage] {attackerName} took <color=red>" +
                $"-{spinDamageToAttacker:F1}</color> spin -> " +
                $"{attackerConfig.CurrentSpin:F1} remaining");
            Debug.Log(
                $"[Impact] relative={attackerImpact.RelativeSpeed:F1} " +
                $"closing={attackerImpact.ClosingSpeed:F1} " +
                $"alignment={attackerImpact.ApproachAlignment:F2} " +
                $"magnitude={attackerImpact.CollisionMagnitude:F2} " +
                $"facing={attackerImpact.DefenderFacingMultiplier:F2} " +
                $"attack={attackerStats.Attack:F0} " +
                $"defense={defenderStats.Defense:F0}");
        }

        /// <summary>
        /// Requires real motion into the contact plane. Fast parallel movement is a
        /// graze rather than a full collision merely because relative speed is high.
        /// </summary>
        public static bool ShouldExchangeSpin(float collisionClosingSpeed)
        {
            return collisionClosingSpeed > 2f;
        }
    }
}
