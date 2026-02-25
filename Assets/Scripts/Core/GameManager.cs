using UnityEngine;

namespace BladeSpinners.Core
{
    /// <summary>
    /// Global balance manager. Every multiplier starts at 100% (1.0).
    /// Tweak sliders in the Inspector at runtime to rebalance on the fly.
    /// All gameplay systems read from GameManager.Instance.XxxMultiplier.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Ensure bey-vs-bey physical collision is disabled at runtime.
            // Trigger colliders (spin exchange) still fire.
            int beyLayer = LayerMask.NameToLayer("Bey");
            if (beyLayer >= 0)
                Physics.IgnoreLayerCollision(beyLayer, beyLayer, true);
        }

        // ══════════════════════════════════════════════════════════════
        //  BALANCE MULTIPLIERS  (100% = 1.0, shown as 0–300% sliders)
        // ══════════════════════════════════════════════════════════════

        [Header("Movement")]
        [Tooltip("Scales BASE_FORWARD_FORCE — higher = faster top speed")]
        [Range(0f, 3f)] public float speedMultiplier = 1f;

        [Tooltip("Scales momentum build rate — higher = reaches top speed quicker")]
        [Range(0f, 3f)] public float accelerationMultiplier = 1f;

        [Tooltip("Scales turn speed (degrees/sec)")]
        [Range(0f, 3f)] public float turnSpeedMultiplier = 1f;

        [Tooltip("Scales jump force")]
        [Range(0f, 3f)] public float jumpMultiplier = 1f;

        [Tooltip("Scales boost force multiplier")]
        [Range(0f, 3f)] public float boostMultiplier = 1f;

        [Header("Combat")]
        [Tooltip("Scales collision knockback impulse")]
        [Range(0f, 3f)] public float knockbackMultiplier = 1f;

        [Tooltip("Scales spin damage exchanged on collision")]
        [Range(0f, 3f)] public float spinExchangeMultiplier = 0.7f;

        [Header("Stamina / Spin")]
        [Tooltip("Scales spin drain rate — higher = beys die faster")]
        [Range(0f, 3f)] public float spinDrainMultiplier = 0.5f;

        [Tooltip("Scales starting spin value")]
        [Range(0f, 3f)] public float startingSpinMultiplier = 1.5f;

        [Header("Mana")]
        [Tooltip("Scales mana regen rate")]
        [Range(0f, 3f)] public float manaRegenMultiplier = 1f;

        [Tooltip("Scales mana pool size")]
        [Range(0f, 3f)] public float manaPoolMultiplier = 1f;

        [Tooltip("Scales ability mana cost — higher = abilities cost more")]
        [Range(0f, 3f)] public float abilityCostMultiplier = 1f;

        [Header("Visual")]
        [Tooltip("Scales visual spin speed")]
        [Range(0f, 3f)] public float visualSpinMultiplier = 1f;

        // ══════════════════════════════════════════════════════════════
        //  ENEMY-SPECIFIC MULTIPLIERS  (stack on top of global ones)
        //  Final enemy value = global × enemy
        // ══════════════════════════════════════════════════════════════

        [Header("Enemy — Movement")]
        [Tooltip("Stacks with Speed: enemy speed = Speed × Enemy Speed")]
        [Range(0f, 3f)] public float enemySpeedMultiplier = 0.4f;

        [Tooltip("Stacks with Acceleration")]
        [Range(0f, 3f)] public float enemyAccelerationMultiplier = 0.35f;

        [Tooltip("Stacks with Turn Speed")]
        [Range(0f, 3f)] public float enemyTurnSpeedMultiplier = 1f;

        [Tooltip("Stacks with Jump")]
        [Range(0f, 3f)] public float enemyJumpMultiplier = 1f;

        [Tooltip("Stacks with Boost")]
        [Range(0f, 3f)] public float enemyBoostMultiplier = 0.3f;

        [Header("Enemy — Combat")]
        [Tooltip("Stacks with Knockback")]
        [Range(0f, 3f)] public float enemyKnockbackMultiplier = 0.7f;

        [Tooltip("Stacks with Spin Exchange")]
        [Range(0f, 3f)] public float enemySpinExchangeMultiplier = 0.6f;

        [Header("Enemy — Stamina / Spin")]
        [Tooltip("Stacks with Spin Drain")]
        [Range(0f, 3f)] public float enemySpinDrainMultiplier = 1.5f;

        [Tooltip("Stacks with Starting Spin")]
        [Range(0f, 3f)] public float enemyStartingSpinMultiplier = 1f;

        [Header("Enemy — Mana")]
        [Tooltip("Stacks with Mana Regen")]
        [Range(0f, 3f)] public float enemyManaRegenMultiplier = 0.75f;

        [Tooltip("Stacks with Mana Pool")]
        [Range(0f, 3f)] public float enemyManaPoolMultiplier = 0.75f;

        [Tooltip("Stacks with Ability Cost")]
        [Range(0f, 3f)] public float enemyAbilityCostMultiplier = 1.5f;

        [Header("Enemy — Visual")]
        [Tooltip("Stacks with Visual Spin")]
        [Range(0f, 3f)] public float enemyVisualSpinMultiplier = 1f;

        // ══════════════════════════════════════════════════════════════
        //  HELPERS — safe access even if no instance exists
        // ══════════════════════════════════════════════════════════════

        /// <summary>Returns the global multiplier, or 1.0 if GameManager doesn't exist.</summary>
        public static float Get(System.Func<GameManager, float> selector)
        {
            return Instance != null ? selector(Instance) : 1f;
        }

        /// <summary>
        /// Returns global × enemy multiplier for a given pair.
        /// Usage: GameManager.GetForBey(isEnemy, g => g.speedMultiplier, g => g.enemySpeedMultiplier)
        /// </summary>
        public static float GetForBey(bool isEnemy,
            System.Func<GameManager, float> globalSelector,
            System.Func<GameManager, float> enemySelector)
        {
            if (Instance == null) return 1f;
            float value = globalSelector(Instance);
            if (isEnemy) value *= enemySelector(Instance);
            return value;
        }
    }
}
