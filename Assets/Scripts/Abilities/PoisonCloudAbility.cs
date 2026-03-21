using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "PoisonCloudAbility", menuName = "Blade Spinners/Abilities/Poison Cloud")]
    public class PoisonCloudAbility : BeyAbility
    {
        [Header("Poison Cloud")]
        [SerializeField] private float radius = 5f;
        [SerializeField] private float duration = 4.5f;
        [SerializeField] private float damagePerSecond = 5f;

        private void OnEnable()
        {
            abilityName = "Poison Cloud";
            description = "Creates a toxic cloud that continuously drains enemy spin.";
            manaCost = 65f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            AbilityRuntimeEffects runtime = AbilityRuntimeEffects.GetOrCreate(beyController);
            if (runtime == null)
                return;

            runtime.SpawnPoisonCloud(radius, duration, damagePerSecond);
            Debug.Log("[Ability] Poison Cloud!");
        }
    }
}
