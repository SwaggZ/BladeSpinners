using UnityEngine;

namespace BladeSpinners.World
{
    public enum ArenaFootprint
    {
        Circle,
        Oval,
        FigureEight,
        RoundedSquare,
        Hex,
        Star,
        TriangleRounded,
        Capsule,
        Trefoil,
        Plus,
        Polygon,
        SharpStar,
        FlowerSharp,
        GatedCircle,
        GearTooth
    }

    public struct ArenaShapeDefinition
    {
        public string Name;
        public ArenaFootprint Footprint;
        public float Radius;
        public float AxisX;
        public float AxisZ;
        public float ShapeStrength;
        public int LobeCount;
        public float Depth;
        public float FlatRatio;
        public float CurvePower;
        public float LipHeight;
        public float LipThickness;
        public float HoleRadiusRatio;
        public float HoleLipHeight;
        public float HoleLipThickness;
        public float InnerTierRadiusRatio;
        public float InnerTierExtraDepth;
        public Color Color;
    }

    /// <summary>
    /// Shared library of the 12 arena shape definitions used by both
    /// the runtime procedural generator and the editor prototype gallery.
    /// </summary>
    public static class ArenaShapeLibrary
    {
        private const float ArenaScale = 4.5f;

        public static ArenaShapeDefinition[] GetAllShapes()
        {
            ArenaShapeDefinition[] shapes = new[]
            {
                // 1. Classic deep round — baseline
                Shape("ClassicRound", ArenaFootprint.Circle, 11.5f, 1f, 1f, 0f, 0, 1.1f, 0.26f, 1.9f, 0.95f, 0.32f, 0f, 0f, 0f, 0f, 0f, new Color(0.30f, 0.33f, 0.38f)),

                // 2. Triple Battle — circular with ~10 gear-teeth pockets around the rim
                Shape("TripleBattle", ArenaFootprint.GearTooth, 11.5f, 1f, 1f, 0.88f, 10, 1.0f, 0.28f, 1.9f, 1.0f, 0.33f, 0f, 0f, 0f, 0f, 0f, new Color(0.22f, 0.24f, 0.48f)),

                // 3. Star Storm — 5-pointed sharp zigzag star
                Shape("StarStorm", ArenaFootprint.SharpStar, 12.0f, 1f, 1f, 0.88f, 5, 0.75f, 0.50f, 1.4f, 1.0f, 0.33f, 0f, 0f, 0f, 0f, 0f, new Color(0.26f, 0.42f, 0.28f)),

                // 4. Bolt Blast — mostly round with 4 wide shallow gate bumps
                Shape("BoltBlast", ArenaFootprint.GatedCircle, 12.0f, 1.06f, 0.96f, 0.85f, 4, 0.8f, 0.50f, 1.5f, 1.0f, 0.33f, 0f, 0f, 0f, 0f, 0f, new Color(0.28f, 0.32f, 0.42f)),

                // 5. Notch Ring — circle with 4 notch gates
                Shape("NotchRing", ArenaFootprint.GatedCircle, 12.5f, 1f, 1f, 0.80f, 4, 0.75f, 0.55f, 1.5f, 1.0f, 0.33f, 0f, 0f, 0f, 0f, 0f, new Color(0.35f, 0.35f, 0.40f)),

                // 6. Pentagon — flat-sided 5-gon with hard corners
                Shape("Pentagon_Arena", ArenaFootprint.Polygon, 11.5f, 1f, 1f, 0.90f, 5, 0.9f, 0.35f, 1.7f, 1.0f, 0.33f, 0f, 0f, 0f, 0f, 0f, new Color(0.36f, 0.30f, 0.42f)),

                // 7. Square Arena — flat-sided 4-gon, tight corner bounces
                Shape("Square_Arena", ArenaFootprint.Polygon, 11.0f, 1f, 1f, 0.88f, 4, 1.0f, 0.28f, 2.0f, 1.0f, 0.33f, 0f, 0f, 0f, 0f, 0f, new Color(0.40f, 0.34f, 0.28f)),

                // 8. Triangle Arena — 3-sided, extreme corner pockets
                Shape("Triangle_Arena", ArenaFootprint.Polygon, 12.0f, 1f, 1f, 0.85f, 3, 1.1f, 0.22f, 2.0f, 1.0f, 0.33f, 0f, 0f, 0f, 0f, 0f, new Color(0.44f, 0.30f, 0.30f)),

                // 9. MaxStampede — raised center hill, sharp-square outline
                Shape("MaxStampede", ArenaFootprint.Polygon, 13.0f, 1f, 1f, 0.80f, 4, 0.8f, 0.52f, 1.5f, 1.0f, 0.33f, 0f, 0f, 0f, 0.30f, -0.65f, new Color(0.32f, 0.36f, 0.45f)),

                // 10. Figure-8 — two basins joined by narrow waist
                Shape("TwinBasin", ArenaFootprint.FigureEight, 11.8f, 1f, 1f, 0.42f, 0, 1.0f, 0.28f, 1.85f, 0.95f, 0.32f, 0f, 0f, 0f, 0f, 0f, new Color(0.38f, 0.36f, 0.30f))

                // 11. Donut Pit — circle with center hole (DISABLED from pool)
                // Shape("DonutPit", ArenaFootprint.Circle, 11.5f, 1f, 1f, 0f, 0, 0.9f, 0.24f, 1.8f, 0.95f, 0.33f, 0.22f, 0.75f, 0.15f, 0f, 0f, new Color(0.39f, 0.30f, 0.34f))

                // 12. Dual-tier — raised circular platform above lower bowl, connected by 4 bridges (DISABLED from pool)
                // Shape("DualTier", ArenaFootprint.Circle, 12.0f, 1f, 1f, 0f, 0, 1.1f, 0.28f, 1.8f, 1.0f, 0.33f, 0f, 0f, 0f, 0.38f, 0.8f, new Color(0.34f, 0.35f, 0.43f))
            };

            // Scale all dimensional properties by ArenaScale
            for (int i = 0; i < shapes.Length; i++)
            {
                ArenaShapeDefinition s = shapes[i];
                s.Radius *= ArenaScale;
                s.Depth *= ArenaScale;
                s.LipHeight *= ArenaScale;
                s.InnerTierExtraDepth *= ArenaScale;
                shapes[i] = s;
            }

            return shapes;
        }

