using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Movement
{
    /// <summary>
    /// Orbit Tip: travels forward while circling a small, moving local anchor.
    /// The resulting path resembles a moon orbiting a planet as the planet moves.
    /// Steering bends the anchor's travel path without expanding the local orbit.
    /// </summary>
    public class OrbitTip : ITipBehavior
    {
        public const float LocalOrbitRadius = 0.75f;
        public const float ForwardTravelSpeed = 18f;
        public const float AngularSpeedDegrees = 240f;

        public TipBehaviorType BehaviorType => TipBehaviorType.Orbit;

        public void ApplyMovement(BeyMovementController controller, float forwardInput)
        {
            float inputMagnitude = Mathf.Clamp01(Mathf.Abs(forwardInput));
            float signedTravelSpeed =
                Mathf.Sign(forwardInput) * ForwardTravelSpeed * inputMagnitude;

            controller.ApplyOrbitMovement(
                LocalOrbitRadius,
                signedTravelSpeed,
                AngularSpeedDegrees);
        }

        public void ApplyPhysicsModifiers(Rigidbody rb)
        {
            // Moderate drag to enable smooth circular motion
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.6f;
        }

        public void OnSpinThresholdCrossed(float newSpin)
        {
            // Orbit tip has no threshold behavior
        }

        public float GetUphillResistanceModifier()
        {
            // Moderate uphill - orbital motion can climb slopes
            return 1f;
        }

        public float GetTiltAmount(Vector3 velocity)
        {
            // Heavy tilt during circular motion
            return Mathf.Clamp01(velocity.magnitude / 25f) * 0.6f;
        }
    }
}
