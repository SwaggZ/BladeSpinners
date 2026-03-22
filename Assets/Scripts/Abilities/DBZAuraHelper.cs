using UnityEngine;

namespace BladeSpinners.Abilities
{
    /// <summary>
    /// Shared helper that spawns a Dragon Ball Z-style charging aura on any transform.
    /// Multi-layer effect: pulsing inner/outer spheres, rising energy streaks, ground ring.
    /// Half-transparent with upward flowing energy — like a power-up charging wave.
    /// </summary>
    public static class DBZAuraHelper
    {
        /// <summary>
        /// Spawns a full DBZ-style aura attached to the given transform.
        /// </summary>
        /// <param name="parent">Transform to attach aura to (typically the bey)</param>
        /// <param name="duration">How long the aura lasts</param>
        /// <param name="coreColor">Primary aura color (e.g. red for Berserk)</param>
        /// <param name="outerColor">Secondary/outer aura color (usually a lighter tint)</param>
        /// <param name="emissionIntensity">Emission multiplier (higher = brighter glow)</param>
        public static void Spawn(Transform parent, float duration, Color coreColor, Color outerColor, float emissionIntensity = 3f)
        {
            if (parent == null) return;

            // ── Inner core sphere: bright, pulsing tight to bey ──
            GameObject inner = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            inner.name = "DBZAuraCore";
            inner.transform.SetParent(parent, false);
            inner.transform.localPosition = Vector3.zero;
            inner.transform.localScale = Vector3.one * 1.4f;
            DisableCollider(inner);
            ApplyTransparentMat(inner, WithAlpha(coreColor, 0.35f), coreColor * emissionIntensity);
            inner.AddComponent<DBZAuraCorePulse>().Init(duration, 1.2f, 1.6f, 6f);
            Object.Destroy(inner, duration + 0.1f);

            // ── Outer flowing sphere: larger, more transparent, counter-pulse ──
            GameObject outer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            outer.name = "DBZAuraOuter";
            outer.transform.SetParent(parent, false);
            outer.transform.localPosition = Vector3.zero;
            outer.transform.localScale = Vector3.one * 2.0f;
            DisableCollider(outer);
            ApplyTransparentMat(outer, WithAlpha(outerColor, 0.15f), outerColor * (emissionIntensity * 0.5f));
            outer.AddComponent<DBZAuraCorePulse>().Init(duration, 1.8f, 2.3f, 4f, true);
            Object.Destroy(outer, duration + 0.1f);

            // ── Rising energy streaks: 8 tall thin cubes flowing upward like flames ──
            for (int i = 0; i < 8; i++)
            {
                GameObject streak = GameObject.CreatePrimitive(PrimitiveType.Cube);
                streak.name = "DBZAuraStreak";
                streak.transform.SetParent(parent, false);
                float angle = i * 45f * Mathf.Deg2Rad;
                float radius = Random.Range(0.35f, 0.65f);
                streak.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Random.Range(-0.4f, 0.2f),
                    Mathf.Sin(angle) * radius
                );
                float height = Random.Range(0.4f, 0.8f);
                streak.transform.localScale = new Vector3(0.07f, height, 0.07f);
                streak.transform.localRotation = Quaternion.Euler(
                    Random.Range(-10f, 10f), angle * Mathf.Rad2Deg, Random.Range(-10f, 10f)
                );
                DisableCollider(streak);
                float hueShift = Random.Range(-0.03f, 0.03f);
                Color streakCol = ShiftHue(coreColor, hueShift);
                ApplyTransparentMat(streak, WithAlpha(streakCol, 0.55f), streakCol * (emissionIntensity * 0.8f));
                streak.AddComponent<DBZAuraStreakRise>().Init(
                    duration,
                    Random.Range(1.5f, 3.0f),
                    radius,
                    angle
                );
                Object.Destroy(streak, duration + 0.1f);
            }

