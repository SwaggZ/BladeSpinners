using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BladeSpinners.Core;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Combat;
using BladeSpinners.World;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Editor utility to automatically create a test scene for Phase 1.
    /// Use the window: Blade Spinners → Test Arena Window for slider controls.
    /// </summary>
    public class TestSceneSetup
    {
        [MenuItem("GameObject/Blade Spinners/Create Test Arena Scene")]
        public static void CreateTestArena()
        {
            CreateTestArena(42, 4, 2, 2, 2, 2);
        }

        public static void CreateTestArena(int seed, int outerWalls, int innerWalls,
            int staminaPickups, int manaPickups, int enemyCount = 2)
        {
            // Clear current scene (optional - comment out for existing scenes)
            ClearScene();

            // Create GameManager (singleton — must exist before any gameplay components)
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();

            // Create ground plane
            CreateGround(seed, outerWalls, innerWalls, staminaPickups, manaPickups);

            // Create player Bey
            GameObject playerBey = CreatePlayerBey();

            // Create camera
            CreateCamera(playerBey);

            // Create match manager
            MatchManager matchManager = CreateMatchManager();

            // Register player with match manager
            PlayerManager playerManager = playerBey.GetComponent<PlayerManager>();
            matchManager.RegisterPlayer(playerManager);

            // Spawn enemy beys
            float arenaRadius = 20f; // approximate; enemies spawn inside the bowl
            List<Transform> enemyTransforms = new List<Transform>();
            for (int i = 0; i < enemyCount; i++)
            {
                GameObject enemy = CreateEnemyBey(i, enemyCount, arenaRadius, playerBey.transform);
                EnemyBeyController enemyCtrl = enemy.GetComponent<EnemyBeyController>();
                matchManager.RegisterEnemy(enemyCtrl);
                enemyTransforms.Add(enemy.transform);
            }

            // Wire enemy transforms into camera so middle-click / scroll can cycle targets
            ThirdPersonCameraController cameraController = playerBey.GetComponentInChildren<ThirdPersonCameraController>();
            if (cameraController != null)
                cameraController.SetEnemyTransforms(enemyTransforms);

            // --- Physics layers: prevent bey PARTS from physically colliding ---
            // Root stays on Default (trigger SphereCollider needs bey-vs-bey triggers).
            // Only part mesh children go on "Bey" layer so MeshColliders don't interlock.
            if (LayerMask.NameToLayer("Bey") == -1)
                CreateLayer("Bey");

            int beyLayer = LayerMask.NameToLayer("Bey");
            if (beyLayer >= 0)
            {
                SetBeyPartLayers(playerBey, beyLayer);
                foreach (var et in enemyTransforms)
                    SetBeyPartLayers(et.gameObject, beyLayer);

                Physics.IgnoreLayerCollision(beyLayer, beyLayer, true);
                Debug.Log($"[TestSceneSetup] Bey layer ({beyLayer}): part-vs-part physical collision DISABLED. Root triggers unaffected.");
            }

            // Start the match
            matchManager.StartMatch();

            Debug.Log($"✅ Test arena created with {enemyCount} enemies!\n" +
                     "1. Create test parts (Right-click Project → Create → BeyPart)\n" +
                     "2. Assign parts to PlayerBey in inspector\n" +
                     "3. Press Play to test!");
        }

        private static void ClearScene()
        {
            // Delete default objects
            Object[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "Main Camera" || obj.name == "Directional Light")
                    Object.DestroyImmediate(obj);
            }
        }

        private static void CreateGround(int seed, int outerWalls, int innerWalls,
            int staminaPickups, int manaPickups)
        {
            // Generate a procedural beyblade arena with explicit feature counts
            GameObject arena = ProceduralArenaGenerator.Generate(
                seed, RoomType.Combat, outerWalls, innerWalls, staminaPickups, manaPickups);
            arena.name = "Arena";

            // Set layer to Ground for all arena objects
            if (LayerMask.NameToLayer("Ground") == -1)
            {
                Debug.LogWarning("Ground layer not found! Creating it...");
                CreateLayer("Ground");
            }
            SetGroundLayer(arena);

            // Tag the bowl with Ground
            try
            {
                Transform bowl = arena.transform.Find("Bowl");
                if (bowl != null) bowl.gameObject.tag = "Ground";
                Transform rim = arena.transform.Find("Rim");
                if (rim != null) rim.gameObject.tag = "Ground";
            }
            catch
            {
                Debug.LogWarning("Ground tag not found! Creating it...");
                CreateTag("Ground");
            }

            Debug.Log($"[TestSceneSetup] Procedural arena created (seed={seed}, " +
                $"outerWalls={outerWalls}, innerWalls={innerWalls}, " +
                $"stamina={staminaPickups}, mana={manaPickups})");
        }

        private static void SetGroundLayer(GameObject obj)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0) groundLayer = 0;
            obj.layer = groundLayer;
            for (int i = 0; i < obj.transform.childCount; i++)
                SetGroundLayer(obj.transform.GetChild(i).gameObject);
        }

        private static void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            for (int i = 0; i < obj.transform.childCount; i++)
                SetLayerRecursive(obj.transform.GetChild(i).gameObject, layer);
        }

        /// <summary>
        /// Sets only TiltPivot, SpinChild, and Part_ children to the given layer.
        /// The root stays on Default so its trigger SphereCollider still detects other beys.
        /// </summary>
        private static void SetBeyPartLayers(GameObject beyRoot, int layer)
        {
            Transform tiltPivot = beyRoot.transform.Find("TiltPivot");
            if (tiltPivot != null)
                SetLayerRecursive(tiltPivot.gameObject, layer);
        }

        private static GameObject CreatePlayerBey()
        {
            // === ROOT: PlayerBey ===
            // This is the physics object. It moves but NEVER rotates.
            // Camera and BeyModel are siblings under this root.
            GameObject beyObj = new GameObject("PlayerBey");
            beyObj.transform.position = new Vector3(0, 3, 0); // Spawn above the arena bowl so it falls in

            // === CHILD 1: TiltPivot ===
            // Handles tilt/lean (X/Z rotation) without affecting the camera.
            // Camera rig is a sibling so it's completely isolated.
            GameObject tiltPivot = new GameObject("TiltPivot");
            tiltPivot.transform.parent = beyObj.transform;
            tiltPivot.transform.localPosition = Vector3.zero;

            // === GRANDCHILD: SpinChild (inside TiltPivot) ===
            // Handles Y-axis spin only. All bey part meshes live here.
            // Because it's inside TiltPivot, tilt axes stay world-aligned
            // regardless of spin angle.
            GameObject spinChild = new GameObject("SpinChild");
            spinChild.transform.parent = tiltPivot.transform;
            spinChild.transform.localPosition = Vector3.zero;

            // No hardcoded visual — BeyAssembler will generate meshes from equipped parts

            // Add Rigidbody to ROOT (physics body)
            Rigidbody rb = beyObj.AddComponent<Rigidbody>();
            rb.mass = 1;
            rb.linearDamping = 0.05f;  // Very low drag - Beyblades have minimal friction
            rb.angularDamping = 0.1f;
            rb.useGravity = true;
            // Freeze ALL rotation on root — tilt is handled visually on BeyModel
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth rendering between physics ticks

            // Part MeshColliders (on BeyModel children) act as compound colliders for the Rigidbody.
            // No SphereCollider needed for physics — part meshes handle that.

            // Add trigger collider to ROOT for Bey-vs-Bey detection
            SphereCollider triggerCollider = beyObj.AddComponent<SphereCollider>();
            triggerCollider.radius = 0.2f;
            triggerCollider.isTrigger = true;

            // Add gameplay components to ROOT
            BeyConfiguration beyConfig = new BeyConfiguration();

            BeyMovementController movement = beyObj.AddComponent<BeyMovementController>();
            BeyTiltController tilt = beyObj.AddComponent<BeyTiltController>();
            BeyCollisionDetector collision = beyObj.AddComponent<BeyCollisionDetector>();
            PlayerInputHandler input = beyObj.AddComponent<PlayerInputHandler>();
            BeyVisualSpin visualSpin = beyObj.AddComponent<BeyVisualSpin>();
            BeyAssembler assembler = beyObj.AddComponent<BeyAssembler>();
            beyObj.AddComponent<BladeSpinners.Gameplay.Effects.BeyBurstEffect>();
            PlayerManager manager = beyObj.AddComponent<PlayerManager>();

            // Wire up references in manager
            manager.GetType().GetField("beyConfiguration", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(manager, beyConfig);
            manager.GetType().GetField("movementController", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(manager, movement);
            manager.GetType().GetField("tiltController", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(manager, tilt);
            manager.GetType().GetField("collisionDetector", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(manager, collision);
            manager.GetType().GetField("inputHandler", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(manager, input);

            // Wire up input handler
            input.GetType().GetField("beyMovementController", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(input, movement);
            input.GetType().GetField("beyConfiguration", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(input, beyConfig);

            // Wire up movement controller — pass TiltPivot as the tilt target
            movement.GetType().GetField("beyConfiguration", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(movement, beyConfig);
            movement.GetType().GetField("beyModelTransform", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(movement, tiltPivot.transform);

            // Wire up tilt controller — separate transforms for tilt and spin
            tilt.GetType().GetField("movementController", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(tilt, movement);
            tilt.GetType().GetField("beyConfiguration", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(tilt, beyConfig);
            tilt.GetType().GetField("tiltPivotTransform", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(tilt, tiltPivot.transform);
            tilt.GetType().GetField("spinChildTransform", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(tilt, spinChild.transform);

            // Wire up collision detector
            collision.GetType().GetField("beyConfiguration", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(collision, beyConfig);
            collision.GetType().GetField("movementController", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(collision, movement);

            // Wire up visual spin — target the SpinChild for spinning
            visualSpin.GetType().GetField("visualRoot", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(visualSpin, spinChild.transform);

            // Wire up assembler — parts are parented under SpinChild so they spin with it
            assembler.GetType().GetField("beyModelTransform", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(assembler, spinChild.transform);

            // Load any existing part assets into assembler slots
            LoadPartsIntoAssembler(assembler);

            // Connect assembler to BeyConfiguration (it pushes equipped parts into config)
            assembler.SetConfiguration(beyConfig);

            return beyObj;
        }

        // ================================================================
        // MATCH MANAGER
        // ================================================================

        private static MatchManager CreateMatchManager()
        {
            GameObject mmObj = new GameObject("MatchManager");
            MatchManager mm = mmObj.AddComponent<MatchManager>();
            return mm;
        }

        // ================================================================
        // ENEMY BEY
        // ================================================================

        private static GameObject CreateEnemyBey(int index, int totalEnemies, float arenaRadius, Transform playerTarget)
        {
            // Distribute enemies evenly around the arena
            float angle = (float)index / totalEnemies * Mathf.PI * 2f;
            float spawnDist = arenaRadius * 0.5f;
            Vector3 spawnPos = new Vector3(
                Mathf.Cos(angle) * spawnDist,
                3f, // above arena so they fall in
                Mathf.Sin(angle) * spawnDist);

            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // ═══════════════════════════════════════════════════════════
            // Mirror the EXACT same setup as CreatePlayerBey, but with
            // AIInputHandler instead of PlayerInputHandler.
            // ═══════════════════════════════════════════════════════════

            // === ROOT ===
            GameObject enemyObj = new GameObject($"EnemyBey_{index}");
            enemyObj.transform.position = spawnPos;

            // === TiltPivot → SpinChild (same hierarchy as player) ===
            GameObject tiltPivot = new GameObject("TiltPivot");
            tiltPivot.transform.parent = enemyObj.transform;
            tiltPivot.transform.localPosition = Vector3.zero;

            GameObject spinChild = new GameObject("SpinChild");
            spinChild.transform.parent = tiltPivot.transform;
            spinChild.transform.localPosition = Vector3.zero;

            // Rigidbody (identical to player)
            Rigidbody rb = enemyObj.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.1f;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationY
                           | RigidbodyConstraints.FreezeRotationZ;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Trigger collider for bey-vs-bey detection (same as player)
            SphereCollider triggerCollider = enemyObj.AddComponent<SphereCollider>();
            triggerCollider.radius = 0.2f;
            triggerCollider.isTrigger = true;

            // BeyConfiguration
            BeyConfiguration enemyConfig = new BeyConfiguration();

            // --- Use existing parts for this enemy (no new set generation) ---
            System.Random enemyPartRng = new System.Random(70000 + index * 1111);

            // --- Components (same as player) ---
            BeyMovementController movement = enemyObj.AddComponent<BeyMovementController>();
            BeyTiltController tilt = enemyObj.AddComponent<BeyTiltController>();
            BeyCollisionDetector collision = enemyObj.AddComponent<BeyCollisionDetector>();
            AIInputHandler aiInput = enemyObj.AddComponent<AIInputHandler>();
            BeyVisualSpin visualSpin = enemyObj.AddComponent<BeyVisualSpin>();
            BeyAssembler assembler = enemyObj.AddComponent<BeyAssembler>();
            enemyObj.AddComponent<BladeSpinners.Gameplay.Effects.BeyBurstEffect>();
            EnemyBeyController enemyCtrl = enemyObj.AddComponent<EnemyBeyController>();

            // --- Wire AIInputHandler (mirrors PlayerInputHandler wiring) ---
            aiInput.GetType().GetField("beyMovementController", flags)
                ?.SetValue(aiInput, movement);
            aiInput.GetType().GetField("beyConfiguration", flags)
                ?.SetValue(aiInput, enemyConfig);

            // --- Wire movement controller (identical to player) ---
            movement.GetType().GetField("beyConfiguration", flags)
                ?.SetValue(movement, enemyConfig);
            movement.GetType().GetField("beyModelTransform", flags)
                ?.SetValue(movement, tiltPivot.transform);

            // --- Wire tilt controller (identical to player) ---
            tilt.GetType().GetField("movementController", flags)
                ?.SetValue(tilt, movement);
            tilt.GetType().GetField("beyConfiguration", flags)
                ?.SetValue(tilt, enemyConfig);
            tilt.GetType().GetField("tiltPivotTransform", flags)
                ?.SetValue(tilt, tiltPivot.transform);
            tilt.GetType().GetField("spinChildTransform", flags)
                ?.SetValue(tilt, spinChild.transform);

            // --- Wire collision detector (identical to player) ---
            collision.GetType().GetField("beyConfiguration", flags)
                ?.SetValue(collision, enemyConfig);
            collision.GetType().GetField("movementController", flags)
                ?.SetValue(collision, movement);

            // --- Wire visual spin (identical to player) ---
            visualSpin.GetType().GetField("visualRoot", flags)
                ?.SetValue(visualSpin, spinChild.transform);

            // --- Wire assembler (identical to player) ---
            assembler.GetType().GetField("beyModelTransform", flags)
                ?.SetValue(assembler, spinChild.transform);

            // Load existing random parts and push to config
            LoadRandomExistingPartsIntoAssembler(assembler, enemyPartRng);
            assembler.SetConfiguration(enemyConfig);

            // --- Initialize the enemy controller + AI ---
            enemyCtrl.Initialize(enemyConfig, playerTarget);

            Debug.Log($"[TestSceneSetup] Spawned enemy {index} using existing part pool at {spawnPos}");

            return enemyObj;
        }

        /// <summary>
        /// Loads parts matching a specific set name into an assembler.
        /// Searches Assets/Parts/ subfolders for parts whose names start with the given prefix.
        /// </summary>
        private static void LoadPartsIntoAssemblerByName(BeyAssembler assembler, string setName)
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            BeyPart tip = FindPartByName(PartType.Tip, setName);
            BeyPart track = FindPartByName(PartType.Track, setName);
            BeyPart fusionWheel = FindPartByName(PartType.FusionWheel, setName);
            BeyPart energyRing = FindPartByName(PartType.EnergyRing, setName);
            BeyPart faceBolt = FindPartByName(PartType.FaceBolt, setName);

            if (tip != null) assembler.GetType().GetField("tipPart", flags)?.SetValue(assembler, tip);
            if (track != null) assembler.GetType().GetField("trackPart", flags)?.SetValue(assembler, track);
            if (fusionWheel != null) assembler.GetType().GetField("fusionWheelPart", flags)?.SetValue(assembler, fusionWheel);
            if (energyRing != null) assembler.GetType().GetField("energyRingPart", flags)?.SetValue(assembler, energyRing);
            if (faceBolt != null) assembler.GetType().GetField("faceBoltPart", flags)?.SetValue(assembler, faceBolt);

            int count = (tip != null ? 1 : 0) + (track != null ? 1 : 0) + (fusionWheel != null ? 1 : 0)
                      + (energyRing != null ? 1 : 0) + (faceBolt != null ? 1 : 0);
            Debug.Log($"[TestSceneSetup] Loaded {count}/5 parts for {setName}");
        }

        /// <summary>
        /// Finds a BeyPart asset of the given type whose name starts with the given prefix.
        /// </summary>
        private static BeyPart FindPartByName(PartType type, string namePrefix)
        {
            string folder = type switch
            {
                PartType.Tip => "Assets/Parts/Tips",
                PartType.Track => "Assets/Parts/Tracks",
                PartType.FusionWheel => "Assets/Parts/Fusion Wheels",
                PartType.EnergyRing => "Assets/Parts/Energy Rings",
                PartType.FaceBolt => "Assets/Parts/Face Bolts",
                _ => "Assets/Parts"
            };

            string[] guids = AssetDatabase.FindAssets("t:BeyPart", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BeyPart part = AssetDatabase.LoadAssetAtPath<BeyPart>(path);
                if (part != null && part.PartType == type && part.PartName.StartsWith(namePrefix))
                    return part;
            }

            return null;
        }

        /// <summary>
        /// Loads one random existing part per slot type into an assembler.
        /// Uses project assets only and does not generate new part sets.
        /// </summary>
        private static void LoadRandomExistingPartsIntoAssembler(BeyAssembler assembler, System.Random rng)
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            BeyPart tip = FindRandomPartOfType(PartType.Tip, rng);
            BeyPart track = FindRandomPartOfType(PartType.Track, rng);
            BeyPart fusionWheel = FindRandomPartOfType(PartType.FusionWheel, rng);
            BeyPart energyRing = FindRandomPartOfType(PartType.EnergyRing, rng);
            BeyPart faceBolt = FindRandomPartOfType(PartType.FaceBolt, rng);

            if (tip != null) assembler.GetType().GetField("tipPart", flags)?.SetValue(assembler, tip);
            if (track != null) assembler.GetType().GetField("trackPart", flags)?.SetValue(assembler, track);
            if (fusionWheel != null) assembler.GetType().GetField("fusionWheelPart", flags)?.SetValue(assembler, fusionWheel);
            if (energyRing != null) assembler.GetType().GetField("energyRingPart", flags)?.SetValue(assembler, energyRing);
            if (faceBolt != null) assembler.GetType().GetField("faceBoltPart", flags)?.SetValue(assembler, faceBolt);

            int count = (tip != null ? 1 : 0) + (track != null ? 1 : 0) + (fusionWheel != null ? 1 : 0)
                      + (energyRing != null ? 1 : 0) + (faceBolt != null ? 1 : 0);
            Debug.Log($"[TestSceneSetup] Loaded {count}/5 random existing parts for enemy");
        }

        private static BeyPart FindRandomPartOfType(PartType type, System.Random rng)
        {
            string folder = type switch
            {
                PartType.Tip => "Assets/Parts/Tips",
                PartType.Track => "Assets/Parts/Tracks",
                PartType.FusionWheel => "Assets/Parts/Fusion Wheels",
                PartType.EnergyRing => "Assets/Parts/Energy Rings",
                PartType.FaceBolt => "Assets/Parts/Face Bolts",
                _ => "Assets/Parts"
            };

            string[] guids = AssetDatabase.FindAssets("t:BeyPart", new[] { folder });
            List<BeyPart> candidates = new List<BeyPart>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BeyPart part = AssetDatabase.LoadAssetAtPath<BeyPart>(path);
                if (part != null && part.PartType == type)
                {
                    candidates.Add(part);
                }
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[TestSceneSetup] No existing parts found for {type} in {folder}");
                return null;
            }

            int pickIndex = rng.Next(candidates.Count);
            return candidates[pickIndex];
        }

        /// <summary>
        /// Tries to load existing part assets and assign them to the assembler's inspector slots.
        /// Searches for any part set in Assets/Parts/. If none found, generates a default set.
        /// </summary>
        private static void LoadPartsIntoAssembler(BeyAssembler assembler)
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // Try to find any Tip, Track, FusionWheel, EnergyRing, FaceBolt
            BeyPart tip = FindFirstPartOfType(PartType.Tip);
            BeyPart track = FindFirstPartOfType(PartType.Track);
            BeyPart fusionWheel = FindFirstPartOfType(PartType.FusionWheel);
            BeyPart energyRing = FindFirstPartOfType(PartType.EnergyRing);
            BeyPart faceBolt = FindFirstPartOfType(PartType.FaceBolt);

            // If no parts exist at all, generate a default set
            if (tip == null && track == null && fusionWheel == null && energyRing == null && faceBolt == null)
            {
                Debug.Log("[TestSceneSetup] No part assets found — generating default set...");
                PartSetGenerator.GenerateSet("Default", 12345, RarityTier.Common);

                tip = FindFirstPartOfType(PartType.Tip);
                track = FindFirstPartOfType(PartType.Track);
                fusionWheel = FindFirstPartOfType(PartType.FusionWheel);
                energyRing = FindFirstPartOfType(PartType.EnergyRing);
                faceBolt = FindFirstPartOfType(PartType.FaceBolt);
            }

            // Assign to assembler's serialized slots
            if (tip != null) assembler.GetType().GetField("tipPart", flags)?.SetValue(assembler, tip);
            if (track != null) assembler.GetType().GetField("trackPart", flags)?.SetValue(assembler, track);
            if (fusionWheel != null) assembler.GetType().GetField("fusionWheelPart", flags)?.SetValue(assembler, fusionWheel);
            if (energyRing != null) assembler.GetType().GetField("energyRingPart", flags)?.SetValue(assembler, energyRing);
            if (faceBolt != null) assembler.GetType().GetField("faceBoltPart", flags)?.SetValue(assembler, faceBolt);

            int count = (tip != null ? 1 : 0) + (track != null ? 1 : 0) + (fusionWheel != null ? 1 : 0)
                      + (energyRing != null ? 1 : 0) + (faceBolt != null ? 1 : 0);
            Debug.Log($"[TestSceneSetup] Loaded {count}/5 parts into BeyAssembler slots");
        }

        /// <summary>
        /// Finds the first BeyPart asset of the given type in the project.
        /// </summary>
        private static BeyPart FindFirstPartOfType(PartType type)
        {
            string folder = type switch
            {
                PartType.Tip => "Assets/Parts/Tips",
                PartType.Track => "Assets/Parts/Tracks",
                PartType.FusionWheel => "Assets/Parts/Fusion Wheels",
                PartType.EnergyRing => "Assets/Parts/Energy Rings",
                PartType.FaceBolt => "Assets/Parts/Face Bolts",
                _ => "Assets/Parts"
            };

            string[] guids = AssetDatabase.FindAssets("t:BeyPart", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BeyPart part = AssetDatabase.LoadAssetAtPath<BeyPart>(path);
                if (part != null && part.PartType == type)
                    return part;
            }

            return null;
        }

        private static void CreateCamera(GameObject beyObj)
        {
            // === CHILD 2: CameraRig ===
            // Sibling of BeyModel under PlayerBey root.
            // Follows root position directly — completely isolated from BeyModel tilt/spin.
            GameObject cameraRig = new GameObject("CameraRig");
            cameraRig.transform.parent = beyObj.transform;
            cameraRig.transform.localPosition = Vector3.zero;
            cameraRig.transform.localRotation = Quaternion.identity;

            // Camera controller on the rig — it will position itself based on root
            ThirdPersonCameraController cameraController = cameraRig.AddComponent<ThirdPersonCameraController>();

            // Main Camera is a child of the rig
            GameObject cameraObj = new GameObject("Main Camera");
            cameraObj.transform.parent = cameraRig.transform;
            cameraObj.transform.localPosition = Vector3.zero;

            Camera cam = cameraObj.AddComponent<Camera>();
            cam.tag = "MainCamera";

            // Add audio listener
            cameraObj.AddComponent<AudioListener>();

            // Wire camera controller to follow the ROOT transform (not BeyModel)
            cameraController.SetBeyTransform(beyObj.transform);

            // Update PlayerManager camera reference
            PlayerManager manager = beyObj.GetComponent<PlayerManager>();
            if (manager != null)
            {
                manager.GetType().GetField("cameraController", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(manager, cameraController);
                manager.GetType().GetField("mainCamera", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(manager, cam);
            }
        }

        private static void CreateLayer(string layerName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (layer.stringValue == "")
                {
                    layer.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return;
                }
            }
        }

        private static void CreateTag(string tagName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tags = tagManager.FindProperty("tags");

            for (int i = 0; i < tags.arraySize; i++)
            {
                SerializedProperty tag = tags.GetArrayElementAtIndex(i);
                if (tag.stringValue == tagName)
                    return; // Tag already exists
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            SerializedProperty newTag = tags.GetArrayElementAtIndex(tags.arraySize - 1);
            newTag.stringValue = tagName;
            tagManager.ApplyModifiedProperties();
        }
    }
}
