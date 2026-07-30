using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "WarCryAbility", menuName = "Blade Spinners/Abilities/War Cry")]
    public class WarCryAbility : BeyAbility
    {
        [Header("War Cry")]
        [SerializeField] private float radius = 8f;
        [SerializeField] private float selfSpeedBoost = 1.4f;
        [SerializeField] private float enemySlowFactor = 0.6f;
        [SerializeField] private float duration = 4f;
        [SerializeField] private float damage = 10f;

        private void OnEnable()
        {
            abilityName = "War Cry";
            description = "Unleash a rallying war cry — boost yourself while weakening nearby enemies.";
            manaCost = 60f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;
            Vector3 pos = beyController.transform.position;

            // Self speed boost
            if (beyController.Rb != null)
                beyController.Rb.linearVelocity *= selfSpeedBoost;

            // Debuff enemies
            foreach (BeyMovementController enemy in
                     AbilityTargetQuery.FindUniqueBeysInRadius(
                         beyController, pos, radius, AbilityTargetRelation.Enemy))
            {
                if (enemy.BeyConfiguration != null)
                    enemy.BeyConfiguration.SetSpin(enemy.BeyConfiguration.CurrentSpin - damage);
                if (enemy.Rb != null)
                    enemy.Rb.linearVelocity *= enemySlowFactor;
            }

            SpawnVisual(beyController, duration);
            Debug.Log("[Ability] War Cry!");
        }

        private void SpawnVisual(BeyMovementController ctrl, float dur)
        {
            // Expanding shockwave ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "WarCryRing";
            ring.transform.position = ctrl.transform.position;
            ring.transform.localScale = new Vector3(1f, 0.05f, 1f);
            Collider c = ring.GetComponent<Collider>(); if (c != null) c.enabled = false;
            ApplyMat(ring, new Color(1f, 0.7f, 0f, 0.3f), new Color(3f, 1.5f, 0f));
            WaveExpandRuntime.Spawn(ring, radius, 1.5f);

            // DBZ charging aura (golden war energy)
            DBZAuraHelper.Spawn(
                ctrl.transform, dur,
                new Color(1f, 0.7f, 0f),    // gold core
                new Color(1f, 0.85f, 0.2f), // bright gold outer
                3f
            );
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

    public class WarCryBurstRise : MonoBehaviour
    {
        private float speed;
        public void Init(float s) { speed = s; }
        private void Update()
        {
            transform.position += Vector3.up * speed * Time.deltaTime;
            transform.localScale *= (1f - Time.deltaTime * 1.2f);
        }
    }
}
