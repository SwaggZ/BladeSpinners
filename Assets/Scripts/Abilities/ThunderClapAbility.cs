using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "ThunderClapAbility", menuName = "Blade Spinners/Abilities/Thunder Clap")]
    public class ThunderClapAbility : BeyAbility
    {
        [Header("Thunder Clap")]
        [SerializeField] private float radius = 8f;
        [SerializeField] private float damage = 20f;
        [SerializeField] private float stunDuration = 1.2f;

        private void OnEnable()
        {
            abilityName = "Thunder Clap";
            description = "Cracks the sky with a deafening thunderclap that stuns and damages all nearby enemies.";
            manaCost = 65f;
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
                if (dist > radius) continue;
                float falloff = 1f - (dist / radius);
                bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - damage * falloff);
                StunRuntime.Apply(bey, stunDuration * falloff);
            }

            SpawnThunderVisual(origin, radius);
            Debug.Log("[Ability] Thunder Clap!");
        }

        private void SpawnThunderVisual(Vector3 center, float r)
        {
            // Bright white flash
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "ThunderFlash";
            flash.transform.position = center + Vector3.up * 0.3f;
            flash.transform.localScale = Vector3.one * 3f;
            Collider fc = flash.GetComponent<Collider>(); if (fc != null) fc.enabled = false;
            Renderer fr = flash.GetComponent<Renderer>();
            if (fr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.9f, 0.95f, 1f, 0.7f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(5f, 5f, 6f)); }
                fr.material = mat;
            }
            Object.Destroy(flash, 0.12f);

            // Electric shock ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ThunderRing";
            ring.transform.position = center;
            ring.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);
            Collider rc = ring.GetComponent<Collider>(); if (rc != null) rc.enabled = false;
            Renderer rr = ring.GetComponent<Renderer>();
            if (rr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.5f, 0.7f, 1f, 0.5f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1f, 2f, 4f)); }
                rr.material = mat;
            }
            WaveExpandRuntime.Spawn(ring, r, 0.25f);

            // Lightning bolts from sky
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * r * 0.3f, 0f, Mathf.Sin(angle) * r * 0.3f);
                GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bolt.name = "ThunderBolt";
                bolt.transform.position = center + offset + Vector3.up * 3f;
                bolt.transform.localScale = new Vector3(0.06f, 6f, 0.06f);
                bolt.transform.rotation = Quaternion.Euler(Random.Range(-8f, 8f), 0f, Random.Range(-8f, 8f));
                Collider bc = bolt.GetComponent<Collider>(); if (bc != null) bc.enabled = false;
                Renderer br = bolt.GetComponent<Renderer>();
                if (br != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                    mat.color = new Color(0.7f, 0.8f, 1f);
                    if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(2f, 3f, 6f)); }
                    br.material = mat;
                }
                Object.Destroy(bolt, 0.15f);
            }
        }
    }
}
