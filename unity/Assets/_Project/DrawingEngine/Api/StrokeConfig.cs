using PieceBook.DrawingEngine.Config;
using UnityEngine;

namespace PieceBook.DrawingEngine
{
    /// <summary>
    /// Everything a stroke needs to exist: cap + paint + color + initial pressure
    /// (ARQUITECTURA §4.3 comment "cap+pintura+color+presión inicial").
    /// Passed by value to <see cref="IDrawingCanvas.BeginStroke"/>; the engine copies
    /// what it needs, so no allocation happens on the hot path.
    /// </summary>
    public struct StrokeConfig
    {
        /// <summary>Cap geometry: cone, density, falloff, flow, overspray (§9 CapDef).</summary>
        public CapDef Cap;

        /// <summary>Paint material: opacity, gloss, dripThreshold, color range (§9 PaintDef).</summary>
        public PaintDef Paint;

        /// <summary>Ink color for this stroke.</summary>
        public Color Color;

        /// <summary>Pressure reported at stroke start (Pencil) or slider value on touch.</summary>
        public float InitialPressure;

        public StrokeConfig(CapDef cap, PaintDef paint, Color color, float initialPressure = 1f)
        {
            Cap = cap;
            Paint = paint;
            Color = color;
            InitialPressure = Mathf.Clamp01(initialPressure);
        }
    }
}
