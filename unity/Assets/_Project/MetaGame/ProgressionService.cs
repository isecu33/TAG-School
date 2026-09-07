using System;
using PieceBook.Core.Events;
using PieceBook.Core.Services;

namespace PieceBook.MetaGame
{
    /// <summary>
    /// Turns lesson results into persistent progression (ARQUITECTURA §3 META-GAME, §9 Progress).
    /// Subscribes to <see cref="LessonPassed"/> on the typed <see cref="EventBus"/> (§7 Observer) and
    /// records best crowns, awards soft currency, and bumps the streak — then flushes through
    /// <see cref="ISaveService"/>. It never touches the repo directly (§7 Repository via the service).
    /// </summary>
    public sealed class ProgressionService : IDisposable
    {
        private readonly ISaveService _save;
        private readonly EventBus _bus;

        /// <summary>Coins granted per crown earned on a lesson.</summary>
        public int CoinsPerCrown = 10;

        public ProgressionService(ISaveService save, EventBus bus)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _bus.Subscribe<LessonPassed>(OnLessonPassed);
        }

        private void OnLessonPassed(LessonPassed e)
        {
            var progress = _save.Current;
            bool improved = progress.RecordCrowns(e.LessonId, e.Crowns);

            // Award coins only for *new* best crowns so replays don't farm currency.
            if (improved) progress.Coins += e.Crowns * CoinsPerCrown;

            // Slice-level streak: +1 per lesson cleared. Real day-based streak is Fase 2.
            progress.Streak += 1;

            _save.Flush();
        }

        public void Dispose() => _bus.Unsubscribe<LessonPassed>(OnLessonPassed);
    }
}
