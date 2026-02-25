using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Movement
{
    /// <summary>
    /// Ball Tip: Balanced grip and behavior. Slight tilt toward movement direction.
    /// Good uphill ability, low drain. Safe, beginner-friendly choice.
    /// </summary>
    public class BallTip : ITipBehavior
    {
        public TipBehaviorType BehaviorType => TipBehaviorType.Ball;

        public void ApplyMovement(BeyMovementController controller, float forwardInput)
        {
            // Ball tip applies balanced forward force
            float forceAmount = forwardInput * GameConstants.BASE_FORWARD_FORCE;
            controller.ApplyForwardForce(forceAmount);
        }

        public void ApplyPhysicsModifiers(Rigidbody rb)
        {
            // Balanced drag for middle-ground feel
            rb.linearDamping = 0.4f;
            rb.angularDamping = 0.5f;
        }

        public void OnSpinThresholdCrossed(float newSpin)
        {
            // Ball tip has no threshold behavior
        }

        public float GetUphillResistanceModifier()
        {
            // Good uphill - balanced
            return 1.1f;
        }

        public float GetTiltAmount(Vector3 velocity)
        {
            // Slight tilt in movement direction
            return Mathf.Clamp01(velocity.magnitude / 32f) * 0.25f;
        }
    }
}
