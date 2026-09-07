using UnityEngine;

namespace PieceBook.Lessons.Evaluation
{
    /// <summary>
    /// Pure geometric metrics over a stroke polyline (ARQUITECTURA §5 "Evaluación de trazos (sin
    /// ML en MVP)"). Every method returns a normalized 0..1 score where 1 is best, so they combine
    /// by simple averaging in <see cref="TraceEvaluator"/>. No Unity scene state, fully testable.
    /// </summary>
    public static class StrokeMetrics
    {
        private const float Eps = 1e-6f;

        /// <summary>Precision: 1 when every sample sits on the template, 0 at/over one tolerance away.</summary>
        public static float Precision(Vector2[] positions, TraceTemplate template, float tolerance)
        {
            if (positions == null || positions.Length == 0 || template == null || tolerance <= Eps)
                return 0f;

            float sum = 0f;
            for (int i = 0; i < positions.Length; i++) sum += template.DistanceTo(positions[i]);
            float mean = sum / positions.Length;
            return Mathf.Clamp01(1f - mean / tolerance);
        }

        /// <summary>
        /// Smoothness: penalizes curvature "jitter" (§5). Uses mean squared turning angle between
        /// consecutive segments — 0 for a straight/uniform curve (score 1), high for jagged input
        /// (score → 0).
        /// </summary>
        public static float Smoothness(Vector2[] positions)
        {
            if (positions == null || positions.Length < 3) return 1f;

            float sumSq = 0f;
            int count = 0;
            for (int i = 1; i < positions.Length - 1; i++)
            {
                Vector2 a = positions[i] - positions[i - 1];
                Vector2 b = positions[i + 1] - positions[i];
                if (a.sqrMagnitude < Eps || b.sqrMagnitude < Eps) continue;
                float ang = Vector2.Angle(a, b) * Mathf.Deg2Rad; // turning angle in radians [0..π]
                sumSq += ang * ang;
                count++;
            }
            if (count == 0) return 1f;

            float meanSq = sumSq / count;
            float refSq = (Mathf.PI * 0.5f) * (Mathf.PI * 0.5f); // a 90° average turn ⇒ score 0
            return Mathf.Clamp01(1f - meanSq / refSq);
        }

        /// <summary>Speed consistency: 1 for a steady hand, 0 for erratic pacing (§5). Uses the CV of segment speeds.</summary>
        public static float SpeedConsistency(Vector2[] positions, float[] times)
        {
            if (positions == null || times == null || positions.Length < 3 || times.Length != positions.Length)
                return 1f;

            int n = positions.Length - 1;
            var speeds = new float[n];
            for (int i = 0; i < n; i++)
            {
                float dt = times[i + 1] - times[i];
                speeds[i] = dt > Eps ? Vector2.Distance(positions[i], positions[i + 1]) / dt : 0f;
            }
            return OneMinusCv(speeds);
        }

        /// <summary>Line weight: stability of pressure/distance (§5). Uses the CV of pressure.</summary>
        public static float LineWeight(float[] pressures)
        {
            if (pressures == null || pressures.Length < 2) return 1f;
            return OneMinusCv(pressures);
        }

        /// <summary>1 - coefficient of variation (std/mean), clamped to [0,1]. Steady series → 1.</summary>
        private static float OneMinusCv(float[] values)
        {
            int n = values.Length;
            float mean = 0f;
            for (int i = 0; i < n; i++) mean += values[i];
            mean /= n;
            if (mean < Eps) return 0f;

            float var = 0f;
            for (int i = 0; i < n; i++) { float d = values[i] - mean; var += d * d; }
            var /= n;
            float cv = Mathf.Sqrt(var) / mean;
            return Mathf.Clamp01(1f - cv);
        }
    }
}
