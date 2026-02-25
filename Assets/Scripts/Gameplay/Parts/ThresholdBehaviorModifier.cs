using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Parts
{
    /// <summary>
    /// Component that defines a spin percentage threshold and a behavior or stat change
    /// that triggers when spin drops below the threshold.
    /// This is a first-class system - any part can carry this, not just Final Drive.
    /// </summary>
    public class ThresholdBehaviorModifier
    {
        [SerializeField]
        private float spinPercentageThreshold = 0.5f; // Between 0 and 1

        [SerializeField]
        private TipBehaviorType altTipBehavior = TipBehaviorType.Ball;

        [SerializeField]
        private float altWeight = -1f; // -1 means no change

        [SerializeField]
        private float altDrainModifier = -1f; // -1 means no change

        public float SpinPercentageThreshold => spinPercentageThreshold;
        public TipBehaviorType AltTipBehavior => altTipBehavior;
        public float AltWeight => altWeight;
        public float AltDrainModifier => altDrainModifier;

        /// <summary>
        /// Checks if the current spin has crossed the threshold.
        /// </summary>
        public bool HasCrossedThreshold(float currentSpin, float maxSpin)
        {
            float spinRatio = currentSpin / maxSpin;
            return spinRatio < spinPercentageThreshold;
        }

        /// <summary>
        /// Applies the threshold modifiers to a stat block.
        /// </summary>
        public void ApplyModifiers(BeyStatBlock stats)
        {
            stats.TipBehavior = AltTipBehavior;

            if (AltWeight >= 0)
                stats.Weight = AltWeight;

            if (AltDrainModifier >= 0)
                stats.BehaviorBasedStaminaDrainModifier = AltDrainModifier;
        }
    }
}
