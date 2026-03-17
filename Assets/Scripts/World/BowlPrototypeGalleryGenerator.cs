using System.Collections.Generic;
using BladeSpinners.Core;
using BladeSpinners.Gameplay;
using UnityEngine;

namespace BladeSpinners.World
{
    /// <summary>
    /// Manual prototype gallery generator for comparing bowl silhouettes and concavity styles.
    /// Attach to an empty GameObject and run GeneratePrototypes from the context menu.
    /// </summary>
    public sealed class BowlPrototypeGalleryGenerator : MonoBehaviour
    {
        [Header("Generation")]
        [SerializeField] private bool generateOnStart;
        [SerializeField] private bool clearPrevious = true;
        [SerializeField] private int columns = 4;
        [SerializeField] private float spacing = 100f;
        [SerializeField] private int angularSegments = 96;
        [SerializeField] private int radialSegments = 36;

        [Header("Layer")]
        [SerializeField] private string arenaLayerName = "Default";

        private readonly List<GameObject> generatedRoots = new List<GameObject>();

        public void ConfigureGeneration(bool shouldClearPrevious, int columnCount, float cellSpacing, int angSeg, int radSeg, string layerName)
        {
            clearPrevious = shouldClearPrevious;
            columns = Mathf.Max(1, columnCount);
            spacing = Mathf.Max(2f, cellSpacing);
            angularSegments = Mathf.Max(24, angSeg);
            radialSegments = Mathf.Max(10, radSeg);
            arenaLayerName = string.IsNullOrWhiteSpace(layerName) ? "Default" : layerName;
        }

        public int PrototypeCount => ArenaShapeLibrary.GetAllShapes().Length;

        private void Start()
        {
            if (generateOnStart)
                GeneratePrototypes();
        }

        [ContextMenu("Generate 12 Bowl Prototypes")]
        public void GeneratePrototypes()
        {
            if (clearPrevious)
                ClearGenerated();

            ArenaShapeDefinition[] prototypes = ArenaShapeLibrary.GetAllShapes();
            int cols = Mathf.Max(1, columns);
            int arenaLayer = LayerMask.NameToLayer(arenaLayerName);
            if (arenaLayer < 0)
                arenaLayer = LayerMask.NameToLayer("Default");

            for (int i = 0; i < prototypes.Length; i++)
            {
                ArenaShapeDefinition proto = prototypes[i];
                int row = i / cols;
                int col = i % cols;

                Vector3 offset = new Vector3(col * spacing, 0f, row * spacing);
                GameObject root = new GameObject($"BowlPrototype_{(i + 1).ToString("00")}_{proto.Name}");
                root.transform.SetParent(transform, false);
                root.transform.localPosition = offset;

                GameObject bowl = CreatePrototypeBowl(proto, Mathf.Max(24, angularSegments), Mathf.Max(10, radialSegments));
                bowl.transform.SetParent(root.transform, false);

                SetLayerRecursive(root, arenaLayer);
                generatedRoots.Add(root);
            }
        }

