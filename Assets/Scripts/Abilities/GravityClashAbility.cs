using System.Collections.Generic;
using UnityEngine;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "GravityClashAbility", menuName = "Blade Spinners/Abilities/Gravity Clash")]
    public class GravityClashAbility : BeyAbility
    {
        [Header("Gravity Clash")]
        [SerializeField] private float searchRadius = 12f;
        [SerializeField] private float pullImpulse = 20f;
        [SerializeField] private float impactSpinDamage = 14f;

        private void OnEnable()
        {
            abilityName = "Gravity Clash";
            description = "Pulls nearby enemies toward each other and forces a brutal collision.";
            manaCost = 75f;
            rarity = Core.AbilityRarity.Legendary;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null)
                return;

            EnemyBeyController[] enemies = Object.FindObjectsByType<EnemyBeyController>(FindObjectsSortMode.None);
            List<EnemyBeyController> candidates = new List<EnemyBeyController>();

            foreach (EnemyBeyController enemy in enemies)
            {
                if (enemy == null || enemy.BeyConfiguration == null)
                    continue;

                Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
                if (enemyRb == null)
                    continue;

                float dist = Vector3.Distance(beyController.transform.position, enemy.transform.position);
                if (dist <= searchRadius)
                    candidates.Add(enemy);
            }

            if (candidates.Count < 2)
            {
                Debug.Log("[Ability] Gravity Clash needs at least 2 enemies in range.");
                return;
            }

            EnemyBeyController first = null;
            EnemyBeyController second = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                for (int j = i + 1; j < candidates.Count; j++)
                {
                    float pairDist = Vector3.Distance(candidates[i].transform.position, candidates[j].transform.position);
                    if (pairDist < bestDist)
                    {
                        bestDist = pairDist;
                        first = candidates[i];
                        second = candidates[j];
                    }
                }
            }

            if (first == null || second == null)
                return;

            Vector3 midpoint = (first.transform.position + second.transform.position) * 0.5f;
            Vector3 firstDir = (midpoint - first.transform.position).normalized;
            Vector3 secondDir = (midpoint - second.transform.position).normalized;

            Rigidbody firstRb = first.GetComponent<Rigidbody>();
            Rigidbody secondRb = second.GetComponent<Rigidbody>();
            if (firstRb == null || secondRb == null)
                return;

            firstRb.AddForce(firstDir * pullImpulse, ForceMode.VelocityChange);
            secondRb.AddForce(secondDir * pullImpulse, ForceMode.VelocityChange);

            first.BeyConfiguration.SetSpin(first.BeyConfiguration.CurrentSpin - impactSpinDamage);
            second.BeyConfiguration.SetSpin(second.BeyConfiguration.CurrentSpin - impactSpinDamage);

            Debug.Log("[Ability] Gravity Clash!");
        }
    }
}
