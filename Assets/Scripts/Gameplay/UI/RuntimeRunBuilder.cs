using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Combat;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.World;

namespace BladeSpinners.Gameplay.UI
{
    public static class RuntimeRunBuilder
    {
        public struct RunContext
        {
            public PlayerManager Player;
            public MatchManager Match;
            public ThirdPersonCameraController CameraController;
        }

        private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static RunContext BuildRandomTestRun(
            Dictionary<PartType, BeyPart> selectedLoadout,
            List<BeyPart> ownedParts,
            List<BeyPart> enemyPartPool,
            int seed,
            int enemyCount)
        {
            ClearSceneForRun();

            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();

            GameObject arena = ProceduralArenaGenerator.Generate(seed, RoomType.Combat);
            arena.name = "Arena";

            GameObject playerObj = CreatePlayerBey(selectedLoadout);
            PlayerManager playerManager = playerObj.GetComponent<PlayerManager>();

            ThirdPersonCameraController camController = CreateCamera(playerObj);
            MatchManager match = CreateMatchManager();
            match.RegisterPlayer(playerManager);

            SeedRunInventory(playerManager, selectedLoadout);

            List<Transform> enemyTransforms = new List<Transform>();
            List<BeyPart> enemyCatalog = (enemyPartPool != null && enemyPartPool.Count > 0) ? enemyPartPool : ownedParts;
            for (int i = 0; i < enemyCount; i++)
            {
                GameObject enemy = CreateEnemyBey(i, enemyCount, playerObj.transform, enemyCatalog);
                EnemyBeyController enemyCtrl = enemy.GetComponent<EnemyBeyController>();
                match.RegisterEnemy(enemyCtrl);
                enemyTransforms.Add(enemy.transform);
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
                CameraController = camController
            };
        }

        private static void ClearSceneForRun()
        {
            GameObject[] roots = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject go = roots[i];
                if (go == null || go.transform.parent != null)
                    continue;

                RuntimeGameUiController ui = go.GetComponent<RuntimeGameUiController>();
                if (ui != null)
                    continue;

                if (go.name.StartsWith("__Preview", StringComparison.Ordinal))
                    continue;

                UnityEngine.Object.Destroy(go);
            }
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

            return root;
        }

        private static GameObject CreateEnemyBey(int index, int totalEnemies, Transform playerTarget, List<BeyPart> catalog)
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
            ApplyLoadoutToAssembler(assembler, GetRandomLoadout(catalog, 9000 + index * 97));
            enemy.Initialize(config, playerTarget);

            return root;
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

        private static void SeedRunInventory(PlayerManager player, Dictionary<PartType, BeyPart> selectedLoadout)
        {
            if (player == null || selectedLoadout == null)
                return;

            foreach (KeyValuePair<PartType, BeyPart> kv in selectedLoadout)
            {
                if (kv.Value != null)
                {
                    player.AddPartToInventory(kv.Value);
                }
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

        private static Dictionary<PartType, BeyPart> GetRandomLoadout(List<BeyPart> catalog, int seed)
        {
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
            foreach (PartType type in Enum.GetValues(typeof(PartType)))
            {
                List<BeyPart> list = byType[type];
                if (list.Count == 0)
                {
                    loadout[type] = RuntimePartFactory.CreateTemporaryPart(type, seed + (int)type * 1000 + rng.Next(1, 9999));
                    continue;
                }

                loadout[type] = list[rng.Next(0, list.Count)];
            }

            return loadout;
        }
    }
}
