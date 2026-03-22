using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "BlackHoleAbility", menuName = "Blade Spinners/Abilities/Black Hole")]
    public class BlackHoleAbility : BeyAbility
    {
        [Header("Black Hole")]
        [SerializeField] private float radius = 10f;
        [SerializeField] private float pullForce = 18f;
        [SerializeField] private float crushDamage = 25f;
        [SerializeField] private float duration = 3f;

        private void OnEnable()
        {
            abilityName = "Black Hole";
            description = "Tear open a singularity that pulls all enemies to their doom — massive damage at the center.";
            manaCost = 90f;
            rarity = Core.AbilityRarity.Legendary;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;
            Vector3 pos = beyController.transform.position + beyController.transform.forward * 3f;
            BlackHoleZone.Spawn(pos, radius, pullForce, crushDamage, duration, beyController.gameObject);
            Debug.Log("[Ability] Black Hole!");
        }
    }

    public class BlackHoleZone : MonoBehaviour
    {
        private float radius, pull, dmg, timer;
        private float tickTimer;
        private GameObject owner;

        public static void Spawn(Vector3 pos, float rad, float pullF, float crushDmg, float dur, GameObject own)
        {
            GameObject zone = new GameObject("BlackHoleZone");
            zone.transform.position = pos;
            BlackHoleZone bh = zone.AddComponent<BlackHoleZone>();
            bh.radius = rad;
            bh.pull = pullF;
            bh.dmg = crushDmg;
            bh.timer = dur;
            bh.owner = own;
            SpawnVisual(zone.transform, dur);
            Object.Destroy(zone, dur + 0.5f);
        }

        private static void SpawnVisual(Transform parent, float dur)
        {
            // Event horizon sphere (dark core)
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "BlackHoleCore";
            core.transform.SetParent(parent, false);
            core.transform.localPosition = Vector3.zero;
            core.transform.localScale = Vector3.one * 0.6f;
            Collider c = core.GetComponent<Collider>(); if (c != null) c.enabled = false;
            ApplyMat(core, new Color(0.01f, 0f, 0.02f), new Color(0.05f, 0f, 0.1f));
            core.AddComponent<BlackHoleCorePulse>().Init(dur);
            Object.Destroy(core, dur);

            // Accretion disc
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "BlackHoleDisc";
            disc.transform.SetParent(parent, false);
            disc.transform.localPosition = Vector3.zero;
            disc.transform.localScale = new Vector3(4f, 0.02f, 4f);
            Collider dc = disc.GetComponent<Collider>(); if (dc != null) dc.enabled = false;
            ApplyMat(disc, new Color(0.6f, 0.2f, 1f, 0.25f), new Color(1.5f, 0.3f, 3f));
            disc.AddComponent<BlackHoleDiscSpin>().Init(360f);
            Object.Destroy(disc, dur);

            // Orbiting matter streams (6 cubes contracting inward)
            for (int i = 0; i < 6; i++)
            {
                GameObject stream = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stream.name = "BlackHoleStream";
                stream.transform.SetParent(parent, false);
                float angle = i * 60f * Mathf.Deg2Rad;
                stream.transform.localPosition = new Vector3(Mathf.Cos(angle) * 2.5f, 0f, Mathf.Sin(angle) * 2.5f);
                stream.transform.localScale = new Vector3(0.08f, 0.08f, 1f);
                stream.transform.LookAt(parent.position);
                Collider sc = stream.GetComponent<Collider>(); if (sc != null) sc.enabled = false;
                float hue = Random.Range(0.7f, 0.85f);
                Color col = Color.HSVToRGB(hue, 0.8f, 1f);
                ApplyMat(stream, new Color(col.r, col.g, col.b, 0.5f), col * 2f);
                stream.AddComponent<BlackHoleStreamContract>().Init(dur, 2.5f, 500f + i * 30f);
                Object.Destroy(stream, dur);
            }

            // Distortion ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "BlackHoleRing";
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(5f, 0.01f, 5f);
            ring.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
            Collider rc = ring.GetComponent<Collider>(); if (rc != null) rc.enabled = false;
            ApplyMat(ring, new Color(0.3f, 0f, 0.6f, 0.15f), new Color(0.8f, 0f, 2f));
            ring.AddComponent<BlackHoleDiscSpin>().Init(-240f);
            Object.Destroy(ring, dur);
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            tickTimer -= Time.deltaTime;
            if (tickTimer > 0f) return;
            tickTimer = 0.15f;

            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider col in hits)
            {
                if (owner != null && col.gameObject == owner) continue;
                BeyMovementController bey = col.GetComponentInParent<BeyMovementController>();
                if (bey == null) continue;
                if (owner != null && bey.gameObject == owner) continue;
                float dist = Vector3.Distance(transform.position, bey.transform.position);
                Vector3 dir = (transform.position - bey.transform.position).normalized;
                float forceMult = 1f + (1f - dist / radius) * 2f; // stronger near center
                if (bey.Rb != null) bey.Rb.AddForce(dir * pull * forceMult, ForceMode.Force);
                if (dist < 2f && bey.BeyConfiguration != null)
                    bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - dmg * Time.deltaTime);
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

    public class BlackHoleCorePulse : MonoBehaviour
    {
        private float timer;
        public void Init(float dur) { timer = dur; }
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            float s = 0.6f + Mathf.Sin(Time.time * 10f) * 0.08f;
            transform.localScale = Vector3.one * s;
        }
    }

    public class BlackHoleDiscSpin : MonoBehaviour
    {
        private float speed;
        public void Init(float s) { speed = s; }
        private void Update() { transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.Self); }
    }

    public class BlackHoleStreamContract : MonoBehaviour
    {
        private float timer, totalDur, maxDist, orbitSpeed, angle;
        public void Init(float dur, float dist, float speed) { timer = dur; totalDur = dur; maxDist = dist; orbitSpeed = speed; angle = Random.Range(0f, 360f); }
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            float t = 1f - timer / totalDur;
            float currentDist = Mathf.Lerp(maxDist, 0.2f, t);
            angle += orbitSpeed * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;
            transform.localPosition = new Vector3(Mathf.Cos(rad) * currentDist, 0f, Mathf.Sin(rad) * currentDist);
            transform.LookAt(transform.parent != null ? transform.parent.position : transform.position + Vector3.up);
        }
    }
}
