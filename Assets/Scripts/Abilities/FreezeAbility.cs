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

            BeyConfiguration ownerConfig = beyController.BeyConfiguration;
            BeyMovementController[] beys = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);

            foreach (BeyMovementController bey in beys)
            {
                if (bey == null || bey.BeyConfiguration == null || bey.BeyConfiguration == ownerConfig)
                    continue;

                if (bey.BeyConfiguration.IsEnemy == ownerConfig.IsEnemy)
                    continue;

                float dist = Vector3.Distance(beyController.transform.position, bey.transform.position);
                if (dist > radius)
                    continue;

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
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "FreezeVisual";
            visual.transform.SetParent(target.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 1.4f;

            Collider col = visual.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.55f, 0.85f, 1f, 0.45f);
                if (mat.HasProperty("_Surface"))
                    mat.SetFloat("_Surface", 1f);   // Transparent
                rend.material = mat;
            }

            Destroy(visual, timer + 0.1f);
        }
    }
}
