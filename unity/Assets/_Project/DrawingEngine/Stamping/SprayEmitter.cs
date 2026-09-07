using PieceBook.DrawingEngine.Config;
using PieceBook.DrawingEngine.Drips;
using PieceBook.DrawingEngine.Interpolation;
using UnityEngine;

namespace PieceBook.DrawingEngine.Stamping
{
    /// <summary>
    /// Turns smoothed input into spray particles. This is where the teaching mechanic
    /// lives (ARQUITECTURA §4.1): nozzle distance widens the cone, softens the edge and
    /// grows the overspray mist in real time. Pure value-type math + UnityEngine.Random,
    /// so it never allocates during a stroke.
    /// </summary>
    public sealed class SprayEmitter : IStampStrategy
    {
        // Rolling window of the last four control points for the Catmull-Rom segment.
        private readonly Vector2[] _pts = new Vector2[4];
        private readonly float[] _press = new float[4];
        private int _filled;

        private CapDef _cap;
        private PaintDef _paint;
        private Color _color;

        public void Begin(in StrokeConfig cfg)
        {
            _cap = cfg.Cap;
            _paint = cfg.Paint;
            _color = cfg.Color;
            _filled = 0;
        }

        /// <summary>
        /// Feed one input sample. Once four points are buffered, the completed central
        /// segment (p1->p2) is stamped. <paramref name="nozzleDistance"/> is 0 (nozzle on
        /// the wall = tight) .. 1 (far = wide, soft, misty).
        /// </summary>
        public void Push(Vector2 pos, float pressure, float nozzleDistance,
                         GpuStamper stamper, DripSystem drips)
        {
            // shift window
            if (_filled < 4) _filled++;
            for (int i = 0; i < 3; i++) { _pts[i] = _pts[i + 1]; _press[i] = _press[i + 1]; }
            _pts[3] = pos;
            _press[3] = pressure;

            if (_filled < 4)
            {
                // Not enough points yet for a real segment — lay a single dot so taps register.
                EmitAt(pos, pressure, nozzleDistance, stamper, drips);
                return;
            }

            EmitSegment(_pts[0], _pts[1], _pts[2], _pts[3], _press[1], _press[2],
                        nozzleDistance, stamper, drips);
        }

        /// <summary>Flush the final trailing segment when the stroke ends.</summary>
        public void End(float nozzleDistance, GpuStamper stamper, DripSystem drips)
        {
            if (_filled >= 3)
            {
                // Emit p2->p3 using p3 duplicated as the outgoing tangent.
                EmitSegment(_pts[1], _pts[2], _pts[3], _pts[3], _press[2], _press[3],
                            nozzleDistance, stamper, drips);
            }
            _filled = 0;
        }

        private void EmitSegment(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
                                 float pr1, float pr2, float nozzleDistance,
                                 GpuStamper stamper, DripSystem drips)
        {
            float coreRadius = CoreRadius(nozzleDistance);
            float segLen = Vector2.Distance(p1, p2);
            float spacing = Mathf.Max(0.0015f, coreRadius * 0.5f);
            int steps = Mathf.Clamp(Mathf.CeilToInt(segLen / spacing), 1, 256);

            for (int s = 1; s <= steps; s++)
            {
                float t = s / (float)steps;
                Vector2 c = CatmullRom.Evaluate(p0, p1, p2, p3, t);
                float pr = CatmullRom.EvaluateScalar(pr1, pr2, t);
                EmitAt(c, pr, nozzleDistance, stamper, drips);
            }
        }

        /// <summary>Emit one puff: scattered core particles + optional outer overspray mist.</summary>
        private void EmitAt(Vector2 center, float pressure, float nozzleDistance,
                            GpuStamper stamper, DripSystem drips)
        {
            float coreRadius = CoreRadius(nozzleDistance);
            float hardness = Mathf.Clamp01(_cap.falloff * (1f - nozzleDistance * 0.75f));
            float flowScale = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(pressure));
            float perAlpha = Mathf.Clamp01(_cap.flowRate * _paint.opacity * flowScale);

            // --- core particles (the line itself) ---
            int coreCount = Mathf.Max(1, Mathf.CeilToInt(_cap.density * 0.25f));
            Color col = _color;
            col.a = perAlpha;
            for (int i = 0; i < coreCount; i++)
            {
                Vector2 jitter = Random.insideUnitCircle * coreRadius;
                float r = coreRadius * Random.Range(0.15f, 0.35f);
                stamper.Add(center + jitter, r, col, hardness);
            }

            // paint build-up feeds drip detection at the puff center
            drips?.DepositPaint(center, perAlpha, _paint.dripThreshold, coreRadius, _color);

            // --- overspray mist (grows with distance) ---
            int mist = Mathf.CeilToInt(_cap.overspray * nozzleDistance * 8f);
            if (mist > 0)
            {
                Color mistCol = _color;
                mistCol.a = perAlpha * 0.3f;
                for (int i = 0; i < mist; i++)
                {
                    Vector2 dir = Random.insideUnitCircle;
                    Vector2 p = center + dir * (coreRadius * Random.Range(1.0f, 2.2f));
                    float r = coreRadius * Random.Range(0.08f, 0.2f);
                    stamper.Add(p, r, mistCol, hardness * 0.5f);
                }
            }
        }

        private float CoreRadius(float nozzleDistance)
            => _cap.BaseRadius01 * (1f + nozzleDistance * 2.5f);
    }
}
