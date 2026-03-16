using UnityEngine;
using System.Collections.Generic;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.World;

namespace BladeSpinners.Gameplay
{
    /// <summary>
    /// Coordinates match state: tracks all active beys, detects burst/KO,
    /// announces results, and handles match restart.
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
        [SerializeField] private float countdownDuration = 3f;
        [SerializeField] private float postMatchDelay = 3f;
        [SerializeField] private bool autoRestartOnPlayerWin = false;
        [SerializeField] private bool autoRestartOnPlayerLoss = false;
        [SerializeField] private float ringOutYThreshold = -2.5f;

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

        public MatchState CurrentState => currentState;
        public int EnemiesRemaining => aliveEnemies.Count;
        public float CountdownRemaining => currentState == MatchState.WaitingToStart ? Mathf.Max(0f, stateTimer) : 0f;
        public float CountdownDuration => countdownDuration;
        public PlayerDefeatReason LastPlayerDefeatReason => lastPlayerDefeatReason;
        public string LastPlayerDefeatMessage => lastPlayerDefeatMessage;
        public IReadOnlyList<BeyPart> LastKillerParts => lastEnemyAggressorParts;

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
                playerManager.BeyConfiguration.SetSpinDrainPaused(false);
            }

            ResetDefeatTracking();
            SetCombatControllersEnabled(false);

            SetState(MatchState.WaitingToStart);
            stateTimer = countdownDuration;
        }

        public void NotifyPlayerJump()
        {
            // Kept for compatibility with callers. Death attribution now depends
            // strictly on recent blade collision timing.
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
                        SetCombatControllersEnabled(true);
                        SetState(MatchState.InProgress);
                    }
                    break;

                case MatchState.InProgress:
                    CheckForBursts();
                    break;

                case MatchState.PlayerWon:
                    if (autoRestartOnPlayerWin)
                    {
                        stateTimer -= Time.deltaTime;
                        if (stateTimer <= 0f)
                            RestartMatch();
                    }
                    break;

                case MatchState.PlayerLost:
                    if (autoRestartOnPlayerLoss)
                    {
                        stateTimer -= Time.deltaTime;
                        if (stateTimer <= 0f)
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
                return "An enemy bursted you.";

            return "You spun out naturally.";
        }

        private string BuildRingOutDefeatMessage(PlayerDefeatReason reason)
        {
            if (reason == PlayerDefeatReason.KnockedOutByEnemy)
                return "An enemy knocked you out of the arena.";

            return "You flew out.";
        }

        private void CacheKillerParts(BeyConfiguration enemyConfig)
        {
            lastEnemyAggressorParts.Clear();
            if (enemyConfig == null)
                return;

            lastEnemyAggressorParts.Add(enemyConfig.GetEquippedPart(PartType.Tip));
            lastEnemyAggressorParts.Add(enemyConfig.GetEquippedPart(PartType.Track));
            lastEnemyAggressorParts.Add(enemyConfig.GetEquippedPart(PartType.FusionWheel));
            lastEnemyAggressorParts.Add(enemyConfig.GetEquippedPart(PartType.EnergyRing));
            lastEnemyAggressorParts.Add(enemyConfig.GetEquippedPart(PartType.FaceBolt));
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

            lastPlayerDefeatReason = reason;
            lastPlayerDefeatMessage = string.IsNullOrWhiteSpace(message) ? "You were defeated." : message;
            OnPlayerDefeated?.Invoke(lastPlayerDefeatReason, lastPlayerDefeatMessage);

            SetCombatControllersEnabled(false);
            SetState(MatchState.PlayerLost);
            stateTimer = postMatchDelay;
        }

        private void HandleEnemyBurst(EnemyBeyController enemy)
        {
            Debug.Log($"💥 {enemy.gameObject.name} BURST!");
            TryDropPartFromEnemy(enemy);
            enemy.OnBurst();
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
            color.a = useTransparentDropMaterial ? Mathf.Clamp01(dropVisualAlpha) : 1f;
            material.color = color;
            if (useTransparentDropMaterial)
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
            }

            AutoCollectAllDroppedParts();
            SetCombatControllersEnabled(false);
            SetState(MatchState.PlayerWon);
            stateTimer = autoRestartOnPlayerWin ? postMatchDelay : 0f;
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
            OnMatchStateChanged?.Invoke(newState);
            Debug.Log($"[MatchManager] State → {newState}");
        }

        private void RestartMatch()
        {
            Debug.Log("[MatchManager] Restarting match...");
            if (playerManager != null)
            {
                BeyMovementController movement = playerManager.MovementController;
                if (movement != null) movement.enabled = true;

                playerManager.BeyConfiguration?.SetSpin(GameConstants.DEFAULT_STARTING_SPIN);
                playerManager.BeyConfiguration?.SetMana(GameConstants.DEFAULT_MANA_POOL);
                playerManager.BeyConfiguration?.SetSpinDrainPaused(false);

                playerManager.transform.position = new Vector3(0, 3, 0);
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
