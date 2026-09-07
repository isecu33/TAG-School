using PieceBook.DrawingEngine.Drips;
using UnityEngine;

namespace PieceBook.DrawingEngine.Stamping
{
    /// <summary>
    /// A tool's stamping behaviour (ARQUITECTURA §7 "Strategy: cada tipo de herramienta —spray,
    /// marker, roller— es una estrategia de stamping intercambiable"). The canvas owns the shared
    /// <see cref="GpuStamper"/> and swaps strategies per stroke; each strategy decides how input
    /// samples become stamps (cone + overspray for spray, a hard edge for marker, etc.).
    /// </summary>
    public interface IStampStrategy
    {
        void Begin(in StrokeConfig cfg);

        /// <summary>Feed one smoothed input sample. <paramref name="nozzleDistance"/> is 0 (tight) .. 1 (wide).</summary>
        void Push(Vector2 pos, float pressure, float nozzleDistance, GpuStamper stamper, DripSystem drips);

        /// <summary>Flush the trailing segment when the stroke ends.</summary>
        void End(float nozzleDistance, GpuStamper stamper, DripSystem drips);
    }
}
