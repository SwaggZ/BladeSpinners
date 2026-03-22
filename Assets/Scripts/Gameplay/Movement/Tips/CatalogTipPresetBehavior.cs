using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Movement
{
    public class CatalogTipPresetBehavior : ITipBehavior
    {
        private readonly TipBehaviorType behaviorType;
        private readonly float forceScale;
        private readonly float linearDamping;
        private readonly float angularDamping;
        private readonly float uphillResistance;
        private readonly float tiltScale;
        private readonly float dynamicWobbleAmplitude;
        private readonly float dynamicWobbleSpeed;

        public CatalogTipPresetBehavior(
            TipBehaviorType behaviorType,
            float forceScale,
            float linearDamping,
            float angularDamping,
            float uphillResistance,
            float tiltScale,
            float dynamicWobbleAmplitude = 0f,
            float dynamicWobbleSpeed = 0f)
        {
            this.behaviorType = behaviorType;
            this.forceScale = forceScale;
            this.linearDamping = linearDamping;
            this.angularDamping = angularDamping;
            this.uphillResistance = uphillResistance;
            this.tiltScale = tiltScale;
            this.dynamicWobbleAmplitude = dynamicWobbleAmplitude;
            this.dynamicWobbleSpeed = dynamicWobbleSpeed;
        }

        public TipBehaviorType BehaviorType => behaviorType;

        public void ApplyMovement(BeyMovementController controller, float forwardInput)
        {
            float dynamicScale = 1f;
            if (dynamicWobbleAmplitude > 0f)
                dynamicScale += Mathf.Sin(Time.time * dynamicWobbleSpeed) * dynamicWobbleAmplitude;

            float forceAmount = forwardInput * GameConstants.BASE_FORWARD_FORCE * forceScale * dynamicScale;
            controller.ApplyForwardForce(forceAmount);
        }

        public void ApplyPhysicsModifiers(Rigidbody rb)
        {
            rb.linearDamping = linearDamping;
            rb.angularDamping = angularDamping;
        }

        public void OnSpinThresholdCrossed(float newSpin)
        {
        }

        public float GetUphillResistanceModifier()
        {
            return uphillResistance;
        }

        public float GetTiltAmount(Vector3 velocity)
        {
            float baseTilt = Mathf.Clamp01(velocity.magnitude / 32f) * tiltScale;
            if (dynamicWobbleAmplitude > 0f)
                baseTilt += Mathf.Sin(Time.time * dynamicWobbleSpeed) * (dynamicWobbleAmplitude * 0.1f);
            return baseTilt;
        }
    }
}