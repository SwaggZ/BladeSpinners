using System;
using UnityEngine;
using UnityEngine.InputSystem;
using BladeSpinners.Abilities;
using BladeSpinners.Audio;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.UI;

namespace BladeSpinners.Gameplay.Combat
{
    public enum ClashMinigameType
    {
        RapidMash = 0,        // Rapid LMB clicking to overpower enemy push
        PrecisionTiming = 1,  // Sweeping needle into target critical zone
        RhythmBeat = 2,       // 3 sequential rhythm pulses closing into center
        TensionBalance = 3,   // Hold/Release LMB to balance inside moving sweet-spot
        OrbitalCrosshair = 4, // Orbiting spark, click when aligned with crosshair
        ReflexTrigger = 5     // Sudden strike prompt: fastest reaction wins
    }

    /// <summary>
    /// Manages the dramatic Head-on Blade Lock Clash Duel minigame.
    /// When Beys collide head-to-head at high speeds, time dilates into slow-motion,
    /// camera zooms in, and 1 of 6 distinct LMB-only minigames is chosen at random.
    /// </summary>
    public class BladeLockDuelManager : MonoBehaviour
    {
        public static BladeLockDuelManager Instance { get; private set; }

        public bool IsInBladeLock { get; private set; }
        public float ClashMeter { get; private set; } = 0.5f; // 0 (Enemy) to 1 (Player)
        public float DurationRemaining { get; private set; }
        public float TotalDuration { get; private set; } = 2.4f;
        public Vector3 ClashPosition { get; private set; }
        public Vector3 ClashNormal { get; private set; }
        public PlayerManager Player { get; private set; }
        public EnemyBeyController Enemy { get; private set; }
        public float LastLockTime { get; private set; } = -999f;
        public float LockCooldown { get; set; } = 10f;
        public int PlayerMashCount { get; private set; }
        public float EnemyPushRate { get; private set; } = 0.28f;

        // ── 6 Clash Minigames State ──────────────────────────────────
        public ClashMinigameType CurrentMinigame { get; private set; } = ClashMinigameType.RapidMash;

        // Precision Timing
        public float NeedlePos { get; private set; } = 0f; // 0..1
        public float SweetSpotMin { get; private set; } = 0.38f;
        public float SweetSpotMax { get; private set; } = 0.62f;
        private float needleSpeed = 2.8f;
        private float needleDir = 1f;

        // Rhythm Beat
        public int CurrentBeatIndex { get; private set; } = 0;
        public int TotalBeats { get; private set; } = 3;
        public float BeatProgress { get; private set; } = 0f; // 0 (start) to 1 (hit window)
        private float beatDuration = 0.70f;
        private float currentBeatTimer = 0f;

        // Tension Balance
        public float BalanceBobberPos { get; private set; } = 0.5f;
        public float BalanceTargetPos { get; private set; } = 0.5f;
        private float balanceBobberVel = 0f;

        // Orbital Crosshair
        public float OrbitAngle { get; private set; } = 0f; // 0..360
        public float TargetLockAngle { get; private set; } = 90f;
        private float orbitSpeed = 420f;

        // Reflex Trigger
        public float ReflexStandbyTimer { get; private set; } = 0f;
        public bool ReflexSignalActive { get; private set; } = false;
        public float PlayerReactionTime { get; private set; } = -1f;
        public bool FalseStart { get; private set; } = false;
        private float reflexSignalStartTime = 0f;

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

            PlayerManager pMgr = beyA.GetComponentInParent<PlayerManager>() 
                ?? beyB.GetComponentInParent<PlayerManager>()
                ?? UnityEngine.Object.FindFirstObjectByType<PlayerManager>();

            EnemyBeyController eCtrl = beyA.GetComponentInParent<EnemyBeyController>() 
                ?? beyB.GetComponentInParent<EnemyBeyController>();

            if (pMgr == null || eCtrl == null)
                return false;

            if (pMgr.BeyConfiguration == null || eCtrl.BeyConfiguration == null)
                return false;

            if (pMgr.BeyConfiguration.IsBurst || eCtrl.BeyConfiguration.IsBurst)
                return false;

            if (pMgr.BeyConfiguration.CurrentSpin <= 5f || eCtrl.BeyConfiguration.CurrentSpin <= 5f)
                return false;

