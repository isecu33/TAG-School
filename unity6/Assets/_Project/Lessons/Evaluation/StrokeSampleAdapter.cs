using PieceBook.DrawingEngine;
using UnityEngine;

namespace PieceBook.Lessons.Evaluation
{
    /// <summary>
    /// Adapts a Drawing Engine <see cref="RecordedStroke"/> (raw input samples, §4.1) into the
    /// flat arrays <see cref="StrokeMetrics"/> consumes. Keeps the evaluator decoupled from the
    /// engine's recording type while still scoring exactly what the player drew.
    /// </summary>
    public static class StrokeSampleAdapter
    {
        public static void Extract(RecordedStroke stroke, out Vector2[] positions, out float[] times, out float[] pressures)
        {
            int n = stroke?.Samples.Count ?? 0;
            positions = new Vector2[n];
            times = new float[n];
            pressures = new float[n];
            for (int i = 0; i < n; i++)
            {
                var s = stroke.Samples[i];
                positions[i] = s.Pos;
                times[i] = s.Time;
                pressures[i] = s.Pressure;
            }
        }
    }
}
