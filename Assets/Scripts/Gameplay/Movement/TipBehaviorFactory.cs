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

                // Metal Fight-inspired presets
                case TipBehaviorType.WideDefense_WD:
                    return new CatalogTipPresetBehavior(behaviorType, 0.65f, 0.85f, 0.95f, 1.60f, 0.08f);
                case TipBehaviorType.Quake_Q:
                    return new CatalogTipPresetBehavior(behaviorType, 1.15f, 0.28f, 0.36f, 0.80f, 0.35f, 0.20f, 6.0f);
                case TipBehaviorType.EternalSharp_ES:
                    return new CatalogTipPresetBehavior(behaviorType, 0.52f, 1.15f, 1.25f, 1.95f, 0.02f);
                case TipBehaviorType.WideDefense2_W2D:
                    return new CatalogTipPresetBehavior(behaviorType, 0.62f, 0.95f, 1.05f, 1.75f, 0.06f);
                case TipBehaviorType.MetalSharp_MS:
                    return new CatalogTipPresetBehavior(behaviorType, 0.68f, 0.90f, 1.00f, 1.70f, 0.08f);
                case TipBehaviorType.EternalDefenseSharp_EDS:
                    return new CatalogTipPresetBehavior(behaviorType, 0.58f, 1.05f, 1.15f, 1.85f, 0.05f);
                case TipBehaviorType.SemiFlat_SF:
                    return new CatalogTipPresetBehavior(behaviorType, 1.08f, 0.35f, 0.45f, 0.90f, 0.30f);
                case TipBehaviorType.MetalBall_MB:
                    return new CatalogTipPresetBehavior(behaviorType, 0.88f, 0.58f, 0.68f, 1.25f, 0.16f);
                case TipBehaviorType.BearingSpike_BS:
                    return new CatalogTipPresetBehavior(behaviorType, 0.55f, 1.10f, 1.20f, 1.90f, 0.03f);
                case TipBehaviorType.SemiDefense_SD:
                    return new CatalogTipPresetBehavior(behaviorType, 0.74f, 0.78f, 0.88f, 1.45f, 0.12f);
                case TipBehaviorType.HoleFlat_HF:
                    return new CatalogTipPresetBehavior(behaviorType, 1.18f, 0.30f, 0.40f, 0.82f, 0.34f);
                case TipBehaviorType.DefenseSharp_DS:
                    return new CatalogTipPresetBehavior(behaviorType, 0.64f, 0.98f, 1.08f, 1.75f, 0.06f);
                case TipBehaviorType.Sharp_S:
                    return new CatalogTipPresetBehavior(behaviorType, 0.56f, 1.08f, 1.18f, 1.90f, 0.03f);
                case TipBehaviorType.FlatSharp_FS:
                    return new CatalogTipPresetBehavior(behaviorType, 0.98f, 0.50f, 0.60f, 1.08f, 0.22f);
                case TipBehaviorType.Ball_B:
                    return new CatalogTipPresetBehavior(behaviorType, 0.92f, 0.62f, 0.72f, 1.20f, 0.18f);
                case TipBehaviorType.RubberSharp_RS:
                    return new CatalogTipPresetBehavior(behaviorType, 0.72f, 0.88f, 0.98f, 1.65f, 0.10f);
                case TipBehaviorType.Flat_F:
                    return new CatalogTipPresetBehavior(behaviorType, 1.22f, 0.26f, 0.34f, 0.74f, 0.38f);
                case TipBehaviorType.Defense_D:
                    return new CatalogTipPresetBehavior(behaviorType, 0.60f, 1.02f, 1.12f, 1.82f, 0.04f);
                case TipBehaviorType.Rubber2Flat_R2F:
                    return new CatalogTipPresetBehavior(behaviorType, 1.26f, 0.22f, 0.30f, 0.72f, 0.42f);
                case TipBehaviorType.EternalWideDefense_EWD:
                    return new CatalogTipPresetBehavior(behaviorType, 0.66f, 0.86f, 0.96f, 1.62f, 0.08f);
                case TipBehaviorType.DeltaDrive_D_D:
                    return new CatalogTipPresetBehavior(behaviorType, 1.05f, 0.42f, 0.52f, 1.05f, 0.24f, 0.10f, 4.0f);
                case TipBehaviorType.CoatSharp_CS:
                    return new CatalogTipPresetBehavior(behaviorType, 0.62f, 1.00f, 1.10f, 1.78f, 0.05f);
                case TipBehaviorType.BearingDrive_B_D:
                    return new CatalogTipPresetBehavior(behaviorType, 0.70f, 0.74f, 0.84f, 1.40f, 0.14f);
                case TipBehaviorType.WideFlat_WF:
                    return new CatalogTipPresetBehavior(behaviorType, 1.12f, 0.32f, 0.42f, 0.86f, 0.34f);
                case TipBehaviorType.RubberBall_RB:
                    return new CatalogTipPresetBehavior(behaviorType, 0.98f, 0.55f, 0.68f, 1.28f, 0.20f);
                case TipBehaviorType.HoleFlatSharp_HF_S:
                    return new CatalogTipPresetBehavior(behaviorType, 1.04f, 0.44f, 0.54f, 1.02f, 0.24f);
                case TipBehaviorType.Fusion_F:
                    return new CatalogTipPresetBehavior(behaviorType, 1.18f, 0.28f, 0.36f, 0.78f, 0.38f);
                default:
                    Debug.LogWarning($"Unknown tip behavior type: {behaviorType}, defaulting to Ball");
                    return new BallTip();
            }
        }
    }
}
