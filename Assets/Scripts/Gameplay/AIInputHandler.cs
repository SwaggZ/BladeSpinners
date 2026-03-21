using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Effects;

namespace BladeSpinners.Gameplay
{
    /// <summary>
    /// Drop-in replacement for PlayerInputHandler.
    /// Instead of reading keyboard/mouse, it simulates WASD + mouse by computing:
    ///   - Forward direction toward the player (like pointing the mouse at the player)
    ///   - W input (always pushing forward)
    ///   - A/D steering (to dodge obstacles)
    ///   - Space (jump when near ledges)
    ///   - Shift (boost when attacking)
    ///   - E (ability when in range)
    /// Feeds the same BeyMovementController via CacheInput + SetDirectionOverride.
    /// </summary>
    public class AIInputHandler : MonoBehaviour
    {
        // ── AI States ────────────────────────────────────────────────
        public enum AIState { Chase, Attack, Reposition }

        [Header("AI Tuning")]
        [SerializeField] private float attackRange = 4f;
        [SerializeField] private float repositionRange = 12f;
        [SerializeField] private float stateChangeInterval = 0.8f;
        [SerializeField] private float obstacleDetectRange = 5f;
        [SerializeField] private float obstacleAvoidStrength = 1.5f;

        // ── References (same as PlayerInputHandler) ──────────────────
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

        // ── Public API ───────────────────────────────────────────────
        public AIState CurrentAIState => currentState;
        public float CurrentForwardInput => currentForwardInput;
        public float CurrentSteeringInput => currentSteeringInput;
        public bool IsBoostActive => isBoostActive;

        public void SetTarget(Transform playerTarget)
        {
            target = playerTarget;
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

            // Compute the "mouse direction" — the direction the AI wants to face
            Vector3 desiredDir = ComputeDesiredDirection();

            // Simulate forward input (W key equivalent)
            currentForwardInput = ComputeForwardInput();

            // Simulate steering input (A/D key equivalent)
            currentSteeringInput = ComputeSteeringInput(desiredDir);

            // Set direction override (replaces the camera direction that player uses)
            Vector3 right = Vector3.Cross(Vector3.up, desiredDir).normalized;
            beyMovementController.SetDirectionOverride(desiredDir, right);

            // Feed input exactly like PlayerInputHandler does
            beyMovementController.CacheInput(currentForwardInput, currentSteeringInput);

            // Simulate boost (shift) when attacking
            if (currentState == AIState.Attack)
            {
                beyMovementController.StartBoost();
                isBoostActive = true;
            }
            else
            {
                beyMovementController.StopBoost();
                isBoostActive = false;
            }

            // Simulate ability (E key) when in attack range and have mana
            if (currentState == AIState.Attack)
                TryActivateAbility();
        }

        // ── State machine ────────────────────────────────────────────

        private void EvaluateState()
        {
            if (target == null) return;

            float dist = Vector3.Distance(transform.position, target.position);

            if (dist <= attackRange)
            {
                currentState = AIState.Attack;
            }
            else if (dist > repositionRange)
            {
                currentState = AIState.Reposition;
                Vector3 dirToTarget = (target.position - transform.position).normalized;
                Vector3 perp = Vector3.Cross(Vector3.up, dirToTarget);
                float side = Random.value > 0.5f ? 1f : -1f;
                repositionTarget = target.position - dirToTarget * (attackRange * 0.7f)
                                 + perp * side * Random.Range(2f, 5f);
                repositionTarget.y = transform.position.y;
            }
            else
            {
                currentState = AIState.Chase;
            }
        }

        // ── Input simulation ─────────────────────────────────────────

        private Vector3 ComputeDesiredDirection()
        {
            Vector3 goalPos = currentState == AIState.Reposition ? repositionTarget : target.position;
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

            // Layer mask: only detect Ground layer + Default. Ignore Bey layer (other bey parts).
            int beyLayer = LayerMask.NameToLayer("Bey");
            int layerMask = ~0; // all layers
            if (beyLayer >= 0)
                layerMask = ~(1 << beyLayer); // exclude Bey layer

            float[] angles = { -90f, -60f, -30f, 0f, 30f, 60f, 90f };
            float[] weights = { 0.3f, 0.6f, 0.9f, 1f, 0.9f, 0.6f, 0.3f };

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
            // W = 1, S = -1. AI always holds W, except moderate during reposition.
            return currentState switch
            {
                AIState.Attack => 1f,
                AIState.Chase => 1f,
                AIState.Reposition => 0.7f,
                _ => 0f
            };
        }

        private float ComputeSteeringInput(Vector3 desiredDir)
        {
            // A = -1, D = +1. AI steers to align velocity with desired direction.
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb == null) return 0f;

            Vector3 currentVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            if (currentVel.sqrMagnitude < 1f) return 0f;

            float cross = Vector3.Cross(currentVel.normalized, desiredDir).y;
            return Mathf.Clamp(cross * 2f, -1f, 1f);
        }

        private void TryActivateAbility()
        {
            if (beyConfiguration == null) return;

            BeyStatBlock stats = beyConfiguration.GetStatBlock();
            if (stats.EquippedAbility == null) return;

            float manaCost = stats.EquippedAbility.ManaCost;
            if (beyConfiguration.CanUseAbility(manaCost))
            {
                beyConfiguration.SpendMana(manaCost);
                stats.EquippedAbility.Activate(beyMovementController);
                AbilityEmblemHologramEffect.Spawn(beyMovementController);
            }
        }

        // ── Gizmos ───────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            Vector3 origin = transform.position - Vector3.up * 0.25f;

            // State color
            switch (currentState)
            {
                case AIState.Chase: Gizmos.color = Color.yellow; break;
                case AIState.Attack: Gizmos.color = Color.red; break;
                case AIState.Reposition: Gizmos.color = Color.blue; break;
            }
            Gizmos.DrawWireSphere(origin, 0.4f);

            // Line to target
            if (target != null)
            {
                Gizmos.DrawLine(origin, target.position + Vector3.up * 0.5f);
            }

            // Reposition target
            if (currentState == AIState.Reposition)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(repositionTarget + Vector3.up * 0.3f, Vector3.one * 0.5f);
            }

            // Obstacle avoidance rays
            if (Application.isPlaying && target != null)
            {
                Vector3 fwd = (target.position - transform.position).normalized;
                fwd.y = 0f;
                if (fwd.sqrMagnitude > 0.01f) fwd.Normalize();
                else fwd = transform.forward;

                // Match actual avoidance: 7 rays with Bey layer excluded
                int beyLayerGizmo = LayerMask.NameToLayer("Bey");
                int gizmoMask = beyLayerGizmo >= 0 ? ~(1 << beyLayerGizmo) : ~0;

                float[] gizmoAngles = { -90f, -60f, -30f, 0f, 30f, 60f, 90f };
                Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;
                foreach (float a in gizmoAngles)
                {
                    Vector3 dir = Quaternion.Euler(0, a, 0) * fwd;
                    if (Physics.Raycast(rayOrigin, dir, out RaycastHit hit, obstacleDetectRange, gizmoMask)
                        && !hit.collider.isTrigger)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(rayOrigin, hit.point);
                    }
                    else
                    {
                        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                        Gizmos.DrawRay(rayOrigin, dir * obstacleDetectRange);
                    }
                }
            }
        }
    }
}
