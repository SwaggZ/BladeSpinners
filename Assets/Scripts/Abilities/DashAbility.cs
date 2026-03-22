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
            SpawnDashTrail(beyController.transform.position, dir);
            Debug.Log("[Ability] Dash!");
        }

        private void SpawnDashTrail(Vector3 origin, Vector3 direction)
        {
            // Speed streaks behind the bey
            for (int i = 0; i < 5; i++)
            {
                GameObject streak = GameObject.CreatePrimitive(PrimitiveType.Cube);
                streak.name = "DashStreak";
                float offset = i * 0.35f;
                streak.transform.position = origin - direction * offset + new Vector3(Random.Range(-0.15f, 0.15f), 0.1f, Random.Range(-0.15f, 0.15f));
                streak.transform.localScale = new Vector3(0.08f, 0.08f, 0.6f - i * 0.08f);
                streak.transform.rotation = Quaternion.LookRotation(direction);
                Collider col = streak.GetComponent<Collider>();
                if (col != null) col.enabled = false;
                Renderer rend = streak.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                    mat.color = new Color(0.3f, 0.8f, 1f, 0.5f - i * 0.08f);
                    if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.5f, 1.5f, 2.5f)); }
                    rend.material = mat;
                }
                Object.Destroy(streak, 0.25f + i * 0.05f);
            }

            // Burst flash at origin
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "DashFlash";
            flash.transform.position = origin;
            flash.transform.localScale = Vector3.one * 0.6f;
            Collider fc = flash.GetComponent<Collider>();
            if (fc != null) fc.enabled = false;
            Renderer fr = flash.GetComponent<Renderer>();
            if (fr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.6f, 0.9f, 1f, 0.5f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1f, 2f, 3f)); }
                fr.material = mat;
            }
            Object.Destroy(flash, 0.2f);
        }
    }
}
