using UnityEngine;
using System.Collections.Generic;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.World;
using BladeSpinners.Audio;

namespace BladeSpinners.Gameplay
{
    /// <summary>
    /// Coordinates match state: tracks all active beys, detects burst/KO,
    /// triggers slow-mo finishes, announces results, and handles match restart.
    /// Attach to a persistent GameObject in the scene (e.g., "MatchManager").
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        public enum MatchState { WaitingToStart, InProgress, PlayerWon, PlayerLost }
        public enum PlayerDefeatReason
        {
            Unknown,
            SpunOut,
            BurstedByEnemy,
            KnockedOutByEnemy,
            JumpedOut
        }

        [Header("Match Settings")]
        [SerializeField] private float countdownDuration = 10f;
        [SerializeField] private float postMatchDelay = 3f;
        [SerializeField] private bool autoRestartOnPlayerWin = false;
        [SerializeField] private bool autoRestartOnPlayerLoss = false;
        [SerializeField] private float ringOutYThreshold = -10f;

        private const float EnemyCollisionKillWindowSeconds = 2f;

        [Header("Enemy Part Drops")]
        [SerializeField, Range(0f, 1f)] private float anyPartDropChance = 0.97f;
        [SerializeField] private Vector3 dropSpawnOffset = new Vector3(0f, 0.35f, 0f);
        [SerializeField] private Vector3 dropVisualScale = new Vector3(1.75f, 1.75f, 1.75f);
        [SerializeField] private bool useTransparentDropMaterial = true;
        [SerializeField, Range(0.15f, 1f)] private float dropVisualAlpha = 0.58f;
        [SerializeField] private float partPickupRadius = 1.25f;

        private MatchState currentState = MatchState.WaitingToStart;
        private float stateTimer;

        /// <summary>Where to place the player on restart. Set by RuntimeRunBuilder for hole arenas.</summary>
        private Vector3 playerSpawnPosition = new Vector3(0f, 3f, 0f);
        public void SetPlayerSpawnPosition(Vector3 pos) => playerSpawnPosition = pos;

        private PlayerManager playerManager;
        private readonly List<EnemyBeyController> enemies = new List<EnemyBeyController>();
        private readonly List<EnemyBeyController> aliveEnemies = new List<EnemyBeyController>();
        private readonly List<BeyPart> lastEnemyAggressorParts = new List<BeyPart>(5);

        private float lastPlayerEnemyHitTime = -999f;
        private PlayerDefeatReason lastPlayerDefeatReason = PlayerDefeatReason.Unknown;
        private string lastPlayerDefeatMessage = "";

        public event System.Action<MatchState> OnMatchStateChanged;
        public event System.Action<string> OnBeyBurst;
        public event System.Action<PlayerDefeatReason, string> OnPlayerDefeated;
        public event System.Action<BladeSpinners.Abilities.LaunchRating, float> OnRipCordExecuted;

        // ── Rip-Cord Minigame ──────────────────────────────────────────
        private bool hasPlayerRipped = false;
        private float ripAccuracy = 0f;
        private BladeSpinners.Abilities.LaunchRating ripRating = BladeSpinners.Abilities.LaunchRating.Good;
        private float ripMultiplier = 1f;

        public bool HasPlayerRipped => hasPlayerRipped;
        public float RipAccuracy => ripAccuracy;
        public BladeSpinners.Abilities.LaunchRating RipRating => ripRating;
        public static MatchManager Instance { get; private set; }

        public float RipMultiplier => ripMultiplier;

        public MatchState CurrentState => currentState;
        public int EnemiesRemaining => aliveEnemies.Count;
        public float CountdownRemaining => currentState == MatchState.WaitingToStart ? Mathf.Max(0f, stateTimer) : 0f;
        public float CountdownDuration => countdownDuration;
        public float StateTimer => stateTimer;
        public PlayerDefeatReason LastPlayerDefeatReason => lastPlayerDefeatReason;
        public string LastPlayerDefeatMessage => lastPlayerDefeatMessage;
        public IReadOnlyList<BeyPart> LastKillerParts => lastEnemyAggressorParts;

