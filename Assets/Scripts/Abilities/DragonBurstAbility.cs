using UnityEngine;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "DragonBurstAbility", menuName = "Blade Spinners/Abilities/Dragon Burst")]
    public class DragonBurstAbility : BeyAbility
    {
        [Header("Dragon Burst")]
        [SerializeField] private float surgeImpulse = 18f;
        [SerializeField] private float coneRange = 10f;
        [SerializeField] private float coneHalfAngle = 28f;
        [SerializeField] private float spinDamage = 24f;
        [SerializeField] private float knockbackImpulse = 14f;

        private void OnEnable()
        {
            abilityName = "Dragon Burst";
            description = "Legendary surge attack that blasts enemies in a forward cone.";
            manaCost = 95f;
            rarity = Core.AbilityRarity.Legendary;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null)
                return;

            Vector3 origin = beyController.transform.position;
            Vector3 forward = beyController.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            if (beyController.Rb != null)
            {
                beyController.Rb.AddForce(forward * surgeImpulse, ForceMode.VelocityChange);
            }

            BeyConfiguration ownerConfig = beyController.BeyConfiguration;
            EnemyBeyController[] enemies = Object.FindObjectsByType<EnemyBeyController>(FindObjectsSortMode.None);
            foreach (EnemyBeyController enemy in enemies)
            {
                if (enemy == null || enemy.BeyConfiguration == null)
                    continue;

                Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
                if (enemyRb == null)
                    continue;

                if (enemy.BeyConfiguration == ownerConfig)
                    continue;

                Vector3 toEnemy = enemy.transform.position - origin;
                float dist = toEnemy.magnitude;
                if (dist > coneRange || dist < 0.01f)
                    continue;

                Vector3 flatDir = toEnemy;
                flatDir.y = 0f;
                if (flatDir.sqrMagnitude < 0.001f)
                    continue;

                float angle = Vector3.Angle(forward, flatDir.normalized);
                if (angle > coneHalfAngle)
                    continue;

                float rangeFalloff = 1f - (dist / coneRange);
                float angleFalloff = 1f - (angle / coneHalfAngle);
                float totalFalloff = Mathf.Clamp01(rangeFalloff * angleFalloff);

                enemy.BeyConfiguration.SetSpin(enemy.BeyConfiguration.CurrentSpin - spinDamage * totalFalloff);
                Vector3 knockDir = flatDir.normalized + Vector3.up * 0.08f;
                enemyRb.AddForce(knockDir.normalized * knockbackImpulse * Mathf.Lerp(0.55f, 1f, totalFalloff), ForceMode.Impulse);
            }

            SpawnDragonVisual(origin, forward, coneRange);
            Debug.Log("[Ability] Dragon Burst!");
        }

        private void SpawnDragonVisual(Vector3 origin, Vector3 forward, float range)
        {
            // Fiery cone blast (3 scaled spheres in a line)
            for (int i = 0; i < 4; i++)
            {
                float t = (i + 1) * 0.25f;
                GameObject fireball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fireball.name = "DragonFlame";
                fireball.transform.position = origin + forward * (range * t * 0.6f) + Vector3.up * 0.15f;
                float scale = 0.5f + t * 1.2f;
                fireball.transform.localScale = Vector3.one * scale;
                Collider col = fireball.GetComponent<Collider>();
                if (col != null) col.enabled = false;
                Renderer rend = fireball.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                    float heatFade = 1f - t * 0.4f;
                    mat.color = new Color(1f, 0.3f * heatFade, 0f, 0.5f * heatFade);
                    if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(3f * heatFade, 0.8f * heatFade, 0f)); }
                    rend.material = mat;
                }
                Object.Destroy(fireball, 0.3f + i * 0.05f);
            }

            // Ground scorch mark
            GameObject scorch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scorch.name = "DragonScorch";
            scorch.transform.position = origin + forward * (range * 0.3f);
            scorch.transform.localScale = new Vector3(range * 0.5f, 0.01f, range * 0.8f);
            scorch.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 0f, 90f);
            Collider sc = scorch.GetComponent<Collider>();
            if (sc != null) sc.enabled = false;
            Renderer sr = scorch.GetComponent<Renderer>();
            if (sr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.3f, 0.1f, 0f, 0.3f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.6f, 0.15f, 0f)); }
                sr.material = mat;
            }
            Object.Destroy(scorch, 0.8f);
        }
    }
}
