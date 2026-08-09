using PieceBook.CitySim.AI;
using UnityEngine;

namespace PieceBook.CitySim.Loop
{
    /// <summary>
    /// The "ventana de pintado" (GD-02 §2): the estimated safe time you can paint a surface
    /// before the patrol's cone sweeps back over it. The estimate is a forward simulation of
    /// the patrol's cycle (<see cref="PatrolAgent.TimeUntilVisible"/>), so it recharges when
    /// the patrol wanders off and shrinks as it approaches — matching the real cycle.
    /// </summary>
    public sealed class PaintingWindow
    {
        private PatrolAgent _patrol;
        private Vector3 _surfacePos;
        private float _horizon;
        private float _cycle;

        // Countdown state (used while actually painting — point 4).
        private float _remaining;
        private float _recompute;

        public float Cycle => _cycle;
        public float Remaining => _remaining;
        public bool Expired => _remaining <= 0.02f;

        /// <summary>Ring fill 0..1 = safe time left relative to a full patrol cycle.</summary>
        public float Fraction => _cycle > 0.01f ? Mathf.Clamp01(_remaining / _cycle) : 0f;

        public void Bind(PatrolAgent patrol, Vector3 surfacePos)
        {
            _patrol = patrol;
            _surfacePos = surfacePos;
            _cycle = Mathf.Max(4f, patrol.EstimateCycleSeconds());
            _horizon = _cycle * 1.6f;
        }

        /// <summary>Live estimate for the observe/explore phase (no consumption).</summary>
        public float LiveEstimate()
        {
            if (_patrol == null) return 0f;
            _remaining = _patrol.TimeUntilVisible(_surfacePos, _horizon);
            return _remaining;
        }

        /// <summary>Start consuming the window (entering painting).</summary>
        public void BeginConsume()
        {
            _remaining = _patrol != null ? _patrol.TimeUntilVisible(_surfacePos, _horizon) : 0f;
            _recompute = 0.4f;
        }

        /// <summary>Consume time while painting; periodically re-estimate to allow recharge.</summary>
        public void Tick(float dt)
        {
            _remaining -= dt;
            _recompute -= dt;
            if (_recompute <= 0f)
            {
                _recompute = 0.4f;
                if (_patrol != null)
                {
                    // Take the fresh estimate but never let a re-estimate resurrect an expired
                    // window mid-cast unless the patrol genuinely moved away (estimate grew).
                    float fresh = _patrol.TimeUntilVisible(_surfacePos, _horizon);
                    _remaining = Mathf.Max(_remaining, 0f);
                    if (fresh > _remaining) _remaining = Mathf.Lerp(_remaining, fresh, 0.5f);
                }
            }
        }

        public static Color Urgency(float fraction) =>
            fraction > 0.5f ? new Color(0.4f, 0.9f, 0.4f)
          : fraction > 0.2f ? new Color(1f, 0.85f, 0.3f)
          : new Color(1f, 0.35f, 0.35f);
    }
}
