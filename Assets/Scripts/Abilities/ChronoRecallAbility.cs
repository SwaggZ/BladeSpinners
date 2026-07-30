using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "ChronoRecallAbility", menuName = "BladeSpinners/Abilities/ChronoRecall")]
    public class ChronoRecallAbility : BeyAbility
    {
        private readonly float shadowDuration = 8f;
        private readonly float replayDelay = 3f;

        private void OnEnable()
        {
            abilityName = "Chrono Recall";
            description = "Create a delayed temporal shadow, then recast to rewind to its position with partial momentum.";
            manaCost = 80f;
            rarity = Core.AbilityRarity.Legendary;
        }

        public override void Activate(BeyMovementController beyController)
        {
            // Recast: teleport back to shadow
            ChronoRecallRuntime existing = beyController.GetComponent<ChronoRecallRuntime>();
            if (existing != null && existing.IsActive)
            {
                existing.RecallToShadow();
                return;
            }

            // First cast: start recording + spawn shadow
            ChronoRecallRuntime rt = beyController.gameObject.AddComponent<ChronoRecallRuntime>();
            rt.Init(beyController, shadowDuration, replayDelay);
        }
    }

    /* ------------------------------------------------------------------ */
    /*  Runtime: records bey path, drives shadow 3 s behind, handles warp */
    /* ------------------------------------------------------------------ */
    public class ChronoRecallRuntime : MonoBehaviour
    {
        private BeyMovementController controller;
        private float duration;
        private float delay;
        private float startTime;
        private const float RecordInterval = 0.04f; // 25 Hz

        private readonly List<Vector3> history = new List<Vector3>(256);
        private GameObject shadow;
        private bool isActive;
        private bool isRecalling;

        public bool IsActive => isActive && !isRecalling;

        /* ---- setup ---- */
        public void Init(BeyMovementController ctrl, float dur, float del)
        {
            controller = ctrl;
            duration   = dur;
            delay      = del;
            startTime  = Time.time;
            isActive   = true;

            history.Add(transform.position);
            SpawnShadow();
        }

        /* ---- tick ---- */
        private float nextRecord;
        void Update()
        {
            if (!isActive || isRecalling) return;

            // record position
            if (Time.time >= nextRecord)
            {
                history.Add(transform.position);
                nextRecord = Time.time + RecordInterval;
            }

            // move shadow along recorded path, delayed
            float elapsed = Time.time - startTime;
            if (elapsed >= delay && shadow != null)
            {
                float shadowTime  = elapsed - delay;
                float rawIdx      = shadowTime / RecordInterval;
                int   idx         = Mathf.FloorToInt(rawIdx);
                idx = Mathf.Clamp(idx, 0, history.Count - 1);
                int nextIdx = Mathf.Min(idx + 1, history.Count - 1);
                float t = rawIdx - idx;
                shadow.transform.position = Vector3.Lerp(history[idx], history[nextIdx], t);
            }

            // timeout
            if (elapsed >= duration) Cleanup();
        }

        /* ---- recast: smooth warp ---- */
        public void RecallToShadow()
        {
            if (!isActive || isRecalling || shadow == null) return;
            isRecalling = true;
            StartCoroutine(SmoothRecall());
        }

        private IEnumerator SmoothRecall()
        {
            Vector3 target = shadow.transform.position;
            Vector3 start  = transform.position;

            SpawnRewindTrail(start, target);

            // freeze physics during warp
            Rigidbody rb = controller.Rb;
            Vector3 savedVel = rb != null ? rb.linearVelocity : Vector3.zero;
            if (rb != null) rb.isKinematic = true;

            // smooth ease warp (0.25 s)
            const float warpTime = 0.25f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / warpTime;
                float s = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(start, target, s);
                yield return null;
            }
            transform.position = target;

            // restore physics with partial momentum
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = savedVel * 0.5f;
            }

            SpawnArrivalFlash(target);
            Cleanup();
        }

        /* ================================================================ */
        /*                         V F X                                    */
        /* ================================================================ */

        /* --- ghostly afterimage sphere + trail wisps --- */
        private void SpawnShadow()
        {
            shadow = new GameObject("ChronoShadow");
            shadow.transform.position = transform.position;

            // Core ghost sphere
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "ShadowCore";
            core.transform.SetParent(shadow.transform, false);
            core.transform.localScale = Vector3.one * 0.9f;
            DisableCollider(core);
            ApplyGhostMat(core, new Color(0.3f, 0.4f, 1f, 0.2f), new Color(0.5f, 0.7f, 3f));
            core.AddComponent<ChronoShadowPulse>();

            // Outer time-energy haze
            GameObject haze = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            haze.name = "ShadowHaze";
            haze.transform.SetParent(shadow.transform, false);
            haze.transform.localScale = Vector3.one * 1.4f;
            DisableCollider(haze);
            ApplyGhostMat(haze, new Color(0.15f, 0.1f, 0.5f, 0.08f), new Color(0.2f, 0.15f, 1.2f));
            haze.AddComponent<ChronoShadowPulse>().inverse = true;

            // Swirling time wisps (4 small orbs orbiting the core)
            for (int i = 0; i < 4; i++)
            {
                GameObject wisp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                wisp.name = "TimeWisp";
                wisp.transform.SetParent(shadow.transform, false);
                wisp.transform.localScale = Vector3.one * 0.08f;
                DisableCollider(wisp);
                ApplyGhostMat(wisp, new Color(0.6f, 0.7f, 1f, 0.5f), new Color(1f, 1.5f, 4f));
                wisp.AddComponent<ChronoWispOrbit>().Init(i * 90f, 0.35f);
            }

            // Ground clock-ring (thin cylinder for time motif)
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "TimeRing";
            ring.transform.SetParent(shadow.transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            ring.transform.localScale = new Vector3(1.6f, 0.015f, 1.6f);
            DisableCollider(ring);
            ApplyGhostMat(ring, new Color(0.2f, 0.3f, 0.9f, 0.15f), new Color(0.3f, 0.5f, 2f));
            ring.AddComponent<ChronoRingSpin>();

            Object.Destroy(shadow, duration + 0.5f);
        }

        /* --- rewind streak trail between start and target --- */
        private void SpawnRewindTrail(Vector3 from, Vector3 to)
        {
            int count = 12;
            for (int i = 0; i < count; i++)
            {
                GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dot.name = "RewindDot";
                float pct = (float)i / (count - 1);
                dot.transform.position = Vector3.Lerp(from, to, pct) +
                                         Random.insideUnitSphere * 0.08f;
                dot.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.04f, pct);
                DisableCollider(dot);
                // blue-to-purple gradient
                Color c = Color.Lerp(new Color(0.3f, 0.5f, 1f), new Color(0.6f, 0.2f, 1f), pct);
                ApplyGhostMat(dot, new Color(c.r, c.g, c.b, 0.6f), c * 3f);
                dot.AddComponent<RewindDotFade>().Init(0.4f + pct * 0.3f);
                Object.Destroy(dot, 1f);
            }
        }

        /* --- arrival time-burst --- */
        private void SpawnArrivalFlash(Vector3 pos)
        {
            // Expanding ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ChronoArrival";
            ring.transform.position = pos + Vector3.down * 0.2f;
            ring.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);
            DisableCollider(ring);
            ApplyGhostMat(ring, new Color(0.4f, 0.5f, 1f, 0.5f), new Color(1f, 1.5f, 5f));
            ring.AddComponent<ChronoArrivalExpand>();
            Object.Destroy(ring, 0.6f);

            // Burst sparks
            for (int i = 0; i < 8; i++)
            {
                GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spark.name = "ChronoSpark";
                spark.transform.position = pos;
                spark.transform.localScale = new Vector3(0.04f, 0.04f, 0.15f);
                spark.transform.rotation = Quaternion.Euler(Random.Range(-30f, 30f), i * 45f, 0f);
                DisableCollider(spark);
                ApplyGhostMat(spark, new Color(0.5f, 0.6f, 1f, 0.7f), new Color(2f, 2.5f, 6f));
                spark.AddComponent<ChronoSparkBurst>().Init(i * 45f);
                Object.Destroy(spark, 0.5f);
            }
        }

        /* ---- Cleanup ---- */
        private void Cleanup()
        {
            isActive = false;
            if (shadow != null) Object.Destroy(shadow);
            Object.Destroy(this);
        }

        /* ================================================================ */
        /*                 Shared material helper                           */
        /* ================================================================ */
        private static void ApplyGhostMat(GameObject obj, Color baseColor, Color emission)
        {
            DBZAuraHelper.ApplyTransparentMat(obj, baseColor, emission);
        }

        private static void DisableCollider(GameObject obj)
        {
            Collider c = obj.GetComponent<Collider>();
            if (c != null) c.enabled = false;
        }
    }

    /* ================================================================ */
    /*                    VFX helper MonoBehaviours                      */
    /* ================================================================ */

    /* Shadow core pulsing (ghostly breathe) */
    public class ChronoShadowPulse : MonoBehaviour
    {
        public bool inverse;
        private Vector3 baseScale;
        void Start() { baseScale = transform.localScale; }
        void Update()
        {
            float t = Time.time * (inverse ? 3.5f : 4.5f);
            float s = inverse
                ? Mathf.Lerp(1.3f, 1.6f, (Mathf.Sin(t) + 1f) * 0.5f)
                : Mathf.Lerp(0.9f, 1.1f, (Mathf.Sin(t) + 1f) * 0.5f);
            transform.localScale = baseScale * s;
        }
    }

    /* Wisps orbiting the shadow */
    public class ChronoWispOrbit : MonoBehaviour
    {
        private float angle;
        private float radius;
        private float speed;
        private float bobOffset;

        public void Init(float startAngle, float orbitRadius)
        {
            angle = startAngle;
            radius = orbitRadius;
            speed = Random.Range(180f, 280f);
            bobOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        void Update()
        {
            angle += speed * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;
            float y = Mathf.Sin(Time.time * 3f + bobOffset) * 0.15f;
            transform.localPosition = new Vector3(
                Mathf.Cos(rad) * radius,
                y,
                Mathf.Sin(rad) * radius
            );
        }
    }

    /* Ground ring slow spin */
    public class ChronoRingSpin : MonoBehaviour
    {
        void Update()
        {
            transform.Rotate(Vector3.up, 45f * Time.deltaTime, Space.Self);
        }
    }

    /* Rewind trail dots fade out */
    public class RewindDotFade : MonoBehaviour
    {
        private float life;
        private float timer;
        private Vector3 baseScale;
        private Renderer rend;

        public void Init(float duration) { life = duration; }
        void Start()
        {
            baseScale = transform.localScale;
            rend = GetComponent<Renderer>();
        }
        void Update()
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / life);
            transform.localScale = baseScale * (1f - t * 0.7f);
            if (rend != null)
            {
                Color c = rend.material.color;
                c.a = Mathf.Lerp(0.6f, 0f, t);
                rend.material.color = c;
            }
        }
    }

    /* Arrival ring expand */
    public class ChronoArrivalExpand : MonoBehaviour
    {
        private float timer;
        void Update()
        {
            timer += Time.deltaTime;
            float t = timer / 0.5f;
            float scale = Mathf.Lerp(0.5f, 3f, t);
            transform.localScale = new Vector3(scale, 0.02f, scale);
            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                Color c = r.material.color;
                c.a = Mathf.Lerp(0.5f, 0f, t);
                r.material.color = c;
            }
        }
    }

    /* Arrival spark burst outward */
    public class ChronoSparkBurst : MonoBehaviour
    {
        private float angle;
        private float timer;
        private float speed;

        public void Init(float dir) { angle = dir; speed = Random.Range(3f, 5f); }
        void Update()
        {
            timer += Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;
            transform.position += new Vector3(Mathf.Cos(rad), 0.5f, Mathf.Sin(rad)) * speed * Time.deltaTime;
            float t = timer / 0.4f;
            transform.localScale = Vector3.one * Mathf.Lerp(0.15f, 0.02f, t);
        }
    }
}