        /// <summary>
        /// Evaluate the boundary radius for a given angle around the arena perimeter.
        /// </summary>
        public static float EvaluateBoundaryRadius(float angle, ArenaShapeDefinition shape)
        {
            float baseRadius = shape.Radius;
            float strength = Mathf.Clamp(shape.ShapeStrength, 0f, 0.9f);

            switch (shape.Footprint)
            {
                case ArenaFootprint.Circle:
                case ArenaFootprint.Oval:
                    return baseRadius;

                case ArenaFootprint.FigureEight:
                {
                    float twin = Mathf.Abs(Mathf.Cos(angle));
                    return baseRadius * Mathf.Lerp(1f - strength, 1f, twin);
                }

                case ArenaFootprint.RoundedSquare:
                {
                    float c4 = Mathf.Cos(angle * 4f);
                    return baseRadius * (1f + strength * 0.26f * c4);
                }

                case ArenaFootprint.Hex:
                {
                    float c6 = Mathf.Cos(angle * 6f);
                    return baseRadius * (1f + strength * 0.18f * c6);
                }

                case ArenaFootprint.Star:
                {
                    int lobes = Mathf.Max(5, shape.LobeCount);
                    float wave = Mathf.Cos(angle * lobes);
                    return baseRadius * (1f + strength * 0.38f * wave);
                }

                case ArenaFootprint.TriangleRounded:
                {
                    float c3 = Mathf.Cos(angle * 3f);
                    return baseRadius * (1f + strength * 0.24f * c3);
                }

                case ArenaFootprint.Capsule:
                {
                    float c2 = Mathf.Abs(Mathf.Cos(angle));
                    return baseRadius * Mathf.Lerp(1f - strength * 0.3f, 1f + strength * 0.12f, c2);
                }

                case ArenaFootprint.Trefoil:
                {
                    float c3 = Mathf.Cos(angle * 3f);
                    return baseRadius * (1f + strength * 0.45f * c3);
                }

                case ArenaFootprint.Plus:
                {
                    float c4 = Mathf.Cos(angle * 4f);
                    return baseRadius * (1f + strength * 0.50f * c4);
                }

                case ArenaFootprint.Polygon:
                {
                    int sides = Mathf.Max(3, shape.LobeCount);
                    float sector = Mathf.PI * 2f / sides;
                    float half = sector * 0.5f;
                    float local = Mathf.Repeat(angle + half, sector) - half;
                    float polyR = Mathf.Cos(half) / Mathf.Cos(local);
                    return baseRadius * Mathf.Lerp(1f, polyR, strength);
                }

                case ArenaFootprint.SharpStar:
                {
                    int points = Mathf.Max(3, shape.LobeCount);
                    float sector = Mathf.PI * 2f / points;
                    float half = sector * 0.5f;
                    float local = Mathf.Repeat(angle + half, sector) - half;
                    float tri = 1f - Mathf.Abs(local) / half;
                    float outer = 1f + strength * 0.40f;
                    float inner = 1f - strength * 0.25f;
                    return baseRadius * Mathf.Lerp(inner, outer, tri);
                }

                case ArenaFootprint.FlowerSharp:
                {
                    int petals = Mathf.Max(3, shape.LobeCount);
                    float f = Mathf.Abs(Mathf.Cos(angle * petals * 0.5f));
                    float outer = 1f + strength * 0.32f;
                    float inner = 1f - strength * 0.18f;
                    return baseRadius * Mathf.Lerp(inner, outer, f);
                }

                case ArenaFootprint.GatedCircle:
                {
                    int gates = Mathf.Max(2, shape.LobeCount);
                    float sector = Mathf.PI * 2f / gates;
                    float half = sector * 0.5f;
                    float local = Mathf.Repeat(angle + half, sector) - half;
                    float gateHalf = half * 0.55f;
                    float absLocal = Mathf.Abs(local);
                    if (absLocal < gateHalf)
                    {
                        float blend = 0.5f + 0.5f * Mathf.Cos(absLocal / gateHalf * Mathf.PI);
                        return baseRadius * (1f + strength * 0.10f * blend);
                    }
                    return baseRadius;
                }

                case ArenaFootprint.GearTooth:
                {
                    int teeth = Mathf.Max(4, shape.LobeCount);
                    float sector = Mathf.PI * 2f / teeth;
                    float half = sector * 0.5f;
                    float local = Mathf.Repeat(angle + half, sector) - half;
                    float toothHalf = half * 0.40f;
                    if (Mathf.Abs(local) < toothHalf)
                        return baseRadius * (1f + strength * 0.18f);
                    return baseRadius;
                }

                default:
                    return baseRadius;
            }
        }

