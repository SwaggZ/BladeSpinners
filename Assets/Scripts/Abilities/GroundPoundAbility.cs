using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "GroundPoundAbility", menuName = "Blade Spinners/Abilities/Ground Pound")]
    public class GroundPoundAbility : BeyAbility
    {
        [Header("Ground Pound")]
        [SerializeField] private float slamForce = 26f;
        [SerializeField] private float impactRadius = 6f;
        [SerializeField] private float spinDamage = 16f;
        [SerializeField] private float knockbackImpulse = 10f;

        private void OnEnable()
        {
            abilityName = "Ground Pound";
            description = "Slam downward and shock nearby enemies with spin damage and knockback.";
            manaCost = 45f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.Rb == null || beyController.BeyConfiguration == null)
                return;

            Rigidbody rb = beyController.Rb;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, 0f), rb.linearVelocity.z);
            rb.AddForce(Vector3.down * slamForce, ForceMode.VelocityChange);

            Vector3 origin = beyController.transform.position;
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
                if (dist > impactRadius)
                    continue;

                float falloff = 1f - (dist / impactRadius);
                enemy.BeyConfiguration.SetSpin(enemy.BeyConfiguration.CurrentSpin - spinDamage * falloff);

                Vector3 dir = dist > 0.01f ? toEnemy.normalized : Vector3.forward;
                dir.y = 0.15f;
                enemyRb.AddForce(dir.normalized * knockbackImpulse * Mathf.Lerp(0.5f, 1f, falloff), ForceMode.Impulse);
            }

            Debug.Log("[Ability] Ground Pound!");
        }
    }
}
