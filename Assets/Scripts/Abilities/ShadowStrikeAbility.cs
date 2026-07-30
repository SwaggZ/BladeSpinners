using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "ShadowStrikeAbility", menuName = "Blade Spinners/Abilities/Shadow Strike")]
    public class ShadowStrikeAbility : BeyAbility
    {
        [Header("Shadow Strike")]
        [SerializeField] private float searchRadius = 14f;
        [SerializeField] private float strikeDamage = 28f;
        [SerializeField] private float knockbackImpulse = 15f;

        private void OnEnable()
        {
            abilityName = "Shadow Strike";
            description = "Phase through shadow to appear beside the nearest enemy and deliver a devastating blow.";
            manaCost = 75f;
            rarity = Core.AbilityRarity.Legendary;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null) return;

            BeyMovementController target = AbilityTargetQuery.FindNearest(
                beyController,
                beyController.transform.position,
                searchRadius,
                AbilityTargetRelation.Enemy);
            if (target == null) return;

            Vector3 startPos = beyController.transform.position;
            Vector3 offset = (beyController.transform.position - target.transform.position).normalized * 1.5f;
            Vector3 strikePos = target.transform.position + offset;

            // Afterimage at start
            SpawnShadowGhost(startPos, beyController.transform.localScale);

            // Teleport
            beyController.transform.position = strikePos;
            if (beyController.Rb != null)
                beyController.Rb.linearVelocity = -offset.normalized * 10f;

            // Damage
            target.BeyConfiguration.SetSpin(target.BeyConfiguration.CurrentSpin - strikeDamage);
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                Vector3 dir = (target.transform.position - strikePos).normalized;
                dir.y = 0.1f;
                targetRb.AddForce(dir.normalized * knockbackImpulse, ForceMode.Impulse);
            }

            // Impact slash visual
            SpawnSlashVisual(target.transform.position);
            Debug.Log("[Ability] Shadow Strike!");
        }

        private void SpawnShadowGhost(Vector3 pos, Vector3 scale)
        {
            GameObject ghost = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ghost.name = "ShadowGhost";
            ghost.transform.position = pos;
            ghost.transform.localScale = scale;
            Collider c = ghost.GetComponent<Collider>(); if (c != null) c.enabled = false;
            Renderer r = ghost.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.1f, 0f, 0.15f, 0.5f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.3f, 0f, 0.5f)); }
                r.material = mat;
            }
            Object.Destroy(ghost, 0.5f);
        }

        private void SpawnSlashVisual(Vector3 pos)
        {
            // Dark slash crosses
            for (int i = 0; i < 2; i++)
            {
                GameObject slash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slash.name = "ShadowSlash";
                slash.transform.position = pos;
                slash.transform.localScale = new Vector3(2f, 0.05f, 0.08f);
                slash.transform.rotation = Quaternion.Euler(0f, i * 90f + 45f, 15f);
                Collider c = slash.GetComponent<Collider>(); if (c != null) c.enabled = false;
                Renderer r = slash.GetComponent<Renderer>();
                if (r != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                    mat.color = new Color(0.3f, 0f, 0.5f, 0.8f);
                    if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.6f, 0f, 2f)); }
                    r.material = mat;
                }
                Object.Destroy(slash, 0.25f);
            }
        }
    }
}
