using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "CrystalBarrageAbility", menuName = "Blade Spinners/Abilities/Crystal Barrage")]
    public class CrystalBarrageAbility : BeyAbility
    {
        [Header("Crystal Barrage")]
        [SerializeField] private int crystalCount = 5;
        [SerializeField] private float orbitRadius = 1.5f;
        [SerializeField] private float damagePerCrystal = 10f;
        [SerializeField] private float duration = 5f;
        [SerializeField] private float fireRange = 6f;

        private void OnEnable()
        {
            abilityName = "Crystal Barrage";
            description = "Summon orbiting crystals that automatically launch at nearby enemies.";
            manaCost = 75f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;
            CrystalBarrageRuntime.Apply(beyController, crystalCount, orbitRadius, damagePerCrystal, duration, fireRange);
            Debug.Log("[Ability] Crystal Barrage!");
        }
    }

    public class CrystalBarrageRuntime : MonoBehaviour
    {
        private BeyMovementController ctrl;
        private float dmg, timer, fireRange, orbitRadius;
        private float scanTimer;
        private GameObject[] crystals;
        private bool[] fired;
        private float orbitSpeed = 200f;
        private float elapsed;

        public static void Apply(BeyMovementController c, int count, float orbRad, float dmgPer, float dur, float range)
        {
            CrystalBarrageRuntime cb = c.gameObject.AddComponent<CrystalBarrageRuntime>();
            cb.ctrl = c;
            cb.dmg = dmgPer;
            cb.timer = dur;
            cb.fireRange = range;
            cb.orbitRadius = orbRad;
            cb.crystals = new GameObject[count];
            cb.fired = new bool[count];

            for (int i = 0; i < count; i++)
            {
                GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crystal.name = "CrystalBarrageShard";
                crystal.transform.SetParent(c.transform, false);
                crystal.transform.localScale = new Vector3(0.12f, 0.25f, 0.12f);
                crystal.transform.localRotation = Quaternion.Euler(45f, i * (360f / count), 0f);
                Collider col = crystal.GetComponent<Collider>(); if (col != null) col.enabled = false;
                float hue = Random.Range(0.45f, 0.65f);
                Color col2 = Color.HSVToRGB(hue, 0.7f, 1f);
                ApplyMat(crystal, new Color(col2.r, col2.g, col2.b, 0.6f), col2 * 2.5f);
                cb.crystals[i] = crystal;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            timer -= Time.deltaTime;

            // Orbit unfired crystals
            int remaining = 0;
            for (int i = 0; i < crystals.Length; i++)
            {
                if (fired[i] || crystals[i] == null) continue;
                remaining++;
                float angle = (elapsed * orbitSpeed + i * (360f / crystals.Length)) * Mathf.Deg2Rad;
                crystals[i].transform.localPosition = new Vector3(Mathf.Cos(angle) * orbitRadius, 0.3f, Mathf.Sin(angle) * orbitRadius);
                crystals[i].transform.Rotate(Vector3.up, 360f * Time.deltaTime, Space.Self);
            }

            if (remaining == 0 || timer <= 0f)
            {
                // Destroy any remaining crystals
                foreach (var c in crystals) if (c != null) Object.Destroy(c);
                Destroy(this);
                return;
            }

            // Auto-fire at nearest enemy
            scanTimer -= Time.deltaTime;
            if (scanTimer > 0f) return;
            scanTimer = 0.5f;

            BeyMovementController target = FindNearest();
            if (target == null) return;

            for (int i = 0; i < crystals.Length; i++)
            {
                if (fired[i] || crystals[i] == null) continue;
                fired[i] = true;
                crystals[i].transform.SetParent(null);
                crystals[i].AddComponent<CrystalProjectile>().Init(target.transform, dmg);
                Object.Destroy(crystals[i], 3f);
                break; // fire one at a time
            }
        }

        private BeyMovementController FindNearest()
        {
            return AbilityTargetQuery.FindNearest(
                ctrl,
                transform.position,
                fireRange,
                AbilityTargetRelation.Enemy);
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

    public class CrystalProjectile : MonoBehaviour
    {
        private Transform target;
        private float dmg;
        private bool hit;

        public void Init(Transform t, float d) { target = t; dmg = d; }

        private void Update()
        {
            if (hit || target == null) return;
            transform.position = Vector3.MoveTowards(transform.position, target.position, 18f * Time.deltaTime);
            if (Vector3.Distance(transform.position, target.position) < 0.5f)
            {
                hit = true;
                BeyMovementController bey = target.GetComponentInParent<BeyMovementController>();
                if (bey != null && bey.BeyConfiguration != null)
                    bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - dmg);
                transform.localScale = Vector3.one * 0.02f;
            }
        }
    }
}
