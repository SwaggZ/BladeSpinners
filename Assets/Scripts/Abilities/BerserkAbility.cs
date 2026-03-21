using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "BerserkAbility", menuName = "Blade Spinners/Abilities/Berserk")]
    public class BerserkAbility : BeyAbility
    {
        [Header("Berserk")]
        [SerializeField] private float speedMultiplier = 2.2f;
        [SerializeField] private float massDelta = -0.4f;  // lighter = faster, more momentum
        [SerializeField] private float duration = 5f;

        private void OnEnable()
        {
            abilityName = "Berserk";
            description = "Enter a frenzied state, dramatically increasing speed at the cost of stability.";
            manaCost = 50f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null)
                return;

            AbilityRuntimeEffects fx = AbilityRuntimeEffects.GetOrCreate(beyController);
            if (fx == null) return;

            // Apply mass delta to indirectly boost speed via lighter Rigidbody
            fx.ApplyTempMassBoost(massDelta, duration);

            // Spawn the visual berserk aura
            BerserkRuntime.Apply(beyController, speedMultiplier, duration);

            Debug.Log("[Ability] Berserk!");
        }
    }

    public class BerserkRuntime : MonoBehaviour
    {
        private BeyMovementController controller;
        private float multiplier;
        private float timer;
        private float originalSpeed;
        private bool applied;

        public static void Apply(BeyMovementController ctrl, float speedMult, float dur)
        {
            if (ctrl == null) return;
            BerserkRuntime existing = ctrl.GetComponent<BerserkRuntime>();
            if (existing != null) { existing.timer = Mathf.Max(existing.timer, dur); return; }

            BerserkRuntime b = ctrl.gameObject.AddComponent<BerserkRuntime>();
            b.controller = ctrl;
            b.multiplier = speedMult;
            b.timer = dur;
            b.SpawnAura();
        }

        private void Start()
        {
            if (controller == null || controller.Rb == null) return;
            controller.Rb.linearVelocity *= multiplier;
            applied = true;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) Destroy(this);
        }

        private void SpawnAura()
        {
            GameObject aura = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            aura.name = "BerserkAura";
            aura.transform.SetParent(transform, false);
            aura.transform.localScale = Vector3.one * 1.6f;
            aura.transform.localPosition = Vector3.zero;

            Collider col = aura.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Renderer rend = aura.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(1f, 0.12f, 0f, 0.35f);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(2f, 0.2f, 0f));
                }
                rend.material = mat;
            }

            Destroy(aura, timer);
        }
    }
}
