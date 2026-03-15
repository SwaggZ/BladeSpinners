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

            GameObject root = new GameObject($"Arena_{seed}");

            // ---- Core bowl parameters from seed ----
            float radius = Mathf.Lerp(GameConstants.ARENA_MIN_RADIUS, GameConstants.ARENA_MAX_RADIUS,
                (float)rng.NextDouble());
            float depth = Mathf.Lerp(GameConstants.ARENA_MIN_DEPTH, GameConstants.ARENA_MAX_DEPTH,
                (float)rng.NextDouble());
            float flatRatio = GameConstants.ARENA_FLOOR_FLAT_RATIO + (float)rng.NextDouble() * 0.15f;

            // ---- Build the bowl mesh ----
            GameObject bowl = CreateBowl(radius, depth, flatRatio, rng);
            bowl.transform.SetParent(root.transform, false);

            // ---- Rim wall segments ----
            int rimWallCount = outerWalls >= 0 ? outerWalls
                : GameConstants.ARENA_MIN_RIM_WALLS +
                  rng.Next(0, GameConstants.ARENA_MAX_RIM_WALLS - GameConstants.ARENA_MIN_RIM_WALLS + 1);
            if (rimWallCount > 0)
            {
                GameObject rimParent = new GameObject("Rim");
                rimParent.transform.SetParent(root.transform, false);
                for (int i = 0; i < rimWallCount; i++)
                {
                    GameObject wall = CreateRimWall(radius, rimWallCount, i, rng);
                    wall.transform.SetParent(rimParent.transform, false);
                }
            }

            // ---- Inner walls ----
            int innerWallCount = innerWalls >= 0 ? innerWalls
                : rng.Next(0, GameConstants.ARENA_MAX_INNER_WALLS + 1);
            for (int i = 0; i < innerWallCount; i++)
            {
                GameObject wall = CreateInnerWall(radius, flatRatio, depth, rng, i);
                wall.transform.SetParent(root.transform, false);
            }

            // ---- Low platforms ----
            int platformCount = rng.Next(0, GameConstants.ARENA_MAX_PLATFORMS + 1);
            for (int i = 0; i < platformCount; i++)
            {
                GameObject platform = CreatePlatform(radius, flatRatio, depth, rng, i);
                platform.transform.SetParent(root.transform, false);
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
                float placementRadius = radFrac * maxPlacementRadius;

                float x = Mathf.Cos(angle) * placementRadius;
                float z = Mathf.Sin(angle) * placementRadius;

                GameObject pickup = CreatePickupPlaceholder(radius, flatRatio, depth, x, z, pickupIndex, isMana);
                pickup.transform.SetParent(root.transform, false);
            }

            // Ground layer for collision detection
            int groundLayer = LayerMask.NameToLayer("Default");
            SetLayerRecursive(root, groundLayer);

            return root;
        }

        // ================================================================
        // BOWL — concave dish with flat center transitioning to curved walls
        // ================================================================

        private static GameObject CreateBowl(float radius, float depth, float flatRatio, System.Random rng)
        {
            int ringSegs = GameConstants.ARENA_RING_SEGMENTS;
            int radSegs = GameConstants.ARENA_RADIAL_SEGMENTS;

            // Total verts: center + (radSegs * ringSegs)
            int vertCount = 1 + radSegs * ringSegs;
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];

            // Center vertex at bowl bottom
            vertices[0] = new Vector3(0, -depth, 0);
            uvs[0] = new Vector2(0.5f, 0.5f);

            float flatRadius = radius * flatRatio;

            for (int r = 0; r < radSegs; r++)
            {
                float rFrac = (float)(r + 1) / radSegs; // 0→1 from center to rim
                float dist = rFrac * radius;

                // Height profile: flat bottom, then parabolic curve up to rim (y=0)
                float y;
                if (dist <= flatRadius)
                {
                    y = -depth; // flat floor
                }
                else
                {
                    // Parabolic rise from flat edge to rim
                    float curveFrac = (dist - flatRadius) / (radius - flatRadius); // 0→1
                    y = -depth + depth * (curveFrac * curveFrac); // quadratic ease-in
                }

                for (int a = 0; a < ringSegs; a++)
                {
                    float angle = (float)a / ringSegs * Mathf.PI * 2f;
                    float x = Mathf.Cos(angle) * dist;
                    float z = Mathf.Sin(angle) * dist;

                    int idx = 1 + r * ringSegs + a;
                    vertices[idx] = new Vector3(x, y, z);
                    uvs[idx] = new Vector2(0.5f + Mathf.Cos(angle) * rFrac * 0.5f,
                                           0.5f + Mathf.Sin(angle) * rFrac * 0.5f);
                }
            }

            // Triangles — center fan + ring quads (no lip, torus is separate)
            int triCount = ringSegs * 3 + (radSegs - 1) * ringSegs * 6;
            int[] triangles = new int[triCount];
            int ti = 0;

            for (int a = 0; a < ringSegs; a++)
            {
                int next = (a + 1) % ringSegs;
                triangles[ti++] = 0;
                triangles[ti++] = 1 + next;
                triangles[ti++] = 1 + a;
            }

            // Ring quads
            for (int r = 0; r < radSegs - 1; r++)
            {
                for (int a = 0; a < ringSegs; a++)
                {
                    int next = (a + 1) % ringSegs;
                    int cur = 1 + r * ringSegs + a;
                    int curNext = 1 + r * ringSegs + next;
                    int outerCur = 1 + (r + 1) * ringSegs + a;
                    int outerNext = 1 + (r + 1) * ringSegs + next;

                    triangles[ti++] = cur;
                    triangles[ti++] = curNext;
                    triangles[ti++] = outerNext;

                    triangles[ti++] = cur;
                    triangles[ti++] = outerNext;
                    triangles[ti++] = outerCur;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "ArenaBowl";
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject bowl = new GameObject("Bowl");
            MeshFilter mf = bowl.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = bowl.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CreateArenaMaterial(
                new Color(0.35f, 0.35f, 0.4f), 0.2f, 0.3f); // dark grey, slight metallic

            MeshCollider mc = bowl.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            // Bowl is concave — do NOT mark convex

            // Add torus lip as a child object around the rim
            GameObject torusLip = CreateTorusLip(radius, depth, ringSegs);
            torusLip.transform.SetParent(bowl.transform, false);

            return bowl;
        }

        // ================================================================
        // TORUS LIP — small donut ring sitting on the bowl rim edge
        // Keeps beys inside while looking smooth and rounded.
        // ================================================================

        private static GameObject CreateTorusLip(float arenaRadius, float bowlDepth, int ringSegs)
        {
            // Torus parameters
            // R = major radius (center of tube follows this circle at the arena rim)
            // r = minor radius (tube cross-section radius — "thickness")
            float R = arenaRadius;
            float r = bowlDepth * 0.06f; // thin tube, scales with bowl depth
            if (r < 0.15f) r = 0.15f;    // minimum visible thickness
            int tubeSegs = 8;             // cross-section resolution

            // Vertex count: ringSegs * tubeSegs (a grid wrapped into a torus)
            int vertCount = ringSegs * tubeSegs;
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];

            for (int i = 0; i < ringSegs; i++)
            {
                float theta = (float)i / ringSegs * Mathf.PI * 2f; // angle around arena
                float cosTheta = Mathf.Cos(theta);
                float sinTheta = Mathf.Sin(theta);

                for (int j = 0; j < tubeSegs; j++)
                {
                    float phi = (float)j / tubeSegs * Mathf.PI * 2f; // angle around tube
                    float cosPhi = Mathf.Cos(phi);
                    float sinPhi = Mathf.Sin(phi);

                    // Torus parametric surface:
                    // x = (R + r*cos(phi)) * cos(theta)
                    // y = r * sin(phi)
                    // z = (R + r*cos(phi)) * sin(theta)
                    float dist = R + r * cosPhi;
                    int idx = i * tubeSegs + j;
                    vertices[idx] = new Vector3(
                        dist * cosTheta,
                        r * sinPhi,       // centered at y=0 (bowl rim level)
                        dist * sinTheta
                    );
                    uvs[idx] = new Vector2((float)i / ringSegs, (float)j / tubeSegs);
                }
            }

            // Triangles: each quad on the torus surface = 2 triangles
            int triCount = ringSegs * tubeSegs * 6;
            int[] triangles = new int[triCount];
            int ti = 0;

            for (int i = 0; i < ringSegs; i++)
            {
                int iNext = (i + 1) % ringSegs;
                for (int j = 0; j < tubeSegs; j++)
                {
                    int jNext = (j + 1) % tubeSegs;

                    int v00 = i * tubeSegs + j;
                    int v01 = i * tubeSegs + jNext;
                    int v10 = iNext * tubeSegs + j;
                    int v11 = iNext * tubeSegs + jNext;

                    // Two triangles per quad (outward-facing normals)
                    triangles[ti++] = v00;
                    triangles[ti++] = v11;
                    triangles[ti++] = v10;

                    triangles[ti++] = v00;
                    triangles[ti++] = v01;
                    triangles[ti++] = v11;
                }
            }

            Mesh torusMesh = new Mesh();
            torusMesh.name = "TorusLip";
            torusMesh.vertices = vertices;
            torusMesh.uv = uvs;
            torusMesh.triangles = triangles;
            torusMesh.RecalculateNormals();
            torusMesh.RecalculateBounds();

            GameObject lip = new GameObject("TorusLip");

            MeshFilter mf = lip.AddComponent<MeshFilter>();
            mf.sharedMesh = torusMesh;

            MeshRenderer mr = lip.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CreateArenaMaterial(
                new Color(0.3f, 0.3f, 0.35f), 0.3f, 0.4f); // slightly darker than bowl, more metallic

            MeshCollider mc = lip.AddComponent<MeshCollider>();
            mc.sharedMesh = torusMesh;

            return lip;
        }

        // ================================================================
        // RIM WALLS — 1-8 evenly spaced wall segments around the bowl lip
        // Built with separate vertices per face for clean normals.
        // ================================================================

        private static GameObject CreateRimWall(float radius, int totalWalls, int wallIndex, System.Random rng)
        {
            float h = GameConstants.ARENA_RIM_HEIGHT * (0.8f + (float)rng.NextDouble() * 0.4f);
            float thick = GameConstants.ARENA_RIM_THICKNESS;
            float rIn = radius;
            float rOut = radius + thick;

            // Angular slot for this wall
            float slotSize = (Mathf.PI * 2f) / totalWalls;
            // With >1 wall, all walls together cover 66.6% of the circumference.
            // With 1 wall, use the default arc fraction of the full circle.
            float wallArc = totalWalls > 1
                ? (Mathf.PI * 2f * 0.666f) / totalWalls   // 66.6% of circumference split evenly
                : slotSize * GameConstants.ARENA_RIM_WALL_ARC_FRACTION;
            float center = slotSize * wallIndex;
            float a0 = center - wallArc * 0.5f;
            float a1 = center + wallArc * 0.5f;

            int segs = Mathf.Max(4, Mathf.CeilToInt(64 * (wallArc / (Mathf.PI * 2f))));

            // Pre-compute positions along the arc for inner/outer at bottom/top
            // For each arc point i (0..segs): 4 positions
            //   IB = inner-bottom, IT = inner-top, OT = outer-top, OB = outer-bottom
            Vector3[] ib = new Vector3[segs + 1];
            Vector3[] it = new Vector3[segs + 1];
            Vector3[] ot = new Vector3[segs + 1];
            Vector3[] ob = new Vector3[segs + 1];

            for (int i = 0; i <= segs; i++)
            {
                float t = (float)i / segs;
                float angle = Mathf.Lerp(a0, a1, t);
                float c = Mathf.Cos(angle);
                float s = Mathf.Sin(angle);

                ib[i] = new Vector3(c * rIn, 0, s * rIn);
                it[i] = new Vector3(c * rIn, h, s * rIn);
                ot[i] = new Vector3(c * rOut, h, s * rOut);
                ob[i] = new Vector3(c * rOut, 0, s * rOut);
            }

            // Build mesh with unique verts per face for hard edges
            // 4 quad-strip faces (inner, outer, top, bottom) + 2 side-cap quads
            // Each strip quad = 4 verts, 6 indices
            int stripQuads = segs;
            int totalVerts = stripQuads * 4 * 4 + 2 * 4; // 4 strips + 2 caps
            int totalTris = stripQuads * 6 * 4 + 2 * 6;

            Vector3[] verts = new Vector3[totalVerts];
            int[] tris = new int[totalTris];
            int vi = 0, ti = 0;

            // Helper: add a quad with 4 unique verts (v0,v1,v2,v3 in CCW order when viewed from front)
            // Front face = v0→v1→v2, v0→v2→v3
            void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
            {
                int start = vi;
                verts[vi++] = v0;
                verts[vi++] = v1;
                verts[vi++] = v2;
                verts[vi++] = v3;
                tris[ti++] = start;     tris[ti++] = start + 1; tris[ti++] = start + 2;
                tris[ti++] = start;     tris[ti++] = start + 2; tris[ti++] = start + 3;
            }

            for (int i = 0; i < segs; i++)
            {
                // Inner face — normal points inward (toward bowl center)
                // Viewed from inside: bottom-left, top-left, top-right, bottom-right
                // Arc goes left-to-right when viewed from inside, so i is left, i+1 is right
                AddQuad(ib[i + 1], it[i + 1], it[i], ib[i]);

                // Outer face — normal points outward (away from bowl)
                // Viewed from outside: i is left, i+1 is right
                AddQuad(ob[i], ot[i], ot[i + 1], ob[i + 1]);

                // Top face — normal points up
                AddQuad(it[i + 1], ot[i + 1], ot[i], it[i]);

                // Bottom face — normal points down
                AddQuad(ib[i], ob[i], ob[i + 1], ib[i + 1]);
            }

            // Left side cap (i=0) — normal points along -arc direction
            // Quad corners: ib[0], ob[0], ot[0], it[0] — viewed from left side
            AddQuad(ib[0], it[0], ot[0], ob[0]);

            // Right side cap (i=segs) — normal points along +arc direction
            AddQuad(ob[segs], ot[segs], it[segs], ib[segs]);

            Mesh mesh = new Mesh();
            mesh.name = $"RimWall_{wallIndex}";
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject wall = new GameObject($"RimWall_{wallIndex}");
            MeshFilter mf = wall.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = wall.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CreateArenaMaterial(
                new Color(0.55f, 0.55f, 0.6f), 0.5f, 0.4f);

            MeshCollider mc = wall.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            return wall;
        }

        // ================================================================
        // INNER WALLS — short walls inside the bowl for deflections
        // ================================================================

        private static GameObject CreateInnerWall(float arenaRadius, float flatRatio, float depth,
            System.Random rng, int index)
        {
            // Random position inside the bowl (biased toward middle zone)
            float placementRadius = arenaRadius * (flatRatio + (float)rng.NextDouble() * (0.7f - flatRatio));
            float placementAngle = (float)rng.NextDouble() * Mathf.PI * 2f;

            float x = Mathf.Cos(placementAngle) * placementRadius;
            float z = Mathf.Sin(placementAngle) * placementRadius;
            float y = GetBowlHeight(placementRadius, arenaRadius, flatRatio, depth);

            // Wall dimensions
            float wallLength = 0.8f + (float)rng.NextDouble() * 2.5f;
            float wallHeight = 0.3f + (float)rng.NextDouble() * (GameConstants.ARENA_INNER_WALL_MAX_HEIGHT - 0.3f);
            float wallThick = 0.15f + (float)rng.NextDouble() * 0.2f;

            // Random rotation around Y
            float yRotation = (float)rng.NextDouble() * 360f;

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = $"InnerWall_{index}";
            wall.transform.localScale = new Vector3(wallLength, wallHeight, wallThick);
            wall.transform.position = new Vector3(x, y + wallHeight * 0.5f, z);
            wall.transform.rotation = Quaternion.Euler(0, yRotation, 0);

            // Material
            Renderer rend = wall.GetComponent<Renderer>();
            rend.sharedMaterial = CreateArenaMaterial(
                new Color(0.5f, 0.45f, 0.4f), 0.3f, 0.2f); // brown-grey

            return wall;
        }

        // ================================================================
        // PLATFORMS — low raised surfaces inside the bowl  
        // ================================================================

        private static GameObject CreatePlatform(float arenaRadius, float flatRatio, float depth,
            System.Random rng, int index)
        {
            float placementRadius = arenaRadius * (float)rng.NextDouble() * flatRatio * 1.3f;
            float placementAngle = (float)rng.NextDouble() * Mathf.PI * 2f;

            float x = Mathf.Cos(placementAngle) * placementRadius;
            float z = Mathf.Sin(placementAngle) * placementRadius;
            float y = GetBowlHeight(placementRadius, arenaRadius, flatRatio, depth);

            // Platform: wide, short cylinder
            float platRadius = 0.6f + (float)rng.NextDouble() * 1.5f;
            float platHeight = 0.1f + (float)rng.NextDouble() * GameConstants.ARENA_PLATFORM_MAX_HEIGHT;

            // Use a cylinder primitive
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = $"Platform_{index}";
            platform.transform.localScale = new Vector3(platRadius * 2f, platHeight * 0.5f, platRadius * 2f);
            platform.transform.position = new Vector3(x, y + platHeight * 0.5f, z);

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

            Renderer rend = platform.GetComponent<Renderer>();
            rend.sharedMaterial = CreateArenaMaterial(
                new Color(0.45f, 0.5f, 0.55f), 0.4f, 0.35f); // blue-grey

            return platform;
        }

        // ================================================================
        // PICKUP COLLECTIBLES — trigger colliders + visual spheres
        // ================================================================

        private static GameObject CreatePickupPlaceholder(float arenaRadius, float flatRatio, float depth,
            float x, float z, int index, bool isMana)
        {
            float distFromCenter = Mathf.Sqrt(x * x + z * z);
            float y = GetBowlHeight(distFromCenter, arenaRadius, flatRatio, depth);

            GameObject pickup = new GameObject(isMana ? $"ManaPickup_{index}" : $"StaminaPickup_{index}");
            pickup.transform.position = new Vector3(x, y + 0.5f, z); // float above ground

            // Trigger collider for collection
            SphereCollider trigger = pickup.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.5f;

            // Visual: small sphere
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(pickup.transform, false);
            visual.transform.localScale = Vector3.one * 0.4f;

            // Remove the primitive's default collider (we use the parent's trigger)
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            Renderer rend = visual.GetComponent<Renderer>();
            Color pickupColor = isMana
                ? new Color(0.3f, 0.5f, 1f, 0.8f) // blue glow for mana
                : new Color(0.2f, 1f, 0.4f, 0.8f); // green glow for stamina
            rend.sharedMaterial = CreateArenaMaterial(pickupColor, 0f, 0.9f); // glowing, smooth

            // Add the placeholder tag component
            pickup.AddComponent<PickupPlaceholder>().Initialize(
                isMana ? PickupType.SpinMedium : PickupType.StaminaTemporary);

            // Add a gentle bob animation marker
            pickup.AddComponent<PickupBobAnimation>();

            return pickup;
        }

        // ================================================================
        // HELPERS
        // ================================================================

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

        private static void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            for (int i = 0; i < obj.transform.childCount; i++)
                SetLayerRecursive(obj.transform.GetChild(i).gameObject, layer);
        }
    }
}
