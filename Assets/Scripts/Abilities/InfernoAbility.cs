using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "InfernoAbility", menuName = "Blade Spinners/Abilities/Inferno")]
    public class InfernoAbility : BeyAbility
    {
        [Header("Inferno")]
        [SerializeField] private float radius = 5f;
        [SerializeField] private float burnDamagePerTick = 4f;
        [SerializeField] private float duration = 4f;

        private void OnEnable()
        {
            abilityName = "Inferno";
            description = "Ignite the ground in a blazing inferno zone — enemies inside burn continuously.";
            manaCost = 55f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;
            InfernoZone.Spawn(beyController.transform.position, radius, burnDamagePerTick, duration, beyController.gameObject);
            Debug.Log("[Ability] Inferno!");
        }
    }

    public class InfernoZone : MonoBehaviour
    {
        private float radius, dmg, timer;
        private float tickTimer;
        private GameObject owner;

        public static void Spawn(Vector3 pos, float rad, float dmgTick, float dur, GameObject own)
        {
            GameObject zone = new GameObject("InfernoZone");
            zone.transform.position = pos;
            InfernoZone iz = zone.AddComponent<InfernoZone>();
            iz.radius = rad;
            iz.dmg = dmgTick;
            iz.timer = dur;
            iz.owner = own;
            SpawnVisual(zone.transform, rad, dur);
            Object.Destroy(zone, dur + 0.2f);
        }

        private static void SpawnVisual(Transform parent, float rad, float dur)
        {
            // Fire ground disc
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "InfernoDisc";
            disc.transform.SetParent(parent, false);
            disc.transform.localPosition = Vector3.zero;
            disc.transform.localScale = new Vector3(rad * 2f, 0.03f, rad * 2f);
            Collider c = disc.GetComponent<Collider>(); if (c != null) c.enabled = false;
            ApplyMat(disc, new Color(1f, 0.3f, 0f, 0.3f), new Color(3f, 0.8f, 0f));
            Object.Destroy(disc, dur);

            // Rising flame pillars
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * rad * 0.6f, 0f, Mathf.Sin(angle) * rad * 0.6f);
                GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Cube);
                flame.name = "InfernoFlame";
                flame.transform.SetParent(parent, false);
                flame.transform.localPosition = offset;
                flame.transform.localScale = new Vector3(0.15f, 0.6f, 0.15f);
                Collider fc = flame.GetComponent<Collider>(); if (fc != null) fc.enabled = false;
                float hue = Random.Range(0f, 0.08f);
                Color col = Color.HSVToRGB(hue, 1f, 1f);
                ApplyMat(flame, col, col * 3f);
                flame.AddComponent<InfernoFlameFlicker>().Init(dur);
                Object.Destroy(flame, dur);
            }

            // Central fire core
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "InfernoCore";
            core.transform.SetParent(parent, false);
            core.transform.localPosition = Vector3.up * 0.3f;
            core.transform.localScale = Vector3.one * 0.8f;
            Collider cc = core.GetComponent<Collider>(); if (cc != null) cc.enabled = false;
            ApplyMat(core, new Color(1f, 0.6f, 0f, 0.3f), new Color(4f, 1.5f, 0f));
            core.AddComponent<InfernoCorePulse>().Init(dur);
            Object.Destroy(core, dur);
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            tickTimer -= Time.deltaTime;
            if (tickTimer > 0f) return;
            tickTimer = 0.3f;

            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider col in hits)
            {
                if (owner != null && col.gameObject == owner) continue;
                BeyMovementController bey = col.GetComponentInParent<BeyMovementController>();
                if (bey == null) continue;
                if (owner != null && bey.gameObject == owner) continue;
                if (bey.BeyConfiguration != null)
                    bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - dmg);
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

    public class InfernoFlameFlicker : MonoBehaviour
    {
        private float timer, baseY;
        public void Init(float dur) { timer = dur; baseY = transform.localScale.y; }
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            float flicker = baseY * (0.7f + Mathf.PerlinNoise(Time.time * 5f + transform.position.x, transform.position.z) * 0.6f);
            Vector3 s = transform.localScale;
            s.y = flicker;
            transform.localScale = s;
            transform.localPosition = new Vector3(transform.localPosition.x, flicker * 0.5f, transform.localPosition.z);
        }
    }

    public class InfernoCorePulse : MonoBehaviour
    {
        private float timer;
        public void Init(float dur) { timer = dur; }
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            float s = 0.8f + Mathf.Sin(Time.time * 6f) * 0.15f;
            transform.localScale = Vector3.one * s;
        }
    }
}
