using UnityEngine;

namespace PieceBook.Core.Events
{
    /// <summary>
    /// Cross-module drawing signals (ARQUITECTURA §3, §7). The Drawing Engine
    /// publishes these; Lessons / MetaGame / Social subscribe. Declared in Core so
    /// no module needs a direct reference to the Drawing Engine to listen.
    /// </summary>
    public readonly struct StrokeStarted : IEvent
    {
        public readonly int LayerIndex;
        public StrokeStarted(int layerIndex) { LayerIndex = layerIndex; }
    }

    public readonly struct StrokeCompleted : IEvent
    {
        public readonly int LayerIndex;
        public readonly int SampleCount;
        public readonly float DurationSeconds;
        public StrokeCompleted(int layerIndex, int sampleCount, float durationSeconds)
        {
            LayerIndex = layerIndex;
            SampleCount = sampleCount;
            DurationSeconds = durationSeconds;
        }
    }

    /// <summary>Raised the frame a procedural drip is born (ENG-04).</summary>
    public readonly struct DripSpawned : IEvent
    {
        public readonly Vector2 CanvasUv;
        public readonly float Length;
        public DripSpawned(Vector2 canvasUv, float length)
        {
            CanvasUv = canvasUv;
            Length = length;
        }
    }
}
