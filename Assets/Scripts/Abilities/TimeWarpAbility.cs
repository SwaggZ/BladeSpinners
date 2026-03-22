using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "TimeWarpAbility", menuName = "Blade Spinners/Abilities/Time Warp")]
    public class TimeWarpAbility : BeyAbility
    {
        [Header("Time Warp")]
        [SerializeField] private float radius = 10f;
        [SerializeField] private float slowFactor = 0.4f;
        [SerializeField] private float duration = 3f;

        private void OnEnable()
        {
            abilityName = "Time Warp";
            description = "Distorts time around you — all enemies in range move in slow motion.";
            manaCost = 70f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null) return;

            Vector3 origin = beyController.transform.position;
            BeyConfiguration ownerConfig = beyController.BeyConfiguration;
            BeyMovementController[] beys = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);

            foreach (BeyMovementController bey in beys)
            {
                if (bey == null || bey.BeyConfiguration == null || bey.BeyConfiguration == ownerConfig) continue;
                if (bey.BeyConfiguration.IsEnemy == ownerConfig.IsEnemy) continue;
                float dist = Vector3.Distance(origin, bey.transform.position);
                if (dist > radius) continue;
                TimeWarpSlowRuntime.Apply(bey, slowFactor, duration);
            }

            SpawnTimeVisual(origin, radius, duration);
            Debug.Log("[Ability] Time Warp!");
        }

        private void SpawnTimeVisual(Vector3 center, float r, float dur)
        {
            // Clock-like ring on the ground
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "TimeWarpField";
            ring.transform.position = center;
            ring.transform.localScale = new Vector3(r * 2f, 0.02f, r * 2f);
            Collider rc = ring.GetComponent<Collider>(); if (rc != null) rc.enabled = false;
            Renderer rr = ring.GetComponent<Renderer>();
            if (rr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.2f, 0.8f, 0.5f, 0.2f);
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.3f, 1.2f, 0.6f)); }
                rr.material = mat;
            }
            ring.AddComponent<TimeWarpFieldPulse>().Init(dur);
            Object.Destroy(ring, dur + 0.1f);

            // Floating clock hands
            for (int i = 0; i < 2; i++)
            {
                GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hand.name = "TimeHand";
                hand.transform.position = center + Vector3.up * 0.5f;
                float length = i == 0 ? 1.5f : 1f;
                hand.transform.localScale = new Vector3(0.06f, 0.02f, length);
                hand.transform.rotation = Quaternion.Euler(0f, i * 90f, 0f);
                Collider hc = hand.GetComponent<Collider>(); if (hc != null) hc.enabled = false;
                Renderer hr = hand.GetComponent<Renderer>();
                if (hr != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                    mat.color = new Color(0.3f, 1f, 0.6f, 0.6f);
                    if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.5f, 2f, 1f)); }
                    hr.material = mat;
                }
                float speed = i == 0 ? -30f : -120f;
                hand.AddComponent<TimeHandSpin>().Init(speed, dur);
                Object.Destroy(hand, dur + 0.1f);
            }
        }
    }

    public class TimeWarpSlowRuntime : MonoBehaviour
    {
        private Rigidbody rb;
        private float slowFactor;
        private float timer;

        public static void Apply(BeyMovementController ctrl, float factor, float dur)
        {
            if (ctrl == null) return;
            TimeWarpSlowRuntime ex = ctrl.GetComponent<TimeWarpSlowRuntime>();
            if (ex != null) { ex.timer = Mathf.Max(ex.timer, dur); return; }
            TimeWarpSlowRuntime s = ctrl.gameObject.AddComponent<TimeWarpSlowRuntime>();
            s.rb = ctrl.Rb;
            s.slowFactor = factor;
            s.timer = dur;
            if (s.rb != null) s.rb.linearVelocity *= factor;
        }

        private void FixedUpdate()
        {
            timer -= Time.fixedDeltaTime;
            if (timer <= 0f) { Destroy(this); return; }
            if (rb != null) rb.linearVelocity *= Mathf.Lerp(1f, slowFactor, 0.1f);
        }
    }

    public class TimeWarpFieldPulse : MonoBehaviour
    {
        private float timer;
        private float elapsed;
        private float baseScale;
        public void Init(float dur) { timer = dur; baseScale = transform.localScale.x; }
        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed > timer) return;
            float pulse = 1f + Mathf.Sin(elapsed * 3f) * 0.03f;
            float s = baseScale * pulse;
            transform.localScale = new Vector3(s, 0.02f, s);
        }
    }

    public class TimeHandSpin : MonoBehaviour
    {
        private float speed;
        private float timer;
        public void Init(float spd, float dur) { speed = spd; timer = dur; }
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            transform.Rotate(Vector3.up, speed * Time.deltaTime);
        }
    }
}
