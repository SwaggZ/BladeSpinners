using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Movement
{
    /// <summary>
    /// Factory for creating ITipBehavior instances based on TipBehaviorType.
    /// </summary>
    public static class TipBehaviorFactory
    {
        /// <summary>
        /// Creates and returns a new instance of the specified tip behavior.
        /// </summary>
        public static ITipBehavior CreateTipBehavior(TipBehaviorType behaviorType)
        {
            switch (behaviorType)
            {
                case TipBehaviorType.Flat:
                    return new FlatTip();
                case TipBehaviorType.Sharp:
                    return new SharpTip();
                case TipBehaviorType.Round:
                    return new RoundTip();
                case TipBehaviorType.RubberFlat:
                    return new RubberFlatTip();
                case TipBehaviorType.Ball:
                    return new BallTip();
                case TipBehaviorType.Spike:
                    return new SpikeTip();
                case TipBehaviorType.Orbit:
                    return new OrbitTip();
                default:
                    Debug.LogWarning($"Unknown tip behavior type: {behaviorType}, defaulting to Ball");
                    return new BallTip();
            }
        }
    }
}
