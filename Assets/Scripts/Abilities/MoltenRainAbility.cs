using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "MoltenRainAbility", menuName = "Blade Spinners/Abilities/Molten Rain")]
    public class MoltenRainAbility : BeyAbility
    {
        [Header("Molten Rain")]
        [SerializeField] private float radius = 6f;
        [SerializeField] private float damagePerDrop = 6f;
        [SerializeField] private int dropCount = 8;
        [SerializeField] private float duration = 3f;

        private void OnEnable()
        {
            abilityName = "Molten Rain";
            description = "Call down a rain of molten meteors over a wide area, scorching all enemies caught below.";
            manaCost = 80f;
            rarity = Core.AbilityRarity.Legendary;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;
            MoltenRainRuntime.Apply(beyController, beyController.transform.position, radius, damagePerDrop, dropCount, duration);
            Debug.Log("[Ability] Molten Rain!");
        }
    }

    public class MoltenRainRuntime : MonoBehaviour
    {
        private BeyMovementController owner;
        private Vector3 center;
        private float radius, dmg, interval, timer, elapsed;
        private int remaining;

        public static void Apply(BeyMovementController ctrl, Vector3 pos, float rad, float dmgPerDrop, int count, float dur)
        {
            MoltenRainRuntime mr = ctrl.gameObject.AddComponent<MoltenRainRuntime>();
            mr.owner = ctrl;
            mr.center = pos;
            mr.radius = rad;
            mr.dmg = dmgPerDrop;
            mr.remaining = count;
            mr.interval = dur / count;

            // Ground zone indicator
            GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            zone.name = "MoltenRainZone";
            zone.transform.position = pos;
            zone.transform.localScale = new Vector3(rad * 2f, 0.02f, rad * 2f);
            Collider c = zone.GetComponent<Collider>(); if (c != null) c.enabled = false;
            ApplyMat(zone, new Color(1f, 0.3f, 0f, 0.15f), new Color(2f, 0.4f, 0f));
            Object.Destroy(zone, dur + 0.5f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            timer -= Time.deltaTime;
            if (timer > 0f || remaining <= 0) { if (remaining <= 0) Destroy(this); return; }
            timer = interval;
            remaining--;
            SpawnDrop();
        }

        private void SpawnDrop()
        {
            Vector2 rnd = Random.insideUnitCircle * radius;
            Vector3 dropPos = center + new Vector3(rnd.x, 6f, rnd.y);
            Vector3 impactPos = center + new Vector3(rnd.x, 0.1f, rnd.y);

            // Falling fireball
            GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            drop.name = "MoltenDrop";
            drop.transform.position = dropPos;
            drop.transform.localScale = Vector3.one * 0.4f;
            Collider c = drop.GetComponent<Collider>(); if (c != null) c.enabled = false;
            ApplyMat(drop, new Color(1f, 0.5f, 0f), new Color(5f, 2f, 0f));
            drop.AddComponent<MoltenDropFall>().Init(owner, impactPos, dmg);
            Object.Destroy(drop, 2f);

            // Impact scorch on ground
            GameObject scorch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scorch.name = "MoltenScorch";
            scorch.transform.position = impactPos;
            scorch.transform.localScale = new Vector3(1.2f, 0.01f, 1.2f);
            Collider sc = scorch.GetComponent<Collider>(); if (sc != null) sc.enabled = false;
            ApplyMat(scorch, new Color(0.8f, 0.2f, 0f, 0.3f), new Color(1.5f, 0.3f, 0f));
            Object.Destroy(scorch, 2f);
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

    public class MoltenDropFall : MonoBehaviour
    {
        private BeyMovementController owner;
        private Vector3 target;
        private float dmg;
        private bool hit;

        public void Init(BeyMovementController caster, Vector3 t, float d)
        {
            owner = caster;
            target = t;
            dmg = d;
        }

        private void Update()
        {
            if (hit) return;
            transform.position = Vector3.MoveTowards(transform.position, target, 15f * Time.deltaTime);
            if (Vector3.Distance(transform.position, target) < 0.2f)
            {
                hit = true;
                foreach (BeyMovementController bey in
                         AbilityTargetQuery.FindUniqueBeysInRadius(
                             owner, target, 1.5f, AbilityTargetRelation.Enemy))
                {
                    if (bey.BeyConfiguration != null)
                        bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - dmg);
                }
                transform.localScale = Vector3.one * 0.1f;
            }
        }
    }
}
