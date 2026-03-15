using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Parts
{
    /// <summary>
    /// Generates procedural 3D meshes for each Beyblade part type.
    /// Each part's MeshSeed drives visual variation — no two parts with different seeds
    /// look the same. Stats also influence shape (weight → bigger wheel, etc.).
    ///
    /// Stack order (bottom to top): Tip → Track → FusionWheel → EnergyRing → FaceBolt
    /// All meshes are generated with bottom at y=0, top at y=height.
    /// BeyAssembler handles vertical stacking and centering.
    /// </summary>
    public static class ProceduralPartMeshGenerator
    {
        private const int RING_SEGMENTS = 32;
        private const int TIP_SEGMENTS = 16;
        private const float FACE_BOLT_MIN_RADIUS = 0.03f;
        private const float FACE_BOLT_MAX_RADIUS = 0.045f;
        private const float ENERGY_RING_HOLE_EXTRA_DIAMETER = 0.03f; // hole width must stay slightly larger than face bolt width

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
                PartType.FusionWheel => GenerateFusionWheelMesh(part, rng),
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
        /// Returns the radius of the FaceBolt (for constraining the EnergyRing hole).
        /// Mirrors the same RNG sequence as GenerateFaceBoltMesh.
        /// </summary>
        public static float GetFaceBoltRadius(BeyPart faceBolt)
        {
            if (faceBolt == null || faceBolt.PartType != PartType.FaceBolt) return 0.025f;
            System.Random rng = new System.Random(faceBolt.MeshSeed);
            rng.Next(0, 5); // skip polygon sides (same as GenerateFaceBoltMesh)
            return FACE_BOLT_MIN_RADIUS + (float)rng.NextDouble() * (FACE_BOLT_MAX_RADIUS - FACE_BOLT_MIN_RADIUS);
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
                TipBehaviorType.Spike => 0.08f,
                TipBehaviorType.Sharp => 0.06f,
                _ => 0.04f
            };
        }

        private static Mesh GenerateTipMesh(BeyPart part, System.Random rng)
        {
            // Seed-based proportion variation (±15%)
            float widthScale = 0.85f + (float)rng.NextDouble() * 0.3f;
            float heightScale = 0.85f + (float)rng.NextDouble() * 0.3f;

            return part.TipBehavior switch
            {
                // Flat tips: slightly narrower at bottom, wider at top (ground contact is flat bottom cap)
                TipBehaviorType.Flat => GenerateCylinder(
                    0.03f * widthScale, 0.03f * widthScale, 0.03f * heightScale,
                    TIP_SEGMENTS, capTop: false, capBottom: true),
                TipBehaviorType.RubberFlat => GenerateCylinder(
                    0.035f * widthScale, 0.03f * widthScale, 0.03f * heightScale,
                    TIP_SEGMENTS, capTop: false, capBottom: true),
                TipBehaviorType.Sharp => GenerateCone(
                    0.03f * widthScale, 0.005f * widthScale, 0.06f * heightScale, TIP_SEGMENTS),
                TipBehaviorType.Spike => GenerateCone(
                    0.025f * widthScale, 0.002f * widthScale, 0.08f * heightScale, TIP_SEGMENTS),
                TipBehaviorType.Round => GenerateHemisphere(
                    0.025f * widthScale, TIP_SEGMENTS, TIP_SEGMENTS / 2, bottomHalf: true),
                TipBehaviorType.Ball => GenerateHemisphere(
                    0.03f * widthScale, TIP_SEGMENTS, TIP_SEGMENTS / 2, bottomHalf: true),
                TipBehaviorType.Orbit => GenerateOrbitTip(widthScale, heightScale),
                _ => GenerateHemisphere(0.025f * widthScale, TIP_SEGMENTS, TIP_SEGMENTS / 2, bottomHalf: true)
            };
        }

        private static Mesh GenerateOrbitTip(float widthScale, float heightScale)
        {
            Mesh mesh = new Mesh();
            mesh.name = "OrbitTip";

            CombineInstance[] combine = new CombineInstance[2];
            Mesh ring = GenerateCylinder(
                0.035f * widthScale, 0.025f * widthScale, 0.025f * heightScale,
                TIP_SEGMENTS, true, true);
            combine[0].mesh = ring;
            combine[0].transform = Matrix4x4.identity;

            Mesh ball = GenerateHemisphere(0.015f * widthScale, 8, 4, bottomHalf: true);
            combine[1].mesh = ball;
            combine[1].transform = Matrix4x4.identity;

            mesh.CombineMeshes(combine, true, true);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
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

        private static Mesh GenerateFusionWheelMesh(BeyPart part, System.Random rng)
        {
            float t = Mathf.InverseLerp(GameConstants.MIN_WEIGHT, GameConstants.MAX_WEIGHT, part.Weight);
            float baseOuterRadius = Mathf.Lerp(0.1f, 0.18f, t);
            float innerRadius = 0.04f;
            float height = GetFusionWheelHeight(part);

            // Symmetry planes (1–2) from seed
            int symmetryPlanes = 1 + rng.Next(0, 2);

            // Seed-driven blade configuration
            int bladeCount = 3 + rng.Next(0, 6); // 3–8 blades
            float bladeProtrusion = 0.015f + (float)rng.NextDouble() * 0.035f; // how far blades stick out
            float bladeWidth = 0.15f + (float)rng.NextDouble() * 0.25f; // angular fraction per blade (0.15–0.4)
            float bladeSweep = -0.05f + (float)rng.NextDouble() * 0.1f; // slight angular offset for swept look

            // Generate per-segment outer radius
            float[] outerRadii = new float[RING_SEGMENTS];
            for (int i = 0; i < RING_SEGMENTS; i++)
            {
                float segAngle = (float)i / RING_SEGMENTS; // 0–1 range

                float maxBladeFactor = 0f;
                for (int b = 0; b < bladeCount; b++)
                {
                    float bladeCenter = ((float)b / bladeCount + bladeSweep) % 1f;
                    float dist = Mathf.Abs(segAngle - bladeCenter);
                    dist = Mathf.Min(dist, 1f - dist); // wrap around

                    float halfWidth = bladeWidth / (2f * bladeCount);
                    if (dist < halfWidth)
                    {
                        // Smooth falloff from blade center
                        float bladeFactor = 1f - (dist / halfWidth);
                        bladeFactor = bladeFactor * bladeFactor; // quadratic falloff for nicer shape
                        maxBladeFactor = Mathf.Max(maxBladeFactor, bladeFactor);
                    }
                }

                outerRadii[i] = baseOuterRadius + bladeProtrusion * maxBladeFactor;
            }

            // Enforce symmetry so blades are evenly mirrored
            EnforceSymmetry(outerRadii, symmetryPlanes);

            return GenerateModulatedRing(outerRadii, innerRadius, height, RING_SEGMENTS);
        }

        // =====================================================================
        // ENERGY RING — thin ring with wavy/scalloped edge from seed
        // =====================================================================

        private static float GetEnergyRingHeight(BeyPart part)
        {
            return 0.02f;
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
        // FACE BOLT — small cap with N-sided polygon shape from seed
        // =====================================================================

        private static float GetFaceBoltHeight()
        {
            return 0.015f;
        }

        private static Mesh GenerateFaceBoltMesh(BeyPart part, System.Random rng)
        {
            // Seed-based polygon sides and size variation
            int sides = 4 + rng.Next(0, 5); // 4–8 sided polygon
            float radius = FACE_BOLT_MIN_RADIUS + (float)rng.NextDouble() * (FACE_BOLT_MAX_RADIUS - FACE_BOLT_MIN_RADIUS);
            float height = GetFaceBoltHeight();

            // Generate with the polygon segment count for angular shape
            return GenerateCylinder(radius, radius, height, sides, capTop: true, capBottom: false);
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
