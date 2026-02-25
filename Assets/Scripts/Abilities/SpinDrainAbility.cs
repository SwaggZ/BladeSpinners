using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    /// <summary>
    /// Spin Drain ability: steals spin from nearby enemy beys within a radius.
    /// High mana cost, powerful effect.
    /// </summary>
    [CreateAssetMenu(fileName = "SpinDrainAbility", menuName = "Blade Spinners/Abilities/Spin Drain")]
    public class SpinDrainAbility : BeyAbility
    {
        [Header("Spin Drain Settings")]
        [SerializeField] private float drainRadius = 6f;
        [SerializeField] private float spinStolen = 20f;

        private void OnEnable()
        {
            abilityName = "Spin Drain";
            description = "Steals spin from all nearby enemies.";
            manaCost = 80f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;

            BeyConfiguration playerConfig = beyController.BeyConfiguration;
            if (playerConfig == null) return;

            Vector3 origin = beyController.transform.position;
            float totalStolen = 0f;

            // Find all enemy beys within radius
            var enemies = Object.FindObjectsByType<BladeSpinners.Gameplay.EnemyBeyController>(
                FindObjectsSortMode.None);

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.BeyConfiguration == null) continue;

                float dist = Vector3.Distance(origin, enemy.transform.position);
                if (dist > drainRadius) continue;

                // Steal spin (scaled by proximity)
                float proximityFactor = 1f - (dist / drainRadius);
                float stolen = spinStolen * proximityFactor;
                float actualStolen = Mathf.Min(stolen, enemy.BeyConfiguration.CurrentSpin);

                enemy.BeyConfiguration.SetSpin(enemy.BeyConfiguration.CurrentSpin - actualStolen);
                totalStolen += actualStolen;
            }

            // Give stolen spin to player
            if (totalStolen > 0f)
            {
                playerConfig.SetSpin(playerConfig.CurrentSpin + totalStolen);
                Debug.Log($"[Ability] Spin Drain stole {totalStolen:F1} total spin!");
            }
            else
            {
                Debug.Log("[Ability] Spin Drain — no enemies in range.");
            }
        }
    }
}
