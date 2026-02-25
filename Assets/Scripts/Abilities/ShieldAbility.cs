using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    /// <summary>
    /// Shield ability: temporarily reduces incoming spin damage and increases knockback resistance.
    /// Works by boosting the bey's effective weight for a few seconds.
    /// </summary>
    [CreateAssetMenu(fileName = "ShieldAbility", menuName = "Blade Spinners/Abilities/Shield")]
    public class ShieldAbility : BeyAbility
    {
        [Header("Shield Settings")]
        [SerializeField] private float duration = 3f;
        [SerializeField] private float weightBoost = 30f; // added to effective weight

        private BeyMovementController activeController;
        private float remainingTime;
        private bool isActive;

        private void OnEnable()
        {
            abilityName = "Shield";
            description = "Temporarily increases weight, reducing spin damage taken.";
            manaCost = 50f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;

            activeController = beyController;
            remainingTime = duration;
            isActive = true;

            // Boost the Rigidbody mass to simulate increased weight
            if (beyController.Rb != null)
                beyController.Rb.mass += weightBoost * 0.1f; // scale down to RB mass units

            Debug.Log($"[Ability] Shield activated! +{weightBoost} effective weight for {duration}s");
        }

        public override void Update()
        {
            if (!isActive) return;

            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0f)
            {
                Deactivate();
            }
        }

        public override void Deactivate()
        {
            if (!isActive) return;
            isActive = false;

            // Revert the mass boost
            if (activeController != null && activeController.Rb != null)
                activeController.Rb.mass -= weightBoost * 0.1f;

            activeController = null;
            Debug.Log("[Ability] Shield expired.");
        }
    }
}