        private void Awake()
        {
            Instance = this;
        }

        public void RegisterPlayer(PlayerManager player)
        {
            playerManager = player;
        }

        public void RegisterEnemy(EnemyBeyController enemy)
        {
            if (!enemies.Contains(enemy))
            {
                enemies.Add(enemy);
                aliveEnemies.Add(enemy);
            }
        }

        public void StartMatch()
        {
            if (playerManager != null && playerManager.BeyConfiguration != null)
            {
                playerManager.BeyConfiguration.ResetResourcesForMatch();
            }
            for (int i = 0; i < enemies.Count; i++)
                enemies[i]?.BeyConfiguration?.ResetResourcesForMatch();

            hasPlayerRipped = false;
            ripAccuracy = 0f;
            ripRating = BladeSpinners.Abilities.LaunchRating.Good;
            ripMultiplier = 1f;

            ResetDefeatTracking();
            SetCombatControllersEnabled(false);

            SetState(MatchState.WaitingToStart);
            stateTimer = countdownDuration;
        }

        /// <summary>
        /// Called by UI or input when the player rips the cord during countdown.
        /// </summary>
        public void ExecuteRipCord(float needlePosition01)
        {
            if (currentState != MatchState.WaitingToStart || hasPlayerRipped)
                return;

            hasPlayerRipped = true;

            bool hasTurboRip = playerManager != null && playerManager.BeyConfiguration != null && playerManager.BeyConfiguration.HasShrinePerk(BladeSpinners.Gameplay.Shrine.ShrinePerkType.TurboRip);
            ripAccuracy = Mathf.Clamp01(needlePosition01);

            if (ripAccuracy >= 0.85f)
            {
                ripRating = BladeSpinners.Abilities.LaunchRating.Perfect;
                ripMultiplier = hasTurboRip ? 1.40f : 1.25f;
                if (playerManager?.BeyConfiguration?.ShrineState != null)
                    playerManager.BeyConfiguration.ShrineState.AddPoints(25);
            }
            else if (ripAccuracy >= 0.60f)
            {
                ripRating = BladeSpinners.Abilities.LaunchRating.Great;
                ripMultiplier = hasTurboRip ? 1.18f : 1.08f;
            }
            else if (ripAccuracy >= 0.30f)
            {
                ripRating = BladeSpinners.Abilities.LaunchRating.Good;
                ripMultiplier = 0.95f;
            }
            else
            {
                ripRating = BladeSpinners.Abilities.LaunchRating.Mishap;
                ripMultiplier = 0.75f;
            }

            ApplyRipLaunchResults();
        }