            MatchManager match = UnityEngine.Object.FindFirstObjectByType<MatchManager>();
            if (match == null || match.CurrentState != MatchManager.MatchState.InProgress)
                return false;

            Vector3 velPlayer = pMgr.GetComponent<BeyMovementController>()?.CurrentVelocity ?? pMgr.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;
            Vector3 velEnemy = eCtrl.GetComponent<BeyMovementController>()?.CurrentVelocity ?? eCtrl.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;

            Vector3 dir = (eCtrl.transform.position - pMgr.transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f)
                dir = contactNormal;
            dir.Normalize();

            float closingSpeed = Vector3.Dot(velPlayer - velEnemy, dir);
            float relSpeed = Vector3.Distance(velPlayer, velEnemy);

            // Trigger when Beys collide with good impact force
            if (closingSpeed < 3.0f && relSpeed < 4.2f)
                return false;

            // ── Instant Burst Execution Smash on Low-Spin Enemies ──────────────
            float playerSpinRatio = pMgr.BeyConfiguration.CurrentSpin / GameConstants.MAX_SPIN;
            float enemySpinRatio = eCtrl.BeyConfiguration.CurrentSpin / GameConstants.MAX_SPIN;

            if (enemySpinRatio <= 0.18f || eCtrl.BeyConfiguration.CurrentSpin <= 18f)
            {
                ExecuteInstantExecutionSmash(pMgr, eCtrl, contactPoint, contactNormal, true);
                return false;
            }
            if (playerSpinRatio <= 0.15f || pMgr.BeyConfiguration.CurrentSpin <= 15f)
            {
                ExecuteInstantExecutionSmash(pMgr, eCtrl, contactPoint, contactNormal, false);
                return false;
            }

            StartBladeLock(pMgr, eCtrl, contactPoint, contactNormal);
            return true;
        }

        private void ExecuteInstantExecutionSmash(PlayerManager pMgr, EnemyBeyController eCtrl, Vector3 contactPoint, Vector3 contactNormal, bool playerWins)
        {
            LastLockTime = Time.unscaledTime;
            SoundManager.PlayBeyHit(contactPoint, 1.8f);
            ThirdPersonCameraController.TriggerScreenShake(1.2f, 0.45f);
            EpicAbilityVFXHelper.SpawnShockwaveRing(contactPoint, new Color(1f, 0.25f, 0.1f, 0.95f), 0.3f, 8.5f, 0.5f);
            EpicAbilityVFXHelper.SpawnParticleBurst(contactPoint, 20, new Color(1f, 0.9f, 0.2f), new Color(1f, 0.15f, 0.05f), 2.2f);

            if (playerWins)
            {
                eCtrl.BeyConfiguration.ModifySpin(-999f);
                Rigidbody erb = eCtrl.GetComponent<Rigidbody>();
                if (erb != null) erb.AddForce(contactNormal * 38f + Vector3.up * 6f, ForceMode.Impulse);
                if (pMgr.BeyConfiguration?.ShrineState != null)
                    pMgr.BeyConfiguration.ShrineState.AddPoints(60);
                RuntimeGameUiController.SpawnGlobalComicPopup("BURST EXECUTION SMASH! (+60 PTS)", new Color(1f, 0.35f, 0.1f), 2.5f);
            }
            else
            {
                pMgr.BeyConfiguration.ModifySpin(-999f);
                Rigidbody prb = pMgr.GetComponent<Rigidbody>();
                if (prb != null) prb.AddForce(-contactNormal * 32f + Vector3.up * 5f, ForceMode.Impulse);
                RuntimeGameUiController.SpawnGlobalComicPopup("BURST EXECUTED!", new Color(1f, 0.15f, 0.15f), 2.5f);
            }
        }

