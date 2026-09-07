using System;
using System.Collections.Generic;

namespace PieceBook.UI.App
{
    /// <summary>App-level screens (ARQUITECTURA §7 "State Machine: Menu→Lesson→Paint→Review").</summary>
    public enum AppState { Menu, Lesson, Paint, Review }

    /// <summary>
    /// The app navigation state machine (UI-01). Pure C# — no MonoBehaviour, no scene — so it is
    /// fully testable (§7 MVP). It only guards which transitions are legal and raises
    /// <see cref="StateChanged"/>; the presenters react to that to show/hide their views.
    /// </summary>
    public sealed class AppStateMachine
    {
        private static readonly Dictionary<AppState, AppState[]> Allowed = new Dictionary<AppState, AppState[]>
        {
            { AppState.Menu,   new[] { AppState.Lesson } },
            { AppState.Lesson, new[] { AppState.Paint, AppState.Review, AppState.Menu } },
            { AppState.Paint,  new[] { AppState.Lesson, AppState.Review } },
            { AppState.Review, new[] { AppState.Menu, AppState.Lesson } },
        };

        public AppState Current { get; private set; } = AppState.Menu;

        public event Action<AppState, AppState> StateChanged; // (from, to)

        public bool CanGoTo(AppState next)
        {
            return Allowed.TryGetValue(Current, out var outs) && Array.IndexOf(outs, next) >= 0;
        }

        /// <summary>Transition to <paramref name="next"/>. Throws on an illegal transition (§7).</summary>
        public void GoTo(AppState next)
        {
            if (next == Current) return;
            if (!CanGoTo(next))
                throw new InvalidOperationException($"Illegal transition {Current} → {next}.");

            var from = Current;
            Current = next;
            StateChanged?.Invoke(from, next);
        }
    }
}
