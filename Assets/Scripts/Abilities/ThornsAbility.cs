using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "ThornsAbility", menuName = "Blade Spinners/Abilities/Thorns")]
    public class ThornsAbility : BeyAbility
    {
        [Header("Thorns")]
        [SerializeField] private float reflectRatio = 0.6f;   // fraction of received spin damage reflected back
        [SerializeField] private float bonusDamage = 8f;       // extra flat damage on each reflect
        [SerializeField] private float duration = 4f;

        private void OnEnable()
        {
            abilityName = "Thorns";
            description = "Envelop the bey in rotating spikes. Any enemy that clashes with you takes reflected spin damage.";
            manaCost = 45f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null)
                return;

            ThornsRuntime.Apply(beyController, reflectRatio, bonusDamage, duration);
            Debug.Log("[Ability] Thorns active!");
        }
    }

    public class ThornsRuntime : MonoBehaviour
    {
        public float ReflectRatio { get; private set; }
        public float BonusDamage { get; private set; }
        private float timer;
        private BeyConfiguration config;

        public static void Apply(BeyMovementController ctrl, float reflectRatio, float bonus, float dur)
        {
            if (ctrl == null) return;
            ThornsRuntime existing = ctrl.GetComponent<ThornsRuntime>();
            if (existing != null) { existing.timer = Mathf.Max(existing.timer, dur); return; }

            ThornsRuntime t = ctrl.gameObject.AddComponent<ThornsRuntime>();
            t.ReflectRatio = reflectRatio;
            t.BonusDamage = bonus;
            t.timer = dur;
            t.config = ctrl.BeyConfiguration;
            t.SpawnVisual();
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) Destroy(this);
        }

        /// <summary>
        /// Call this from BeyCollisionDetector when an enemy collides with a thorn-protected bey.
        /// Reflects a portion of the impact spin damage back to the attacker.
        /// </summary>
        public void TriggerReflect(BeyConfiguration attacker, float incomingDamage)
        {
            if (attacker == null || timer <= 0f) return;
            float reflected = incomingDamage * ReflectRatio + BonusDamage;
            attacker.SetSpin(attacker.CurrentSpin - reflected);
        }

        private void SpawnVisual()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "ThornsShield";
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * 1.5f;

            Collider col = visual.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.2f, 0.75f, 0.1f, 0.4f);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.1f, 1.2f, 0.05f));
                }
                rend.material = mat;
            }

            Destroy(visual, timer);
        }
    }
}
