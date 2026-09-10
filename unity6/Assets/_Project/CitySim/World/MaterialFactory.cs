using UnityEngine;
using UnityEngine.Rendering;

namespace PieceBook.CitySim.World
{
    /// <summary>
    /// Tiny material helper for the gray blockout (NO art — cubes/capsules per the brief).
    /// Resolves shaders by name so it works under URP or Built-in, and sets colour on both
    /// URP (_BaseColor) and legacy (_Color) property names. Pipeline-aware: it asks
    /// GraphicsSettings which render pipeline is active so a clean checkout without an assigned
    /// URP asset falls back to Standard/Sprites instead of rendering magenta.
    /// </summary>
    public static class MaterialFactory
    {
        private static bool UrpActive => GraphicsSettings.currentRenderPipeline != null;

        private static Shader LitShader =>
            UrpActive
                ? (Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                : (Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit"));

        private static Shader UnlitTransparentShader =>
            UrpActive
                ? (Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"))
                : (Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));

        public static Material Solid(Color c)
        {
            var m = new Material(LitShader) { hideFlags = HideFlags.DontSave };
            Tint(m, c);
            return m;
        }

        /// <summary>Unlit, alpha-blended material for cones/indicators. Under URP the Unlit
        /// shader is opaque by default, so we flip it to the transparent surface type here.</summary>
        public static Material UnlitTransparent(Color c)
        {
            var m = new Material(UnlitTransparentShader) { hideFlags = HideFlags.DontSave };
            ConfigureTransparent(m);
            Tint(m, c);
            return m;
        }

        public static void Tint(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        /// <summary>Adds a subtle emissive glow so a surface reads as a "target". No-op on
        /// shaders that lack an emission slot (e.g. Sprites/Default).</summary>
        public static void SetEmission(Material m, Color emission)
        {
            if (!m.HasProperty("_EmissionColor")) return;
            m.SetColor("_EmissionColor", emission);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        /// <summary>Flip a material to alpha-blended transparent. Harmless on shaders that are
        /// already blended (Sprites/Default); required for "Universal Render Pipeline/Unlit".</summary>
        private static void ConfigureTransparent(Material m)
        {
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);   // 0 opaque, 1 transparent
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);       // 0 = alpha
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
