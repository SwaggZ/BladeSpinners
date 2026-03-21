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

            AbilityRuntimeEffects runtime = AbilityRuntimeEffects.GetOrCreate(beyController);
            if (runtime == null)
                return;

            runtime.ApplyTempMassBoost(weightBoost * 0.1f, duration);

            Debug.Log($"[Ability] Shield activated! +{weightBoost} effective weight for {duration}s");
        }
    }
}