        /// <summary>
        /// Evaluate the bowl surface height at a normalized radial distance [0..1].
        /// </summary>
        public static float EvaluateSurfaceHeight(float radiusNorm, ArenaShapeDefinition shape)
        {
            float norm = Mathf.Clamp01(radiusNorm);
            float flat = Mathf.Clamp(shape.FlatRatio, 0f, 0.95f);
            float depth = Mathf.Max(0.2f, shape.Depth);
            float power = Mathf.Max(1f, shape.CurvePower);

            float y;
            if (norm <= flat)
            {
                y = -depth;
            }
            else
            {
                float t = (norm - flat) / Mathf.Max(0.0001f, 1f - flat);
                y = -depth + depth * Mathf.Pow(t, power);
            }

            float innerTierRadius = Mathf.Clamp01(shape.InnerTierRadiusRatio);
            if (innerTierRadius > 0.001f && Mathf.Abs(shape.InnerTierExtraDepth) > 0.001f
                && shape.InnerTierExtraDepth < 0f && norm <= innerTierRadius)
            {
                float rimNorm = innerTierRadius;
                float rimY;
                if (rimNorm <= flat)
                    rimY = -depth;
                else
                {
                    float tR = (rimNorm - flat) / Mathf.Max(0.0001f, 1f - flat);
                    rimY = -depth + depth * Mathf.Pow(tR, power);
                }

                float innerFloor = -(depth + shape.InnerTierExtraDepth);
                float innerNorm = norm / innerTierRadius;
                const float innerFlatRatio = 0.40f;

                if (innerNorm <= innerFlatRatio)
                    y = innerFloor;
                else
                {
                    float tI = (innerNorm - innerFlatRatio) / (1f - innerFlatRatio);
                    y = Mathf.Lerp(innerFloor, rimY, tI);
                }
            }

            return y;
        }

        private static ArenaShapeDefinition Shape(
            string name, ArenaFootprint footprint,
            float radius, float axisX, float axisZ,
            float shapeStrength, int lobeCount,
            float depth, float flatRatio, float curvePower,
            float lipHeight, float lipThickness,
            float holeRadiusRatio, float holeLipHeight, float holeLipThickness,
            float innerTierRadiusRatio, float innerTierExtraDepth,
            Color color)
        {
            return new ArenaShapeDefinition
            {
                Name = name,
                Footprint = footprint,
                Radius = radius,
                AxisX = axisX,
                AxisZ = axisZ,
                ShapeStrength = shapeStrength,
                LobeCount = lobeCount,
                Depth = depth,
                FlatRatio = flatRatio,
                CurvePower = curvePower,
                LipHeight = lipHeight,
                LipThickness = lipThickness,
                HoleRadiusRatio = holeRadiusRatio,
                HoleLipHeight = holeLipHeight,
                HoleLipThickness = holeLipThickness,
                InnerTierRadiusRatio = innerTierRadiusRatio,
                InnerTierExtraDepth = innerTierExtraDepth,
                Color = color
            };
        }
    }
}
