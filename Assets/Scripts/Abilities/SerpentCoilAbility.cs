using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "SerpentCoilAbility", menuName = "Blade Spinners/Abilities/Serpent Coil")]
    public class SerpentCoilAbility : BeyAbility
    {
        [Header("Serpent Coil")]
        [SerializeField] private float pullRange = 9f;
        [SerializeField] private float pullImpulse = 14f;
        [SerializeField] private float immobilizeDuration = 2f;
        [SerializeField] private float spinDamage = 15f;

        private void OnEnable()
        {
            abilityName = "Serpent Coil";
            description = "Lashes out with a serpentine force, dragging the nearest enemy close and coiling them in place.";
            manaCost = 55f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null)
                return;

            BeyMovementController target = AbilityTargetQuery.FindNearest(
                beyController,
                beyController.transform.position,
                pullRange,
                AbilityTargetRelation.Enemy);
            if (target == null)
            {
                Debug.Log("[Ability] Serpent Coil: no target found.");
                return;
            }

            // Pull the target toward the caster
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                Vector3 pullDir = (beyController.transform.position - target.transform.position).normalized;
                targetRb.AddForce(pullDir * pullImpulse, ForceMode.VelocityChange);
            }

            // Deal spin damage
            target.BeyConfiguration.SetSpin(target.BeyConfiguration.CurrentSpin - spinDamage);

            // Immobilize (freeze)
            FreezeRuntime.Apply(target, immobilizeDuration);

            SpawnVisual(beyController.transform.position, target.transform.position);
            Debug.Log("[Ability] Serpent Coil!");
        }

        private void SpawnVisual(Vector3 from, Vector3 to)
        {
            // Green serpentine "beam" between caster and target
            GameObject beam = new GameObject("SerpentBeam");
            beam.transform.position = (from + to) * 0.5f;

            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.transform.SetParent(beam.transform, false);
            float dist = Vector3.Distance(from, to);
            line.transform.localScale = new Vector3(0.08f, 0.08f, dist);
            line.transform.LookAt(to);

            Collider col = line.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Renderer rend = line.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.1f, 0.9f, 0.2f);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.1f, 2f, 0.1f));
                }
                rend.material = mat;
            }

            Object.Destroy(beam, 0.4f);
        }
    }
}
