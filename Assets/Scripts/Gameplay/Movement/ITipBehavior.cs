using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Movement
{
    /// <summary>
    /// Interface for all Tip behaviors. Each behavior defines how the Bey moves and responds to physics.
    /// All behaviors apply force exclusively along the Bey's forward axis.
    /// Steering rotates the facing direction, and momentum does the rest.
    /// </summary>
    public interface ITipBehavior
    {
        /// <summary>
        /// The type of tip behavior this represents.
        /// </summary>
        TipBehaviorType BehaviorType { get; }

        /// <summary>
        /// Applies movement force/logic specific to this tip behavior.
        /// Force is applied only along the Bey's forward axis.
        /// </summary>
        /// <param name="controller">The BeyMovementController requesting movement.</param>
        /// <param name="forwardInput">Input from -1 to 1, where 1 is full forward, -1 is brake.</param>
        void ApplyMovement(BeyMovementController controller, float forwardInput);

        /// <summary>
        /// Applies physics modifiers specific to this tip (drag, friction, etc).
        /// Called once per physics update.
        /// </summary>
        /// <param name="rb">The Rigidbody to modify.</param>
        void ApplyPhysicsModifiers(Rigidbody rb);

        /// <summary>
        /// Called when spin crosses the threshold (if one exists).
        /// Used for behaviors that change at low spin.
        /// </summary>
        /// <param name="newSpin">The new spin value that triggered the crossing.</param>
        void OnSpinThresholdCrossed(float newSpin);

        /// <summary>
        /// Gets the uphill resistance multiplier for this behavior.
        /// </summary>
        float GetUphillResistanceModifier();

        /// <summary>
        /// Gets the groove/tilt at various speeds (used by BeyTiltController).
        /// </summary>
        float GetTiltAmount(Vector3 velocity);
    }
}
