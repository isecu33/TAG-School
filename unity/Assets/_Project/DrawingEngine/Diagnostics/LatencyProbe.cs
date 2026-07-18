using UnityEngine;

namespace PieceBook.DrawingEngine.Diagnostics
{
    /// <summary>
    /// Measures the slice of input-to-pixel latency the engine actually controls: the time
    /// from when an input sample is received to when its stamps are submitted to the GPU
    /// (§4.2 target &lt; 30 ms, ideal 16).
    ///
    /// This is NOT the full motion-to-photon latency — OS touch sampling, compositor and
    /// display scan-out add device-dependent milliseconds on top. Read the on-device number
    /// as the engine's contribution; validate end-to-end with a high-speed camera on target
    /// hardware (see INFORME-GO-NOGO.md).
    /// </summary>
    public sealed class LatencyProbe
    {
        private readonly float[] _samplesMs;
        private int _head;
        private int _count;

        private float _pendingInputTime;
        private bool _hasPending;

        public LatencyProbe(int window = 120)
        {
            _samplesMs = new float[Mathf.Max(8, window)];
        }

        /// <summary>Call when an input sample is accepted (realtimeSinceStartup).</summary>
        public void MarkInput(float realtime)
        {
            // Keep the OLDEST unsubmitted input time so we measure worst-case queueing.
            if (!_hasPending)
            {
                _pendingInputTime = realtime;
                _hasPending = true;
            }
        }

        /// <summary>Call right after the frame's stamps are submitted to the GPU.</summary>
        public void MarkSubmitted(float realtime)
        {
            if (!_hasPending) return;
            float ms = (realtime - _pendingInputTime) * 1000f;
            _samplesMs[_head] = ms;
            _head = (_head + 1) % _samplesMs.Length;
            if (_count < _samplesMs.Length) _count++;
            _hasPending = false;
        }

        public float LastMs => _count == 0 ? 0f : _samplesMs[(_head - 1 + _samplesMs.Length) % _samplesMs.Length];

        public float AverageMs
        {
            get
            {
                if (_count == 0) return 0f;
                float sum = 0f;
                for (int i = 0; i < _count; i++) sum += _samplesMs[i];
                return sum / _count;
            }
        }

        public float WorstMs
        {
            get
            {
                float worst = 0f;
                for (int i = 0; i < _count; i++) if (_samplesMs[i] > worst) worst = _samplesMs[i];
                return worst;
            }
        }
    }
}