        private void StartBladeLock(
            PlayerManager player,
            EnemyBeyController enemy,
            Vector3 contactPoint,
            Vector3 contactNormal)
        {
            IsInBladeLock = true;
            ClashMeter = 0.5f;
            TotalDuration = 2.4f;
            DurationRemaining = TotalDuration;
            ClashPosition = contactPoint;
            ClashNormal = contactNormal.normalized;
            Player = player;
            Enemy = enemy;
            LastLockTime = Time.unscaledTime;
            PlayerMashCount = 0;
            nextSparkTime = 0f;

            // Pick 1 of 6 minigames at random!
            CurrentMinigame = (ClashMinigameType)UnityEngine.Random.Range(0, 6);

            // Initialize specific minigame parameters
            switch (CurrentMinigame)
            {
                case ClashMinigameType.PrecisionTiming:
                    NeedlePos = 0f;
                    needleDir = 1f;
                    needleSpeed = UnityEngine.Random.Range(2.4f, 3.2f);
                    SweetSpotMin = UnityEngine.Random.Range(0.35f, 0.45f);
                    SweetSpotMax = SweetSpotMin + UnityEngine.Random.Range(0.18f, 0.25f);
                    break;

                case ClashMinigameType.RhythmBeat:
                    CurrentBeatIndex = 0;
                    TotalBeats = 3;
                    currentBeatTimer = 0f;
                    beatDuration = 0.72f;
                    break;

                case ClashMinigameType.TensionBalance:
                    BalanceBobberPos = 0.5f;
                    BalanceTargetPos = 0.5f;
                    balanceBobberVel = 0f;
                    break;

                case ClashMinigameType.OrbitalCrosshair:
                    OrbitAngle = 0f;
                    orbitSpeed = UnityEngine.Random.Range(380f, 480f);
                    TargetLockAngle = UnityEngine.Random.Range(45f, 315f);
                    break;

                case ClashMinigameType.ReflexTrigger:
                    ReflexStandbyTimer = UnityEngine.Random.Range(0.65f, 1.25f);
                    ReflexSignalActive = false;
                    PlayerReactionTime = -1f;
                    FalseStart = false;
                    reflexSignalStartTime = 0f;
                    break;
            }

            playerRb = player.GetComponent<Rigidbody>();
            enemyRb = enemy.GetComponent<Rigidbody>();

            Vector3 sep = (player.transform.position - enemy.transform.position).normalized;
            if (sep.sqrMagnitude < 0.01f)
                sep = ClashNormal;

            lockPlayerAnchor = contactPoint + sep * 0.42f;
            lockEnemyAnchor = contactPoint - sep * 0.42f;

            EnemyPushRate = UnityEngine.Random.Range(0.24f, 0.35f);

            // Enter slow-motion
            Time.timeScale = 0.20f;

            // Camera zoom focus
            ThirdPersonCameraController.SetBladeLockFocus(contactPoint, true);
            ThirdPersonCameraController.TriggerScreenShake(0.5f, 0.3f);

            // Visuals & Sound
            SoundManager.PlayBeyHit(contactPoint, 1.2f);
            EpicAbilityVFXHelper.SpawnShockwaveRing(contactPoint, new Color(1f, 0.85f, 0.2f, 0.65f), 0.2f, 3.5f, 0.4f);
            EpicAbilityVFXHelper.SpawnParticleBurst(contactPoint, 12, new Color(1f, 0.8f, 0.2f), new Color(1f, 0.3f, 0.1f), 0.9f);
        }

