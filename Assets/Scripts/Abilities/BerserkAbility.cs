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
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) Destroy(this);
        }

        private void SpawnAura()
        {
            DBZAuraHelper.Spawn(
                transform, timer,
                new Color(1f, 0.15f, 0f),   // fiery red-orange core
                new Color(1f, 0.5f, 0f),    // orange outer glow
                3.5f
            );
        }

        private static void DisableCollider(GameObject obj)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        private static void ApplyBerserkMat(GameObject obj, Color baseColor, Color emissionColor)
        {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend == null) return;
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
            mat.color = baseColor;
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }
            rend.material = mat;
        }
    }

    public class BerserkAuraPulse : MonoBehaviour
    {
        private float timer;
        private float elapsed;
        private float minScale;
        private float maxScale;
        public void Init(float duration, float min = 1.3f, float max = 1.7f) { timer = duration; minScale = min; maxScale = max; }
        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed > timer) return;
            float pulse = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(elapsed * 6f) + 1f) * 0.5f);
            float y = transform.localScale.y;
            transform.localScale = new Vector3(pulse, y < 0.1f ? y : pulse, pulse);
        }
    }

    public class BerserkStreakRise : MonoBehaviour
    {
        private float speed;
        private float timer;
        public void Init(float duration) { speed = Random.Range(0.8f, 1.8f); timer = duration; }
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            transform.localPosition += Vector3.up * speed * Time.deltaTime;
            if (transform.localPosition.y > 1.5f)
            {
                Vector3 p = transform.localPosition;
                p.y = -0.2f;
                transform.localPosition = p;
            }
        }
    }
}
