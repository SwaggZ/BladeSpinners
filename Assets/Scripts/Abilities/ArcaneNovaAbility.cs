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
            BeyConfiguration ownerConfig = beyController.BeyConfiguration;
            BeyMovementController[] beys = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);

            foreach (BeyMovementController bey in beys)
            {
                if (bey == null || bey.BeyConfiguration == null || bey.BeyConfiguration == ownerConfig) continue;
                if (bey.BeyConfiguration.IsEnemy == ownerConfig.IsEnemy) continue;
                float dist = Vector3.Distance(origin, bey.transform.position);
                if (dist > novaRadius) continue;

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

            SpawnNovaVisual(origin, novaRadius);
            Debug.Log("[Ability] Arcane Nova!");
        }

        private void SpawnNovaVisual(Vector3 center, float r)
        {
            // Purple expanding ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ArcaneRing";
            ring.transform.position = center;
            ring.transform.localScale = new Vector3(0.5f, 0.06f, 0.5f);
            Collider rc = ring.GetComponent<Collider>(); if (rc != null) rc.enabled = false;
            Renderer rr = ring.GetComponent<Renderer>();
            if (rr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.6f, 0.2f, 1f, 0.5f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1.5f, 0.4f, 3.5f)); }
                rr.material = mat;
            }
            WaveExpandRuntime.Spawn(ring, r, 0.45f);

            // Arcane rune particles
            for (int i = 0; i < 8; i++)
            {
                GameObject rune = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rune.name = "ArcaneRune";
                float angle = i * 45f * Mathf.Deg2Rad;
                rune.transform.position = center + new Vector3(Mathf.Cos(angle), 0.1f, Mathf.Sin(angle)) * (r * 0.3f);
                rune.transform.localScale = new Vector3(0.2f, 0.02f, 0.2f);
                rune.transform.rotation = Quaternion.Euler(0f, i * 45f, 0f);
                Collider c = rune.GetComponent<Collider>(); if (c != null) c.enabled = false;
                Renderer re = rune.GetComponent<Renderer>();
                if (re != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                    mat.color = new Color(0.8f, 0.4f, 1f, 0.7f);
                    if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(2f, 0.8f, 4f)); }
                    re.material = mat;
                }
                Object.Destroy(rune, 0.5f);
            }

            // Core flash
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "ArcaneCore";
            core.transform.position = center;
            core.transform.localScale = Vector3.one * 1f;
            Collider cc = core.GetComponent<Collider>(); if (cc != null) cc.enabled = false;
            Renderer cr = core.GetComponent<Renderer>();
            if (cr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.7f, 0.3f, 1f, 0.6f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(3f, 1f, 5f)); }
                cr.material = mat;
            }
            Object.Destroy(core, 0.3f);
        }
    }
}