        private void Update()
        {
            if (!IsInBladeLock)
                return;

            DurationRemaining -= Time.unscaledDeltaTime;

            // ONLY LEFT MOUSE BUTTON (LMB) is accepted for all Clash minigames
            bool lmbPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool lmbHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;

            // Update specific minigame logic
            switch (CurrentMinigame)
            {
                case ClashMinigameType.RapidMash:
                    UpdateRapidMash(lmbPressed);
                    break;

                case ClashMinigameType.PrecisionTiming:
                    UpdatePrecisionTiming(lmbPressed);
                    break;

                case ClashMinigameType.RhythmBeat:
                    UpdateRhythmBeat(lmbPressed);
                    break;

                case ClashMinigameType.TensionBalance:
                    UpdateTensionBalance(lmbHeld);
                    break;

                case ClashMinigameType.OrbitalCrosshair:
                    UpdateOrbitalCrosshair(lmbPressed);
                    break;

                case ClashMinigameType.ReflexTrigger:
                    UpdateReflexTrigger(lmbPressed);
                    break;
            }

            // Contact holding & Micro-jitter vibration
            Vector3 jitter = UnityEngine.Random.insideUnitSphere * 0.04f;
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
                    enemyRb.angularVelocity = -Vector3.up * 60f;
                }
            }

            // Spark emission at clash point
            if (Time.unscaledTime >= nextSparkTime)
            {
                nextSparkTime = Time.unscaledTime + UnityEngine.Random.Range(0.04f, 0.09f);
                EpicAbilityVFXHelper.SpawnParticleBurst(
                    ClashPosition + UnityEngine.Random.insideUnitSphere * 0.06f,
                    4,
                    new Color(1f, 0.85f, 0.2f),
                    new Color(1f, 0.3f, 0.05f),
                    0.4f);
            }

            // Check Resolution Condition
            if (DurationRemaining <= 0f || ClashMeter >= 1.0f || ClashMeter <= 0.0f)
            {
                ResolveBladeLock();
            }
        }

        // ── 1. Rapid Mash Minigame ──────────────────────────────────
        private void UpdateRapidMash(bool lmbPressed)
        {
            if (lmbPressed)
            {
                PlayerMashCount++;
                ClashMeter = Mathf.Clamp01(ClashMeter + 0.078f);
                ThirdPersonCameraController.TriggerScreenShake(0.16f, 0.06f);
                SoundManager.PlayUiConfirm();

                EpicAbilityVFXHelper.SpawnParticleBurst(
                    ClashPosition,
                    6,
                    new Color(0.2f, 0.9f, 1f),
                    new Color(1f, 0.85f, 0.2f),
                    0.6f);
            }

            // Enemy resistance push
            ClashMeter = Mathf.Clamp01(ClashMeter - EnemyPushRate * Time.unscaledDeltaTime);
        }

        // ── 2. Precision Timing Minigame ─────────────────────────────
        private void UpdatePrecisionTiming(bool lmbPressed)
        {
            NeedlePos += needleDir * needleSpeed * Time.unscaledDeltaTime;
            if (NeedlePos >= 1f) { NeedlePos = 1f; needleDir = -1f; }
            else if (NeedlePos <= 0f) { NeedlePos = 0f; needleDir = 1f; }

            if (lmbPressed)
            {
                bool isHit = NeedlePos >= SweetSpotMin && NeedlePos <= SweetSpotMax;
                if (isHit)
                {
                    ClashMeter = Mathf.Clamp01(ClashMeter + 0.38f);
                    ThirdPersonCameraController.TriggerScreenShake(0.35f, 0.12f);
                    SoundManager.PlayUiConfirm();
                    RuntimeGameUiController.SpawnGlobalComicPopup("PERFECT STRIKE! (+38%)", new Color(0.2f, 1f, 0.6f), 1.2f);
                    
                    // Reposition sweet spot
                    SweetSpotMin = UnityEngine.Random.Range(0.25f, 0.65f);
                    SweetSpotMax = SweetSpotMin + UnityEngine.Random.Range(0.16f, 0.22f);
                }
                else
                {
                    ClashMeter = Mathf.Clamp01(ClashMeter - 0.14f);
                    SoundManager.PlayBeyHit(ClashPosition, 1f);
                    RuntimeGameUiController.SpawnGlobalComicPopup("MISSED TIMING!", new Color(1f, 0.2f, 0.2f), 1f);
                }
            }

            // Slight enemy pressure
            ClashMeter = Mathf.Clamp01(ClashMeter - (EnemyPushRate * 0.4f) * Time.unscaledDeltaTime);
        }

        // ── 3. Rhythm Beat Minigame ──────────────────────────────────
        private void UpdateRhythmBeat(bool lmbPressed)
        {
            currentBeatTimer += Time.unscaledDeltaTime;
            BeatProgress = Mathf.Clamp01(currentBeatTimer / beatDuration);

            if (lmbPressed)
            {
                // Sweet timing window is near completion of beat (0.80..1.0)
                if (BeatProgress >= 0.78f && BeatProgress <= 1.0f)
                {
                    ClashMeter = Mathf.Clamp01(ClashMeter + 0.32f);
                    ThirdPersonCameraController.TriggerScreenShake(0.30f, 0.10f);
                    SoundManager.PlayUiConfirm();
                    RuntimeGameUiController.SpawnGlobalComicPopup($"PERFECT BEAT [{CurrentBeatIndex + 1}/{TotalBeats}]!", new Color(0.2f, 1f, 0.8f), 1f);
                }
                else
                {
                    ClashMeter = Mathf.Clamp01(ClashMeter - 0.12f);
                    SoundManager.PlayBeyHit(ClashPosition, 0.9f);
                    RuntimeGameUiController.SpawnGlobalComicPopup("OFF BEAT!", new Color(1f, 0.3f, 0.3f), 0.9f);
                }

                AdvanceNextBeat();
            }
            else if (currentBeatTimer >= beatDuration + 0.12f)
            {
                // Missed the beat entirely
                ClashMeter = Mathf.Clamp01(ClashMeter - 0.15f);
                AdvanceNextBeat();
            }
        }

        private void AdvanceNextBeat()
        {
            CurrentBeatIndex++;
            currentBeatTimer = 0f;
            BeatProgress = 0f;
            if (CurrentBeatIndex >= TotalBeats)
            {
                // Completed all beats!
                DurationRemaining = 0f; // trigger resolution
            }
        }

        // ── 4. Tension Balance Minigame ──────────────────────────────
        private void UpdateTensionBalance(bool lmbHeld)
        {
            // Gliding target
            BalanceTargetPos = 0.5f + 0.38f * Mathf.Sin(Time.unscaledTime * 3.5f);

            // Bobber physics (holds = upwards thrust, releases = gravity)
            float thrust = lmbHeld ? 4.5f : -4.0f;
            balanceBobberVel += thrust * Time.unscaledDeltaTime;
            balanceBobberVel *= 0.90f; // damping
            BalanceBobberPos = Mathf.Clamp01(BalanceBobberPos + balanceBobberVel * Time.unscaledDeltaTime);

            bool inZone = Mathf.Abs(BalanceBobberPos - BalanceTargetPos) <= 0.14f;
            if (inZone)
            {
                ClashMeter = Mathf.Clamp01(ClashMeter + 0.42f * Time.unscaledDeltaTime);
            }
            else
            {
                ClashMeter = Mathf.Clamp01(ClashMeter - 0.28f * Time.unscaledDeltaTime);
            }
        }

        // ── 5. Orbital Crosshair Minigame ────────────────────────────
        private void UpdateOrbitalCrosshair(bool lmbPressed)
        {
            OrbitAngle = (OrbitAngle + orbitSpeed * Time.unscaledDeltaTime) % 360f;

            if (lmbPressed)
            {
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(OrbitAngle, TargetLockAngle));
                if (angleDiff <= 28f)
                {
                    ClashMeter = Mathf.Clamp01(ClashMeter + 0.35f);
                    ThirdPersonCameraController.TriggerScreenShake(0.35f, 0.10f);
                    SoundManager.PlayUiConfirm();
                    RuntimeGameUiController.SpawnGlobalComicPopup("ORBITAL LOCK HIT!", new Color(0.2f, 0.85f, 1f), 1f);

                    // Relocate target
                    TargetLockAngle = (TargetLockAngle + UnityEngine.Random.Range(90f, 270f)) % 360f;
                }
                else
                {
                    ClashMeter = Mathf.Clamp01(ClashMeter - 0.12f);
                    SoundManager.PlayBeyHit(ClashPosition, 0.8f);
                }
            }

            ClashMeter = Mathf.Clamp01(ClashMeter - (EnemyPushRate * 0.45f) * Time.unscaledDeltaTime);
        }

        // ── 6. Reflex Trigger Minigame ───────────────────────────────
        private void UpdateReflexTrigger(bool lmbPressed)
        {
            if (!ReflexSignalActive)
            {
                ReflexStandbyTimer -= Time.unscaledDeltaTime;
                if (lmbPressed)
                {
                    // False start! Clicked too early!
                    FalseStart = true;
                    ClashMeter = 0.15f;
                    SoundManager.PlayBeyHit(ClashPosition, 1.2f);
                    RuntimeGameUiController.SpawnGlobalComicPopup("FALSE START! EARLY CLICK!", ACCENT_RED, 1.5f);
                    DurationRemaining = 0f; // resolve with penalty
                    return;
                }

                if (ReflexStandbyTimer <= 0f)
                {
                    ReflexSignalActive = true;
                    reflexSignalStartTime = Time.unscaledTime;
                    SoundManager.PlayUiConfirm();
                    ThirdPersonCameraController.TriggerScreenShake(0.4f, 0.15f);
                }
            }
            else
            {
                if (lmbPressed && PlayerReactionTime < 0f)
                {
                    PlayerReactionTime = Time.unscaledTime - reflexSignalStartTime;
                    if (PlayerReactionTime < 0.28f)
                    {
                        ClashMeter = 1.0f; // Instant Crush!
                        RuntimeGameUiController.SpawnGlobalComicPopup($"LIGHTNING REFLEX ({PlayerReactionTime:F3}s) // CRUSH!", ACCENT_GOLD, 2f);
                    }
                    else if (PlayerReactionTime < 0.45f)
                    {
                        ClashMeter = Mathf.Clamp01(ClashMeter + 0.45f);
                        RuntimeGameUiController.SpawnGlobalComicPopup($"FAST REFLEX ({PlayerReactionTime:F3}s)!", ACCENT_CYAN, 1.5f);
                    }
                    else
                    {
                        ClashMeter = Mathf.Clamp01(ClashMeter - 0.20f);
                        RuntimeGameUiController.SpawnGlobalComicPopup($"TOO SLOW ({PlayerReactionTime:F3}s)!", ACCENT_RED, 1.2f);
                    }
                    DurationRemaining = 0f;
                }
            }
        }

        private static readonly Color ACCENT_GOLD = new Color(1f, 0.82f, 0.20f, 1f);
        private static readonly Color ACCENT_CYAN = new Color(0.18f, 0.90f, 1f, 1f);
        private static readonly Color ACCENT_RED = new Color(1f, 0.22f, 0.28f, 1f);

        private void ResolveBladeLock()
        {
            IsInBladeLock = false;
            Time.timeScale = 1.0f;
            ThirdPersonCameraController.SetBladeLockFocus(Vector3.zero, false);

            EpicAbilityVFXHelper.SpawnShockwaveRing(ClashPosition, new Color(1f, 0.95f, 0.4f, 0.85f), 0.4f, 6.5f, 0.45f);
            EpicAbilityVFXHelper.SpawnParticleBurst(ClashPosition, 18, new Color(1f, 0.85f, 0.2f), new Color(1f, 0.2f, 0.1f), 1.8f);
            SoundManager.PlayBeyHit(ClashPosition, 1.6f);

            Vector3 pushDir = (Player != null && Enemy != null) ? (Enemy.transform.position - Player.transform.position).normalized : Vector3.forward;
            if (pushDir.sqrMagnitude < 0.01f)
                pushDir = ClashNormal;

            if (ClashMeter >= 0.5f)
            {
                // Player Won the Clash!
                float winStrength = (ClashMeter - 0.5f) * 2f;
                float recoilForce = Mathf.Lerp(22f, 44f, winStrength);
                float spinDamage = Mathf.Lerp(25f, 60f, winStrength);

                if (enemyRb != null) enemyRb.AddForce(pushDir * recoilForce + Vector3.up * 4f, ForceMode.Impulse);
                if (playerRb != null) playerRb.AddForce(-pushDir * 6f, ForceMode.Impulse);

                if (Enemy != null && Enemy.BeyConfiguration != null)
                {
                    Enemy.BeyConfiguration.ModifySpin(-spinDamage);
                }

                if (Player != null && Player.BeyConfiguration?.ShrineState != null)
                {
                    int bonusPts = Mathf.RoundToInt(Mathf.Lerp(15f, 40f, winStrength));
                    Player.BeyConfiguration.ShrineState.AddPoints(bonusPts);
                }

                ThirdPersonCameraController.TriggerScreenShake(0.8f, 0.4f);
                RuntimeGameUiController.SpawnGlobalComicPopup($"CLASH VICTORY! (-{spinDamage:F0} ENEMY SPIN)", ACCENT_CYAN, 2.2f);
            }
            else
            {
                // Enemy Won the Clash
                float loseStrength = (0.5f - ClashMeter) * 2f;
                float recoilForce = Mathf.Lerp(20f, 38f, loseStrength);
                float spinDamage = Mathf.Lerp(20f, 45f, loseStrength);

                if (playerRb != null) playerRb.AddForce(-pushDir * recoilForce + Vector3.up * 4f, ForceMode.Impulse);
                if (enemyRb != null) enemyRb.AddForce(pushDir * 6f, ForceMode.Impulse);

                if (Player != null && Player.BeyConfiguration != null)
                {
                    Player.BeyConfiguration.ModifySpin(-spinDamage);
                }

                ThirdPersonCameraController.TriggerScreenShake(0.7f, 0.35f);
                RuntimeGameUiController.SpawnGlobalComicPopup($"OVERPOWERED! (-{spinDamage:F0} SPIN)", ACCENT_RED, 2.0f);
            }
        }
    }
}
