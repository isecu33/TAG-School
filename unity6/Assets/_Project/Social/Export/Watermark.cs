using UnityEngine;

namespace PieceBook.Social.Export
{
    /// <summary>
    /// Applies a subtle game watermark to exported artwork (ARQUITECTURA §6 "Marca de agua sutil
    /// … crecimiento orgánico"). Fase 2 ships a procedural corner bar (like the spike's generated
    /// art); a branded logo texture replaces it later. Premium users skip the mark — driven by a
    /// flag that Remote Config will own in Fase 3 (DATA-04), a constant for now.
    ///
    /// Pure CPU pixel op on a readable <see cref="Texture2D"/>, so it is fully testable.
    /// </summary>
    public static class Watermark
    {
        /// <summary>Height of the watermark bar as a fraction of the image height.</summary>
        public const float BarHeightFraction = 0.06f;

        /// <summary>Apply the default watermark in-place. No-op when <paramref name="premium"/> is true.</summary>
        public static void ApplyDefault(Texture2D tex, bool premium = false)
        {
            if (premium || tex == null) return;

            int w = tex.width;
            int h = tex.height;
            int barH = Mathf.Max(1, Mathf.RoundToInt(h * BarHeightFraction));
            var tint = new Color(0f, 0f, 0f, 0.35f); // subtle dark strip

            var pixels = tex.GetPixels(0, 0, w, barH); // bottom strip
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.Lerp(pixels[i], tint, tint.a);
            tex.SetPixels(0, 0, w, barH, pixels);
            tex.Apply(false);
        }

        /// <summary>True if a pixel row is inside the watermark bar (used by tests / UI overlays).</summary>
        public static bool IsInBar(int y, int textureHeight) =>
            y >= 0 && y < Mathf.Max(1, Mathf.RoundToInt(textureHeight * BarHeightFraction));
    }
}
