using System.Collections.Generic;
using UnityEngine;
using BladeSpinners.Gameplay;

namespace BladeSpinners.Abilities
{
    public enum LaunchRating
    {
        Mishap,
        Good,
        Great,
        Perfect
    }

    /// <summary>
    /// Central procedural VFX engine for epic anime-style Beyblade ability animations,
    /// particle systems, shockwave meshes, elemental effects, and runtime textures.
    /// </summary>
    public static class EpicAbilityVFXHelper
    {
        private static Texture2D softGlowTexture;
        private static Texture2D sparkFlareTexture;
        private static Texture2D shockRingTexture;

        // ══════════════════════════════════════════════════════════════
        // TEXTURE GENERATION
        // ══════════════════════════════════════════════════════════════

        public static Texture2D GetSoftGlowTexture()
        {
            if (softGlowTexture != null) return softGlowTexture;

            const int size = 64;
            softGlowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "VFX_SoftGlow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxDist = size * 0.5f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float norm = Mathf.Clamp01(dist / maxDist);
                    // Smooth gaussian-style falloff with bright intense core
                    float alpha = Mathf.Pow(1f - norm, 2.2f);
                    softGlowTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            softGlowTexture.Apply();
            return softGlowTexture;
        }

        public static Texture2D GetSparkFlareTexture()
        {
            if (sparkFlareTexture != null) return sparkFlareTexture;

            const int size = 64;
            sparkFlareTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "VFX_SparkFlare",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxDist = size * 0.5f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = Mathf.Abs(x - center.x) / maxDist;
                    float dy = Mathf.Abs(y - center.y) / maxDist;
                    float cross = Mathf.Max(
                        Mathf.Pow(Mathf.Clamp01(1f - dx * 0.5f), 8f) * Mathf.Pow(Mathf.Clamp01(1f - dy * 4f), 2f),
                        Mathf.Pow(Mathf.Clamp01(1f - dy * 0.5f), 8f) * Mathf.Pow(Mathf.Clamp01(1f - dx * 4f), 2f)
                    );
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    float core = Mathf.Pow(Mathf.Clamp01(1f - dist), 3f);
                    float alpha = Mathf.Clamp01(cross + core);
                    sparkFlareTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            sparkFlareTexture.Apply();
            return sparkFlareTexture;
        }

        public static Texture2D GetShockRingTexture()
        {
            if (shockRingTexture != null) return shockRingTexture;

            const int size = 64;
            shockRingTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "VFX_ShockRing",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxDist = size * 0.5f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    // Sharp ring at radius ~0.7
                    float ring = 1f - Mathf.Abs(dist - 0.7f) / 0.25f;
                    float alpha = Mathf.Clamp01(Mathf.Pow(Mathf.Max(0f, ring), 2f));
                    shockRingTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            shockRingTexture.Apply();
            return shockRingTexture;
        }

        // ══════════════════════════════════════════════════════════════
        // MATERIAL CREATION
        // ══════════════════════════════════════════════════════════════

        public static Material CreateVFXMaterial(Color baseColor, Color emissionColor, Texture2D texture = null, bool additive = true)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");

            Material mat = new Material(shader);
            mat.name = "EpicVFX_Mat";

            if (texture != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", additive ? 1f : 0f); // Additive or Alpha
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);

            if (additive)
            {
                if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            }
            else
            {
                if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }

            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;
            return mat;
        }

        // ══════════════════════════════════════════════════════════════
        // SHOCKWAVE EXPANSION MESH
        // ══════════════════════════════════════════════════════════════

