using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField] private Color focusedArrowColor = new Color(1f, 0.9f, 0.2f, 1f);

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
        private TextMesh focusedArrowTextMesh;

        private void Start()
        {
            // Auto-discover enemies at runtime so lock-on works
            // even if SetEnemyTransforms was never called manually.
            AutoDiscoverEnemies();

            EnsureFocusedArrowExists();
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
        }

        // ══════════════════════════════════════════════════════════════
        //  INPUT: middle-click toggle + scroll cycle
        // ══════════════════════════════════════════════════════════════

        private void ReadTargetSwitchInput()
        {
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

        public void SetBeyTransform(Transform t)
        {
            beyTransform = t;
            playerTransform = t;
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

            TextMesh textMesh = arrowObject.AddComponent<TextMesh>();
            textMesh.text = "▼";
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = Mathf.Max(0.01f, focusedArrowTextSize);
            textMesh.color = focusedArrowColor;

            MeshRenderer renderer = arrowObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            focusedArrowTransform = arrowObject.transform;
            focusedArrowTextMesh = textMesh;
            focusedArrowTransform.gameObject.SetActive(false);
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

            Vector3 worldPosition = lockedEnemyTransform.position + Vector3.up * focusedArrowHeight;
            focusedArrowTransform.position = worldPosition;

            if (focusedArrowTextMesh != null)
            {
                focusedArrowTextMesh.characterSize = Mathf.Max(0.01f, focusedArrowTextSize);
                focusedArrowTextMesh.color = focusedArrowColor;
            }

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

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
