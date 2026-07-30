using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Parts
{
    /// <summary>
    /// Generates procedural 3D meshes for each Beyblade part type.
    /// Each part's MeshSeed drives visual variation — no two parts with different seeds
    /// look the same. Stats also influence shape (weight → bigger wheel, etc.).
    ///
    /// FaceBolts are the exception: all FaceBolts share one canonical hex mesh.
    /// Only color, emblem, name, and ability differ between FaceBolts.
    ///
    /// Stack order (bottom to top): Tip → Track → FusionWheel → EnergyRing → FaceBolt
    /// All meshes are generated with bottom at y=0, top at y=height.
    /// BeyAssembler handles vertical stacking and centering.
    /// </summary>
    public static class ProceduralPartMeshGenerator
    {
        private const int RING_SEGMENTS = 32;
        private const int TIP_SEGMENTS = 16;
        private const float FACE_BOLT_RADIUS = 0.038f;
        private const float ENERGY_RING_HOLE_EXTRA_DIAMETER = 0.03f; // hole width must stay slightly larger than face bolt width
        private static Mesh sharedFaceBoltMesh;

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Generate a mesh for the given part. The part's MeshSeed drives variation.
        /// </summary>
        public static Mesh GenerateMesh(BeyPart part)
        {
            if (part == null) return null;

            System.Random rng = new System.Random(part.MeshSeed);

            Mesh mesh = part.PartType switch
            {
                PartType.Tip => GenerateTipMesh(part, rng),
                PartType.Track => GenerateTrackMesh(part, rng),
                PartType.FusionWheel => GenerateFusionWheelMesh(part),
                PartType.EnergyRing => GenerateEnergyRingMesh(part, rng),
                PartType.FaceBolt => GenerateFaceBoltMesh(part, rng),
                _ => null
            };

            return mesh;
        }

        /// <summary>
        /// Generate an EnergyRing mesh constrained by neighboring parts.
        /// maxOuterRadius: ring cannot be wider than this (from FusionWheel).
        /// faceBoltRadius: used to enforce minimum hole width clearance.
        /// </summary>
        public static Mesh GenerateConstrainedEnergyRing(BeyPart part, float maxOuterRadius, float faceBoltRadius)
        {
            if (part == null || part.PartType != PartType.EnergyRing) return null;
            System.Random rng = new System.Random(part.MeshSeed);
            return GenerateEnergyRingMesh(part, rng, maxOuterRadius, faceBoltRadius);
        }

        /// <summary>
        /// Returns the canonical FaceBolt radius (all FaceBolts share one mesh template).
        /// Used by EnergyRing to size its inner hole.
        /// </summary>
        public static float GetFaceBoltRadius(BeyPart faceBolt = null)
        {
            return FACE_BOLT_RADIUS;
        }

        /// <summary>
        /// Returns the vertical height this part occupies, used for stacking.
        /// </summary>
        public static float GetPartHeight(BeyPart part)
        {
            if (part == null) return 0f;

            return part.PartType switch
            {
                PartType.Tip => GetTipHeight(part),
                PartType.Track => GetTrackHeight(part),
                PartType.FusionWheel => GetFusionWheelHeight(part),
                PartType.EnergyRing => GetEnergyRingHeight(part),
                PartType.FaceBolt => GetFaceBoltHeight(),
                _ => 0f
            };
        }

        // =====================================================================
        // TIP — shape from TipBehaviorType, proportions varied by seed
        // =====================================================================

        private static float GetTipHeight(BeyPart part)
        {
            return part.TipBehavior switch
            {
                TipBehaviorType.Spike => 0.050f,
                TipBehaviorType.Sharp => 0.055f,
                TipBehaviorType.Round => 0.052f,
                TipBehaviorType.Ball => 0.054f,
                TipBehaviorType.Orbit => 0.042f,
                TipBehaviorType.EternalSharp_ES => 0.064f,
                TipBehaviorType.BearingSpike_BS => 0.070f,
                TipBehaviorType.Sharp_S => 0.058f,
                TipBehaviorType.DefenseSharp_DS => 0.056f,
                TipBehaviorType.MetalSharp_MS => 0.054f,
                TipBehaviorType.EternalDefenseSharp_EDS => 0.060f,
                TipBehaviorType.CoatSharp_CS => 0.057f,
                TipBehaviorType.RubberSharp_RS => 0.056f,
                TipBehaviorType.FlatSharp_FS => 0.050f,

                TipBehaviorType.WideDefense_WD => 0.043f,
                TipBehaviorType.WideDefense2_W2D => 0.043f,
                TipBehaviorType.EternalWideDefense_EWD => 0.044f,
                TipBehaviorType.Defense_D => 0.048f,
                TipBehaviorType.SemiDefense_SD => 0.047f,
                TipBehaviorType.HoleFlat_HF => 0.044f,
                TipBehaviorType.Flat_F => 0.042f,
                TipBehaviorType.WideFlat_WF => 0.043f,
                TipBehaviorType.Fusion_F => 0.043f,
                TipBehaviorType.SemiFlat_SF => 0.044f,
                TipBehaviorType.Rubber2Flat_R2F => 0.043f,
                TipBehaviorType.HoleFlatSharp_HF_S => 0.046f,

                TipBehaviorType.MetalBall_MB => 0.055f,
                TipBehaviorType.Ball_B => 0.054f,
                TipBehaviorType.RubberBall_RB => 0.055f,

                TipBehaviorType.BearingDrive_B_D => 0.050f,
                TipBehaviorType.DeltaDrive_D_D => 0.052f,
                TipBehaviorType.Quake_Q => 0.050f,
                _ => 0.04f
            };
        }

        private static Mesh GenerateTipMesh(BeyPart part, System.Random rng)
        {
            // Seed-based proportion variation (±15%)
            float widthScale = 0.85f + (float)rng.NextDouble() * 0.3f;
            float heightScale = 0.85f + (float)rng.NextDouble() * 0.3f;

            Mesh tipMesh = part.TipBehavior switch
            {
                // More IRL-like layered profiles per tip family
                TipBehaviorType.Flat => GenerateFlatProfileTip(widthScale, heightScale, rubberStyle: false),
                TipBehaviorType.RubberFlat => GenerateFlatProfileTip(widthScale, heightScale, rubberStyle: true),
                TipBehaviorType.WideFlat_WF => GenerateFlatProfileTip(widthScale * 1.22f, heightScale * 0.94f, rubberStyle: false),
                TipBehaviorType.Fusion_F => GenerateFlatProfileTip(widthScale * 1.10f, heightScale * 0.96f, rubberStyle: false),
                TipBehaviorType.Sharp => GenerateSharpProfileTip(widthScale, heightScale, spikeStyle: false),
                TipBehaviorType.Spike => GenerateSpikeProfileTip(widthScale, heightScale),
                TipBehaviorType.Round => GenerateRoundTipProfile(widthScale, heightScale),
                TipBehaviorType.Ball => GenerateBallTipProfile(widthScale, heightScale),
                TipBehaviorType.RubberBall_RB => GenerateBallTipProfile(widthScale * 1.10f, heightScale * 1.02f),
                TipBehaviorType.Orbit => GenerateOrbitTip(widthScale, heightScale),

                // Catalog-specific silhouettes
                TipBehaviorType.WideDefense_WD => GenerateDefenseWideTip(widthScale * 1.18f, heightScale * 0.95f, extraWide: false),
                TipBehaviorType.WideDefense2_W2D => GenerateDefenseWideTip(widthScale * 1.24f, heightScale * 0.95f, extraWide: true),
                TipBehaviorType.EternalWideDefense_EWD => GenerateDefenseWideTip(widthScale * 1.20f, heightScale * 0.98f, extraWide: false),

                TipBehaviorType.EternalSharp_ES => GenerateSharpProfileTip(widthScale * 0.94f, heightScale * 1.20f, spikeStyle: false),
                TipBehaviorType.MetalSharp_MS => GenerateSharpProfileTip(widthScale * 1.02f, heightScale * 1.06f, spikeStyle: false),
                TipBehaviorType.EternalDefenseSharp_EDS => GenerateDefenseSharpTip(widthScale * 1.05f, heightScale * 1.14f),
                TipBehaviorType.DefenseSharp_DS => GenerateDefenseSharpTip(widthScale * 1.02f, heightScale * 1.08f),
                TipBehaviorType.Sharp_S => GenerateSharpProfileTip(widthScale * 0.96f, heightScale * 1.14f, spikeStyle: false),
                TipBehaviorType.CoatSharp_CS => GenerateDefenseSharpTip(widthScale * 1.00f, heightScale * 1.08f),
                TipBehaviorType.RubberSharp_RS => GenerateDefenseSharpTip(widthScale * 1.06f, heightScale * 1.04f),

                TipBehaviorType.SemiFlat_SF => GenerateFlatProfileTip(widthScale * 1.05f, heightScale * 0.95f, rubberStyle: false),
                TipBehaviorType.HoleFlat_HF => GenerateHoleFlatTip(widthScale * 1.04f, heightScale * 0.95f),
                TipBehaviorType.HoleFlatSharp_HF_S => GenerateHoleFlatSharpTip(widthScale * 1.03f, heightScale * 1.00f),
                TipBehaviorType.Flat_F => GenerateFlatProfileTip(widthScale * 1.08f, heightScale * 0.94f, rubberStyle: false),
                TipBehaviorType.Rubber2Flat_R2F => GenerateFlatProfileTip(widthScale * 1.12f, heightScale * 0.95f, rubberStyle: true),
                TipBehaviorType.FlatSharp_FS => GenerateFlatSharpHybridTip(widthScale * 1.00f, heightScale * 1.02f),

                TipBehaviorType.MetalBall_MB => GenerateBallTipProfile(widthScale * 1.08f, heightScale * 1.06f),
                TipBehaviorType.Ball_B => GenerateBallTipProfile(widthScale * 1.03f, heightScale * 1.03f),
                TipBehaviorType.Defense_D => GenerateDefenseWideTip(widthScale * 1.10f, heightScale * 1.00f, extraWide: false),

                TipBehaviorType.BearingSpike_BS => GenerateSharpProfileTip(widthScale * 0.96f, heightScale * 1.28f, spikeStyle: true),
                TipBehaviorType.BearingDrive_B_D => GenerateBearingDriveTip(widthScale * 1.02f, heightScale * 1.02f),
                TipBehaviorType.DeltaDrive_D_D => GenerateDeltaDriveTip(widthScale, heightScale),
                TipBehaviorType.Quake_Q => GenerateQuakeTip(widthScale * 1.06f, heightScale * 1.00f),
                _ => GenerateBallTipProfile(widthScale, heightScale)
            };

            // Ensure all tip meshes are oriented correctly and taper toward the bottom.
            // Disc/ring-contact profiles skip radial taper to preserve their flat contact shape.
            bool applyTaper = part.TipBehavior switch
            {
                TipBehaviorType.Spike           => false,
                TipBehaviorType.Round           => false,
                TipBehaviorType.Ball            => false,
                TipBehaviorType.Orbit           => false,
                TipBehaviorType.HoleFlat_HF     => false,
                TipBehaviorType.WideFlat_WF     => false,
                TipBehaviorType.MetalBall_MB    => false,
                TipBehaviorType.Ball_B          => false,
                TipBehaviorType.RubberBall_RB   => false,
                TipBehaviorType.BearingDrive_B_D => false,
                TipBehaviorType.DeltaDrive_D_D  => false,
                TipBehaviorType.Quake_Q         => false,
                _ => true
            };
            return ReorientAndTaperTipMesh(tipMesh, applyTaper);
        }

        private static Mesh GenerateFlatProfileTip(float widthScale, float heightScale, bool rubberStyle)
        {
            float baseTopRadius = (rubberStyle ? 0.040f : 0.036f) * widthScale;
            float baseBottomRadius = (rubberStyle ? 0.036f : 0.032f) * widthScale;
            float baseHeight = (rubberStyle ? 0.020f : 0.018f) * heightScale;
            float collarHeight = (rubberStyle ? 0.008f : 0.010f) * heightScale;
            float stemHeight = 0.012f * heightScale;
            float stemRadius = (rubberStyle ? 0.011f : 0.010f) * widthScale;

            CombineInstance[] combine = new CombineInstance[3];
            combine[0].mesh = GenerateCylinder(baseTopRadius, baseBottomRadius, baseHeight, TIP_SEGMENTS, capTop: true, capBottom: true);
            combine[0].transform = Matrix4x4.identity;

            combine[1].mesh = GenerateCylinder(baseBottomRadius * 0.84f, baseBottomRadius * 0.76f, collarHeight, TIP_SEGMENTS, capTop: true, capBottom: true);
            combine[1].transform = Matrix4x4.Translate(new Vector3(0f, baseHeight, 0f));

            combine[2].mesh = GenerateCylinder(stemRadius, stemRadius, stemHeight, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[2].transform = Matrix4x4.Translate(new Vector3(0f, baseHeight + collarHeight, 0f));

            return CombineTipMeshes("FlatProfileTip", combine);
        }

        private static Mesh GenerateSharpProfileTip(float widthScale, float heightScale, bool spikeStyle)
        {
            float needleBaseRadius = (spikeStyle ? 0.013f : 0.016f) * widthScale;
            float needleTipRadius = (spikeStyle ? 0.0015f : 0.003f) * widthScale;
            float needleHeight = (spikeStyle ? 0.038f : 0.024f) * heightScale;
            float collarRadius = (spikeStyle ? 0.017f : 0.020f) * widthScale;
            float collarHeight = 0.008f * heightScale;
            float stemRadius = (spikeStyle ? 0.010f : 0.011f) * widthScale;
            float stemHeight = (spikeStyle ? 0.028f : 0.022f) * heightScale;

            CombineInstance[] combine = new CombineInstance[3];
            combine[0].mesh = GenerateCone(needleBaseRadius, needleTipRadius, needleHeight, TIP_SEGMENTS);
            combine[0].transform = Matrix4x4.identity;

            combine[1].mesh = GenerateCylinder(collarRadius, needleBaseRadius, collarHeight, TIP_SEGMENTS, capTop: true, capBottom: true);
            combine[1].transform = Matrix4x4.Translate(new Vector3(0f, needleHeight, 0f));

            combine[2].mesh = GenerateCylinder(stemRadius, stemRadius, stemHeight, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[2].transform = Matrix4x4.Translate(new Vector3(0f, needleHeight + collarHeight, 0f));

            return CombineTipMeshes(spikeStyle ? "SpikeProfileTip" : "SharpProfileTip", combine);
        }

        private static Mesh GenerateRoundTipProfile(float widthScale, float heightScale)
        {
            // Round tip: slimmer rounded contact, sharper shoulder, less bulbous than Ball.
            float domeRadius = 0.0165f * widthScale;
            float lowerBandHeight = 0.007f * heightScale;
            float lowerBandRadius = 0.020f * widthScale;
            float flareHeight = 0.012f * heightScale;
            float flareTopRadius = 0.033f * widthScale;
            float stemRadius = 0.0125f * widthScale;
            float stemHeight = 0.017f * heightScale;

            CombineInstance[] combine = new CombineInstance[4];
            combine[0].mesh = GenerateHemisphere(domeRadius, TIP_SEGMENTS, TIP_SEGMENTS / 2, bottomHalf: true);
            combine[0].transform = Matrix4x4.Translate(new Vector3(0f, domeRadius, 0f));

            combine[1].mesh = GenerateCylinder(lowerBandRadius, domeRadius * 0.95f, lowerBandHeight, TIP_SEGMENTS, capTop: false, capBottom: false);
            combine[1].transform = Matrix4x4.Translate(new Vector3(0f, domeRadius, 0f));

            combine[2].mesh = GenerateCylinder(flareTopRadius, lowerBandRadius, flareHeight, TIP_SEGMENTS, capTop: false, capBottom: false);
            combine[2].transform = Matrix4x4.Translate(new Vector3(0f, domeRadius + lowerBandHeight, 0f));

            combine[3].mesh = GenerateCylinder(stemRadius, stemRadius, stemHeight, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[3].transform = Matrix4x4.Translate(new Vector3(0f, domeRadius + lowerBandHeight + flareHeight, 0f));

            return CombineTipMeshes("RoundProfileTip", combine);
        }

        private static Mesh GenerateBallTipProfile(float widthScale, float heightScale)
        {
            // Ball tip: fuller bulb contact and softer transition into the upper body.
            float bulbRadius = 0.0205f * widthScale;
            float lowerBandHeight = 0.008f * heightScale;
            float lowerBandRadius = 0.0225f * widthScale;
            float flareHeight = 0.011f * heightScale;
            float flareTopRadius = 0.034f * widthScale;
            float stemRadius = 0.013f * widthScale;
            float stemHeight = 0.016f * heightScale;

            CombineInstance[] combine = new CombineInstance[4];
            combine[0].mesh = GenerateHemisphere(bulbRadius, TIP_SEGMENTS, TIP_SEGMENTS / 2, bottomHalf: true);
            combine[0].transform = Matrix4x4.Translate(new Vector3(0f, bulbRadius, 0f));

            combine[1].mesh = GenerateCylinder(lowerBandRadius, bulbRadius * 0.98f, lowerBandHeight, TIP_SEGMENTS, capTop: false, capBottom: false);
            combine[1].transform = Matrix4x4.Translate(new Vector3(0f, bulbRadius, 0f));

            combine[2].mesh = GenerateCylinder(flareTopRadius, lowerBandRadius, flareHeight, TIP_SEGMENTS, capTop: false, capBottom: false);
            combine[2].transform = Matrix4x4.Translate(new Vector3(0f, bulbRadius + lowerBandHeight, 0f));

            combine[3].mesh = GenerateCylinder(stemRadius, stemRadius, stemHeight, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[3].transform = Matrix4x4.Translate(new Vector3(0f, bulbRadius + lowerBandHeight + flareHeight, 0f));

            return CombineTipMeshes("BallProfileTip", combine);
        }

        private static Mesh GenerateOrbitTip(float widthScale, float heightScale)
        {
            CombineInstance[] combine = new CombineInstance[4];

            combine[0].mesh = GenerateRing(0.030f * widthScale, 0.020f * widthScale, 0.012f * heightScale, TIP_SEGMENTS);
            combine[0].transform = Matrix4x4.identity;

            combine[1].mesh = GenerateCone(0.013f * widthScale, 0.004f * widthScale, 0.016f * heightScale, TIP_SEGMENTS);
            combine[1].transform = Matrix4x4.identity;

            combine[2].mesh = GenerateCylinder(0.012f * widthScale, 0.012f * widthScale, 0.012f * heightScale, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[2].transform = Matrix4x4.Translate(new Vector3(0f, 0.016f * heightScale, 0f));

            combine[3].mesh = GenerateCylinder(0.009f * widthScale, 0.009f * widthScale, 0.012f * heightScale, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[3].transform = Matrix4x4.Translate(new Vector3(0f, 0.028f * heightScale, 0f));

            return CombineTipMeshes("OrbitTip", combine);
        }

        private static Mesh GenerateDefenseWideTip(float widthScale, float heightScale, bool extraWide)
        {
            float plateTopRadius = (extraWide ? 0.041f : 0.038f) * widthScale;
            float plateBottomRadius = (extraWide ? 0.035f : 0.033f) * widthScale;
            float plateHeight = 0.011f * heightScale;
            float contactRadius = (extraWide ? 0.012f : 0.011f) * widthScale;
            float contactHeight = 0.010f * heightScale;
            float stemRadius = 0.010f * widthScale;
            float stemHeight = 0.014f * heightScale;

            CombineInstance[] combine = new CombineInstance[3];
            combine[0].mesh = GenerateCylinder(plateTopRadius, plateBottomRadius, plateHeight, TIP_SEGMENTS, capTop: true, capBottom: true);
            combine[0].transform = Matrix4x4.identity;

            combine[1].mesh = GenerateCylinder(contactRadius, contactRadius * 0.90f, contactHeight, TIP_SEGMENTS, capTop: true, capBottom: true);
            combine[1].transform = Matrix4x4.Translate(new Vector3(0f, plateHeight, 0f));

            combine[2].mesh = GenerateCylinder(stemRadius, stemRadius, stemHeight, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[2].transform = Matrix4x4.Translate(new Vector3(0f, plateHeight + contactHeight, 0f));

            return CombineTipMeshes(extraWide ? "WideDefense2Tip" : "WideDefenseTip", combine);
        }

        private static Mesh GenerateDefenseSharpTip(float widthScale, float heightScale)
        {
            float coneBaseRadius = 0.015f * widthScale;
            float coneTipRadius = 0.0025f * widthScale;
            float coneHeight = 0.024f * heightScale;
            float defenseCollarRadius = 0.022f * widthScale;
            float defenseCollarHeight = 0.010f * heightScale;
            float upperStemRadius = 0.011f * widthScale;
            float upperStemHeight = 0.018f * heightScale;

            CombineInstance[] combine = new CombineInstance[3];
            combine[0].mesh = GenerateCone(coneBaseRadius, coneTipRadius, coneHeight, TIP_SEGMENTS);
            combine[0].transform = Matrix4x4.identity;

            combine[1].mesh = GenerateCylinder(defenseCollarRadius, coneBaseRadius, defenseCollarHeight, TIP_SEGMENTS, capTop: true, capBottom: true);
            combine[1].transform = Matrix4x4.Translate(new Vector3(0f, coneHeight, 0f));

            combine[2].mesh = GenerateCylinder(upperStemRadius, upperStemRadius, upperStemHeight, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[2].transform = Matrix4x4.Translate(new Vector3(0f, coneHeight + defenseCollarHeight, 0f));

            return CombineTipMeshes("DefenseSharpTip", combine);
        }

        private static Mesh GenerateFlatSharpHybridTip(float widthScale, float heightScale)
        {
            float baseTopRadius = 0.030f * widthScale;
            float baseBottomRadius = 0.026f * widthScale;
            float baseHeight = 0.012f * heightScale;
            float coneBaseRadius = 0.010f * widthScale;
            float coneTipRadius = 0.0035f * widthScale;
            float coneHeight = 0.014f * heightScale;
            float stemRadius = 0.010f * widthScale;
            float stemHeight = 0.014f * heightScale;

            CombineInstance[] combine = new CombineInstance[3];
            combine[0].mesh = GenerateCylinder(baseTopRadius, baseBottomRadius, baseHeight, TIP_SEGMENTS, capTop: true, capBottom: true);
            combine[0].transform = Matrix4x4.identity;

            combine[1].mesh = GenerateCone(coneBaseRadius, coneTipRadius, coneHeight, TIP_SEGMENTS);
            combine[1].transform = Matrix4x4.Translate(new Vector3(0f, baseHeight, 0f));

            combine[2].mesh = GenerateCylinder(stemRadius, stemRadius, stemHeight, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[2].transform = Matrix4x4.Translate(new Vector3(0f, baseHeight + coneHeight, 0f));

            return CombineTipMeshes("FlatSharpTip", combine);
        }

        private static Mesh GenerateHoleFlatTip(float widthScale, float heightScale)
        {
            CombineInstance[] combine = new CombineInstance[3];

            combine[0].mesh = GenerateRing(0.032f * widthScale, 0.018f * widthScale, 0.010f * heightScale, TIP_SEGMENTS);
            combine[0].transform = Matrix4x4.identity;

            combine[1].mesh = GenerateCylinder(0.013f * widthScale, 0.010f * widthScale, 0.010f * heightScale, TIP_SEGMENTS, capTop: true, capBottom: true);
            combine[1].transform = Matrix4x4.Translate(new Vector3(0f, 0.010f * heightScale, 0f));

            combine[2].mesh = GenerateCylinder(0.010f * widthScale, 0.010f * widthScale, 0.012f * heightScale, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[2].transform = Matrix4x4.Translate(new Vector3(0f, 0.020f * heightScale, 0f));

            return CombineTipMeshes("HoleFlatTip", combine);
        }

        private static Mesh GenerateHoleFlatSharpTip(float widthScale, float heightScale)
        {
            // HF/S hybrid: hole-flat ring body with a sharper lower contact cone.
            CombineInstance[] combine = new CombineInstance[4];

            combine[0].mesh = GenerateRing(0.031f * widthScale, 0.018f * widthScale, 0.009f * heightScale, TIP_SEGMENTS);
            combine[0].transform = Matrix4x4.identity;

            combine[1].mesh = GenerateCone(0.0115f * widthScale, 0.0026f * widthScale, 0.010f * heightScale, TIP_SEGMENTS);
            combine[1].transform = Matrix4x4.Translate(new Vector3(0f, 0.009f * heightScale, 0f));

            combine[2].mesh = GenerateCylinder(0.0125f * widthScale, 0.0105f * widthScale, 0.009f * heightScale, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[2].transform = Matrix4x4.Translate(new Vector3(0f, 0.019f * heightScale, 0f));

            combine[3].mesh = GenerateCylinder(0.0095f * widthScale, 0.0095f * widthScale, 0.011f * heightScale, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[3].transform = Matrix4x4.Translate(new Vector3(0f, 0.028f * heightScale, 0f));

            return CombineTipMeshes("HoleFlatSharpTip", combine);
        }

        private static Mesh GenerateBearingDriveTip(float widthScale, float heightScale)
        {
            CombineInstance[] combine = new CombineInstance[4];

            combine[0].mesh = GenerateRing(0.028f * widthScale, 0.019f * widthScale, 0.010f * heightScale, TIP_SEGMENTS);
            combine[0].transform = Matrix4x4.identity;

            combine[1].mesh = GenerateBallTipProfile(0.70f * widthScale, 0.85f * heightScale);
            combine[1].transform = Matrix4x4.identity;

            combine[2].mesh = GenerateCylinder(0.011f * widthScale, 0.011f * widthScale, 0.010f * heightScale, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[2].transform = Matrix4x4.Translate(new Vector3(0f, 0.020f * heightScale, 0f));

            combine[3].mesh = GenerateCylinder(0.009f * widthScale, 0.009f * widthScale, 0.010f * heightScale, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[3].transform = Matrix4x4.Translate(new Vector3(0f, 0.030f * heightScale, 0f));

            return CombineTipMeshes("BearingDriveTip", combine);
        }

        private static Mesh GenerateDeltaDriveTip(float widthScale, float heightScale)
        {
            CombineInstance[] combine = new CombineInstance[4];

            combine[0].mesh = GenerateCylinder(0.022f * widthScale, 0.020f * widthScale, 0.012f * heightScale, 3, capTop: true, capBottom: true);
            combine[0].transform = Matrix4x4.identity;

            combine[1].mesh = GenerateCone(0.013f * widthScale, 0.0035f * widthScale, 0.014f * heightScale, TIP_SEGMENTS);
            combine[1].transform = Matrix4x4.identity;

            combine[2].mesh = GenerateCylinder(0.011f * widthScale, 0.011f * widthScale, 0.010f * heightScale, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[2].transform = Matrix4x4.Translate(new Vector3(0f, 0.014f * heightScale, 0f));

            combine[3].mesh = GenerateCylinder(0.009f * widthScale, 0.009f * widthScale, 0.010f * heightScale, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[3].transform = Matrix4x4.Translate(new Vector3(0f, 0.024f * heightScale, 0f));

            return CombineTipMeshes("DeltaDriveTip", combine);
        }

        private static Mesh GenerateQuakeTip(float widthScale, float heightScale)
        {
            CombineInstance[] combine = new CombineInstance[4];

            combine[0].mesh = GenerateRing(0.031f * widthScale, 0.020f * widthScale, 0.010f * heightScale, TIP_SEGMENTS);
            combine[0].transform = Matrix4x4.identity;

            combine[1].mesh = GenerateCone(0.014f * widthScale, 0.004f * widthScale, 0.012f * heightScale, 5);
            combine[1].transform = Matrix4x4.identity;

            combine[2].mesh = GenerateCylinder(0.0105f * widthScale, 0.0105f * widthScale, 0.011f * heightScale, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[2].transform = Matrix4x4.Translate(new Vector3(0f, 0.012f * heightScale, 0f));

            combine[3].mesh = GenerateCylinder(0.0085f * widthScale, 0.0085f * widthScale, 0.010f * heightScale, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[3].transform = Matrix4x4.Translate(new Vector3(0f, 0.023f * heightScale, 0f));

            return CombineTipMeshes("QuakeTip", combine);
        }

        private static Mesh GenerateSpikeProfileTip(float widthScale, float heightScale)
        {
            // IRL Spike tip profile (y=0 = contact point, y=top = Track interface):
            //   BOTTOM (y=0): true sharp spike apex
            //   spike cone  : widens upward to body radius
            //   waist       : short bridge cylinder
            //   flare       : body expands out to wide disc width
            //   TOP cap     : wide flat disc — same profile as flat tip, sits in Track barrel
            float spikeH     = 0.024f * heightScale;
            float spikeBaseR = 0.012f * widthScale;
            float waistH     = 0.005f * heightScale;
            float flareH     = 0.012f * heightScale;
            float wideTopR   = 0.032f * widthScale;
            float capH       = 0.010f * heightScale;

            float y0 = 0f;
            float y1 = y0 + spikeH;
            float y2 = y1 + waistH;
            float y3 = y2 + flareH;

            CombineInstance[] combine = new CombineInstance[4];

            // True sharp spike — apex (r≈0) at y=0, widens to spikeBaseR at y1
            combine[0].mesh = GenerateCone(spikeBaseR, 0.0008f, spikeH, TIP_SEGMENTS);
            combine[0].transform = Matrix4x4.Translate(new Vector3(0f, y0, 0f));

            // Short waist cylinder bridging spike base to flare
            combine[1].mesh = GenerateCylinder(spikeBaseR, spikeBaseR, waistH, TIP_SEGMENTS, capTop: false, capBottom: false);
            combine[1].transform = Matrix4x4.Translate(new Vector3(0f, y1, 0f));

            // Flare: widens from waist radius up to wide top radius
            combine[2].mesh = GenerateCylinder(wideTopR, spikeBaseR, flareH, TIP_SEGMENTS, capTop: false, capBottom: false);
            combine[2].transform = Matrix4x4.Translate(new Vector3(0f, y2, 0f));

            // Wide flat top cap — same as flat tip top; slots inside Track barrel
            combine[3].mesh = GenerateCylinder(wideTopR, wideTopR, capH, TIP_SEGMENTS, capTop: true, capBottom: false);
            combine[3].transform = Matrix4x4.Translate(new Vector3(0f, y3, 0f));

            return CombineTipMeshes("SpikeProfileTip", combine);
        }

        private static Mesh CombineTipMeshes(string name, CombineInstance[] combine)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.CombineMeshes(combine, true, true);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh ReorientAndTaperTipMesh(Mesh mesh, bool applyTaper = true)
        {
            if (mesh == null)
                return null;

            Vector3[] verts = mesh.vertices;

            // Orientation is now profile-dependent. Some generated tips are already correct,
            // while others may be inverted. Detect inversion by comparing radial profile:
            // the contact side should generally be narrower than the top side.
            float preMinY = float.MaxValue;
            float preMaxY = float.MinValue;
            for (int i = 0; i < verts.Length; i++)
            {
                if (verts[i].y < preMinY) preMinY = verts[i].y;
                if (verts[i].y > preMaxY) preMaxY = verts[i].y;
            }

            float bottomRadius = AverageRadialInBand(verts, preMinY, preMaxY, topBand: false);
            float topRadius = AverageRadialInBand(verts, preMinY, preMaxY, topBand: true);

            if (bottomRadius > topRadius * 1.02f)
            {
                Quaternion rotation = Quaternion.Euler(180f, 0f, 0f);
                for (int i = 0; i < verts.Length; i++)
                    verts[i] = rotation * verts[i];
            }

            float minY = float.MaxValue;
            for (int i = 0; i < verts.Length; i++)
                if (verts[i].y < minY) minY = verts[i].y;

            Vector3 yOffset = new Vector3(0f, -minY, 0f);
            for (int i = 0; i < verts.Length; i++)
                verts[i] += yOffset;

            // IRL-like tip profile rule: lower points should have smaller diameter.
            // Apply a smooth, monotonic taper along Y so XZ radius shrinks toward the bottom.
            float maxY = float.MinValue;
            for (int i = 0; i < verts.Length; i++)
                if (verts[i].y > maxY) maxY = verts[i].y;

            if (applyTaper && maxY > 0.0001f)
            {
                const float bottomScale = 0.62f;
                for (int i = 0; i < verts.Length; i++)
                {
                    float y01 = Mathf.Clamp01(verts[i].y / maxY);
                    float radialScale = Mathf.Lerp(bottomScale, 1f, y01);
                    verts[i] = new Vector3(verts[i].x * radialScale, verts[i].y, verts[i].z * radialScale);
                }
            }

            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float AverageRadialInBand(Vector3[] verts, float minY, float maxY, bool topBand)
        {
            if (verts == null || verts.Length == 0)
                return 0f;

            float height = Mathf.Max(0.0001f, maxY - minY);
            float bandHeight = height * 0.15f;

            float bandMin = topBand ? (maxY - bandHeight) : minY;
            float bandMax = topBand ? maxY : (minY + bandHeight);

            float radialSum = 0f;
            int count = 0;

            for (int i = 0; i < verts.Length; i++)
            {
                float y = verts[i].y;
                if (y < bandMin || y > bandMax) continue;

                radialSum += Mathf.Sqrt(verts[i].x * verts[i].x + verts[i].z * verts[i].z);
                count++;
            }

            if (count == 0)
                return 0f;

            return radialSum / count;
        }

        // =====================================================================
        // TRACK — fluted cylinder, ridge count from seed
        // =====================================================================

        private static float GetTrackHeight(BeyPart part)
        {
            // Use the part's TrackHeight stat directly as the mesh height.
            // PartSetGenerator produces values in [MIN_TRACK_HEIGHT, MAX_TRACK_HEIGHT].
            return Mathf.Clamp(part.TrackHeight, GameConstants.MIN_TRACK_HEIGHT, GameConstants.MAX_TRACK_HEIGHT);
        }

        private static Mesh GenerateTrackMesh(BeyPart part, System.Random rng)
        {
            float height = GetTrackHeight(part);
            float topRadius = 0.04f;
            float bottomRadius = 0.03f;

            // Symmetry planes (1–2) from seed
            int symmetryPlanes = 1 + rng.Next(0, 2);

            // Seed-based: number of flutes (ridges) along the outside
            int ridgeCount = rng.Next(0, 7); // 0 = smooth, up to 6 ridges
            float ridgeDepth = 0.003f + (float)rng.NextDouble() * 0.007f; // 0.003–0.01
            float taperVariation = 0.9f + (float)rng.NextDouble() * 0.2f;
            topRadius *= taperVariation;

            if (ridgeCount == 0)
            {
                return GenerateCylinder(topRadius, bottomRadius, height, RING_SEGMENTS, capTop: true, capBottom: true);
            }

            // Fluted track: modulate the radius per segment
            float[] topRadii = new float[RING_SEGMENTS];
            float[] bottomRadii = new float[RING_SEGMENTS];
            for (int i = 0; i < RING_SEGMENTS; i++)
            {
                float angle = (float)i / RING_SEGMENTS * Mathf.PI * 2f;
                float ridge = Mathf.Abs(Mathf.Sin(angle * ridgeCount)) * ridgeDepth;
                topRadii[i] = topRadius + ridge;
                bottomRadii[i] = bottomRadius + ridge;
            }

            EnforceSymmetry(topRadii, symmetryPlanes);
            EnforceSymmetry(bottomRadii, symmetryPlanes);

            return GenerateModulatedCylinder(topRadii, bottomRadii, height, RING_SEGMENTS, capTop: true, capBottom: true);
        }

        // =====================================================================
        // FUSION WHEEL — ringed disc with blade protrusions, count/shape from seed
        // =====================================================================

        private static float GetFusionWheelHeight(BeyPart part)
        {
            float t = Mathf.InverseLerp(GameConstants.MIN_WEIGHT, GameConstants.MAX_WEIGHT, part.Weight);
            return Mathf.Lerp(0.03f, 0.07f, t);
        }

        private static Mesh GenerateFusionWheelMesh(BeyPart part)
        {
            float t = Mathf.InverseLerp(GameConstants.MIN_WEIGHT, GameConstants.MAX_WEIGHT, part.Weight);
            float baseOuterRadius = Mathf.Lerp(0.1f, 0.18f, t);
            float height = GetFusionWheelHeight(part);
            FusionWheelCombatProfile profile =
                FusionWheelCombatProfile.FromPart(part);

            // Generate per-segment outer radius
            float[] outerRadii = new float[RING_SEGMENTS];
            for (int i = 0; i < RING_SEGMENTS; i++)
            {
                float segAngle = (float)i / RING_SEGMENTS; // 0–1 range

                float maxBladeFactor = 0f;
                for (int b = 0; b < profile.BladeCount; b++)
                {
                    float bladeCenter =
                        ((float)b / profile.BladeCount + profile.BladeSweep) % 1f;
                    float dist = Mathf.Abs(segAngle - bladeCenter);
                    dist = Mathf.Min(dist, 1f - dist); // wrap around

                    float halfWidth =
                        profile.BladeWidth / (2f * profile.BladeCount);
                    if (dist < halfWidth)
                    {
                        // Smooth falloff from blade center
                        float bladeFactor = 1f - (dist / halfWidth);
                        bladeFactor = bladeFactor * bladeFactor; // quadratic falloff for nicer shape
                        maxBladeFactor = Mathf.Max(maxBladeFactor, bladeFactor);
                    }
                }

                outerRadii[i] =
                    baseOuterRadius
                    + profile.BladeProtrusion * maxBladeFactor;
            }

            // Enforce symmetry so blades are evenly mirrored
            EnforceSymmetry(outerRadii, profile.SymmetryPlanes);

            // Fusion Wheels are solid metal cores (no center hole).
            return GenerateModulatedSolidDisc(outerRadii, height, RING_SEGMENTS);
        }

        private static Mesh GenerateModulatedSolidDisc(float[] outerRadii, float height, int segments)
        {
            Mesh mesh = new Mesh();
            mesh.name = "ModulatedSolidDisc";

            int vCount = (segments + 1) * 2 + 2; // side rings + top center + bottom center
            Vector3[] vertices = new Vector3[vCount];
            Vector3[] normals = new Vector3[vCount];
            Vector2[] uvs = new Vector2[vCount];

            int topCenter = (segments + 1) * 2;
            int bottomCenter = topCenter + 1;

            for (int i = 0; i <= segments; i++)
            {
                int seg = i % segments;
                float angle = (Mathf.PI * 2f * i) / segments;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                float radius = outerRadii[seg];

                int topOuter = i * 2;
                int bottomOuter = topOuter + 1;

                vertices[topOuter] = new Vector3(cos * radius, height, sin * radius);
                vertices[bottomOuter] = new Vector3(cos * radius, 0f, sin * radius);

                Vector3 radial = new Vector3(cos, 0f, sin);
                normals[topOuter] = radial;
                normals[bottomOuter] = radial;

                uvs[topOuter] = new Vector2((float)i / segments, 1f);
                uvs[bottomOuter] = new Vector2((float)i / segments, 0f);
            }

            vertices[topCenter] = new Vector3(0f, height, 0f);
            vertices[bottomCenter] = new Vector3(0f, 0f, 0f);
            normals[topCenter] = Vector3.up;
            normals[bottomCenter] = Vector3.down;
            uvs[topCenter] = new Vector2(0.5f, 0.5f);
            uvs[bottomCenter] = new Vector2(0.5f, 0.5f);

            int[] triangles = new int[segments * 12]; // side (6) + top cap (3) + bottom cap (3)
            int tri = 0;

            // Side faces
            for (int i = 0; i < segments; i++)
            {
                int currTop = i * 2;
                int currBottom = currTop + 1;
                int nextTop = (i + 1) * 2;
                int nextBottom = nextTop + 1;

                triangles[tri++] = currTop;
                triangles[tri++] = nextTop;
                triangles[tri++] = nextBottom;

                triangles[tri++] = currTop;
                triangles[tri++] = nextBottom;
                triangles[tri++] = currBottom;
            }

            // Top cap
            for (int i = 0; i < segments; i++)
            {
                int currTop = i * 2;
                int nextTop = (i + 1) * 2;

                triangles[tri++] = topCenter;
                triangles[tri++] = nextTop;
                triangles[tri++] = currTop;
            }

            // Bottom cap
            for (int i = 0; i < segments; i++)
            {
                int currBottom = i * 2 + 1;
                int nextBottom = (i + 1) * 2 + 1;

                triangles[tri++] = bottomCenter;
                triangles[tri++] = currBottom;
                triangles[tri++] = nextBottom;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // =====================================================================
        // ENERGY RING — thin ring with wavy/scalloped edge from seed
        // =====================================================================

        private static float GetEnergyRingHeight(BeyPart part)
        {
            return 0.01f;
        }

        private static Mesh GenerateEnergyRingMesh(BeyPart part, System.Random rng,
            float maxOuterRadius = float.MaxValue, float faceBoltRadius = 0.025f)
        {
            float t = Mathf.InverseLerp(GameConstants.MIN_MANA_POOL, GameConstants.MAX_MANA_POOL, part.ManaPoolSize);
            float baseOuterRadius = Mathf.Lerp(0.11f, 0.20f, t);

            // Clamp outer radius so the ring sits inside the FusionWheel with a visible margin
            const float RING_MARGIN = 0.015f; // inset from fusion wheel edge
            baseOuterRadius = Mathf.Min(baseOuterRadius, maxOuterRadius - RING_MARGIN);

            // Inner radius (hole) must always stay slightly wider (diameter) than FaceBolt.
            // Diameter clearance +0.03 means radius clearance +0.015.
            float minHoleRadiusFromFaceBolt = Mathf.Max(0.005f, faceBoltRadius + ENERGY_RING_HOLE_EXTRA_DIAMETER * 0.5f);

            // Keep some ring thickness even at minimum outer radius.
            float maxAllowedInnerRadius = baseOuterRadius - 0.01f;

            // Start from procedural default, then enforce minimum hole clearance.
            float innerRadius = Mathf.Max(baseOuterRadius * 0.3f, minHoleRadiusFromFaceBolt);
            innerRadius = Mathf.Clamp(innerRadius, 0.005f, maxAllowedInnerRadius);

            float height = GetEnergyRingHeight(part);

            // Symmetry planes (1–2) from seed
            int symmetryPlanes = 1 + rng.Next(0, 2);

            // Seed-driven wave pattern
            int waveCount = 4 + rng.Next(0, 9); // 4–12 waves
            float waveAmplitude = 0.005f + (float)rng.NextDouble() * 0.015f; // 0.005–0.02
            bool spiky = rng.NextDouble() > 0.5; // smooth vs angular waves

            float[] outerRadii = new float[RING_SEGMENTS];
            for (int i = 0; i < RING_SEGMENTS; i++)
            {
                float angle = (float)i / RING_SEGMENTS * Mathf.PI * 2f;
                float wave;
                if (spiky)
                {
                    // Triangular wave for angular/spiky look
                    wave = Mathf.Abs(Mathf.Repeat(angle * waveCount / (Mathf.PI * 2f), 1f) * 2f - 1f);
                }
                else
                {
                    // Smooth sinusoidal wave
                    wave = (Mathf.Sin(angle * waveCount) + 1f) * 0.5f;
                }
                // Clamp each segment so it stays inside the fusion wheel with margin
                outerRadii[i] = Mathf.Min(baseOuterRadius + waveAmplitude * wave, maxOuterRadius - RING_MARGIN);
            }

            // Enforce symmetry so the ring edge is evenly mirrored
            EnforceSymmetry(outerRadii, symmetryPlanes);

            return GenerateModulatedRing(outerRadii, innerRadius, height, RING_SEGMENTS);
        }

        // =====================================================================
        // FACE BOLT — shared canonical hex mesh (same geometry for every FaceBolt)
        // Only color, emblem sprite, name, and ability vary between FaceBolts.
        // =====================================================================

        private const float FACE_BOLT_HEIGHT = 0.015f;

        private static float GetFaceBoltHeight()
        {
            return FACE_BOLT_HEIGHT;
        }

        private static Mesh GenerateFaceBoltMesh(BeyPart part, System.Random rng)
        {
            if (sharedFaceBoltMesh == null)
            {
                sharedFaceBoltMesh = GenerateCylinder(FACE_BOLT_RADIUS, FACE_BOLT_RADIUS, FACE_BOLT_HEIGHT, 6, capTop: true, capBottom: false);
                sharedFaceBoltMesh.name = "SharedFaceBoltHex";
            }

            return sharedFaceBoltMesh;
        }

        // =====================================================================
        // PRIMITIVE MESH BUILDERS — all with correct CW winding for Unity
        // =====================================================================

        /// <summary>
        /// Enforces N-plane symmetry on a per-segment radius array.
        /// Generates one mirrored slice from the first 1/N of the array, then repeats it.
        /// symmetryPlanes: 1 = bilateral, 2 = 4-fold, 3 = 6-fold, 4 = 8-fold.
        /// </summary>
        private static void EnforceSymmetry(float[] radii, int symmetryPlanes)
        {
            int n = radii.Length;
            if (symmetryPlanes < 1) symmetryPlanes = 1;
            int sliceSize = n / symmetryPlanes;
            if (sliceSize < 2) return;

            int half = sliceSize / 2;

            // Mirror the first half of slice 0 onto the second half
            for (int i = 0; i < half; i++)
            {
                int mirror = sliceSize - 1 - i;
                if (mirror > i && mirror < sliceSize)
                {
                    float avg = (radii[i] + radii[mirror]) * 0.5f;
                    radii[i] = avg;
                    radii[mirror] = avg;
                }
            }

            // Copy slice 0 to all other slices
            for (int s = 1; s < symmetryPlanes; s++)
            {
                for (int i = 0; i < sliceSize; i++)
                {
                    int idx = s * sliceSize + i;
                    if (idx < n)
                        radii[idx] = radii[i];
                }
            }

            // Fill any remainder segments (from integer division)
            int filled = symmetryPlanes * sliceSize;
            for (int i = filled; i < n; i++)
                radii[i] = radii[i - sliceSize];
        }

        /// <summary>
        /// Generates a cylinder/tapered cylinder. Bottom at y=0, top at y=height.
        /// Uniform radius per ring.
        /// </summary>
        private static Mesh GenerateCylinder(float topRadius, float bottomRadius, float height,
            int segments, bool capTop, bool capBottom)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Cylinder";

            int vertCount = (segments + 1) * 2;
            if (capTop) vertCount += segments + 1;
            if (capBottom) vertCount += segments + 1;

            Vector3[] verts = new Vector3[vertCount];
            int triCount = segments * 6;
            if (capTop) triCount += segments * 3;
            if (capBottom) triCount += segments * 3;
            int[] tris = new int[triCount];

            int v = 0, t = 0;

            // --- Side vertices ---
            int sideStart = v;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                verts[v++] = new Vector3(cos * bottomRadius, 0, sin * bottomRadius);
                verts[v++] = new Vector3(cos * topRadius, height, sin * topRadius);
            }

            // --- Side triangles (outward-facing CW) ---
            for (int i = 0; i < segments; i++)
            {
                int bot0 = sideStart + i * 2;
                int top0 = bot0 + 1;
                int bot1 = bot0 + 2;
                int top1 = bot0 + 3;

                tris[t++] = bot0; tris[t++] = top0; tris[t++] = bot1;
                tris[t++] = top0; tris[t++] = top1; tris[t++] = bot1;
            }

            // --- Top cap (upward-facing) ---
            if (capTop)
            {
                int centerIdx = v;
                verts[v++] = new Vector3(0, height, 0);
                int capStart = v;
                for (int i = 0; i < segments; i++)
                {
                    float angle = (float)i / segments * Mathf.PI * 2f;
                    verts[v++] = new Vector3(Mathf.Cos(angle) * topRadius, height, Mathf.Sin(angle) * topRadius);
                }
                for (int i = 0; i < segments; i++)
                {
                    tris[t++] = centerIdx;
                    tris[t++] = capStart + (i + 1) % segments;
                    tris[t++] = capStart + i;
                }
            }

            // --- Bottom cap (downward-facing) ---
            if (capBottom)
            {
                int centerIdx = v;
                verts[v++] = new Vector3(0, 0, 0);
                int capStart = v;
                for (int i = 0; i < segments; i++)
                {
                    float angle = (float)i / segments * Mathf.PI * 2f;
                    verts[v++] = new Vector3(Mathf.Cos(angle) * bottomRadius, 0, Mathf.Sin(angle) * bottomRadius);
                }
                for (int i = 0; i < segments; i++)
                {
                    tris[t++] = centerIdx;
                    tris[t++] = capStart + i;
                    tris[t++] = capStart + (i + 1) % segments;
                }
            }

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Generates a cylinder where each segment can have a different radius.
        /// Used for fluted tracks.
        /// </summary>
        private static Mesh GenerateModulatedCylinder(float[] topRadii, float[] bottomRadii, float height,
            int segments, bool capTop, bool capBottom)
        {
            Mesh mesh = new Mesh();
            mesh.name = "ModulatedCylinder";

            int vertCount = (segments + 1) * 2;
            if (capTop) vertCount += segments + 1;
            if (capBottom) vertCount += segments + 1;

            Vector3[] verts = new Vector3[vertCount];
            int triCount = segments * 6;
            if (capTop) triCount += segments * 3;
            if (capBottom) triCount += segments * 3;
            int[] tris = new int[triCount];

            int v = 0, t = 0;

            int sideStart = v;
            for (int i = 0; i <= segments; i++)
            {
                int idx = i % segments;
                float angle = (float)i / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                verts[v++] = new Vector3(cos * bottomRadii[idx], 0, sin * bottomRadii[idx]);
                verts[v++] = new Vector3(cos * topRadii[idx], height, sin * topRadii[idx]);
            }

            for (int i = 0; i < segments; i++)
            {
                int bot0 = sideStart + i * 2;
                int top0 = bot0 + 1;
                int bot1 = bot0 + 2;
                int top1 = bot0 + 3;

                tris[t++] = bot0; tris[t++] = top0; tris[t++] = bot1;
                tris[t++] = top0; tris[t++] = top1; tris[t++] = bot1;
            }

            if (capTop)
            {
                int centerIdx = v;
                verts[v++] = new Vector3(0, height, 0);
                int capStart = v;
                for (int i = 0; i < segments; i++)
                {
                    float angle = (float)i / segments * Mathf.PI * 2f;
                    verts[v++] = new Vector3(Mathf.Cos(angle) * topRadii[i], height, Mathf.Sin(angle) * topRadii[i]);
                }
                for (int i = 0; i < segments; i++)
                {
                    tris[t++] = centerIdx;
                    tris[t++] = capStart + (i + 1) % segments;
                    tris[t++] = capStart + i;
                }
            }

            if (capBottom)
            {
                int centerIdx = v;
                verts[v++] = new Vector3(0, 0, 0);
                int capStart = v;
                for (int i = 0; i < segments; i++)
                {
                    float angle = (float)i / segments * Mathf.PI * 2f;
                    verts[v++] = new Vector3(Mathf.Cos(angle) * bottomRadii[i], 0, Mathf.Sin(angle) * bottomRadii[i]);
                }
                for (int i = 0; i < segments; i++)
                {
                    tris[t++] = centerIdx;
                    tris[t++] = capStart + i;
                    tris[t++] = capStart + (i + 1) % segments;
                }
            }

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Generates a cone with the narrow tip at y=0 (pointing down) and wide base at y=height.
        /// This ensures tips always point downward toward the ground.
        /// </summary>
        private static Mesh GenerateCone(float baseRadius, float tipRadius, float height, int segments)
        {
            // tipRadius at bottom (y=0), baseRadius at top (y=height)
            // capBottom seals the narrow tip, top left open for connection to next part
            return GenerateCylinder(baseRadius, tipRadius, height, segments, capTop: false, capBottom: true);
        }

        /// <summary>
        /// Generates a hemisphere. If bottomHalf, dome opens upward (south pole at bottom).
        /// </summary>
        private static Mesh GenerateHemisphere(float radius, int longSegments, int latSegments, bool bottomHalf)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Hemisphere";

            int vertCount = (longSegments + 1) * (latSegments + 1);
            Vector3[] verts = new Vector3[vertCount];
            int[] tris = new int[longSegments * latSegments * 6];

            int v = 0;
            for (int lat = 0; lat <= latSegments; lat++)
            {
                float latAngle;
                if (bottomHalf)
                    latAngle = -Mathf.PI / 2f + (float)lat / latSegments * (Mathf.PI / 2f);
                else
                    latAngle = (float)lat / latSegments * (Mathf.PI / 2f);

                float y = Mathf.Sin(latAngle) * radius;
                float ringRadius = Mathf.Cos(latAngle) * radius;

                for (int lon = 0; lon <= longSegments; lon++)
                {
                    float lonAngle = (float)lon / longSegments * Mathf.PI * 2f;
                    verts[v++] = new Vector3(
                        Mathf.Cos(lonAngle) * ringRadius,
                        y,
                        Mathf.Sin(lonAngle) * ringRadius);
                }
            }

            int t = 0;
            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < longSegments; lon++)
                {
                    int current = lat * (longSegments + 1) + lon;
                    int next = current + longSegments + 1;

                    tris[t++] = current;
                    tris[t++] = next;
                    tris[t++] = current + 1;

                    tris[t++] = current + 1;
                    tris[t++] = next;
                    tris[t++] = next + 1;
                }
            }

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Generates a ring (hollow cylinder) with uniform outer/inner radii.
        /// </summary>
        private static Mesh GenerateRing(float outerRadius, float innerRadius, float height, int segments)
        {
            float[] outerRadii = new float[segments];
            for (int i = 0; i < segments; i++)
                outerRadii[i] = outerRadius;
            return GenerateModulatedRing(outerRadii, innerRadius, height, segments);
        }

        /// <summary>
        /// Generates a ring with per-segment outer radius variation.
        /// Used for bladed fusion wheels and wavy energy rings.
        /// Inner radius is uniform. Bottom at y=0, top at y=height.
        /// </summary>
        private static Mesh GenerateModulatedRing(float[] outerRadii, float innerRadius, float height, int segments)
        {
            Mesh mesh = new Mesh();
            mesh.name = "ModulatedRing";

            int ringVerts = segments + 1;
            // 4 side surfaces (outer bot/top, inner bot/top) + 2 cap surfaces (top/bottom)
            int vertCount = ringVerts * 4  // outer + inner sides
                          + ringVerts * 4; // top + bottom caps
            Vector3[] verts = new Vector3[vertCount];

            int triCount = segments * 6 * 2  // outer + inner sides
                         + segments * 6 * 2; // top + bottom caps
            int[] tris = new int[triCount];

            int v = 0, t = 0;

            // === OUTER SIDE ===
            int outerStart = v;
            for (int i = 0; i <= segments; i++)
            {
                int idx = i % segments;
                float angle = (float)i / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                float outerR = outerRadii[idx];

                verts[v++] = new Vector3(cos * outerR, 0, sin * outerR);       // bottom
                verts[v++] = new Vector3(cos * outerR, height, sin * outerR);   // top
            }

            // Outer side tris (outward-facing)
            for (int i = 0; i < segments; i++)
            {
                int b0 = outerStart + i * 2;
                tris[t++] = b0;     tris[t++] = b0 + 1; tris[t++] = b0 + 2;
                tris[t++] = b0 + 1; tris[t++] = b0 + 3; tris[t++] = b0 + 2;
            }

            // === INNER SIDE ===
            int innerStart = v;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                verts[v++] = new Vector3(cos * innerRadius, 0, sin * innerRadius);       // bottom
                verts[v++] = new Vector3(cos * innerRadius, height, sin * innerRadius);   // top
            }

            // Inner side tris (inward-facing = reversed winding)
            for (int i = 0; i < segments; i++)
            {
                int b0 = innerStart + i * 2;
                tris[t++] = b0;     tris[t++] = b0 + 2; tris[t++] = b0 + 1;
                tris[t++] = b0 + 1; tris[t++] = b0 + 2; tris[t++] = b0 + 3;
            }

            // === TOP CAP (upward-facing) ===
            int topCapStart = v;
            for (int i = 0; i <= segments; i++)
            {
                int idx = i % segments;
                float angle = (float)i / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                float outerR = outerRadii[idx];

                verts[v++] = new Vector3(cos * outerR, height, sin * outerR);       // outer
                verts[v++] = new Vector3(cos * innerRadius, height, sin * innerRadius); // inner
            }

            for (int i = 0; i < segments; i++)
            {
                int b0 = topCapStart + i * 2;
                // outer[i], inner[i], outer[i+1] — upward normal
                tris[t++] = b0;     tris[t++] = b0 + 1; tris[t++] = b0 + 2;
                // inner[i], inner[i+1], outer[i+1] — upward normal
                tris[t++] = b0 + 1; tris[t++] = b0 + 3; tris[t++] = b0 + 2;
            }

            // === BOTTOM CAP (downward-facing) ===
            int botCapStart = v;
            for (int i = 0; i <= segments; i++)
            {
                int idx = i % segments;
                float angle = (float)i / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                float outerR = outerRadii[idx];

                verts[v++] = new Vector3(cos * outerR, 0, sin * outerR);       // outer
                verts[v++] = new Vector3(cos * innerRadius, 0, sin * innerRadius); // inner
            }

            for (int i = 0; i < segments; i++)
            {
                int b0 = botCapStart + i * 2;
                // Reversed winding from top cap for downward normal
                tris[t++] = b0;     tris[t++] = b0 + 2; tris[t++] = b0 + 1;
                tris[t++] = b0 + 1; tris[t++] = b0 + 2; tris[t++] = b0 + 3;
            }

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
