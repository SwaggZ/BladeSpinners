using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Gameplay
{
    /// <summary>
    /// Third-person camera with Dragon Ball Xenoverse-style lock-on.
    /// 
    /// FREE MODE: GTA-style orbit around player via mouse/gamepad.
    /// LOCK-ON MODE (middle-click): Camera positions behind the player,
    ///   looking THROUGH the player TOWARD the locked enemy — player always
    ///   stays between camera and enemy. Scroll wheel cycles enemies.
    ///   Middle-click again releases lock.
    /// </summary>
    public class ThirdPersonCameraController : MonoBehaviour
    {
        [SerializeField]
        private Transform beyTransform; // The player bey (always followed)

        [Header("Speed Feedback")]
        [SerializeField] private float baseFieldOfView = 60f;
        [SerializeField] private float maxSpeedFieldOfView = 76f;
        [SerializeField] private float speedFovStart = 4f;
        [SerializeField] private float speedFovFull = 20f;
        [SerializeField] private float fovSmoothTime = 0.12f;
        [SerializeField] private bool showSpeedLines = true;
        [SerializeField] private float speedLinesStart = 6f;
        [SerializeField] private float speedLinesFull = 18f;
        [SerializeField] private int maxSpeedLines = 14;
        [SerializeField] private Color speedLineColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private float speedLineRelocateRate = 3.6f;
        [SerializeField] private bool fadeOccludingGeometry = true;
        [SerializeField] private float occluderFadeSpeed = 8f;
        [SerializeField, Range(0.05f, 1f)] private float occluderMinAlpha = 0.2f;
        [SerializeField] private LayerMask occluderMask = ~0;

        [Header("Free Camera")]
        [SerializeField] private float orbitDistance = 2f;
        [SerializeField] private float orbitHeight = 0.5f;
        [SerializeField] private float mouseSensitivity = 3f;
        [SerializeField] private float gamepadSensitivity = 120f;
        [SerializeField] private bool invertY = false;
        [SerializeField] private Vector2 pitchClamps = new Vector2(-10f, 60f);
        [SerializeField] private float followSmoothTime = 0.15f;

        [Header("Lock-On Camera")]
        [SerializeField] private float lockOnDistance = 3f;     // how far behind player
        [SerializeField] private float lockOnHeight = 1.5f;     // camera height above player
        [SerializeField] private float lockOnSmoothTime = 0.2f; // position smoothing
        [SerializeField] private float lockOnLookHeight = 0.3f; // look target height offset on player

        [Header("Focused Enemy Arrow")]
        [SerializeField] private bool showFocusedEnemyArrow = true;
        [SerializeField] private float focusedArrowHeight = 1.1f;
        [SerializeField] private float focusedArrowTextSize = 0.2f;
        [SerializeField] private float focusedArrowScaleMultiplier = 2f;
        [SerializeField] private Color focusedArrowColor = new Color(1f, 0.9f, 0.2f, 1f);
        [SerializeField] private Color focusedArrowHighSpinColor = new Color(0.16f, 1f, 0.24f, 1f);
        [SerializeField] private Color focusedArrowLowSpinColor = new Color(1f, 0.18f, 0.12f, 1f);
        [SerializeField, Range(0f, 1f)] private float focusedArrowShakeThreshold = 0.3f;
        [SerializeField] private float focusedArrowMaxShake = 0.035f;
        [SerializeField] private float focusedArrowShakeFrequency = 26f;

        private float currentYaw = 0f;
        private float currentPitch = 25f;
        private Vector3 smoothVelocity = Vector3.zero;
        private bool initialized = false;

        // --- Target switching ---
        private Transform playerTransform;
        private List<Transform> enemyTransforms = new List<Transform>();
        private int enemyTargetIndex = 0;
        private bool lockedToEnemy = false;
        private Transform lockedEnemyTransform;
        private Transform focusedArrowTransform;
        private Transform focusedArrowOutlineTransform;
        private Transform focusedArrowFillTransform;
        private MeshFilter focusedArrowOutlineMeshFilter;
        private MeshFilter focusedArrowFillMeshFilter;
        private MeshRenderer focusedArrowOutlineRenderer;
        private MeshRenderer focusedArrowFillRenderer;
        private Mesh focusedArrowOutlineMesh;
        private Mesh focusedArrowFillMesh;
        private Material focusedArrowOutlineMaterial;
        private Material focusedArrowFillMaterial;
        private Camera controlledCamera;
        private BeyMovementController playerMovementController;
        private MatchManager activeMatchManager;
        private float fovVelocity;
        private Texture2D speedWedgeTexture;
        private readonly Dictionary<Renderer, OccluderState> occluderStates = new Dictionary<Renderer, OccluderState>();
        private readonly HashSet<Renderer> occludersThisFrame = new HashSet<Renderer>();
        private readonly List<Renderer> occluderKeyBuffer = new List<Renderer>();

        private sealed class OccluderState
        {
            public Renderer Renderer;
            public Material[] OriginalSharedMaterials;
            public Material[] RuntimeMaterials;
            public float CurrentAlpha = 1f;
        }

        private void Start()
        {
            // Auto-discover enemies at runtime so lock-on works
            // even if SetEnemyTransforms was never called manually.
            AutoDiscoverEnemies();

            EnsureFocusedArrowExists();
            EnsureCameraReferences();
        }

        /// <summary>
        /// Finds all EnemyBeyController instances in scene and registers their
        /// root transforms for lock-on targeting.
        /// </summary>
        private void AutoDiscoverEnemies()
        {
            if (enemyTransforms.Count > 0) return; // already wired manually

            var enemies = FindObjectsByType<EnemyBeyController>(FindObjectsSortMode.None);
            if (enemies.Length > 0)
            {
                var list = new List<Transform>();
                foreach (var e in enemies)
                    list.Add(e.transform);
                enemyTransforms = list;
                enemyTargetIndex = 0;
                Debug.Log($"[Camera] Auto-discovered {list.Count} enemy target(s).");
            }
        }

        private void LateUpdate()
        {
            if (beyTransform == null)
                return;

            EnsureCameraReferences();

            if (IsPlayerLostStateActive())
            {
                lockedToEnemy = false;
                lockedEnemyTransform = null;
                UpdateFocusedArrow();
                FadeAllTrackedToOpaque();
                return;
            }

            ReadTargetSwitchInput();

            if (lockedToEnemy && lockedEnemyTransform != null)
            {
                UpdateLockOnCamera();
            }
            else
            {
                ReadCameraInput();
                UpdateFreeCamera();
            }

            UpdateFocusedArrow();
            UpdateSpeedFeedback();
            UpdateOccluderFading();
        }

        private void OnGUI()
        {
            if (!showSpeedLines)
                return;

            EnsureCameraReferences();
            if (controlledCamera == null || !controlledCamera.enabled || playerMovementController == null)
                return;

            float speed = playerMovementController.CurrentHorizontalSpeed;
            float intensity = Mathf.InverseLerp(speedLinesStart, speedLinesFull, speed);
            if (intensity <= 0.01f)
                return;

            EnsureSpeedWedgeTexture();

            int lineCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(0f, maxSpeedLines, intensity)), 0, maxSpeedLines);
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float minLength = Mathf.Lerp(Screen.height * 0.20f, Screen.height * 0.28f, intensity);
            float maxLength = Mathf.Lerp(Screen.height * 0.36f, Screen.height * 0.54f, intensity);
            float minBaseWidth = Mathf.Lerp(24f, 36f, intensity);
            float maxBaseWidth = Mathf.Lerp(58f, 92f, intensity);
            float centerDeadZone = Mathf.Lerp(Screen.height * 0.2f, Screen.height * 0.28f, intensity);
            float relocateRate = Mathf.Lerp(speedLineRelocateRate * 0.75f, speedLineRelocateRate * 1.65f, intensity);

            for (int i = 0; i < lineCount; i++)
            {
                float cycleT = Time.unscaledTime * relocateRate + i * 0.61f;
                int cycleIndex = Mathf.FloorToInt(cycleT);
                float lifeT = Mathf.Repeat(cycleT, 1f);

                float lineBaseWidth = Mathf.Lerp(minBaseWidth, maxBaseWidth, Hash01(cycleIndex, i, 4));
                float offscreenStart = lineBaseWidth * 1.35f + Mathf.Lerp(54f, 140f, intensity);
                float edgeSelector = Hash01(cycleIndex, i, 1);
                float edgeOffset = Hash01(cycleIndex, i, 2);
                Vector2 edgeAnchor = GetEdgePoint(edgeSelector, edgeOffset, offscreenStart);

                Vector2 toCenter = screenCenter - edgeAnchor;
                float distanceToCenter = toCenter.magnitude;
                if (distanceToCenter <= centerDeadZone + 1f)
                    continue;

                Vector2 direction = toCenter / distanceToCenter;
                float availableLength = Mathf.Max(0f, distanceToCenter - centerDeadZone);

                float lineLength = Mathf.Lerp(minLength, maxLength, Hash01(cycleIndex, i, 3));
                lineLength = Mathf.Min(lineLength, availableLength * 0.92f);
                if (lineLength <= 1f)
                    continue;

                Vector2 anchor = edgeAnchor;

                float lifeFade = 1f - Mathf.Abs(lifeT * 2f - 1f);
                lifeFade = Mathf.SmoothStep(0f, 1f, lifeFade);

                Color lineColor = Color.Lerp(new Color(0.72f, 0.72f, 0.72f, 0.65f), speedLineColor, Mathf.Repeat(i * 0.47f, 1f));
                lineColor.a *= intensity * lifeFade * Mathf.Lerp(0.7f, 1f, Hash01(cycleIndex, i, 5));

                DrawSpeedWedge(anchor, direction, lineLength, lineBaseWidth, lineColor);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  INPUT: middle-click toggle + scroll cycle
        // ══════════════════════════════════════════════════════════════

        private void ReadTargetSwitchInput()
        {
            if (IsPlayerLostStateActive())
                return;

            var mouse = Mouse.current;
            if (mouse == null)
                return;

            // Middle-click: toggle lock-on
            if (mouse.middleButton.wasPressedThisFrame)
            {
                Debug.Log($"[Camera] Middle-click detected. lockedToEnemy={lockedToEnemy}, enemyCount={enemyTransforms.Count}");
                if (lockedToEnemy)
                {
                    // Release lock — return to free camera
                    lockedToEnemy = false;
                    lockedEnemyTransform = null;
                    Debug.Log("[Camera] Lock-on RELEASED — returning to free camera");

                    // Sync yaw/pitch to current orientation so there's no snap
                    SyncYawPitchFromCurrentRotation();
                }
                else if (enemyTransforms.Count > 0)
                {
                    // Lock to closest enemy
                    lockedToEnemy = true;
                    enemyTargetIndex = FindClosestEnemyIndex();
                    lockedEnemyTransform = enemyTransforms[enemyTargetIndex];
                    Debug.Log($"[Camera] LOCKED ON to enemy index {enemyTargetIndex}: {lockedEnemyTransform.name}");
                }
            }

            // Scroll wheel: cycle enemies (only while locked or auto-lock)
            if (enemyTransforms.Count > 0)
            {
                float scrollY = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scrollY) > 0.1f)
                {
                    if (scrollY > 0f) enemyTargetIndex++;
                    else enemyTargetIndex--;

                    ClampEnemyIndex();
                    lockedToEnemy = true;
                    lockedEnemyTransform = enemyTransforms[enemyTargetIndex];
                }
            }

            // If locked enemy has burst (spin=0), destroyed, or deactivated — switch target immediately
            if (lockedToEnemy && IsEnemyDead(lockedEnemyTransform))
            {
                // Remove all burst/dead/destroyed enemies from the list
                enemyTransforms.RemoveAll(t => IsEnemyDead(t));

                if (enemyTransforms.Count > 0)
                {
                    // Switch to the closest alive enemy
                    enemyTargetIndex = FindClosestEnemyIndex();
                    lockedEnemyTransform = enemyTransforms[enemyTargetIndex];
                    Debug.Log($"[Camera] Locked enemy burst — switching to {lockedEnemyTransform.name} ({enemyTransforms.Count} remaining)");
                }
                else
                {
                    // No enemies left — return to free camera
                    lockedToEnemy = false;
                    lockedEnemyTransform = null;
                    SyncYawPitchFromCurrentRotation();
                    Debug.Log("[Camera] Locked enemy burst — no enemies left, returning to free camera");
                }
            }
        }

        private int FindClosestEnemyIndex()
        {
            int closest = 0;
            float closestDist = float.MaxValue;
            for (int i = 0; i < enemyTransforms.Count; i++)
            {
                if (IsEnemyDead(enemyTransforms[i]))
                    continue;
                float dist = Vector3.Distance(playerTransform.position, enemyTransforms[i].position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = i;
                }
            }
            return closest;
        }

        /// <summary>
        /// Returns true if the enemy transform is null, destroyed, inactive, or has burst (spin=0).
        /// </summary>
        private static bool IsEnemyDead(Transform t)
        {
            if (t == null || !t.gameObject.activeInHierarchy)
                return true;

            var enemy = t.GetComponent<EnemyBeyController>();
            if (enemy != null && enemy.BeyConfiguration != null && enemy.BeyConfiguration.IsBurst)
                return true;

            return false;
        }

        private void ClampEnemyIndex()
        {
            if (enemyTransforms.Count == 0) return;
            enemyTargetIndex = ((enemyTargetIndex % enemyTransforms.Count) + enemyTransforms.Count) % enemyTransforms.Count;
        }

        /// <summary>
        /// After releasing lock-on, sync yaw/pitch from where the camera is
        /// so the free camera continues from the same angle without snapping.
        /// </summary>
        private void SyncYawPitchFromCurrentRotation()
        {
            Vector3 dir = (transform.position - playerTransform.position).normalized;
            currentYaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
            currentPitch = Mathf.Asin(dir.y) * Mathf.Rad2Deg;
            currentPitch = Mathf.Clamp(currentPitch, pitchClamps.x, pitchClamps.y);
        }

        // ══════════════════════════════════════════════════════════════
        //  FREE CAMERA (GTA orbit)
        // ══════════════════════════════════════════════════════════════

        private void ReadCameraInput()
        {
            if (IsPlayerLostStateActive())
                return;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();
                currentYaw += mouseDelta.x * mouseSensitivity * 0.1f;
                float verticalInput = invertY ? mouseDelta.y : -mouseDelta.y;
                currentPitch += verticalInput * mouseSensitivity * 0.1f;
                currentPitch = Mathf.Clamp(currentPitch, pitchClamps.x, pitchClamps.y);
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 rightStick = gamepad.rightStick.ReadValue();
                if (rightStick.magnitude > 0.1f)
                {
                    currentYaw += rightStick.x * gamepadSensitivity * Time.deltaTime;
                    float verticalInput = invertY ? -rightStick.y : rightStick.y;
                    currentPitch += verticalInput * gamepadSensitivity * Time.deltaTime;
                    currentPitch = Mathf.Clamp(currentPitch, pitchClamps.x, pitchClamps.y);
                }
            }
        }

        private void UpdateFreeCamera()
        {
            float yawRad = currentYaw * Mathf.Deg2Rad;
            float pitchRad = currentPitch * Mathf.Deg2Rad;

            Vector3 orbitOffset = new Vector3(
                -Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                Mathf.Sin(pitchRad),
                -Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
            ) * orbitDistance;

            Vector3 lookTarget = beyTransform.position + Vector3.up * orbitHeight;

            if (!initialized)
            {
                transform.position = lookTarget + orbitOffset;
                initialized = true;
            }

            Vector3 desiredPosition = lookTarget + orbitOffset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref smoothVelocity, followSmoothTime);
            transform.LookAt(lookTarget);
        }

        // ══════════════════════════════════════════════════════════════
        //  LOCK-ON CAMERA (Xenoverse style)
        //  Camera sits behind the player, looking through player toward enemy.
        //  Player is always between camera and enemy.
        // ══════════════════════════════════════════════════════════════

        private void UpdateLockOnCamera()
        {
            Vector3 playerPos = playerTransform.position;
            Vector3 enemyPos = lockedEnemyTransform.position;

            // Direction from player toward enemy (on XZ plane for stable camera)
            Vector3 toEnemy = enemyPos - playerPos;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude < 0.01f)
                toEnemy = Vector3.forward; // fallback
            toEnemy.Normalize();

            // Camera goes BEHIND the player (opposite of enemy direction)
            Vector3 desiredPosition = playerPos
                - toEnemy * lockOnDistance
                + Vector3.up * lockOnHeight;

            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPosition, ref smoothVelocity, lockOnSmoothTime);

            // Look at a point slightly above the player so they stay in frame
            Vector3 lookTarget = playerPos + Vector3.up * lockOnLookHeight;
            transform.LookAt(lookTarget);
        }

        // ══════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════════

        public void ResetCamera()
        {
            currentYaw = 0f;
            currentPitch = 25f;
            lockedToEnemy = false;
            lockedEnemyTransform = null;
        }

        public void SetOrbitDistance(float distance)
        {
            orbitDistance = Mathf.Clamp(distance, 3f, 20f);
        }

        public float OrbitDistance => orbitDistance;
        public float CurrentYaw => currentYaw;
        public float CurrentPitch => currentPitch;

        public void SetOccluderOpacity(float alpha)
        {
            occluderMinAlpha = Mathf.Clamp(alpha, 0.1f, 0.6f);
        }

        public void SetBeyTransform(Transform t)
        {
            beyTransform = t;
            playerTransform = t;
            playerMovementController = t != null ? t.GetComponent<BeyMovementController>() : null;
        }

        public void SetEnemyTransforms(List<Transform> enemies)
        {
            enemyTransforms = enemies ?? new List<Transform>();
            enemyTargetIndex = 0;
        }

        public bool IsLockedToEnemy => lockedToEnemy;
        public int CurrentEnemyIndex => lockedToEnemy ? enemyTargetIndex : -1;
        public Transform CurrentLockedEnemy => lockedToEnemy ? lockedEnemyTransform : null;

        private void EnsureFocusedArrowExists()
        {
            if (!showFocusedEnemyArrow || focusedArrowTransform != null)
            {
                return;
            }

            GameObject arrowObject = new GameObject("FocusedEnemyArrow");
            arrowObject.hideFlags = HideFlags.DontSave;

            focusedArrowTransform = arrowObject.transform;
            focusedArrowOutlineTransform = CreateFocusedArrowLayer("Outline", focusedArrowTransform, out focusedArrowOutlineMeshFilter, out focusedArrowOutlineRenderer, 0f);
            focusedArrowFillTransform = CreateFocusedArrowLayer("Fill", focusedArrowTransform, out focusedArrowFillMeshFilter, out focusedArrowFillRenderer, -0.002f);

            focusedArrowOutlineMaterial = CreateFocusedArrowMaterial("FocusedEnemyArrowOutline", focusedArrowColor, 3100);
            focusedArrowFillMaterial = CreateFocusedArrowMaterial("FocusedEnemyArrowFill", focusedArrowColor, 3101);

            if (focusedArrowOutlineRenderer != null)
                focusedArrowOutlineRenderer.sharedMaterial = focusedArrowOutlineMaterial;
            if (focusedArrowFillRenderer != null)
                focusedArrowFillRenderer.sharedMaterial = focusedArrowFillMaterial;

            focusedArrowOutlineMesh = BuildTriangleMesh("FocusedEnemyTriangleOutline", 0.78f, 0.62f, 1f);
            focusedArrowFillMesh = BuildTriangleMesh("FocusedEnemyTriangleFill", 0.58f, 0.44f, 1f);

            if (focusedArrowOutlineMeshFilter != null)
                focusedArrowOutlineMeshFilter.sharedMesh = focusedArrowOutlineMesh;
            if (focusedArrowFillMeshFilter != null)
                focusedArrowFillMeshFilter.sharedMesh = focusedArrowFillMesh;

            focusedArrowTransform.localScale = Vector3.one * Mathf.Max(0.01f, focusedArrowTextSize * Mathf.Max(0.1f, focusedArrowScaleMultiplier));
            focusedArrowTransform.gameObject.SetActive(false);
        }

        private static Transform CreateFocusedArrowLayer(string name, Transform parent, out MeshFilter meshFilter, out MeshRenderer meshRenderer, float localZ)
        {
            GameObject layer = new GameObject(name);
            layer.hideFlags = HideFlags.DontSave;
            layer.transform.SetParent(parent, false);
            layer.transform.localPosition = new Vector3(0f, 0f, localZ);
            layer.transform.localRotation = Quaternion.identity;
            layer.transform.localScale = Vector3.one;

            meshFilter = layer.AddComponent<MeshFilter>();
            meshRenderer = layer.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            meshRenderer.sortingLayerName = "Default";
            meshRenderer.sortingOrder = 5000;
            return layer.transform;
        }

        private static Material CreateFocusedArrowMaterial(string name, Color color, int renderQueue)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
                return null;

            Material material = new Material(shader);
            material.name = name;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            // Render as a camera-facing overlay indicator.
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            material.renderQueue = renderQueue;
            return material;
        }

        private static Mesh BuildTriangleMesh(string meshName, float halfWidth, float halfHeight, float fillFraction)
        {
            Mesh mesh = new Mesh();
            mesh.name = meshName;
            UpdateTriangleMesh(mesh, halfWidth, halfHeight, fillFraction);
            return mesh;
        }

        private static void UpdateTriangleMesh(Mesh mesh, float halfWidth, float halfHeight, float fillFraction)
        {
            if (mesh == null)
                return;

            float fraction = Mathf.Clamp01(fillFraction);

            if (fraction <= 0.001f)
            {
                mesh.vertices = new[]
                {
                    new Vector3(-halfWidth, halfHeight, 0f),
                    new Vector3( halfWidth, halfHeight, 0f),
                    new Vector3( halfWidth, halfHeight, 0f),
                    new Vector3(-halfWidth, halfHeight, 0f)
                };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                return;
            }

            float topY = halfHeight;
            float bottomY = Mathf.Lerp(halfHeight, -halfHeight, fraction);
            float t = Mathf.InverseLerp(-halfHeight, halfHeight, bottomY);
            float bottomHalfWidth = Mathf.Lerp(0f, halfWidth, t);

            Vector3 topLeft = new Vector3(-halfWidth, topY, 0f);
            Vector3 topRight = new Vector3(halfWidth, topY, 0f);
            Vector3 bottomRight = new Vector3(bottomHalfWidth, bottomY, 0f);
            Vector3 bottomLeft = new Vector3(-bottomHalfWidth, bottomY, 0f);

            mesh.vertices = new[] { topLeft, topRight, bottomRight, bottomLeft };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
                return;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        private void EnsureCameraReferences()
        {
            if (controlledCamera == null)
            {
                controlledCamera = GetComponentInChildren<Camera>();
                if (controlledCamera != null)
                    controlledCamera.fieldOfView = baseFieldOfView;
            }

            if (playerMovementController == null && beyTransform != null)
                playerMovementController = beyTransform.GetComponent<BeyMovementController>();

            if (activeMatchManager == null)
                activeMatchManager = FindFirstObjectByType<MatchManager>();
        }

        private bool IsPlayerLostStateActive()
        {
            return activeMatchManager != null
                && activeMatchManager.CurrentState == MatchManager.MatchState.PlayerLost;
        }

        private void UpdateSpeedFeedback()
        {
            if (controlledCamera == null || playerMovementController == null)
                return;

            float speed = playerMovementController.CurrentHorizontalSpeed;
            float intensity = Mathf.InverseLerp(speedFovStart, speedFovFull, speed);
            float targetFov = Mathf.Lerp(baseFieldOfView, maxSpeedFieldOfView, intensity);
            controlledCamera.fieldOfView = Mathf.SmoothDamp(
                controlledCamera.fieldOfView,
                targetFov,
                ref fovVelocity,
                fovSmoothTime);
        }

        private void UpdateOccluderFading()
        {
            if (!fadeOccludingGeometry || beyTransform == null)
            {
                RestoreAllOccludersImmediate();
                return;
            }

            Vector3 cameraPos = controlledCamera != null ? controlledCamera.transform.position : transform.position;
            Vector3 targetPos = beyTransform.position;
            Vector3 toTarget = targetPos - cameraPos;
            float distance = toTarget.magnitude;
            if (distance <= 0.01f)
            {
                FadeAllTrackedToOpaque();
                return;
            }

            Vector3 rayDir = toTarget / distance;
            RaycastHit[] hits = Physics.RaycastAll(
                cameraPos,
                rayDir,
                Mathf.Max(0f, distance - 0.02f),
                occluderMask,
                QueryTriggerInteraction.Ignore);

            occludersThisFrame.Clear();
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null)
                    continue;

                CollectHitRenderers(hitCollider, occludersThisFrame);
            }

            occluderKeyBuffer.Clear();
            foreach (KeyValuePair<Renderer, OccluderState> kv in occluderStates)
                occluderKeyBuffer.Add(kv.Key);

            for (int i = 0; i < occluderKeyBuffer.Count; i++)
            {
                Renderer renderer = occluderKeyBuffer[i];
                if (renderer == null)
                {
                    occluderStates.Remove(renderer);
                    continue;
                }

                bool shouldFade = occludersThisFrame.Contains(renderer)
                    && (beyTransform == null || !renderer.transform.IsChildOf(beyTransform));
                float targetAlpha = shouldFade ? occluderMinAlpha : 1f;

                OccluderState state = EnsureOccluderState(renderer);
                if (state == null)
                    continue;

                state.CurrentAlpha = Mathf.MoveTowards(
                    state.CurrentAlpha,
                    targetAlpha,
                    Time.unscaledDeltaTime * occluderFadeSpeed);

                if (state.CurrentAlpha < 0.999f)
                {
                    ApplyStateAlpha(state, state.CurrentAlpha);
                }
                else if (!shouldFade)
                {
                    RestoreOccluderState(state);
                    occluderStates.Remove(renderer);
                }
            }

            foreach (Renderer renderer in occludersThisFrame)
            {
                if (renderer == null || occluderStates.ContainsKey(renderer))
                    continue;
                if (beyTransform != null && renderer.transform.IsChildOf(beyTransform))
                    continue;

                OccluderState state = EnsureOccluderState(renderer);
                if (state == null)
                    continue;

                state.CurrentAlpha = Mathf.MoveTowards(1f, occluderMinAlpha, Time.unscaledDeltaTime * occluderFadeSpeed);
                ApplyStateAlpha(state, state.CurrentAlpha);
            }

            occludersThisFrame.Clear();
        }

        private void FadeAllTrackedToOpaque()
        {
            if (occluderStates.Count == 0)
                return;

            occluderKeyBuffer.Clear();
            foreach (KeyValuePair<Renderer, OccluderState> kv in occluderStates)
                occluderKeyBuffer.Add(kv.Key);

            for (int i = 0; i < occluderKeyBuffer.Count; i++)
            {
                Renderer renderer = occluderKeyBuffer[i];
                if (renderer == null)
                {
                    occluderStates.Remove(renderer);
                    continue;
                }

                OccluderState state = occluderStates[renderer];
                state.CurrentAlpha = Mathf.MoveTowards(state.CurrentAlpha, 1f, Time.unscaledDeltaTime * occluderFadeSpeed);
                if (state.CurrentAlpha >= 0.999f)
                {
                    RestoreOccluderState(state);
                    occluderStates.Remove(renderer);
                }
                else
                {
                    ApplyStateAlpha(state, state.CurrentAlpha);
                }
            }
        }

        private void RestoreAllOccludersImmediate()
        {
            if (occluderStates.Count == 0)
                return;

            occluderKeyBuffer.Clear();
            foreach (KeyValuePair<Renderer, OccluderState> kv in occluderStates)
                occluderKeyBuffer.Add(kv.Key);

            for (int i = 0; i < occluderKeyBuffer.Count; i++)
            {
                Renderer renderer = occluderKeyBuffer[i];
                if (renderer == null)
                    continue;

                OccluderState state = occluderStates[renderer];
                RestoreOccluderState(state);
            }

            occluderStates.Clear();
            occludersThisFrame.Clear();
        }

        private static void CollectHitRenderers(Collider collider, HashSet<Renderer> target)
        {
            if (collider == null || target == null)
                return;

            Renderer own = collider.GetComponent<Renderer>();
            if (own != null)
                target.Add(own);

            Renderer[] children = collider.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null)
                    target.Add(children[i]);
            }

            Transform parent = collider.transform.parent;
            while (parent != null)
            {
                Renderer parentRenderer = parent.GetComponent<Renderer>();
                if (parentRenderer != null)
                    target.Add(parentRenderer);
                parent = parent.parent;
            }
        }

        private OccluderState EnsureOccluderState(Renderer renderer)
        {
            if (renderer == null)
                return null;

            if (occluderStates.TryGetValue(renderer, out OccluderState existing))
                return existing;

            Material[] originalShared = renderer.sharedMaterials;
            if (originalShared == null || originalShared.Length == 0)
                return null;

            Material[] runtimeMaterials = new Material[originalShared.Length];
            for (int i = 0; i < originalShared.Length; i++)
            {
                Material source = originalShared[i];
                if (source == null)
                    continue;

                Material runtime = new Material(source);
                ConfigureTransparentForFade(runtime);
                runtimeMaterials[i] = runtime;
            }

            renderer.materials = runtimeMaterials;

            OccluderState state = new OccluderState
            {
                Renderer = renderer,
                OriginalSharedMaterials = originalShared,
                RuntimeMaterials = runtimeMaterials,
                CurrentAlpha = 1f
            };

            occluderStates.Add(renderer, state);
            return state;
        }

        private static void ApplyStateAlpha(OccluderState state, float alpha)
        {
            if (state == null || state.RuntimeMaterials == null)
                return;

            for (int i = 0; i < state.RuntimeMaterials.Length; i++)
            {
                Material mat = state.RuntimeMaterials[i];
                if (mat == null)
                    continue;

                if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    mat.SetColor("_BaseColor", c);
                }
                else if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
            }
        }

        private static void ConfigureTransparentForFade(Material material)
        {
            if (material == null)
                return;

            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private static void RestoreOccluderState(OccluderState state)
        {
            if (state == null || state.Renderer == null)
                return;

            if (state.OriginalSharedMaterials != null && state.OriginalSharedMaterials.Length > 0)
                state.Renderer.sharedMaterials = state.OriginalSharedMaterials;

            if (state.RuntimeMaterials != null)
            {
                for (int i = 0; i < state.RuntimeMaterials.Length; i++)
                {
                    Material runtime = state.RuntimeMaterials[i];
                    if (runtime != null)
                        Destroy(runtime);
                }
            }
        }

        private void EnsureSpeedWedgeTexture()
        {
            if (speedWedgeTexture != null)
                return;

            const int texWidth = 320;
            const int texHeight = 96;
            speedWedgeTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int x = 0; x < texWidth; x++)
            {
                float x01 = x / (float)(texWidth - 1);
                float halfWidth = 0.5f * (1f - Mathf.Pow(x01, 0.92f));
                float lengthFade = Mathf.Lerp(0.82f, 1f, x01);

                for (int y = 0; y < texHeight; y++)
                {
                    float y01 = y / (float)(texHeight - 1);
                    float distanceFromCenter = Mathf.Abs(y01 - 0.5f) * 2f;
                    float widthMask = 1f - Mathf.Clamp01(distanceFromCenter / Mathf.Max(0.001f, halfWidth));
                    widthMask = Mathf.SmoothStep(0f, 1f, widthMask);
                    float alpha = widthMask * lengthFade;
                    speedWedgeTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            speedWedgeTexture.Apply();
        }

        private void DrawSpeedWedge(Vector2 anchor, Vector2 direction, float length, float baseWidth, Color color)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            float angleDeg = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Rect rect = new Rect(anchor.x, anchor.y - baseWidth * 0.5f, length, baseWidth);
            Vector2 pivot = new Vector2(rect.x, rect.y + rect.height * 0.5f);

            GUIUtility.RotateAroundPivot(angleDeg, pivot);
            GUI.color = color;
            GUI.DrawTexture(rect, speedWedgeTexture);

            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private static Vector2 GetEdgePoint(float selector, float offset, float outsideDistance)
        {
            if (selector < 0.25f)
                return new Vector2(Mathf.Lerp(-outsideDistance, Screen.width + outsideDistance, offset), -outsideDistance);

            if (selector < 0.5f)
                return new Vector2(Screen.width + outsideDistance, Mathf.Lerp(-outsideDistance, Screen.height + outsideDistance, offset));

            if (selector < 0.75f)
                return new Vector2(Mathf.Lerp(Screen.width + outsideDistance, -outsideDistance, offset), Screen.height + outsideDistance);

            return new Vector2(-outsideDistance, Mathf.Lerp(Screen.height + outsideDistance, -outsideDistance, offset));
        }

        private static float Hash01(int cycle, int index, int salt)
        {
            unchecked
            {
                uint h = (uint)(cycle * 73856093 ^ index * 19349663 ^ salt * 83492791);
                h ^= h >> 13;
                h *= 1274126177u;
                h ^= h >> 16;
                return (h & 0x00FFFFFF) / 16777215f;
            }
        }

        private void UpdateFocusedArrow()
        {
            if (!showFocusedEnemyArrow)
            {
                if (focusedArrowTransform != null)
                {
                    focusedArrowTransform.gameObject.SetActive(false);
                }
                return;
            }

            EnsureFocusedArrowExists();
            if (focusedArrowTransform == null)
            {
                return;
            }

            bool hasFocusedEnemy = lockedToEnemy && !IsEnemyDead(lockedEnemyTransform);
            if (!hasFocusedEnemy)
            {
                focusedArrowTransform.gameObject.SetActive(false);
                return;
            }

            if (!focusedArrowTransform.gameObject.activeSelf)
            {
                focusedArrowTransform.gameObject.SetActive(true);
            }

            BeyMovementController enemyMovement = lockedEnemyTransform.GetComponent<BeyMovementController>();
            float spinFraction = 1f;
            if (enemyMovement != null && enemyMovement.BeyConfiguration != null)
                spinFraction = Mathf.Clamp01(enemyMovement.BeyConfiguration.CurrentSpin / GameConstants.MAX_SPIN);

            Color spinColor = Color.Lerp(focusedArrowLowSpinColor, focusedArrowHighSpinColor, spinFraction);
            float shakeStrength = spinFraction < focusedArrowShakeThreshold
                ? Mathf.InverseLerp(focusedArrowShakeThreshold, 0f, spinFraction) * focusedArrowMaxShake
                : 0f;
            Vector3 shakeOffset = Vector3.zero;
            if (shakeStrength > 0.0001f)
            {
                float time = Time.unscaledTime * focusedArrowShakeFrequency;
                shakeOffset = new Vector3(
                    Mathf.Sin(time) * shakeStrength,
                    Mathf.Cos(time * 1.31f) * shakeStrength * 0.55f,
                    0f);
            }

            Vector3 worldPosition = lockedEnemyTransform.position + Vector3.up * focusedArrowHeight + shakeOffset;
            focusedArrowTransform.position = worldPosition;
            focusedArrowTransform.localScale = Vector3.one * Mathf.Max(0.01f, focusedArrowTextSize * Mathf.Max(0.1f, focusedArrowScaleMultiplier));

            Color fillColor = new Color(spinColor.r * 0.55f, spinColor.g * 0.55f, spinColor.b * 0.55f, 0.95f);
            SetMaterialColor(focusedArrowOutlineMaterial, spinColor);
            SetMaterialColor(focusedArrowFillMaterial, fillColor);

            // (24/3/2026) Visual mapping fix: drain direction should decrease with spin.
            UpdateTriangleMesh(focusedArrowFillMesh, 0.58f, 0.44f, 1f - spinFraction);

            Vector3 toCamera = transform.position - focusedArrowTransform.position;
            if (toCamera.sqrMagnitude > 0.0001f)
            {
                focusedArrowTransform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
            }
        }

        private void OnDestroy()
        {
            if (focusedArrowTransform != null)
            {
                Destroy(focusedArrowTransform.gameObject);
            }

            if (focusedArrowOutlineMaterial != null)
            {
                Destroy(focusedArrowOutlineMaterial);
            }

            if (focusedArrowFillMaterial != null)
            {
                Destroy(focusedArrowFillMaterial);
            }

            if (focusedArrowOutlineMesh != null)
            {
                Destroy(focusedArrowOutlineMesh);
            }

            if (focusedArrowFillMesh != null)
            {
                Destroy(focusedArrowFillMesh);
            }

            if (speedWedgeTexture != null)
            {
                Destroy(speedWedgeTexture);
            }

            RestoreAllOccludersImmediate();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
