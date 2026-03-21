using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "FireBoltAbility", menuName = "Blade Spinners/Abilities/Fire Bolt")]
    public class FireBoltAbility : BeyAbility
    {
        [Header("Fire Bolt")]
        [SerializeField] private float projectileSpeed = 22f;
        [SerializeField] private float directHitDamage = 20f;
        [SerializeField] private float burnDamagePerSecond = 5f;
        [SerializeField] private float burnDuration = 3f;
        [SerializeField] private float homingStrength = 4f;

        private void OnEnable()
        {
            abilityName = "Fire Bolt";
            description = "Launches a homing bolt of flame that burns the target on impact.";
            manaCost = 55f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null)
                return;

            BeyMovementController target = FindNearestEnemy(beyController);

            GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bolt.name = "FireBolt";
            bolt.transform.position = beyController.transform.position + Vector3.up * 0.3f;
            bolt.transform.localScale = Vector3.one * 0.35f;

            Collider col = bolt.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            Rigidbody rb = bolt.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            Renderer rend = bolt.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(1f, 0.4f, 0.05f);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(2f, 0.6f, 0f));
                }
                rend.material = mat;
            }

            FireBoltProjectile proj = bolt.AddComponent<FireBoltProjectile>();
            proj.Initialize(beyController.BeyConfiguration, target, projectileSpeed, directHitDamage, burnDamagePerSecond, burnDuration, homingStrength);

            Debug.Log("[Ability] Fire Bolt launched!");
        }

        private BeyMovementController FindNearestEnemy(BeyMovementController owner)
        {
            BeyMovementController[] all = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);
            BeyMovementController nearest = null;
            float best = float.MaxValue;
            foreach (BeyMovementController bey in all)
            {
                if (bey == null || bey == owner || bey.BeyConfiguration == null) continue;
                if (bey.BeyConfiguration.IsEnemy == owner.BeyConfiguration.IsEnemy) continue;
                float d = Vector3.Distance(owner.transform.position, bey.transform.position);
                if (d < best) { best = d; nearest = bey; }
            }
            return nearest;
        }
    }

    public class FireBoltProjectile : MonoBehaviour
    {
        private BeyConfiguration ownerConfig;
        private BeyMovementController target;
        private Rigidbody rb;
        private float speed;
        private float directDamage;
        private float burnDps;
        private float burnDur;
        private float homing;
        private float lifetime = 5f;

        public void Initialize(BeyConfiguration owner, BeyMovementController tgt, float spd, float dmg, float bdps, float bdur, float homingStr)
        {
            ownerConfig = owner;
            target = tgt;
            speed = spd;
            directDamage = dmg;
            burnDps = bdps;
            burnDur = bdur;
            homing = homingStr;
            rb = GetComponent<Rigidbody>();

            Vector3 dir = target != null
                ? (target.transform.position - transform.position).normalized
                : transform.forward;
            if (rb != null)
                rb.linearVelocity = dir * speed;

            Destroy(gameObject, lifetime);
        }

        private void FixedUpdate()
        {
            if (rb == null) return;
            if (target != null)
            {
                Vector3 toTarget = (target.transform.position - transform.position).normalized;
                rb.linearVelocity = Vector3.RotateTowards(rb.linearVelocity.normalized, toTarget, homing * Time.fixedDeltaTime, 0f) * speed;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            BeyMovementController hit = other.GetComponentInParent<BeyMovementController>();
            if (hit == null) return;
            if (hit.BeyConfiguration == null || hit.BeyConfiguration == ownerConfig) return;
            if (ownerConfig != null && hit.BeyConfiguration.IsEnemy == ownerConfig.IsEnemy) return;

            hit.BeyConfiguration.SetSpin(hit.BeyConfiguration.CurrentSpin - directDamage);
            BurnRuntime.Apply(hit, burnDps, burnDur);
            Destroy(gameObject);
        }
    }

    public class BurnRuntime : MonoBehaviour
    {
        private float dps;
        private float duration;
        private float tick = 0.25f;
        private float tickTimer;
        private BeyConfiguration config;

        public static void Apply(BeyMovementController controller, float damagePerSecond, float burnDuration)
        {
            if (controller == null) return;
            BurnRuntime existing = controller.GetComponent<BurnRuntime>();
            if (existing != null)
            {
                existing.duration = Mathf.Max(existing.duration, burnDuration);
                return;
            }
            BurnRuntime burn = controller.gameObject.AddComponent<BurnRuntime>();
            burn.config = controller.BeyConfiguration;
            burn.dps = damagePerSecond;
            burn.duration = burnDuration;
            burn.SpawnVisual();
        }

        private void Update()
        {
            duration -= Time.deltaTime;
            if (duration <= 0f) { Destroy(this); return; }

            tickTimer -= Time.deltaTime;
            if (tickTimer > 0f) return;
            tickTimer = tick;

            if (config != null)
                config.SetSpin(config.CurrentSpin - dps * tick);
        }

        private void SpawnVisual()
        {
            // Small fire-colored particle ring around the bey
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "BurnVisual";
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = new Vector3(1.2f, 0.08f, 1.2f);
            visual.transform.localPosition = Vector3.zero;

            Collider col = visual.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(1f, 0.35f, 0f, 0.6f);
                rend.material = mat;
            }

            Destroy(visual, duration);
        }
    }
}
