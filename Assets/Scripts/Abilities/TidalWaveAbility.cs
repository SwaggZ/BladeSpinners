using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "TidalWaveAbility", menuName = "Blade Spinners/Abilities/Tidal Wave")]
    public class TidalWaveAbility : BeyAbility
    {
        [Header("Tidal Wave")]
        [SerializeField] private float waveRadius = 9f;
        [SerializeField] private float knockbackImpulse = 18f;
        [SerializeField] private float spinDamage = 10f;
        [SerializeField] private float slowDuration = 2f;

        private void OnEnable()
        {
            abilityName = "Tidal Wave";
            description = "Unleashes a crashing wave that blasts all nearby enemies outward and slows them.";
            manaCost = 60f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null)
                return;

            Vector3 origin = beyController.transform.position;
            BeyConfiguration ownerConfig = beyController.BeyConfiguration;
            BeyMovementController[] beys = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);

            foreach (BeyMovementController bey in beys)
            {
                if (bey == null || bey.BeyConfiguration == null || bey.BeyConfiguration == ownerConfig) continue;
                if (bey.BeyConfiguration.IsEnemy == ownerConfig.IsEnemy) continue;

                Vector3 toEnemy = bey.transform.position - origin;
                float dist = toEnemy.magnitude;
                if (dist > waveRadius) continue;

                float falloff = 1f - (dist / waveRadius);
                bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - spinDamage * falloff);

                Rigidbody enemyRb = bey.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    Vector3 dir = dist > 0.01f ? toEnemy.normalized : Random.onUnitSphere;
                    dir.y = Mathf.Max(dir.y, 0.1f);
                    enemyRb.AddForce(dir.normalized * knockbackImpulse * falloff, ForceMode.Impulse);
                }

                WaveSlowRuntime.Apply(bey, slowDuration);
            }

            SpawnWaveVisual(origin, waveRadius);
            Debug.Log("[Ability] Tidal Wave!");
        }

        private void SpawnWaveVisual(Vector3 center, float r)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ring.name = "WaveRing";
            ring.transform.position = center;
            ring.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);

            Collider col = ring.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Renderer rend = ring.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.2f, 0.55f, 1f, 0.5f);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0f, 0.6f, 2f));
                }
                rend.material = mat;
            }

            WaveExpandRuntime.Spawn(ring, r, 0.4f);
        }
    }

    public class WaveSlowRuntime : MonoBehaviour
    {
        private Rigidbody rb;
        private float timer;

        public static void Apply(BeyMovementController ctrl, float dur)
        {
            if (ctrl == null) return;
            WaveSlowRuntime ex = ctrl.GetComponent<WaveSlowRuntime>();
            if (ex != null) { ex.timer = Mathf.Max(ex.timer, dur); return; }
            WaveSlowRuntime s = ctrl.gameObject.AddComponent<WaveSlowRuntime>();
            s.rb = ctrl.Rb;
            s.timer = dur;
        }

        private void FixedUpdate()
        {
            timer -= Time.fixedDeltaTime;
            if (timer <= 0f) { Destroy(this); return; }
            if (rb != null)
                rb.linearVelocity *= 0.88f;
        }
    }

    public class WaveExpandRuntime : MonoBehaviour
    {
        private float targetScale;
        private float expandTime;
        private float elapsed;

        public static void Spawn(GameObject obj, float maxRadius, float time)
        {
            WaveExpandRuntime w = obj.AddComponent<WaveExpandRuntime>();
            w.targetScale = maxRadius * 2f;
            w.expandTime = time;
            Object.Destroy(obj, time + 0.1f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / expandTime);
            float s = Mathf.Lerp(0.2f, targetScale, t);
            transform.localScale = new Vector3(s, 0.3f, s);
        }
    }
}