        private void ApplyRipLaunchResults()
        {
            if (playerManager != null && playerManager.BeyConfiguration != null)
            {
                playerManager.BeyConfiguration.ApplyLaunchRipMultiplier(ripMultiplier);

                Vector3 pPos = playerManager.transform.position;
                Vector3 pFwd = playerManager.transform.forward;

                BladeSpinners.Abilities.EpicAbilityVFXHelper.SpawnLaunchRipVFX(pPos, pFwd, ripRating);

                // Forward surge impulse based on quality
                Rigidbody pRb = playerManager.GetComponent<Rigidbody>();
                if (pRb != null)
                {
                    float launchForce = ripRating switch
                    {
                        BladeSpinners.Abilities.LaunchRating.Perfect => 18f,
                        BladeSpinners.Abilities.LaunchRating.Great => 12f,
                        BladeSpinners.Abilities.LaunchRating.Good => 6f,
                        _ => 2f
                    };
                    pRb.AddForce(pFwd * launchForce + Vector3.down * 4f, ForceMode.Impulse);
                }

                // Comic popup
                string ratingTitle = ripRating switch
                {
                    BladeSpinners.Abilities.LaunchRating.Perfect => $"PERFECT RIP!! {Mathf.RoundToInt(ripMultiplier * 100)}% SPIN",
                    BladeSpinners.Abilities.LaunchRating.Great => $"GREAT RIP! {Mathf.RoundToInt(ripMultiplier * 100)}% SPIN",
                    BladeSpinners.Abilities.LaunchRating.Good => $"GOOD RIP {Mathf.RoundToInt(ripMultiplier * 100)}% SPIN",
                    _ => $"MISHAP RIP... {Mathf.RoundToInt(ripMultiplier * 100)}% SPIN"
                };
                Color ratingColor = ripRating switch
                {
                    BladeSpinners.Abilities.LaunchRating.Perfect => new Color(1f, 0.85f, 0.1f, 1f),
                    BladeSpinners.Abilities.LaunchRating.Great => new Color(0.1f, 0.85f, 1f, 1f),
                    BladeSpinners.Abilities.LaunchRating.Good => new Color(1f, 0.6f, 0.1f, 1f),
                    _ => new Color(0.7f, 0.7f, 0.7f, 1f)
                };
                float popScale = ripRating == BladeSpinners.Abilities.LaunchRating.Perfect ? 1.55f : 1.15f;
                BladeSpinners.Gameplay.UI.RuntimeGameUiController.SpawnComicPopup(ratingTitle, ratingColor, popScale);
            }

            OnRipCordExecuted?.Invoke(ripRating, ripMultiplier);
        }

        public void NotifyPlayerJump()
        {
            // Kept for compatibility with callers.
        }

        public void NotifyPlayerHitByEnemy(BeyConfiguration enemyConfig, bool wasKnockback)
        {
            lastPlayerEnemyHitTime = Time.time;
            CacheKillerParts(enemyConfig);
        }

        public void RequestRestart()
        {
            RestartMatch();
        }

        private void Start()
        {
            if (playerManager == null)
            {
                playerManager = FindFirstObjectByType<PlayerManager>();
                if (playerManager != null)
                    Debug.Log($"[MatchManager] Auto-discovered player: {playerManager.gameObject.name}");
            }

            if (aliveEnemies.Count == 0)
            {
                EnemyBeyController[] foundEnemies = FindObjectsByType<EnemyBeyController>(FindObjectsSortMode.None);
                for (int i = 0; i < foundEnemies.Length; i++)
                {
                    EnemyBeyController enemy = foundEnemies[i];
                    if (!enemies.Contains(enemy))
                    {
                        enemies.Add(enemy);
                        aliveEnemies.Add(enemy);
                    }
                }
                Debug.Log($"[MatchManager] Auto-discovered {aliveEnemies.Count} enemies");
            }

            if (currentState == MatchState.WaitingToStart && stateTimer <= 0f)
            {
                stateTimer = countdownDuration;
            }
        }

        private void Update()
        {
            switch (currentState)
            {
                case MatchState.WaitingToStart:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                    {
                        if (!hasPlayerRipped)
                        {
                            ExecuteRipCord(0.5f); // fallback rip
                        }
                        SetCombatControllersEnabled(true);
                        SetState(MatchState.InProgress);
                    }
                    break;

                case MatchState.InProgress:
                    CheckForBursts();
                    break;

                case MatchState.PlayerWon:
                    stateTimer -= Time.unscaledDeltaTime;
                    if (autoRestartOnPlayerWin && stateTimer <= 0f)
                    {
                        RestartMatch();
                    }
                    break;

                case MatchState.PlayerLost:
                    stateTimer -= Time.unscaledDeltaTime;
                    if (autoRestartOnPlayerLoss && stateTimer <= 0f)
                    {
                        RestartMatch();
                    }
                    break;
            }
        }

