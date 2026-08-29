using System;
using UnityEngine;
using UnityEngine.InputSystem;
using BladeSpinners.Abilities;
using BladeSpinners.Audio;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.UI;

namespace BladeSpinners.Gameplay.Combat
{
    /// <summary>
    /// Manages the dramatic Head-on Blade Lock Clash Duel minigame.
    /// When Beys collide head-to-head at high speeds, time dilates into slow-motion,
    /// camera zooms in, and players rapid-mash Space/Click to overpower their rival.
    /// </summary>
    public class BladeLockDuelManager : MonoBehaviour
    {
        public static BladeLockDuelManager Instance { get; private set; }

        public bool IsInBladeLock { get; private set; }
        public float ClashMeter { get; private set; } = 0.5f; // 0 (Enemy) to 1 (Player)
        public float DurationRemaining { get; private set; }
        public float TotalDuration { get; private set; } = 2.2f;
        public Vector3 ClashPosition { get; private set; }
        public Vector3 ClashNormal { get; private set; }
        public PlayerManager Player { get; private set; }
        public EnemyBeyController Enemy { get; private set; }
        public float LastLockTime { get; private set; } = -999f;
        public float LockCooldown { get; set; } = 10f;
        public int PlayerMashCount { get; private set; }
        public float EnemyPushRate { get; private set; } = 0.28f;

        private float nextSparkTime = 0f;
        private Rigidbody playerRb;
        private Rigidbody enemyRb;
        private Vector3 lockPlayerAnchor;
        private Vector3 lockEnemyAnchor;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public static BladeLockDuelManager EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            BladeLockDuelManager existing = UnityEngine.Object.FindFirstObjectByType<BladeLockDuelManager>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }

