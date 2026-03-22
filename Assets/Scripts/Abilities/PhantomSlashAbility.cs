using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "PhantomSlashAbility", menuName = "Blade Spinners/Abilities/Phantom Slash")]
    public class PhantomSlashAbility : BeyAbility
    {
        [Header("Phantom Slash")]
        [SerializeField] private float dashDistance = 12f;
        [SerializeField] private float slashWidth = 2f;
        [SerializeField] private float slashDamage = 22f;
        [SerializeField] private float dashSpeed = 30f;

        private void OnEnable()
        {
            abilityName = "Phantom Slash";
            description = "Dashes forward in a blinding line, cutting through all enemies in the path.";
            manaCost = 60f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null) return;

            Vector3 forward = beyController.Rb != null && beyController.Rb.linearVelocity.sqrMagnitude > 0.5f
                ? beyController.Rb.linearVelocity.normalized : beyController.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 start = beyController.transform.position;
            Vector3 end = start + forward * dashDistance;

            // Damage all enemies in the line
            BeyConfiguration ownerConfig = beyController.BeyConfiguration;
            BeyMovementController[] beys = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);
            foreach (BeyMovementController bey in beys)
            {
                if (bey == null || bey.BeyConfiguration == null || bey.BeyConfiguration == ownerConfig) continue;
                if (bey.BeyConfiguration.IsEnemy == ownerConfig.IsEnemy) continue;
                Vector3 toEnemy = bey.transform.position - start;
                float dot = Vector3.Dot(toEnemy, forward);
                if (dot < 0f || dot > dashDistance) continue;
                Vector3 closest = start + forward * dot;
                float perpDist = Vector3.Distance(bey.transform.position, closest);
                if (perpDist > slashWidth) continue;
                bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - slashDamage);
                Rigidbody rb = bey.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 knockDir = (bey.transform.position - closest).normalized;
                    knockDir.y = 0.15f;
                    rb.AddForce(knockDir.normalized * 10f, ForceMode.Impulse);
                }
            }

            // Move bey to end
            if (Physics.Linecast(start + Vector3.up * 0.2f, end + Vector3.up * 0.2f, out RaycastHit hit))
                end = hit.point - forward * 0.75f;
            beyController.transform.position = end;
            if (beyController.Rb != null)
                beyController.Rb.linearVelocity = forward * dashSpeed * 0.3f;

            SpawnSlashTrail(start, end, forward);
            Debug.Log("[Ability] Phantom Slash!");
        }

        private void SpawnSlashTrail(Vector3 start, Vector3 end, Vector3 dir)
        {
            // Slash line
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "PhantomSlashLine";
            line.transform.position = (start + end) * 0.5f;
            float len = Vector3.Distance(start, end);
            line.transform.localScale = new Vector3(0.15f, 0.04f, len);
            line.transform.rotation = Quaternion.LookRotation(dir);
            Collider c = line.GetComponent<Collider>(); if (c != null) c.enabled = false;
            Renderer r = line.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.8f, 0.4f, 1f, 0.6f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(2f, 0.8f, 3f)); }
                r.material = mat;
            }
            Object.Destroy(line, 0.3f);

            // Afterimage at start
            GameObject ghost = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ghost.name = "PhantomGhost";
            ghost.transform.position = start;
            ghost.transform.localScale = Vector3.one * 0.8f;
            Collider gc = ghost.GetComponent<Collider>(); if (gc != null) gc.enabled = false;
            Renderer gr = ghost.GetComponent<Renderer>();
            if (gr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.6f, 0.3f, 0.9f, 0.3f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1f, 0.4f, 2f)); }
                gr.material = mat;
            }
            Object.Destroy(ghost, 0.4f);
        }
    }
}