        public static void SpawnShockwaveRing(Transform parent, Vector3 center, Color color, float startRadius, float maxRadius, float duration, float extra = 0f)
        {
            GameObject ring = new GameObject("VFX_ShockwaveRing");
            if (parent != null) ring.transform.SetParent(parent, false);
            ring.transform.position = center + Vector3.up * 0.1f;

            MeshFilter mf = ring.AddComponent<MeshFilter>();
            MeshRenderer mr = ring.AddComponent<MeshRenderer>();

            Material mat = CreateVFXMaterial(color, color * 2.5f, GetShockRingTexture(), true);
            mr.material = mat;

            // Simple flat quad on XZ plane
            Mesh mesh = new Mesh { name = "ShockwaveQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-1f, 0f, -1f),
                new Vector3( 1f, 0f, -1f),
                new Vector3( 1f, 0f,  1f),
                new Vector3(-1f, 0f,  1f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mf.sharedMesh = mesh;

            ShockwaveAnimator anim = ring.AddComponent<ShockwaveAnimator>();
            anim.Initialize(startRadius, maxRadius, duration, mat, color);
            Object.Destroy(ring, duration + 0.1f);
        }

        public static void SpawnShockwaveRing(Vector3 center, Color color, float startRadius, float maxRadius, float duration)
        {
            SpawnShockwaveRing(null, center, color, startRadius, maxRadius, duration, 0f);
        }

        public static void SpawnInfernoPillar(Vector3 center, float radius = 1.5f)
        {
            GameObject root = new GameObject("VFX_InfernoPillar");
            root.transform.position = center;

            Color fireGold = new Color(1f, 0.88f, 0.2f, 1f);
            Color fireRed = new Color(1f, 0.25f, 0.05f, 1f);

            SpawnParticleBurst(root.transform, center + Vector3.up * 0.2f, fireGold, fireRed, 50, 10f, 1.0f, 0.8f);
            SpawnPillarFlash(root.transform, center, fireGold, radius, 6f, 0.5f);
            SpawnShockwaveRing(center, fireGold, 0.4f, radius * 2.5f, 0.6f);
            SpawnSparkBurst(root.transform, center + Vector3.up * 0.5f, fireGold, 35, 12f, 0.7f);

            Object.Destroy(root, 2.0f);
        }

        private class ShockwaveAnimator : MonoBehaviour
        {
            private float startR, maxR, dur, timer;
            private Material mat;
            private Color baseCol;

            public void Initialize(float sR, float mR, float d, Material m, Color c)
            {
                startR = sR; maxR = mR; dur = d; mat = m; baseCol = c; timer = 0f;
                transform.localScale = Vector3.one * startR;
            }

            private void Update()
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / dur);
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);
                float currentRadius = Mathf.Lerp(startR, maxR, easeOut);
                transform.localScale = new Vector3(currentRadius, 1f, currentRadius);

                float alpha = (1f - t);
                Color c = new Color(baseCol.r, baseCol.g, baseCol.b, baseCol.a * alpha);
                if (mat != null)
                {
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
                }
            }