            GameObject go = new GameObject("BladeLockDuelManager");
            Instance = go.AddComponent<BladeLockDuelManager>();
            return Instance;
        }

        /// <summary>
        /// Validates whether a collision qualifies for a Blade Lock Duel.
        /// </summary>
        public bool TryTriggerBladeLock(
            BeyCollisionDetector beyA,
            BeyCollisionDetector beyB,
            Vector3 contactPoint,
            Vector3 contactNormal)
        {
            if (IsInBladeLock)
                return false;

            if (Time.unscaledTime - LastLockTime < LockCooldown)
                return false;

            if (beyA == null || beyB == null)
                return false;

            PlayerManager pMgr = beyA.GetComponent<PlayerManager>() ?? beyB.GetComponent<PlayerManager>();
            EnemyBeyController eCtrl = beyA.GetComponent<EnemyBeyController>() ?? beyB.GetComponent<EnemyBeyController>();

            if (pMgr == null || eCtrl == null)
                return false;

            if (pMgr.BeyConfiguration == null || eCtrl.BeyConfiguration == null)
                return false;

            if (pMgr.BeyConfiguration.CurrentSpin <= 5f || eCtrl.BeyConfiguration.CurrentSpin <= 5f)
                return false;

            MatchManager match = UnityEngine.Object.FindFirstObjectByType<MatchManager>();
            if (match == null || match.CurrentState != MatchManager.MatchState.InProgress)
                return false;

            Vector3 velPlayer = pMgr.GetComponent<BeyMovementController>()?.CurrentVelocity ?? pMgr.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;
            Vector3 velEnemy = eCtrl.GetComponent<BeyMovementController>()?.CurrentVelocity ?? eCtrl.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;

            float relSpeed = Vector3.Distance(velPlayer, velEnemy);
            if (relSpeed < 7.0f)
                return false;

            if (velPlayer.sqrMagnitude < 4.0f || velEnemy.sqrMagnitude < 4.0f)
                return false;

            float dot = Vector3.Dot(velPlayer.normalized, velEnemy.normalized);
            // Must be traveling towards each other (opposing directions)
            if (dot > -0.50f)
                return false;

            StartBladeLock(pMgr, eCtrl, contactPoint, contactNormal);
            return true;
        }

        private void StartBladeLock(
            PlayerManager player,
            EnemyBeyController enemy,
            Vector3 contactPoint,
            Vector3 contactNormal)
        {
            IsInBladeLock = true;
            ClashMeter = 0.5f;
            TotalDuration = 2.2f;
            DurationRemaining = TotalDuration;
            ClashPosition = contactPoint;
            ClashNormal = contactNormal.normalized;
            Player = player;
            Enemy = enemy;
            LastLockTime = Time.unscaledTime;
            PlayerMashCount = 0;
            nextSparkTime = 0f;

            playerRb = player.GetComponent<Rigidbody>();
            enemyRb = enemy.GetComponent<Rigidbody>();

            Vector3 sep = (player.transform.position - enemy.transform.position).normalized;
            if (sep.sqrMagnitude < 0.01f)
                sep = ClashNormal;

            lockPlayerAnchor = contactPoint + sep * 0.42f;
            lockEnemyAnchor = contactPoint - sep * 0.42f;

            // Compute AI push rate based on match progression / difficulty
            EnemyPushRate = UnityEngine.Random.Range(0.24f, 0.36f);

            // Enter slow-motion
            Time.timeScale = 0.20f;

            // Camera zoom focus
            ThirdPersonCameraController.SetBladeLockFocus(contactPoint, true);
            ThirdPersonCameraController.TriggerScreenShake(0.5f, 0.3f);

            // Visuals & Sound
            SoundManager.PlayBeyHit(contactPoint, 1.2f);
            EpicAbilityVFXHelper.SpawnShockwaveRing(contactPoint, new Color(1f, 0.85f, 0.2f, 0.9f), 0.2f, 3.5f, 0.4f);
            EpicAbilityVFXHelper.SpawnParticleBurst(contactPoint, 30, new Color(1f, 0.8f, 0.2f), new Color(1f, 0.3f, 0.1f), 1.2f);

            RuntimeGameUiController.SpawnGlobalComicPopup("BLADE LOCK CLASH!", new Color(1f, 0.85f, 0.1f), 1.8f);
        }

        private void Update()
        {
            if (!IsInBladeLock)
                return;

            DurationRemaining -= Time.unscaledDeltaTime;

            // Handle Player Input
            bool mashPressed = false;
            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                mashPressed = true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                mashPressed = true;
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                mashPressed = true;
#endif

            if (mashPressed)
            {
                PlayerMashCount++;
                ClashMeter = Mathf.Clamp01(ClashMeter + 0.078f);
                ThirdPersonCameraController.TriggerScreenShake(0.18f, 0.08f);
                SoundManager.PlayUiConfirm();

                // Spark burst towards enemy
                EpicAbilityVFXHelper.SpawnParticleBurst(
                    ClashPosition,
                    12,
                    new Color(0.2f, 0.9f, 1f),
                    new Color(1f, 0.85f, 0.2f),
                    0.8f);
            }

            // Enemy AI Push Resistance
            ClashMeter = Mathf.Clamp01(ClashMeter - EnemyPushRate * Time.unscaledDeltaTime);

            // Contact holding & Micro-jitter vibration
            Vector3 jitter = UnityEngine.Random.insideUnitSphere * 0.06f;
            if (Player != null)
            {
                Player.transform.position = lockPlayerAnchor + jitter;
                if (playerRb != null)
                {
                    playerRb.linearVelocity = Vector3.zero;
                    playerRb.angularVelocity = Vector3.up * 60f;
                }
            }
            if (Enemy != null)
            {
                Enemy.transform.position = lockEnemyAnchor - jitter;
                if (enemyRb != null)
                {
                    enemyRb.linearVelocity = Vector3.zero;
                    enemyRb.angularVelocity = Vector3.up * 60f;
                }
            }

            // Continuous roaring grinding friction sparks
            if (Time.unscaledTime >= nextSparkTime)
            {
                nextSparkTime = Time.unscaledTime + 0.06f;
                EpicAbilityVFXHelper.SpawnParticleBurst(
                    ClashPosition + UnityEngine.Random.insideUnitSphere * 0.1f,
                    8,
                    new Color(1f, 0.85f, 0.2f),
                    new Color(1f, 0.35f, 0.05f),
                    0.5f);
            }

            // Check completion conditions
            if (DurationRemaining <= 0f || ClashMeter >= 1.0f || ClashMeter <= 0.0f)
            {
                ResolveBladeLock();
            }
        }

        private void ResolveBladeLock()
        {
            IsInBladeLock = false;
            Time.timeScale = 1.0f;
            ThirdPersonCameraController.SetBladeLockFocus(Vector3.zero, false);
            ThirdPersonCameraController.TriggerScreenShake(0.85f, 0.45f);

            // Explosive shockwave burst
            EpicAbilityVFXHelper.SpawnShockwaveRing(ClashPosition, new Color(1f, 0.95f, 0.4f, 1f), 0.5f, 7.5f, 0.55f);
            EpicAbilityVFXHelper.SpawnParticleBurst(ClashPosition, 45, new Color(1f, 0.85f, 0.2f), new Color(1f, 0.2f, 0.1f), 2.2f);
            SoundManager.PlayBeyHit(ClashPosition, 1.6f);

            Vector3 pushDir = (lockEnemyAnchor - lockPlayerAnchor).normalized;
            if (pushDir.sqrMagnitude < 0.01f)
                pushDir = ClashNormal;

            if (ClashMeter >= 0.5f)
            {
                // ── Player Victory ──────────────────────────────────────────────
                if (Enemy != null && Enemy.BeyConfiguration != null)
                {
                    Enemy.BeyConfiguration.ModifySpin(-35f);
                }

                if (enemyRb != null)
                {
                    enemyRb.AddForce(pushDir * 32f + Vector3.up * 4.5f, ForceMode.Impulse);
                }

                // Award Blader Points
                if (Player != null && Player.BeyConfiguration != null && Player.BeyConfiguration.ShrineState != null)
                {
                    Player.BeyConfiguration.ShrineState.AddPoints(100);
                }

                RuntimeGameUiController.SpawnGlobalComicPopup("BLADE LOCK OVERPOWER! (+100 PTS)", new Color(1f, 0.88f, 0.15f), 2.2f);
            }
            else
            {
                // ── Enemy Victory ───────────────────────────────────────────────
                if (Player != null && Player.BeyConfiguration != null)
                {
                    Player.BeyConfiguration.ModifySpin(-22f);
                }

                if (playerRb != null)
                {
                    playerRb.AddForce(-pushDir * 24f + Vector3.up * 3.5f, ForceMode.Impulse);
                }

                RuntimeGameUiController.SpawnGlobalComicPopup("CLASH BROKEN!", new Color(1f, 0.3f, 0.3f), 1.6f);
            }
        }
    }
}
