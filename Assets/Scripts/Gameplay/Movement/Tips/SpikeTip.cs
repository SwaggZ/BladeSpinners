using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Movement
{
    /// <summary>
    /// Spike Tip: Nearly stationary, doesn't move much. Maximum stamina drain resistance
    /// (very low behavior-based drain). Facing rotation has minimal effect on path.
    /// Defensive, stamina-efficient tip for patient playstyle.
    /// </summary>
    public class SpikeTip : ITipBehavior
    {
        public TipBehaviorType BehaviorType => TipBehaviorType.Spike;

        public void ApplyMovement(BeyMovementController controller, float forwardInput)
        {
            // Spike tip applies very weak forward force - it prefers to stay in place
            float forceAmount = forwardInput * GameConstants.BASE_FORWARD_FORCE * 0.4f;
            controller.ApplyForwardForce(forceAmount);
        }

        public void ApplyPhysicsModifiers(Rigidbody rb)
        {
            // Very high drag - spike barely moves
            rb.linearDamping = 1.2f;
            rb.angularDamping = 1.3f;
        }

        public void OnSpinThresholdCrossed(float newSpin)
        {
            // Spike tip has no threshold behavior
        }

        public float GetUphillResistanceModifier()
        {
            // Excellent uphill - spike doesn't slide
            return 2f;
        }

        public float GetTiltAmount(Vector3 velocity)
        {
            // No tilt - spike stays perfectly upright
            return 0f;
        }
    }
}
