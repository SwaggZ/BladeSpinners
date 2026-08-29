using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Effects;
using BladeSpinners.Audio;

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

        [Header("Temporary Hit VFX")]
        [SerializeField]
        private bool spawnPlaceholderHitParticle = true;

        [SerializeField]
        private Color placeholderHitColor = new Color(1f, 0.78f, 0.2f, 1f);

        private float lastCollisionTime = -1f;
        private float lastWallCollisionTime = -1f;

        /// <summary>
        /// Called when this Bey collides with another.
        /// </summary>
        public event System.Action<BeyCollisionDetector> OnCollisionWithBey;

        private void OnTriggerEnter(Collider other)
        {
            TryProcessCollision(other.GetComponentInParent<BeyCollisionDetector>());
        }

        private void OnCollisionEnter(Collision collision)
        {
            BeyCollisionDetector otherBey =
                collision.collider.GetComponentInParent<BeyCollisionDetector>();
            if (otherBey != null)
                TryProcessCollision(otherBey);
            else
                TryPlayWallCollision(collision);
        }

        private void TryProcessCollision(BeyCollisionDetector otherBeyCollider)
        {
            // Skip if either bey is invalid/dead
            if (otherBeyCollider == null || otherBeyCollider == this)
                return;

            if (beyConfiguration != null && beyConfiguration.IsBurst)
                return;

            if (otherBeyCollider.beyConfiguration != null && otherBeyCollider.beyConfiguration.IsBurst)
                return;

            // Check collision cooldown
            if (Time.time - lastCollisionTime < collisionCooldown)
                return;

            // Deduplication: process only on lower instance id
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
            float thisSpinBefore = beyConfiguration.CurrentSpin;
            float otherSpinBefore =
                otherBey.beyConfiguration.CurrentSpin;
            Vector3 thisVelocity = movementController.CurrentVelocity;
            Vector3 otherVelocity = otherBey.movementController.CurrentVelocity;

            // Trigger contacts do not provide a ContactPoint, so derive the planar
            // contact normal from the two Bey roots.
            Vector3 thisToOther =
                otherBey.transform.position - transform.position;
            thisToOther.y = 0f;
            if (thisToOther.sqrMagnitude < 0.0001f)
            {
                thisToOther = thisVelocity - otherVelocity;
                thisToOther.y = 0f;
            }
            if (thisToOther.sqrMagnitude < 0.0001f)
                thisToOther = transform.forward;
            thisToOther.Normalize();

            float relativeVelocity = Vector3.Distance(thisVelocity, otherVelocity);
            Vector3 hitPosition = (transform.position + otherBey.transform.position) * 0.5f;

            // Check if this head-on collision triggers a dramatic Blade Lock Clash Duel
            BladeLockDuelManager duelMgr = BladeLockDuelManager.EnsureInstance();
            if (duelMgr != null && duelMgr.TryTriggerBladeLock(this, otherBey, hitPosition, thisToOther))
            {
                lastCollisionTime = Time.time;
                otherBey.lastCollisionTime = Time.time;
                return;
            }

            if (relativeVelocity >= 2f)
            {
                float hitIntensity = Mathf.Lerp(
                    0.3f, 1f, Mathf.InverseLerp(2f, 22f, relativeVelocity));
                SoundManager.PlayBeyHit(hitPosition, hitIntensity);
            }

            // Only exchange spin if collision is meaningful
            // Use attacker's enemy status for the spin exchange multiplier
            float gmSpinExchangeThis = GameManager.GetForBey(beyConfiguration.IsEnemy,
                g => g.spinExchangeMultiplier, g => g.enemySpinExchangeMultiplier);
            float gmSpinExchangeOther = GameManager.GetForBey(otherBey.beyConfiguration.IsEnemy,
                g => g.spinExchangeMultiplier, g => g.enemySpinExchangeMultiplier);
            float thisClosingContribution = Mathf.Max(
                0f, Vector3.Dot(thisVelocity, thisToOther));
            float otherClosingContribution = Mathf.Max(
                0f, Vector3.Dot(otherVelocity, -thisToOther));
            float closingSpeed = Mathf.Max(
                0f,
                Vector3.Dot(
                    thisVelocity - otherVelocity,
                    thisToOther));

            if (SpinExchangeHandler.ShouldExchangeSpin(closingSpeed))
            {
                // The Bey contributing more velocity into the contact is the attacker.
                // Fall back to total speed only for an exact contribution tie.
                bool thisIsAttacker =
                    thisClosingContribution > otherClosingContribution
                    || (Mathf.Approximately(
                            thisClosingContribution, otherClosingContribution)
                        && thisVelocity.sqrMagnitude
                            > otherVelocity.sqrMagnitude);

                if (thisIsAttacker)
                {
                    // This Bey is attacking
                    SpinExchangeHandler.HandleCollision(
                        beyConfiguration,
                        otherBey.beyConfiguration,
                        thisStats,
                        otherStats,
                        thisVelocity,
                        otherVelocity,
                        thisToOther,
                        transform.forward,
                        otherBey.transform.forward,
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
                        -thisToOther,
                        otherBey.transform.forward,
                        transform.forward,
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

            float relSpeed = relativeVelocity;
            float baseKnockback = GameConstants.COLLISION_KNOCKBACK_BASE;

            // Heavier bey knocks the lighter one harder.
            // weightRatio > 1 means this bey is heavier → takes less knockback.
            float thisWeight = thisStats.Weight;
            float otherWeight = otherStats.Weight;

            float knockbackOnThis  = baseKnockback * (otherWeight / Mathf.Max(thisWeight, 1f))
                                   * (1f + relSpeed * 0.05f);
            float knockbackOnOther = baseKnockback * (thisWeight / Mathf.Max(otherWeight, 1f))
                                   * (1f + relSpeed * 0.05f);

            if (beyConfiguration != null && beyConfiguration.HasShrinePerk(BladeSpinners.Gameplay.Shrine.ShrinePerkType.HeavyweightCore) && !beyConfiguration.IsEnemy)
            {
                knockbackOnOther *= 1.25f;
            }

            SpawnPlaceholderHitParticle(otherBey, relSpeed);

            // Screen shake scaled by impact speed
            ThirdPersonCameraController.TriggerScreenShake(Mathf.Clamp01(relSpeed / 20f) * 0.45f + 0.1f, 0.16f);

            // Comic popup on heavy hits
            if (relSpeed > 7f)
            {
                string[] clashPhrases = { "CLASH!", "CRITICAL RIP!", "SPIN IMPACT!", "SLAM!" };
                Color clashColor = relSpeed > 14f ? new Color(1f, 0.2f, 0.1f, 1f) : new Color(1f, 0.85f, 0.2f, 1f);
                float scale = relSpeed > 14f ? 1.4f : 1.0f;
                BladeSpinners.Gameplay.UI.RuntimeGameUiController.SpawnComicPopup(clashPhrases[Random.Range(0, clashPhrases.Length)], clashColor, scale);
            }

            // Blader Shrine Static Overload: Chain Lightning on Heavy Impacts
            if (relSpeed >= 6f && beyConfiguration != null && beyConfiguration.HasShrinePerk(BladeSpinners.Gameplay.Shrine.ShrinePerkType.StaticOverload) && !beyConfiguration.IsEnemy)
            {
                BeyMovementController[] allBeys = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);
                Vector3 myPos = transform.position;
                foreach (var b in allBeys)
                {
                    if (b != null && b != movementController && b.BeyConfiguration != null && b.BeyConfiguration.IsEnemy && !b.BeyConfiguration.IsBurst)
                    {
                        float dist = Vector3.Distance(myPos, b.transform.position);
                        if (dist <= 8.5f)
                        {
                            BladeSpinners.Abilities.EpicAbilityVFXHelper.SpawnLightningArc(myPos + Vector3.up * 0.3f, b.transform.position + Vector3.up * 0.3f, new Color(0.3f, 0.85f, 1f, 1f));
                            b.BeyConfiguration.SetSpin(b.BeyConfiguration.CurrentSpin - 12f);
                            BladeSpinners.Abilities.EpicAbilityVFXHelper.SpawnSparkBurst(b.transform.position, new Color(0.3f, 0.85f, 1f, 1f), 10);
                        }
                    }
                }
            }

            movementController.ApplyKnockback(knockDir, knockbackOnThis);
            otherBey.movementController.ApplyKnockback(-knockDir, knockbackOnOther);

            // Fire collision event
            OnCollisionWithBey?.Invoke(otherBey);
            otherBey.OnCollisionWithBey?.Invoke(this);

            float damageTakenByThis = Mathf.Max(
                0f, thisSpinBefore - beyConfiguration.CurrentSpin);
            float damageTakenByOther = Mathf.Max(
                0f,
                otherSpinBefore
                    - otherBey.beyConfiguration.CurrentSpin);
            beyConfiguration.NotifyBeyCollision(
                otherBey.beyConfiguration,
                damageTakenByOther,
                damageTakenByThis);
            otherBey.beyConfiguration.NotifyBeyCollision(
                beyConfiguration,
                damageTakenByThis,
                damageTakenByOther);

            // Immediately trigger burst if either bey just died from this hit.
            // Don't wait for MatchManager's next Update() — prevents ghost collisions.
            if (beyConfiguration != null && beyConfiguration.IsBurst)
            {
                BladeSpinners.Gameplay.UI.RuntimeGameUiController.SpawnComicPopup("BURST FINISH!!", new Color(1f, 0.15f, 0.4f, 1f), 1.6f);
            }
            else if (otherBey.beyConfiguration != null && otherBey.beyConfiguration.IsBurst)
            {
                BladeSpinners.Gameplay.UI.RuntimeGameUiController.SpawnComicPopup("BURST FINISH!!", new Color(1f, 0.85f, 0.1f, 1f), 1.6f);
            }

            TryImmediateBurst(beyConfiguration);
            TryImmediateBurst(otherBey.beyConfiguration);
        }

        private void TryPlayWallCollision(Collision collision)
        {
            if (collision == null
                || collision.contactCount == 0
                || Time.time - lastWallCollisionTime < 0.15f)
            {
                return;
            }

            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < 2.5f)
                return;

            // Arena floors and walls share the Ground layer, so use the contact plane:
            // floor contacts point mostly vertically, wall/rim contacts mostly sideways.
            ContactPoint wallContact = collision.GetContact(0);
            bool foundWallContact = false;
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint candidate = collision.GetContact(i);
                if (Mathf.Abs(candidate.normal.y) <= 0.6f)
                {
                    wallContact = candidate;
                    foundWallContact = true;
                    break;
                }
            }

            if (!foundWallContact)
                return;

            lastWallCollisionTime = Time.time;
            float intensity = Mathf.Lerp(
                0.25f, 1f, Mathf.InverseLerp(2.5f, 18f, impactSpeed));
            SoundManager.PlayWallHit(wallContact.point, intensity);
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

        private void SpawnPlaceholderHitParticle(BeyCollisionDetector otherBey, float relativeSpeed)
        {
            if (!spawnPlaceholderHitParticle || otherBey == null)
                return;

            Vector3 spawnPos = (transform.position + otherBey.transform.position) * 0.5f + Vector3.up * 0.15f;
            BeyHitImpactEffect.Spawn(spawnPos, placeholderHitColor, relativeSpeed);
        }
    }
}
