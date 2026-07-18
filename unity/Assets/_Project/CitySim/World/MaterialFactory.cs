using UnityEngine;

namespace PieceBook.CitySim.World
{
    /// <summary>
    /// Tiny material helper for the gray blockout (NO art — cubes/capsules per the brief).
    /// Resolves shaders by name so it works under URP or Built-in, and sets colour on both
    /// URP (_BaseColor) and legacy (_Color) property names.
    /// </summary>
    public static class MaterialFactory
    {
        private static Shader LitShader =>
            Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        private static Shader UnlitTransparentShader =>
            Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");

        public static Material Solid(Color c)
        {
            var m = new Material(LitShader) { hideFlags = HideFlags.DontSave };
            Tint(m, c);
            return m;
        }

        /// <summary>Unlit, vertex-colour-friendly, alpha-blended material for cones/indicators.</summary>
        public static Material UnlitTransparent(Color c)
        {
            var m = new Material(UnlitTransparentShader) { hideFlags = HideFlags.DontSave };
            Tint(m, c);
            return m;
        }

        public static void Tint(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }
}