            // ── Ground energy ring ──
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "DBZAuraRing";
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            ring.transform.localScale = new Vector3(2.0f, 0.02f, 2.0f);
            DisableCollider(ring);
            ApplyTransparentMat(ring, WithAlpha(outerColor, 0.2f), outerColor * (emissionIntensity * 0.4f));
            ring.AddComponent<DBZAuraRingPulse>().Init(duration, 1.8f, 2.4f);
            Object.Destroy(ring, duration + 0.1f);
        }

        private static void DisableCollider(GameObject obj)
        {
            Collider c = obj.GetComponent<Collider>();
            if (c != null) c.enabled = false;
        }

        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private static Color ShiftHue(Color c, float shift)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            h = Mathf.Repeat(h + shift, 1f);
            return Color.HSVToRGB(h, s, v);
        }

        /// <summary>
        /// Applies a properly configured URP transparent material with emission.
        /// </summary>
        public static void ApplyTransparentMat(GameObject obj, Color baseColor, Color emissionColor)
        {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend == null) return;
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
            mat.color = baseColor;

            // URP transparency setup
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }

            rend.material = mat;
        }
    }

    /// <summary>
    /// Pulsing scale animation for aura spheres. Supports inverse mode for counter-pulsing.
    /// </summary>
    public class DBZAuraCorePulse : MonoBehaviour
    {
        private float timer, elapsed, minScale, maxScale, speed;
        private bool inverse;

        public void Init(float duration, float min, float max, float spd, bool inv = false)
        {
            timer = duration; minScale = min; maxScale = max; speed = spd; inverse = inv;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed > timer) return;
            float wave = (Mathf.Sin(elapsed * speed) + 1f) * 0.5f;
            if (inverse) wave = 1f - wave;
            float s = Mathf.Lerp(minScale, maxScale, wave);
            transform.localScale = Vector3.one * s;
        }
    }

    /// <summary>
    /// Rising flame streak that flows upward and resets to bottom — creates the DBZ flame effect.
    /// Includes slight wobble and scale taper as it rises.
    /// </summary>
    public class DBZAuraStreakRise : MonoBehaviour
    {
        private float timer, speed, orbitRadius, angleRad;
        private float resetY = -0.4f;
        private float maxY = 1.8f;
        private float baseScaleX, baseScaleZ;

        public void Init(float duration, float riseSpeed, float radius, float angle)
        {
            timer = duration;
            speed = riseSpeed;
            orbitRadius = radius;
            angleRad = angle;
            baseScaleX = transform.localScale.x;
            baseScaleZ = transform.localScale.z;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;

            // Rise upward
            Vector3 pos = transform.localPosition;
            pos.y += speed * Time.deltaTime;

            // Slight horizontal wobble
            float wobble = Mathf.Sin(Time.time * 8f + angleRad * 3f) * 0.05f;
            pos.x = Mathf.Cos(angleRad) * orbitRadius + wobble;
            pos.z = Mathf.Sin(angleRad) * orbitRadius + wobble;

            // Reset to bottom when reaching top
            if (pos.y > maxY)
            {
                pos.y = resetY;
                speed = Random.Range(1.5f, 3.0f); // randomize speed on each cycle
            }

            transform.localPosition = pos;

            // Taper: get thinner and shorter as it rises
            float heightT = Mathf.InverseLerp(resetY, maxY, pos.y);
            float taper = Mathf.Lerp(1f, 0.2f, heightT);
            float scaleY = transform.localScale.y * (1f - Time.deltaTime * 0.3f * heightT);
            transform.localScale = new Vector3(baseScaleX * taper, Mathf.Max(scaleY, 0.05f), baseScaleZ * taper);
        }
    }

    /// <summary>
    /// Pulsing ground ring for the aura base.
    /// </summary>
    public class DBZAuraRingPulse : MonoBehaviour
    {
        private float timer, elapsed, minScale, maxScale;

        public void Init(float duration, float min, float max)
        {
            timer = duration; minScale = min; maxScale = max;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed > timer) return;
            float wave = (Mathf.Sin(elapsed * 5f) + 1f) * 0.5f;
            float s = Mathf.Lerp(minScale, maxScale, wave);
            float y = transform.localScale.y;
            transform.localScale = new Vector3(s, y, s);
        }
    }
}
