using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Core;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "RegenerationAbility", menuName = "Blade Spinners/Abilities/Regeneration")]
    public class RegenerationAbility : BeyAbility
    {
        [Header("Regeneration")]
        [SerializeField] private float healPerSecond = 8f;
        [SerializeField] private float duration = 5f;

        private void OnEnable()
        {
            abilityName = "Regeneration";
            description = "Gradually restores spin over time with soothing energy.";
            manaCost = 55f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null) return;
            RegenerationRuntime.Apply(beyController, healPerSecond, duration);
            Debug.Log("[Ability] Regeneration!");
        }
    }

    public class RegenerationRuntime : MonoBehaviour
    {
        private BeyConfiguration config;
        private float hps;
        private float timer;
        private float tickTimer;
        private GameObject visualObj;

        public static void Apply(BeyMovementController ctrl, float healPerSec, float dur)
        {
            RegenerationRuntime ex = ctrl.GetComponent<RegenerationRuntime>();
            if (ex != null) { ex.timer = Mathf.Max(ex.timer, dur); return; }
            RegenerationRuntime regen = ctrl.gameObject.AddComponent<RegenerationRuntime>();
            regen.config = ctrl.BeyConfiguration;
            regen.hps = healPerSec;
            regen.timer = dur;
            regen.SpawnVisual(ctrl, dur);
        }

        private void SpawnVisual(BeyMovementController ctrl, float dur)
        {
            // DBZ heal aura (soft green energy)
            DBZAuraHelper.Spawn(
                ctrl.transform, dur,
                new Color(0.2f, 0.9f, 0.3f),   // green core
                new Color(0.4f, 1f, 0.5f),      // light green outer
                2f
            );

            // Heal sparkles (on top of aura)
            for (int i = 0; i < 4; i++)
            {
                GameObject sparkle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sparkle.name = "RegenSparkle";
                sparkle.transform.SetParent(ctrl.transform, false);
                float a = i * 90f * Mathf.Deg2Rad;
                sparkle.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.6f, -0.2f, Mathf.Sin(a) * 0.6f);
                sparkle.transform.localScale = Vector3.one * 0.06f;
                Collider sc = sparkle.GetComponent<Collider>(); if (sc != null) sc.enabled = false;
                DBZAuraHelper.ApplyTransparentMat(sparkle, new Color(0.4f, 1f, 0.5f, 0.4f), new Color(1f, 3f, 1f));
                sparkle.AddComponent<RegenSparkleFloat>().Init(dur);
                Object.Destroy(sparkle, dur + 0.1f);
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
                config.SetSpin(Mathf.Min(config.CurrentSpin + hps * 0.25f, GameConstants.MAX_SPIN));
        }
    }

    public class RegenSparkleFloat : MonoBehaviour
    {
        private float speed;
        private float timer;
        public void Init(float dur) { speed = Random.Range(0.4f, 0.8f); timer = dur; }
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            transform.localPosition += Vector3.up * speed * Time.deltaTime;
            if (transform.localPosition.y > 1.2f)
            {
                Vector3 p = transform.localPosition;
                p.y = -0.2f;
                transform.localPosition = p;
            }
        }
    }
}