        private void CheckForBursts()
        {
            if (CheckForPlayerRingOut())
                return;

            if (playerManager != null && playerManager.BeyConfiguration != null && playerManager.BeyConfiguration.IsBurst)
            {
                OnBeyBurst?.Invoke("Player");
                HandlePlayerBurst(GetBurstDefeatReason(), BuildBurstDefeatMessage());
                return;
            }

            for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            {
                EnemyBeyController enemy = aliveEnemies[i];
                if (enemy == null || enemy.BeyConfiguration == null)
                {
                    aliveEnemies.RemoveAt(i);
                    continue;
                }

                if (enemy.transform.position.y <= ringOutYThreshold)
                {
                    string ringOutEnemyName = enemy.gameObject.name;
                    Debug.Log($"💥 {ringOutEnemyName} RING-OUT! Triggering disassembly...");
                    OnBeyBurst?.Invoke(ringOutEnemyName);
                    HandleEnemyBurst(enemy);
                    aliveEnemies.RemoveAt(i);

                    if (aliveEnemies.Count == 0)
                    {
                        HandlePlayerWin();
                        return;
                    }

                    continue;
                }

                if (!enemy.BeyConfiguration.IsBurst)
                    continue;

                string burstEnemyName = enemy.gameObject.name;
                Debug.Log($"💥 {burstEnemyName} BURST! Triggering disassembly...");
                OnBeyBurst?.Invoke(burstEnemyName);
                HandleEnemyBurst(enemy);
                aliveEnemies.RemoveAt(i);

                if (aliveEnemies.Count == 0)
                {
                    HandlePlayerWin();
                    return;
                }
            }
        }

        private bool CheckForPlayerRingOut()
        {
            if (playerManager == null)
                return false;

            if (playerManager.transform.position.y > ringOutYThreshold)
                return false;

            PlayerDefeatReason reason = GetRingOutDefeatReason();
            HandlePlayerBurst(reason, BuildRingOutDefeatMessage(reason));
            return true;
        }

        private PlayerDefeatReason GetBurstDefeatReason()
        {
            if (WasRecentEnemyCollision())
                return PlayerDefeatReason.BurstedByEnemy;

            return PlayerDefeatReason.SpunOut;
        }

        private PlayerDefeatReason GetRingOutDefeatReason()
        {
            if (WasRecentEnemyCollision())
                return PlayerDefeatReason.KnockedOutByEnemy;

            return PlayerDefeatReason.JumpedOut;
        }

        private bool WasRecentEnemyCollision()
        {
            return Time.time - lastPlayerEnemyHitTime <= EnemyCollisionKillWindowSeconds;
        }

        private string BuildBurstDefeatMessage()
        {
            if (WasRecentEnemyCollision())
            {
                string[] burstLines =
                {
                    "Opponent sent your parts flying into the stratosphere!",
                    "DISMANTLED! Your Bey didn't even have time to scream!",
                    "That hit shattered your spin in 4K Ultra HD!",
                    "MEGA CRITICAL! Your Bey exploded into confetti!"
                };
                return burstLines[Random.Range(0, burstLines.Length)];
            }

            string[] spinOutLines =
            {
                "Friction 1 - 0 You. Spin stamina completely exhausted!",
                "You ran out of juice! Did you forget to wind the launcher?",
                "Total spin flatline! The arena floor claimed your velocity!"
            };
            return spinOutLines[Random.Range(0, spinOutLines.Length)];
        }

        private string BuildRingOutDefeatMessage(PlayerDefeatReason reason)
        {
            if (reason == PlayerDefeatReason.KnockedOutByEnemy)
            {
                string[] knockOutLines =
                {
                    "MEGA YEET! You were launched clean out of the stadium!",
                    "Calculated home run by the opponent! Bye bye!",
                    "Orbit achieved! You are now an official low-altitude satellite!",
                    "Sent packing beyond the outer rim!"
                };
                return knockOutLines[Random.Range(0, knockOutLines.Length)];
            }

            string[] selfYeetLines =
            {
                "You self-yeeted directly into the void... 10/10 diving form though!",
                "Nobody pushed you, champion. That was purely self-directed aerial exploration!",
                "Down you go! Next time remember the arena has boundaries!"
            };
            return selfYeetLines[Random.Range(0, selfYeetLines.Length)];
        }

