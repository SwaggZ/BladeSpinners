using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "ArcaneNovaAbility", menuName = "Blade Spinners/Abilities/Arcane Nova")]
    public class ArcaneNovaAbility : BeyAbility
    {
        [Header("Arcane Nova")]
        [SerializeField] private float novaRadius = 10f;
        [SerializeField] private float innerDamage = 10f;
        [SerializeField] private float outerDamage = 30f;
        [SerializeField] private float knockbackImpulse = 16f;

        private void OnEnable()
        {
            abilityName = "Arcane Nova";
            description = "Unleashes an expanding ring of arcane energy — enemies at the edge take the most damage.";
            manaCost = 70f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null) return;

            Vector3 origin = beyController.transform.position;
            foreach (BeyMovementController bey in
                     AbilityTargetQuery.FindUniqueBeysInRadius(
                         beyController,
                         origin,
                         novaRadius,
                         AbilityTargetRelation.Enemy))
            {
                float dist = Vector3.Distance(origin, bey.transform.position);

                // Damage scales UP toward the edge
                float edgeFactor = dist / novaRadius;
                float dmg = Mathf.Lerp(innerDamage, outerDamage, edgeFactor);
                bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - dmg);

                Rigidbody rb = bey.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 dir = (bey.transform.position - origin);
                    dir.y = 0.15f;
                    rb.AddForce(dir.normalized * knockbackImpulse * edgeFactor, ForceMode.Impulse);
                }
            }

            EpicAbilityVFXHelper.SpawnArcaneNovaVFX(origin, novaRadius, new Color(0.7f, 0.25f, 1f, 1f), new Color(0.95f, 0.8f, 1f, 1f));
            Debug.Log("[Ability] Arcane Nova!");
        }
    }
}
