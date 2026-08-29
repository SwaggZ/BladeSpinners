using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Abilities;

namespace BladeSpinners.Gameplay.Effects
{
    /// <summary>
    /// Dynamic movement trail and ground friction effect system for Beys.
    /// Spawns aerodynamic motion ribbons, tip dirt/dust kickup plumes, and
    /// friction sparks that react to speed, ground contact, spin, and boosting.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BeyMovementController))]
    public class BeyGroundTrailEffect : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float tipYOffset = -0.28f;
        [SerializeField] private float baseRibbonWidth = 0.28f;
        [SerializeField] private float maxRibbonWidth = 0.55f;
        [SerializeField] private float baseTrailLifetime = 0.32f;

        [Header("Colors")]
        [SerializeField] private Color defaultTrailColor = new Color(0.12f, 0.85f, 1f, 0.85f);
        [SerializeField] private Color dustColor = new Color(0.72f, 0.62f, 0.48f, 0.55f);
        [SerializeField] private Color sparkColor = new Color(1f, 0.82f, 0.25f, 1f);

        private BeyMovementController movementController;
        private BeyConfiguration configuration;
        private Rigidbody rb;

        // Visual components
        private GameObject ribbonObject;
        private TrailRenderer movementRibbon;

        private GameObject dirtPlumeObject;
        private ParticleSystem dirtPlume;
        private ParticleSystem.EmissionModule dirtEmission;

        private GameObject sparksObject;
        private ParticleSystem frictionSparks;
        private ParticleSystem.EmissionModule sparkEmission;

        private Color currentBeyColor;
        private bool isInitialized = false;

        private void Awake()
        {
            movementController = GetComponent<BeyMovementController>();
            rb = GetComponent<Rigidbody>();
            configuration = movementController != null ? movementController.BeyConfiguration : null;

            BuildEffects();
        }

        private void Start()
        {
            if (configuration == null && movementController != null)
                configuration = movementController.BeyConfiguration;

            UpdateThemeColor();
        }

        public void Initialize(BeyConfiguration config)
        {
            configuration = config;
            UpdateThemeColor();
        }