        private void CacheKillerParts(BeyConfiguration enemyConfig)
        {
            lastEnemyAggressorParts.Clear();
            if (enemyConfig == null)
                return;

            lastEnemyAggressorParts.Add(enemyConfig.GetEquippedPart(PartType.FaceBolt));
            lastEnemyAggressorParts.Add(enemyConfig.GetEquippedPart(PartType.EnergyRing));
            lastEnemyAggressorParts.Add(enemyConfig.GetEquippedPart(PartType.FusionWheel));
            lastEnemyAggressorParts.Add(enemyConfig.GetEquippedPart(PartType.Track));
            lastEnemyAggressorParts.Add(enemyConfig.GetEquippedPart(PartType.Tip));
        }

        private Coroutine slowMoCoroutine;

        public void TriggerSlowMoFinish(float slowTimeScale = 0.22f, float realDuration = 0.65f)
        {
            if (slowMoCoroutine != null) StopCoroutine(slowMoCoroutine);
            slowMoCoroutine = StartCoroutine(SlowMoRoutine(slowTimeScale, realDuration));
        }

        private System.Collections.IEnumerator SlowMoRoutine(float scale, float duration)
        {
            Time.timeScale = scale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            slowMoCoroutine = null;
        }

        private void HandlePlayerBurst(PlayerDefeatReason reason, string message)
        {
            if (currentState == MatchState.PlayerLost)
                return;

            Debug.Log($"💥 PLAYER DEFEATED! Reason={reason} Message={message}");

            if (playerManager != null)
            {
                Effects.BeyBurstEffect burstEffect = playerManager.GetComponent<Effects.BeyBurstEffect>();
                if (burstEffect == null)
                    burstEffect = playerManager.gameObject.AddComponent<Effects.BeyBurstEffect>();
                burstEffect.TriggerBurst();
            }

            TriggerSlowMoFinish(0.18f, 2.5f);
            ThirdPersonCameraController.TriggerScreenShake(0.75f, 0.5f);

            lastPlayerDefeatReason = reason;
            lastPlayerDefeatMessage = string.IsNullOrWhiteSpace(message) ? "You were defeated." : message;
            OnPlayerDefeated?.Invoke(lastPlayerDefeatReason, lastPlayerDefeatMessage);

            SetCombatControllersEnabled(false);
            SetState(MatchState.PlayerLost);
            stateTimer = 3.2f;
        }

        private void HandleEnemyBurst(EnemyBeyController enemy)
        {
            Debug.Log($"💥 {enemy.gameObject.name} BURST!");
            if (playerManager != null && playerManager.BeyConfiguration?.ShrineState != null)
            {
                playerManager.BeyConfiguration.ShrineState.AddPoints(40);
            }
            TryDropPartFromEnemy(enemy);
            enemy.OnBurst();

            ThirdPersonCameraController.TriggerTakedownCam(enemy.transform, 1.35f);
            TriggerSlowMoFinish(0.20f, 1.15f);
        }

        private void TryDropPartFromEnemy(EnemyBeyController enemy)
        {
            if (enemy == null)
                return;

            if (Random.value > anyPartDropChance)
                return;

            List<BeyPart> equippedParts = enemy.GetEquippedParts();
            if (equippedParts.Count == 0)
                return;

            BeyPart selectedPart = equippedParts[Random.Range(0, equippedParts.Count)];
            if (selectedPart == null)
                return;

            float rarityDropChance = GetRarityDropChance(selectedPart.Rarity);
            if (Random.value > rarityDropChance)
            {
                Debug.Log($"[PartDrop] {enemy.name} selected {PartDisplayNameFormatter.ToShortDisplayName(selectedPart)} ({selectedPart.Rarity}) but failed rarity roll ({rarityDropChance:P0}).");
                return;
            }

            SpawnPartDropPickup(selectedPart, enemy.transform.position + dropSpawnOffset);
        }