        [ContextMenu("Clear Generated Prototypes")]
        public void ClearGenerated()
        {
            for (int i = generatedRoots.Count - 1; i >= 0; i--)
            {
                GameObject go = generatedRoots[i];
                if (go == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(go);
                else
                    DestroyImmediate(go);
            }

            generatedRoots.Clear();

            // Safety clear in case hierarchy changed externally.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null)
                    continue;

                if (!child.name.StartsWith("BowlPrototype_"))
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private GameObject CreatePrototypeBowl(ArenaShapeDefinition proto, int angSeg, int radSeg)
        {
            bool hasHole = proto.HoleRadiusRatio > 0.001f;
            float innerNorm = Mathf.Clamp01(proto.HoleRadiusRatio);
            bool hasCenter = !hasHole;

            int firstRingStep = hasCenter ? 1 : 0;
            int ringCount = (radSeg + 1) - firstRingStep;

            int vertexOffset = hasCenter ? 1 : 0;
            int vertCount = vertexOffset + ringCount * angSeg;
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];

            if (hasCenter)
            {
                float centerY = ArenaShapeLibrary.EvaluateSurfaceHeight(0f, proto);
                vertices[0] = new Vector3(0f, centerY, 0f);
                uvs[0] = new Vector2(0.5f, 0.5f);
            }

            Vector3[] outerRingPoints = new Vector3[angSeg];
            Vector3[] holeRingPoints = hasHole ? new Vector3[angSeg] : null;

            for (int ring = 0; ring < ringCount; ring++)
            {
                float norm = hasCenter
                    ? (float)(ring + 1) / radSeg
                    : Mathf.Lerp(innerNorm, 1f, ring / Mathf.Max(1f, ringCount - 1f));

                for (int a = 0; a < angSeg; a++)
                {
                    float t = (float)a / angSeg;
                    float angle = t * Mathf.PI * 2f;
                    float boundary = Mathf.Max(0.5f, ArenaShapeLibrary.EvaluateBoundaryRadius(angle, proto));
                    float dist = boundary * norm;

                    float x = Mathf.Cos(angle) * dist * proto.AxisX;
                    float z = Mathf.Sin(angle) * dist * proto.AxisZ;
                    float y = ArenaShapeLibrary.EvaluateSurfaceHeight(norm, proto);

                    int idx = vertexOffset + ring * angSeg + a;
                    vertices[idx] = new Vector3(x, y, z);
                    uvs[idx] = new Vector2(0.5f + Mathf.Cos(angle) * norm * 0.5f,
                                           0.5f + Mathf.Sin(angle) * norm * 0.5f);

                    if (ring == ringCount - 1)
                        outerRingPoints[a] = vertices[idx];
                    if (hasHole && ring == 0)
                        holeRingPoints[a] = vertices[idx];
                }
            }

            List<int> triangles = new List<int>(angSeg * radSeg * 6 + angSeg * 6);

            if (hasCenter)
            {
                for (int a = 0; a < angSeg; a++)
                {
                    int next = (a + 1) % angSeg;
                    int cur = vertexOffset + a;
                    int nxt = vertexOffset + next;

                    triangles.Add(0);
                    triangles.Add(nxt);
                    triangles.Add(cur);
                }
            }

            for (int ring = 0; ring < ringCount - 1; ring++)
            {
                for (int a = 0; a < angSeg; a++)
                {
                    int next = (a + 1) % angSeg;

                    int cur = vertexOffset + ring * angSeg + a;
                    int curNext = vertexOffset + ring * angSeg + next;
                    int outCur = vertexOffset + (ring + 1) * angSeg + a;
                    int outNext = vertexOffset + (ring + 1) * angSeg + next;

                    triangles.Add(cur);
                    triangles.Add(curNext);
                    triangles.Add(outNext);

                    triangles.Add(cur);
                    triangles.Add(outNext);
                    triangles.Add(outCur);
                }
            }

            Mesh mesh = new Mesh { name = $"{proto.Name}_BowlMesh" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject bowl = new GameObject(proto.Name);
            MeshFilter mf = bowl.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = bowl.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CreateArenaMaterial(proto.Color, 0.22f, 0.33f);

            MeshCollider mc = bowl.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            GameObject outerLip = CreatePerimeterLip(proto.Name + "_OuterLip", outerRingPoints, proto.LipHeight, proto.LipThickness, false);
            outerLip.transform.SetParent(bowl.transform, false);

            GameObject outerWalls = CreateSegmentedRimWalls(proto.Name + "_RimWalls", outerRingPoints, proto.LipHeight);
            outerWalls.transform.SetParent(bowl.transform, false);

            if (hasHole && holeRingPoints != null)
            {
                GameObject holeLip = CreatePerimeterLip(
                    proto.Name + "_HoleLip",
                    holeRingPoints,
                    proto.HoleLipHeight,
                    proto.HoleLipThickness,
                    true);
                holeLip.transform.SetParent(bowl.transform, false);
            }

            // Raised platform with bridges (positive InnerTierExtraDepth)
            if (proto.InnerTierRadiusRatio > 0.001f && proto.InnerTierExtraDepth > 0.001f)
            {
                float platformRadius = proto.Radius * proto.InnerTierRadiusRatio;
                float floorY = ArenaShapeLibrary.EvaluateSurfaceHeight(proto.InnerTierRadiusRatio, proto);
                float platformHeight = proto.InnerTierExtraDepth;
                GameObject platform = CreateRaisedPlatformWithBridges(
                    proto.Name + "_Platform",
                    platformRadius,
                    floorY,
                    platformHeight,
                    proto.Radius,
                    proto.AxisX,
                    proto.AxisZ,
                    angSeg,
                    proto.Color);
                platform.transform.SetParent(bowl.transform, false);
            }

            // ---- Spawn obstacles inside the bowl ----
            SpawnObstacles(bowl, proto);

            return bowl;
        }

        // ================================================================
        // OBSTACLE SPAWNING — populates each prototype with random obstacles
        // ================================================================

        private static void SpawnObstacles(GameObject bowl, ArenaShapeDefinition proto)
        {
            // Use a deterministic seed from the prototype name so each bowl
            // always gets the same obstacles.
            int seed = proto.Name.GetHashCode();
            System.Random rng = new System.Random(seed);
            List<Vector3> occupied = new List<Vector3>(); // (x, z, footprint)

            float radius = proto.Radius;
            float depth = proto.Depth;
            float flatRatio = proto.FlatRatio;
            float curvePower = proto.CurvePower;

            // Scale obstacle counts proportionally to arena size
            float sizeScale = radius / 12f; // 12 is a typical radius
            int innerWalls = rng.Next(0, Mathf.Max(1, Mathf.RoundToInt(3 * sizeScale)));
            int platforms   = rng.Next(0, Mathf.Max(1, Mathf.RoundToInt(2 * sizeScale)));
            int ramps       = rng.Next(0, Mathf.Max(1, Mathf.RoundToInt(2 * sizeScale)));
            int bumpers     = rng.Next(0, Mathf.Max(1, Mathf.RoundToInt(3 * sizeScale)));
            int pillars     = rng.Next(0, Mathf.Max(1, Mathf.RoundToInt(2 * sizeScale)));
            int spires      = rng.Next(0, Mathf.Max(1, Mathf.RoundToInt(2 * sizeScale)));

            for (int i = 0; i < innerWalls; i++)
            {
                float wLen = 0.4f + (float)rng.NextDouble() * 1.2f;
                float wThick = 0.08f + (float)rng.NextDouble() * 0.1f;
                float footprint = Mathf.Max(wLen, wThick) * 0.5f;
                if (!TryFindPlacement(rng, radius * flatRatio * 0.3f, radius * 0.65f, footprint, occupied, out float x, out float z))
                    continue;
                float dist = Mathf.Sqrt(x * x + z * z);
                float y = GetBowlY(dist, radius, flatRatio, depth, curvePower);
                float wH = 0.15f + (float)rng.NextDouble() * 0.6f;

                // Base below the arena's lowest point (-depth)
                float baseY = -depth - 0.1f;
                float topY = y + wH;
                float fullH = topY - baseY;

                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"InnerWall_{i}";
                wall.transform.SetParent(bowl.transform, false);
                wall.transform.localScale = new Vector3(wLen, fullH, wThick);
                wall.transform.localPosition = new Vector3(x, baseY + fullH * 0.5f, z);
                wall.transform.localRotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
                wall.GetComponent<Renderer>().sharedMaterial =
                    CreateArenaMaterial(proto.Color * 0.75f, 0.3f, 0.2f);
            }

            for (int i = 0; i < platforms; i++)
            {
                float pRad = 0.3f + (float)rng.NextDouble() * 0.8f;
                if (!TryFindPlacement(rng, 0f, radius * flatRatio * 1.2f, pRad, occupied, out float x, out float z))
                    continue;
                float dist = Mathf.Sqrt(x * x + z * z);
                float y = GetBowlY(dist, radius, flatRatio, depth, curvePower);
                float pH = 0.05f + (float)rng.NextDouble() * 0.25f;

                // Base below the arena's lowest point (-depth)
                float baseY = -depth - 0.1f;
                float topY = y + pH;
                float fullH = topY - baseY;

                GameObject plat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                plat.name = $"Platform_{i}";
                plat.transform.SetParent(bowl.transform, false);
                plat.transform.localScale = new Vector3(pRad * 2f, fullH * 0.5f, pRad * 2f);
                plat.transform.localPosition = new Vector3(x, baseY + fullH * 0.5f, z);
                plat.GetComponent<Renderer>().sharedMaterial =
                    CreateArenaMaterial(proto.Color * 0.9f, 0.4f, 0.35f);
            }

            for (int i = 0; i < ramps; i++)
            {
                float rLen = 0.8f + (float)rng.NextDouble() * 1.2f;
                float rW = 0.5f + (float)rng.NextDouble() * 0.7f;
                float footprint = Mathf.Max(rLen, rW) * 0.5f;
                if (!TryFindPlacement(rng, radius * flatRatio * 0.3f, radius * 0.6f, footprint, occupied, out float x, out float z))
                    continue;
                float dist = Mathf.Sqrt(x * x + z * z);
                float y = GetBowlY(dist, radius, flatRatio, depth, curvePower);
                float rH = 0.15f + (float)rng.NextDouble() * 0.4f;

                float baseY = -depth - 0.1f;
                float sinkH = y - baseY;

                // Simple box approximation for ramp visual
                GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ramp.name = $"Ramp_{i}";
                ramp.transform.SetParent(bowl.transform, false);
                ramp.transform.localScale = new Vector3(rW, rH + sinkH, rLen);
                ramp.transform.localPosition = new Vector3(x, baseY + (rH + sinkH) * 0.5f, z);
                ramp.transform.localRotation = Quaternion.Euler(
                    -Mathf.Atan2(rH, rLen) * Mathf.Rad2Deg, (float)rng.NextDouble() * 360f, 0);
                ramp.GetComponent<Renderer>().sharedMaterial =
                    CreateArenaMaterial(proto.Color * 0.8f, 0.35f, 0.25f);
            }

            for (int i = 0; i < bumpers; i++)
            {
                float bRad = 0.2f + (float)rng.NextDouble() * 0.4f;
                if (!TryFindPlacement(rng, 0f, radius * (flatRatio + 0.25f), bRad, occupied, out float x, out float z))
                    continue;
                float dist = Mathf.Sqrt(x * x + z * z);
                float y = GetBowlY(dist, radius, flatRatio, depth, curvePower);

                GameObject bumper = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bumper.name = $"Bumper_{i}";
                bumper.transform.SetParent(bowl.transform, false);
                bumper.transform.localScale = new Vector3(bRad * 2f, bRad, bRad * 2f);
                bumper.transform.localPosition = new Vector3(x, y + bRad * 0.5f, z);
                bumper.GetComponent<Renderer>().sharedMaterial =
                    CreateArenaMaterial(new Color(0.7f, 0.25f, 0.25f), 0.5f, 0.5f);
            }

            for (int i = 0; i < pillars; i++)
            {
                float pRad = 0.15f + (float)rng.NextDouble() * 0.25f;
                if (!TryFindPlacement(rng, radius * flatRatio * 0.3f, radius * flatRatio * 1.5f, pRad, occupied, out float x, out float z))
                    continue;
                float dist = Mathf.Sqrt(x * x + z * z);
                float y = GetBowlY(dist, radius, flatRatio, depth, curvePower);
                float pH = 0.5f + (float)rng.NextDouble() * 1.2f;

                float baseY = -depth - 0.1f;
                float topY = y + pH;
                float fullH = topY - baseY;

                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Pillar_{i}";
                pillar.transform.SetParent(bowl.transform, false);
                pillar.transform.localScale = new Vector3(pRad * 2f, fullH * 0.5f, pRad * 2f);
                pillar.transform.localPosition = new Vector3(x, baseY + fullH * 0.5f, z);
                pillar.GetComponent<Renderer>().sharedMaterial =
                    CreateArenaMaterial(proto.Color * 0.7f, 0.6f, 0.4f);
            }

            for (int i = 0; i < spires; i++)
            {
                float sRad = 0.05f + (float)rng.NextDouble() * 0.08f;
                if (!TryFindPlacement(rng, radius * flatRatio * 0.2f, radius * flatRatio * 1.3f, sRad, occupied, out float x, out float z))
                    continue;
                float dist = Mathf.Sqrt(x * x + z * z);
                float y = GetBowlY(dist, radius, flatRatio, depth, curvePower);
                float sH = 1.5f + (float)rng.NextDouble() * 2f;

                float baseY = -depth - 0.1f;
                float topY = y + sH;
                float fullH = topY - baseY;

                GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                spire.name = $"Spire_{i}";
                spire.transform.SetParent(bowl.transform, false);
                spire.transform.localScale = new Vector3(sRad * 2f, fullH * 0.5f, sRad * 2f);
                spire.transform.localPosition = new Vector3(x, baseY + fullH * 0.5f, z);
                spire.GetComponent<Renderer>().sharedMaterial =
                    CreateArenaMaterial(proto.Color * 0.6f, 0.7f, 0.5f);
            }
        }

        /// <summary>
        /// Bowl surface Y at a given radial distance, matching the prototype's parabolic profile.
        /// </summary>
        private static float GetBowlY(float dist, float radius, float flatRatio, float depth, float curvePower)
        {
            float flatR = radius * flatRatio;
            if (dist <= flatR) return -depth;
            float t = Mathf.Clamp01((dist - flatR) / (radius - flatR));
            return -depth + depth * Mathf.Pow(t, curvePower);
        }

        /// <summary>
        /// Try to find a non-overlapping placement within [minR, maxR].
        /// occupied entries: (x, z, footprintRadius).
        /// </summary>
        private static bool TryFindPlacement(System.Random rng, float minR, float maxR,
            float footprint, List<Vector3> occupied, out float x, out float z)
        {
            for (int attempt = 0; attempt < 15; attempt++)
            {
                float r = minR + (float)rng.NextDouble() * Mathf.Max(0.01f, maxR - minR);
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float cx = Mathf.Cos(angle) * r;
                float cz = Mathf.Sin(angle) * r;

                bool overlaps = false;
                for (int i = 0; i < occupied.Count; i++)
                {
                    Vector3 occ = occupied[i];
                    float dx = cx - occ.x;
                    float dz = cz - occ.y;
                    float minDist = footprint + occ.z;
                    if (dx * dx + dz * dz < minDist * minDist)
                    { overlaps = true; break; }
                }
                if (!overlaps)
                {
                    occupied.Add(new Vector3(cx, cz, footprint));
                    x = cx; z = cz;
                    return true;
                }
            }
            x = 0f; z = 0f;
            return false;
        }

        // ================================================================
        // TORUS LIP — smooth tube ring following the bowl's perimeter shape
        // Matches the style of ProceduralArenaGenerator.CreateTorusLip.
        // ================================================================
        private static GameObject CreatePerimeterLip(
            string name,
            Vector3[] ringPoints,
            float lipHeight,
            float lipThickness,
            bool invertDirection)
        {
            GameObject lip = new GameObject(name);

            if (ringPoints == null || ringPoints.Length < 3 || lipHeight <= 0.001f || lipThickness <= 0.001f)
                return lip;

            int ringSegs = ringPoints.Length;
            int tubeSegs = 8;
            float r = Mathf.Max(0.25f, Mathf.Min(lipHeight, lipThickness) * 0.5f);

            Vector3[] vertices = new Vector3[ringSegs * tubeSegs];
            Vector2[] uvs     = new Vector2[ringSegs * tubeSegs];

            for (int i = 0; i < ringSegs; i++)
            {
                Vector3 p      = ringPoints[i];
                Vector3 prevPt = ringPoints[(i - 1 + ringSegs) % ringSegs];
                Vector3 nextPt = ringPoints[(i + 1) % ringSegs];

                // Tangent of the path and outward perpendicular in XZ plane
                Vector3 tangent = new Vector3(nextPt.x - prevPt.x, 0f, nextPt.z - prevPt.z).normalized;
                Vector3 outward = Vector3.Cross(Vector3.up, tangent).normalized;
                if (Vector3.Dot(outward, new Vector3(p.x, 0f, p.z).normalized) < 0f)
                    outward = -outward;

                if (invertDirection)
                    outward = -outward;

                for (int j = 0; j < tubeSegs; j++)
                {
                    float phi = (float)j / tubeSegs * Mathf.PI * 2f;
                    int idx = i * tubeSegs + j;
                    // Tube cross-section: cos(phi)*outward is radial, sin(phi)*up is vertical
                    vertices[idx] = p + r * Mathf.Cos(phi) * outward + r * Mathf.Sin(phi) * Vector3.up;
                    uvs[idx]      = new Vector2((float)i / ringSegs, (float)j / tubeSegs);
                }
            }

            int[] triangles = new int[ringSegs * tubeSegs * 6];
            int ti = 0;
            for (int i = 0; i < ringSegs; i++)
            {
                int iNext = (i + 1) % ringSegs;
                for (int j = 0; j < tubeSegs; j++)
                {
                    int jNext = (j + 1) % tubeSegs;
                    int v00 = i     * tubeSegs + j;
                    int v01 = i     * tubeSegs + jNext;
                    int v10 = iNext * tubeSegs + j;
                    int v11 = iNext * tubeSegs + jNext;

                    // Flip winding for inner-hole lips so normals face outward from the tube
                    if (!invertDirection)
                    {
                        triangles[ti++] = v00; triangles[ti++] = v11; triangles[ti++] = v10;
                        triangles[ti++] = v00; triangles[ti++] = v01; triangles[ti++] = v11;
                    }
                    else
                    {
                        triangles[ti++] = v00; triangles[ti++] = v10; triangles[ti++] = v11;
                        triangles[ti++] = v00; triangles[ti++] = v11; triangles[ti++] = v01;
                    }
                }
            }

            Mesh mesh = new Mesh { name = name };
            mesh.vertices  = vertices;
            mesh.uv        = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter mf = lip.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = lip.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CreateArenaMaterial(new Color(0.3f, 0.3f, 0.35f), 0.3f, 0.4f);

            MeshCollider mc = lip.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            return lip;
        }

        // ================================================================
        // RAISED PLATFORM WITH BRIDGES — a smaller circular platform sitting
        // above the main bowl floor, connected to the rim by 4 bridge ramps.
        // ================================================================

        private static GameObject CreateRaisedPlatformWithBridges(
            string name, float platformRadius, float floorY, float platformHeight,
            float bowlRadius, float axisX, float axisZ, int angSeg, Color color)
        {
            GameObject root = new GameObject(name);
            float topY = floorY + platformHeight;

            // --- Circular platform disc (double-sided) ---
            int platSegs = Mathf.Max(24, angSeg / 2);
            int halfVerts = platSegs + 1; // center + ring
            int platVertCount = halfVerts * 2; // top face + bottom face
            Vector3[] pVerts = new Vector3[platVertCount];
            Vector2[] pUvs = new Vector2[platVertCount];

            // Top face verts
            pVerts[0] = new Vector3(0f, topY, 0f);
            pUvs[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < platSegs; i++)
            {
                float a = (float)i / platSegs * Mathf.PI * 2f;
                float cx = Mathf.Cos(a) * platformRadius * axisX;
                float cz = Mathf.Sin(a) * platformRadius * axisZ;
                pVerts[i + 1] = new Vector3(cx, topY, cz);
                pUvs[i + 1] = new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f);
            }

            // Bottom face verts (same positions, separate for correct normals)
            for (int i = 0; i < halfVerts; i++)
            {
                pVerts[halfVerts + i] = pVerts[i];
                pUvs[halfVerts + i] = pUvs[i];
            }

            int[] pTris = new int[platSegs * 3 * 2];
            for (int i = 0; i < platSegs; i++)
            {
                int cur = i + 1;
                int nxt = (i + 1) % platSegs + 1;
                // Top face
                pTris[i * 3] = 0;
                pTris[i * 3 + 1] = nxt;
                pTris[i * 3 + 2] = cur;
                // Bottom face (reversed winding)
                int bIdx = platSegs * 3 + i * 3;
                pTris[bIdx] = halfVerts;
                pTris[bIdx + 1] = halfVerts + cur;
                pTris[bIdx + 2] = halfVerts + nxt;
            }

            Mesh platMesh = new Mesh { name = name + "_Disc" };
            platMesh.vertices = pVerts;
            platMesh.uv = pUvs;
            platMesh.triangles = pTris;
            platMesh.RecalculateNormals();
            platMesh.RecalculateBounds();

            GameObject disc = new GameObject("PlatformDisc");
            disc.transform.SetParent(root.transform, false);
            disc.AddComponent<MeshFilter>().sharedMesh = platMesh;
            disc.AddComponent<MeshRenderer>().sharedMaterial =
                CreateArenaMaterial(color * 1.15f, 0.25f, 0.35f);
            disc.AddComponent<MeshCollider>().sharedMesh = platMesh;

            // --- 4 Bridge ramps at 90° intervals ---
            int bridgeCount = 4;
            float bridgeWidth = platformRadius * 0.55f;
            int bridgeSegsAlong = 8;

            for (int b = 0; b < bridgeCount; b++)
            {
                float bAngle = b * Mathf.PI * 2f / bridgeCount;
                float cosA = Mathf.Cos(bAngle);
                float sinA = Mathf.Sin(bAngle);

                // Bridge starts at platform edge, ends at the bowl floor further out
                float startR = platformRadius;
                float endR = platformRadius + (bowlRadius - platformRadius) * 0.55f;

                int stripVerts = (bridgeSegsAlong + 1) * 2;
                int bvCount = stripVerts * 2; // duplicate for backface normals
                int btCount = bridgeSegsAlong * 6 * 2; // front + back triangles
                Vector3[] bVerts = new Vector3[bvCount];
                int[] bTris = new int[btCount];

                Vector3 perp = new Vector3(-sinA * axisX, 0f, cosA * axisZ).normalized;

                // Build front-face strip verts
                for (int s = 0; s <= bridgeSegsAlong; s++)
                {
                    float t = (float)s / bridgeSegsAlong;
                    float r = Mathf.Lerp(startR, endR, t);
                    float by = Mathf.Lerp(topY, floorY, t); // ramp down

                    Vector3 center = new Vector3(cosA * r * axisX, by, sinA * r * axisZ);
                    float hw = bridgeWidth * 0.5f * Mathf.Lerp(1f, 1.3f, t);

                    bVerts[s * 2] = center - perp * hw;
                    bVerts[s * 2 + 1] = center + perp * hw;
                }

                // Snap inner-edge verts (s=0) onto the platform circle
                for (int side = 0; side < 2; side++)
                {
                    Vector3 v = bVerts[side];
                    float dist = Mathf.Sqrt(v.x * v.x + v.z * v.z);
                    if (dist > 0.001f)
                    {
                        float scale = platformRadius / dist;
                        bVerts[side] = new Vector3(v.x * scale, topY, v.z * scale);
                    }
                }

                // Copy front-face verts to back-face verts (separate for normals)
                for (int s = 0; s <= bridgeSegsAlong; s++)
                {
                    bVerts[stripVerts + s * 2] = bVerts[s * 2];
                    bVerts[stripVerts + s * 2 + 1] = bVerts[s * 2 + 1];
                }

                int bi = 0;
                for (int s = 0; s < bridgeSegsAlong; s++)
                {
                    int c0 = s * 2;
                    int c1 = s * 2 + 1;
                    int n0 = (s + 1) * 2;
                    int n1 = (s + 1) * 2 + 1;

                    // Front face (top)
                    bTris[bi++] = c0; bTris[bi++] = n1; bTris[bi++] = n0;
                    bTris[bi++] = c0; bTris[bi++] = c1; bTris[bi++] = n1;

                    // Back face (bottom) — reversed winding
                    int bc0 = stripVerts + c0;
                    int bc1 = stripVerts + c1;
                    int bn0 = stripVerts + n0;
                    int bn1 = stripVerts + n1;
                    bTris[bi++] = bc0; bTris[bi++] = bn0; bTris[bi++] = bn1;
                    bTris[bi++] = bc0; bTris[bi++] = bn1; bTris[bi++] = bc1;
                }

                Mesh bridgeMesh = new Mesh { name = $"{name}_Bridge_{b}" };
                bridgeMesh.vertices = bVerts;
                bridgeMesh.triangles = bTris;
                bridgeMesh.RecalculateNormals();
                bridgeMesh.RecalculateBounds();

                GameObject bridge = new GameObject($"Bridge_{b}");
                bridge.transform.SetParent(root.transform, false);
                bridge.AddComponent<MeshFilter>().sharedMesh = bridgeMesh;
                bridge.AddComponent<MeshRenderer>().sharedMaterial =
                    CreateArenaMaterial(color * 0.95f, 0.28f, 0.32f);
                bridge.AddComponent<MeshCollider>().sharedMesh = bridgeMesh;
            }

            return root;
        }

        // ================================================================
        // RIM WALLS — curved arc walls following the bowl perimeter.
        // Matches ProceduralArenaGenerator.CreateRimWall: separate mesh faces
        // for inner/outer/top/bottom so normals are correct on every surface.
        // ================================================================
        private static GameObject CreateSegmentedRimWalls(string name, Vector3[] ringPoints, float lipHeight)
        {
            GameObject root = new GameObject(name);
            if (ringPoints == null || ringPoints.Length < 8)
                return root;

            int totalPts  = ringPoints.Length;
            int wallCount = Mathf.Clamp(Mathf.RoundToInt(totalPts / 14f), 4, 8);

            // 66.6 % coverage matches ProceduralArenaGenerator
            int ptsPerWall = Mathf.Max(4, Mathf.RoundToInt(totalPts * 0.666f / wallCount));
            int halfPts    = ptsPerWall / 2;

            float wallH = GameConstants.ARENA_RIM_HEIGHT;
            float thick = GameConstants.ARENA_RIM_THICKNESS;

            for (int w = 0; w < wallCount; w++)
            {
                int centerIdx = Mathf.RoundToInt(w * totalPts / (float)wallCount) % totalPts;
                int segs = ptsPerWall; // segs quad-strips need segs+1 sample points

                // Build inner/outer bottom/top positions along the arc
                Vector3[] ib = new Vector3[segs + 1];
                Vector3[] it = new Vector3[segs + 1];
                Vector3[] ot = new Vector3[segs + 1];
                Vector3[] ob = new Vector3[segs + 1];

                for (int k = 0; k <= segs; k++)
                {
                    int ptIdx = WrapIndex(centerIdx - halfPts + k, totalPts);
                    Vector3 p = ringPoints[ptIdx];

                    // Outward direction perpendicular to path tangent in XZ, pointing away from bowl
                    Vector3 pv = ringPoints[WrapIndex(ptIdx - 1, totalPts)];
                    Vector3 pn = ringPoints[WrapIndex(ptIdx + 1, totalPts)];
                    Vector3 tang = new Vector3(pn.x - pv.x, 0f, pn.z - pv.z).normalized;
                    Vector3 outw = Vector3.Cross(Vector3.up, tang).normalized;
                    if (Vector3.Dot(outw, new Vector3(p.x, 0f, p.z).normalized) < 0f)
                        outw = -outw;

                    ib[k] = new Vector3(p.x,              p.y,          p.z);
                    it[k] = new Vector3(p.x,              p.y + wallH,  p.z);
                    ot[k] = new Vector3(p.x + outw.x * thick, p.y + wallH, p.z + outw.z * thick);
                    ob[k] = new Vector3(p.x + outw.x * thick, p.y,         p.z + outw.z * thick);
                }

                // Mesh: 4 quad-strip faces + 2 side caps, unique verts per face for hard normals
                int totalVerts = segs * 4 * 4 + 2 * 4;
                int totalTris  = segs * 6 * 4 + 2 * 6;
                Vector3[] verts = new Vector3[totalVerts];
                int[] tris      = new int[totalTris];
                int vi = 0, ti = 0;

                // Local helper — captures arrays from this iteration
                void Quad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
                {
                    int s = vi;
                    verts[vi++] = v0; verts[vi++] = v1;
                    verts[vi++] = v2; verts[vi++] = v3;
                    tris[ti++] = s;     tris[ti++] = s + 1; tris[ti++] = s + 2;
                    tris[ti++] = s;     tris[ti++] = s + 2; tris[ti++] = s + 3;
                }

                for (int i = 0; i < segs; i++)
                {
                    // Inner face: normal points inward (toward bowl center)
                    Quad(ib[i + 1], it[i + 1], it[i], ib[i]);
                    // Outer face: normal points outward
                    Quad(ob[i], ot[i], ot[i + 1], ob[i + 1]);
                    // Top face: normal points up
                    Quad(it[i + 1], ot[i + 1], ot[i], it[i]);
                    // Bottom face: normal points down
                    Quad(ib[i], ob[i], ob[i + 1], ib[i + 1]);
                }

                // Side end-caps
                Quad(ib[0], it[0], ot[0], ob[0]);
                Quad(ob[segs], ot[segs], it[segs], ib[segs]);

                Mesh mesh = new Mesh { name = $"RimWall_{w}" };
                mesh.vertices  = verts;
                mesh.triangles = tris;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                GameObject wall = new GameObject($"RimWall_{w}");
                wall.transform.SetParent(root.transform, false);
                wall.AddComponent<MeshFilter>().sharedMesh   = mesh;
                wall.AddComponent<MeshRenderer>().sharedMaterial =
                    CreateArenaMaterial(new Color(0.55f, 0.55f, 0.6f), 0.5f, 0.4f);
                wall.AddComponent<MeshCollider>().sharedMesh = mesh;
            }

            return root;
        }

        private static int WrapIndex(int value, int size)
        {
            if (size <= 0)
                return 0;

            int result = value % size;
            return result < 0 ? result + size : result;
        }

        private static void AddQuad(Vector3[] verts, ref int v, List<int> tris, bool flip, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            int s = v;
            verts[v++] = v0;
            verts[v++] = v1;
            verts[v++] = v2;
            verts[v++] = v3;

            if (!flip)
            {
                tris.Add(s);
                tris.Add(s + 1);
                tris.Add(s + 2);
                tris.Add(s);
                tris.Add(s + 2);
                tris.Add(s + 3);
            }
            else
            {
                tris.Add(s);
                tris.Add(s + 2);
                tris.Add(s + 1);
                tris.Add(s);
                tris.Add(s + 3);
                tris.Add(s + 2);
            }
        }

        private static Material CreateArenaMaterial(Color color, float metallic, float smoothness)
        {
            Shader shader = ShaderProvider.URPLit;
            Material mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }

        private static void SetLayerRecursive(GameObject obj, int layer)
        {
            if (obj == null)
                return;

            obj.layer = layer;
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Transform child = obj.transform.GetChild(i);
                if (child != null)
                    SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}