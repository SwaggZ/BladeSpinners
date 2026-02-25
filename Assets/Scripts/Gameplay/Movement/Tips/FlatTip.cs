using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Movement
{
    /// <summary>
    /// Flat Tip: Fast, aggressive, low grip so momentum carries wide when turning.
    /// Struggles uphill. High behavior-based stamina drain.
    /// This is an aggressive, speed-focused tip for experienced players.
    /// </summary>
    public class FlatTip : ITipBehavior
    {
        public TipBehaviorType BehaviorType => TipBehaviorType.Flat;

        public void ApplyMovement(BeyMovementController controller, float forwardInput)
        {
            // Flat tip applies strong forward force but with minimal grip
            // This creates wide arcs when turning
            float forceAmount = forwardInput * GameConstants.BASE_FORWARD_FORCE * 1.3f; // 30% stronger
            Debug.Log($"[FlatTip] ApplyMovement called - forwardInput: {forwardInput}, forceAmount: {forceAmount}");
            controller.ApplyForwardForce(forceAmount);
        }

        public void ApplyPhysicsModifiers(Rigidbody rb)
        {
            // Low drag allows momentum to carry far
            rb.linearDamping = 0.2f;
            rb.angularDamping = 0.3f;
        }

        public void OnSpinThresholdCrossed(float newSpin)
        {
            // Flat tip has no threshold behavior
        }

        public float GetUphillResistanceModifier()
        {
            // Struggles uphill - low multiplier applied to uphill force
            return 0.6f;
        }

        public float GetTiltAmount(Vector3 velocity)
        {
            // Aggressive tilt at speed
            return Mathf.Clamp01(velocity.magnitude / 30f) * 0.4f;
        }
    }
}
