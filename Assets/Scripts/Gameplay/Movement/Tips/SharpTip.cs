using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Movement
{
    /// <summary>
    /// Sharp Tip: Slow drifting arcs, maximum stamina conservation.
    /// Very difficult to control precisely due to unpredictable curves.
    /// Lowest behavior-based stamina drain.
    /// </summary>
    public class SharpTip : ITipBehavior
    {
        public TipBehaviorType BehaviorType => TipBehaviorType.Sharp;

        public void ApplyMovement(BeyMovementController controller, float forwardInput)
        {
            // Sharp tip applies reduced forward force but the sharpness creates natural curves
            float forceAmount = forwardInput * GameConstants.BASE_FORWARD_FORCE * 0.7f; // 30% weaker
            controller.ApplyForwardForce(forceAmount);
        }

        public void ApplyPhysicsModifiers(Rigidbody rb)
        {
            // Higher drag conserves momentum but creates gliding feel
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.6f;
        }

        public void OnSpinThresholdCrossed(float newSpin)
        {
            // Sharp tip has no threshold behavior
        }

        public float GetUphillResistanceModifier()
        {
            // Decent uphill ability due to consistent angle
            return 1.2f;
        }

        public float GetTiltAmount(Vector3 velocity)
        {
            // Minimal tilt - sharp tips stay stable
            return Mathf.Clamp01(velocity.magnitude / 40f) * 0.15f;
        }
    }
}