        private void UpdateThemeColor()
        {
            currentBeyColor = defaultTrailColor;

            if (configuration != null)
            {
                BeyPart energyRing = configuration.GetEquippedPart(PartType.EnergyRing);
                BeyPart fusionWheel = configuration.GetEquippedPart(PartType.FusionWheel);
                BeyPart faceBolt = configuration.GetEquippedPart(PartType.FaceBolt);

                if (energyRing != null && energyRing.PrimaryColor.a > 0.1f)
                    currentBeyColor = energyRing.PrimaryColor;
                else if (faceBolt != null && faceBolt.PrimaryColor.a > 0.1f)
                    currentBeyColor = faceBolt.PrimaryColor;
                else if (fusionWheel != null && fusionWheel.PrimaryColor.a > 0.1f)
                    currentBeyColor = fusionWheel.PrimaryColor;

                currentBeyColor.a = 0.85f;
            }

            if (movementRibbon != null)
            {
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new[] { new GradientColorKey(currentBeyColor, 0f), new GradientColorKey(Color.white, 0.2f), new GradientColorKey(currentBeyColor, 1f) },
                    new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0.5f, 0.4f), new GradientAlphaKey(0f, 1f) }
                );
                movementRibbon.colorGradient = grad;
            }
        }

        private void BuildEffects()
        {
            if (isInitialized) return;
            isInitialized = true;

            // 1. MOVEMENT RIBBON (TrailRenderer)
            ribbonObject = new GameObject("VFX_MovementRibbon");
            ribbonObject.transform.SetParent(transform, false);
            ribbonObject.transform.localPosition = new Vector3(0f, tipYOffset + 0.08f, 0f);

            movementRibbon = ribbonObject.AddComponent<TrailRenderer>();
            movementRibbon.time = baseTrailLifetime;
            movementRibbon.minVertexDistance = 0.06f;
            movementRibbon.autodestruct = false;
            movementRibbon.emitting = true;

            AnimationCurve widthCurve = new AnimationCurve();
            widthCurve.AddKey(0f, baseRibbonWidth);
            widthCurve.AddKey(0.35f, baseRibbonWidth * 0.75f);
            widthCurve.AddKey(1f, 0f);
            movementRibbon.widthCurve = widthCurve;

            Material ribbonMat = EpicAbilityVFXHelper.CreateVFXMaterial(
                currentBeyColor,
                currentBeyColor * 2.5f,
                EpicAbilityVFXHelper.GetSoftGlowTexture(),
                additive: true
            );
            movementRibbon.material = ribbonMat;

            // 2. TIP DIRT & DUST PLUME (ParticleSystem)
            dirtPlumeObject = new GameObject("VFX_TipDirtPlume");
            dirtPlumeObject.transform.SetParent(transform, false);
            dirtPlumeObject.transform.localPosition = new Vector3(0f, tipYOffset, 0f);

            dirtPlume = dirtPlumeObject.AddComponent<ParticleSystem>();
            dirtPlume.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var dirtMain = dirtPlume.main;
            dirtMain.duration = 1f;
            dirtMain.loop = true;
            dirtMain.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            dirtMain.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.8f);
            dirtMain.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.32f);
            dirtMain.startColor = dustColor;
            dirtMain.maxParticles = 180;
            dirtMain.simulationSpace = ParticleSystemSimulationSpace.World;
            dirtMain.playOnAwake = true;
            dirtMain.gravityModifier = new ParticleSystem.MinMaxCurve(-0.1f, 0.15f);

            dirtEmission = dirtPlume.emission;
            dirtEmission.enabled = true;
            dirtEmission.rateOverTime = 0f;

            var dirtShape = dirtPlume.shape;
            dirtShape.enabled = true;
            dirtShape.shapeType = ParticleSystemShapeType.Circle;
            dirtShape.radius = 0.18f;
            dirtShape.arc = 360f;

            var dirtCol = dirtPlume.colorOverLifetime;
            dirtCol.enabled = true;
            Gradient dirtGrad = new Gradient();
            dirtGrad.SetKeys(
                new[] { new GradientColorKey(dustColor, 0f), new GradientColorKey(new Color(dustColor.r * 0.85f, dustColor.g * 0.85f, dustColor.b * 0.85f), 1f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0.45f, 0.4f), new GradientAlphaKey(0f, 1f) }
            );
            dirtCol.color = new ParticleSystem.MinMaxGradient(dirtGrad);

            var dirtSize = dirtPlume.sizeOverLifetime;
            dirtSize.enabled = true;
            AnimationCurve dirtSizeCurve = new AnimationCurve();
            dirtSizeCurve.AddKey(0f, 0.5f);
            dirtSizeCurve.AddKey(0.5f, 1.2f);
            dirtSizeCurve.AddKey(1f, 2.2f);
            dirtSize.size = new ParticleSystem.MinMaxCurve(1f, dirtSizeCurve);

            var dirtRend = dirtPlumeObject.GetComponent<ParticleSystemRenderer>();
            dirtRend.material = EpicAbilityVFXHelper.CreateVFXMaterial(
                dustColor,
                Color.black,
                EpicAbilityVFXHelper.GetSoftGlowTexture(),
                additive: false
            );
            dirtPlume.Play();

            // 3. TIP FRICTION SPARKS (ParticleSystem)
            sparksObject = new GameObject("VFX_TipFrictionSparks");
            sparksObject.transform.SetParent(transform, false);
            sparksObject.transform.localPosition = new Vector3(0f, tipYOffset, 0f);

            frictionSparks = sparksObject.AddComponent<ParticleSystem>();
            frictionSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var sparkMain = frictionSparks.main;
            sparkMain.duration = 1f;
            sparkMain.loop = true;
            sparkMain.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
            sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6.5f);
            sparkMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            sparkMain.startColor = sparkColor;
            sparkMain.maxParticles = 120;
            sparkMain.simulationSpace = ParticleSystemSimulationSpace.World;
            sparkMain.playOnAwake = true;
            sparkMain.gravityModifier = new ParticleSystem.MinMaxCurve(0.7f);

            sparkEmission = frictionSparks.emission;
            sparkEmission.enabled = true;
            sparkEmission.rateOverTime = 0f;

            var sparkShape = frictionSparks.shape;
            sparkShape.enabled = true;
            sparkShape.shapeType = ParticleSystemShapeType.Hemisphere;
            sparkShape.radius = 0.12f;

            var sparkCol = frictionSparks.colorOverLifetime;
            sparkCol.enabled = true;
            Gradient sparkGrad = new Gradient();
            sparkGrad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(sparkColor, 0.4f), new GradientColorKey(new Color(1f, 0.3f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            sparkCol.color = new ParticleSystem.MinMaxGradient(sparkGrad);

            var sparkRend = sparksObject.GetComponent<ParticleSystemRenderer>();
            sparkRend.material = EpicAbilityVFXHelper.CreateVFXMaterial(
                sparkColor,
                sparkColor * 3f,
                EpicAbilityVFXHelper.GetSparkFlareTexture(),
                additive: true
            );
            frictionSparks.Play();
        }

        private void LateUpdate()
        {
            if (movementController == null || rb == null)
                return;

            bool isGrounded = movementController.IsGrounded;
            float speed = movementController.CurrentHorizontalSpeed;
            float boostMult = movementController.CurrentBoostMultiplier;
            bool isBoosting = boostMult > 1.15f;

            // Check if Bey is alive
            if (configuration != null && configuration.IsBurst)
            {
                if (movementRibbon != null) movementRibbon.emitting = false;
                if (dirtEmission.enabled) dirtEmission.rateOverTime = 0f;
                if (sparkEmission.enabled) sparkEmission.rateOverTime = 0f;
                return;
            }

            // 1. Update Movement Ribbon
            if (movementRibbon != null)
            {
                bool shouldEmitRibbon = isGrounded && speed > 0.8f;
                movementRibbon.emitting = shouldEmitRibbon;

                float speed01 = Mathf.Clamp01(speed / 28f);
                movementRibbon.time = Mathf.Lerp(0.18f, baseTrailLifetime * (isBoosting ? 1.4f : 1f), speed01);

                float currentWidth = Mathf.Lerp(baseRibbonWidth, maxRibbonWidth, speed01 * (isBoosting ? 1.35f : 1f));
                movementRibbon.startWidth = currentWidth;
            }

            // 2. Update Tip Dirt & Dust Plume
            if (dirtPlume != null)
            {
                if (!isGrounded || speed < 0.3f)
                {
                    dirtEmission.rateOverTime = 0f;
                }
                else
                {
                    // Scale emission with speed and boost
                    float speed01 = Mathf.Clamp01(speed / 24f);
                    float targetRate = Mathf.Lerp(8f, 75f, speed01) * (isBoosting ? 1.6f : 1f);
                    dirtEmission.rateOverTime = targetRate;

                    // Direct dirt kickback opposite to movement
                    if (speed > 0.5f)
                    {
                        Vector3 kickbackDir = -rb.linearVelocity.normalized + Vector3.up * 0.35f;
                        dirtPlumeObject.transform.rotation = Quaternion.LookRotation(kickbackDir.normalized, Vector3.up);
                    }
                }
            }

            // 3. Update Friction Sparks
            if (frictionSparks != null)
            {
                if (!isGrounded || speed < 6.0f)
                {
                    sparkEmission.rateOverTime = isBoosting && isGrounded ? 35f : 0f;
                }
                else
                {
                    float speed01 = Mathf.Clamp01((speed - 6f) / 20f);
                    float targetRate = Mathf.Lerp(12f, 65f, speed01) * (isBoosting ? 1.8f : 1f);
                    sparkEmission.rateOverTime = targetRate;

                    if (speed > 1f)
                    {
                        Vector3 sparkDir = -rb.linearVelocity.normalized + Vector3.up * 0.5f;
                        sparksObject.transform.rotation = Quaternion.LookRotation(sparkDir.normalized, Vector3.up);
                    }
                }
            }
        }
    }
}