            private void OnDestroy()
            {
                if (mat != null) Destroy(mat);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // ELEMENTAL & ABILITY SPECIALIZED VFX SPAWNERS
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Spawns a massive dragon fire eruption: bursting fireball core, dragon fire trail,
        /// directional sparks, and expanding fire shockwave ring.
        /// </summary>
        public static void SpawnDragonBurstVFX(Vector3 origin, Vector3 forward, float range, float coneAngle)
        {
            GameObject root = new GameObject("VFX_DragonBurst");
            root.transform.position = origin;

            Color fireRed = new Color(1f, 0.22f, 0.05f, 1f);
            Color fireGold = new Color(1f, 0.85f, 0.15f, 1f);

            // 1. Core fireball explosion at origin
            SpawnParticleBurst(root.transform, origin + Vector3.up * 0.2f, fireGold, fireRed, 45, 12f, 0.8f, 0.5f);

            // 2. Dragon flame breath cascade along cone
            int steps = 6;
            for (int i = 1; i <= steps; i++)
            {
                float frac = (float)i / steps;
                Vector3 stepPos = origin + forward * (range * frac) + Vector3.up * (0.15f + frac * 0.35f);
                float stepScale = 0.8f + frac * 2.2f;
                SpawnParticleBurst(root.transform, stepPos, Color.Lerp(fireGold, fireRed, frac), fireRed, 25, 8f * frac, stepScale, 0.6f + frac * 0.2f);
            }

            // 3. Ground fire shockwave ring
            SpawnShockwaveRing(origin, fireGold, 0.5f, range * 0.85f, 0.55f);

            // Camera shake
            ThirdPersonCameraController.TriggerScreenShake(0.45f, 0.25f);

            Object.Destroy(root, 2.0f);
        }

        /// <summary>
        /// Spawns a cosmic arcane / supernova burst with radiant star sparks and celestial shockwave.
        /// </summary>
        public static void SpawnArcaneNovaVFX(Vector3 center, float radius, Color coreColor, Color sparkColor)
        {
            GameObject root = new GameObject("VFX_ArcaneNova");
            root.transform.position = center;

            // 1. High energy plasma burst
            SpawnParticleBurst(root.transform, center + Vector3.up * 0.2f, coreColor, sparkColor, 60, radius * 2.5f, 1.2f, 0.7f);

            // 2. Radial expanding shockwave ring
            SpawnShockwaveRing(center, coreColor, 0.4f, radius, 0.6f);

            // 3. Vertical pillar beam flash
            SpawnPillarFlash(root.transform, center, coreColor, radius * 0.4f, 4f, 0.4f);

            ThirdPersonCameraController.TriggerScreenShake(0.35f, 0.2f);
            Object.Destroy(root, 1.8f);
        }

        /// <summary>
        /// Spawns an electric lightning chain: jagged zigzag bolts + ozone plasma particles.
        /// </summary>
        public static void SpawnLightningArc(Vector3 start, Vector3 target, Color boltColor)
        {
            GameObject boltObj = new GameObject("VFX_LightningBolt");
            LineRenderer lr = boltObj.AddComponent<LineRenderer>();

            Material mat = CreateVFXMaterial(boltColor, boltColor * 4f, GetSoftGlowTexture(), true);
            lr.material = mat;
            lr.startWidth = 0.18f;
            lr.endWidth = 0.06f;
            lr.positionCount = 9;

            Vector3 dir = (target - start);
            float dist = dir.magnitude;
            Vector3 side = Vector3.Cross(dir.normalized, Vector3.up).normalized;
            if (side.sqrMagnitude < 0.01f) side = Vector3.right;

            lr.SetPosition(0, start + Vector3.up * 0.2f);
            for (int i = 1; i < 8; i++)
            {
                float t = (float)i / 8;
                Vector3 p = start + dir * t + Vector3.up * 0.2f;
                float jitter = Random.Range(-0.35f, 0.35f);
                float jitterY = Random.Range(-0.2f, 0.2f);
                p += side * jitter + Vector3.up * jitterY;
                lr.SetPosition(i, p);
            }
            lr.SetPosition(8, target + Vector3.up * 0.2f);

            // Sparks at hit target
            SpawnParticleBurst(boltObj.transform, target + Vector3.up * 0.2f, boltColor, Color.white, 20, 6f, 0.6f, 0.35f);

            ThirdPersonCameraController.TriggerScreenShake(0.25f, 0.15f);

            // Quick fade and destroy
            LightningFader fader = boltObj.AddComponent<LightningFader>();
            fader.Initialize(lr, mat, 0.22f);
            Object.Destroy(boltObj, 0.35f);
        }

        private class LightningFader : MonoBehaviour
        {
            private LineRenderer lr;
            private Material mat;
            private float dur, timer;

            public void Initialize(LineRenderer l, Material m, float d)
            {
                lr = l; mat = m; dur = d; timer = 0f;
            }

            private void Update()
            {
                timer += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(timer / dur);
                if (lr != null)
                {
                    lr.startWidth *= 0.92f;
                    lr.endWidth *= 0.92f;
                }
            }

            private void OnDestroy()
            {
                if (mat != null) Destroy(mat);
            }
        }

        /// <summary>
        /// Spawns a swirling gravity black hole accretion disk with inward pulling vortex particles.
        /// </summary>
        public static void SpawnBlackHoleVFX(Vector3 center, float radius, float duration)
        {
            GameObject root = new GameObject("VFX_BlackHole");
            root.transform.position = center + Vector3.up * 0.2f;

            Color darkPurple = new Color(0.35f, 0.05f, 0.6f, 0.9f);
            Color voidBlack = new Color(0.05f, 0.02f, 0.1f, 1f);
            Color horizonGlow = new Color(0.7f, 0.2f, 1f, 1f);

            // Event horizon sphere
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "EventHorizon";
            sphere.transform.SetParent(root.transform, false);
            sphere.transform.localScale = Vector3.one * (radius * 0.45f);
            Collider c = sphere.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);

            Renderer r = sphere.GetComponent<Renderer>();
            if (r != null)
            {
                Material m = CreateVFXMaterial(voidBlack, darkPurple * 2f, GetSoftGlowTexture(), false);
                r.material = m;
            }

            // Swirling inward particles
            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = duration;
            main.loop = true;
            main.startLifetime = 0.6f;
            main.startSpeed = -radius * 1.8f; // inward negative speed
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = horizonGlow;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;

            var em = ps.emission;
            em.rateOverTime = 60;

            var rend = root.GetComponent<ParticleSystemRenderer>();
            rend.material = CreateVFXMaterial(horizonGlow, horizonGlow * 3f, GetSoftGlowTexture(), true);

            // Accretion disk shockwave
            SpawnShockwaveRing(center, horizonGlow, radius * 0.3f, radius, duration);

            ThirdPersonCameraController.TriggerScreenShake(0.3f, duration * 0.5f);
            Object.Destroy(root, duration + 0.2f);
        }

