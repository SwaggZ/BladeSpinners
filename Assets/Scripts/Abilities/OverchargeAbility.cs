using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "OverchargeAbility", menuName = "Blade Spinners/Abilities/Overcharge")]
    public class OverchargeAbility : BeyAbility
    {
        [Header("Overcharge")]
        [SerializeField] private float spinBoost = 40f;
        [SerializeField] private float manaDrainPerSecond = 15f;
        [SerializeField] private float duration = 5f;

        private void OnEnable()
        {
            abilityName = "Overcharge";
            description = "Supercharge your spin power beyond limits — but it drains mana rapidly.";
            manaCost = 30f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null) return;
            beyController.BeyConfiguration.SetSpin(beyController.BeyConfiguration.CurrentSpin + spinBoost);
            OverchargeRuntime.Apply(beyController, manaDrainPerSecond, duration);
            Debug.Log("[Ability] Overcharge!");
        }
    }

    public class OverchargeRuntime : MonoBehaviour
    {
        private BeyConfiguration config;
        private float manaDrain;
        private float timer;
        private float tickTimer;

        public static void Apply(BeyMovementController ctrl, float drain, float dur)
        {
            OverchargeRuntime ex = ctrl.GetComponent<OverchargeRuntime>();
            if (ex != null) { ex.timer = Mathf.Max(ex.timer, dur); return; }
            OverchargeRuntime oc = ctrl.gameObject.AddComponent<OverchargeRuntime>();
            oc.config = ctrl.BeyConfiguration;
            oc.manaDrain = drain;
            oc.timer = dur;
            SpawnVisual(ctrl, dur);
        }

        private static void SpawnVisual(BeyMovementController ctrl, float dur)
        {
            // DBZ charging aura (electric yellow-white)
            DBZAuraHelper.Spawn(
                ctrl.transform, dur,
                new Color(1f, 1f, 0.3f),   // bright yellow core
                new Color(1f, 1f, 0.7f),    // white-yellow outer
                4f
            );

            // Sparking arcs (on top of aura)
            for (int i = 0; i < 3; i++)
            {
                GameObject arc = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arc.name = "OverchargeArc";
                arc.transform.SetParent(ctrl.transform, false);
                arc.transform.localPosition = Random.onUnitSphere * 0.5f;
                arc.transform.localScale = new Vector3(0.03f, 0.03f, 0.5f);
                arc.transform.localRotation = Random.rotation;
                Collider ac = arc.GetComponent<Collider>(); if (ac != null) ac.enabled = false;
                DBZAuraHelper.ApplyTransparentMat(arc, new Color(1f, 1f, 0.5f, 0.7f), new Color(4f, 4f, 1f));
                arc.AddComponent<OverchargeArcJitter>().Init(dur);
                Object.Destroy(arc, dur + 0.1f);
            }
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) { Destroy(this); return; }
            tickTimer -= Time.deltaTime;
            if (tickTimer > 0f) return;
            tickTimer = 0.25f;
            if (config != null)
                config.SetMana(Mathf.Max(0f, config.CurrentMana - manaDrain * 0.25f));
        }
    }

    public class OverchargePulse : MonoBehaviour
    {
        private float timer, elapsed, baseScale;
        public void Init(float dur) { timer = dur; baseScale = transform.localScale.x; }
        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed > timer) return;
            float pulse = 1f + Mathf.Sin(elapsed * 10f) * 0.08f;
            transform.localScale = Vector3.one * baseScale * pulse;
        }
    }

    public class OverchargeArcJitter : MonoBehaviour
    {
        private float timer;
        private float jitterTimer;
        public void Init(float dur) { timer = dur; }
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            jitterTimer -= Time.deltaTime;
            if (jitterTimer > 0f) return;
            jitterTimer = Random.Range(0.05f, 0.15f);
            transform.localPosition = Random.onUnitSphere * Random.Range(0.3f, 0.7f);
            transform.localRotation = Random.rotation;
        }
    }
}
