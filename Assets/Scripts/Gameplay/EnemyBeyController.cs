using UnityEngine;
using System.Collections.Generic;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Combat;

namespace BladeSpinners.Gameplay
{
    /// <summary>
    /// Thin management shell for an enemy bey.
    /// The enemy is built with the EXACT same components as the player:
    ///   BeyMovementController, BeyTiltController, BeyCollisionDetector, BeyAssembler, BeyVisualSpin.
    /// Instead of PlayerInputHandler, it uses AIInputHandler which simulates WASD/mouse input.
    /// This class only handles burst/reset lifecycle and provides the BeyConfiguration reference
    /// that MatchManager needs.
    /// </summary>
    public class EnemyBeyController : MonoBehaviour
    {
        // ── References ───────────────────────────────────────────────
        private Rigidbody rb;
        private BeyMovementController movementController;
        private BeyTiltController tiltController;
        private BeyCollisionDetector collisionDetector;
        private AIInputHandler aiInput;
        private BeyAssembler assembler;
        private BeyConfiguration beyConfiguration;
        private Vector3 spawnPosition;
        private bool isBurst;

        // ── Public API (used by MatchManager) ────────────────────────
        public BeyConfiguration BeyConfiguration => beyConfiguration;
        public bool IsBurst => isBurst;

        public List<BeyPart> GetEquippedParts()
        {
            List<BeyPart> equippedParts = new List<BeyPart>();

            if (assembler == null)
                assembler = GetComponent<BeyAssembler>();

            if (assembler == null)
                return equippedParts;

            PartType[] slots =
            {
                PartType.Tip,
                PartType.Track,
                PartType.FusionWheel,
                PartType.EnergyRing,
                PartType.FaceBolt
            };

            for (int i = 0; i < slots.Length; i++)
            {
                BeyPart part = assembler.GetEquippedPart(slots[i]);
                if (part != null)
                    equippedParts.Add(part);
            }

            return equippedParts;
        }

        // ══════════════════════════════════════════════════════════════
        // Awake: re-wire all components.
        // BeyConfiguration is a plain C# class (not [System.Serializable]),
        // so Unity's serialization wipes it when entering Play mode.
        // This mirrors what PlayerManager.WireComponents() does for the player.
        // ══════════════════════════════════════════════════════════════
        private void Awake()
        {
            spawnPosition = transform.position;
            rb = GetComponent<Rigidbody>();
            movementController = GetComponent<BeyMovementController>();
            tiltController = GetComponent<BeyTiltController>();
            collisionDetector = GetComponent<BeyCollisionDetector>();
            aiInput = GetComponent<AIInputHandler>();
            assembler = GetComponent<BeyAssembler>();

            // Create a fresh config (serialization wiped the old one)
            if (beyConfiguration == null)
                beyConfiguration = new BeyConfiguration();
            beyConfiguration.IsEnemy = true;

            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // Re-wire BeyMovementController
            if (movementController != null)
            {
                typeof(BeyMovementController).GetField("beyConfiguration", flags)
                    ?.SetValue(movementController, beyConfiguration);
            }

            // Re-wire BeyTiltController
            if (tiltController != null)
            {
                typeof(BeyTiltController).GetField("movementController", flags)
                    ?.SetValue(tiltController, movementController);
                typeof(BeyTiltController).GetField("beyConfiguration", flags)
                    ?.SetValue(tiltController, beyConfiguration);
            }

            // Re-wire BeyCollisionDetector
            if (collisionDetector != null)
            {
                typeof(BeyCollisionDetector).GetField("beyConfiguration", flags)
                    ?.SetValue(collisionDetector, beyConfiguration);
                typeof(BeyCollisionDetector).GetField("movementController", flags)
                    ?.SetValue(collisionDetector, movementController);
            }

            // Re-wire AIInputHandler
            if (aiInput != null)
            {
                typeof(AIInputHandler).GetField("beyMovementController", flags)
                    ?.SetValue(aiInput, movementController);
                typeof(AIInputHandler).GetField("beyConfiguration", flags)
                    ?.SetValue(aiInput, beyConfiguration);

                // Find the player and set as target
                PlayerManager player = FindFirstObjectByType<PlayerManager>();
                if (player != null)
                    aiInput.SetTarget(player.transform);
            }

            // Re-push parts from assembler into the fresh config
            if (assembler != null)
                assembler.SetConfiguration(beyConfiguration);
        }

        /// <summary>
        /// Called after all components are added and wired (edit-mode setup).
        /// </summary>
        public void Initialize(BeyConfiguration config, Transform playerTarget)
        {
            beyConfiguration = config;
            beyConfiguration.IsEnemy = true;
            spawnPosition = transform.position;
            rb = GetComponent<Rigidbody>();
            movementController = GetComponent<BeyMovementController>();
            aiInput = GetComponent<AIInputHandler>();
            isBurst = false;

            if (aiInput != null)
                aiInput.SetTarget(playerTarget);
        }

        /// <summary>Called by MatchManager when this bey's spin hits 0.</summary>
        public void OnBurst()
        {
            isBurst = true;

            // Trigger burst effect — parts detach and fly outward
            var burstEffect = GetComponent<Effects.BeyBurstEffect>();
            if (burstEffect == null)
                burstEffect = gameObject.AddComponent<Effects.BeyBurstEffect>();

            burstEffect.TriggerBurst();
        }

        /// <summary>Called by MatchManager on match restart.</summary>
        public void ResetBey()
        {
            isBurst = false;
            transform.position = spawnPosition;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.constraints = RigidbodyConstraints.FreezeRotationX
                               | RigidbodyConstraints.FreezeRotationY
                               | RigidbodyConstraints.FreezeRotationZ;
                rb.linearDamping = 0.05f;
            }

            if (movementController != null) movementController.enabled = true;
            if (aiInput != null) aiInput.enabled = true;

            beyConfiguration?.SetSpin(GameConstants.DEFAULT_STARTING_SPIN);
            beyConfiguration?.SetMana(GameConstants.DEFAULT_MANA_POOL);
            gameObject.SetActive(true);
        }
    }
}