        /// <summary>
        /// Spawns a frost nova / ice explosion with flying crystal shards and subzero vapor.
        /// </summary>
        public static void SpawnFrostNovaVFX(Vector3 center, float radius)
        {
            GameObject root = new GameObject("VFX_FrostNova");
            root.transform.position = center;

            Color iceCyan = new Color(0.3f, 0.9f, 1f, 1f);
            Color iceWhite = new Color(0.85f, 0.98f, 1f, 1f);

            // 1. Crystal burst particles
            SpawnParticleBurst(root.transform, center + Vector3.up * 0.2f, iceCyan, iceWhite, 50, radius * 2f, 0.9f, 0.6f);

            // 2. Frost shockwave ring
            SpawnShockwaveRing(center, iceCyan, 0.3f, radius, 0.5f);

            // 3. Ice spike crystal primitives
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (radius * 0.55f);
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "IceShard";
                shard.transform.SetParent(root.transform, true);
                shard.transform.position = pos + Vector3.up * 0.2f;
                shard.transform.localScale = new Vector3(0.12f, 0.6f, 0.12f);
                shard.transform.rotation = Quaternion.Euler(Random.Range(-25f, 25f), angle * Mathf.Rad2Deg, Random.Range(-25f, 25f));
                Collider sc = shard.GetComponent<Collider>();
                if (sc != null) Object.Destroy(sc);

                Renderer sr = shard.GetComponent<Renderer>();
                if (sr != null)
                {
                    sr.material = CreateVFXMaterial(new Color(0.5f, 0.9f, 1f, 0.8f), iceCyan * 2f, null, false);
                }
            }

