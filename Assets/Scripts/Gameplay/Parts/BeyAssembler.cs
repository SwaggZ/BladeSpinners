using UnityEngine;
using System.Collections.Generic;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Parts
{
    /// <summary>
    /// Assembles the visual Beyblade from equipped parts.
    /// 
    /// HOW TO USE:
    ///   1. Generate a part set via "GameObject → Blade Spinners → Generate Part Set"
    ///   2. Drag BeyPart assets into the 5 slots on this component in the Inspector
    ///   3. The model updates live — change a slot and the mesh/hitbox rebuilds instantly
    ///
    /// The assembler owns the part references. It pushes them into BeyConfiguration
    /// (which handles stats) and generates procedural meshes under BeyModel.
    /// </summary>
    [ExecuteInEditMode]
    public class BeyAssembler : MonoBehaviour
    {
        [Header("Part Slots — drag BeyPart assets here")]
        [SerializeField] private BeyPart tipPart;
        [SerializeField] private BeyPart trackPart;
        [SerializeField] private BeyPart fusionWheelPart;
        [SerializeField] private BeyPart energyRingPart;
        [SerializeField] private BeyPart faceBoltPart;

        [Header("References")]
        [SerializeField] private Transform beyModelTransform;

        /// <summary>
        /// Runtime BeyConfiguration that holds equipped parts and calculates stats.
        /// Set via reflection or SetConfiguration().
        /// </summary>
        private BeyConfiguration beyConfiguration;

        private Dictionary<PartType, GameObject> partObjects = new Dictionary<PartType, GameObject>();

        // Change detection — tracks last-seen asset instances to detect inspector changes
        private BeyPart lastTip, lastTrack, lastFusionWheel, lastEnergyRing, lastFaceBolt;

        private static Shader urpLitShader;
        private static PhysicsMaterial bouncyMaterial;

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                PushPartsToBeyConfiguration();
                Assemble();
            }
        }

        private void Update()
        {
            // Detect inspector slot changes (works in Edit mode and Play mode)
            if (HasPartsChanged())
            {
                ValidateSlots();
                SnapshotParts();
                PushPartsToBeyConfiguration();
                Assemble();
            }
        }

        /// <summary>
        /// Rejects any part placed in the wrong slot (e.g. FaceBolt in Tip slot).
        /// Nulls out mismatched assignments and logs a warning.
        /// </summary>
        private void ValidateSlots()
        {
            tipPart = ValidateSlot(tipPart, PartType.Tip, "Tip");
            trackPart = ValidateSlot(trackPart, PartType.Track, "Track");
            fusionWheelPart = ValidateSlot(fusionWheelPart, PartType.FusionWheel, "Fusion Wheel");
            energyRingPart = ValidateSlot(energyRingPart, PartType.EnergyRing, "Energy Ring");
            faceBoltPart = ValidateSlot(faceBoltPart, PartType.FaceBolt, "Face Bolt");
        }

        private BeyPart ValidateSlot(BeyPart part, PartType expectedType, string slotName)
        {
            if (part != null && part.PartType != expectedType)
            {
                Debug.LogWarning($"[BeyAssembler] '{part.PartName}' is a {part.PartType}, not a {expectedType}. " +
                                 $"Removed from {slotName} slot.");
                return null;
            }
            return part;
        }

        private void OnDestroy()
        {
            if (beyConfiguration != null)
                beyConfiguration.OnPartSwapped -= OnExternalPartSwap;
        }

        private void OnValidate()
        {
            // OnValidate fires when inspector values change in Edit mode
            // We can't call Assemble() here directly (not safe), so Update() will catch it
        }

        // ================================================================
        // INITIALIZATION
        // ================================================================

        private void EnsureInitialized()
        {
            if (urpLitShader == null)
            {
                // Try URP Lit first, then fallback
                urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLitShader == null)
                    urpLitShader = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (urpLitShader == null)
                    urpLitShader = Shader.Find("Standard"); // last resort
            }

            if (bouncyMaterial == null)
            {
                bouncyMaterial = new PhysicsMaterial("BeyPartBounce");
                bouncyMaterial.bounciness = 0.4f;
                bouncyMaterial.dynamicFriction = 0.1f;
                bouncyMaterial.staticFriction = 0.1f;
                bouncyMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
                bouncyMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;
            }
        }

        // ================================================================
        // PART MANAGEMENT
        // ================================================================

        /// <summary>
        /// Pushes the 5 inspector slots into the BeyConfiguration so stats are recalculated.
        /// </summary>
        private void PushPartsToBeyConfiguration()
        {
            if (beyConfiguration == null) return;

            if (tipPart != null) beyConfiguration.EquipPart(tipPart);
            else beyConfiguration.UnequipPart(PartType.Tip);

            if (trackPart != null) beyConfiguration.EquipPart(trackPart);
            else beyConfiguration.UnequipPart(PartType.Track);

            if (fusionWheelPart != null) beyConfiguration.EquipPart(fusionWheelPart);
            else beyConfiguration.UnequipPart(PartType.FusionWheel);

            if (energyRingPart != null) beyConfiguration.EquipPart(energyRingPart);
            else beyConfiguration.UnequipPart(PartType.EnergyRing);

            if (faceBoltPart != null) beyConfiguration.EquipPart(faceBoltPart);
            else beyConfiguration.UnequipPart(PartType.FaceBolt);
        }

        /// <summary>
        /// Detects if any inspector slot was changed since last check.
        /// </summary>
        private bool HasPartsChanged()
        {
            return tipPart != lastTip
                || trackPart != lastTrack
                || fusionWheelPart != lastFusionWheel
                || energyRingPart != lastEnergyRing
                || faceBoltPart != lastFaceBolt;
        }

        private void SnapshotParts()
        {
            lastTip = tipPart;
            lastTrack = trackPart;
            lastFusionWheel = fusionWheelPart;
            lastEnergyRing = energyRingPart;
            lastFaceBolt = faceBoltPart;
        }

        /// <summary>
        /// Called when BeyConfiguration.OnPartSwapped fires from external code
        /// (e.g. dungeon loot pickup). Syncs slots back from config.
        /// </summary>
        private void OnExternalPartSwap(PartType slot, BeyPart newPart)
        {
            // Sync inspector slots from config
            switch (slot)
            {
                case PartType.Tip: tipPart = newPart; break;
                case PartType.Track: trackPart = newPart; break;
                case PartType.FusionWheel: fusionWheelPart = newPart; break;
                case PartType.EnergyRing: energyRingPart = newPart; break;
                case PartType.FaceBolt: faceBoltPart = newPart; break;
            }
            SnapshotParts();
            Assemble();
        }

        // ================================================================
        // ASSEMBLY — builds meshes from parts
        // ================================================================

        /// <summary>
        /// Full reassembly: clears BeyModel children, generates procedural meshes
        /// for each equipped part, stacks them vertically, rebuilds hitbox.
        /// </summary>
        public void Assemble()
        {
            if (beyModelTransform == null)
                return;

            EnsureInitialized();
            ClearExistingParts();

            BeyPart[] parts = { tipPart, trackPart, fusionWheelPart, energyRingPart, faceBoltPart };
            PartType[] slots = { PartType.Tip, PartType.Track, PartType.FusionWheel, PartType.EnergyRing, PartType.FaceBolt };

            // First pass: generate all meshes EXCEPT EnergyRing (needs constraints from FW & FB)
            Mesh[] meshes = new Mesh[parts.Length];
            float totalHeight = 0f;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null) continue;
                if (slots[i] == PartType.EnergyRing) continue; // deferred
                meshes[i] = ProceduralPartMeshGenerator.GenerateMesh(parts[i]);
                if (meshes[i] != null)
                    totalHeight += meshes[i].bounds.size.y;
            }

            // Generate EnergyRing constrained by FusionWheel (max width) and FaceBolt (max hole)
            int erIndex = System.Array.IndexOf(slots, PartType.EnergyRing);
            if (parts[erIndex] != null)
            {
                int fwIndex = System.Array.IndexOf(slots, PartType.FusionWheel);
                int fbIndex = System.Array.IndexOf(slots, PartType.FaceBolt);

                float fwMaxRadius = (meshes[fwIndex] != null)
                    ? Mathf.Max(meshes[fwIndex].bounds.extents.x, meshes[fwIndex].bounds.extents.z)
                    : float.MaxValue;
                float fbRadius = (parts[fbIndex] != null)
                    ? ProceduralPartMeshGenerator.GetFaceBoltRadius(parts[fbIndex])
                    : float.MaxValue;

                meshes[erIndex] = ProceduralPartMeshGenerator.GenerateConstrainedEnergyRing(
                    parts[erIndex], fwMaxRadius, fbRadius);
                if (meshes[erIndex] != null)
                    totalHeight += meshes[erIndex].bounds.size.y;
            }

            // Second pass: build, stack using mesh connection points (top of previous = bottom of next)
            float currentY = -totalHeight / 2f;

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null || meshes[i] == null)
                    continue;

                Mesh partMesh = meshes[i];
                Bounds meshBounds = partMesh.bounds;

                // Connection point: position so this mesh's bottom (bounds.min.y) sits at currentY
                float meshBottomOffset = meshBounds.min.y;
                float localY = currentY - meshBottomOffset;

                GameObject partObj = new GameObject($"Part_{slots[i]}");
                partObj.transform.SetParent(beyModelTransform, false);
                partObj.transform.localPosition = new Vector3(0, localY, 0);
                partObj.transform.localRotation = Quaternion.identity;
                partObj.hideFlags = HideFlags.DontSave; // Don't serialize generated objects

                // Inherit layer from root bey (Bey layer) so physics ignoring works
                partObj.layer = beyModelTransform.root.gameObject.layer;

                MeshFilter mf = partObj.AddComponent<MeshFilter>();
                mf.sharedMesh = partMesh;

                MeshRenderer mr = partObj.AddComponent<MeshRenderer>();
                Material mat = new Material(urpLitShader != null ? urpLitShader : Shader.Find("Universal Render Pipeline/Lit"));

                // URP Lit uses _BaseColor (not _Color), _Metallic, _Smoothness (not _Glossiness)
                mat.SetColor("_BaseColor", parts[i].PrimaryColor);

                // Material style per slot
                switch (slots[i])
                {
                    case PartType.FusionWheel:
                        mat.SetFloat("_Metallic", 0.8f);
                        mat.SetFloat("_Smoothness", 0.6f);
                        break;
                    case PartType.EnergyRing:
                        mat.SetFloat("_Metallic", 0.3f);
                        mat.SetFloat("_Smoothness", 0.8f);
                        break;
                    case PartType.FaceBolt:
                        mat.SetFloat("_Metallic", 0.9f);
                        mat.SetFloat("_Smoothness", 0.7f);
                        break;
                    case PartType.Tip:
                        mat.SetFloat("_Metallic", 0.5f);
                        mat.SetFloat("_Smoothness", 0.5f);
                        break;
                    case PartType.Track:
                        mat.SetFloat("_Metallic", 0.6f);
                        mat.SetFloat("_Smoothness", 0.4f);
                        break;
                }

                mr.sharedMaterial = mat;

                // MeshCollider — this is how the player and enemies physically hit parts
                MeshCollider mc = partObj.AddComponent<MeshCollider>();
                mc.sharedMesh = partMesh;
                mc.convex = true; // required for Rigidbody interaction
                mc.material = bouncyMaterial; // bouncy physics for realistic Beyblade collisions

                partObjects[slots[i]] = partObj;

                // Advance currentY to the top of this mesh (connection point for the next part)
                currentY += meshBounds.size.y;
            }
        }



        // ================================================================
        // CLEANUP
        // ================================================================

        private void ClearExistingParts()
        {
            foreach (var kvp in partObjects)
            {
                if (kvp.Value != null)
                {
                    if (Application.isPlaying)
                        Destroy(kvp.Value);
                    else
                        DestroyImmediate(kvp.Value);
                }
            }
            partObjects.Clear();

            // Also destroy any leftover children
            for (int i = beyModelTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = beyModelTransform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        // ================================================================
        // PUBLIC API
        // ================================================================

        /// <summary>
        /// Set the BeyConfiguration at runtime. Subscribes to swap events.
        /// </summary>
        public void SetConfiguration(BeyConfiguration config)
        {
            if (beyConfiguration != null)
                beyConfiguration.OnPartSwapped -= OnExternalPartSwap;

            beyConfiguration = config;

            if (beyConfiguration != null)
            {
                beyConfiguration.OnPartSwapped += OnExternalPartSwap;
                PushPartsToBeyConfiguration();
                Assemble();
            }
        }

        /// <summary>
        /// Equip a part into the matching slot. Updates inspector, config, and model.
        /// </summary>
        public void EquipPart(BeyPart part)
        {
            if (part == null) return;

            switch (part.PartType)
            {
                case PartType.Tip: tipPart = part; break;
                case PartType.Track: trackPart = part; break;
                case PartType.FusionWheel: fusionWheelPart = part; break;
                case PartType.EnergyRing: energyRingPart = part; break;
                case PartType.FaceBolt: faceBoltPart = part; break;
            }

            SnapshotParts();
            PushPartsToBeyConfiguration();
            Assemble();
        }

        /// <summary>
        /// Get the part equipped in a slot.
        /// </summary>
        public BeyPart GetEquippedPart(PartType slot)
        {
            return slot switch
            {
                PartType.Tip => tipPart,
                PartType.Track => trackPart,
                PartType.FusionWheel => fusionWheelPart,
                PartType.EnergyRing => energyRingPart,
                PartType.FaceBolt => faceBoltPart,
                _ => null
            };
        }

        /// <summary>
        /// Gets the generated visual GameObject for a part slot (for VFX attachment, etc.)
        /// </summary>
        public GameObject GetPartObject(PartType slot)
        {
            return partObjects.TryGetValue(slot, out GameObject obj) ? obj : null;
        }

        /// <summary>
        /// Gets the approximate collider radius from the assembled mesh colliders.
        /// </summary>
        public float GetColliderRadius()
        {
            float maxRadius = 0.15f;
            foreach (var kvp in partObjects)
            {
                if (kvp.Value == null) continue;
                MeshCollider mc = kvp.Value.GetComponent<MeshCollider>();
                if (mc != null && mc.sharedMesh != null)
                {
                    float r = Mathf.Max(mc.sharedMesh.bounds.extents.x, mc.sharedMesh.bounds.extents.z);
                    if (r > maxRadius) maxRadius = r;
                }
            }
            return maxRadius;
        }

        /// <summary>
        /// Forces a re-assembly.
        /// </summary>
        public void ForceReassemble()
        {
            PushPartsToBeyConfiguration();
            Assemble();
        }
    }
}
