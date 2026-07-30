using BladeSpinners.Core;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Parts;
using UnityEngine;

namespace BladeSpinners.Gameplay.Effects
{
    public static class AbilityEmblemHologramEffect
    {
        public static void Spawn(BeyMovementController controller)
        {
            if (controller == null || controller.BeyConfiguration == null)
                return;

            BeyPart faceBolt = controller.BeyConfiguration.GetEquippedPart(PartType.FaceBolt);
            if (faceBolt == null || faceBolt.FaceBoltEmblem == null)
                return;

            Vector3 position = controller.transform.position + Vector3.up * 1.75f;
            GameObject root = new GameObject("AbilityEmblemHologram");
            root.transform.position = position;

            SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = faceBolt.FaceBoltEmblem;
            spriteRenderer.sortingOrder = 300;
            spriteRenderer.color = new Color(0.45f, 0.95f, 1f, 0.5f);

            Material material = new Material(ShaderProvider.URPUnlit);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", spriteRenderer.color);

            spriteRenderer.material = material;

            AbilityEmblemHologramRuntime runtime = root.AddComponent<AbilityEmblemHologramRuntime>();
            runtime.Initialize(1.05f, 1.9f, 0.24f);
        }
    }

    public class AbilityEmblemHologramRuntime : MonoBehaviour
    {
        private float duration;
        private float riseSpeed;
        private float endScale;
        private float elapsed;
        private Vector3 startScale;
        private SpriteRenderer spriteRenderer;

        public void Initialize(float lifetime, float risePerSecond, float targetScale)
        {
            duration = Mathf.Max(0.1f, lifetime);
            riseSpeed = risePerSecond;
            endScale = Mathf.Max(0.1f, targetScale);
            startScale = new Vector3(0.25f, 0.25f, 1f);
            transform.localScale = startScale;
            spriteRenderer = GetComponent<SpriteRenderer>();
            Destroy(gameObject, duration + 0.05f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            transform.position += Vector3.up * riseSpeed * Time.deltaTime;
            float scale = Mathf.Lerp(startScale.x, endScale, t);
            transform.localScale = new Vector3(scale, scale, 1f);

            Camera camera = Camera.main;
            if (camera != null)
            {
                Vector3 direction = transform.position - camera.transform.position;
                if (direction.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(0.5f, 0f, t);
                spriteRenderer.color = color;
                if (spriteRenderer.material != null && spriteRenderer.material.HasProperty("_BaseColor"))
                    spriteRenderer.material.SetColor("_BaseColor", color);
            }
        }
    }
}