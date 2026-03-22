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
            // --- Inner wave ring (fast, bright) ---
            GameObject innerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            innerRing.name = "WaveInnerRing";
            innerRing.transform.position = center;
            innerRing.transform.localScale = new Vector3(0.3f, 0.05f, 0.3f);
            DisableCollider(innerRing);
            ApplyWaveMaterial(innerRing, new Color(0.3f, 0.65f, 1f, 0.6f), new Color(0.2f, 0.8f, 3f));
            WaveExpandRuntime.Spawn(innerRing, r * 0.7f, 0.3f);

            // --- Outer wave ring (slower, wider, more transparent) ---
            GameObject outerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerRing.name = "WaveOuterRing";
            outerRing.transform.position = center;
            outerRing.transform.localScale = new Vector3(0.3f, 0.03f, 0.3f);
            DisableCollider(outerRing);
            ApplyWaveMaterial(outerRing, new Color(0.15f, 0.45f, 0.9f, 0.35f), new Color(0f, 0.4f, 1.5f));
            WaveExpandRuntime.Spawn(outerRing, r, 0.5f);

            // --- Central splash column ---
            GameObject splash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            splash.name = "WaveSplash";
            splash.transform.position = center + Vector3.up * 0.1f;
            splash.transform.localScale = new Vector3(0.8f, 1.8f, 0.8f);
            DisableCollider(splash);
            ApplyWaveMaterial(splash, new Color(0.4f, 0.7f, 1f, 0.4f), new Color(0.3f, 0.9f, 2f));
            splash.AddComponent<WaveSplashFade>().Init(0.5f);
            Object.Destroy(splash, 0.6f);

            // --- Water droplet particles arcing outward ---
            for (int i = 0; i < 10; i++)
            {
                GameObject droplet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                droplet.name = "WaveDroplet";
                droplet.transform.position = center + Vector3.up * 0.3f;
                droplet.transform.localScale = Vector3.one * Random.Range(0.06f, 0.14f);
                DisableCollider(droplet);
                ApplyWaveMaterial(droplet, new Color(0.5f, 0.8f, 1f, 0.7f), new Color(0.5f, 1.2f, 2.5f));
                float angle = i * 36f * Mathf.Deg2Rad + Random.Range(-0.2f, 0.2f);
                Vector3 dir = new Vector3(Mathf.Cos(angle), Random.Range(0.5f, 1.2f), Mathf.Sin(angle));
                droplet.AddComponent<WaveDropletArc>().Init(dir * Random.Range(3f, 7f));
                Object.Destroy(droplet, 0.8f);
            }

            // --- Rising mist wisps ---
            for (int i = 0; i < 6; i++)
            {
                GameObject mist = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mist.name = "WaveMist";
                float a = Random.Range(0f, Mathf.PI * 2f);
                float rd = Random.Range(0.5f, r * 0.4f);
                mist.transform.position = center + new Vector3(Mathf.Cos(a) * rd, 0f, Mathf.Sin(a) * rd);
                mist.transform.localScale = Vector3.one * Random.Range(0.3f, 0.6f);
                DisableCollider(mist);
                ApplyWaveMaterial(mist, new Color(0.6f, 0.85f, 1f, 0.15f), new Color(0.1f, 0.3f, 0.6f));
                mist.AddComponent<WaveMistRise>();
                Object.Destroy(mist, 1.2f);
            }
        }

        private static void DisableCollider(GameObject obj)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        private static void ApplyWaveMaterial(GameObject obj, Color baseColor, Color emissionColor)
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
            float y = transform.localScale.y;
            transform.localScale = new Vector3(s, y, s);
        }
    }

    public class WaveSplashFade : MonoBehaviour
    {
        private float timer;
        private float elapsed;
        private Vector3 startScale;
        public void Init(float duration) { timer = duration; startScale = transform.localScale; }
        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / timer);
            float scaleY = startScale.y * (1f + t * 0.5f);
            float scaleXZ = startScale.x * (1f - t * 0.6f);
            transform.localScale = new Vector3(Mathf.Max(scaleXZ, 0.1f), scaleY, Mathf.Max(scaleXZ, 0.1f));
            transform.position += Vector3.up * Time.deltaTime * 2f;
        }
    }

    public class WaveDropletArc : MonoBehaviour
    {
        private Vector3 velocity;
        public void Init(Vector3 vel) { velocity = vel; }
        private void Update()
        {
            velocity += Vector3.down * 12f * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            float s = transform.localScale.x * (1f - Time.deltaTime * 1.5f);
            transform.localScale = Vector3.one * Mathf.Max(s, 0.02f);
        }
    }

    public class WaveMistRise : MonoBehaviour
    {
        private float speed;
        private void Awake() { speed = Random.Range(0.5f, 1.2f); }
        private void Update()
        {
            transform.position += Vector3.up * speed * Time.deltaTime;
            float s = transform.localScale.x * (1f - Time.deltaTime * 0.6f);
            transform.localScale = Vector3.one * Mathf.Max(s, 0.05f);
        }
    }
}
