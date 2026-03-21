using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "SolarFlareAbility", menuName = "Blade Spinners/Abilities/Solar Flare")]
    public class SolarFlareAbility : BeyAbility
    {
        [Header("Solar Flare")]
        [SerializeField] private float explosionRadius = 8f;
        [SerializeField] private float centerDamage = 35f;
        [SerializeField] private float knockbackImpulse = 22f;
        [SerializeField] private float blindDuration = 1.5f;

        private void OnEnable()
        {
            abilityName = "Solar Flare";
            description = "Detonates a brilliant explosion of solar energy, dealing heavy damage and knocking enemies back.";
            manaCost = 80f;
            rarity = Core.AbilityRarity.Legendary;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null)
                return;

            Vector3 origin = beyController.transform.position;
            BeyConfiguration ownerConfig = beyController.BeyConfiguration;
            BeyMovementController[] beys = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);

            foreach (BeyMovementController bey in beys)
            {
                if (bey == null || bey.BeyConfiguration == null || bey.BeyConfiguration == ownerConfig) continue;
                if (bey.BeyConfiguration.IsEnemy == ownerConfig.IsEnemy) continue;

                Vector3 toEnemy = bey.transform.position - origin;
                float dist = toEnemy.magnitude;
                if (dist > explosionRadius) continue;

                float falloff = 1f - (dist / explosionRadius);
                bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - centerDamage * falloff);

                Rigidbody enemyRb = bey.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    Vector3 dir = dist > 0.01f ? toEnemy.normalized : Vector3.forward;
                    dir.y = Mathf.Max(dir.y, 0.2f);
                    enemyRb.AddForce(dir.normalized * knockbackImpulse * falloff, ForceMode.Impulse);
                }
            }

            SpawnExplosionVisual(origin, explosionRadius);
            Debug.Log("[Ability] Solar Flare!");
        }

        private void SpawnExplosionVisual(Vector3 center, float r)
        {
            // Inner bright core
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "SolarCore";
            core.transform.position = center;
            core.transform.localScale = Vector3.one * 0.5f;

            Collider col = core.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Renderer coreRend = core.GetComponent<Renderer>();
            if (coreRend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(1f, 0.95f, 0.3f);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(4f, 3f, 0.5f));
                }
                coreRend.material = mat;
            }

            // Outer expanding shockwave ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ring.name = "SolarShockwave";
            ring.transform.position = center;
            ring.transform.localScale = new Vector3(0.5f, 0.12f, 0.5f);

            Collider ringCol = ring.GetComponent<Collider>();
            if (ringCol != null) ringCol.enabled = false;

            Renderer ringRend = ring.GetComponent<Renderer>();
            if (ringRend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(1f, 0.6f, 0f, 0.6f);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(3f, 1.5f, 0f));
                }
                ringRend.material = mat;
            }

            WaveExpandRuntime.Spawn(ring, r, 0.5f);
            Object.Destroy(core, 0.5f);
        }
    }
}
