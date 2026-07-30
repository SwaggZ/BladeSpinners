using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "TornadoAbility", menuName = "Blade Spinners/Abilities/Tornado")]
    public class TornadoAbility : BeyAbility
    {
        [Header("Tornado")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float tornadoRadius = 4f;
        [SerializeField] private float damagePerSecond = 8f;
        [SerializeField] private float liftForce = 12f;
        [SerializeField] private float duration = 4f;

        private void OnEnable()
        {
            abilityName = "Tornado";
            description = "Launches a moving tornado that catches enemies, lifts them, and shreds spin.";
            manaCost = 75f;
            rarity = Core.AbilityRarity.Legendary;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null) return;

            Vector3 forward = beyController.Rb != null && beyController.Rb.linearVelocity.sqrMagnitude > 0.5f
                ? beyController.Rb.linearVelocity.normalized : beyController.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();

            TornadoRuntime.Spawn(beyController.transform.position, forward, beyController,
                moveSpeed, tornadoRadius, damagePerSecond, liftForce, duration);
            Debug.Log("[Ability] Tornado!");
        }
    }

    public class TornadoRuntime : MonoBehaviour
    {
        private BeyMovementController owner;
        private Vector3 moveDir;
        private float speed, radius, dps, lift, timer;
        private float tickTimer;

        public static void Spawn(Vector3 pos, Vector3 dir, BeyMovementController ownerController,
            float spd, float rad, float dps, float lift, float dur)
        {
            GameObject obj = new GameObject("Tornado");
            obj.transform.position = pos;
            TornadoRuntime t = obj.AddComponent<TornadoRuntime>();
            t.owner = ownerController; t.moveDir = dir; t.speed = spd;
            t.radius = rad; t.dps = dps; t.lift = lift; t.timer = dur;
            t.CreateVisual(dur);
            Object.Destroy(obj, dur + 0.2f);
        }

        private void CreateVisual(float dur)
        {
            // Stacked spinning rings to form funnel
            for (int i = 0; i < 5; i++)
            {
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "TornadoLayer";
                ring.transform.SetParent(transform, false);
                float height = i * 0.5f;
                float scale = radius * 2f * (1f - i * 0.15f);
                ring.transform.localPosition = Vector3.up * height;
                ring.transform.localScale = new Vector3(scale, 0.02f, scale);
                Collider col = ring.GetComponent<Collider>(); if (col != null) col.enabled = false;
                Renderer rend = ring.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                    float a = 0.25f - i * 0.03f;
                    mat.color = new Color(0.55f, 0.55f, 0.55f, a);
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                    if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.4f, 0.4f, 0.5f)); }
                    rend.material = mat;
                }
                ring.AddComponent<TornadoLayerSpin>().Init(200f + i * 80f);
                Object.Destroy(ring, dur);
            }

            // Central column
            GameObject col2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            col2.name = "TornadoColumn";
            col2.transform.SetParent(transform, false);
            col2.transform.localPosition = Vector3.up * 1f;
            col2.transform.localScale = new Vector3(radius * 0.6f, 2f, radius * 0.6f);
            Collider cc = col2.GetComponent<Collider>(); if (cc != null) cc.enabled = false;
            Renderer cr = col2.GetComponent<Renderer>();
            if (cr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.5f, 0.5f, 0.5f, 0.12f);
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                cr.material = mat;
            }
            Object.Destroy(col2, dur);
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;

            transform.position += moveDir * speed * Time.deltaTime;

            tickTimer -= Time.deltaTime;
            if (tickTimer > 0f) return;
            tickTimer = 0.2f;

            foreach (BeyMovementController bey in
                     AbilityTargetQuery.FindUniqueBeysInRadius(
                         owner,
                         transform.position,
                         radius,
                         AbilityTargetRelation.Enemy))
            {
                float dist = Vector3.Distance(transform.position, bey.transform.position);
                float falloff = 1f - (dist / radius);
                bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - dps * 0.2f * falloff);
                Rigidbody rb = bey.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.AddForce(Vector3.up * lift * falloff, ForceMode.Force);
            }
        }
    }

    public class TornadoLayerSpin : MonoBehaviour
    {
        private float spinSpeed;
        public void Init(float speed) { spinSpeed = speed; }
        private void Update() { transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime); }
    }
}
