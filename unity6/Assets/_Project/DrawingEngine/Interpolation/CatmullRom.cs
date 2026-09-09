using UnityEngine;

namespace PieceBook.DrawingEngine.Interpolation
{
    /// <summary>
    /// Centripetal-ish Catmull-Rom evaluation for smoothing raw touch/pen input
    /// (ARQUITECTURA §4.1 "se interpolan puntos del input (Catmull-Rom)").
    /// Pure math, zero allocation — the caller owns all buffers.
    /// </summary>
    public static class CatmullRom
    {
        /// <summary>
        /// Standard uniform Catmull-Rom point at parameter t in [0,1] on the segment p1..p2,
        /// using p0 and p3 as tangent neighbours.
        /// </summary>
        public static Vector2 Evaluate(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            // 0.5 * [ 2p1 + (-p0+p2)t + (2p0-5p1+4p2-p3)t^2 + (-p0+3p1-3p2+p3)t^3 ]
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        /// <summary>
        /// Linearly interpolate a scalar attribute (pressure, tilt) across the same segment.
        /// Catmull-Rom on the position, linear on the scalar keeps things cheap and stable.
        /// </summary>
        public static float EvaluateScalar(float a, float b, float t) => a + (b - a) * t;
    }
}
