using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Gameplay
{
    /// <summary>
    /// AI controller simulating player input (WASD, mouse aim, boost, ability).
    /// Dynamically scales intelligence, predictive intercept, flanking angles,
    /// tactical boosting, and ability execution based on arena run depth.
    /// </summary>
    public class AIInputHandler : MonoBehaviour
    {
        // ── AI States ────────────────────────────────────────────────
        public enum AIState { Chase, Attack, Reposition, Evade }

        [Header("AI Tuning")]
        [SerializeField] private float attackRange = 4.5f;
        [SerializeField] private float repositionRange = 11f;
        [SerializeField] private float stateChangeInterval = 0.6f;
        [SerializeField] private float obstacleDetectRange = 5.5f;
        [SerializeField] private float obstacleAvoidStrength = 1.6f;

        // ── References ───────────────────────────────────────────────
        [SerializeField]
        private BeyMovementController beyMovementController;

        [SerializeField]
        private BeyConfiguration beyConfiguration;

        // ── AI-specific ──────────────────────────────────────────────
        private Transform target;
        private Rigidbody rb;

        private AIState currentState = AIState.Chase;
        private float stateTimer;
        private Vector3 repositionTarget;

        private float currentForwardInput;
        private float currentSteeringInput;
        private bool isBoostActive;

        // ── Progression / Difficulty Scaling ─────────────────────────
        private float difficulty01 = 0.4f; // 0 = Rookie (depth 1), 1 = Master/Boss (depth 8+)
        private int enemyIndex = 0;
        private int totalEnemiesInMatch = 1;
        private float abilityCheckCooldown = 0f;

        // ── Public API ───────────────────────────────────────────────
        public AIState CurrentAIState => currentState;
        public float CurrentForwardInput => currentForwardInput;
        public float CurrentSteeringInput => currentSteeringInput;
        public bool IsBoostActive => isBoostActive;
        public float Difficulty => difficulty01;

        public void SetTarget(Transform playerTarget)
        {
            target = playerTarget;
        }

        public void SetDifficulty(float diff01, int index = 0, int totalEnemies = 1)
        {
            difficulty01 = Mathf.Clamp01(diff01);
            enemyIndex = index;
            totalEnemiesInMatch = Mathf.Max(1, totalEnemies);

            // Scale tactical parameters with difficulty
            attackRange = Mathf.Lerp(3.5f, 6.0f, difficulty01);
            repositionRange = Mathf.Lerp(13f, 8.5f, difficulty01);
            stateChangeInterval = Mathf.Lerp(0.85f, 0.22f, difficulty01);
            obstacleDetectRange = Mathf.Lerp(4f, 7.5f, difficulty01);
            obstacleAvoidStrength = Mathf.Lerp(1.2f, 2.4f, difficulty01);
        }

        // ── Lifecycle ────────────────────────────────────────────────

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (beyMovementController == null || beyConfiguration == null || target == null)
                return;

            // Evaluate state transitions
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                EvaluateState();
                stateTimer = stateChangeInterval;
            }

            // Compute desired direction (with predictive aiming and obstacle avoidance)
            Vector3 desiredDir = ComputeDesiredDirection();

            // Simulate forward input (W key equivalent)
            currentForwardInput = ComputeForwardInput();

            // Simulate steering input (A/D key equivalent)
            currentSteeringInput = ComputeSteeringInput(desiredDir);

            // Set direction override (replaces the camera direction that player uses)
            Vector3 right = Vector3.Cross(Vector3.up, desiredDir).normalized;
            beyMovementController.SetDirectionOverride(desiredDir, right);

            // Feed input to movement controller
            beyMovementController.CacheInput(currentForwardInput, currentSteeringInput);

            // Tactical Boost Management
            UpdateBoostDecision(desiredDir);

            // Tactical Ability Activation
            abilityCheckCooldown -= Time.deltaTime;
            if (abilityCheckCooldown <= 0f)
            {
                abilityCheckCooldown = Mathf.Lerp(1.2f, 0.4f, difficulty01);
                TryTacticalAbility();
            }
        }

        // ── State Machine ────────────────────────────────────────────

        private void EvaluateState()
        {
            if (target == null) return;

            float dist = Vector3.Distance(transform.position, target.position);
            Vector3 toTarget = (target.position - transform.position).normalized;

            // High-difficulty evasion check: if player is charging fast directly at AI with boost
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (difficulty01 >= 0.4f && targetRb != null && dist < 7f)
            {
                Vector3 targetVel = targetRb.linearVelocity;
                targetVel.y = 0f;
                if (targetVel.magnitude > 8f && Vector3.Dot(targetVel.normalized, -toTarget) > 0.75f)
                {
                    // Evade incoming ramming strike
                    currentState = AIState.Evade;
                    Vector3 perp = Vector3.Cross(Vector3.up, toTarget);
                    float side = (enemyIndex % 2 == 0) ? 1f : -1f;
                    repositionTarget = transform.position + perp * side * 4f;
                    return;
                }
            }

            if (dist <= attackRange)
            {
                currentState = AIState.Attack;
            }
            else if (dist > repositionRange && difficulty01 > 0.2f && Random.value < 0.4f)
            {
                currentState = AIState.Reposition;
                Vector3 perp = Vector3.Cross(Vector3.up, toTarget);
                float side = Random.value > 0.5f ? 1f : -1f;
                repositionTarget = target.position - toTarget * (attackRange * 0.8f)
                                 + perp * side * Random.Range(2.5f, 5.5f);
                repositionTarget.y = transform.position.y;
            }
            else
            {
                currentState = AIState.Chase;
            }
        }

        // ── Input Simulation & Aim Prediction ────────────────────────

        private Vector3 ComputeDesiredDirection()
        {
            Vector3 goalPos;

            if (currentState == AIState.Reposition || currentState == AIState.Evade)
            {
                goalPos = repositionTarget;
            }
            else
            {
                goalPos = target.position;

                // Predictive Intercept Leading: scale target lead distance by player speed & closing time
                Rigidbody targetRb = target.GetComponent<Rigidbody>();
                if (targetRb != null && difficulty01 > 0.15f)
                {
                    Vector3 targetVel = targetRb.linearVelocity;
                    targetVel.y = 0f;
                    float dist = Vector3.Distance(transform.position, goalPos);
                    float mySpeed = Mathf.Max(4f, beyMovementController.CurrentHorizontalSpeed);
                    float timeToIntercept = Mathf.Clamp(dist / mySpeed, 0f, 1.2f);

                    float predictionFactor = Mathf.Lerp(0.15f, 0.90f, difficulty01);
                    goalPos += targetVel * (timeToIntercept * predictionFactor);
                }

                // Multi-Enemy Flanking Angle
                if (totalEnemiesInMatch > 1)
                {
                    float angleSpread = (enemyIndex - (totalEnemiesInMatch - 1) * 0.5f) * 32f * Mathf.Deg2Rad;
                    Vector3 toGoal = (goalPos - transform.position);
                    toGoal.y = 0f;
                    if (toGoal.sqrMagnitude > 0.01f)
                    {
                        toGoal = Quaternion.Euler(0f, angleSpread * Mathf.Rad2Deg, 0f) * toGoal;
                        goalPos = transform.position + toGoal;
                    }
                }
            }

            Vector3 rawDir = goalPos - transform.position;
            rawDir.y = 0f;

            if (rawDir.sqrMagnitude < 0.01f) return transform.forward;
            rawDir.Normalize();

            // Obstacle avoidance
            Vector3 avoidance = ComputeObstacleAvoidance(rawDir);
            return (rawDir + avoidance * obstacleAvoidStrength).normalized;
        }

        private Vector3 ComputeObstacleAvoidance(Vector3 forward)
        {
            Vector3 avoidance = Vector3.zero;
            Vector3 origin = transform.position + Vector3.up * 0.2f;

            // Detect obstacles and steep arena walls, ignoring Bey layer
            int beyLayer = LayerMask.NameToLayer("Bey");
            int layerMask = beyLayer >= 0 ? ~(1 << beyLayer) : ~0;

            float[] angles = { -90f, -60f, -30f, 0f, 30f, 60f, 90f };
            float[] weights = { 0.35f, 0.65f, 0.95f, 1f, 0.95f, 0.65f, 0.35f };

            for (int i = 0; i < angles.Length; i++)
            {
                Vector3 dir = Quaternion.Euler(0, angles[i], 0) * forward;
                if (Physics.Raycast(origin, dir, out RaycastHit hit, obstacleDetectRange, layerMask))
                {
                    if (hit.collider.isTrigger) continue;

                    float proximity = 1f - (hit.distance / obstacleDetectRange);
                    avoidance += hit.normal * proximity * weights[i];
                }
            }

            avoidance.y = 0f;
            return avoidance;
        }

        private float ComputeForwardInput()
        {
            return currentState switch
            {
                AIState.Attack => 1f,
                AIState.Chase => 1f,
                AIState.Reposition => 0.75f,
                AIState.Evade => 1f,
                _ => 0.5f
            };
        }

        private float ComputeSteeringInput(Vector3 desiredDir)
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb == null) return 0f;

            Vector3 currentVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            if (currentVel.sqrMagnitude < 0.5f) return 0f;

            float cross = Vector3.Cross(currentVel.normalized, desiredDir).y;
            float steerAgility = Mathf.Lerp(1.8f, 3.2f, difficulty01);
            return Mathf.Clamp(cross * steerAgility, -1f, 1f);
        }

        // ── Tactical Boost Management ────────────────────────────────

        private void UpdateBoostDecision(Vector3 desiredDir)
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb == null || beyMovementController == null || target == null)
            {
                isBoostActive = false;
                return;
            }

            float dist = Vector3.Distance(transform.position, target.position);
            Vector3 vel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 toTarget = (target.position - transform.position).normalized;
            float alignment = vel.sqrMagnitude > 1f ? Vector3.Dot(vel.normalized, toTarget) : Vector3.Dot(desiredDir, toTarget);

            bool shouldBoost = false;

            if (currentState == AIState.Attack && dist < attackRange * 1.25f && alignment > 0.65f)
            {
                // Ramming strike charge
                shouldBoost = true;
            }
            else if (currentState == AIState.Evade && difficulty01 > 0.4f)
            {
                // Boost to escape charging player
                shouldBoost = true;
            }
            else if (currentState == AIState.Chase && dist > 8f && alignment > 0.85f && difficulty01 > 0.5f)
            {
                // Intercept sprint across arena
                shouldBoost = true;
            }

            if (shouldBoost && beyConfiguration.CurrentMana > 15f)
            {
                beyMovementController.StartBoost();
                isBoostActive = true;
            }
            else
            {
                beyMovementController.StopBoost();
                isBoostActive = false;
            }
        }

        // ── Tactical Ability Activation ──────────────────────────────

        private void TryTacticalAbility()
        {
            if (beyConfiguration == null || beyMovementController == null || target == null)
                return;

            float dist = Vector3.Distance(transform.position, target.position);
            float currentSpinRatio = beyConfiguration.CurrentSpin / Mathf.Max(1f, beyConfiguration.StartingSpin);

            // Defensive trigger: if low on health/spin, attempt ability immediately
            if (currentSpinRatio < 0.35f)
            {
                AbilityActivationService.TryActivateEquipped(beyConfiguration, beyMovementController);
                return;
            }

            // Offensive trigger: when aligned and in attack range
            if (dist <= attackRange * 1.1f)
            {
                AbilityActivationService.TryActivateEquipped(beyConfiguration, beyMovementController);
            }
            else if (dist <= 9f && difficulty01 > 0.45f && Random.value < 0.35f)
            {
                // Mid-range projectile / rush ability cast
                AbilityActivationService.TryActivateEquipped(beyConfiguration, beyMovementController);
            }
        }

        // ── Gizmos ───────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            Vector3 origin = transform.position - Vector3.up * 0.25f;

            switch (currentState)
            {
                case AIState.Chase: Gizmos.color = Color.yellow; break;
                case AIState.Attack: Gizmos.color = Color.red; break;
                case AIState.Reposition: Gizmos.color = Color.blue; break;
                case AIState.Evade: Gizmos.color = Color.magenta; break;
            }
            Gizmos.DrawWireSphere(origin, 0.4f);

            if (target != null)
            {
                Gizmos.DrawLine(origin, target.position + Vector3.up * 0.5f);
            }
        }
    }
}
