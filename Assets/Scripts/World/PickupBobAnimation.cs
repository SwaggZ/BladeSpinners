using UnityEngine;

namespace BladeSpinners.World
{
    /// <summary>
    /// Simple bob animation for pickup placeholder objects.
    /// Gently floats up and down to indicate collectibility.
    /// </summary>
    public class PickupBobAnimation : MonoBehaviour
    {
        private float bobSpeed = 2f;
        private float bobHeight = 0.15f;
        private float rotateSpeed = 90f;
        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.localPosition;
        }

        private void Update()
        {
            // Bob up and down
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = startPos + Vector3.up * yOffset;

            // Slow rotation
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.Self);
        }
    }
}
