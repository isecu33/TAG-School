using PieceBook.CitySim.AI;
using PieceBook.Core.Events;
using UnityEngine;

namespace PieceBook.CitySim.Events
{
    /// <summary>
    /// CitySim signals, published on the Core EventBus (ARQUITECTURA §7 Observer). They
    /// implement Core's <see cref="IEvent"/> but are DECLARED here, so CitySim never
    /// modifies Core — it only depends on the stable EventBus type (§3 "todos pueden
    /// depender de Core"). If a future build must drop the Core dependency, these become
    /// plain C# events; noted in SPIKE-B-REPORT.md.
    /// </summary>
    public readonly struct PatrolStateChanged : IEvent
    {
        public readonly PatrolState From;
        public readonly PatrolState To;
        public PatrolStateChanged(PatrolState from, PatrolState to) { From = from; To = to; }
    }

    public readonly struct LoopPhaseChanged : IEvent
    {
        public readonly LoopPhase From;
        public readonly LoopPhase To;
        public LoopPhaseChanged(LoopPhase from, LoopPhase to) { From = from; To = to; }
    }

    public readonly struct PlayerSpotted : IEvent
    {
        public readonly Vector3 Position;
        public PlayerSpotted(Vector3 position) { Position = position; }
    }

    public readonly struct ZoneHeatChanged : IEvent
    {
        public readonly float Heat; // 0..1
        public ZoneHeatChanged(float heat) { Heat = heat; }
    }
}
