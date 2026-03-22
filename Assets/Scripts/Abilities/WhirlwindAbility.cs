using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "WhirlwindAbility", menuName = "Blade Spinners/Abilities/Whirlwind")]
    public class WhirlwindAbility : BeyAbility
    {
        [Header("Whirlwind")]
        [SerializeField] private float vortexRadius = 7f;
        [SerializeField] private float pullForce = 8f;
        [SerializeField] private float damagePerSecond = 6f;
        [SerializeField] private float duration = 3.5f;

        private void OnEnable()
        {
            abilityName = "Whirlwind";
            description = "Creates a raging vortex that pulls enemies in and shreds their spin.";
            manaCost = 70f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null) return;
            WhirlwindRuntime.Spawn(beyController, vortexRadius, pullForce, damagePerSecond, duration);
        }
    }

    public class WhirlwindRuntime : MonoBehaviour
    {
        private BeyConfiguration ownerConfig;
        private float radius, pull, dps, timer;
        private float tickTimer;
        private GameObject[] visualRings;

        public static void Spawn(BeyMovementController ctrl, float r, float p, float d, float dur)
        {
            GameObject vortex = new GameObject("Whirlwind");
            vortex.transform.position = ctrl.transform.position;
            WhirlwindRuntime w = vortex.AddComponent<WhirlwindRuntime>();
            w.ownerConfig = ctrl.BeyConfiguration;
            w.radius = r; w.pull = p; w.dps = d; w.timer = dur;
            w.CreateVisuals(dur);
            Object.Destroy(vortex, dur + 0.2f);
        }

        private void CreateVisuals(float dur)
        {
            visualRings = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "VortexRing";
                ring.transform.SetParent(transform, false);
                ring.transform.localPosition = Vector3.up * (i * 0.3f);
                float s = radius * 2f * (1f - i * 0.2f);
                ring.transform.localScale = new Vector3(s, 0.02f, s);
                Collider col = ring.GetComponent<Collider>(); if (col != null) col.enabled = false;
                Renderer rend = ring.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                    float alpha = 0.25f - i * 0.05f;
                    mat.color = new Color(0.6f, 0.85f, 0.7f, alpha);
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                    if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.3f, 1f, 0.5f)); }
                    rend.material = mat;
                }
                Object.Destroy(ring, dur);
                visualRings[i] = ring;
            }

            // Central funnel
            GameObject funnel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            funnel.name = "VortexFunnel";
            funnel.transform.SetParent(transform, false);
            funnel.transform.localPosition = Vector3.up * 0.5f;
            funnel.transform.localScale = new Vector3(1f, 1.5f, 1f);
            Collider fc = funnel.GetComponent<Collider>(); if (fc != null) fc.enabled = false;
            Renderer fr = funnel.GetComponent<Renderer>();
            if (fr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.4f, 0.9f, 0.6f, 0.15f);
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.2f, 0.8f, 0.3f)); }
                fr.material = mat;
            }
            Object.Destroy(funnel, dur);
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;

            // Rotate visual rings
            if (visualRings != null)
                for (int i = 0; i < visualRings.Length; i++)
                    if (visualRings[i] != null)
                        visualRings[i].transform.Rotate(Vector3.up, (300f + i * 100f) * Time.deltaTime);

            tickTimer -= Time.deltaTime;
            if (tickTimer > 0f) return;
            tickTimer = 0.2f;

            BeyMovementController[] beys = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);
            foreach (BeyMovementController bey in beys)
            {
                if (bey == null || bey.BeyConfiguration == null || bey.BeyConfiguration == ownerConfig) continue;
                if (bey.BeyConfiguration.IsEnemy == ownerConfig.IsEnemy) continue;
                float dist = Vector3.Distance(transform.position, bey.transform.position);
                if (dist > radius) continue;
                float falloff = 1f - (dist / radius);
                bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - dps * 0.2f * falloff);
                Rigidbody rb = bey.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 toCenter = (transform.position - bey.transform.position).normalized;
                    rb.AddForce(toCenter * pull * falloff, ForceMode.Force);
                }
            }
        }
    }
}