        private void SpawnPartDropPickup(BeyPart part, Vector3 worldPosition)
        {
            if (part == null)
                return;

            GameObject dropObject = new GameObject($"PartDrop_{PartDisplayNameFormatter.ToShortDisplayName(part).Replace(' ', '_')}");
            dropObject.transform.position = worldPosition;
            dropObject.transform.localScale = dropVisualScale;

            MeshFilter meshFilter = dropObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = dropObject.AddComponent<MeshRenderer>();
            Mesh dropMesh = ProceduralPartMeshGenerator.GenerateMesh(part);
            if (dropMesh != null)
            {
                meshFilter.sharedMesh = dropMesh;
            }

            Material material = new Material(ShaderProvider.URPLit);
            Color color = part.PrimaryColor;
            bool isEnergyRing = part.PartType == PartType.EnergyRing;
            color.a = isEnergyRing ? 0.56f : (useTransparentDropMaterial ? Mathf.Clamp01(dropVisualAlpha) : 1f);
            material.color = color;
            if (part.PartType == PartType.FusionWheel && material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 1f);
            if (part.PartType == PartType.FusionWheel && material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.92f);
            if (isEnergyRing || useTransparentDropMaterial)
            {
                ApplyTransparentMaterialSettings(material);
            }
            meshRenderer.sharedMaterial = material;

            SphereCollider trigger = dropObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = Mathf.Max(0.1f, partPickupRadius);

            dropObject.AddComponent<PickupBobAnimation>();
            PartDropPickup pickup = dropObject.AddComponent<PartDropPickup>();
            pickup.Initialize(part);

            Debug.Log($"[PartDrop] Dropped {PartDisplayNameFormatter.ToShortDisplayName(part)} ({part.Rarity}).");
        }

        private static void ApplyTransparentMaterialSettings(Material material)
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

        private static float GetRarityDropChance(RarityTier rarity)
        {
            return rarity switch
            {
                RarityTier.Common => 1f,
                RarityTier.Uncommon => 0.9f,
                RarityTier.Rare => 0.72f,
                RarityTier.Epic => 0.5f,
                RarityTier.Legendary => 0.24f,
                _ => 0.5f
            };
        }

        private void HandlePlayerWin()
        {
            Debug.Log("🏆 ALL ENEMIES BURST! You win!");

            if (playerManager != null && playerManager.BeyConfiguration != null)
            {
                playerManager.BeyConfiguration.SetSpinDrainPaused(true);
                if (playerManager.BeyConfiguration.ShrineState != null)
                {
                    playerManager.BeyConfiguration.ShrineState.AddPoints(150);
                }
            }

            AutoCollectAllDroppedParts();
            SetCombatControllersEnabled(false);

            if (playerManager != null)
            {
                ThirdPersonCameraController.TriggerVictoryShowcase(playerManager.transform, 5.2f);
            }
            TriggerSlowMoFinish(0.30f, 2.4f);

            SetState(MatchState.PlayerWon);
            stateTimer = 5.2f;
        }

