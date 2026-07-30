using UnityEngine;

namespace BladeSpinners.World
{
    /// <summary>
    /// Bob animation + billboard facing for pickup sprites.
    /// Gently floats up and down, always faces the camera.
    /// </summary>
    public class PickupBobAnimation : MonoBehaviour
    {
        private float bobSpeed = 2f;
        private float bobHeight = 0.25f;
        private Vector3 startPos;
        private Vector3 visualStartScale;
        private Camera mainCamera;
        private PickupPlaceholder pickup;
        private SpriteRenderer spriteRenderer;
        private Transform visualTransform;

        private void Start()
        {
            startPos = transform.localPosition;
            mainCamera = Camera.main;
            pickup = GetComponent<PickupPlaceholder>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            visualTransform =
                spriteRenderer != null
                    ? spriteRenderer.transform
                    : null;
            visualStartScale =
                visualTransform != null
                    ? visualTransform.localScale
                    : Vector3.one;
        }

        private void LateUpdate()
        {
            // Bob up and down
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = startPos + Vector3.up * yOffset;

            float charge = pickup != null ? pickup.Charge01 : 1f;
            if (visualTransform != null)
            {
                visualTransform.localScale = visualStartScale
                    * Mathf.Lerp(0.45f, 1f, charge);
            }
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(0.12f, 0.55f, charge);
                spriteRenderer.color = color;
            }

            // Billboard — always face camera
            if (mainCamera == null)
                mainCamera = Camera.main;
            if (mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(
                    transform.position - mainCamera.transform.position,
                    Vector3.up);
            }
        }
    }
}
