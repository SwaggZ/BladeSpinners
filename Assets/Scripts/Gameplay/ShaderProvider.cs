using UnityEngine;

namespace BladeSpinners.Gameplay
{
    /// <summary>
    /// Provides build-safe shader references by loading from Resources materials.
    /// Shader.Find() returns null in builds for shaders not referenced by any asset.
    /// This class loads reference materials from Resources/ to guarantee inclusion.
    /// </summary>
    public static class ShaderProvider
    {
        private static Shader urpLit;
        private static Shader urpUnlit;
        private static bool initialized;

        public static Shader URPLit
        {
            get
            {
                EnsureInitialized();
                return urpLit;
            }
        }

        public static Shader URPUnlit
        {
            get
            {
                EnsureInitialized();
                return urpUnlit;
            }
        }

        private static void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            // Load from reference materials — these are always included in builds
            Material litRef = Resources.Load<Material>("URPLitReference");
            if (litRef != null)
                urpLit = litRef.shader;

            Material unlitRef = Resources.Load<Material>("URPUnlitReference");
            if (unlitRef != null)
                urpUnlit = unlitRef.shader;

            // Fallback to Shader.Find (works in editor, may fail in builds)
            if (urpLit == null)
                urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
                urpLit = Shader.Find("Standard");

            if (urpUnlit == null)
                urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpUnlit == null)
                urpUnlit = Shader.Find("Unlit/Color");
        }

        /// <summary>Creates a build-safe emissive lit material.</summary>
        public static Material CreateEmissiveMaterial(Color color, float emissionMultiplier = 2.0f)
        {
            Material mat = new Material(URPLit);
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emissionMultiplier);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return mat;
        }

        /// <summary>Creates a build-safe unlit material.</summary>
        public static Material CreateUnlitMaterial(Color color)
        {
            Material mat = new Material(URPUnlit);
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            return mat;
        }
    }
}
