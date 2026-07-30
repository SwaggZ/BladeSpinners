using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "NightfallAbility", menuName = "Blade Spinners/Abilities/Nightfall")]
    public class NightfallAbility : BeyAbility
    {
        [Header("Nightfall")]
        [SerializeField] private float radius = 9f;
        [SerializeField] private float slowFactor = 0.5f;
        [SerializeField] private float duration = 4f;
        [SerializeField] private float damage = 8f;

        private void OnEnable()
        {
            abilityName = "Nightfall";
            description = "Plunge the arena into darkness — enemies lose sight and slow to a crawl.";
            manaCost = 60f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;
            Vector3 pos = beyController.transform.position;

            foreach (BeyMovementController enemy in
                     AbilityTargetQuery.FindUniqueBeysInRadius(
                         beyController, pos, radius, AbilityTargetRelation.Enemy))
            {
                if (enemy.BeyConfiguration != null)
                    enemy.BeyConfiguration.SetSpin(enemy.BeyConfiguration.CurrentSpin - damage);
                if (enemy.Rb != null)
                    enemy.Rb.linearVelocity *= slowFactor;
                NightfallBlind.Apply(enemy, duration, slowFactor);
            }

            SpawnVisual(beyController, duration, radius);
            Debug.Log("[Ability] Nightfall!");
        }

        private void SpawnVisual(BeyMovementController ctrl, float dur, float rad)
        {
            // Dark expanding dome
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "NightfallDome";
            dome.transform.position = ctrl.transform.position;
            dome.transform.localScale = Vector3.one * rad * 2f;
            Collider c = dome.GetComponent<Collider>(); if (c != null) c.enabled = false;
            ApplyMat(dome, new Color(0.02f, 0f, 0.05f, 0.35f), new Color(0.1f, 0f, 0.2f));
            dome.AddComponent<NightfallDomeFade>().Init(dur);
            Object.Destroy(dome, dur + 0.2f);

            // Ground shadow ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "NightfallRing";
            ring.transform.position = ctrl.transform.position;
            ring.transform.localScale = new Vector3(rad * 2f, 0.02f, rad * 2f);
            Collider rc = ring.GetComponent<Collider>(); if (rc != null) rc.enabled = false;
            ApplyMat(ring, new Color(0.05f, 0f, 0.1f, 0.4f), new Color(0.15f, 0f, 0.4f));
            Object.Destroy(ring, dur);

            // Floating dark wisps
            for (int i = 0; i < 8; i++)
            {
                GameObject wisp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                wisp.name = "NightfallWisp";
                wisp.transform.position = ctrl.transform.position + Random.insideUnitSphere * rad * 0.6f;
                wisp.transform.localScale = Vector3.one * Random.Range(0.15f, 0.3f);
                Collider wc = wisp.GetComponent<Collider>(); if (wc != null) wc.enabled = false;
                ApplyMat(wisp, new Color(0.02f, 0f, 0.05f, 0.5f), new Color(0.1f, 0f, 0.3f));
                wisp.AddComponent<NightfallWispDrift>().Init(dur);
                Object.Destroy(wisp, dur);
            }
        }

        private static void ApplyMat(GameObject obj, Color baseCol, Color emission)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r == null) return;
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
            mat.color = baseCol;
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emission); }
            r.material = mat;
        }
    }

    public class NightfallBlind : MonoBehaviour
    {
        private float timer, slowFactor;
        public static void Apply(BeyMovementController ctrl, float dur, float slow)
        {
            NightfallBlind existing = ctrl.GetComponent<NightfallBlind>();
            if (existing != null) { existing.timer = Mathf.Max(existing.timer, dur); return; }
            NightfallBlind nb = ctrl.gameObject.AddComponent<NightfallBlind>();
            nb.timer = dur;
            nb.slowFactor = slow;
        }
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) { Destroy(this); return; }
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, rb.linearVelocity * slowFactor, Time.deltaTime * 2f);
        }
    }

    public class NightfallDomeFade : MonoBehaviour
    {
        private float timer, totalDur;
        public void Init(float dur) { timer = dur; totalDur = dur; }
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            float t = timer / totalDur;
            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                Color c = r.material.color;
                c.a = 0.35f * t;
                r.material.color = c;
            }
        }
    }

    public class NightfallWispDrift : MonoBehaviour
    {
        private Vector3 drift;
        private float timer;
        public void Init(float dur) { timer = dur; drift = Random.insideUnitSphere * 0.5f; drift.y = Mathf.Abs(drift.y); }
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            transform.position += drift * Time.deltaTime;
            transform.localScale *= (1f - Time.deltaTime * 0.3f);
        }
    }
}
