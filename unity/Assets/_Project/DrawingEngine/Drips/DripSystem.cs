using System.Collections.Generic;
using PieceBook.Core.Events;
using PieceBook.DrawingEngine.Pooling;
using PieceBook.DrawingEngine.Stamping;
using UnityEngine;

namespace PieceBook.DrawingEngine.Drips
{
    /// <summary>
    /// Procedural drip simulation (ENG-04 / §4.1). Owns a <see cref="PaintAccumulator"/>
    /// and a pool of <see cref="Drip"/> instances. Spawns a drip when accumulated paint
    /// beats the paint's dripThreshold, then advances live drips each frame, stamping
    /// their trail through the shared <see cref="GpuStamper"/>. Zero allocations per stroke
    /// (§7 Flyweight + Object Pool).
    /// </summary>
    public sealed class DripSystem
    {
        private const float Gravity = 0.18f; // canvas units / s^2 — tuned for readable runs

        private readonly PaintAccumulator _accum;
        private readonly ObjectPool<Drip> _pool;
        private readonly List<Drip> _active = new List<Drip>(64);
        private readonly EventBus _bus;

        public int ActiveDrips => _active.Count;

        public DripSystem(EventBus bus, int accumResolution = 96, int prewarm = 64)
        {
            _bus = bus;
            _accum = new PaintAccumulator(accumResolution);
            _pool = new ObjectPool<Drip>(() => new Drip(), prewarm);
        }

        public void Reset()
        {
            for (int i = 0; i < _active.Count; i++) _pool.Return(_active[i]);
            _active.Clear();
            _accum.Clear();
        }

        /// <summary>
        /// Register paint landing at a point. If the cell tips over the threshold, spawn a drip.
        /// </summary>
        public void DepositPaint(Vector2 pos01, float amount, float dripThreshold, float coreRadius, Color color)
        {
            int idx = _accum.Deposit(pos01, amount);
            float loaded = _accum.Get(idx);
            if (loaded < dripThreshold) return;

            // Length scales with how far past the threshold we went — heavier build-up runs longer.
            float excess = loaded - dripThreshold;
            float length = Mathf.Lerp(0.04f, 0.22f, Mathf.Clamp01(excess / dripThreshold));
            length *= (0.8f + Random.value * 0.4f);

            var drip = _pool.Get();
            drip.Init(_accum.CellCenter01(idx), length, coreRadius * 0.55f, color);
            _active.Add(drip);
            _accum.Consume(idx);

            if (_bus != null) _bus.Publish(new DripSpawned(drip.Pos, length));
        }

        /// <summary>Advance and stamp all live drips. Call once per frame with the frame stamper.</summary>
        public void Tick(float dt, GpuStamper stamper)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var drip = _active[i];
                bool alive = drip.Step(dt, Gravity);

                // Drips read a touch darker/wetter than the spray mist: near-solid core.
                stamper.Add(drip.Pos, drip.Radius, drip.Color, 0.85f);

                if (!alive)
                {
                    _pool.Return(drip);
                    // swap-remove keeps the list compact without shifting
                    _active[i] = _active[_active.Count - 1];
                    _active.RemoveAt(_active.Count - 1);
                }
            }
        }
    }
}
