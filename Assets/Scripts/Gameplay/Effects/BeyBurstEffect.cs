using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BladeSpinners.Gameplay.Effects
{
    /// <summary>
    /// Burst (death) effect: stops the bey, detaches all part meshes,
    /// lets them fall to the ground, fades them over 7 seconds, then
    /// destroys the root bey GameObject.
    /// 
    /// Call TriggerBurst() to start the effect. Can be on any bey (player or enemy).
    /// </summary>
    public class BeyBurstEffect : MonoBehaviour
    {
        [Header("Despawn")]
        [SerializeField] private float fadeDuration = 7f; // seconds to fade out and destroy

        private bool hasBurst = false;

        /// <summary>
        /// Trigger the burst effect. Stops movement, detaches parts, lets them fall and fade.
        /// </summary>
        public void TriggerBurst()
        {
            if (hasBurst) return;
            hasBurst = true;

            Debug.Log($"[BurstEffect] TriggerBurst called on {gameObject.name}");

            // 1) Stop the bey
            StopBey();

            // 2) Find the model container (SpinChild) — parts are children of it
            Transform modelContainer = FindModelContainer();
            if (modelContainer == null)
            {
                Debug.LogWarning($"[BurstEffect] No model container found on {gameObject.name}! Hierarchy:");
                foreach (Transform child in transform)
                    Debug.LogWarning($"  child: {child.name} (children: {child.childCount})");
                Destroy(gameObject, 0.5f);
                return;
            }

            Debug.Log($"[BurstEffect] Found model container: {modelContainer.name} with {modelContainer.childCount} children");

            // 3) Gather all part GameObjects
            List<Transform> parts = new List<Transform>();
            for (int i = modelContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = modelContainer.GetChild(i);
                if (child.GetComponent<MeshRenderer>() != null)
                    parts.Add(child);
            }

            if (parts.Count == 0)
            {
                Debug.LogWarning("[BurstEffect] No part meshes found, destroying.");
                Destroy(gameObject, 0.5f);
                return;
            }

            // 4) Detach each part — they fall with gravity and fade out
            foreach (Transform part in parts)
            {
                DetachAndDrop(part);
            }

            // 5) Hide the root bey object (keep parts alive as world objects)
            DisableRootVisuals();

            Debug.Log($"[BurstEffect] {gameObject.name} BURST! {parts.Count} parts detached.");

            // 6) Destroy the root bey after the fade completes
            Destroy(gameObject, fadeDuration + 0.5f);
        }

        private void StopBey()
        {
            // Disable movement controller
            var movement = GetComponent<Movement.BeyMovementController>();
            if (movement != null) movement.enabled = false;

            // Disable AI
            var ai = GetComponent<AIInputHandler>();
            if (ai != null) ai.enabled = false;

            // Disable player input
            var playerInput = GetComponent<PlayerInputHandler>();
            if (playerInput != null) playerInput.enabled = false;

            // Freeze rigidbody
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // Disable collision detector
            var collisionDetector = GetComponent<Combat.BeyCollisionDetector>();
            if (collisionDetector != null) collisionDetector.enabled = false;
        }

        private Transform FindModelContainer()
        {
            // The model container is the SpinChild — look for it by name
            Transform spinChild = transform.Find("TiltPivot/SpinChild");
            if (spinChild != null) return spinChild;

            // Fallback: search all children for one that has Part_ grandchildren
            foreach (Transform child in transform)
            {
                foreach (Transform grandchild in child)
                {
                    if (grandchild.name.StartsWith("Part_"))
                        return child;
                    foreach (Transform greatGrandchild in grandchild)
                    {
                        if (greatGrandchild.name.StartsWith("Part_"))
                            return grandchild;
                    }
                }
            }

            return null;
        }

        private void DetachAndDrop(Transform part)
        {
            // World-space position before detaching
            Vector3 worldPos = part.position;
            Quaternion worldRot = part.rotation;

            // Unparent — make it a root-level object
            part.SetParent(null, true);
            part.position = worldPos;
            part.rotation = worldRot;

            // Set layer to Default so parts can collide with ground
            part.gameObject.layer = 0;

            // Add Rigidbody — just gravity, no lateral force
            Rigidbody partRb = part.GetComponent<Rigidbody>();
            if (partRb == null)
                partRb = part.gameObject.AddComponent<Rigidbody>();

            partRb.mass = 0.2f;
            partRb.linearDamping = 1f;
            partRb.angularDamping = 0.5f;
            partRb.useGravity = true;
            partRb.isKinematic = false;

            // Make MeshCollider convex if present (required for non-kinematic rigidbody)
            MeshCollider mc = part.GetComponent<MeshCollider>();
            if (mc != null)
                mc.convex = true;

            // Start fade + destroy coroutine
            BurstPartFade fade = part.gameObject.AddComponent<BurstPartFade>();
            fade.StartFade(fadeDuration);
        }

        private void DisableRootVisuals()
        {
            // Disable any remaining renderers on the bey hierarchy
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
                renderer.enabled = false;

            // Disable trigger collider
            foreach (var col in GetComponents<Collider>())
                col.enabled = false;
        }
    }

    /// <summary>
    /// Attached to each detached part. Fades the material alpha over time, then self-destructs.
    /// </summary>
    public class BurstPartFade : MonoBehaviour
    {
        private float duration;

        public void StartFade(float fadeDuration)
        {
            duration = fadeDuration;
            StartCoroutine(FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr == null)
            {
                Destroy(gameObject, duration);
                yield break;
            }

            // Grab the original color before replacing the material
            Color startColor = Color.white;
            if (mr.material != null && mr.material.HasProperty("_BaseColor"))
                startColor = mr.material.GetColor("_BaseColor");
            else if (mr.material != null && mr.material.HasProperty("_Color"))
                startColor = mr.material.GetColor("_Color");

            // Create a fresh Unlit transparent material — avoids URP Lit
            // shader variant issues when switching from Opaque to Transparent
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null)
                unlitShader = Shader.Find("Unlit/Color");

            Material fadeMat = new Material(unlitShader);
            fadeMat.SetColor("_BaseColor", startColor);

            // Configure for transparency
            fadeMat.SetFloat("_Surface", 1f);            // Transparent
            fadeMat.SetFloat("_Blend", 0f);              // Alpha blend
            fadeMat.SetOverrideTag("RenderType", "Transparent");
            fadeMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            fadeMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            fadeMat.SetInt("_ZWrite", 0);
            fadeMat.renderQueue = 3000;
            fadeMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            mr.material = fadeMat;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                fadeMat.SetColor("_BaseColor", new Color(startColor.r, startColor.g, startColor.b, alpha));
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
