using UnityEngine;

namespace PieceBook.Spike
{
    /// <summary>
    /// Generates a cheap placeholder wall texture (brick + grain) at runtime so the spike
    /// ships no art (NO HACER: arte final). Purely to give the spray something to sit on.
    /// </summary>
    public static class WallTextureFactory
    {
        public static Texture2D Create(int size, Color tint)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "SpikeWall",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];

            int brickH = size / 12;                 // rows
            int brickW = size / 6;                  // columns
            int mortar = Mathf.Max(2, size / 160);  // joint thickness
            Color mortarCol = tint * 0.72f;

            for (int y = 0; y < size; y++)
            {
                int row = y / brickH;
                int rowOffset = (row % 2 == 0) ? 0 : brickW / 2; // running bond
                for (int x = 0; x < size; x++)
                {
                    int bx = (x + rowOffset) % brickW;
                    int by = y % brickH;
                    bool isMortar = by < mortar || bx < mortar;

                    // per-brick tint variation + fine grain
                    float brickSeed = Mathf.PerlinNoise(row * 0.7f, ((x + rowOffset) / brickW) * 0.7f);
                    float grain = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.12f - 0.06f;

                    Color c = isMortar
                        ? mortarCol
                        : tint * Mathf.Lerp(0.82f, 1.05f, brickSeed);
                    c.r = Mathf.Clamp01(c.r + grain);
                    c.g = Mathf.Clamp01(c.g + grain);
                    c.b = Mathf.Clamp01(c.b + grain);
                    c.a = 1f;

                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }
    }
}
