using System.Collections.Generic;
using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "ChainLightningAbility", menuName = "Blade Spinners/Abilities/Chain Lightning")]
    public class ChainLightningAbility : BeyAbility
    {
        [Header("Chain Lightning")]
        [SerializeField] private float initialRadius = 8f;
        [SerializeField] private float chainRadius = 6f;
        [SerializeField] private int maxChains = 4;
        [SerializeField] private float damagePerHit = 12f;
        [SerializeField] private float stunDuration = 0.8f;

        private void OnEnable()
        {
            abilityName = "Chain Lightning";
            description = "Releases a bolt that arcs between multiple nearby enemies, stunning and dealing spin damage.";
            manaCost = 70f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null)
                return;

            List<BeyMovementController> enemies =
                AbilityTargetQuery.FindAll(beyController, AbilityTargetRelation.Enemy);

            // Chain from owner's position
            Vector3 lastPos = beyController.transform.position;
            HashSet<BeyMovementController> hit = new HashSet<BeyMovementController>();
            float currentRadius = initialRadius;
            int chains = 0;

            while (chains < maxChains && enemies.Count > 0)
            {
                BeyMovementController nearest = null;
                float best = float.MaxValue;
                foreach (BeyMovementController enemy in enemies)
                {
                    if (hit.Contains(enemy)) continue;
                    float d = Vector3.Distance(lastPos, enemy.transform.position);
                    if (d < best && d <= currentRadius) { best = d; nearest = enemy; }
                }

                if (nearest == null) break;

                float falloff = Mathf.Lerp(1f, 0.5f, (float)chains / maxChains);
                nearest.BeyConfiguration.SetSpin(nearest.BeyConfiguration.CurrentSpin - damagePerHit * falloff);
                StunRuntime.Apply(nearest, stunDuration * falloff);
                SpawnLightningVisual(lastPos, nearest.transform.position);

                hit.Add(nearest);
                lastPos = nearest.transform.position;
                currentRadius = chainRadius;
                chains++;
            }

            Debug.Log($"[Ability] Chain Lightning! Chained {chains} enemies.");
        }

        private void SpawnLightningVisual(Vector3 from, Vector3 to)
        {
            GameObject bolt = new GameObject("LightningBolt");
            bolt.transform.position = (from + to) * 0.5f;

            // Use a stretched cube as a simple lightning line
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "LightningLine";
            line.transform.SetParent(bolt.transform, false);

            float dist = Vector3.Distance(from, to);
            line.transform.localScale = new Vector3(0.06f, 0.06f, dist);
            line.transform.LookAt(to);

            Collider col = line.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Renderer rend = line.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.7f, 0.8f, 1f);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(1f, 1.5f, 3f));
                }
                rend.material = mat;
            }

            Object.Destroy(bolt, 0.3f);
        }
    }

    public class StunRuntime : MonoBehaviour
    {
        private Rigidbody rb;
        private float timer;
        private Vector3 frozenVelocity;

        public static void Apply(BeyMovementController controller, float duration)
        {
            if (controller == null || duration <= 0f) return;
            StunRuntime existing = controller.GetComponent<StunRuntime>();
            if (existing != null) { existing.timer = Mathf.Max(existing.timer, duration); return; }

            StunRuntime stun = controller.gameObject.AddComponent<StunRuntime>();
            stun.rb = controller.Rb;
            stun.timer = duration;
            if (stun.rb != null) stun.frozenVelocity = stun.rb.linearVelocity;
        }

        private void FixedUpdate()
        {
            timer -= Time.fixedDeltaTime;
            if (timer <= 0f) { Destroy(this); return; }
            if (rb != null)
            {
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, 0.4f);
            }
        }
    }
}
