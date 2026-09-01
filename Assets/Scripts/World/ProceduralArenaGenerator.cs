using System.Collections.Generic;
using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay;

namespace BladeSpinners.World
{
    /// <summary>
    /// Generates procedural beyblade arenas — concave bowls with optional
    /// inner walls, low platforms, rim walls, and pickup placeholders.
    /// Everything is seed-driven so each arena is reproducible.
    /// </summary>
    public static class ProceduralArenaGenerator
    {
        // Shared zero-friction physics material for all arena colliders.
        // Prevents beys from catching on polygon edges at steep angles.
        private static PhysicsMaterial _arenaPhysicsMaterial;
        private static PhysicsMaterial ArenaPhysicsMaterial
        {
            get
            {
                if (_arenaPhysicsMaterial == null)
                {
                    // (23/3/2026): Reinforced zero-bounce material to prevent seam bounces
                    _arenaPhysicsMaterial = new PhysicsMaterial("ArenaZeroFriction");
                    _arenaPhysicsMaterial.dynamicFriction = 0f;
                    _arenaPhysicsMaterial.staticFriction = 0f;
                    _arenaPhysicsMaterial.bounciness = 0f;  // Absolute zero bounce
                    _arenaPhysicsMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
                    _arenaPhysicsMaterial.bounceCombine = PhysicsMaterialCombine.Minimum;  // Force no bounce even on seams
                }
                return _arenaPhysicsMaterial;
            }
        }

        /// <summary>Applies the shared zero-friction material to a MeshCollider.</summary>
        private static void ApplyArenaPhysicsMaterial(MeshCollider mc)
        {
            if (mc != null)
                mc.sharedMaterial = ArenaPhysicsMaterial;
        }

        // ================================================================
        // PUBLIC API
        // ================================================================

        /// <summary>
        /// Generate a complete arena as a root GameObject.
        /// </summary>
        /// <param name="seed">Seed for reproducible generation.</param>
        /// <param name="roomType">Room type affects pickup counts and feature density.</param>
        /// <returns>Root GameObject containing all arena parts.</returns>
        public static GameObject Generate(int seed, RoomType roomType = RoomType.Combat)
        {
            return Generate(seed, roomType, -1, -1, -1, -1);
        }

        /// <summary>
        /// Generate an arena with explicit control over feature counts.
        /// Pass -1 for any parameter to use seed-random values.
        /// </summary>
        public static GameObject Generate(int seed, RoomType roomType,
            int outerWalls, int innerWalls, int staminaPickups, int manaPickups)
        {
            System.Random rng = new System.Random(seed);

            // ---- Pick a random arena shape from the 12 prototypes ----
            ArenaShapeDefinition[] shapes = ArenaShapeLibrary.GetAllShapes();
            ArenaShapeDefinition shape = shapes[rng.Next(shapes.Length)];

            GameObject root = new GameObject($"Arena_{seed}_{shape.Name}");

            // Use the shape's parameters (with slight random variation on depth)
            float radius = shape.Radius;
            float depth = shape.Depth * Mathf.Lerp(0.85f, 1.15f, (float)rng.NextDouble());
            float flatRatio = shape.FlatRatio;

            // ---- Build the bowl mesh using the shape's footprint ----
            GameObject bowl = CreateShapedBowl(shape, depth, rng);
            bowl.transform.SetParent(root.transform, false);

            // Rim walls and lip are now built by CreateShapedBowl using the shape's outer ring.

            // Shared occupied-position list: (x, z, footprintRadius)
            // Prevents obstacles from overlapping each other.
            List<Vector3> occupied = new List<Vector3>();

            // Exclude center hole area from obstacle placement
            bool hasHole = shape.HoleRadiusRatio > 0.001f;
            float holeExclusionRadius = hasHole ? radius * shape.HoleRadiusRatio + 1f : 0f;
            if (holeExclusionRadius > 0f)
                occupied.Add(new Vector3(0f, 0f, holeExclusionRadius));

            // ---- Inner walls ----
            int innerWallCount = innerWalls >= 0 ? innerWalls
                : rng.Next(0, GameConstants.ARENA_MAX_INNER_WALLS + 1);
            for (int i = 0; i < innerWallCount; i++)
            {
                GameObject wall = CreateInnerWall(radius, flatRatio, depth, rng, i, occupied);
                if (wall != null) wall.transform.SetParent(root.transform, false);
            }

            // ---- Low platforms ----
            int platformCount = rng.Next(0, GameConstants.ARENA_MAX_PLATFORMS + 1);
            for (int i = 0; i < platformCount; i++)
            {
                GameObject platform = CreatePlatform(radius, flatRatio, depth, rng, i, occupied);
                if (platform != null) platform.transform.SetParent(root.transform, false);
            }

            // ---- Ramps ----
            int rampCount = rng.Next(0, GameConstants.ARENA_MAX_RAMPS + 1);
            for (int i = 0; i < rampCount; i++)
            {
                GameObject ramp = CreateRamp(radius, flatRatio, depth, rng, i, occupied);
                if (ramp != null) ramp.transform.SetParent(root.transform, false);
            }

            // ---- Bumpers (half-spheres) ----
            int bumperCount = rng.Next(0, GameConstants.ARENA_MAX_BUMPERS + 1);
            for (int i = 0; i < bumperCount; i++)
            {
                GameObject bumper = CreateBumper(radius, flatRatio, depth, rng, i, occupied);
                if (bumper != null) bumper.transform.SetParent(root.transform, false);
            }

            // ---- Pillars ----
            int pillarCount = rng.Next(0, GameConstants.ARENA_MAX_PILLARS + 1);
            for (int i = 0; i < pillarCount; i++)
            {
                GameObject pillar = CreatePillar(radius, flatRatio, depth, rng, i, occupied);
                if (pillar != null) pillar.transform.SetParent(root.transform, false);
            }

            // ---- Spires (thin tall pillars) ----
            int spireCount = rng.Next(0, GameConstants.ARENA_MAX_SPIRES + 1);
            for (int i = 0; i < spireCount; i++)
            {
                GameObject spire = CreateSpire(radius, flatRatio, depth, rng, i, occupied);
                if (spire != null) spire.transform.SetParent(root.transform, false);
            }

            // ---- Pickup placeholders ----
            int stamCount = staminaPickups >= 0 ? staminaPickups : -1;
            int manaCount = manaPickups >= 0 ? manaPickups : -1;

            if (stamCount < 0 || manaCount < 0)
            {
                int totalRandom = GetPickupCount(roomType, rng);
                if (stamCount < 0 && manaCount < 0)
                {
                    // Split randomly
                    stamCount = totalRandom / 2;
                    manaCount = totalRandom - stamCount;
                }
                else if (stamCount < 0)
                    stamCount = totalRandom;
                else
                    manaCount = totalRandom;
            }

            int totalPickups = stamCount + manaCount;

            // Pre-compute evenly distributed positions across the bowl using golden angle.
            // This ensures pickups are spread uniformly rather than clumped randomly.
            float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f)); // ~137.5°
            float angleOffset = (float)(rng.NextDouble() * Mathf.PI * 2f); // random starting rotation

