using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "VoidPulseAbility", menuName = "Blade Spinners/Abilities/Void Pulse")]
    public class VoidPulseAbility : BeyAbility
    {
        [Header("Void Pulse")]
        [SerializeField] private float pulseRadius = 8f;
        [SerializeField] private float pushForce = 24f;
        [SerializeField] private float spinDamage = 15f;

        private void OnEnable()
        {
            abilityName = "Void Pulse";
            description = "Emits a void-energy shockwave that blasts all enemies away.";
            manaCost = 55f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null) return;

            Vector3 origin = beyController.transform.position;
            foreach (BeyMovementController bey in
                     AbilityTargetQuery.FindUniqueBeysInRadius(
                         beyController,
                         origin,
                         pulseRadius,
                         AbilityTargetRelation.Enemy))
            {
                float dist = Vector3.Distance(origin, bey.transform.position);
                float falloff = 1f - (dist / pulseRadius);
                bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - spinDamage * falloff);
                Rigidbody rb = bey.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 dir = (bey.transform.position - origin);
                    dir.y = 0.2f;
                    rb.AddForce(dir.normalized * pushForce * falloff, ForceMode.Impulse);
                }
            }

            // Dark void ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "VoidRing";
            ring.transform.position = origin;
            ring.transform.localScale = new Vector3(0.5f, 0.06f, 0.5f);
            Collider rc = ring.GetComponent<Collider>(); if (rc != null) rc.enabled = false;
            Renderer rr = ring.GetComponent<Renderer>();
            if (rr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.15f, 0f, 0.3f, 0.6f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.4f, 0f, 1.5f)); }
                rr.material = mat;
            }
            WaveExpandRuntime.Spawn(ring, pulseRadius, 0.3f);

            // Dark core implode-explode
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "VoidCore";
            core.transform.position = origin;
            core.transform.localScale = Vector3.one * 1.5f;
            Collider cc = core.GetComponent<Collider>(); if (cc != null) cc.enabled = false;
            Renderer cr = core.GetComponent<Renderer>();
            if (cr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.05f, 0f, 0.1f, 0.7f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.2f, 0f, 0.8f)); }
                cr.material = mat;
            }
            Object.Destroy(core, 0.25f);
            Debug.Log("[Ability] Void Pulse!");
        }
    }
}
