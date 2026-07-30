using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "GravityWellAbility", menuName = "Blade Spinners/Abilities/Gravity Well")]
    public class GravityWellAbility : BeyAbility
    {
        [Header("Gravity Well")]
        [SerializeField] private float radius = 8f;
        [SerializeField] private float pullForce = 6f;
        [SerializeField] private float damagePerTick = 3f;
        [SerializeField] private float duration = 4f;

        private void OnEnable()
        {
            abilityName = "Gravity Well";
            description = "Create a persistent gravity well that slowly pulls all enemies toward its crushing center.";
            manaCost = 70f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;
            Vector3 pos = beyController.transform.position;
            GravityWellZone.Spawn(pos, radius, pullForce, damagePerTick, duration, beyController.gameObject);
            Debug.Log("[Ability] Gravity Well!");
        }
    }

    public class GravityWellZone : MonoBehaviour
    {
        private float radius, pull, dmg, timer;
        private float tickTimer;
        private GameObject owner;

        public static void Spawn(Vector3 pos, float rad, float pullF, float dmgTick, float dur, GameObject own)
        {
            GameObject zone = new GameObject("GravityWellZone");
            zone.transform.position = pos;
            GravityWellZone gz = zone.AddComponent<GravityWellZone>();
            gz.radius = rad;
            gz.pull = pullF;
            gz.dmg = dmgTick;
            gz.timer = dur;
            gz.owner = own;
            SpawnVisual(zone.transform, rad, dur);
            Object.Destroy(zone, dur + 0.2f);
        }

        private static void SpawnVisual(Transform parent, float rad, float dur)
        {
            // Dark swirling vortex disc
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "GravWellDisc";
            disc.transform.SetParent(parent, false);
            disc.transform.localPosition = Vector3.zero;
            disc.transform.localScale = new Vector3(rad * 1.5f, 0.03f, rad * 1.5f);
            Collider c = disc.GetComponent<Collider>(); if (c != null) c.enabled = false;
            ApplyMat(disc, new Color(0.15f, 0f, 0.3f, 0.2f), new Color(0.5f, 0f, 1.5f));
            disc.AddComponent<GravWellDiscSpin>().Init(180f);
            Object.Destroy(disc, dur);

            // Core singularity sphere
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "GravWellCore";
            core.transform.SetParent(parent, false);
            core.transform.localPosition = Vector3.up * 0.2f;
            core.transform.localScale = Vector3.one * 0.5f;
            Collider cc = core.GetComponent<Collider>(); if (cc != null) cc.enabled = false;
            ApplyMat(core, new Color(0.05f, 0f, 0.1f), new Color(0.2f, 0f, 0.8f));
            core.AddComponent<GravWellCorePulse>().Init(dur);
            Object.Destroy(core, dur);

            // Orbiting debris (4 small cubes)
            for (int i = 0; i < 4; i++)
            {
                GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
                debris.name = "GravWellDebris";
                debris.transform.SetParent(parent, false);
                float angle = i * 90f * Mathf.Deg2Rad;
                debris.transform.localPosition = new Vector3(Mathf.Cos(angle) * 1.5f, 0.3f, Mathf.Sin(angle) * 1.5f);
                debris.transform.localScale = Vector3.one * 0.15f;
                debris.transform.rotation = Random.rotation;
                Collider dc = debris.GetComponent<Collider>(); if (dc != null) dc.enabled = false;
                ApplyMat(debris, new Color(0.3f, 0.1f, 0.5f), new Color(0.8f, 0.2f, 1.5f));
                debris.AddComponent<GravWellDebrisOrbit>().Init(200f + i * 30f, 1.5f);
                Object.Destroy(debris, dur);
            }
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            tickTimer -= Time.deltaTime;
            if (tickTimer > 0f) return;
            tickTimer = 0.2f;

            BeyMovementController ownerBey =
                owner != null ? owner.GetComponentInParent<BeyMovementController>() : null;
            foreach (BeyMovementController bey in
                     AbilityTargetQuery.FindUniqueBeysInRadius(
                         ownerBey, transform.position, radius, AbilityTargetRelation.Enemy))
            {
                Vector3 dir = (transform.position - bey.transform.position).normalized;
                if (bey.Rb != null) bey.Rb.AddForce(dir * pull, ForceMode.Force);
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

    public class GravWellDiscSpin : MonoBehaviour
    {
        private float speed;
        public void Init(float s) { speed = s; }
        private void Update() { transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.Self); }
    }

    public class GravWellCorePulse : MonoBehaviour
    {
        private float timer;
        public void Init(float dur) { timer = dur; }
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            float s = 0.5f + Mathf.Sin(Time.time * 8f) * 0.1f;
            transform.localScale = Vector3.one * s;
        }
    }

    public class GravWellDebrisOrbit : MonoBehaviour
    {
        private float speed, orbitRadius;
        private float angle;
        public void Init(float s, float r) { speed = s; orbitRadius = r; angle = Random.Range(0f, 360f); }
        private void Update()
        {
            angle += speed * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;
            transform.localPosition = new Vector3(Mathf.Cos(rad) * orbitRadius, 0.3f, Mathf.Sin(rad) * orbitRadius);
            transform.Rotate(Vector3.one, speed * 0.5f * Time.deltaTime, Space.Self);
        }
    }
}
