using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "FlashStepAbility", menuName = "Blade Spinners/Abilities/Flash Step")]
    public class FlashStepAbility : BeyAbility
    {
        [Header("Flash Step")]
        [SerializeField] private float stepDistance = 7f;
        [SerializeField] private float endImpulse = 8f;

        private void OnEnable()
        {
            abilityName = "Flash Step";
            description = "Instantly step forward and keep momentum.";
            manaCost = 40f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null)
                return;

            Vector3 direction = beyController.transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector3.forward;
            direction.Normalize();

            Vector3 start = beyController.transform.position;
            Vector3 target = start + direction * stepDistance;

            if (Physics.Linecast(start + Vector3.up * 0.2f, target + Vector3.up * 0.2f, out RaycastHit hit))
            {
                target = hit.point - direction * 0.75f;
            }

            beyController.transform.position = target;
            if (beyController.Rb != null)
            {
                beyController.Rb.linearVelocity = new Vector3(0f, beyController.Rb.linearVelocity.y, 0f);
                beyController.Rb.AddForce(direction * endImpulse, ForceMode.VelocityChange);
            }

            Debug.Log("[Ability] Flash Step!");
        }
    }
}
