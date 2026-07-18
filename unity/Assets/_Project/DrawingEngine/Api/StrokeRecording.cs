using System.Collections.Generic;
using UnityEngine;

namespace PieceBook.DrawingEngine
{
    /// <summary>
    /// One raw input sample. We record inputs, not frames (ARQUITECTURA §4.1 "Replay:
    /// se graba la secuencia de inputs"), so a whole session re-renders to MP4 at any
    /// resolution for pennies of disk.
    /// </summary>
    public struct StrokeSample
    {
        public Vector2 Pos;      // normalized canvas space [0,1]
        public float Pressure;
        public float Tilt;
        public float Time;       // seconds since stroke start
    }

    /// <summary>Header + samples for a single stroke.</summary>
    public sealed class RecordedStroke
    {
        public string CapId;
        public string PaintId;
        public Color Color;
        public readonly List<StrokeSample> Samples;

        public RecordedStroke(int sampleCapacity = 256)
        {
            Samples = new List<StrokeSample>(sampleCapacity);
        }

        public void Reset(string capId, string paintId, Color color)
        {
            CapId = capId;
            PaintId = paintId;
            Color = color;
            Samples.Clear();
        }
    }

    /// <summary>
    /// The replayable record of a drawing session. Cheap to keep in memory, cheap on
    /// disk, and the source of truth for undo/redo determinism and video export.
    /// </summary>
    public sealed class StrokeRecording
    {
        public readonly List<RecordedStroke> Strokes = new List<RecordedStroke>(64);

        public int StrokeCount => Strokes.Count;

        public int TotalSamples
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Strokes.Count; i++) n += Strokes[i].Samples.Count;
                return n;
            }
        }

        public void Clear() => Strokes.Clear();
    }
}
