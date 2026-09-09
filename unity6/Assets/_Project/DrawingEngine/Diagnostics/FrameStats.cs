using UnityEngine;

namespace PieceBook.DrawingEngine.Diagnostics
{
    /// <summary>
    /// Rolling FPS / frame-time meter for the 60 fps acceptance criterion (§4.2).
    /// Allocation-free ring of recent frame durations.
    /// </summary>
    public sealed class FrameStats
    {
        private readonly float[] _frames;
        private int _head;
        private int _count;

        public FrameStats(int window = 120)
        {
            _frames = new float[Mathf.Max(8, window)];
        }

        public void Sample(float unscaledDeltaTime)
        {
            _frames[_head] = unscaledDeltaTime;
            _head = (_head + 1) % _frames.Length;
            if (_count < _frames.Length) _count++;
        }

        public float AverageMs
        {
            get
            {
                if (_count == 0) return 0f;
                float sum = 0f;
                for (int i = 0; i < _count; i++) sum += _frames[i];
                return (sum / _count) * 1000f;
            }
        }

        public float WorstMs
        {
            get
            {
                float worst = 0f;
                for (int i = 0; i < _count; i++) if (_frames[i] > worst) worst = _frames[i];
                return worst * 1000f;
            }
        }

        public float AverageFps
        {
            get { float ms = AverageMs; return ms > 0.0001f ? 1000f / ms : 0f; }
        }
    }
}
