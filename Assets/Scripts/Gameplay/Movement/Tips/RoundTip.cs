using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Movement
{
    /// <summary>
    /// Round Tip: Orbits own axis while drifting. Facing rotates continuously
    /// producing unpredictable arc movement. Moderate stamina drain.
    /// High skill ceiling - requires active steering to control.
    /// </summary>
    public class RoundTip : ITipBehavior
    {
        private float orbitRotation = 0f;
        private const float ORBIT_SPEED = 180f; // degrees per second

        public TipBehaviorType BehaviorType => TipBehaviorType.Round;

        public void ApplyMovement(BeyMovementController controller, float forwardInput)
        {
            // Round tip applies normal forward force but the orbit mechanics create the unpredictability
            float forceAmount = forwardInput * GameConstants.BASE_FORWARD_FORCE;
            controller.ApplyForwardForce(forceAmount);

            // Update internal orbit rotation
            orbitRotation += ORBIT_SPEED * Time.deltaTime;
            orbitRotation = orbitRotation % 360f;
        }

        public void ApplyPhysicsModifiers(Rigidbody rb)
        {
            // Moderate drag to enable orbital feel
            rb.linearDamping = 0.35f;
            rb.angularDamping = 0.4f;
        }

        public void OnSpinThresholdCrossed(float newSpin)
        {
            // Round tip has no threshold behavior
        }

        public float GetUphillResistanceModifier()
        {
            // Medium uphill difficulty
            return 1f;
        }

        public float GetTiltAmount(Vector3 velocity)
        {
            // Orbits visually tilt constantly
            return Mathf.Sin(Time.time * ORBIT_SPEED * Mathf.Deg2Rad) * 0.3f;
        }
    }
}
