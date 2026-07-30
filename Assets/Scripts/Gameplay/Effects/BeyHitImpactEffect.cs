using UnityEngine;

namespace BladeSpinners.Gameplay.Effects
{
    public static class BeyHitImpactEffect
    {
        public static void Spawn(Vector3 position, Color color, float relativeSpeed)
        {
            float intensity = Mathf.Clamp01(relativeSpeed / 28f);

            GameObject root = new GameObject("BeyHitImpactVFX");
            root.transform.position = position;

            ParticleSystem sparks = CreateSparks(root.transform, color, intensity);
            ParticleSystem burst = CreateBurst(root.transform, color, intensity);

            sparks.Play();
            burst.Play();

            Object.Destroy(root, 1.25f);
        }

        private static ParticleSystem CreateSparks(Transform parent, Color color, float intensity)
        {
            GameObject go = new GameObject("Sparks");
            go.transform.SetParent(parent, false);

            ParticleSystem particleSystem = go.AddComponent<ParticleSystem>();
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particleSystem.main;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, Mathf.Lerp(7f, 13f, intensity));
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.startColor = color;
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.15f);

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(Mathf.Lerp(14f, 28f, intensity)))
            });

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(new Color(color.r, color.g * 0.8f, color.b * 0.6f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            return particleSystem;
        }

        private static ParticleSystem CreateBurst(Transform parent, Color color, float intensity)
        {
            GameObject go = new GameObject("CoreBurst");
            go.transform.SetParent(parent, false);

            ParticleSystem particleSystem = go.AddComponent<ParticleSystem>();
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particleSystem.main;
            main.duration = 0.16f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.5f + intensity * 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.28f + intensity * 0.18f);
            main.startColor = Color.Lerp(color, Color.white, 0.35f);
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(Mathf.Lerp(10f, 20f, intensity)))
            });

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.02f;

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.4f, 1f),
                new Keyframe(1f, 0f)));

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.Lerp(color, Color.white, 0.45f), 0f),
                    new GradientColorKey(color, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            return particleSystem;
        }
    }
}
