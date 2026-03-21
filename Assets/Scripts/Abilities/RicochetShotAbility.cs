using System.Collections.Generic;
using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "RicochetShotAbility", menuName = "Blade Spinners/Abilities/Ricochet Shot")]
    public class RicochetShotAbility : BeyAbility
    {
        [Header("Ricochet Shot")]
        [SerializeField] private float projectileSpeed = 18f;
        [SerializeField] private float damagePerBounce = 10f;
        [SerializeField] private int maxBounces = 5;
        [SerializeField] private float bounceRadius = 8f;

        private void OnEnable()
        {
            abilityName = "Ricochet Shot";
            description = "Fires a bolt that bounces between enemies, dealing spin damage on each hit.";
            manaCost = 55f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null)
                return;

            BeyMovementController firstTarget = FindNearestEnemy(beyController, null);
            if (firstTarget == null)
            {
                Debug.Log("[Ability] Ricochet: no target.");
                return;
            }

            // Spawn the bouncing projectile
            GameObject proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proj.name = "RicochetBolt";
            proj.transform.position = beyController.transform.position + Vector3.up * 0.3f;
            proj.transform.localScale = Vector3.one * 0.28f;

            Collider col = proj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            Rigidbody rb = proj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            Renderer rend = proj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.9f, 0.7f, 0.1f);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(2f, 1.4f, 0.1f));
                }
                rend.material = mat;
            }

            RicochetProjectileRuntime runtime = proj.AddComponent<RicochetProjectileRuntime>();
            runtime.Initialize(beyController.BeyConfiguration, firstTarget, projectileSpeed, damagePerBounce, maxBounces, bounceRadius);

            Debug.Log("[Ability] Ricochet Shot!");
        }

        private BeyMovementController FindNearestEnemy(BeyMovementController owner, BeyMovementController exclude)
        {
            BeyMovementController[] all = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);
            BeyMovementController nearest = null;
            float best = float.MaxValue;
            foreach (BeyMovementController bey in all)
            {
                if (bey == null || bey == owner || bey == exclude || bey.BeyConfiguration == null) continue;
                if (bey.BeyConfiguration.IsEnemy == owner.BeyConfiguration.IsEnemy) continue;
                float d = Vector3.Distance(owner.transform.position, bey.transform.position);
                if (d < best) { best = d; nearest = bey; }
            }
            return nearest;
        }
    }

    public class RicochetProjectileRuntime : MonoBehaviour
    {
        private BeyConfiguration ownerConfig;
        private BeyMovementController currentTarget;
        private Rigidbody rb;
        private float speed;
        private float damage;
        private int bouncesLeft;
        private float bounceRadius;
        private HashSet<BeyMovementController> alreadyHit = new HashSet<BeyMovementController>();
        private float lifetime = 6f;

        public void Initialize(BeyConfiguration owner, BeyMovementController firstTarget, float spd, float dmg, int maxBounces, float bRadius)
        {
            ownerConfig = owner;
            currentTarget = firstTarget;
            speed = spd;
            damage = dmg;
            bouncesLeft = maxBounces;
            bounceRadius = bRadius;
            rb = GetComponent<Rigidbody>();
            Destroy(gameObject, lifetime);
            SetVelocityToward(currentTarget != null ? currentTarget.transform.position : transform.forward);
        }

        private void SetVelocityToward(Vector3 position)
        {
            if (rb == null) return;
            Vector3 dir = (position - transform.position).normalized;
            rb.linearVelocity = dir * speed;
        }

        private void FixedUpdate()
        {
            if (rb == null || currentTarget == null) return;
            Vector3 toTarget = (currentTarget.transform.position - transform.position).normalized;
            rb.linearVelocity = Vector3.RotateTowards(rb.linearVelocity.normalized, toTarget, 5f * Time.fixedDeltaTime, 0f) * speed;
        }

        private void OnTriggerEnter(Collider other)
        {
            BeyMovementController bey = other.GetComponentInParent<BeyMovementController>();
            if (bey == null || bey.BeyConfiguration == null) return;
            if (bey.BeyConfiguration == ownerConfig) return;
            if (ownerConfig != null && bey.BeyConfiguration.IsEnemy == ownerConfig.IsEnemy) return;
            if (alreadyHit.Contains(bey)) return;

            bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - damage);
            alreadyHit.Add(bey);
            bouncesLeft--;

            if (bouncesLeft <= 0) { Destroy(gameObject); return; }

            // Find next target in range NOT already hit
            BeyMovementController next = FindNextTarget(bey.transform.position);
            if (next == null) { Destroy(gameObject); return; }

            currentTarget = next;
            SetVelocityToward(currentTarget.transform.position);
        }

        private BeyMovementController FindNextTarget(Vector3 fromPos)
        {
            BeyMovementController[] all = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);
            BeyMovementController nearest = null;
            float best = float.MaxValue;
            foreach (BeyMovementController bey in all)
            {
                if (bey == null || bey.BeyConfiguration == null) continue;
                if (ownerConfig != null && bey.BeyConfiguration == ownerConfig) continue;
                if (ownerConfig != null && bey.BeyConfiguration.IsEnemy == ownerConfig.IsEnemy) continue;
                if (alreadyHit.Contains(bey)) continue;
                float d = Vector3.Distance(fromPos, bey.transform.position);
                if (d < best && d <= bounceRadius) { best = d; nearest = bey; }
            }
            return nearest;
        }
    }
}
