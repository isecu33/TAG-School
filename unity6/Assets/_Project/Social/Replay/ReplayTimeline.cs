using PieceBook.DrawingEngine;
using UnityEngine;

namespace PieceBook.Social.Replay
{
    /// <summary>
    /// Deterministic frame plan for rendering a <see cref="StrokeRecording"/> to video
    /// (ARQUITECTURA §4.1 "Replay: se graba la secuencia de inputs → re-renderizar el proceso a MP4
    /// en cualquier resolución"). Pure math: the same recording + fps always yields the same frame
    /// times, which is what makes the export reproducible.
    /// </summary>
    public static class ReplayTimeline
    {
        /// <summary>Gap inserted between consecutive strokes in the global replay timeline.</summary>
        public const float InterStrokeGap = 0.15f;

        /// <summary>
        /// Global duration of a recording: each stroke's own length (its last sample Time, which is
        /// relative to that stroke's start) plus a small gap between strokes.
        /// </summary>
        public static float Duration(StrokeRecording recording)
        {
            if (recording == null || recording.StrokeCount == 0) return 0f;
            float total = 0f;
            for (int i = 0; i < recording.Strokes.Count; i++)
            {
                var s = recording.Strokes[i];
                float len = s.Samples.Count > 0 ? s.Samples[s.Samples.Count - 1].Time : 0f;
                total += len;
                if (i < recording.Strokes.Count - 1) total += InterStrokeGap;
            }
            return total;
        }

        /// <summary>Number of frames at <paramref name="fps"/>, including t=0 and the final frame.</summary>
        public static int FrameCount(float durationSeconds, int fps)
        {
            if (fps <= 0) return 0;
            if (durationSeconds <= 0f) return 1;
            return Mathf.CeilToInt(durationSeconds * fps) + 1;
        }

        /// <summary>Evenly spaced frame timestamps in seconds, [0 .. duration].</summary>
        public static float[] FrameTimes(float durationSeconds, int fps)
        {
            int n = FrameCount(durationSeconds, fps);
            var times = new float[n];
            float step = 1f / Mathf.Max(1, fps);
            for (int i = 0; i < n; i++) times[i] = Mathf.Min(i * step, durationSeconds);
            return times;
        }
    }
}
