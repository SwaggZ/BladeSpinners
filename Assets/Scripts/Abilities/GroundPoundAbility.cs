using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "GroundPoundAbility", menuName = "Blade Spinners/Abilities/Ground Pound")]
    public class GroundPoundAbility : BeyAbility
    {
        [Header("Ground Pound")]
        [SerializeField] private float slamForce = 26f;
        [SerializeField] private float impactRadius = 6f;
        [SerializeField] private float spinDamage = 16f;
        [SerializeField] private float knockbackImpulse = 10f;

        private void OnEnable()
        {
            abilityName = "Ground Pound";
            description = "Slam downward and shock nearby enemies with spin damage and knockback.";
            manaCost = 45f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.Rb == null || beyController.BeyConfiguration == null)
                return;

            Rigidbody rb = beyController.Rb;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, 0f), rb.linearVelocity.z);
            rb.AddForce(Vector3.down * slamForce, ForceMode.VelocityChange);

            Vector3 origin = beyController.transform.position;
            foreach (BeyMovementController enemy in
                     AbilityTargetQuery.FindUniqueBeysInRadius(
                         beyController,
                         origin,
                         impactRadius,
                         AbilityTargetRelation.Enemy))
            {
                Rigidbody enemyRb = enemy.Rb;
                if (enemyRb == null)
                    continue;

                Vector3 toEnemy = enemy.transform.position - origin;
                float dist = toEnemy.magnitude;
                float falloff = 1f - (dist / impactRadius);
                enemy.BeyConfiguration.SetSpin(enemy.BeyConfiguration.CurrentSpin - spinDamage * falloff);

                Vector3 dir = dist > 0.01f ? toEnemy.normalized : Vector3.forward;
                dir.y = 0.15f;
                enemyRb.AddForce(dir.normalized * knockbackImpulse * Mathf.Lerp(0.5f, 1f, falloff), ForceMode.Impulse);
            }

            SpawnImpactVisual(origin, impactRadius);
            Debug.Log("[Ability] Ground Pound!");
        }

        private void SpawnImpactVisual(Vector3 center, float radius)
        {
            // Impact shockwave ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "PoundShockwave";
            ring.transform.position = center;
            ring.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);
            Collider col = ring.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Renderer rend = ring.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.8f, 0.6f, 0.3f, 0.5f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1.5f, 1f, 0.3f)); }
                rend.material = mat;
            }
            WaveExpandRuntime.Spawn(ring, radius, 0.35f);

            // Dust cloud particles
            for (int i = 0; i < 8; i++)
            {
                GameObject dust = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dust.name = "PoundDust";
                float angle = i * 45f * Mathf.Deg2Rad;
                dust.transform.position = center + new Vector3(Mathf.Cos(angle) * 0.4f, 0.1f, Mathf.Sin(angle) * 0.4f);
                dust.transform.localScale = Vector3.one * Random.Range(0.15f, 0.35f);
                Collider dc = dust.GetComponent<Collider>();
                if (dc != null) dc.enabled = false;
                Renderer dr = dust.GetComponent<Renderer>();
                if (dr != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                    mat.color = new Color(0.65f, 0.55f, 0.35f, 0.4f);
                    dr.material = mat;
                }
                Vector3 vel = new Vector3(Mathf.Cos(angle), Random.Range(0.3f, 0.8f), Mathf.Sin(angle)) * Random.Range(2f, 4f);
                dust.AddComponent<GroundPoundDustDrift>().Init(vel);
                Object.Destroy(dust, 0.6f);
            }

            // Central impact flash
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "PoundFlash";
            flash.transform.position = center;
            flash.transform.localScale = Vector3.one * 1.2f;
            Collider fc = flash.GetComponent<Collider>();
            if (fc != null) fc.enabled = false;
            Renderer fr = flash.GetComponent<Renderer>();
            if (fr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(1f, 0.8f, 0.3f, 0.6f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(2f, 1.5f, 0.3f)); }
                fr.material = mat;
            }
            Object.Destroy(flash, 0.2f);
        }
    }

    public class GroundPoundDustDrift : MonoBehaviour
    {
        private Vector3 velocity;
        public void Init(Vector3 vel) { velocity = vel; }
        private void Update()
        {
            velocity += Vector3.down * 5f * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            float s = transform.localScale.x * (1f - Time.deltaTime * 2f);
            transform.localScale = Vector3.one * Mathf.Max(s, 0.02f);
        }
    }
}
