using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Gameplay.Movement
{
    /// <summary>
    /// Central controller for Beyblade movement using momentum-based "ice skating" physics.
    /// Force builds gradually in one direction. Changing direction requires decaying old
    /// momentum before new momentum can fully take over — like ice skating or drifting.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BeyMovementController : MonoBehaviour
    {
        [SerializeField]
        private BeyConfiguration beyConfiguration;

        [SerializeField]
        private float turnSpeed = GameConstants.BASE_TURN_SPEED;

        [SerializeField]
        private bool debugMovement = false;

        private Rigidbody rb;
        private ITipBehavior activeTipBehavior;
        private float boost = 1f;
        private bool isGrounded = true;
        private float groundCheckDistance = 0.1f;
        private LayerMask groundLayer;

        // Input caching
        private float cachedForwardInput = 0f;
        private float cachedSteeringInput = 0f;

        // Inertia: weight affects how quickly the Bey can change direction
        private float currentWeight = GameConstants.MIN_WEIGHT;

        // === Momentum / Ice-Skating System ===
        // Force direction always tracks the camera instantly.
        // momentumStrength (0→1) ramps up while W is held and decays when released.
        // The ice-skating feel comes from Rigidbody velocity persisting in the old direction
        // while the new force gradually overcomes it — just like real skating physics.
        private float momentumStrength = 0f;

        // How quickly momentum strength builds when holding W (per second, 0→1 scale)
        // Lighter beys ramp up faster, heavier ones slower.
        private const float BASE_MOMENTUM_BUILDUP = 2.2f;

        // How quickly momentum strength decays when NOT pressing W (per second)
        private const float BASE_MOMENTUM_DECAY = 2.5f;

        // Extra linear damping added on top of tip damping to bleed off old-direction velocity.
        // This is what makes the bey "slow down" in the old direction when you redirect.
        private const float ICE_SKATE_DAMPING_BONUS = 0.8f;

        // Cached for gizmo drawing
        private Vector3 lastAppliedForce = Vector3.zero;
        private Vector3 lastForceDirection = Vector3.zero;

        // When changing direction sharply while at speed, reduce force effectiveness.
        // This makes it harder to instantly overpower existing velocity.
        private const float DIRECTION_CHANGE_PENALTY = 0.4f;

        // How quickly momentum fades while airborne (per second)
        private const float AIRBORNE_MOMENTUM_DECAY = 1.0f;
        private const float STEERING_FORCE_MULTIPLIER = 1.85f;

        // BeyModel child transform — this is what tilts/spins, not the root
        [SerializeField]
        private Transform beyModelTransform;

        // After jumping, ignore ground contact for this duration so OnCollisionStay
        // can't immediately re-ground the bey before it lifts off.
        private float jumpGraceTimer = 0f;
        private const float JUMP_GRACE_DURATION = 0.15f; // seconds

        // Orbital movement tracking (for OrbitTip)
        private bool isOrbiting = false;
        private Vector3 orbitCenter = Vector3.zero;
        private float orbitRadius = 5f;
        private float orbitAngle = 0f;

        // --- AI direction override ---
        // When set, ApplyForwardForce/ApplySteeringInput use this instead of Camera.main.
        // Null = use camera (player mode). Set by EnemyBeyController.
        private Vector3? overrideForwardDirection = null;
        private Vector3? overrideRightDirection = null;

        // True if this bey belongs to an enemy (auto-detected in Start)
        private bool isEnemy = false;

        // === Knockback hitstun ===
        // Brief window after being knocked back where movement forces are suppressed,
        // so the impulse can actually push the bey instead of being instantly counteracted.
        private float knockbackStunTimer = 0f;
        private const float KNOCKBACK_STUN_DURATION = 0.25f; // seconds

        /// <summary>
        /// Sets the desired forward/right directions for AI-driven beys.
        /// Pass null to revert to camera-based direction (player mode).
        /// </summary>
        public void SetDirectionOverride(Vector3? forward, Vector3? right = null)
        {
            overrideForwardDirection = forward;
            overrideRightDirection = right;
        }

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            isEnemy = GetComponent<EnemyBeyController>() != null;
            groundLayer = LayerMask.GetMask("Ground");

            if (rb != null)
            {
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            
            // Add bouncy physics material for realistic Beyblade landing
            SphereCollider sphereCol = GetComponent<SphereCollider>();
            if (sphereCol != null && !sphereCol.isTrigger && sphereCol.sharedMaterial == null)
            {
                PhysicsMaterial bouncyMat = new PhysicsMaterial("BeyBounce");
                bouncyMat.bounciness = 0.02f;
                bouncyMat.dynamicFriction = 0f; // Zero friction to reduce collision sticking
                bouncyMat.staticFriction = 0f;
                bouncyMat.frictionCombine = PhysicsMaterialCombine.Minimum;
                bouncyMat.bounceCombine = PhysicsMaterialCombine.Minimum;
                sphereCol.material = bouncyMat;
            }
            
            if (debugMovement)
            {
                Debug.Log($"[BeyMovement] START - Rigidbody: {(rb != null)}, GroundLayer: {groundLayer}");
                if (groundLayer == 0)
                    Debug.LogWarning("[BeyMovement] ⚠️  'Ground' layer not found! Will raycast all layers instead");
                
                // List all objects in scene with colliders
                Collider[] allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
                Debug.Log($"[BeyMovement] Found {allColliders.Length} colliders in scene");
                foreach (var col in allColliders)
                {
                    Debug.Log($"  - {col.gameObject.name} at Y={col.transform.position.y}, Layer: {LayerMask.LayerToName(col.gameObject.layer)}");
                }
            }

            UpdateActiveTipBehavior();

            if (beyConfiguration != null)
            {
                beyConfiguration.OnSpinChanged += OnSpinChanged;
            }
        }

        private void OnDestroy()
        {
            if (beyConfiguration != null)
            {
                beyConfiguration.OnSpinChanged -= OnSpinChanged;
            }
        }

        private void Update()
        {
            // Don't override collision-based grounding with raycasts
            // OnCollisionStay/OnCollisionExit handle ground detection
            
            if (debugMovement && !isGrounded)
                Debug.LogWarning($"[BeyMovement] NOT GROUNDED - Position: {transform.position}");
        }

        private void OnCollisionStay(Collision collision)
        {
            // Don't re-ground during jump grace period
            if (jumpGraceTimer > 0f)
                return;

            // Check contact normals — if any contact points upward (within ~60° of vertical),
            // we're on a walkable surface. This works on flat ground, slopes, and bowl walls.
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f) // ~60° from horizontal or flatter
                {
                    isGrounded = true;

                    if (rb != null && rb.linearVelocity.y > 1.2f)
                    {
                        Vector3 velocity = rb.linearVelocity;
                        velocity.y = 1.2f;
                        rb.linearVelocity = velocity;
                    }

                    if (debugMovement)
                        Debug.Log($"[BeyMovement] GROUNDED via collision: {collision.gameObject.name}, normal: {contact.normal}");
                    return;
                }
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            // When leaving any collider, do a quick spherecast down to verify we're truly airborne.
            // This prevents false negatives when sliding between adjacent colliders (bowl + wall).
            if (!Physics.SphereCast(transform.position, 0.15f, Vector3.down, out _, groundCheckDistance + 0.2f))
            {
                isGrounded = false;
                if (debugMovement)
                    Debug.LogWarning($"[BeyMovement] LEFT GROUND: {collision.gameObject.name}");
            }
        }

        private void FixedUpdate()
        {
            if (beyConfiguration == null || activeTipBehavior == null)
                return;

            // Update Rigidbody mass from weight stat for realistic inertia
            BeyStatBlock currentStats = beyConfiguration.GetStatBlock();
            currentWeight = currentStats.Weight;
            rb.mass = Mathf.Lerp(1f, 4f, (currentWeight - GameConstants.MIN_WEIGHT) / (GameConstants.MAX_WEIGHT - GameConstants.MIN_WEIGHT));

            activeTipBehavior.ApplyPhysicsModifiers(rb);

            // Tick down jump grace timer
            if (jumpGraceTimer > 0f)
                jumpGraceTimer -= Time.fixedDeltaTime;

            // Tick down knockback stun timer
            if (knockbackStunTimer > 0f)
                knockbackStunTimer -= Time.fixedDeltaTime;

            // Keep Bey grounded with strong downward force (like a spinning top)
            if (isGrounded)
            {
                // Apply continuous downward pressure to keep Bey on ground
                rb.AddForce(Vector3.down * 50f, ForceMode.Acceleration);
                // Tilt is handled by BeyTiltController in LateUpdate (on TiltPivot transform)
            }

            // Skip movement forces during knockback stun — let the impulse carry the bey
            if (knockbackStunTimer > 0f)
            {
                // Still drain spin during stun; boost now consumes mana instead of extra spin.
                beyConfiguration.DrainSpin(Time.fixedDeltaTime, 1f);
                if (boost > 1f)
                    DrainBoostMana(Time.fixedDeltaTime);
                else
                    beyConfiguration.RegenMana(Time.fixedDeltaTime);
                if (beyConfiguration.IsBurst) OnBurst();
                return;
            }

            if (cachedForwardInput != 0)
            {
                if (debugMovement)
                    Debug.Log($"[BeyMovement] FORWARD INPUT: {cachedForwardInput:F2}, GROUNDED: {isGrounded}");
                
                activeTipBehavior.ApplyMovement(this, cachedForwardInput);
            }
            else
            {
                // No forward input — decay momentum strength (coast/slow down)
                DecayMomentum();
            }

            if (cachedSteeringInput != 0)
            {
                ApplySteeringInput(cachedSteeringInput);
            }

            beyConfiguration.DrainSpin(Time.fixedDeltaTime, 1f);

            if (boost > 1f)
                DrainBoostMana(Time.fixedDeltaTime);
            else
                beyConfiguration.RegenMana(Time.fixedDeltaTime);

            if (beyConfiguration.IsBurst)
            {
                OnBurst();
            }
        }

        public void CacheInput(float forwardInput, float steeringInput)
        {
            cachedForwardInput = forwardInput;
            cachedSteeringInput = steeringInput;

            if (debugMovement && (forwardInput != 0 || steeringInput != 0))
                Debug.Log($"[BeyMovement] INPUT CACHED - Forward: {forwardInput:F2}, Steering: {steeringInput:F2}");
        }

        /// <summary>
        /// Called by the active ITipBehavior via ApplyMovement.
        /// Force direction always follows the camera instantly.
        /// Momentum strength ramps up over time — the ice-skating feel comes from
        /// the Rigidbody's existing velocity persisting in the old direction while
        /// new force pushes in the camera direction.
        /// </summary>
        public void ApplyForwardForce(float forceAmount)
        {
            if (rb == null)
            {
                if (debugMovement)
                    Debug.LogError("[BeyMovement] Rigidbody is NULL!");
                return;
            }

            if (beyConfiguration == null)
            {
                if (debugMovement)
                    Debug.LogError("[BeyMovement] BeyConfiguration is NULL!");
                return;
            }

            // Weight factor: 0 = lightest, 1 = heaviest
            float weightFactor = (currentWeight - GameConstants.MIN_WEIGHT) / (GameConstants.MAX_WEIGHT - GameConstants.MIN_WEIGHT);

            // Tip force multiplier extracted from the forceAmount
            float tipScale = Mathf.Abs(forceAmount) / GameConstants.BASE_FORWARD_FORCE;

            // --- Always ramp up momentum strength while W is held, even when airborne ---
            // This way when you land you have full force ready to go.
            float gmAccel = GameManager.GetForBey(isEnemy, g => g.accelerationMultiplier, g => g.enemyAccelerationMultiplier);
            float buildupRate = BASE_MOMENTUM_BUILDUP * tipScale * Mathf.Lerp(1.3f, 0.6f, weightFactor) * gmAccel;
            momentumStrength = Mathf.Min(momentumStrength + buildupRate * Time.fixedDeltaTime, 1f);

            // While airborne: gradually fade momentum instead of instant zero.
            // Rigidbody velocity carries the bey naturally; force bleeds off over time.
            // AI-controlled beys (direction override set) still get reduced force so they
            // don't freeze in the air waiting for grounding.
            if (!isGrounded)
            {
                if (overrideForwardDirection.HasValue)
                {
                    // AI: apply reduced force so the enemy can still move to collide with ground
                    momentumStrength = Mathf.Max(0.3f, momentumStrength - AIRBORNE_MOMENTUM_DECAY * Time.fixedDeltaTime);
                    // fall through to apply force below
                }
                else
                {
                    momentumStrength = Mathf.Max(0f, momentumStrength - AIRBORNE_MOMENTUM_DECAY * Time.fixedDeltaTime);
                    lastAppliedForce = Vector3.zero;
                    lastForceDirection = Vector3.zero;
                    if (debugMovement)
                        Debug.LogWarning("[BeyMovement] NOT GROUNDED - momentum zeroed, velocity carries bey");
                    return;
                }
            }

            BeyStatBlock stats = beyConfiguration.GetStatBlock();
            float uphillMultiplier = GetUphillResistanceMultiplier(stats);

            // Force direction: use override (AI) or camera (player)
            Vector3 desiredDirection;
            if (overrideForwardDirection.HasValue)
            {
                desiredDirection = overrideForwardDirection.Value;
            }
            else
            {
                Camera mainCamera = Camera.main;
                desiredDirection = mainCamera != null
                    ? mainCamera.transform.forward
                    : transform.forward;
            }
            desiredDirection.y = 0;
            desiredDirection.Normalize();

            float inputSign = Mathf.Sign(forceAmount);
            Vector3 forceDirection = desiredDirection * inputSign;

            // --- Direction change penalty ---
            // If current velocity is going one way and we're pushing another,
            // reduce force effectiveness so you can't instantly overpower old momentum.
            Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            float speed = currentHorizontalVel.magnitude;
            float directionEffectiveness = 1f;

            if (speed > 1f)
            {
                float dot = Vector3.Dot(currentHorizontalVel.normalized, forceDirection);
                // dot: +1 = same dir (full force), 0 = perpendicular, -1 = opposite
                // effectiveness: 1.0 when aligned, drops to DIRECTION_CHANGE_PENALTY when opposing
                directionEffectiveness = Mathf.Lerp(DIRECTION_CHANGE_PENALTY, 1f, (dot + 1f) * 0.5f);
            }

            // --- Apply force in camera direction, scaled by ramped momentum ---
            float gmSpeed = GameManager.GetForBey(isEnemy, g => g.speedMultiplier, g => g.enemySpeedMultiplier);
            Vector3 appliedForce = forceDirection * GameConstants.BASE_FORWARD_FORCE * tipScale
                * momentumStrength * directionEffectiveness * boost * uphillMultiplier * gmSpeed;
            rb.AddForce(appliedForce, ForceMode.Force);

            lastAppliedForce = appliedForce;
            lastForceDirection = forceDirection;

            // --- Add extra damping to bleed off velocity in the old direction ---
            // This is the key to the ice-skating redirect: old velocity fades while new builds
            rb.linearDamping = Mathf.Max(rb.linearDamping, rb.linearDamping + ICE_SKATE_DAMPING_BONUS * (1f - Mathf.Max(0f, Vector3.Dot(currentHorizontalVel.normalized, forceDirection))));

            if (debugMovement)
                Debug.Log($"[BeyMovement] MOMENTUM: strength={momentumStrength:F2}, dir={forceDirection}, effectiveness={directionEffectiveness:F2}, force={appliedForce.magnitude:F1}, vel={speed:F1}");
        }

        /// <summary>
        /// Decays momentum strength when the player isn't pressing W.
        /// The Rigidbody velocity still carries the bey forward (coasting),
        /// but next time they press W the ramp starts lower.
        /// </summary>
        private void DecayMomentum()
        {
            if (momentumStrength <= 0f)
                return;

            float weightFactor = (currentWeight - GameConstants.MIN_WEIGHT) / (GameConstants.MAX_WEIGHT - GameConstants.MIN_WEIGHT);
            // Heavier beys hold momentum strength longer, lighter ones shed it faster
            float decayRate = BASE_MOMENTUM_DECAY * Mathf.Lerp(1.2f, 0.6f, weightFactor);

            momentumStrength = Mathf.Max(0f, momentumStrength - decayRate * Time.fixedDeltaTime);
        }

        public void ApplySteeringInput(float steeringInput)
        {
            if (beyConfiguration == null || rb == null || !isGrounded)
                return;

            // Steering applies sideways force relative to the camera.
            // At speed, this creates a curving arc (like leaning on ice skates).
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            float speed = horizontalVelocity.magnitude;
            
            if (speed < 0.5f)
                return; // No steering when stationary

            Vector3 sideDirection;
            if (overrideRightDirection.HasValue)
            {
                sideDirection = overrideRightDirection.Value;
            }
            else
            {
                Camera mainCamera = Camera.main;
                sideDirection = mainCamera != null
                    ? mainCamera.transform.right
                    : transform.right;
            }
            sideDirection.y = 0;
            sideDirection.Normalize();

            // Weight affects how easily you can steer (lighter = more agile)
            float weightFactor = (currentWeight - GameConstants.MIN_WEIGHT) / (GameConstants.MAX_WEIGHT - GameConstants.MIN_WEIGHT);
            float steerStrength = Mathf.Lerp(0.4f, 0.15f, weightFactor);

            // Sideways force scales with speed — faster = wider arcs
            float gmTurn = GameManager.GetForBey(isEnemy, g => g.turnSpeedMultiplier, g => g.enemyTurnSpeedMultiplier);
            float sideForce = steeringInput * speed * steerStrength * rb.mass * gmTurn * STEERING_FORCE_MULTIPLIER;
            rb.AddForce(sideDirection * sideForce, ForceMode.Force);
        }

        public void StartBoost()
        {
            if (beyConfiguration == null || beyConfiguration.CurrentMana <= GameConstants.MIN_MANA)
            {
                StopBoost();
                return;
            }

            float gmBoost = GameManager.GetForBey(isEnemy, g => g.boostMultiplier, g => g.enemyBoostMultiplier);
            boost = GameConstants.BOOST_FORCE_MULTIPLIER * gmBoost;
            if (debugMovement)
                Debug.Log("[BeyMovement] BOOST ON");
        }

        public void StopBoost()
        {
            boost = 1f;
            if (debugMovement)
                Debug.Log("[BeyMovement] BOOST OFF");
        }

        private void DrainBoostMana(float deltaTime)
        {
            if (beyConfiguration == null)
                return;

            float manaDrain = GameConstants.BOOST_MANA_DRAIN_PER_SECOND * deltaTime;
            beyConfiguration.SetMana(beyConfiguration.CurrentMana - manaDrain);

            if (beyConfiguration.CurrentMana <= GameConstants.MIN_MANA)
            {
                StopBoost();
            }
        }

        /// <summary>
        /// Applies an impulse knockback force. Used by BeyCollisionDetector on hit.
        /// Direction is the knockback direction (away from attacker).
        /// Strength is scaled by weight differential and collision speed.
        /// </summary>
        public void ApplyKnockback(Vector3 direction, float strength)
        {
            if (rb == null) return;
            float gmKnockback = GameManager.GetForBey(isEnemy, g => g.knockbackMultiplier, g => g.enemyKnockbackMultiplier);
            rb.AddForce(direction * strength * gmKnockback, ForceMode.Impulse);

            // Brief hitstun: suppress movement forces so the impulse actually moves the bey
            knockbackStunTimer = KNOCKBACK_STUN_DURATION;
            // Reset momentum so the bey has to rebuild speed after being hit
            momentumStrength = 0f;

            Debug.Log($"[BeyMovement] KNOCKBACK on {gameObject.name}  dir={direction:F2}  str={strength:F1}  force={strength * gmKnockback:F1}");
        }

        public void ApplyBrake(float brakeStrength = 0.5f)
        {
            if (rb != null)
                rb.linearVelocity *= (1f - brakeStrength);
            
            // Also kill momentum strength so the bey doesn't keep pushing after braking
            momentumStrength *= (1f - brakeStrength);
        }

        public void Jump()
        {
            if (!isGrounded || beyConfiguration == null)
                return;

            BeyStatBlock stats = beyConfiguration.GetStatBlock();
            float gmJump = GameManager.GetForBey(isEnemy, g => g.jumpMultiplier, g => g.enemyJumpMultiplier);
            float jumpForce = GameConstants.JUMP_FORCE * stats.JumpArcModifier * gmJump;
            
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            isGrounded = false;
            jumpGraceTimer = JUMP_GRACE_DURATION;

            if (!isEnemy)
            {
                MatchManager match = FindFirstObjectByType<MatchManager>();
                match?.NotifyPlayerJump();
            }
            
            if (debugMovement)
                Debug.Log("[BeyMovement] JUMP");
        }

        public void ApplyOrbitMovement(Vector3 center, float radius, float speed)
        {
            if (!isGrounded)
                return;

            isOrbiting = true;
            orbitCenter = center;
            orbitRadius = radius;

            float radiansPerSecond = speed / radius;
            orbitAngle += radiansPerSecond * Time.fixedDeltaTime;

            Vector3 orbitPosition = orbitCenter + new Vector3(
                Mathf.Cos(orbitAngle) * radius,
                0,
                Mathf.Sin(orbitAngle) * radius
            );

            Vector3 directionToOrbit = (orbitPosition - transform.position).normalized;
            rb.linearVelocity = directionToOrbit * speed;

            transform.LookAt(transform.position + directionToOrbit);
        }

        private void CheckGrounded()
        {
            RaycastHit hit;
            bool wasGrounded = isGrounded;
            
            // If groundLayer is 0 (layer not found), raycast without layer mask to find ANY collider
            if (groundLayer == 0)
            {
                isGrounded = Physics.Raycast(
                    transform.position,
                    Vector3.down,
                    out hit,
                    groundCheckDistance
                );
                
                if (debugMovement)
                    Debug.LogWarning($"[BeyMovement] Ground layer not found! Raycasting all layers. Hit: {hit.collider?.name}");
            }
            else
            {
                isGrounded = Physics.Raycast(
                    transform.position,
                    Vector3.down,
                    out hit,
                    groundCheckDistance,
                    groundLayer
                );
            }

            if (debugMovement && wasGrounded != isGrounded)
            {
                Debug.Log($"[BeyMovement] GROUNDED CHANGED: {isGrounded}, Hit: {hit.collider?.name}, Distance: {hit.distance:F2}");
            }
        }

        private float GetUphillResistanceMultiplier(BeyStatBlock stats)
        {
            RaycastHit hit;
            if (Physics.Raycast(
                transform.position,
                Vector3.down,
                out hit,
                GameConstants.UPHILL_CHECK_DISTANCE,
                groundLayer
            ))
            {
                float uphillAngle = Vector3.Angle(hit.normal, transform.forward);
                
                if (uphillAngle > 90f)
                {
                    float angleAdjustment = (uphillAngle - 90f) / 90f;
                    float resistance = Mathf.Lerp(1f, stats.UphillResistanceMultiplier, angleAdjustment);
                    return resistance * stats.SlopeMultiplier;
                }
            }

            return 1f;
        }

        private void OnSpinChanged(float newSpin)
        {
            UpdateActiveTipBehavior();
        }

        private void UpdateActiveTipBehavior()
        {
            if (beyConfiguration == null)
                return;

            TipBehaviorType activeBehaviorType = beyConfiguration.GetActiveTipBehavior();
            activeTipBehavior = TipBehaviorFactory.CreateTipBehavior(activeBehaviorType);
        }

        private void OnBurst()
        {
            // Enemy burst is handled by MatchManager → EnemyBeyController.OnBurst()
            if (GetComponent<EnemyBeyController>() != null)
                return;

            // Player burst — trigger part-detach effect
            var burstEffect = GetComponent<Effects.BeyBurstEffect>();
            if (burstEffect == null)
                burstEffect = gameObject.AddComponent<Effects.BeyBurstEffect>();

            burstEffect.TriggerBurst();
        }

        public Rigidbody Rb => rb;
        public bool IsGrounded => isGrounded;
        public Vector3 CurrentVelocity => rb.linearVelocity;
        public float CurrentHorizontalSpeed => rb != null
            ? new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude
            : 0f;
        public float MomentumStrength => momentumStrength;
        public float CurrentBoostMultiplier => boost;
        public ITipBehavior ActiveTipBehavior => activeTipBehavior;
        public BeyConfiguration BeyConfiguration => beyConfiguration;

        private void OnDrawGizmos()
        {
            Rigidbody gizmoRb = rb != null ? rb : GetComponent<Rigidbody>();
            if (gizmoRb == null)
                return;

            Vector3 origin = transform.position - Vector3.up * 0.25f;

            // Green arrow: current velocity (what the bey is actually doing)
            Vector3 horizontalVel = new Vector3(gizmoRb.linearVelocity.x, 0, gizmoRb.linearVelocity.z);
            if (horizontalVel.magnitude > 0.1f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(origin, horizontalVel.normalized * Mathf.Min(horizontalVel.magnitude * 0.3f, 5f));
            }

            // Yellow arrow: camera-facing force direction (where the player wants to go)
            if (lastForceDirection.sqrMagnitude > 0.001f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(origin, lastForceDirection * 3f);
            }

            // Red arrow: actual applied force (direction + magnitude, scaled for visibility)
            if (lastAppliedForce.sqrMagnitude > 0.1f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(origin, lastAppliedForce.normalized * Mathf.Min(lastAppliedForce.magnitude * 0.02f, 5f));
            }

            // Cyan sphere at origin: size shows momentum strength (0→1)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, 0.1f + momentumStrength * 0.5f);

            // ── Stat ring gizmos ──────────────────────────────────────
            // Three concentric arc rings above the bey showing spin, mana, speed.
            // Full ring = max value; partial arc = current fraction.
            DrawStatRings(origin, gizmoRb);
        }

        // -------------------------------------------------------------------
        // Stat ring gizmos — three flat wire-arc rings floating above the bey
        // -------------------------------------------------------------------
        private const int RING_RESOLUTION = 48;
        private const float RING_Y_OFFSET = 0.5f;        // raised above bey
        private const float RING_GAP = 0.18f;            // radial gap between rings

        private void DrawStatRings(Vector3 origin, Rigidbody gizmoRb)
        {
            Vector3 center = origin + Vector3.up * RING_Y_OFFSET;

            // --- Spin (green-yellow) ---
            float spinFrac = 0f;
            float manaFrac = 0f;
            float maxSpeed = 30f; // reference speed for full ring
            float speedFrac = 0f;

            if (beyConfiguration != null)
            {
                spinFrac = Mathf.Clamp01(beyConfiguration.CurrentSpin / GameConstants.MAX_SPIN);
                float manaPool = beyConfiguration.GetStatBlock().ManaPoolSize;
                manaFrac = manaPool > 0 ? Mathf.Clamp01(beyConfiguration.CurrentMana / manaPool) : 0f;
            }

            Vector3 vel = gizmoRb != null ? gizmoRb.linearVelocity : Vector3.zero;
            float speed = new Vector3(vel.x, 0, vel.z).magnitude;
            speedFrac = Mathf.Clamp01(speed / maxSpeed);

            float r0 = 0.45f;                      // innermost ring radius
            float r1 = r0 + RING_GAP;
            float r2 = r1 + RING_GAP;

            // Spin ring: green when healthy, lerps to red as it drops
            Color spinColor = Color.Lerp(Color.red, Color.green, spinFrac);
            DrawArcRing(center, r0, spinFrac, spinColor);
            DrawArcRingBackground(center, r0, new Color(spinColor.r, spinColor.g, spinColor.b, 0.15f));
            DrawRingLabel(center, r0, "Spin", spinColor);

            // Mana ring: blue
            Color manaColor = new Color(0.3f, 0.5f, 1f);
            DrawArcRing(center, r1, manaFrac, manaColor);
            DrawArcRingBackground(center, r1, new Color(0.3f, 0.5f, 1f, 0.15f));
            DrawRingLabel(center, r1, "Mana", manaColor);

            // Speed ring: magenta
            Color speedColor = new Color(1f, 0.3f, 0.9f);
            DrawArcRing(center, r2, speedFrac, speedColor);
            DrawArcRingBackground(center, r2, new Color(1f, 0.3f, 0.9f, 0.15f));
            DrawRingLabel(center, r2, "Speed", speedColor);
        }

        /// <summary>Draws a text label at the +X edge of a ring.</summary>
        private void DrawRingLabel(Vector3 center, float radius, string text, Color color)
        {
#if UNITY_EDITOR
            GUIStyle style = new GUIStyle();
            style.normal.textColor = color;
            style.fontSize = 11;
            style.fontStyle = FontStyle.Bold;
            Vector3 labelPos = center + new Vector3(radius + 0.06f, 0f, 0f);
            UnityEditor.Handles.Label(labelPos, text, style);
#endif
        }

        /// <summary>Draws a filled arc (fraction 0–1) as a series of line segments on the XZ plane.</summary>
        private void DrawArcRing(Vector3 center, float radius, float fraction, Color color)
        {
            if (fraction <= 0f) return;
            Gizmos.color = color;
            int segs = Mathf.Max(1, Mathf.CeilToInt(RING_RESOLUTION * fraction));
            float totalAngle = fraction * Mathf.PI * 2f;

            Vector3 prev = center + new Vector3(Mathf.Sin(0) * radius, 0, Mathf.Cos(0) * radius);
            for (int i = 1; i <= segs; i++)
            {
                float angle = (float)i / segs * totalAngle;
                Vector3 next = center + new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }

        /// <summary>Draws a full faint ring as background reference.</summary>
        private void DrawArcRingBackground(Vector3 center, float radius, Color color)
        {
            Gizmos.color = color;
            Vector3 prev = center + new Vector3(0, 0, radius);
            for (int i = 1; i <= RING_RESOLUTION; i++)
            {
                float angle = (float)i / RING_RESOLUTION * Mathf.PI * 2f;
                Vector3 next = center + new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
