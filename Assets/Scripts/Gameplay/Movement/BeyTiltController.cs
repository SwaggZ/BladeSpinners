using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Gameplay.Movement
{
    /// <summary>
    /// Handles visual and physical tilt/lean of the Bey based on movement direction and speed.
    /// At very low spin values, enters a wobble state that serves as a visual health indicator.
    /// The tilt amount is determined by the active Tip behavior.
    /// </summary>
    public class BeyTiltController : MonoBehaviour
    {
        [SerializeField]
        private BeyMovementController movementController;

        [SerializeField]
        private BeyConfiguration beyConfiguration;

        // TiltPivot: applies tilt (X/Z lean) only. Sibling of CameraRig under root.
        [SerializeField]
        private Transform tiltPivotTransform;

        // SpinChild: child of TiltPivot, applies Y-axis spin only.
        // All bey part meshes are children of this.
        [SerializeField]
        private Transform spinChildTransform;

        private Vector3 targetTiltEuler = Vector3.zero;
        private Vector3 currentTiltEuler = Vector3.zero;
        private float tiltSmoothSpeed = 50f;
        private bool isWobbling = false;
        private float wobbleIntensity = 0f;

        // Continuous Y-axis spin (visual spinning top effect)
        private float spinAngleY = 0f;
        private const float BASE_SPIN_SPEED = 1800f; // degrees/sec at full spin
        private const float MIN_SPIN_SPEED = 200f;   // degrees/sec at near-zero spin

        private void LateUpdate()
        {
            if (beyConfiguration == null)
                return;

            // --- Continuous Y-axis spin (always runs, even without movement controller) ---
            float spinFraction = Mathf.Clamp01(beyConfiguration.CurrentSpin / GameConstants.DEFAULT_STARTING_SPIN);
            float gmVisual = GameManager.GetForBey(beyConfiguration.IsEnemy, g => g.visualSpinMultiplier, g => g.enemyVisualSpinMultiplier);
            float currentSpinSpeed = Mathf.Lerp(MIN_SPIN_SPEED, BASE_SPIN_SPEED, spinFraction) * gmVisual;
            spinAngleY += currentSpinSpeed * Time.deltaTime;
            if (spinAngleY > 360f) spinAngleY -= 360f;

            // Apply Y-axis spin to SpinChild — part meshes spin but tilt axes stay world-aligned
            if (spinChildTransform != null)
            {
                spinChildTransform.localRotation = Quaternion.Euler(0, spinAngleY, 0);
            }

            // --- Tilt (requires movement controller for velocity) ---
            if (movementController != null)
            {
                // Check if entering wobble state (low spin)
                float spinRatio = beyConfiguration.CurrentSpin / GameConstants.DEFAULT_STARTING_SPIN;
                bool shouldWobble = spinRatio < GameConstants.SPIN_WOBBLE_THRESHOLD;

                if (shouldWobble && !isWobbling)
                {
                    isWobbling = true;
                    wobbleIntensity = 0f;
                }
                else if (!shouldWobble && isWobbling)
                {
                    isWobbling = false;
                    wobbleIntensity = 0f;
                }

                if (isWobbling)
                {
                    ApplyWobble(spinRatio);
                }
                else
                {
                    ApplyNormalTilt();
                }

                // Smooth tilt transition
                currentTiltEuler = Vector3.Lerp(currentTiltEuler, targetTiltEuler, Time.deltaTime * tiltSmoothSpeed);

                // Clamp extreme tilts
                currentTiltEuler.x = Mathf.Clamp(currentTiltEuler.x, -GameConstants.MAX_TILT_ANGLE, GameConstants.MAX_TILT_ANGLE);
                currentTiltEuler.z = Mathf.Clamp(currentTiltEuler.z, -GameConstants.MAX_TILT_ANGLE, GameConstants.MAX_TILT_ANGLE);
            }

            // Apply tilt (X/Z) to TiltPivot — camera is a sibling, so unaffected
            if (tiltPivotTransform != null)
            {
                tiltPivotTransform.localRotation = Quaternion.Euler(currentTiltEuler.x, 0, currentTiltEuler.z);
            }
        }

        /// <summary>
        /// Applies normal tilt based on velocity direction and active tip behavior.
        /// The bey leans INTO the direction it's moving, like in the anime —
        /// faster movement = deeper lean.
        /// </summary>
        private void ApplyNormalTilt()
        {
            Vector3 velocity = movementController.CurrentVelocity;
            ITipBehavior tipBehavior = movementController.ActiveTipBehavior;

            if (tipBehavior == null)
            {
                targetTiltEuler = Vector3.zero;
                return;
            }

            // tipBehavior returns a 0-1 factor scaled by speed (tip personality)
            float tiltFactor = tipBehavior.GetTiltAmount(velocity);

            // Horizontal velocity only — vertical movement shouldn't tilt the bey
            Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);
            float speed = horizontalVel.magnitude;

            if (speed < 0.1f)
            {
                // Nearly stationary — return upright
                targetTiltEuler = Vector3.zero;
                return;
            }

            // Lean angle: tip factor × max tilt gives the final angle
            float leanAngle = tiltFactor * GameConstants.MAX_TILT_ANGLE;

            // Direction of travel in world space
            Vector3 moveDir = horizontalVel / speed;

            // Tilt TOWARD movement direction:
            //   Moving along +Z (world forward)  → tilt around X axis (positive X euler = nose down)
            //   Moving along +X (world right)    → tilt around Z axis (negative Z euler = lean right)
            // This gives the anime "leaning into the dash" look.
            float tiltX = moveDir.z * leanAngle;   // forward/back movement → pitch
            float tiltZ = -moveDir.x * leanAngle;  // left/right movement  → roll

            targetTiltEuler = new Vector3(tiltX, 0f, tiltZ);
        }

        /// <summary>
        /// Applies wobble effect when spin is critically low.
        /// Wobble serves as a visual "low health" indicator.
        /// </summary>
        private void ApplyWobble(float spinRatio)
        {
            wobbleIntensity = Mathf.Lerp(wobbleIntensity, 1f, Time.deltaTime * GameConstants.WOBBLE_ANIMATION_SPEED);

            // Erratic wobble pattern
            float wobbleX = Mathf.Sin(Time.time * 8f) * wobbleIntensity * 20f;
            float wobbleZ = Mathf.Cos(Time.time * 12f) * wobbleIntensity * 20f;
            float wobbleY = Mathf.Sin(Time.time * 6f) * wobbleIntensity * 10f;

            targetTiltEuler = new Vector3(wobbleX, wobbleY, wobbleZ);
        }

        // Public methods for external checks
        public bool IsWobbling => isWobbling;
        public float WobbleIntensity => wobbleIntensity;
    }
}
