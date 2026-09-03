using UnityEngine;

namespace BladeSpinners.World
{
    /// <summary>
    /// Attached to the generated arena bowl to store geometry metadata and
    /// provide global access to the active arena's radius, depth, and floor height.
    /// </summary>
    public class ArenaBowlMetadata : MonoBehaviour
    {
        public static ArenaBowlMetadata Active { get; private set; }

        public float Radius = 50f;
        public float Depth = 4.5f;
        public float FlatRatio = 0.28f;
        public float HoleRadiusRatio = 0f;
        public ArenaShapeDefinition Shape;

        private void OnEnable()
        {
            Active = this;
        }

        private void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        private void Awake()
        {
            Active = this;
        }

        /// <summary>
        /// Calculates the theoretical surface height of the arena bowl at a given horizontal distance from center.
        /// </summary>
        public float GetSurfaceHeightAt(float horizontalDist)
        {
            return ProceduralArenaGenerator.GetBowlHeight(horizontalDist, Radius, FlatRatio, Depth);
        }
    }
}
