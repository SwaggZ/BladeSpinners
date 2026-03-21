using UnityEngine;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "DragonBurstAbility", menuName = "Blade Spinners/Abilities/Dragon Burst")]
    public class DragonBurstAbility : BeyAbility
    {
        [Header("Dragon Burst")]
        [SerializeField] private float surgeImpulse = 18f;
        [SerializeField] private float coneRange = 10f;
        [SerializeField] private float coneHalfAngle = 28f;
        [SerializeField] private float spinDamage = 24f;
        [SerializeField] private float knockbackImpulse = 14f;

        private void OnEnable()
        {
            abilityName = "Dragon Burst";
            description = "Legendary surge attack that blasts enemies in a forward cone.";
            manaCost = 95f;
            rarity = Core.AbilityRarity.Legendary;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null)
                return;

            Vector3 origin = beyController.transform.position;
            Vector3 forward = beyController.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            if (beyController.Rb != null)
            {
                beyController.Rb.AddForce(forward * surgeImpulse, ForceMode.VelocityChange);
            }

            BeyConfiguration ownerConfig = beyController.BeyConfiguration;
            EnemyBeyController[] enemies = Object.FindObjectsByType<EnemyBeyController>(FindObjectsSortMode.None);
            foreach (EnemyBeyController enemy in enemies)
            {
                if (enemy == null || enemy.BeyConfiguration == null)
                    continue;

                Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
                if (enemyRb == null)
                    continue;

                if (enemy.BeyConfiguration == ownerConfig)
                    continue;

                Vector3 toEnemy = enemy.transform.position - origin;
                float dist = toEnemy.magnitude;
                if (dist > coneRange || dist < 0.01f)
                    continue;

                Vector3 flatDir = toEnemy;
                flatDir.y = 0f;
                if (flatDir.sqrMagnitude < 0.001f)
                    continue;

                float angle = Vector3.Angle(forward, flatDir.normalized);
                if (angle > coneHalfAngle)
                    continue;

                float rangeFalloff = 1f - (dist / coneRange);
                float angleFalloff = 1f - (angle / coneHalfAngle);
                float totalFalloff = Mathf.Clamp01(rangeFalloff * angleFalloff);

                enemy.BeyConfiguration.SetSpin(enemy.BeyConfiguration.CurrentSpin - spinDamage * totalFalloff);
                Vector3 knockDir = flatDir.normalized + Vector3.up * 0.08f;
                enemyRb.AddForce(knockDir.normalized * knockbackImpulse * Mathf.Lerp(0.55f, 1f, totalFalloff), ForceMode.Impulse);
            }

            Debug.Log("[Ability] Dragon Burst!");
        }
    }
}
