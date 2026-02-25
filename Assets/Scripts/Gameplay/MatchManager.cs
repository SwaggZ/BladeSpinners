using UnityEngine;
using System.Collections.Generic;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Gameplay
{
    /// <summary>
    /// Coordinates match state: tracks all active beys, detects burst/KO,
    /// announces results, and handles match restart.
    /// Attach to a persistent GameObject in the scene (e.g., "MatchManager").
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        // ── Match state ──────────────────────────────────────────────
        public enum MatchState { WaitingToStart, InProgress, PlayerWon, PlayerLost }

        [Header("Match Settings")]
        [SerializeField] private float countdownDuration = 3f;
        [SerializeField] private float postMatchDelay = 3f;

        private MatchState currentState = MatchState.WaitingToStart;
        private float stateTimer;

        // ── Bey registry ─────────────────────────────────────────────
        private PlayerManager playerManager;
        private readonly List<EnemyBeyController> enemies = new List<EnemyBeyController>();
        private readonly List<EnemyBeyController> aliveEnemies = new List<EnemyBeyController>();

        // ── Events ───────────────────────────────────────────────────
        public event System.Action<MatchState> OnMatchStateChanged;
        public event System.Action<string> OnBeyBurst;  // name of the burst bey

        // ── Public API ───────────────────────────────────────────────
        public MatchState CurrentState => currentState;
        public int EnemiesRemaining => aliveEnemies.Count;

        /// <summary>Register the player. Called by TestSceneSetup or a spawner.</summary>
        public void RegisterPlayer(PlayerManager player)
        {
            playerManager = player;
        }

        /// <summary>Register an enemy bey. Called when enemies are spawned.</summary>
        public void RegisterEnemy(EnemyBeyController enemy)
        {
            if (!enemies.Contains(enemy))
            {
                enemies.Add(enemy);
                aliveEnemies.Add(enemy);
            }
        }

        /// <summary>Begin the match (starts countdown).</summary>
        public void StartMatch()
        {
            SetState(MatchState.WaitingToStart);
            stateTimer = countdownDuration;
        }

        // ── Lifecycle ────────────────────────────────────────────────

        /// <summary>
        /// Auto-discover player and enemies at runtime.
        /// Edit-time registration (from TestSceneSetup) is lost because these
        /// fields are non-serialized — they reset to null/empty on Play.
        /// </summary>
        private void Start()
        {
            // Discover player
            if (playerManager == null)
            {
                playerManager = FindObjectOfType<PlayerManager>();
                if (playerManager != null)
                    Debug.Log($"[MatchManager] Auto-discovered player: {playerManager.gameObject.name}");
            }

            // Discover enemies
            if (aliveEnemies.Count == 0)
            {
                var foundEnemies = FindObjectsByType<EnemyBeyController>(FindObjectsSortMode.None);
                foreach (var enemy in foundEnemies)
                {
                    if (!enemies.Contains(enemy))
                    {
                        enemies.Add(enemy);
                        aliveEnemies.Add(enemy);
                    }
                }
                Debug.Log($"[MatchManager] Auto-discovered {aliveEnemies.Count} enemies");
            }

            // Auto-start match
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
                        SetState(MatchState.InProgress);
                    break;

                case MatchState.InProgress:
                    CheckForBursts();
                    break;

                case MatchState.PlayerWon:
                case MatchState.PlayerLost:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                        RestartMatch();
                    break;
            }
        }

        // ── Burst detection ──────────────────────────────────────────

        private void CheckForBursts()
        {
            // Check player burst
            if (playerManager != null && playerManager.BeyConfiguration != null)
            {
                if (playerManager.BeyConfiguration.IsBurst)
                {
                    OnBeyBurst?.Invoke("Player");
                    HandlePlayerBurst();
                    return;
                }
            }

            // Check enemy bursts
            for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            {
                EnemyBeyController enemy = aliveEnemies[i];
                if (enemy == null || enemy.BeyConfiguration == null)
                {
                    aliveEnemies.RemoveAt(i);
                    continue;
                }

                if (enemy.BeyConfiguration.IsBurst)
                {
                    string enemyName = enemy.gameObject.name;
                    Debug.Log($"💥 {enemyName} BURST! Triggering disassembly...");
                    OnBeyBurst?.Invoke(enemyName);
                    HandleEnemyBurst(enemy);
                    aliveEnemies.RemoveAt(i);

                    // Check win condition after each enemy burst
                    if (aliveEnemies.Count == 0)
                    {
                        HandlePlayerWin();
                        return;
                    }
                }
            }
        }

        private void HandlePlayerBurst()
        {
            Debug.Log("💥 PLAYER BURST! You lost!");

            // Trigger burst effect on the player bey (parts fly off)
            if (playerManager != null)
            {
                var burstEffect = playerManager.GetComponent<Effects.BeyBurstEffect>();
                if (burstEffect == null)
                    burstEffect = playerManager.gameObject.AddComponent<Effects.BeyBurstEffect>();
                burstEffect.TriggerBurst();
            }

            SetState(MatchState.PlayerLost);
            stateTimer = postMatchDelay;
        }

        private void HandleEnemyBurst(EnemyBeyController enemy)
        {
            Debug.Log($"💥 {enemy.gameObject.name} BURST!");
            // Disable the enemy bey (stop movement, make it fall over)
            enemy.OnBurst();
        }

        private void HandlePlayerWin()
        {
            Debug.Log("🏆 ALL ENEMIES BURST! You win!");
            SetState(MatchState.PlayerWon);
            stateTimer = postMatchDelay;
        }

        // ── Match flow helpers ───────────────────────────────────────

        private void SetState(MatchState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            OnMatchStateChanged?.Invoke(newState);
            Debug.Log($"[MatchManager] State → {newState}");
        }

        private void DisablePlayerMovement()
        {
            if (playerManager == null) return;
            BeyMovementController movement = playerManager.MovementController;
            if (movement != null) movement.enabled = false;
        }

        private void RestartMatch()
        {
            Debug.Log("[MatchManager] Restarting match...");
            // Re-enable player
            if (playerManager != null)
            {
                BeyMovementController movement = playerManager.MovementController;
                if (movement != null) movement.enabled = true;

                // Reset player spin
                playerManager.BeyConfiguration?.SetSpin(GameConstants.DEFAULT_STARTING_SPIN);
                playerManager.BeyConfiguration?.SetMana(GameConstants.DEFAULT_MANA_POOL);

                // Reset position
                playerManager.transform.position = new Vector3(0, 3, 0);
                Rigidbody rb = playerManager.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            // Reset enemies
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                    enemy.ResetBey();
            }
            aliveEnemies.Clear();
            aliveEnemies.AddRange(enemies);

            // Remove destroyed enemies
            aliveEnemies.RemoveAll(e => e == null);
            enemies.RemoveAll(e => e == null);

            SetState(MatchState.WaitingToStart);
            stateTimer = countdownDuration;
        }

        // ── Gizmos ───────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            // Draw match state label at world origin
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

            // Draw lines from match manager to all tracked beys
            if (playerManager != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(pos, playerManager.transform.position);
            }
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    Gizmos.color = aliveEnemies.Contains(enemy) ? Color.red : Color.gray;
                    Gizmos.DrawLine(pos, enemy.transform.position);
                }
            }
        }
    }
}
