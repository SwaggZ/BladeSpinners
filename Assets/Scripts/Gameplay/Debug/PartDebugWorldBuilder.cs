using System;
using System.Collections.Generic;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.UI;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BladeSpinners.Gameplay.PartDebugging
{
    public class PartDebugWorldBuilder : MonoBehaviour
    {
        private const float FaceBoltEmblemWorldSize = 0.07f;

        [Header("Layout")]
        [SerializeField] private float partSpacingX = 1.5f;
        [SerializeField] private float rowSpacingZ = 4.0f;
        [SerializeField] private float orientationSpacingZ = 1.0f;
        [SerializeField] private float partScale = 2.5f;
        [SerializeField] private bool spawnOrthogonalOrientations = false;
        [SerializeField] private bool spawnLabels = true;

        [Header("Build")]
        [SerializeField] private bool rebuildOnStart = true;

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();

        private static readonly PartType[] OrderedPartTypes =
        {
            PartType.Tip,
            PartType.Track,
            PartType.FusionWheel,
            PartType.EnergyRing,
            PartType.FaceBolt
        };

        private static readonly Quaternion[] OrthogonalOrientations =
        {
            Quaternion.identity,
            Quaternion.Euler(90f, 0f, 0f),
            Quaternion.Euler(-90f, 0f, 0f),
            Quaternion.Euler(0f, 0f, 90f),
            Quaternion.Euler(0f, 0f, -90f),
            Quaternion.Euler(180f, 0f, 0f)
        };

        private void Start()
        {
            if (rebuildOnStart)
                RebuildWorld();
        }

        [ContextMenu("Rebuild Parts Debug World")]
        public void RebuildWorld()
        {
            ClearWorld();

            List<BeyPart> allParts = LoadAllParts();
            if (allParts.Count == 0)
            {
                Debug.LogWarning("[PartDebugWorldBuilder] No BeyPart assets found to spawn.");
                return;
            }

            Dictionary<PartType, List<BeyPart>> grouped = GroupByType(allParts);
            int maxColumns = 0;

            for (int row = 0; row < OrderedPartTypes.Length; row++)
            {
                PartType type = OrderedPartTypes[row];
                if (!grouped.TryGetValue(type, out List<BeyPart> partsOfType) || partsOfType.Count == 0)
                    continue;

                partsOfType.Sort((a, b) => string.Compare(a.PartName, b.PartName, StringComparison.OrdinalIgnoreCase));
                maxColumns = Mathf.Max(maxColumns, partsOfType.Count);

                for (int col = 0; col < partsOfType.Count; col++)
                {
                    BeyPart part = partsOfType[col];
                    Vector3 basePos = new Vector3(col * partSpacingX, 0f, row * rowSpacingZ);
                    SpawnPartEntry(part, basePos);
                }
            }

            SpawnGround(maxColumns);
            Debug.Log($"[PartDebugWorldBuilder] Spawned {spawnedObjects.Count} debug objects for {allParts.Count} parts.");
        }

        private void SpawnPartEntry(BeyPart part, Vector3 basePos)
        {
            Quaternion[] orientations = spawnOrthogonalOrientations
                ? OrthogonalOrientations
                : new[] { Quaternion.identity };

            for (int i = 0; i < orientations.Length; i++)
            {
                GameObject root = new GameObject($"{part.PartType}_{part.PartName}_O{i}");
                root.transform.SetParent(transform, false);
                root.transform.position = basePos + new Vector3(0f, 0f, i * orientationSpacingZ);
                root.transform.rotation = orientations[i];
                root.transform.localScale = Vector3.one * partScale;

                MeshFilter filter = root.AddComponent<MeshFilter>();
                MeshRenderer renderer = root.AddComponent<MeshRenderer>();

                Mesh mesh = ProceduralPartMeshGenerator.GenerateMesh(part);
                filter.sharedMesh = mesh;

                Material mat = BuildPartMaterial(part);
                renderer.sharedMaterial = mat;

                if (part.PartType == PartType.FaceBolt && part.FaceBoltEmblem != null && mesh != null)
                    SpawnFaceBoltEmblem(root.transform, mesh.bounds, part.FaceBoltEmblem);

                spawnedObjects.Add(root);

                if (spawnLabels)
                    SpawnLabel(root.transform, part, i);
            }
        }

        private static Material BuildPartMaterial(BeyPart part)
        {
            Material mat = new Material(ShaderProvider.URPLit);

            Color color = part.PrimaryColor;
            if (part.PartType == PartType.EnergyRing)
                color.a = 0.56f;

            mat.SetColor("_BaseColor", color);

            switch (part.PartType)
            {
                case PartType.FusionWheel:
                    mat.SetColor("_BaseColor", GetFusionWheelMetalColor(color));
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 1f);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f);
                    if (mat.HasProperty("_EnvironmentReflections")) mat.SetFloat("_EnvironmentReflections", 1f);
                    if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 1f);
                    break;

                case PartType.EnergyRing:
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.3f);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.8f);
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                    if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
                    if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
                    if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;

                default:
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.55f);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.55f);
                    break;
            }

            return mat;
        }

        private static Color GetFusionWheelMetalColor(Color source)
        {
            float luminance = source.grayscale;
            Color neutral = new Color(luminance, luminance, luminance, 1f);
            Color coated = Color.Lerp(neutral, new Color(source.r, source.g, source.b, 1f), 0.18f);
            return Color.Lerp(coated, new Color(0.72f, 0.72f, 0.72f, 1f), 0.2f);
        }

        private static void SpawnFaceBoltEmblem(Transform parent, Bounds meshBounds, Sprite emblemSprite)
        {
            GameObject emblemObject = new GameObject("FaceBoltEmblem");
            emblemObject.transform.SetParent(parent, false);

            float topY = meshBounds.max.y + 0.002f;
            emblemObject.transform.localPosition = new Vector3(0f, topY, 0f);
            emblemObject.transform.localRotation = Quaternion.Euler(90f, 0f, -30f);
            float spriteWorldDiameter = Mathf.Max(emblemSprite.bounds.size.x, emblemSprite.bounds.size.y);
            float normalizedScale = (spriteWorldDiameter > 0f) ? FaceBoltEmblemWorldSize / spriteWorldDiameter : 0.008f;
            emblemObject.transform.localScale = Vector3.one * normalizedScale;

            SpriteRenderer spriteRenderer = emblemObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = emblemSprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            spriteRenderer.sortingOrder = 20;
            spriteRenderer.maskInteraction = SpriteMaskInteraction.None;
        }

        private void SpawnLabel(Transform parent, BeyPart part, int orientationIndex)
        {
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(parent, false);
            labelObj.transform.localPosition = new Vector3(0f, 0.45f, 0f);

            TextMesh text = labelObj.AddComponent<TextMesh>();
            text.text = spawnOrthogonalOrientations
                ? $"{part.PartName}\n{part.PartType}\nOri {orientationIndex}"
                : $"{part.PartName}\n{part.PartType}";
            text.characterSize = 0.06f;
            text.fontSize = 36;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
        }

        private void SpawnGround(int maxColumns)
        {
            float width = Mathf.Max(8f, maxColumns * partSpacingX + 2f);
            float depth = Mathf.Max(8f, OrderedPartTypes.Length * rowSpacingZ + 2f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "DebugGround";
            ground.transform.SetParent(transform, false);
            ground.transform.position = new Vector3(width * 0.5f - partSpacingX * 0.5f, -0.02f, depth * 0.5f - rowSpacingZ * 0.5f);
            ground.transform.localScale = new Vector3(width / 10f, 1f, depth / 10f);

            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(ShaderProvider.URPLit);
                mat.SetColor("_BaseColor", new Color(0.09f, 0.11f, 0.14f, 1f));
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.08f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);
                renderer.sharedMaterial = mat;
            }

            spawnedObjects.Add(ground);
        }

        private Dictionary<PartType, List<BeyPart>> GroupByType(List<BeyPart> allParts)
        {
            Dictionary<PartType, List<BeyPart>> grouped = new Dictionary<PartType, List<BeyPart>>();
            foreach (PartType type in OrderedPartTypes)
                grouped[type] = new List<BeyPart>();

            for (int i = 0; i < allParts.Count; i++)
            {
                BeyPart part = allParts[i];
                if (part == null)
                    continue;

                if (!grouped.ContainsKey(part.PartType))
                    grouped[part.PartType] = new List<BeyPart>();

                if (!grouped[part.PartType].Contains(part))
                    grouped[part.PartType].Add(part);
            }

            return grouped;
        }

        private List<BeyPart> LoadAllParts()
        {
            List<BeyPart> result = new List<BeyPart>();

            StarterPartsConfig config = Resources.Load<StarterPartsConfig>("StarterPartsConfig");
            if (config != null)
            {
                List<BeyPart> catalog = config.GetRuntimePartCatalog();
                for (int i = 0; i < catalog.Count; i++)
                {
                    BeyPart part = catalog[i];
                    if (part != null && !result.Contains(part))
                        result.Add(part);
                }
            }

#if UNITY_EDITOR
            // Always merge all BeyPart assets for debug-world inspection.
            // Starter/runtime catalog can be intentionally limited for gameplay,
            // but debug scene should show the full authored set (e.g. 150 sets).
            {
                string[] guids = AssetDatabase.FindAssets("t:BeyPart");
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    BeyPart part = AssetDatabase.LoadAssetAtPath<BeyPart>(path);
                    if (part != null && !result.Contains(part))
                        result.Add(part);
                }
            }
#endif

            return result;
        }

        private void ClearWorld()
        {
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = spawnedObjects[i];
                if (obj == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }
            spawnedObjects.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }
}