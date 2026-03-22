using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "EarthquakeAbility", menuName = "Blade Spinners/Abilities/Earthquake")]
    public class EarthquakeAbility : BeyAbility
    {
        [Header("Earthquake")]
        [SerializeField] private float radius = 9f;
        [SerializeField] private float damagePerWave = 8f;
        [SerializeField] private float waveCount = 3;
        [SerializeField] private float waveInterval = 0.5f;
        [SerializeField] private float knockbackImpulse = 8f;

        private void OnEnable()
        {
            abilityName = "Earthquake";
            description = "Shatters the ground in successive shockwaves that stagger and damage enemies.";
            manaCost = 75f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null) return;
            EarthquakeRuntime.Spawn(beyController.transform.position, beyController.BeyConfiguration,
                radius, damagePerWave, (int)waveCount, waveInterval, knockbackImpulse);
            Debug.Log("[Ability] Earthquake!");
        }
    }

    public class EarthquakeRuntime : MonoBehaviour
    {
        private BeyConfiguration ownerConfig;
        private float radius, damage, knockback;
        private int wavesLeft;
        private float interval, timer;

        public static void Spawn(Vector3 pos, BeyConfiguration owner, float r, float d, int waves, float interval, float kb)
        {
            GameObject obj = new GameObject("Earthquake");
            obj.transform.position = pos;
            EarthquakeRuntime eq = obj.AddComponent<EarthquakeRuntime>();
            eq.ownerConfig = owner; eq.radius = r; eq.damage = d;
            eq.wavesLeft = waves; eq.interval = interval; eq.knockback = kb;
            eq.timer = 0f;
            Object.Destroy(obj, waves * interval + 0.5f);
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f || wavesLeft <= 0) return;
            wavesLeft--;
            timer = interval;
            DoWave();
        }

        private void DoWave()
        {
            BeyMovementController[] beys = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);
            foreach (BeyMovementController bey in beys)
            {
                if (bey == null || bey.BeyConfiguration == null || bey.BeyConfiguration == ownerConfig) continue;
                if (bey.BeyConfiguration.IsEnemy == ownerConfig.IsEnemy) continue;
                float dist = Vector3.Distance(transform.position, bey.transform.position);
                if (dist > radius) continue;
                float falloff = 1f - (dist / radius);
                bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - damage * falloff);
                Rigidbody rb = bey.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.AddForce(Vector3.up * knockback * falloff * 0.5f + (bey.transform.position - transform.position).normalized * knockback * falloff, ForceMode.Impulse);
            }
            SpawnWaveVisual();
        }

        private void SpawnWaveVisual()
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "QuakeWave";
            ring.transform.position = transform.position;
            ring.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);
            Collider c = ring.GetComponent<Collider>(); if (c != null) c.enabled = false;
            Renderer r = ring.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.6f, 0.4f, 0.15f, 0.5f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1.2f, 0.8f, 0.2f)); }
                r.material = mat;
            }
            WaveExpandRuntime.Spawn(ring, radius, 0.35f);

            // Rock debris
            for (int i = 0; i < 5; i++)
            {
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "QuakeRock";
                float a = Random.Range(0f, Mathf.PI * 2f);
                float d = Random.Range(0.5f, radius * 0.4f);
                rock.transform.position = transform.position + new Vector3(Mathf.Cos(a) * d, 0.1f, Mathf.Sin(a) * d);
                rock.transform.localScale = Vector3.one * Random.Range(0.1f, 0.25f);
                rock.transform.rotation = Random.rotation;
                Collider rc = rock.GetComponent<Collider>(); if (rc != null) rc.enabled = false;
                Renderer rr = rock.GetComponent<Renderer>();
                if (rr != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                    mat.color = new Color(0.45f, 0.35f, 0.2f);
                    rr.material = mat;
                }
                Rigidbody rb = rock.AddComponent<Rigidbody>();
                rb.mass = 0.1f;
                rb.linearVelocity = new Vector3(Random.Range(-2f, 2f), Random.Range(2f, 5f), Random.Range(-2f, 2f));
                Object.Destroy(rock, 0.8f);
            }
        }
    }
}
