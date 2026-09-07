using UnityEngine;

namespace PieceBook.Lessons.Evaluation
{
    /// <summary>
    /// A trace target as a polyline in normalized canvas space [0,1] (ARQUITECTURA §5). Precision
    /// is the mean distance from the player's samples to this line. §5 calls for a *precomputed*
    /// distance field per template; this class computes exact point-to-polyline distance on the
    /// fly, which is correct and cheap for slice-sized templates. A baked <c>DistanceField</c> is
    /// a drop-in optimization later (same <see cref="DistanceTo"/> signature).
    /// </summary>
    public sealed class TraceTemplate
    {
        public readonly Vector2[] Points;

        public TraceTemplate(Vector2[] points)
        {
            Points = points ?? new Vector2[0];
        }

        /// <summary>Shortest distance from <paramref name="p"/> to any segment of the polyline.</summary>
        public float DistanceTo(Vector2 p)
        {
            if (Points.Length == 0) return 0f;
            if (Points.Length == 1) return Vector2.Distance(p, Points[0]);

            float best = float.MaxValue;
            for (int i = 0; i < Points.Length - 1; i++)
            {
                float d = DistancePointToSegment(p, Points[i], Points[i + 1]);
                if (d < best) best = d;
            }
            return best;
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 1e-12f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            Vector2 proj = a + t * ab;
            return Vector2.Distance(p, proj);
        }
    }
}
