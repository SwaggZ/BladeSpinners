using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using BladeSpinners.Audio;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Combat;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.Shrine;
using BladeSpinners.World;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BladeSpinners.Gameplay.UI
{
    public static class RuntimeRunBuilder
    {
        private const int DefaultLevelCount = 3;
        private const int DefaultArenasPerLevel = 3;

        public sealed class RunProgression
        {
            public int RunSeed { get; }
            public int TotalLevels { get; }
            public int ArenasPerLevel { get; }

            public int CurrentLevelIndex { get; private set; }
            public int CurrentArenaIndex { get; private set; }

            public int TotalArenaCount => TotalLevels * ArenasPerLevel;
            public int DepthIndex => CurrentLevelIndex * ArenasPerLevel + CurrentArenaIndex;
            public int CurrentLevelOneBased => CurrentLevelIndex + 1;
            public int CurrentArenaOneBased => CurrentArenaIndex + 1;
            public bool IsLastArena => DepthIndex >= TotalArenaCount - 1;

            public RunProgression(int runSeed, int totalLevels, int arenasPerLevel)
            {
                RunSeed = runSeed;
                TotalLevels = Mathf.Max(1, totalLevels);
                ArenasPerLevel = Mathf.Max(1, arenasPerLevel);
                CurrentLevelIndex = 0;
                CurrentArenaIndex = 0;
            }

            public bool TryAdvance()
            {
                if (IsLastArena)
                    return false;

                CurrentArenaIndex++;
                if (CurrentArenaIndex >= ArenasPerLevel)
                {
                    CurrentArenaIndex = 0;
                    CurrentLevelIndex = Mathf.Min(CurrentLevelIndex + 1, TotalLevels - 1);
                }

                return true;
            }
        }

        public struct RunContext
        {
            public PlayerManager Player;
            public MatchManager Match;
            public ThirdPersonCameraController CameraController;
            public RunProgression Progression;
            public BladerShrineRunState ShrineState;
            public int ArenaSeed;
            public int DepthIndex;
        }

        private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static RunContext BuildRandomTestRun(
            Dictionary<PartType, BeyPart> selectedLoadout,
            List<BeyPart> ownedParts,
            List<BeyPart> enemyPartPool,
            int seed,
            int enemyCount,
            RunProgression progression = null,
            List<BeyPart> carriedInventory = null,
            BladerShrineRunState carriedShrineState = null)
        {
            ClearSceneForRun();

            RunProgression activeProgression = progression
                ?? new RunProgression(seed, DefaultLevelCount, DefaultArenasPerLevel);
            int depthIndex = activeProgression.DepthIndex;
            int arenaSeed = ComputeArenaSeed(activeProgression.RunSeed, depthIndex);
            int depthScaledEnemyCount = Mathf.Clamp(enemyCount + depthIndex / 2, 2, GameConstants.ENEMY_MAX_PER_COMBAT_ROOM);

            EnsureGameManager();
            int spinPickupCount = Mathf.Max(depthScaledEnemyCount + 1, 3);
            int staminaPickupCount = Mathf.Max(1, depthScaledEnemyCount / 2);
            GameObject arena = ProceduralArenaGenerator.Generate(
                arenaSeed,
                RoomType.Combat,
                -1,
                -1,
                staminaPickupCount,
                spinPickupCount);
            arena.name = $"Arena_L{activeProgression.CurrentLevelOneBased}_A{activeProgression.CurrentArenaOneBased}";

            GameObject playerObj = CreatePlayerBey(selectedLoadout);
            PlayerManager playerManager = playerObj.GetComponent<PlayerManager>();

            BladerShrineRunState activeShrineState = carriedShrineState ?? new BladerShrineRunState();
            if (playerManager != null && playerManager.BeyConfiguration != null)
            {
                playerManager.BeyConfiguration.ShrineState = activeShrineState;
            }
            activeShrineState.RefreshOfferingsForArena(depthIndex, activeProgression.RunSeed);

            ThirdPersonCameraController camController = CreateCamera(playerObj);
            MatchManager match = CreateMatchManager();
            match.RegisterPlayer(playerManager);

            SeedRunInventory(playerManager, selectedLoadout, carriedInventory);

            List<Transform> enemyTransforms = new List<Transform>();
            List<BeyPart> enemyCatalog = BuildExpandedEnemyCatalog(selectedLoadout, ownedParts, enemyPartPool);
            for (int i = 0; i < depthScaledEnemyCount; i++)
            {
                GameObject enemy = CreateEnemyBey(
                    i,
                    depthScaledEnemyCount,
                    playerObj.transform,
                    enemyCatalog,
                    depthIndex,
                    Mathf.Max(1, activeProgression.TotalArenaCount),
                    activeProgression.RunSeed);
                EnemyBeyController enemyCtrl = enemy.GetComponent<EnemyBeyController>();
                match.RegisterEnemy(enemyCtrl);
                enemyTransforms.Add(enemy.transform);
            }

            ConfigureBeyCollisionPairs();

            // Hole-aware spawn: if arena has a center hole, move all beys to a ring
            System.Random shapeRng = new System.Random(arenaSeed);
            ArenaShapeDefinition[] allShapes = ArenaShapeLibrary.GetAllShapes();
            ArenaShapeDefinition arenaShape = allShapes[shapeRng.Next(allShapes.Length)];
            Debug.Log($"[RuntimeRunBuilder] Arena shape: {arenaShape.Name}, HoleRadiusRatio={arenaShape.HoleRadiusRatio}, seed={arenaSeed}");
            if (arenaShape.HoleRadiusRatio > 0.001f)
            {
                float holeR = arenaShape.Radius * arenaShape.HoleRadiusRatio;
                float safeR = arenaShape.Radius * 0.5f; // halfway out — well clear of the hole
                int totalBeys = 1 + depthScaledEnemyCount;
                playerObj.transform.position = new Vector3(0f, 3f, safeR);
                Debug.Log($"[RuntimeRunBuilder] Hole arena detected — holeR={holeR:F2}, player spawn at Z={safeR:F2}");
                for (int i = 0; i < enemyTransforms.Count; i++)
                {
                    float angle = (float)(i + 1) / totalBeys * Mathf.PI * 2f;
                    enemyTransforms[i].position = new Vector3(
                        Mathf.Cos(angle) * safeR, 3f, Mathf.Sin(angle) * safeR);
                }
                match.SetPlayerSpawnPosition(playerObj.transform.position);
            }

            if (camController != null)
            {
                camController.SetEnemyTransforms(enemyTransforms);
            }

            match.StartMatch();

            return new RunContext
            {
                Player = playerManager,
                Match = match,
                CameraController = camController,
                Progression = activeProgression,
                ShrineState = activeShrineState,
                ArenaSeed = arenaSeed,
                DepthIndex = depthIndex
            };
        }

        public static RunProgression CreateRunProgression(int runSeed, int levelCount = DefaultLevelCount, int arenasPerLevel = DefaultArenasPerLevel)
        {
            return new RunProgression(runSeed, levelCount, arenasPerLevel);
        }

        public static void ClearRunObjectsForMainMenu()
        {
            ClearSceneForRun();
        }

        private static void ClearSceneForRun()
        {
            ThirdPersonCameraController.SetBladeLockFocus(Vector3.zero, false);
            GameObject[] roots = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject go = roots[i];
                if (go == null || go.transform.parent != null)
                    continue;

                RuntimeGameUiController ui = go.GetComponent<RuntimeGameUiController>();
                if (ui != null)
                    continue;

                // Audio services are run-independent. Preserve them so crossfades and
                // now-playing subscriptions survive arena and menu transitions.
                if (go.GetComponent<SoundManager>() != null
                    || go.GetComponent<MusicNowPlayingBanner>() != null)
                {
                    continue;
                }

                // Game balance is run-level state. Reusing it avoids the deferred-destroy
                // singleton race when the next arena is built in the same frame.
                if (go.GetComponent<GameManager>() != null)
                    continue;

                if (go.name.StartsWith("__Preview", StringComparison.Ordinal))
                    continue;

                // Destroy is deferred until end-of-frame. Deactivate immediately so old
                // cameras, listeners, physics bodies, and target queries cannot overlap
                // with the newly constructed arena.
                go.SetActive(false);
                UnityEngine.Object.Destroy(go);
            }
        }

        private static GameManager EnsureGameManager()
        {
            if (GameManager.Instance != null)
                return GameManager.Instance;

            GameManager existing = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (existing != null)
                return existing;

            GameObject managerObject = new GameObject("GameManager");
            return managerObject.AddComponent<GameManager>();
        }

        private static GameObject CreatePlayerBey(Dictionary<PartType, BeyPart> selectedLoadout)
        {
            GameObject root = new GameObject("PlayerBey");
            root.transform.position = new Vector3(0f, 3f, 0f);

            GameObject tiltPivot = new GameObject("TiltPivot");
            tiltPivot.transform.SetParent(root.transform, false);

            GameObject spinChild = new GameObject("SpinChild");
            spinChild.transform.SetParent(tiltPivot.transform, false);

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.1f;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.radius = 0.2f;
            trigger.isTrigger = true;

            BeyConfiguration config = new BeyConfiguration();
            BeyMovementController movement = root.AddComponent<BeyMovementController>();
            BeyTiltController tilt = root.AddComponent<BeyTiltController>();
            BeyCollisionDetector collision = root.AddComponent<BeyCollisionDetector>();
            PlayerInputHandler input = root.AddComponent<PlayerInputHandler>();
            BeyVisualSpin visualSpin = root.AddComponent<BeyVisualSpin>();
            BeyAssembler assembler = root.AddComponent<BeyAssembler>();
            root.AddComponent<Effects.BeyBurstEffect>();
            root.AddComponent<Effects.BeyGroundTrailEffect>();
            PlayerManager manager = root.AddComponent<PlayerManager>();

            typeof(BeyMovementController).GetField("beyConfiguration", Flags)?.SetValue(movement, config);
            typeof(BeyMovementController).GetField("beyModelTransform", Flags)?.SetValue(movement, tiltPivot.transform);

            typeof(BeyTiltController).GetField("movementController", Flags)?.SetValue(tilt, movement);
            typeof(BeyTiltController).GetField("beyConfiguration", Flags)?.SetValue(tilt, config);
            typeof(BeyTiltController).GetField("tiltPivotTransform", Flags)?.SetValue(tilt, tiltPivot.transform);
            typeof(BeyTiltController).GetField("spinChildTransform", Flags)?.SetValue(tilt, spinChild.transform);

            typeof(BeyCollisionDetector).GetField("beyConfiguration", Flags)?.SetValue(collision, config);
            typeof(BeyCollisionDetector).GetField("movementController", Flags)?.SetValue(collision, movement);

            typeof(PlayerInputHandler).GetField("beyMovementController", Flags)?.SetValue(input, movement);
            typeof(PlayerInputHandler).GetField("beyConfiguration", Flags)?.SetValue(input, config);

            typeof(BeyVisualSpin).GetField("visualRoot", Flags)?.SetValue(visualSpin, spinChild.transform);
            typeof(BeyAssembler).GetField("beyModelTransform", Flags)?.SetValue(assembler, spinChild.transform);

            typeof(PlayerManager).GetField("beyConfiguration", Flags)?.SetValue(manager, config);
            typeof(PlayerManager).GetField("movementController", Flags)?.SetValue(manager, movement);
            typeof(PlayerManager).GetField("tiltController", Flags)?.SetValue(manager, tilt);
            typeof(PlayerManager).GetField("collisionDetector", Flags)?.SetValue(manager, collision);
            typeof(PlayerManager).GetField("inputHandler", Flags)?.SetValue(manager, input);

            assembler.SetConfiguration(config);
            ApplyLoadoutToAssembler(assembler, selectedLoadout);

            // RewireStatRings MUST be called after all reflection field-sets above,
            // because PlayerManager.Awake() (triggered by AddComponent) captured an
            // empty BeyConfiguration before our external config was injected.
            manager.RewireStatRings();
            SetLayerRecursive(root, GetRequiredLayer("Bey"));

            return root;
        }

        private static GameObject CreateEnemyBey(
            int index,
            int totalEnemies,
            Transform playerTarget,
            List<BeyPart> catalog,
            int depthIndex,
            int totalArenaCount,
            int runSeed)
        {
            float angle = (float)index / Mathf.Max(1, totalEnemies) * Mathf.PI * 2f;
            Vector3 spawn = new Vector3(Mathf.Cos(angle) * 10f, 3f, Mathf.Sin(angle) * 10f);

            GameObject root = new GameObject($"EnemyBey_{index}");
            root.transform.position = spawn;

            GameObject tiltPivot = new GameObject("TiltPivot");
            tiltPivot.transform.SetParent(root.transform, false);

            GameObject spinChild = new GameObject("SpinChild");
            spinChild.transform.SetParent(tiltPivot.transform, false);

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.1f;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.radius = 0.2f;
            trigger.isTrigger = true;

            BeyConfiguration config = new BeyConfiguration { IsEnemy = true };
            BeyMovementController movement = root.AddComponent<BeyMovementController>();
            BeyTiltController tilt = root.AddComponent<BeyTiltController>();
            BeyCollisionDetector collision = root.AddComponent<BeyCollisionDetector>();
            AIInputHandler aiInput = root.AddComponent<AIInputHandler>();
            BeyVisualSpin visualSpin = root.AddComponent<BeyVisualSpin>();
            BeyAssembler assembler = root.AddComponent<BeyAssembler>();
            root.AddComponent<Effects.BeyBurstEffect>();
            root.AddComponent<Effects.BeyGroundTrailEffect>();
            EnemyBeyController enemy = root.AddComponent<EnemyBeyController>();

            typeof(BeyMovementController).GetField("beyConfiguration", Flags)?.SetValue(movement, config);
            typeof(BeyMovementController).GetField("beyModelTransform", Flags)?.SetValue(movement, tiltPivot.transform);

            typeof(BeyTiltController).GetField("movementController", Flags)?.SetValue(tilt, movement);
            typeof(BeyTiltController).GetField("beyConfiguration", Flags)?.SetValue(tilt, config);
            typeof(BeyTiltController).GetField("tiltPivotTransform", Flags)?.SetValue(tilt, tiltPivot.transform);
            typeof(BeyTiltController).GetField("spinChildTransform", Flags)?.SetValue(tilt, spinChild.transform);

            typeof(BeyCollisionDetector).GetField("beyConfiguration", Flags)?.SetValue(collision, config);
            typeof(BeyCollisionDetector).GetField("movementController", Flags)?.SetValue(collision, movement);

            typeof(AIInputHandler).GetField("beyMovementController", Flags)?.SetValue(aiInput, movement);
            typeof(AIInputHandler).GetField("beyConfiguration", Flags)?.SetValue(aiInput, config);

            typeof(BeyVisualSpin).GetField("visualRoot", Flags)?.SetValue(visualSpin, spinChild.transform);
            typeof(BeyAssembler).GetField("beyModelTransform", Flags)?.SetValue(assembler, spinChild.transform);

            assembler.SetConfiguration(config);
            int enemySeed = ComputeArenaSeed(runSeed, 9000 + index * 97 + depthIndex * 211);
            ApplyLoadoutToAssembler(assembler, GetRandomLoadout(catalog, enemySeed, depthIndex, totalArenaCount));
            float depth01 = Mathf.Clamp01(totalArenaCount <= 1 ? 1f : (float)depthIndex / (totalArenaCount - 1));
            enemy.Initialize(config, playerTarget, depth01, index, totalEnemies);
            SetLayerRecursive(root, GetRequiredLayer("Bey"));

            return root;
        }

        private static int GetRequiredLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
                throw new InvalidOperationException($"Required Unity layer '{layerName}' is not configured.");
            return layer;
        }

        private static void SetLayerRecursive(GameObject root, int layer)
        {
            if (root == null)
                return;

            root.layer = layer;
            for (int i = 0; i < root.transform.childCount; i++)
                SetLayerRecursive(root.transform.GetChild(i).gameObject, layer);
        }

        private static void ConfigureBeyCollisionPairs()
        {
            BeyCollisionDetector[] detectors =
                UnityEngine.Object.FindObjectsByType<BeyCollisionDetector>(FindObjectsSortMode.None);

            for (int leftIndex = 0; leftIndex < detectors.Length; leftIndex++)
            {
                BeyCollisionDetector left = detectors[leftIndex];
                if (left == null)
                    continue;

                Collider[] leftColliders = left.GetComponentsInChildren<Collider>(true);
                for (int rightIndex = leftIndex + 1; rightIndex < detectors.Length; rightIndex++)
                {
                    BeyCollisionDetector right = detectors[rightIndex];
                    if (right == null)
                        continue;

                    Collider[] rightColliders = right.GetComponentsInChildren<Collider>(true);
                    for (int i = 0; i < leftColliders.Length; i++)
                    {
                        Collider leftCollider = leftColliders[i];
                        if (leftCollider == null)
                            continue;

                        for (int j = 0; j < rightColliders.Length; j++)
                        {
                            Collider rightCollider = rightColliders[j];
                            if (rightCollider == null)
                                continue;

                            // Leave only trigger-to-trigger interaction enabled. This
                            // preserves one spin-exchange contact while preventing the
                            // generated part meshes from physically shoving each other.
                            bool bothTriggers = leftCollider.isTrigger && rightCollider.isTrigger;
                            Physics.IgnoreCollision(leftCollider, rightCollider, !bothTriggers);
                        }
                    }
                }
            }
        }

        private static ThirdPersonCameraController CreateCamera(GameObject playerRoot)
        {
            GameObject rig = new GameObject("CameraRig");
            rig.transform.SetParent(playerRoot.transform, false);

            ThirdPersonCameraController controller = rig.AddComponent<ThirdPersonCameraController>();

            GameObject camObject = new GameObject("Main Camera");
            camObject.transform.SetParent(rig.transform, false);
            Camera camera = camObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camObject.AddComponent<AudioListener>();

            controller.SetBeyTransform(playerRoot.transform);

            PlayerManager manager = playerRoot.GetComponent<PlayerManager>();
            if (manager != null)
            {
                typeof(PlayerManager).GetField("cameraController", Flags)?.SetValue(manager, controller);
                typeof(PlayerManager).GetField("mainCamera", Flags)?.SetValue(manager, camera);
            }

            return controller;
        }

        private static MatchManager CreateMatchManager()
        {
            GameObject matchObject = new GameObject("MatchManager");
            return matchObject.AddComponent<MatchManager>();
        }

        private static void SeedRunInventory(
            PlayerManager player,
            Dictionary<PartType, BeyPart> selectedLoadout,
            List<BeyPart> carriedInventory)
        {
            if (player == null)
                return;

            if (selectedLoadout != null)
            {
                foreach (KeyValuePair<PartType, BeyPart> kv in selectedLoadout)
                {
                    if (kv.Value != null)
                        player.AddPartToInventory(kv.Value);
                }
            }

            if (carriedInventory == null)
                return;

            for (int i = 0; i < carriedInventory.Count; i++)
            {
                BeyPart part = carriedInventory[i];
                if (part != null)
                    player.AddPartToInventory(part);
            }
        }

        private static void ApplyLoadoutToAssembler(BeyAssembler assembler, Dictionary<PartType, BeyPart> loadout)
        {
            if (assembler == null || loadout == null)
                return;

            foreach (KeyValuePair<PartType, BeyPart> kv in loadout)
            {
                if (kv.Value != null)
                {
                    assembler.EquipPart(kv.Value);
                }
            }
        }

        private static Dictionary<PartType, BeyPart> GetRandomLoadout(List<BeyPart> catalog, int seed, int depthIndex, int totalArenaCount)
        {
            if (catalog == null)
                catalog = new List<BeyPart>();

            Dictionary<PartType, List<BeyPart>> byType = new Dictionary<PartType, List<BeyPart>>();
            foreach (PartType type in Enum.GetValues(typeof(PartType)))
            {
                byType[type] = new List<BeyPart>();
            }

            for (int i = 0; i < catalog.Count; i++)
            {
                BeyPart part = catalog[i];
                if (part == null) continue;
                if (byType.TryGetValue(part.PartType, out List<BeyPart> list))
                {
                    list.Add(part);
                }
            }

            System.Random rng = new System.Random(seed);
            Dictionary<PartType, BeyPart> loadout = new Dictionary<PartType, BeyPart>();
            float depth01 = Mathf.Clamp01(totalArenaCount <= 1 ? 1f : (float)depthIndex / (totalArenaCount - 1));
            foreach (PartType type in Enum.GetValues(typeof(PartType)))
            {
                List<BeyPart> list = byType[type];
                if (list.Count == 0)
                {
                    loadout[type] = RuntimePartFactory.CreateTemporaryPart(type, seed + (int)type * 1000 + rng.Next(1, 9999));
                    continue;
                }

                loadout[type] = ChoosePartForDepth(list, rng, depth01);
            }

            return loadout;
        }

        private static BeyPart ChoosePartForDepth(List<BeyPart> parts, System.Random rng, float depth01)
        {
            if (parts == null || parts.Count == 0)
                return null;

            int maxRarityIndex = Enum.GetValues(typeof(RarityTier)).Length - 1;
            int targetRarity = Mathf.Clamp(Mathf.RoundToInt(depth01 * maxRarityIndex), 0, maxRarityIndex);
            int floorRarity = Mathf.Clamp(Mathf.FloorToInt(depth01 * Mathf.Max(1, maxRarityIndex - 1)), 0, maxRarityIndex);

            float totalWeight = 0f;
            float[] weights = new float[parts.Count];
            for (int i = 0; i < parts.Count; i++)
            {
                BeyPart part = parts[i];
                int rarityIndex = part != null ? (int)part.Rarity : 0;
                int rarityDistance = Mathf.Abs(rarityIndex - targetRarity);

                float weight = 1f / (1f + rarityDistance * rarityDistance);
                if (rarityIndex < targetRarity)
                    weight *= 0.65f;
                if (rarityIndex > targetRarity)
                    weight *= 1.15f;
                if (rarityIndex < floorRarity)
                    weight *= 0.2f;

                weights[i] = Mathf.Max(0.0001f, weight);
                totalWeight += weights[i];
            }

            float roll = (float)rng.NextDouble() * totalWeight;
            for (int i = 0; i < parts.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f)
                    return parts[i];
            }

            return parts[parts.Count - 1];
        }

        private static List<BeyPart> BuildExpandedEnemyCatalog(
            Dictionary<PartType, BeyPart> selectedLoadout,
            List<BeyPart> ownedParts,
            List<BeyPart> enemyPartPool)
        {
            HashSet<BeyPart> unique = new HashSet<BeyPart>();
            AddParts(unique, enemyPartPool);
            AddParts(unique, ownedParts);

            if (selectedLoadout != null)
            {
                foreach (KeyValuePair<PartType, BeyPart> kv in selectedLoadout)
                {
                    if (kv.Value != null)
                        unique.Add(kv.Value);
                }
            }

            AddParts(unique, Resources.FindObjectsOfTypeAll<BeyPart>());

#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("t:BeyPart");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                BeyPart part = AssetDatabase.LoadAssetAtPath<BeyPart>(path);
                if (part != null)
                    unique.Add(part);
            }
#endif

            return new List<BeyPart>(unique);
        }

        private static int ComputeArenaSeed(int runSeed, int depthIndex)
        {
            unchecked
            {
                return runSeed * 73856093 ^ depthIndex * 19349663;
            }
        }

        private static void AddParts(IEnumerable<BeyPart> source, HashSet<BeyPart> target)
        {
            if (source == null || target == null)
                return;

            foreach (BeyPart part in source)
            {
                if (part != null)
                    target.Add(part);
            }
        }

        private static void AddParts(HashSet<BeyPart> target, IEnumerable<BeyPart> source)
        {
            AddParts(source, target);
        }
    }
}
