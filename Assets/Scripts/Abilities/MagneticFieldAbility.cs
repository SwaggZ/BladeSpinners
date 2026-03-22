using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "MagneticFieldAbility", menuName = "Blade Spinners/Abilities/Magnetic Field")]
    public class MagneticFieldAbility : BeyAbility
    {
        [Header("Magnetic Field")]
        [SerializeField] private float radius = 7f;
        [SerializeField] private float repelForce = 12f;
        [SerializeField] private float duration = 3.5f;

        private void OnEnable()
        {
            abilityName = "Magnetic Field";
            description = "Project a magnetic barrier that forcefully repels all nearby enemies.";
            manaCost = 50f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;
            MagneticFieldRuntime.Apply(beyController, radius, repelForce, duration);
            Debug.Log("[Ability] Magnetic Field!");
        }
    }

    public class MagneticFieldRuntime : MonoBehaviour
    {
        private float radius, force, timer;
        private float tickTimer;

        public static void Apply(BeyMovementController ctrl, float rad, float f, float dur)
        {
            MagneticFieldRuntime existing = ctrl.GetComponent<MagneticFieldRuntime>();
            if (existing != null) { existing.timer = Mathf.Max(existing.timer, dur); return; }
            MagneticFieldRuntime mf = ctrl.gameObject.AddComponent<MagneticFieldRuntime>();
            mf.radius = rad;
            mf.force = f;
            mf.timer = dur;
            SpawnVisual(ctrl, dur);
        }

        private static void SpawnVisual(BeyMovementController ctrl, float dur)
        {
            // Magnetic dome
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "MagneticDome";
            dome.transform.SetParent(ctrl.transform, false);
            dome.transform.localPosition = Vector3.zero;
            dome.transform.localScale = Vector3.one * 3f;
            Collider c = dome.GetComponent<Collider>(); if (c != null) c.enabled = false;
            ApplyMat(dome, new Color(0.2f, 0.5f, 1f, 0.1f), new Color(0.3f, 0.8f, 2f));
            dome.AddComponent<MagneticDomePulse>().Init(dur);
            Object.Destroy(dome, dur + 0.1f);

            // Orbiting field lines (3 rings)
            for (int i = 0; i < 3; i++)
            {
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "MagneticRing";
                ring.transform.SetParent(ctrl.transform, false);
                ring.transform.localPosition = Vector3.up * (i * 0.3f - 0.3f);
                ring.transform.localScale = new Vector3(2f + i * 0.5f, 0.02f, 2f + i * 0.5f);
                ring.transform.localRotation = Quaternion.Euler(0f, i * 60f, 0f);
                Collider rc = ring.GetComponent<Collider>(); if (rc != null) rc.enabled = false;
                float hue = 0.55f + i * 0.05f;
                Color col = Color.HSVToRGB(hue, 0.7f, 1f);
                ApplyMat(ring, new Color(col.r, col.g, col.b, 0.25f), col * 2f);
                ring.AddComponent<MagneticRingSpin>().Init(120f + i * 40f);
                Object.Destroy(ring, dur + 0.1f);
            }
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) { Destroy(this); return; }
            tickTimer -= Time.deltaTime;
            if (tickTimer > 0f) return;
            tickTimer = 0.15f;

            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider col in hits)
            {
                if (col.gameObject == gameObject) continue;
                BeyMovementController enemy = col.GetComponentInParent<BeyMovementController>();
                if (enemy == null || enemy.gameObject == gameObject) continue;
                if (enemy.Rb == null) continue;
                Vector3 dir = (enemy.transform.position - transform.position).normalized;
                enemy.Rb.AddForce(dir * force, ForceMode.Impulse);
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

    public class MagneticDomePulse : MonoBehaviour
    {
        private float timer, baseScale;
        public void Init(float dur) { timer = dur; baseScale = transform.localScale.x; }
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.06f;
            transform.localScale = Vector3.one * baseScale * pulse;
        }
    }

    public class MagneticRingSpin : MonoBehaviour
    {
        private float speed;
        public void Init(float s) { speed = s; }
        private void Update()
        {
            transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.Self);
        }
    }
}
