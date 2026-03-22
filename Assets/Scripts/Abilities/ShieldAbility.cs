using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    /// <summary>
    /// Shield ability: temporarily reduces incoming spin damage and increases knockback resistance.
    /// Works by boosting the bey's effective weight for a few seconds.
    /// </summary>
    [CreateAssetMenu(fileName = "ShieldAbility", menuName = "Blade Spinners/Abilities/Shield")]
    public class ShieldAbility : BeyAbility
    {
        [Header("Shield Settings")]
        [SerializeField] private float duration = 3f;
        [SerializeField] private float weightBoost = 30f; // added to effective weight

        private void OnEnable()
        {
            abilityName = "Shield";
            description = "Temporarily increases weight, reducing spin damage taken.";
            manaCost = 50f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;

            AbilityRuntimeEffects runtime = AbilityRuntimeEffects.GetOrCreate(beyController);
            if (runtime == null)
                return;

            runtime.ApplyTempMassBoost(weightBoost * 0.1f, duration);
            SpawnShieldDome(beyController);

            Debug.Log($"[Ability] Shield activated! +{weightBoost} effective weight for {duration}s");
        }

        private void SpawnShieldDome(BeyMovementController ctrl)
        {
            // Golden translucent dome
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "ShieldDome";
            dome.transform.SetParent(ctrl.transform, false);
            dome.transform.localPosition = Vector3.zero;
            dome.transform.localScale = Vector3.one * 1.8f;
            Collider col = dome.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Renderer rend = dome.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(1f, 0.85f, 0.2f, 0.2f);
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1.5f, 1.2f, 0.2f)); }
                rend.material = mat;
            }
            dome.AddComponent<ShieldDomePulse>().Init(duration);
            Object.Destroy(dome, duration + 0.1f);

            // Hexagonal ground ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ShieldRing";
            ring.transform.SetParent(ctrl.transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            ring.transform.localScale = new Vector3(2f, 0.02f, 2f);
            Collider rc = ring.GetComponent<Collider>();
            if (rc != null) rc.enabled = false;
            Renderer rr = ring.GetComponent<Renderer>();
            if (rr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(1f, 0.9f, 0.3f, 0.35f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1.2f, 1f, 0.1f)); }
                rr.material = mat;
            }
            Object.Destroy(ring, duration + 0.1f);
        }
    }

    public class ShieldDomePulse : MonoBehaviour
    {
        private float timer;
        private float elapsed;
        private float baseScale;
        public void Init(float duration) { timer = duration; baseScale = transform.localScale.x; }
        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed > timer) return;
            float pulse = 1f + Mathf.Sin(elapsed * 4f) * 0.04f;
            transform.localScale = Vector3.one * baseScale * pulse;
        }
    }
}
