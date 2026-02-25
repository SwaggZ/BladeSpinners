using UnityEngine;

namespace BladeSpinners.Gameplay
{
    /// <summary>
    /// Makes the Bey visually spin based on its velocity/movement speed.
    /// Rotates the child visual representation around the Y axis.
    /// The spin speed increases with movement speed.
    /// </summary>
    public class BeyVisualSpin : MonoBehaviour
    {
        [SerializeField]
        private Transform visualRoot; // The sphere/Bey visual to rotate

        [SerializeField]
        private float spinSpeedMultiplier = 50f; // How fast it spins per m/s of movement

        [SerializeField]
        private float maxSpinSpeed = 3600f; // Max degrees per second

        private Rigidbody rb;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();

            // If no visual root assigned, try to find the child Sphere
            if (visualRoot == null && transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
            }
        }

        private void Update()
        {
            // Visual spin is now handled by BeyTiltController which combines
            // tilt + continuous Y-axis spin into one rotation.
            // This component is kept for backwards compatibility but does nothing.
        }
    }
}
