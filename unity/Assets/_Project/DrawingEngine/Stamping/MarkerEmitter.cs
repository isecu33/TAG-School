using PieceBook.DrawingEngine.Config;
using PieceBook.DrawingEngine.Drips;
using PieceBook.DrawingEngine.Interpolation;
using UnityEngine;

namespace PieceBook.DrawingEngine.Stamping
{
    /// <summary>
    /// Marker tool strategy (ARQUITECTURA §7 Strategy). Same Catmull-Rom smoothing as the spray,
    /// but a <b>hard edge and no overspray mist</b>: nozzle distance widens the tip slightly but
    /// never softens it into a cloud. This is the concrete proof that tools are interchangeable
    /// over one shared <see cref="GpuStamper"/> (ENG-07) without the canvas knowing which is active.
    /// Zero allocations during a stroke, like the spray.
    /// </summary>
    public sealed class MarkerEmitter : IStampStrategy
    {
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

        public void Push(Vector2 pos, float pressure, float nozzleDistance, GpuStamper stamper, DripSystem drips)
        {
            if (_filled < 4) _filled++;
            for (int i = 0; i < 3; i++) { _pts[i] = _pts[i + 1]; _press[i] = _press[i + 1]; }
            _pts[3] = pos;
            _press[3] = pressure;

            if (_filled < 4) { EmitAt(pos, pressure, nozzleDistance, stamper, drips); return; }

            EmitSegment(_pts[0], _pts[1], _pts[2], _pts[3], _press[1], _press[2], nozzleDistance, stamper, drips);
        }

        public void End(float nozzleDistance, GpuStamper stamper, DripSystem drips)
        {
            if (_filled >= 3)
                EmitSegment(_pts[1], _pts[2], _pts[3], _pts[3], _press[2], _press[3], nozzleDistance, stamper, drips);
            _filled = 0;
        }

        private void EmitSegment(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
                                 float pr1, float pr2, float nozzleDistance, GpuStamper stamper, DripSystem drips)
        {
            float radius = TipRadius(nozzleDistance);
            float segLen = Vector2.Distance(p1, p2);
            float spacing = Mathf.Max(0.0015f, radius * 0.4f); // dense spacing keeps the line solid
            int steps = Mathf.Clamp(Mathf.CeilToInt(segLen / spacing), 1, 256);

            for (int s = 1; s <= steps; s++)
            {
                float t = s / (float)steps;
                Vector2 c = CatmullRom.Evaluate(p0, p1, p2, p3, t);
                float pr = CatmullRom.EvaluateScalar(pr1, pr2, t);
                EmitAt(c, pr, nozzleDistance, stamper, drips);
            }
        }

        private void EmitAt(Vector2 center, float pressure, float nozzleDistance, GpuStamper stamper, DripSystem drips)
        {
            float radius = TipRadius(nozzleDistance);
            const float hardness = 1f; // crisp edge — the defining marker trait, no falloff cloud
            float perAlpha = Mathf.Clamp01(_cap.flowRate * _paint.opacity * Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(pressure)));

            Color col = _color;
            col.a = perAlpha;
            // A single solid dot per step (no scatter, no mist).
            stamper.Add(center, radius, col, hardness);

            drips?.DepositPaint(center, perAlpha, _paint.dripThreshold, radius, _color);
        }

        // Marker tip barely grows with distance and never sprays a cone.
        private float TipRadius(float nozzleDistance) => _cap.BaseRadius01 * (1f + nozzleDistance * 0.4f);
    }
}
