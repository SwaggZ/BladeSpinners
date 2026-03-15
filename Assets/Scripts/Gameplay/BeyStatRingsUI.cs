using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Gameplay
{
    /// <summary>
    /// World-space ring UI that shows Spin (yellow/inner), Mana (cyan/middle), and
    /// Speed (magenta/outer) around the player's Bey.
    ///
    /// Rings stay horizontal and include a front cut/gap.
    /// </summary>
    [DisallowMultipleComponent]
    public class BeyStatRingsUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private BeyConfiguration beyConfiguration;
        [SerializeField] private BeyMovementController movementController;

        [Header("Layout")]
        [SerializeField] private float yOffset = 0.10f;
        [SerializeField] private float ringGap = 0.20f;
        [SerializeField] private float innerRadius = 0.42f;
        [SerializeField] private int ringResolution = 64;

        [Header("Ring Style")]
        [SerializeField] private float fillWidth = 0.055f;
        [SerializeField] private float backgroundWidth = 0.055f;
        [SerializeField] private float baseOutlineWidth = 0.09f;
        [SerializeField] private float baseDarkenAmount = 0.65f;
        [SerializeField] private float fillVividBoost = 0.22f;

        [Header("Gap")]
        [SerializeField] private float gapDegrees = 72f;

        [Header("Colors — inner to outer")]
        [SerializeField] private Color spinColor = new Color(1.00f, 0.87f, 0.00f, 1f);
        [SerializeField] private Color manaColor = new Color(0.00f, 0.95f, 1.00f, 1f);
        [SerializeField] private Color speedColor = new Color(1.00f, 0.00f, 0.90f, 1f);

        [Header("Labels")]
        [SerializeField] private float labelCharSize = 0.026f;
        [SerializeField] private float labelXOffset = 0.015f;

        [Header("Behavior")]
        [SerializeField] private float speedReference = 30f;
        [SerializeField] private float speedBoostAllowance = 12f;

        private RingGroup spinRing;
        private RingGroup manaRing;
        private RingGroup speedRing;
        private Camera mainCamera;
        private bool visualsCreated;

        private float GapHalfRadians => Mathf.Clamp(gapDegrees, 0f, 120f) * Mathf.Deg2Rad * 0.5f;
        private float ArcRangeRadians => Mathf.PI * 2f - Mathf.Clamp(gapDegrees, 0f, 120f) * Mathf.Deg2Rad;

        private struct RingGroup
        {
            public Transform Root;
            public LineRenderer FillLine;
            public LineRenderer FillOutline;
            public LineRenderer BgOutline;
            public LineRenderer BgFill;
            public TextMesh Label;
            public float Radius;
        }

        public void Initialize(BeyConfiguration config, BeyMovementController movement, Transform followTarget = null)
        {
            beyConfiguration = config;
            movementController = movement;
            target = followTarget != null ? followTarget : transform;

            if (!visualsCreated)
            {
                BuildVisuals();
            }
        }

        private void Awake()
        {
            if (target == null)
            {
                target = transform;
            }

            mainCamera = Camera.main;
            BuildVisuals();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            Vector3 center = target.position + Vector3.up * yOffset;
            MoveRoot(spinRing.Root, center);
            MoveRoot(manaRing.Root, center);
            MoveRoot(speedRing.Root, center);

            float spinFraction = 0f;
            float manaFraction = 0f;
            float speedFraction = 0f;

            if (beyConfiguration != null)
            {
                spinFraction = Mathf.Clamp01(beyConfiguration.CurrentSpin / GameConstants.MAX_SPIN);
                float manaPool = beyConfiguration.GetStatBlock().ManaPoolSize;
                manaFraction = manaPool > 0f ? Mathf.Clamp01(beyConfiguration.CurrentMana / manaPool) : 0f;
            }

            if (movementController != null && movementController.Rb != null)
            {
                Vector3 velocity = movementController.Rb.linearVelocity;
                float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
                float boost = Mathf.Clamp01(movementController.MomentumStrength) * Mathf.Max(0f, speedBoostAllowance);
                float effectiveSpeed = horizontalSpeed + boost;
                float maxSpeedWithBoost = Mathf.Max(0.01f, speedReference + Mathf.Max(0f, speedBoostAllowance));
                speedFraction = Mathf.Clamp01(effectiveSpeed / maxSpeedWithBoost);
            }

            SetRingFill(spinRing, spinFraction);
            SetRingFill(manaRing, manaFraction);
            SetRingFill(speedRing, speedFraction);

            if (mainCamera != null)
            {
                BillboardLabel(spinRing.Label);
                BillboardLabel(manaRing.Label);
                BillboardLabel(speedRing.Label);
            }
        }

        private void MoveRoot(Transform root, Vector3 center)
        {
            if (root == null)
            {
                return;
            }

            root.position = center;
            float yaw = 0f;
            if (mainCamera != null)
            {
                Vector3 cameraForwardFlat = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up);
                if (cameraForwardFlat.sqrMagnitude > 0.0001f)
                {
                    yaw = Mathf.Atan2(cameraForwardFlat.x, cameraForwardFlat.z) * Mathf.Rad2Deg;
                }
            }

            root.rotation = Quaternion.Euler(-90f, yaw + 180f, 0f);
        }

        private void BuildVisuals()
        {
            if (visualsCreated)
            {
                return;
            }

            visualsCreated = true;

            spinRing = CreateRingGroup("Spin", innerRadius, spinColor, "Spin");
            manaRing = CreateRingGroup("Mana", innerRadius + ringGap, manaColor, "Mana");
            speedRing = CreateRingGroup("Speed", innerRadius + ringGap * 2f, speedColor, "Speed");

            WriteArc(spinRing.BgOutline, spinRing.Radius, GapHalfRadians, ArcRangeRadians);
            WriteArc(spinRing.BgFill, spinRing.Radius, GapHalfRadians, ArcRangeRadians);
            WriteArc(manaRing.BgOutline, manaRing.Radius, GapHalfRadians, ArcRangeRadians);
            WriteArc(manaRing.BgFill, manaRing.Radius, GapHalfRadians, ArcRangeRadians);
            WriteArc(speedRing.BgOutline, speedRing.Radius, GapHalfRadians, ArcRangeRadians);
            WriteArc(speedRing.BgFill, speedRing.Radius, GapHalfRadians, ArcRangeRadians);

            SetRingFill(spinRing, 0f);
            SetRingFill(manaRing, 0f);
            SetRingFill(speedRing, 0f);
        }

        private RingGroup CreateRingGroup(string tag, float radius, Color color, string labelText)
        {
            GameObject rootObject = new GameObject($"Ring_{tag}");
            rootObject.transform.SetParent(transform, false);

            Color darkBaseColor = GetDarkBaseColor(color);
            Color brightFillColor = GetBrightFillColor(color);

            RingGroup ring = new RingGroup
            {
                Root = rootObject.transform,
                Radius = radius,
                BgOutline = CreateLine(rootObject.transform, $"{tag}_BgOutline", Color.black, baseOutlineWidth, 2994),
                BgFill = CreateLine(rootObject.transform, $"{tag}_BgFill", darkBaseColor, backgroundWidth, 2995),
                FillOutline = CreateLine(rootObject.transform, $"{tag}_FillOutline", Color.black, baseOutlineWidth, 2996),
                FillLine = CreateLine(rootObject.transform, $"{tag}_Fill", brightFillColor, fillWidth, 2997),
                Label = CreateLabel(rootObject.transform, radius, color, labelText)
            };

            return ring;
        }

        private LineRenderer CreateLine(Transform parent, string objectName, Color color, float width, int renderQueue)
        {
            GameObject lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(parent, false);

            Shader shader = ShaderProvider.URPUnlit;

            Material material = new Material(shader);
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetFloat("_Cull", 0f); // render both sides so rings remain visible from above/below
            material.SetFloat("_Surface", 1f); // transparent mode
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f); // avoid coplanar depth fighting artifacts
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = renderQueue;

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = false;
            line.widthMultiplier = width;
            line.numCornerVertices = 4;
            line.numCapVertices = 6;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.TransformZ;
            line.material = material;
            line.startColor = color;
            line.endColor = color;
            line.colorGradient = CreateFlatGradient(color);

            return line;
        }

        private static Gradient CreateFlatGradient(Color color)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(color.a, 1f) }
            );
            return gradient;
        }

        private void WriteArc(LineRenderer line, float radius, float startAngle, float arcLength)
        {
            int points = Mathf.Max(2, ringResolution + 1);
            line.positionCount = points;

            for (int i = 0; i < points; i++)
            {
                float t = (float)i / (points - 1);
                float angle = startAngle + t * arcLength;
                line.SetPosition(i, new Vector3(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius, 0f));
            }
        }

        private void SetRingFill(RingGroup ring, float fraction)
        {
            if (fraction <= 0.001f)
            {
                ring.FillLine.positionCount = 0;
                ring.FillOutline.positionCount = 0;
                return;
            }

            float clampedFraction = Mathf.Clamp01(fraction);
            float arcLength = ArcRangeRadians * clampedFraction;
            int segments = Mathf.Max(2, Mathf.CeilToInt(ringResolution * clampedFraction) + 1);

            ring.FillLine.positionCount = segments;
            ring.FillOutline.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / (segments - 1);
                float angle = GapHalfRadians + t * arcLength;
                Vector3 position = new Vector3(Mathf.Sin(angle) * ring.Radius, Mathf.Cos(angle) * ring.Radius, 0f);
                ring.FillLine.SetPosition(i, position);
                ring.FillOutline.SetPosition(i, position);
            }
        }

        private Color GetDarkBaseColor(Color source)
        {
            return Color.Lerp(source, Color.black, Mathf.Clamp01(baseDarkenAmount));
        }

        private Color GetBrightFillColor(Color source)
        {
            Color.RGBToHSV(source, out float hue, out float saturation, out float value);
            saturation = Mathf.Clamp01(saturation + fillVividBoost * 0.6f);
            value = Mathf.Clamp01(value + fillVividBoost);
            Color boosted = Color.HSVToRGB(hue, saturation, value);
            boosted.a = source.a;
            return boosted;
        }

        private TextMesh CreateLabel(Transform parent, float radius, Color color, string text)
        {
            GameObject labelObject = new GameObject($"Label_{text}");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(radius + labelXOffset, 0f, 0f);
            labelObject.transform.localRotation = Quaternion.identity;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = 22;
            label.characterSize = labelCharSize;
            label.anchor = TextAnchor.MiddleLeft;
            label.alignment = TextAlignment.Left;
            label.color = color;
            label.fontStyle = FontStyle.Bold;

            MeshRenderer labelRenderer = labelObject.GetComponent<MeshRenderer>();
            if (labelRenderer != null)
            {
                ConfigureTextRenderer(labelRenderer, 4101);
            }

            const float outlineOffset = 0.007f;
            CreateLabelOutlineLayer(labelObject.transform, text, new Vector3( outlineOffset, 0f, 0f));
            CreateLabelOutlineLayer(labelObject.transform, text, new Vector3(-outlineOffset, 0f, 0f));
            CreateLabelOutlineLayer(labelObject.transform, text, new Vector3(0f,  outlineOffset, 0f));
            CreateLabelOutlineLayer(labelObject.transform, text, new Vector3(0f, -outlineOffset, 0f));
            CreateLabelOutlineLayer(labelObject.transform, text, new Vector3( outlineOffset,  outlineOffset, 0f));
            CreateLabelOutlineLayer(labelObject.transform, text, new Vector3(-outlineOffset,  outlineOffset, 0f));
            CreateLabelOutlineLayer(labelObject.transform, text, new Vector3( outlineOffset, -outlineOffset, 0f));
            CreateLabelOutlineLayer(labelObject.transform, text, new Vector3(-outlineOffset, -outlineOffset, 0f));

            return label;
        }

        private void CreateLabelOutlineLayer(Transform parent, string text, Vector3 offset)
        {
            GameObject outlineObject = new GameObject("LabelOutline");
            outlineObject.transform.SetParent(parent, false);
            outlineObject.transform.localPosition = offset;
            outlineObject.transform.localScale = Vector3.one;

            TextMesh outline = outlineObject.AddComponent<TextMesh>();
            outline.text = text;
            outline.fontSize = 22;
            outline.characterSize = labelCharSize;
            outline.anchor = TextAnchor.MiddleLeft;
            outline.alignment = TextAlignment.Left;
            outline.color = new Color(0f, 0f, 0f, 0.9f);
            outline.fontStyle = FontStyle.Bold;

            MeshRenderer outlineRenderer = outlineObject.GetComponent<MeshRenderer>();
            if (outlineRenderer != null)
            {
                ConfigureTextRenderer(outlineRenderer, 4100);
            }
        }

        private void ConfigureTextRenderer(MeshRenderer renderer, int sortingOrder)
        {
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (renderer.sharedMaterial == null)
            {
                return;
            }

            Material frontMaterial = new Material(renderer.sharedMaterial);
            frontMaterial.SetFloat("_Cull", 0f);
            frontMaterial.SetFloat("_Surface", 1f);
            frontMaterial.SetFloat("_Blend", 0f);
            frontMaterial.SetFloat("_ZWrite", 0f);
            frontMaterial.SetFloat("_ZTest", 8f); // Always
            frontMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            frontMaterial.renderQueue = 4100;

            renderer.material = frontMaterial;
        }

        private void BillboardLabel(TextMesh label)
        {
            if (label == null || mainCamera == null)
            {
                return;
            }

            Transform labelTransform = label.transform;
            labelTransform.rotation = Quaternion.LookRotation(
                labelTransform.position - mainCamera.transform.position,
                Vector3.up
            );
        }
    }
}
