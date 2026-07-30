using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "FreezeAbility", menuName = "Blade Spinners/Abilities/Freeze")]
    public class FreezeAbility : BeyAbility
    {
        [Header("Freeze")]
        [SerializeField] private float radius = 6f;
        [SerializeField] private float freezeDuration = 2.5f;
        [SerializeField] private float spinDamage = 18f;

        private void OnEnable()
        {
            abilityName = "Freeze";
            description = "Encases nearby enemies in ice, halting their movement and shattering their spin.";
            manaCost = 65f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null)
                return;

            foreach (BeyMovementController bey in
                     AbilityTargetQuery.FindUniqueBeysInRadius(
                         beyController,
                         beyController.transform.position,
                         radius,
                         AbilityTargetRelation.Enemy))
            {
                float dist = Vector3.Distance(beyController.transform.position, bey.transform.position);
                float falloff = 1f - (dist / radius);
                bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - spinDamage * falloff);
                FreezeRuntime.Apply(bey, freezeDuration);
            }

            Debug.Log("[Ability] Freeze!");
        }
    }

    public class FreezeRuntime : MonoBehaviour
    {
        private BeyMovementController target;
        private RigidbodyConstraints originalConstraints;
        private float timer;

        public static void Apply(BeyMovementController controller, float duration)
        {
            if (controller == null)
                return;

            FreezeRuntime existing = controller.GetComponent<FreezeRuntime>();
            if (existing != null)
            {
                existing.timer = Mathf.Max(existing.timer, duration);
                return;
            }

            FreezeRuntime freeze = controller.gameObject.AddComponent<FreezeRuntime>();
            freeze.target = controller;
            freeze.timer = duration;
            freeze.originalConstraints = controller.Rb != null ? controller.Rb.constraints : RigidbodyConstraints.None;
            freeze.ApplyFreeze();
        }

        private void ApplyFreeze()
        {
            if (target == null || target.Rb == null)
                return;

            target.Rb.linearVelocity = Vector3.zero;
            target.Rb.angularVelocity = Vector3.zero;
            target.Rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
            SpawnIceVisual();
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                Thaw();
        }

        private void Thaw()
        {
            if (target != null && target.Rb != null)
                target.Rb.constraints = originalConstraints;

            Destroy(this);
        }

        private void OnDestroy()
        {
            if (target != null && target.Rb != null)
                target.Rb.constraints = originalConstraints;
        }

        private void SpawnIceVisual()
        {
            // --- Main ice shell (pulsing, frosted sphere) ---
            GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = "FreezeShell";
            shell.transform.SetParent(target.transform, false);
            shell.transform.localPosition = Vector3.zero;
            shell.transform.localScale = Vector3.one * 1.5f;
            DisableCollider(shell);
            ApplyIceMaterial(shell, new Color(0.45f, 0.78f, 1f, 0.25f), new Color(0.4f, 1.2f, 2.5f));
            shell.AddComponent<FreezeShellPulse>().Init(timer);
            Destroy(shell, timer + 0.1f);

            // --- Ice crystal shards (4 rotated cubes orbiting) ---
            for (int i = 0; i < 4; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "IceShard";
                shard.transform.SetParent(target.transform, false);
                float angle = i * 90f;
                float rad = angle * Mathf.Deg2Rad;
                shard.transform.localPosition = new Vector3(Mathf.Cos(rad) * 0.9f, 0.2f + (i % 2) * 0.4f, Mathf.Sin(rad) * 0.9f);
                shard.transform.localScale = new Vector3(0.12f, 0.45f, 0.08f);
                shard.transform.localRotation = Quaternion.Euler(15f + i * 10f, angle, 20f - i * 8f);
                DisableCollider(shard);
                ApplyIceMaterial(shard, new Color(0.65f, 0.9f, 1f, 0.3f), new Color(0.8f, 1.5f, 3f));
                Destroy(shard, timer + 0.1f);
            }

            // --- Ground frost ring ---
            GameObject frostRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            frostRing.name = "FrostRing";
            frostRing.transform.SetParent(target.transform, false);
            frostRing.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            frostRing.transform.localScale = new Vector3(2.4f, 0.02f, 2.4f);
            DisableCollider(frostRing);
            ApplyIceMaterial(frostRing, new Color(0.5f, 0.82f, 1f, 0.25f), new Color(0.2f, 0.6f, 1.5f));
            Destroy(frostRing, timer + 0.1f);

            // --- Rising ice sparkles ---
            for (int i = 0; i < 6; i++)
            {
                GameObject sparkle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sparkle.name = "IceSparkle";
                sparkle.transform.SetParent(target.transform, false);
                float r = Random.Range(0.3f, 1.0f);
                float a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                sparkle.transform.localPosition = new Vector3(Mathf.Cos(a) * r, Random.Range(-0.2f, 0.4f), Mathf.Sin(a) * r);
                sparkle.transform.localScale = Vector3.one * Random.Range(0.04f, 0.08f);
                DisableCollider(sparkle);
                ApplyIceMaterial(sparkle, new Color(0.8f, 0.95f, 1f, 0.35f), new Color(2f, 3f, 4f));
                sparkle.AddComponent<IceSparkleRise>().Init(timer);
                Destroy(sparkle, timer + 0.1f);
            }
        }

        private static void DisableCollider(GameObject obj)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        private static void ApplyIceMaterial(GameObject obj, Color baseColor, Color emissionColor)
        {
            DBZAuraHelper.ApplyTransparentMat(obj, baseColor, emissionColor);
        }
    }

    public class FreezeShellPulse : MonoBehaviour
    {
        private float baseScale;
        private float timer;
        private float elapsed;
        public void Init(float duration) { baseScale = transform.localScale.x; timer = duration; }
        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed > timer) return;
            float pulse = 1f + Mathf.Sin(elapsed * 8f) * 0.06f;
            float fade = 1f - (elapsed / timer) * 0.3f;
            transform.localScale = Vector3.one * baseScale * pulse * fade;
        }
    }

    public class IceSparkleRise : MonoBehaviour
    {
        private float speed;
        private float timer;
        public void Init(float duration) { speed = Random.Range(0.3f, 0.7f); timer = duration; }
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            transform.localPosition += Vector3.up * speed * Time.deltaTime;
            float s = transform.localScale.x * (1f - Time.deltaTime * 0.8f);
            transform.localScale = Vector3.one * Mathf.Max(s, 0.01f);
        }
    }
}
