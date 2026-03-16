using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Gameplay.Combat
{
    /// <summary>
    /// Detects collisions with other Beyblades and other Beys and handles spin exchange.
    /// Attached to the Bey GameObject with a trigger collider dimension.
    /// </summary>
    public class BeyCollisionDetector : MonoBehaviour
    {
        [SerializeField]
        private BeyConfiguration beyConfiguration;

        [SerializeField]
        private BeyMovementController movementController;

        [SerializeField]
        private float collisionCooldown = 0.2f; // Prevent repeated collisions in same frame

        private float lastCollisionTime = -1f;

        /// <summary>
        /// Called when this Bey collides with another.
        /// </summary>
        public event System.Action<BeyCollisionDetector> OnCollisionWithBey;

        private void OnTriggerEnter(Collider other)
        {
            // Skip if either bey is already dead
            if (beyConfiguration != null && beyConfiguration.IsBurst) return;

            // Check collision cooldown
            if (Time.time - lastCollisionTime < collisionCooldown)
                return;

            // Check if other object is a Bey
            BeyCollisionDetector otherBeyCollider = other.GetComponent<BeyCollisionDetector>();
            if (otherBeyCollider == null)
                return;

            // Skip if other bey is dead
            if (otherBeyCollider.beyConfiguration != null && otherBeyCollider.beyConfiguration.IsBurst)
                return;

            // Deduplication: OnTriggerEnter fires on BOTH beys. Only the one
            // with the lower instance ID processes the collision to prevent
            // double damage/knockback.
            if (GetInstanceID() > otherBeyCollider.GetInstanceID())
                return;

            lastCollisionTime = Time.time;
            otherBeyCollider.lastCollisionTime = Time.time;

            HandleCollision(otherBeyCollider);
        }

        private void HandleCollision(BeyCollisionDetector otherBey)
        {
            // Guard against missing references (reflection-wired fields may be null)
            if (beyConfiguration == null || otherBey.beyConfiguration == null) return;
            if (movementController == null || otherBey.movementController == null) return;

            // Get stat blocks and velocities
            BeyStatBlock thisStats = beyConfiguration.GetStatBlock();
            BeyStatBlock otherStats = otherBey.beyConfiguration.GetStatBlock();
            Vector3 thisVelocity = movementController.CurrentVelocity;
            Vector3 otherVelocity = otherBey.movementController.CurrentVelocity;

            // Calculate relative velocity
            float relativeVelocity = Vector3.Distance(thisVelocity, otherVelocity);

            // Only exchange spin if collision is meaningful
            // Use attacker's enemy status for the spin exchange multiplier
            float gmSpinExchangeThis = GameManager.GetForBey(beyConfiguration.IsEnemy,
                g => g.spinExchangeMultiplier, g => g.enemySpinExchangeMultiplier);
            float gmSpinExchangeOther = GameManager.GetForBey(otherBey.beyConfiguration.IsEnemy,
                g => g.spinExchangeMultiplier, g => g.enemySpinExchangeMultiplier);
            if (SpinExchangeHandler.ShouldExchangeSpin(relativeVelocity))
            {
                // Determine who is attacking (higher velocity)
                if (thisVelocity.magnitude > otherVelocity.magnitude)
                {
                    // This Bey is attacking
                    SpinExchangeHandler.HandleCollision(
                        beyConfiguration,
                        otherBey.beyConfiguration,
                        thisStats,
                        otherStats,
                        thisVelocity,
                        otherVelocity,
                        gmSpinExchangeThis
                    );
                }
                else
                {
                    // Other Bey is attacking
                    SpinExchangeHandler.HandleCollision(
                        otherBey.beyConfiguration,
                        beyConfiguration,
                        otherStats,
                        thisStats,
                        otherVelocity,
                        thisVelocity,
                        gmSpinExchangeOther
                    );
                }
            }

            NotifyPlayerEnemyHit(otherBey);

            // ── Knockback ──────────────────────────────────────────
            // Direction: push each bey AWAY from the other.
            // Strength: base impulse scaled by weight ratio + speed.
            Vector3 knockDir = (transform.position - otherBey.transform.position).normalized;
            // Prevent purely vertical or zero knockback
            knockDir.y = 0f;
            if (knockDir.sqrMagnitude < 0.01f)
                knockDir = transform.forward;
            knockDir.Normalize();

            float relSpeed = Mathf.Max(thisVelocity.magnitude, otherVelocity.magnitude);
            float baseKnockback = GameConstants.COLLISION_KNOCKBACK_BASE;

            // Heavier bey knocks the lighter one harder.
            // weightRatio > 1 means this bey is heavier → takes less knockback.
            float thisWeight = thisStats.Weight;
            float otherWeight = otherStats.Weight;

            float knockbackOnThis  = baseKnockback * (otherWeight / Mathf.Max(thisWeight, 1f))
                                   * (1f + relSpeed * 0.05f);
            float knockbackOnOther = baseKnockback * (thisWeight / Mathf.Max(otherWeight, 1f))
                                   * (1f + relSpeed * 0.05f);

            movementController.ApplyKnockback(-knockDir, knockbackOnThis);
            otherBey.movementController.ApplyKnockback(knockDir, knockbackOnOther);

            // Fire collision event
            OnCollisionWithBey?.Invoke(otherBey);
            otherBey.OnCollisionWithBey?.Invoke(this);

            // Immediately trigger burst if either bey just died from this hit.
            // Don't wait for MatchManager's next Update() — prevents ghost collisions.
            TryImmediateBurst(beyConfiguration);
            TryImmediateBurst(otherBey.beyConfiguration);
        }

        private void NotifyPlayerEnemyHit(BeyCollisionDetector otherBey)
        {
            if (otherBey == null || beyConfiguration == null || otherBey.beyConfiguration == null)
                return;

            MatchManager match = FindFirstObjectByType<MatchManager>();
            if (match == null)
                return;

            // Case A: this is player, other is enemy.
            if (!beyConfiguration.IsEnemy && otherBey.beyConfiguration.IsEnemy)
            {
                match.NotifyPlayerHitByEnemy(otherBey.beyConfiguration, true);
                return;
            }

            // Case B: this is enemy, other is player.
            if (beyConfiguration.IsEnemy && !otherBey.beyConfiguration.IsEnemy)
            {
                match.NotifyPlayerHitByEnemy(beyConfiguration, true);
            }
        }

        /// <summary>
        /// If config just hit 0 spin, disable the collision detector immediately
        /// to prevent ghost hits before MatchManager's next Update() detects the burst.
        /// We don't trigger the full burst effect here — MatchManager handles that.
        /// </summary>
        private static void TryImmediateBurst(BeyConfiguration config)
        {
            if (config == null || !config.IsBurst) return;

            // Find ALL collision detectors and disable the one that owns this config.
            // We can't get from BeyConfiguration → GameObject directly, so scan all detectors.
            var detectors = FindObjectsByType<BeyCollisionDetector>(FindObjectsSortMode.None);
            foreach (var det in detectors)
            {
                if (det.beyConfiguration == config && det.enabled)
                {
                    det.enabled = false;
                    Debug.Log($"[BeyCollisionDetector] Disabled detector on {det.gameObject.name} (spin hit 0)");
                    break;
                }
            }
        }
    }
}