        private void SetCombatControllersEnabled(bool enabled)
        {
            if (playerManager != null)
            {
                if (playerManager.InputHandler != null)
                    playerManager.InputHandler.enabled = enabled;
                if (playerManager.MovementController != null)
                    playerManager.MovementController.enabled = enabled;

                Rigidbody playerRb = playerManager.GetComponent<Rigidbody>();
                if (!enabled && playerRb != null)
                {
                    playerRb.linearVelocity = Vector3.zero;
                    playerRb.angularVelocity = Vector3.zero;
                }
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyBeyController enemy = enemies[i];
                if (enemy == null)
                    continue;

                AIInputHandler ai = enemy.GetComponent<AIInputHandler>();
                if (ai != null)
                    ai.enabled = enabled;

                BeyMovementController move = enemy.GetComponent<BeyMovementController>();
                if (move != null)
                    move.enabled = enabled;

                Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
                if (!enabled && enemyRb != null)
                {
                    enemyRb.linearVelocity = Vector3.zero;
                    enemyRb.angularVelocity = Vector3.zero;
                }
            }
        }

        private void ResetDefeatTracking()
        {
            lastPlayerEnemyHitTime = -999f;
            lastEnemyAggressorParts.Clear();
            lastPlayerDefeatReason = PlayerDefeatReason.Unknown;
            lastPlayerDefeatMessage = "";
        }

        private void AutoCollectAllDroppedParts()
        {
            if (playerManager == null)
                return;

            PartDropPickup[] worldDrops = FindObjectsByType<PartDropPickup>(FindObjectsSortMode.None);
            int collectedCount = 0;
            for (int i = 0; i < worldDrops.Length; i++)
            {
                PartDropPickup drop = worldDrops[i];
                if (drop != null && drop.TryCollect(playerManager))
                    collectedCount++;
            }

            if (collectedCount > 0)
            {
                Debug.Log($"[PartDrop] Auto-collected {collectedCount} dropped part(s) when all enemies were destroyed.");
            }
        }

        private void SetState(MatchState newState)
        {
            if (currentState == newState) return;
            currentState = newState;

            switch (newState)
            {
                case MatchState.InProgress:
                    SoundManager.PlayUi(SoundPaths.GuiGameStart);
                    break;
                case MatchState.PlayerWon:
                    SoundManager.PlayUi(SoundPaths.GuiGameWin);
                    break;
                case MatchState.PlayerLost:
                    SoundManager.PlayUi(SoundPaths.GuiGameLose);
                    break;
            }

            OnMatchStateChanged?.Invoke(newState);
            Debug.Log($"[MatchManager] State → {newState}");
        }

        private void RestartMatch()
        {
            Debug.Log("[MatchManager] Restarting match...");
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            if (playerManager != null)
            {
                BeyMovementController movement = playerManager.MovementController;
                if (movement != null) movement.enabled = true;

                playerManager.BeyConfiguration?.ResetResourcesForMatch();

                playerManager.transform.position = playerSpawnPosition;
                Rigidbody rb = playerManager.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            foreach (EnemyBeyController enemy in enemies)
            {
                if (enemy != null)
                    enemy.ResetBey();
            }

            aliveEnemies.Clear();
            aliveEnemies.AddRange(enemies);
            aliveEnemies.RemoveAll(e => e == null);
            enemies.RemoveAll(e => e == null);

            ResetDefeatTracking();
            SetCombatControllersEnabled(false);
            SetState(MatchState.WaitingToStart);
            stateTimer = countdownDuration;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        private void OnDrawGizmos()
        {
            Vector3 pos = Vector3.up * 15f;
            switch (currentState)
            {
                case MatchState.WaitingToStart:
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(pos, Vector3.one * 2f);
                    break;
                case MatchState.InProgress:
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(pos, 1f);
                    break;
                case MatchState.PlayerWon:
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(pos, 2f);
                    break;
                case MatchState.PlayerLost:
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(pos, 2f);
                    break;
            }

            if (playerManager != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(pos, playerManager.transform.position);
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyBeyController enemy = enemies[i];
                if (enemy == null)
                    continue;

                Gizmos.color = aliveEnemies.Contains(enemy) ? Color.red : Color.gray;
                Gizmos.DrawLine(pos, enemy.transform.position);
            }
        }
    }
}
