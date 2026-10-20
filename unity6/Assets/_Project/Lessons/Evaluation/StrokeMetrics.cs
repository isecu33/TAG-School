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

        /// <summary>
        /// Line weight measured on the real line width: a spray line thickens with less pressure
        /// (nozzle further away) and thins when the hand moves fast (<see cref="SprayWidth"/>).
        /// Without this a mouse or finger (pressure always 1) scored a perfect weight for free.
        /// </summary>
        public static float LineWeight(Vector2[] positions, float[] times, float[] pressures)
        {
            if (positions == null || positions.Length < 3 || times == null || times.Length != positions.Length)
                return LineWeight(pressures);

            int n = positions.Length;
            var raw = new float[n];
            for (int i = 0; i < n; i++)
            {
                int a = Mathf.Max(0, i - 1), b = Mathf.Min(n - 1, i + 1);
                float dt = times[b] - times[a];
                raw[i] = dt > Eps ? Vector2.Distance(positions[a], positions[b]) / dt : 0f;
            }
            var widths = new float[n];
            for (int i = 0; i < n; i++)
            {
                // 3-sample moving average: per-frame input is noisy, the hand's speed is not.
                int a = Mathf.Max(0, i - 1), b = Mathf.Min(n - 1, i + 1);
                float v = 0f;
                for (int k = a; k <= b; k++) v += raw[k];
                v /= b - a + 1;
                float p = pressures != null && i < pressures.Length ? pressures[i] : 1f;
                widths[i] = SprayWidth(p, v);
            }
            return OneMinusCv(widths);
        }

        /// <summary>Speed (canvas widths/s) below which the line keeps its full width.</summary>
        public const float ThinStartSpeed = 0.25f;
        /// <summary>Speed range over which the line thins down to <see cref="MaxThinning"/>.</summary>
        public const float ThinRangeSpeed = 1.25f;
        /// <summary>Fraction of the width lost at full speed.</summary>
        public const float MaxThinning = 0.35f;

        /// <summary>
        /// Relative width of a spray line for a pressure (0..1) and hand speed (canvas widths/s).
        /// Same model as the web demo's engine, so both score the same drawing the same way.
        /// </summary>
        public static float SprayWidth(float pressure, float speed)
        {
            float distance = 1f + 0.5f * (1f - Mathf.Clamp01(pressure));
            float thin = 1f - MaxThinning * Mathf.Clamp01((speed - ThinStartSpeed) / ThinRangeSpeed);
            return distance * thin;
        }

        /// <summary>
        /// Coverage: fraction of the template the stroke actually goes over (0..1). Precision alone
        /// only measures stroke→template distance, so a single dot on the line scores 1; coverage
        /// measures template→stroke and is used as a gate ("trace the whole letter") before scoring.
        /// The template is probed every <paramref name="step"/> units of arc length.
        /// </summary>
        public static float Coverage(Vector2[] positions, TraceTemplate template, float tolerance, float step = 0.02f)
        {
            if (positions == null || positions.Length == 0 || template == null || template.Points.Length == 0)
                return 0f;

            var stroke = new TraceTemplate(positions);
            var pts = template.Points;
            int probes = 0, hits = 0;
            if (pts.Length == 1)
                return stroke.DistanceTo(pts[0]) <= tolerance ? 1f : 0f;

            for (int i = 0; i < pts.Length - 1; i++)
            {
                float len = Vector2.Distance(pts[i], pts[i + 1]);
                int n = Mathf.Max(1, Mathf.CeilToInt(len / step));
                for (int k = (i == 0 ? 0 : 1); k <= n; k++)
                {
                    probes++;
                    if (stroke.DistanceTo(Vector2.Lerp(pts[i], pts[i + 1], k / (float)n)) <= tolerance) hits++;
                }
            }
            return probes > 0 ? hits / (float)probes : 0f;
        }

        /// <summary>
        /// Maps a free-hand drawing onto the template's bounding box (translate + per-axis scale).
        /// A freeform step has no guide, so Precision is measured on the shape, not the position.
        /// </summary>
        public static Vector2[] AlignToTemplate(Vector2[] positions, TraceTemplate template)
        {
            if (positions == null || positions.Length == 0 || template == null || template.Points.Length == 0)
                return positions;

            Bounds(positions, out var aMin, out var aMax);
            Bounds(template.Points, out var bMin, out var bMax);
            var aSize = Vector2.Max(aMax - aMin, new Vector2(Eps, Eps));
            var bSize = bMax - bMin;

            var aligned = new Vector2[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                var t = positions[i] - aMin;
                aligned[i] = bMin + new Vector2(t.x / aSize.x * bSize.x, t.y / aSize.y * bSize.y);
            }
            return aligned;
        }

        private static void Bounds(Vector2[] pts, out Vector2 min, out Vector2 max)
        {
            min = pts[0];
            max = pts[0];
            for (int i = 1; i < pts.Length; i++)
            {
                min = Vector2.Min(min, pts[i]);
                max = Vector2.Max(max, pts[i]);
            }
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