            for (int i = 0; i < totalPickups; i++)
            {
                bool isMana = i >= stamCount; // first stamCount are stamina, rest are mana
                int pickupIndex = isMana ? i - stamCount : i;

                // Sunflower / golden-angle distribution for even spacing
                float angle = angleOffset + i * goldenAngle;
                // Radius: spread from center outward using sqrt for equal-area distribution
                float radFrac = Mathf.Sqrt((float)(i + 1) / (totalPickups + 1));
                // Keep within playable bowl area (flat zone + lower curve)
                float maxPlacementRadius = radius * (flatRatio + 0.25f);
                float placementRadius = hasHole
                    ? Mathf.Lerp(holeExclusionRadius, maxPlacementRadius, radFrac)
                    : radFrac * maxPlacementRadius;

                float x = Mathf.Cos(angle) * placementRadius;
                float z = Mathf.Sin(angle) * placementRadius;

                GameObject pickup = CreatePickupPlaceholder(
                    shape,
                    depth,
                    x,
                    z,
                    pickupIndex,
                    isMana);
                pickup.transform.SetParent(root.transform, false);
            }

            // Ground layer for collision detection
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0)
            {
                Debug.LogError("[ProceduralArenaGenerator] Required 'Ground' layer is missing. Falling back to Default.");
                groundLayer = 0;
            }
            SetLayerRecursive(root, groundLayer);
            PickupPlaceholder[] pickups =
                root.GetComponentsInChildren<PickupPlaceholder>(true);
            for (int i = 0; i < pickups.Length; i++)
                SetLayerRecursive(pickups[i].gameObject, 0);

            // ---- Distant Tournament Stadium Backdrop (League of Legends / AAA style) ----
            GameObject backdrop = CreateDistantStadiumBackdrop(radius, rng);
            if (backdrop != null) backdrop.transform.SetParent(root.transform, false);

            return root;
        }

        // ================================================================
        // BOWL — concave dish with flat center transitioning to curved walls
        // ================================================================

        private static GameObject CreateShapedBowl(ArenaShapeDefinition shape, float depthOverride, System.Random rng)
        {
            int ringSegs = GameConstants.ARENA_RING_SEGMENTS;
            int radSegs = GameConstants.ARENA_RADIAL_SEGMENTS;

            // Apply depth override while keeping shape definition intact for surface eval
            ArenaShapeDefinition s = shape;
            s.Depth = depthOverride;

            bool hasHole = s.HoleRadiusRatio > 0.001f;
            float innerNorm = Mathf.Clamp01(s.HoleRadiusRatio);
            bool hasCenter = !hasHole;

            int firstRingStep = hasCenter ? 1 : 0;
            int ringCount = (radSegs + 1) - firstRingStep;

            int vertexOffset = hasCenter ? 1 : 0;
            int vertCount = vertexOffset + ringCount * ringSegs;
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];

            if (hasCenter)
            {
                float centerY = ArenaShapeLibrary.EvaluateSurfaceHeight(0f, s);
                vertices[0] = new Vector3(0f, centerY, 0f);
                uvs[0] = new Vector2(0.5f, 0.5f);
            }

            Vector3[] outerRingPoints = new Vector3[ringSegs];
            Vector3[] holeRingPoints = hasHole ? new Vector3[ringSegs] : null;

            for (int ring = 0; ring < ringCount; ring++)
            {
                float norm = hasCenter
                    ? (float)(ring + 1) / radSegs
                    : Mathf.Lerp(innerNorm, 1f, ring / Mathf.Max(1f, ringCount - 1f));

                for (int a = 0; a < ringSegs; a++)
                {
                    float t = (float)a / ringSegs;
                    float angle = t * Mathf.PI * 2f;
                    float boundary = Mathf.Max(0.5f, ArenaShapeLibrary.EvaluateBoundaryRadius(angle, s));
                    float dist = boundary * norm;

                    float x = Mathf.Cos(angle) * dist * s.AxisX;
                    float z = Mathf.Sin(angle) * dist * s.AxisZ;
                    float y = ArenaShapeLibrary.EvaluateSurfaceHeight(norm, s);

                    int idx = vertexOffset + ring * ringSegs + a;
                    vertices[idx] = new Vector3(x, y, z);
                    uvs[idx] = new Vector2(0.5f + Mathf.Cos(angle) * norm * 0.5f,
                                           0.5f + Mathf.Sin(angle) * norm * 0.5f);

                    if (ring == ringCount - 1)
                        outerRingPoints[a] = vertices[idx];
                    if (hasHole && ring == 0)
                        holeRingPoints[a] = vertices[idx];
                }
            }

            List<int> triangles = new List<int>(ringSegs * radSegs * 6 + ringSegs * 6);

            if (hasCenter)
            {
                for (int a = 0; a < ringSegs; a++)
                {
                    int next = (a + 1) % ringSegs;
                    int cur = vertexOffset + a;
                    int nxt = vertexOffset + next;
                    triangles.Add(0);
                    triangles.Add(nxt);
                    triangles.Add(cur);
                }
            }

            for (int ring = 0; ring < ringCount - 1; ring++)
            {
                for (int a = 0; a < ringSegs; a++)
                {
                    int next = (a + 1) % ringSegs;
                    int cur = vertexOffset + ring * ringSegs + a;
                    int curNext = vertexOffset + ring * ringSegs + next;
                    int outCur = vertexOffset + (ring + 1) * ringSegs + a;
                    int outNext = vertexOffset + (ring + 1) * ringSegs + next;

                    triangles.Add(cur);
                    triangles.Add(curNext);
                    triangles.Add(outNext);

                    triangles.Add(cur);
                    triangles.Add(outNext);
                    triangles.Add(outCur);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = $"{s.Name}_ArenaBowl";
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject bowl = new GameObject("Bowl");
            MeshFilter mf = bowl.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = bowl.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CreateArenaMaterial(s.Color, 0.22f, 0.33f);

            MeshCollider mc = bowl.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            ApplyArenaPhysicsMaterial(mc);

            // Perimeter lip around the outer rim
            GameObject outerLip = CreatePerimeterLip($"{s.Name}_OuterLip", outerRingPoints, s.LipHeight, s.LipThickness, false);
            outerLip.transform.SetParent(bowl.transform, false);

            // Segmented rim walls
            GameObject rimWalls = CreateSegmentedRimWalls($"{s.Name}_RimWalls", outerRingPoints, s.LipHeight);
            rimWalls.transform.SetParent(bowl.transform, false);

            // Hole lip for donut shapes
            if (hasHole && holeRingPoints != null)
            {
                GameObject holeLip = CreatePerimeterLip(
                    $"{s.Name}_HoleLip", holeRingPoints, s.HoleLipHeight, s.HoleLipThickness, true);
                holeLip.transform.SetParent(bowl.transform, false);
            }

            // Raised platform with bridges (DualTier, positive InnerTierExtraDepth)
            if (s.InnerTierRadiusRatio > 0.001f && s.InnerTierExtraDepth > 0.001f)
            {
                float platformRadius = s.Radius * s.InnerTierRadiusRatio;
                float floorY = ArenaShapeLibrary.EvaluateSurfaceHeight(s.InnerTierRadiusRatio, s);
                float platformHeight = s.InnerTierExtraDepth;
                GameObject platform = CreateRaisedPlatformWithBridges(
                    $"{s.Name}_Platform", platformRadius, floorY, platformHeight,
                    s.Radius, s.AxisX, s.AxisZ, ringSegs, s.Color);
                platform.transform.SetParent(bowl.transform, false);
            }

            return bowl;
        }

        // ================================================================
        // PERIMETER LIP — torus-like lip following the outer ring points
        // ================================================================

        private static GameObject CreatePerimeterLip(string name, Vector3[] ringPoints,
            float lipHeight, float lipThickness, bool inner)
        {
            GameObject lip = new GameObject(name);
            if (ringPoints == null || ringPoints.Length < 4 || lipHeight < 0.01f || lipThickness < 0.01f)
                return lip;

            int ringCount = ringPoints.Length;
            float r = lipThickness * 0.5f;
            int tubeSegs = 8;

            int vertCount = ringCount * tubeSegs;
            Vector3[] verts = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];

            for (int i = 0; i < ringCount; i++)
            {
                Vector3 p = ringPoints[i];
                Vector3 pPrev = ringPoints[(i - 1 + ringCount) % ringCount];
                Vector3 pNext = ringPoints[(i + 1) % ringCount];
                Vector3 tangent = (pNext - pPrev).normalized;
                Vector3 outward = Vector3.Cross(Vector3.up, tangent).normalized;
                if (inner) outward = -outward;

                // Center of the tube ring at this point (offset to sit on top of the rim)
                Vector3 center = p + Vector3.up * r;

                for (int j = 0; j < tubeSegs; j++)
                {
                    float phi = (float)j / tubeSegs * Mathf.PI * 2f;
                    float cosPhi = Mathf.Cos(phi);
                    float sinPhi = Mathf.Sin(phi);

                    Vector3 offset = outward * (r * cosPhi) + Vector3.up * (r * sinPhi);
                    int idx = i * tubeSegs + j;
                    verts[idx] = center + offset;
                    uvs[idx] = new Vector2((float)i / ringCount, (float)j / tubeSegs);
                }
            }

            int triCount = ringCount * tubeSegs * 6;
            int[] tris = new int[triCount];
            int ti = 0;

            for (int i = 0; i < ringCount; i++)
            {
                int iNext = (i + 1) % ringCount;
                for (int j = 0; j < tubeSegs; j++)
                {
                    int jNext = (j + 1) % tubeSegs;
                    int v00 = i * tubeSegs + j;
                    int v01 = i * tubeSegs + jNext;
                    int v10 = iNext * tubeSegs + j;
                    int v11 = iNext * tubeSegs + jNext;

                    if (inner)
                    {
                        tris[ti++] = v00; tris[ti++] = v10; tris[ti++] = v11;
                        tris[ti++] = v00; tris[ti++] = v11; tris[ti++] = v01;
                    }
                    else
                    {
                        tris[ti++] = v00; tris[ti++] = v11; tris[ti++] = v10;
                        tris[ti++] = v00; tris[ti++] = v01; tris[ti++] = v11;
                    }
                }
            }

            Mesh torusMesh = new Mesh { name = name };
            torusMesh.vertices = verts;
            torusMesh.uv = uvs;
            torusMesh.triangles = tris;
            torusMesh.RecalculateNormals();
            torusMesh.RecalculateBounds();

            MeshFilter mfLip = lip.AddComponent<MeshFilter>();
            mfLip.sharedMesh = torusMesh;
            MeshRenderer mrLip = lip.AddComponent<MeshRenderer>();
            mrLip.sharedMaterial = CreateArenaMaterial(new Color(0.3f, 0.3f, 0.35f), 0.3f, 0.4f);
            MeshCollider mcLip = lip.AddComponent<MeshCollider>();
            mcLip.sharedMesh = torusMesh;
            ApplyArenaPhysicsMaterial(mcLip);

            return lip;
        }

        // ================================================================
        // SEGMENTED RIM WALLS — curved arc walls around the bowl perimeter
        // ================================================================

        private static GameObject CreateSegmentedRimWalls(string name, Vector3[] ringPoints, float lipHeight)
        {
            GameObject root = new GameObject(name);
            if (ringPoints == null || ringPoints.Length < 8)
                return root;

            int totalPts = ringPoints.Length;
            int wallCount = Mathf.Clamp(Mathf.RoundToInt(totalPts / 14f), 4, 8);
            int ptsPerWall = Mathf.Max(4, Mathf.RoundToInt(totalPts * 0.666f / wallCount));
            int halfPts = ptsPerWall / 2;

            float wallH = GameConstants.ARENA_RIM_HEIGHT;
            float thick = GameConstants.ARENA_RIM_THICKNESS;

            for (int w = 0; w < wallCount; w++)
            {
                int centerIdx = Mathf.RoundToInt(w * totalPts / (float)wallCount) % totalPts;
                int segs = ptsPerWall;

                Vector3[] ib = new Vector3[segs + 1];
                Vector3[] it = new Vector3[segs + 1];
                Vector3[] ot = new Vector3[segs + 1];
                Vector3[] ob = new Vector3[segs + 1];

                for (int k = 0; k <= segs; k++)
                {
                    int ptIdx = ((centerIdx - halfPts + k) % totalPts + totalPts) % totalPts;
                    Vector3 p = ringPoints[ptIdx];

                    Vector3 pv = ringPoints[((ptIdx - 1) % totalPts + totalPts) % totalPts];
                    Vector3 pn = ringPoints[(ptIdx + 1) % totalPts];
                    Vector3 tang = new Vector3(pn.x - pv.x, 0f, pn.z - pv.z).normalized;
                    Vector3 outw = Vector3.Cross(Vector3.up, tang).normalized;
                    if (Vector3.Dot(outw, new Vector3(p.x, 0f, p.z).normalized) < 0f)
                        outw = -outw;

                    ib[k] = new Vector3(p.x, p.y, p.z);
                    it[k] = new Vector3(p.x, p.y + wallH, p.z);
                    ot[k] = new Vector3(p.x + outw.x * thick, p.y + wallH, p.z + outw.z * thick);
                    ob[k] = new Vector3(p.x + outw.x * thick, p.y, p.z + outw.z * thick);
                }

                int totalVerts = segs * 4 * 4 + 2 * 4;
                int totalTris = segs * 6 * 4 + 2 * 6;
                Vector3[] verts = new Vector3[totalVerts];
                int[] tris = new int[totalTris];
                int vi = 0, ti = 0;

                void Quad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
                {
                    int start = vi;
                    verts[vi++] = v0; verts[vi++] = v1;
                    verts[vi++] = v2; verts[vi++] = v3;
                    tris[ti++] = start; tris[ti++] = start + 1; tris[ti++] = start + 2;
                    tris[ti++] = start; tris[ti++] = start + 2; tris[ti++] = start + 3;
                }

                for (int i = 0; i < segs; i++)
                {
                    Quad(ib[i + 1], it[i + 1], it[i], ib[i]);
                    Quad(ob[i], ot[i], ot[i + 1], ob[i + 1]);
                    Quad(it[i + 1], ot[i + 1], ot[i], it[i]);
                    Quad(ib[i], ob[i], ob[i + 1], ib[i + 1]);
                }

                Quad(ib[0], it[0], ot[0], ob[0]);
                Quad(ob[segs], ot[segs], it[segs], ib[segs]);

                Mesh wallMesh = new Mesh { name = $"{name}_Wall_{w}" };
                wallMesh.vertices = verts;
                wallMesh.triangles = tris;
                wallMesh.RecalculateNormals();
                wallMesh.RecalculateBounds();

                GameObject wall = new GameObject($"Wall_{w}");
                wall.transform.SetParent(root.transform, false);
                wall.AddComponent<MeshFilter>().sharedMesh = wallMesh;
                wall.AddComponent<MeshRenderer>().sharedMaterial =
                    CreateArenaMaterial(new Color(0.55f, 0.55f, 0.6f), 0.5f, 0.4f);
                MeshCollider wallMc = wall.AddComponent<MeshCollider>();
                wallMc.sharedMesh = wallMesh;
                ApplyArenaPhysicsMaterial(wallMc);
            }

            return root;
        }

        // ================================================================
        // RAISED PLATFORM WITH BRIDGES — DualTier inner platform
        // ================================================================

        private static GameObject CreateRaisedPlatformWithBridges(
            string name, float platformRadius, float floorY, float platformHeight,
            float bowlRadius, float axisX, float axisZ, int angSeg, Color color)
        {
            GameObject root = new GameObject(name);
            float topY = floorY + platformHeight;

            int platSegs = Mathf.Max(24, angSeg / 2);
            int halfVerts = platSegs + 1;
            int platVertCount = halfVerts * 2;
            Vector3[] pVerts = new Vector3[platVertCount];
            Vector2[] pUvs = new Vector2[platVertCount];

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
                pTris[i * 3] = 0;
                pTris[i * 3 + 1] = nxt;
                pTris[i * 3 + 2] = cur;
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
            MeshCollider discMc = disc.AddComponent<MeshCollider>();
            discMc.sharedMesh = platMesh;
            ApplyArenaPhysicsMaterial(discMc);

            int bridgeCount = 4;
            float bridgeWidth = platformRadius * 0.55f;
            int bridgeSegsAlong = 8;

            for (int b = 0; b < bridgeCount; b++)
            {
                float bAngle = b * Mathf.PI * 2f / bridgeCount;
                float cosA = Mathf.Cos(bAngle);
                float sinA = Mathf.Sin(bAngle);

                float startR = platformRadius;
                float endR = platformRadius + (bowlRadius - platformRadius) * 0.55f;

                int stripVerts = (bridgeSegsAlong + 1) * 2;
                int bvCount = stripVerts * 2;
                int btCount = bridgeSegsAlong * 6 * 2;
                Vector3[] bVerts = new Vector3[bvCount];
                int[] bTris = new int[btCount];

                Vector3 perp = new Vector3(-sinA * axisX, 0f, cosA * axisZ).normalized;

                for (int seg = 0; seg <= bridgeSegsAlong; seg++)
                {
                    float t = (float)seg / bridgeSegsAlong;
                    float r = Mathf.Lerp(startR, endR, t);
                    float by = Mathf.Lerp(topY, floorY, t);

                    Vector3 center = new Vector3(cosA * r * axisX, by, sinA * r * axisZ);
                    float hw = bridgeWidth * 0.5f * Mathf.Lerp(1f, 1.3f, t);

                    bVerts[seg * 2] = center - perp * hw;
                    bVerts[seg * 2 + 1] = center + perp * hw;
                }

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

                for (int seg = 0; seg <= bridgeSegsAlong; seg++)
                {
                    bVerts[stripVerts + seg * 2] = bVerts[seg * 2];
                    bVerts[stripVerts + seg * 2 + 1] = bVerts[seg * 2 + 1];
                }

                int bi = 0;
                for (int seg = 0; seg < bridgeSegsAlong; seg++)
                {
                    int c0 = seg * 2;
                    int c1 = seg * 2 + 1;
                    int n0 = (seg + 1) * 2;
                    int n1 = (seg + 1) * 2 + 1;

                    bTris[bi++] = c0; bTris[bi++] = n1; bTris[bi++] = n0;
                    bTris[bi++] = c0; bTris[bi++] = c1; bTris[bi++] = n1;

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
                MeshCollider bridgeMc = bridge.AddComponent<MeshCollider>();
                bridgeMc.sharedMesh = bridgeMesh;
                ApplyArenaPhysicsMaterial(bridgeMc);
            }

            return root;
        }

        // ================================================================
        // INNER WALLS — short walls inside the bowl for deflections
        // ================================================================

        private static GameObject CreateInnerWall(float arenaRadius, float flatRatio, float depth,
            System.Random rng, int index, List<Vector3> occupied)
        {
            // Wall dimensions (rolled first so we know footprint for overlap check)
            float wallLength = 0.8f + (float)rng.NextDouble() * 2.5f;
            float wallHeight = 0.3f + (float)rng.NextDouble() * (GameConstants.ARENA_INNER_WALL_MAX_HEIGHT - 0.3f);
            float wallThick = 0.15f + (float)rng.NextDouble() * 0.2f;
            float footprint = Mathf.Max(wallLength, wallThick) * 0.5f;

            float minR = arenaRadius * flatRatio;
            float maxR = arenaRadius * 0.7f;
            if (!TryFindPlacement(rng, minR, maxR, footprint, occupied, out float x, out float z))
                return null;

            float placementRadius = Mathf.Sqrt(x * x + z * z);
            float y = GetBowlHeight(placementRadius, arenaRadius, flatRatio, depth);
            float yRotation = (float)rng.NextDouble() * 360f;

            // Base of the wall must sit below the arena's lowest point (-depth)
            float baseY = -depth - 0.2f;
            float topY = y + wallHeight;
            float fullHeight = topY - baseY;

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = $"InnerWall_{index}";
            wall.transform.localScale = new Vector3(wallLength, fullHeight, wallThick);
            wall.transform.position = new Vector3(x, baseY + fullHeight * 0.5f, z);
            wall.transform.rotation = Quaternion.Euler(0, yRotation, 0);

            Renderer rend = wall.GetComponent<Renderer>();
            rend.sharedMaterial = CreateArenaMaterial(
                new Color(0.5f, 0.45f, 0.4f), 0.3f, 0.2f);

            Collider wallCol = wall.GetComponent<Collider>();
            if (wallCol != null) wallCol.sharedMaterial = ArenaPhysicsMaterial;

            return wall;
        }

        // ================================================================
        // PLATFORMS — low raised surfaces inside the bowl  
        // ================================================================

        private static GameObject CreatePlatform(float arenaRadius, float flatRatio, float depth,
            System.Random rng, int index, List<Vector3> occupied)
        {
            float platRadius = 0.6f + (float)rng.NextDouble() * 1.5f;
            float platHeight = 0.1f + (float)rng.NextDouble() * GameConstants.ARENA_PLATFORM_MAX_HEIGHT;

            if (!TryFindPlacement(rng, 0f, arenaRadius * flatRatio * 1.3f, platRadius, occupied, out float x, out float z))
                return null;

            float placementRadius = Mathf.Sqrt(x * x + z * z);
            float y = GetBowlHeight(placementRadius, arenaRadius, flatRatio, depth);

            // Use a cylinder primitive
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = $"Platform_{index}";

            // Base of the platform must sit below the arena's lowest point (-depth)
            float baseY = -depth - 0.2f;
            float topY = y + platHeight;
            float fullHeight = topY - baseY;
            platform.transform.localScale = new Vector3(platRadius * 2f, fullHeight * 0.5f, platRadius * 2f);
            platform.transform.position = new Vector3(x, baseY + fullHeight * 0.5f, z);

            // Ensure platforms use a true cylindrical collider shape.
            // Unity doesn't provide a built-in CylinderCollider, so use MeshCollider
            // from the cylinder mesh for reliable shape matching.
            Collider existingCollider = platform.GetComponent<Collider>();
            if (existingCollider != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(existingCollider);
                }
                else
                {
                    Object.DestroyImmediate(existingCollider);
                }
            }

            MeshCollider platformCollider = platform.AddComponent<MeshCollider>();
            platformCollider.convex = false;
            ApplyArenaPhysicsMaterial(platformCollider);

            Renderer rend = platform.GetComponent<Renderer>();
            rend.sharedMaterial = CreateArenaMaterial(
                new Color(0.45f, 0.5f, 0.55f), 0.4f, 0.35f); // blue-grey

            return platform;
        }

        // ================================================================
        // PICKUP COLLECTIBLES — trigger colliders + visual spheres
        // ================================================================

        private static GameObject CreatePickupPlaceholder(
            ArenaShapeDefinition shape,
            float depth,
            float x, float z, int index, bool isMana)
        {
            float y = GetSurfaceHeight(shape, depth, x, z);

            GameObject pickup = new GameObject(
                isMana
                    ? $"ManaPickup_{index}"
                    : $"SpinPickup_{index}");
            pickup.transform.localPosition = new Vector3(
                x,
                y + GameConstants.PICKUP_SPAWN_HEIGHT,
                z);

            // Trigger collider for collection — larger radius for easier pickup
            SphereCollider trigger = pickup.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = GameConstants.PICKUP_TRIGGER_RADIUS;

            // Visual: billboard sprite (Fire_Focus for mana, Lightning_Focus for spin/stamina)
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(pickup.transform, false);
            visual.transform.localScale = Vector3.one * 0.075f;

            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            string spriteName = isMana ? "Fire_Focus" : "Lightning_Focus";
            Texture2D tex = Resources.Load<Texture2D>(spriteName);
            if (tex != null)
            {
                sr.sprite = Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 100f);
            }
            sr.color = new Color(1f, 1f, 1f, 0.4f); // 40% opacity
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;

            // Add the placeholder tag component
            pickup.AddComponent<PickupPlaceholder>().Initialize(
                isMana ? PickupType.Mana : PickupType.SpinMedium);

            // Add bob + billboard animation
            pickup.AddComponent<PickupBobAnimation>();

            return pickup;
        }

        public static float GetSurfaceHeight(
            ArenaShapeDefinition shape,
            float depth,
            float x,
            float z)
        {
            ArenaShapeDefinition evaluatedShape = shape;
            evaluatedShape.Depth = depth;
            float axisX = Mathf.Max(0.01f, evaluatedShape.AxisX);
            float axisZ = Mathf.Max(0.01f, evaluatedShape.AxisZ);
            float scaledX = x / axisX;
            float scaledZ = z / axisZ;
            float angle = Mathf.Atan2(scaledZ, scaledX);
            float distance = Mathf.Sqrt(
                scaledX * scaledX + scaledZ * scaledZ);
            float boundary = Mathf.Max(
                0.01f,
                ArenaShapeLibrary.EvaluateBoundaryRadius(
                    angle,
                    evaluatedShape));
            return ArenaShapeLibrary.EvaluateSurfaceHeight(
                distance / boundary,
                evaluatedShape);
        }

        // ================================================================
        // RAMPS — wedge-shaped obstacles that launch beys upward
        // ================================================================

        private static GameObject CreateRamp(float arenaRadius, float flatRatio, float depth,
            System.Random rng, int index, List<Vector3> occupied)
        {
            float rampLength = 1.5f + (float)rng.NextDouble() * 2.5f;
            float rampWidth = 1f + (float)rng.NextDouble() * 1.5f;
            float footprint = Mathf.Max(rampLength, rampWidth) * 0.5f;

            float minR = arenaRadius * flatRatio;
            float maxR = arenaRadius * 0.6f;
            if (!TryFindPlacement(rng, minR, maxR, footprint, occupied, out float x, out float z))
                return null;

            float placementRadius = Mathf.Sqrt(x * x + z * z);
            float y = GetBowlHeight(placementRadius, arenaRadius, flatRatio, depth);
            float rampHeight = 0.3f + (float)rng.NextDouble() * 0.8f;
            float yRotation = (float)rng.NextDouble() * 360f;

            // Base must sit below the arena's lowest point
            float baseY = -depth - 0.2f;
            float sinkOffset = y - baseY; // how far below surface origin to shift verts

            // Wedge mesh: 6 verts (triangular prism)
            Vector3[] verts = new Vector3[6];
            // Bottom face (starts at -sinkOffset so base goes below arena floor)
            verts[0] = new Vector3(-rampWidth * 0.5f, -sinkOffset, 0f);
            verts[1] = new Vector3( rampWidth * 0.5f, -sinkOffset, 0f);
            verts[2] = new Vector3(-rampWidth * 0.5f, -sinkOffset, rampLength);
            verts[3] = new Vector3( rampWidth * 0.5f, -sinkOffset, rampLength);
            // Top edge (raised back)
            verts[4] = new Vector3(-rampWidth * 0.5f, rampHeight, 0f);
            verts[5] = new Vector3( rampWidth * 0.5f, rampHeight, 0f);

            // Build with unique verts per face for hard normals
            int faceVertCount = 5 * 4 + 2 * 3; // 5 quads + 2 triangles
            Vector3[] fv = new Vector3[faceVertCount];
            int[] ft = new int[(5 * 6) + (2 * 3)];
            int fvi = 0, fti = 0;

            void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int s = fvi;
                fv[fvi++] = a; fv[fvi++] = b; fv[fvi++] = c; fv[fvi++] = d;
                ft[fti++] = s; ft[fti++] = s+1; ft[fti++] = s+2;
                ft[fti++] = s; ft[fti++] = s+2; ft[fti++] = s+3;
            }
            void AddTri(Vector3 a, Vector3 b, Vector3 c)
            {
                int s = fvi;
                fv[fvi++] = a; fv[fvi++] = b; fv[fvi++] = c;
                ft[fti++] = s; ft[fti++] = s+1; ft[fti++] = s+2;
            }

            // Ramp surface (top slope)
            AddQuad(verts[2], verts[3], verts[5], verts[4]);
            // Bottom
            AddQuad(verts[0], verts[1], verts[3], verts[2]);
            // Back wall
            AddQuad(verts[4], verts[5], verts[1], verts[0]);
            // Left side
            AddTri(verts[0], verts[2], verts[4]);
            // Right side
            AddTri(verts[1], verts[5], verts[3]);

            Mesh mesh = new Mesh { name = $"Ramp_{index}" };
            mesh.vertices = fv;
            mesh.triangles = ft;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject ramp = new GameObject($"Ramp_{index}");
            ramp.transform.position = new Vector3(x, y, z);
            ramp.transform.rotation = Quaternion.Euler(0, yRotation, 0);
            ramp.AddComponent<MeshFilter>().sharedMesh = mesh;
            ramp.AddComponent<MeshRenderer>().sharedMaterial =
                CreateArenaMaterial(new Color(0.55f, 0.50f, 0.45f), 0.35f, 0.25f);
            MeshCollider rampMc = ramp.AddComponent<MeshCollider>();
            rampMc.sharedMesh = mesh;
            ApplyArenaPhysicsMaterial(rampMc);

            return ramp;
        }

        // ================================================================
        // BUMPERS — half-sphere obstacles that deflect beys on contact
        // ================================================================

        private static GameObject CreateBumper(float arenaRadius, float flatRatio, float depth,
            System.Random rng, int index, List<Vector3> occupied)
        {
            float bumperRadius = 0.4f + (float)rng.NextDouble() * 0.8f;

            if (!TryFindPlacement(rng, 0f, arenaRadius * (flatRatio + 0.3f), bumperRadius, occupied, out float x, out float z))
                return null;

            float placementRadius = Mathf.Sqrt(x * x + z * z);
            float y = GetBowlHeight(placementRadius, arenaRadius, flatRatio, depth);

            // Half-sphere mesh
            int hSegs = 12;
            int vSegs = 6; // only top hemisphere
            int vertCount = (hSegs + 1) * (vSegs + 1);
            Vector3[] bVerts = new Vector3[vertCount];
            Vector2[] bUvs = new Vector2[vertCount];
            int vi = 0;

            for (int v = 0; v <= vSegs; v++)
            {
                float phi = (float)v / vSegs * Mathf.PI * 0.5f; // 0 to 90°
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);
                for (int h = 0; h <= hSegs; h++)
                {
                    float theta = (float)h / hSegs * Mathf.PI * 2f;
                    bVerts[vi] = new Vector3(
                        Mathf.Cos(theta) * sinPhi * bumperRadius,
                        cosPhi * bumperRadius,
                        Mathf.Sin(theta) * sinPhi * bumperRadius);
                    bUvs[vi] = new Vector2((float)h / hSegs, (float)v / vSegs);
                    vi++;
                }
            }

            // Flip so dome points up: reverse the hemisphere (phi 0=top, vSegs=equator)
            // Actually the loop above creates top at v=0 (phi=0, y=bumperRadius) going down.
            // That's correct for a dome sitting on the ground at y=0.

            int triCount = hSegs * vSegs * 6;
            int[] bTris = new int[triCount];
            int ti = 0;
            for (int v = 0; v < vSegs; v++)
            {
                for (int h = 0; h < hSegs; h++)
                {
                    int c = v * (hSegs + 1) + h;
                    int n = c + hSegs + 1;
                    bTris[ti++] = c; bTris[ti++] = c + 1; bTris[ti++] = n + 1;
                    bTris[ti++] = c; bTris[ti++] = n + 1; bTris[ti++] = n;
                }
            }

            Mesh mesh = new Mesh { name = $"Bumper_{index}" };
            mesh.vertices = bVerts;
            mesh.uv = bUvs;
            mesh.triangles = bTris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject bumper = new GameObject($"Bumper_{index}");
            bumper.transform.position = new Vector3(x, y, z);
            bumper.AddComponent<MeshFilter>().sharedMesh = mesh;
            bumper.AddComponent<MeshRenderer>().sharedMaterial =
                CreateArenaMaterial(new Color(0.7f, 0.25f, 0.25f), 0.5f, 0.5f); // red-ish
            MeshCollider bumperMc = bumper.AddComponent<MeshCollider>();
            bumperMc.sharedMesh = mesh;
            bumperMc.convex = true;
            ApplyArenaPhysicsMaterial(bumperMc);

            return bumper;
        }

        // ================================================================
        // PILLARS — tall cylindrical columns inside the bowl
        // ================================================================

        private static GameObject CreatePillar(float arenaRadius, float flatRatio, float depth,
            System.Random rng, int index, List<Vector3> occupied)
        {
            float pillarRadius = 0.3f + (float)rng.NextDouble() * 0.5f;

            float minR = arenaRadius * flatRatio * 0.5f;
            float maxR = arenaRadius * flatRatio * 1.7f;
            if (!TryFindPlacement(rng, minR, maxR, pillarRadius, occupied, out float x, out float z))
                return null;

            float placementRadius = Mathf.Sqrt(x * x + z * z);
            float y = GetBowlHeight(placementRadius, arenaRadius, flatRatio, depth);
            float pillarHeight = 1f + (float)rng.NextDouble() * 2.5f;

            float baseY = -depth - 0.2f;
            float topY = y + pillarHeight;
            float fullHeight = topY - baseY;

            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = $"Pillar_{index}";
            pillar.transform.localScale = new Vector3(pillarRadius * 2f, fullHeight * 0.5f, pillarRadius * 2f);
            pillar.transform.position = new Vector3(x, baseY + fullHeight * 0.5f, z);

            Renderer rend = pillar.GetComponent<Renderer>();
            rend.sharedMaterial = CreateArenaMaterial(
                new Color(0.4f, 0.4f, 0.5f), 0.6f, 0.4f); // grey-blue metallic

            Collider pillarCol = pillar.GetComponent<Collider>();
            if (pillarCol != null) pillarCol.sharedMaterial = ArenaPhysicsMaterial;

            return pillar;
        }

        // ================================================================
        // SPIRES — thin, tall cylindrical pillars
        // ================================================================

        private static GameObject CreateSpire(float arenaRadius, float flatRatio, float depth,
            System.Random rng, int index, List<Vector3> occupied)
        {
            float spireRadius = 0.1f + (float)rng.NextDouble() * 0.15f;

            float minR = arenaRadius * flatRatio * 0.3f;
            float maxR = arenaRadius * flatRatio * 1.5f;
            if (!TryFindPlacement(rng, minR, maxR, spireRadius, occupied, out float x, out float z))
                return null;

            float placementRadius = Mathf.Sqrt(x * x + z * z);
            float y = GetBowlHeight(placementRadius, arenaRadius, flatRatio, depth);
            float spireHeight = 3f + (float)rng.NextDouble() * 4f;

            float baseY = -depth - 0.2f;
            float topY = y + spireHeight;
            float fullHeight = topY - baseY;

            GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spire.name = $"Spire_{index}";
            spire.transform.localScale = new Vector3(spireRadius * 2f, fullHeight * 0.5f, spireRadius * 2f);
            spire.transform.position = new Vector3(x, baseY + fullHeight * 0.5f, z);

            Renderer rend = spire.GetComponent<Renderer>();
            rend.sharedMaterial = CreateArenaMaterial(
                new Color(0.35f, 0.35f, 0.45f), 0.7f, 0.5f); // dark metallic

            Collider spireCol = spire.GetComponent<Collider>();
            if (spireCol != null) spireCol.sharedMaterial = ArenaPhysicsMaterial;

            return spire;
        }

        // ================================================================
        // HELPERS
        // ================================================================

        // ================================================================
        // PLACEMENT OVERLAP PREVENTION
        // ================================================================

        private const int MaxPlacementAttempts = 15;

        /// <summary>
        /// Try up to MaxPlacementAttempts random positions within [minR, maxR] from center.
        /// Returns false if no non-overlapping position could be found.
        /// On success, adds the position to the occupied list.
        /// occupied entries: (x, z, footprintRadius).
        /// </summary>
        private static bool TryFindPlacement(System.Random rng, float minR, float maxR,
            float footprint, List<Vector3> occupied, out float x, out float z)
        {
            for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                float r = minR + (float)rng.NextDouble() * (maxR - minR);
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float cx = Mathf.Cos(angle) * r;
                float cz = Mathf.Sin(angle) * r;

                bool overlaps = false;
                for (int i = 0; i < occupied.Count; i++)
                {
                    Vector3 occ = occupied[i];
                    float dx = cx - occ.x;
                    float dz = cz - occ.y; // y stores z-coord
                    float minDist = footprint + occ.z; // z stores radius
                    if (dx * dx + dz * dz < minDist * minDist)
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    occupied.Add(new Vector3(cx, cz, footprint));
                    x = cx;
                    z = cz;
                    return true;
                }
            }

            x = 0f;
            z = 0f;
            return false;
        }

        /// <summary>
        /// Returns the Y height of the bowl surface at a given radial distance from center.
        /// </summary>
        public static float GetBowlHeight(float distFromCenter, float arenaRadius, float flatRatio, float depth)
        {
            float flatRadius = arenaRadius * flatRatio;
            if (distFromCenter <= flatRadius)
                return -depth;

            float curveFrac = (distFromCenter - flatRadius) / (arenaRadius - flatRadius);
            curveFrac = Mathf.Clamp01(curveFrac);
            return -depth + depth * (curveFrac * curveFrac);
        }

        private static int GetPickupCount(RoomType roomType, System.Random rng)
        {
            return roomType switch
            {
                RoomType.Loot => GameConstants.ARENA_MAX_PICKUPS,
                RoomType.Boss => GameConstants.BOSS_ROOM_PICKUPS,
                RoomType.Combat => 2 + rng.Next(0, GameConstants.ARENA_MAX_PICKUPS - 1),
                RoomType.TreasureChest => GameConstants.ARENA_MAX_PICKUPS,
                _ => rng.Next(1, 4)
            };
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

        private static GameObject CreateDistantStadiumBackdrop(float arenaRadius, System.Random rng)
        {
            GameObject backdrop = new GameObject("DistantStadiumBackdrop");

            Material concreteMat = CreateArenaMaterial(new Color(0.04f, 0.07f, 0.12f, 1f), 0.35f, 0.40f);
            Material metalMat = CreateArenaMaterial(new Color(0.08f, 0.14f, 0.22f, 1f), 0.85f, 0.65f);
            Material cyanEmissive = ShaderProvider.CreateEmissiveMaterial(new Color(0f, 0.85f, 1f, 1f), 2.5f);
            Material orangeEmissive = ShaderProvider.CreateEmissiveMaterial(new Color(1f, 0.45f, 0.05f, 1f), 2.5f);
            Material yellowEmissive = ShaderProvider.CreateEmissiveMaterial(new Color(1f, 0.85f, 0.15f, 1f), 2.2f);
            Material magentaEmissive = ShaderProvider.CreateEmissiveMaterial(new Color(0.95f, 0.15f, 0.80f, 1f), 2.2f);

            // 1. Concentric Grandstand Bleacher Rings
            int ringSegments = 36;
            float[] tierRadii = new float[] { arenaRadius + 8f, arenaRadius + 18f, arenaRadius + 30f, arenaRadius + 44f };
            float[] tierHeights = new float[] { 3f, 8f, 16f, 26f };

            for (int t = 0; t < tierRadii.Length - 1; t++)
            {
                float innerR = tierRadii[t];
                float outerR = tierRadii[t + 1];
                float botY = tierHeights[t];
                float topY = tierHeights[t + 1];

                GameObject tierObj = new GameObject($"Stadium_Tier_{t}");
                tierObj.transform.SetParent(backdrop.transform, false);

                MeshFilter mf = tierObj.AddComponent<MeshFilter>();
                MeshRenderer mr = tierObj.AddComponent<MeshRenderer>();
                mr.sharedMaterial = concreteMat;

                Mesh mesh = new Mesh();
                mesh.name = $"StadiumTierMesh_{t}";

                Vector3[] verts = new Vector3[(ringSegments + 1) * 2];
                Vector2[] uvs = new Vector2[(ringSegments + 1) * 2];
                int[] tris = new int[ringSegments * 6];

                for (int i = 0; i <= ringSegments; i++)
                {
                    float angle = (Mathf.PI * 2f * i) / ringSegments;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);

                    verts[i * 2] = new Vector3(cos * innerR, botY, sin * innerR);
                    verts[i * 2 + 1] = new Vector3(cos * outerR, topY, sin * outerR);

                    uvs[i * 2] = new Vector2((float)i / ringSegments * 6f, 0f);
                    uvs[i * 2 + 1] = new Vector2((float)i / ringSegments * 6f, 1f);
                }

                int tri = 0;
                for (int i = 0; i < ringSegments; i++)
                {
                    int b0 = i * 2;
                    int t0 = i * 2 + 1;
                    int b1 = (i + 1) * 2;
                    int t1 = (i + 1) * 2 + 1;

                    tris[tri++] = b0;
                    tris[tri++] = t0;
                    tris[tri++] = t1;

                    tris[tri++] = b0;
                    tris[tri++] = t1;
                    tris[tri++] = b1;
                }

                mesh.vertices = verts;
                mesh.uv = uvs;
                mesh.triangles = tris;
                mesh.RecalculateNormals();
                mf.sharedMesh = mesh;
            }

            // 2. Spectator Crowd Glow Flecks across the Grandstands
            int crowdPillars = 48;
            for (int i = 0; i < crowdPillars; i++)
            {
                float angle = (float)i / crowdPillars * Mathf.PI * 2f + ((float)rng.NextDouble() * 0.08f);
                float rDist = Mathf.Lerp(arenaRadius + 10f, arenaRadius + 42f, (float)rng.NextDouble());
                float yPos = Mathf.Lerp(4f, 24f, (rDist - (arenaRadius + 10f)) / 32f) + (float)rng.NextDouble() * 0.8f;

                GameObject crowdCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crowdCube.name = "CrowdLight";
                crowdCube.transform.SetParent(backdrop.transform, false);
                crowdCube.transform.position = new Vector3(Mathf.Cos(angle) * rDist, yPos, Mathf.Sin(angle) * rDist);
                crowdCube.transform.localScale = new Vector3(0.6f + (float)rng.NextDouble() * 0.6f, 0.3f, 1.2f);
                crowdCube.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);

                // Remove collider so it never interacts with gameplay physics
                Collider col = crowdCube.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);

                MeshRenderer cr = crowdCube.GetComponent<MeshRenderer>();
                int colorPick = rng.Next(4);
                cr.sharedMaterial = colorPick switch
                {
                    0 => cyanEmissive,
                    1 => orangeEmissive,
                    2 => yellowEmissive,
                    _ => magentaEmissive
                };
            }

            // 3. Tournament Holographic Pylon Spires (8 Massive Spires)
            int spireCount = 8;
            float spireRadius = arenaRadius + 32f;
            for (int i = 0; i < spireCount; i++)
            {
                float angle = (float)i / spireCount * Mathf.PI * 2f;
                Vector3 spirePos = new Vector3(Mathf.Cos(angle) * spireRadius, 0f, Mathf.Sin(angle) * spireRadius);

                GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                spire.name = $"TournamentSpire_{i}";
                spire.transform.SetParent(backdrop.transform, false);
                spire.transform.position = spirePos + Vector3.up * 26f;
                spire.transform.localScale = new Vector3(3.2f, 26f, 3.2f);

                Collider col = spire.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);

                MeshRenderer mr = spire.GetComponent<MeshRenderer>();
                mr.sharedMaterial = metalMat;

                // Emissive Neon Rib on Spire
                GameObject rib = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rib.name = "SpireNeonRib";
                rib.transform.SetParent(spire.transform, false);
                rib.transform.localPosition = new Vector3(0f, 0f, 0.55f);
                rib.transform.localScale = new Vector3(0.2f, 1f, 0.2f);

                Collider ribCol = rib.GetComponent<Collider>();
                if (ribCol != null) Object.Destroy(ribCol);

                MeshRenderer ribMr = rib.GetComponent<MeshRenderer>();
                ribMr.sharedMaterial = (i % 2 == 0) ? cyanEmissive : orangeEmissive;
            }

            // 4. Floating Hologram Jumbotron Screens (4 Screens)
            int screenCount = 4;
            float screenRadius = arenaRadius + 20f;
            for (int i = 0; i < screenCount; i++)
            {
                float angle = ((float)i / screenCount + 0.125f) * Mathf.PI * 2f;
                Vector3 screenPos = new Vector3(Mathf.Cos(angle) * screenRadius, 20f, Mathf.Sin(angle) * screenRadius);

                GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
                screen.name = $"HoloJumbotron_{i}";
                screen.transform.SetParent(backdrop.transform, false);
                screen.transform.position = screenPos;
                screen.transform.localScale = new Vector3(14f, 6.5f, 0.5f);

                // Look at arena center, slightly tilted down
                Vector3 toCenter = (Vector3.up * 2f - screenPos).normalized;
                screen.transform.rotation = Quaternion.LookRotation(toCenter);

                Collider col = screen.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);

                MeshRenderer mr = screen.GetComponent<MeshRenderer>();
                mr.sharedMaterial = (i % 2 == 0) ? cyanEmissive : magentaEmissive;
            }

            // 5. Sky Lasers / Beacons shooting into the upper atmosphere
            int laserCount = 6;
            float laserRadius = arenaRadius + 40f;
            for (int i = 0; i < laserCount; i++)
            {
                float angle = (float)i / laserCount * Mathf.PI * 2f;
                Vector3 laserPos = new Vector3(Mathf.Cos(angle) * laserRadius, 45f, Mathf.Sin(angle) * laserRadius);

                GameObject laser = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                laser.name = $"SkyLaser_{i}";
                laser.transform.SetParent(backdrop.transform, false);
                laser.transform.position = laserPos;
                laser.transform.localScale = new Vector3(0.7f, 45f, 0.7f);

                Collider col = laser.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);

                MeshRenderer mr = laser.GetComponent<MeshRenderer>();
                mr.sharedMaterial = (i % 2 == 0) ? cyanEmissive : orangeEmissive;
            }

            return backdrop;
        }

        private static void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            for (int i = 0; i < obj.transform.childCount; i++)
                SetLayerRecursive(obj.transform.GetChild(i).gameObject, layer);
        }
    }
}
