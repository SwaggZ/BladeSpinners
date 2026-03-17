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
        private Camera mainCamera;

        private void Start()
        {
            startPos = transform.localPosition;
            mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            // Bob up and down
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = startPos + Vector3.up * yOffset;

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
