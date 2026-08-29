using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    /// <summary>
    /// Dash ability: instant burst of speed in the current movement direction.
    /// Low mana cost, short cooldown feel via mana gating.
    /// </summary>
    [CreateAssetMenu(fileName = "DashAbility", menuName = "Blade Spinners/Abilities/Dash")]
    public class DashAbility : BeyAbility
    {
        [Header("Dash Settings")]
        [SerializeField] private float dashForce = 40f;

        private void OnEnable()
        {
            abilityName = "Dash";
            description = "Instant burst of speed in your current direction.";
            manaCost = 25f;
            rarity = Core.AbilityRarity.Common;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.Rb == null) return;

            // Dash in the direction the bey is currently moving,
            // or forward from camera if standing still
            Vector3 vel = beyController.Rb.linearVelocity;
            Vector3 dir = new Vector3(vel.x, 0, vel.z);

            if (dir.sqrMagnitude < 0.5f)
            {
                // Use camera forward if barely moving
                Camera cam = Camera.main;
                if (cam != null)
                {
                    dir = cam.transform.forward;
                    dir.y = 0f;
                }
            }

            dir.Normalize();
            beyController.Rb.AddForce(dir * dashForce, ForceMode.Impulse);
            EpicAbilityVFXHelper.SpawnDashSlashVFX(beyController.transform.position, dir, new Color(0.25f, 0.85f, 1f, 1f), 1.2f);
            Debug.Log("[Ability] Dash!");
        }
    }
}
