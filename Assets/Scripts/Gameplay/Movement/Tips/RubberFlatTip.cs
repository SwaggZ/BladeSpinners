using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Movement
{
    /// <summary>
    /// RubberFlat Tip: High grip, facing rotation translates quickly into tight arc changes.
    /// Highest behavior-based stamina drain due to constant grip friction.
    /// Precise, responsive control but very stamina-hungry.
    /// </summary>
    public class RubberFlatTip : ITipBehavior
    {
        public TipBehaviorType BehaviorType => TipBehaviorType.RubberFlat;

        public void ApplyMovement(BeyMovementController controller, float forwardInput)
        {
            // RubberFlat applies strong forward force and has excellent grip response
            float forceAmount = forwardInput * GameConstants.BASE_FORWARD_FORCE * 1.1f; // 10% stronger
            controller.ApplyForwardForce(forceAmount);
        }

        public void ApplyPhysicsModifiers(Rigidbody rb)
        {
            // Very high drag - rubber grips hard to the surface
            rb.linearDamping = 0.8f;
            rb.angularDamping = 0.9f;
        }

        public void OnSpinThresholdCrossed(float newSpin)
        {
            // RubberFlat has no threshold behavior
        }

        public float GetUphillResistanceModifier()
        {
            // Excellent uphill grip
            return 1.5f;
        }

        public float GetTiltAmount(Vector3 velocity)
        {
            // Minimal tilt - rubber grips hard and stays stable
            return Mathf.Clamp01(velocity.magnitude / 35f) * 0.2f;
        }
    }
}
