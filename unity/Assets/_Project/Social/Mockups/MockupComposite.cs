using UnityEngine;

namespace PieceBook.Social.Mockups
{
    /// <summary>
    /// Perspective placement math for wall mockups (ARQUITECTURA §6). Maps a point in the artwork's
    /// own normalized space [0,1]² onto the background photo using bilinear interpolation of the four
    /// placement corners — the cheap perspective a flat wall photo needs. Pure and testable; a
    /// shader/CPU pass uses this mapping to draw the artwork into the photo.
    /// </summary>
    public static class MockupComposite
    {
        /// <summary>Map an artwork-local point (0,0=bottom-left .. 1,1=top-right) to background space.</summary>
        public static Vector2 MapPoint(Vector2 local01, WallMockup m) =>
            MapPoint(local01, m.cornerBL, m.cornerBR, m.cornerTL, m.cornerTR);

        public static Vector2 MapPoint(Vector2 local01, Vector2 bl, Vector2 br, Vector2 tl, Vector2 tr)
        {
            float x = Mathf.Clamp01(local01.x);
            float y = Mathf.Clamp01(local01.y);
            Vector2 bottom = Vector2.LerpUnclamped(bl, br, x);
            Vector2 top = Vector2.LerpUnclamped(tl, tr, x);
            return Vector2.LerpUnclamped(bottom, top, y);
        }
    }
}
