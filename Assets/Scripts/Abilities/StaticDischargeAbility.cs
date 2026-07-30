using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "StaticDischargeAbility", menuName = "Blade Spinners/Abilities/Static Discharge")]
    public class StaticDischargeAbility : BeyAbility
    {
        [Header("Static Discharge")]
        [SerializeField] private float chargeUpTime = 2f;
        [SerializeField] private float explosionRadius = 5f;
        [SerializeField] private float explosionDamage = 30f;
        [SerializeField] private float knockbackForce = 15f;

        private void OnEnable()
        {
            abilityName = "Static Discharge";
            description = "Build up a massive electrical charge — on your next collision, release a devastating shock explosion.";
            manaCost = 45f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;
            StaticChargeRuntime.Apply(beyController, chargeUpTime, explosionRadius, explosionDamage, knockbackForce);
            Debug.Log("[Ability] Static Discharge charging!");
        }
    }

    public class StaticChargeRuntime : MonoBehaviour
    {
        private float chargeTimer, radius, dmg, knockback;
        private bool charged;
        private GameObject chargeVfx;

        public static void Apply(BeyMovementController ctrl, float chargeTime, float rad, float d, float kb)
        {
            StaticChargeRuntime existing = ctrl.GetComponent<StaticChargeRuntime>();
            if (existing != null) return;
            StaticChargeRuntime sc = ctrl.gameObject.AddComponent<StaticChargeRuntime>();
            sc.chargeTimer = chargeTime;
            sc.radius = rad;
            sc.dmg = d;
            sc.knockback = kb;
            sc.SpawnChargeVisual();
        }

        private void SpawnChargeVisual()
        {
            chargeVfx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            chargeVfx.name = "StaticCharge";
            chargeVfx.transform.SetParent(transform, false);
            chargeVfx.transform.localPosition = Vector3.zero;
            chargeVfx.transform.localScale = Vector3.one * 0.8f;
            Collider c = chargeVfx.GetComponent<Collider>(); if (c != null) c.enabled = false;
            ApplyMat(chargeVfx, new Color(0.5f, 0.8f, 1f, 0.2f), new Color(1f, 2f, 4f));
        }

        private void Update()
        {
            if (charged) return;
            chargeTimer -= Time.deltaTime;
            if (chargeTimer > 0f)
            {
                // Grow charge visual
                if (chargeVfx != null)
                {
                    float s = Mathf.Lerp(0.8f, 1.6f, 1f - chargeTimer / 2f) + Mathf.Sin(Time.time * 12f) * 0.1f;
                    chargeVfx.transform.localScale = Vector3.one * s;
                }
                return;
            }
            charged = true;
            if (chargeVfx != null)
            {
                // Change to bright yellow when charged
                Renderer r = chargeVfx.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.color = new Color(1f, 1f, 0.3f, 0.3f);
                    if (r.material.HasProperty("_EmissionColor"))
                        r.material.SetColor("_EmissionColor", new Color(5f, 5f, 1f));
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!charged) return;
            Explode();
        }

        private void Explode()
        {
            Vector3 pos = transform.position;
            if (chargeVfx != null) Object.Destroy(chargeVfx);

            // Damage + knockback
            BeyMovementController ownerBey = GetComponent<BeyMovementController>();
            foreach (BeyMovementController enemy in
                     AbilityTargetQuery.FindUniqueBeysInRadius(
                         ownerBey, pos, radius, AbilityTargetRelation.Enemy))
            {
                if (enemy.BeyConfiguration != null)
                    enemy.BeyConfiguration.SetSpin(enemy.BeyConfiguration.CurrentSpin - dmg);
                if (enemy.Rb != null)
                {
                    Vector3 dir = (enemy.transform.position - pos).normalized;
                    enemy.Rb.AddForce(dir * knockback, ForceMode.Impulse);
                }
            }

            // Explosion flash
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "StaticExplosion";
            flash.transform.position = pos;
            flash.transform.localScale = Vector3.one * 0.5f;
            Collider fc = flash.GetComponent<Collider>(); if (fc != null) fc.enabled = false;
            ApplyMat(flash, new Color(1f, 1f, 0.5f), new Color(6f, 6f, 2f));
            WaveExpandRuntime.Spawn(flash, radius * 1.25f, 0.6f);

            // Electric arcs
            for (int i = 0; i < 5; i++)
            {
                GameObject arc = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arc.name = "StaticArc";
                arc.transform.position = pos;
                arc.transform.localScale = new Vector3(0.04f, 0.04f, Random.Range(2f, 4f));
                arc.transform.rotation = Random.rotation;
                Collider ac = arc.GetComponent<Collider>(); if (ac != null) ac.enabled = false;
                ApplyMat(arc, new Color(0.5f, 0.8f, 1f), new Color(3f, 4f, 6f));
                Object.Destroy(arc, 0.4f);
            }

            Destroy(this);
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
}
