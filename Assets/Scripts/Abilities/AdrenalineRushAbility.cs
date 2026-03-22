using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "AdrenalineRushAbility", menuName = "Blade Spinners/Abilities/Adrenaline Rush")]
    public class AdrenalineRushAbility : BeyAbility
    {
        [Header("Adrenaline Rush")]
        [SerializeField] private float speedMultiplier = 1.6f;
        [SerializeField] private float massDelta = -0.3f;
        [SerializeField] private float duration = 4f;
        [SerializeField] private float contactDamageBonus = 8f;

        private void OnEnable()
        {
            abilityName = "Adrenaline Rush";
            description = "Adrenaline surges through your core — move faster and hit harder on contact.";
            manaCost = 45f;
            rarity = Core.AbilityRarity.Common;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;
            AbilityRuntimeEffects fx = AbilityRuntimeEffects.GetOrCreate(beyController);
            if (fx != null) fx.ApplyTempMassBoost(massDelta, duration);
            if (beyController.Rb != null) beyController.Rb.linearVelocity *= speedMultiplier;

            DBZAuraHelper.Spawn(
                beyController.transform, duration,
                new Color(0.5f, 1f, 0.2f),   // green-yellow core
                new Color(0.8f, 1f, 0.3f),    // light green outer
                2.5f
            );
            Debug.Log("[Ability] Adrenaline Rush!");
        }
    }
}