            ThirdPersonCameraController.TriggerScreenShake(0.3f, 0.2f);
            Object.Destroy(root, 1.5f);
        }

        /// <summary>
        /// Spawns a high-speed dash / blade slash crescent wave.
        /// </summary>
        public static void SpawnDashSlashVFX(Vector3 origin, Vector3 forward, Color slashColor, float scale = 1.5f)
        {
            GameObject slashObj = new GameObject("VFX_DashSlash");
            slashObj.transform.position = origin + Vector3.up * 0.2f;
            slashObj.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            // Crescent slash arc using LineRenderer
            LineRenderer lr = slashObj.AddComponent<LineRenderer>();
            Material mat = CreateVFXMaterial(slashColor, slashColor * 3.5f, GetSparkFlareTexture(), true);
            lr.material = mat;
            lr.startWidth = 0.35f * scale;
            lr.endWidth = 0.02f;
            lr.positionCount = 12;

            float arcAngle = 120f;
            float radius = 1.6f * scale;
            for (int i = 0; i < 12; i++)
            {
                float t = (float)i / 11;
                float angle = Mathf.Lerp(-arcAngle * 0.5f, arcAngle * 0.5f, t) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                lr.SetPosition(i, p);
            }
            lr.useWorldSpace = false;

            // Spark burst
            SpawnParticleBurst(slashObj.transform, origin + forward * 0.8f + Vector3.up * 0.2f, slashColor, Color.white, 20, 8f, 0.5f, 0.3f);

            ThirdPersonCameraController.TriggerScreenShake(0.2f, 0.12f);

            SlashAnimator sa = slashObj.AddComponent<SlashAnimator>();
            sa.Initialize(lr, mat, forward * 10f * scale, 0.25f);
            Object.Destroy(slashObj, 0.35f);
        }

        private class SlashAnimator : MonoBehaviour
        {
            private LineRenderer lr;
            private Material mat;
            private Vector3 vel;
            private float dur, timer;

            public void Initialize(LineRenderer l, Material m, Vector3 v, float d)
            {
                lr = l; mat = m; vel = v; dur = d; timer = 0f;
            }

            private void Update()
            {
                timer += Time.deltaTime;
                transform.position += vel * Time.deltaTime;
                float t = Mathf.Clamp01(timer / dur);
                if (lr != null)
                {
                    lr.startWidth *= 0.88f;
                }
            }

            private void OnDestroy()
            {
                if (mat != null) Destroy(mat);
            }
        }

        /// <summary>
        /// Spawns a ground pound / earthquake rock shatter with expanding dust ring and debris chunks.
        /// </summary>
        public static void SpawnEarthquakeVFX(Vector3 center, float radius)
        {
            GameObject root = new GameObject("VFX_Earthquake");
            root.transform.position = center;

            Color earthBrown = new Color(0.65f, 0.45f, 0.25f, 1f);
            Color shockYellow = new Color(1f, 0.85f, 0.3f, 1f);

            // 1. Dust & rock particle explosion
            SpawnParticleBurst(root.transform, center + Vector3.up * 0.15f, earthBrown, shockYellow, 40, radius * 2f, 1.1f, 0.65f);

            // 2. Ground shockwave ring
            SpawnShockwaveRing(center, shockYellow, 0.5f, radius, 0.5f);

            // 3. Erupting rock debris
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 p = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (radius * 0.5f);
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "EarthDebris";
                rock.transform.position = p + Vector3.up * 0.1f;
                rock.transform.localScale = Vector3.one * Random.Range(0.2f, 0.45f);
                rock.transform.rotation = Random.rotation;
                Collider rc = rock.GetComponent<Collider>();
                if (rc != null) Object.Destroy(rc);

                Rigidbody rb = rock.AddComponent<Rigidbody>();
                rb.linearVelocity = (Vector3.up * Random.Range(4f, 8f) + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2f);
                rb.angularVelocity = Random.insideUnitSphere * 15f;

                Renderer rr = rock.GetComponent<Renderer>();
                if (rr != null)
                {
                    rr.material = CreateVFXMaterial(earthBrown, earthBrown * 0.5f, null, false);
                }
                Object.Destroy(rock, 1.2f);
            }

            ThirdPersonCameraController.TriggerScreenShake(0.5f, 0.3f);
            Object.Destroy(root, 1.8f);
        }

        // ══════════════════════════════════════════════════════════════
        // LAUNCH RIP-CORD VFX
        // ══════════════════════════════════════════════════════════════

        public static void SpawnLaunchRipVFX(Vector3 origin, Vector3 forward, LaunchRating rating)
        {
            GameObject container = new GameObject($"VFX_LaunchRip_{rating}");
            container.transform.position = origin;

            switch (rating)
            {
                case LaunchRating.Perfect:
                    SpawnShockwaveRing(container.transform, origin, new Color(1f, 0.85f, 0.1f, 0.95f), 0.5f, 10.5f, 0.55f, 0.7f);
                    SpawnShockwaveRing(container.transform, origin, new Color(1f, 0.3f, 0.05f, 0.8f), 0.3f, 7.0f, 0.4f, 0.55f);
                    SpawnPillarFlash(container.transform, origin, new Color(1f, 0.85f, 0.2f, 0.9f), 1.8f, 7.0f, 0.45f);
                    SpawnParticleBurst(container.transform, origin, new Color(1f, 0.88f, 0.1f), new Color(1f, 0.3f, 0.05f), 75, 9.5f, 0.45f, 0.85f);
                    SpawnSparkBurst(container.transform, origin, new Color(1f, 0.95f, 0.5f), 45, 14f, 0.75f);
                    ThirdPersonCameraController.TriggerScreenShake(0.6f, 0.35f);
                    break;

                case LaunchRating.Great:
                    SpawnShockwaveRing(container.transform, origin, new Color(0.1f, 0.85f, 1f, 0.95f), 0.5f, 8.0f, 0.45f, 0.55f);
                    SpawnPillarFlash(container.transform, origin, new Color(0.2f, 0.8f, 1f, 0.75f), 1.3f, 5.0f, 0.35f);
                    SpawnParticleBurst(container.transform, origin, new Color(0.15f, 0.85f, 1f), new Color(0.8f, 0.3f, 1f), 50, 7.5f, 0.38f, 0.7f);
                    SpawnSparkBurst(container.transform, origin, Color.white, 30, 11f, 0.65f);
                    ThirdPersonCameraController.TriggerScreenShake(0.4f, 0.25f);
                    break;

                case LaunchRating.Good:
                    SpawnShockwaveRing(container.transform, origin, new Color(1f, 0.65f, 0.1f, 0.8f), 0.4f, 5.2f, 0.35f, 0.45f);
                    SpawnParticleBurst(container.transform, origin, new Color(1f, 0.6f, 0.1f), new Color(1f, 0.9f, 0.2f), 30, 4.8f, 0.3f, 0.55f);
                    ThirdPersonCameraController.TriggerScreenShake(0.2f, 0.15f);
                    break;

                case LaunchRating.Mishap:
                    SpawnParticleBurst(container.transform, origin, new Color(0.45f, 0.45f, 0.5f, 0.6f), new Color(0.3f, 0.3f, 0.35f, 0f), 18, 2.5f, 0.4f, 0.65f);
                    ThirdPersonCameraController.TriggerScreenShake(0.1f, 0.1f);
                    break;
            }

            Object.Destroy(container, 1.6f);
        }

        public static void SpawnSparkBurst(Transform parent, Vector3 position, Color color, int count = 25, float speed = 10f, float duration = 0.5f)
        {
            GameObject psObj = new GameObject("VFX_Sparks");
            if (parent != null) psObj.transform.SetParent(parent, false);
            psObj.transform.position = position;

            ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(duration * 0.4f, duration);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.32f);
            main.startColor = color;
            main.maxParticles = count + 10;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.5f);

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var rend = psObj.GetComponent<ParticleSystemRenderer>();
            rend.material = CreateVFXMaterial(color, color * 3f, GetSparkFlareTexture(), true);

            ps.Play();
            Object.Destroy(psObj, duration + 0.15f);
        }

        public static void SpawnSparkBurst(Vector3 position, Color color, int count = 25, float speed = 10f, float duration = 0.5f)
        {
            SpawnSparkBurst(null, position, color, count, speed, duration);
        }

        // ══════════════════════════════════════════════════════════════
        // PARTICLE SYSTEM BURST BUILDER
        // ══════════════════════════════════════════════════════════════

        public static ParticleSystem SpawnParticleBurst(
            Transform parent,
            Vector3 position,
            Color primaryColor,
            Color secondaryColor,
            int count,
            float speed,
            float size,
            float duration)
        {
            GameObject psObj = new GameObject("VFX_ParticleBurst");
            if (parent != null) psObj.transform.SetParent(parent, false);
            psObj.transform.position = position;

            ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(duration * 0.4f, duration);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.4f, size);
            main.startColor = primaryColor;
            main.maxParticles = count + 10;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.2f);

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var colOverLife = ps.colorOverLifetime;
            colOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(primaryColor, 0f), new GradientColorKey(secondaryColor, 0.7f), new GradientColorKey(secondaryColor, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(0.7f, 0.7f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var rend = psObj.GetComponent<ParticleSystemRenderer>();
            rend.material = CreateVFXMaterial(primaryColor, primaryColor * 2.5f, GetSoftGlowTexture(), true);

            ps.Play();
            Object.Destroy(psObj, duration + 0.15f);
            return ps;
        }

        public static ParticleSystem SpawnParticleBurst(
            Vector3 position,
            int count,
            Color primaryColor,
            Color secondaryColor,
            float size = 1f,
            float speed = 8f,
            float duration = 0.5f)
        {
            return SpawnParticleBurst(null, position, primaryColor, secondaryColor, count, speed, size, duration);
        }

        public static ParticleSystem SpawnParticleBurst(
            Vector3 position,
            Color primaryColor,
            Color secondaryColor,
            int count = 25,
            float size = 1f)
        {
            return SpawnParticleBurst(null, position, primaryColor, secondaryColor, count, 8f, size, 0.5f);
        }

        private static void SpawnPillarFlash(Transform parent, Vector3 center, Color color, float width, float height, float duration)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "VFX_PillarFlash";
            pillar.transform.SetParent(parent, false);
            pillar.transform.position = center + Vector3.up * (height * 0.5f);
            pillar.transform.localScale = new Vector3(width, height * 0.5f, width);
            Collider c = pillar.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);

            Renderer r = pillar.GetComponent<Renderer>();
            if (r != null)
            {
                Material m = CreateVFXMaterial(color, color * 3f, GetSoftGlowTexture(), true);
                r.material = m;
            }

            PillarFader pf = pillar.AddComponent<PillarFader>();
            pf.Initialize(duration, width);
            Object.Destroy(pillar, duration + 0.1f);
        }

        private class PillarFader : MonoBehaviour
        {
            private float dur, timer, baseW;
            public void Initialize(float d, float w) { dur = d; baseW = w; timer = 0f; }
            private void Update()
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / dur);
                float w = Mathf.Lerp(baseW, 0f, t);
                transform.localScale = new Vector3(w, transform.localScale.y, w);
            }
        }
    }
}
