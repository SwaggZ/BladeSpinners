using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Movement
{
    /// <summary>
    /// Orbit Tip: Player controls orbit radius and the Bey travels in a circle
    /// around a central axis. Steering input adjusts the orbit rather than direct facing.
    /// High skill ceiling, devastating on contact. Moderate drain.
    /// This is a completely different control scheme.
    /// </summary>
    public class OrbitTip : ITipBehavior
    {
        private Vector3 orbitCenter = Vector3.zero;
        private float orbitRadius = 5f;
        private float orbitSpeed = 15f; // m/s movement speed

        public TipBehaviorType BehaviorType => TipBehaviorType.Orbit;

        public void ApplyMovement(BeyMovementController controller, float forwardInput)
        {
            // (23/3/2026): Set orbit center to Bey's current position for self-orbit
            // Previously defaulted to world (0,0,0) causing orbital flight to map center
            orbitCenter = controller.transform.position;
            
            // Orbit tip moves in a circle around a point
            // Forward input controls orbital speed, turning adjusts orbit radius
            // This is delegated to BeyMovementController to re-interpret as orbital movement
            orbitSpeed = Mathf.Max(0.1f, forwardInput * GameConstants.BASE_FORWARD_FORCE);
            
            // The actual orbital motion is handled in BeyMovementController
            // because it needs access to the steering input too
            controller.ApplyOrbitMovement(orbitCenter, orbitRadius, orbitSpeed);
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
