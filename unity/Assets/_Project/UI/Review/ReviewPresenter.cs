using System;
using System.Collections.Generic;
using PieceBook.Core.Events;
using PieceBook.Core.Services;

namespace PieceBook.UI.Review
{
    /// <summary>Dumb review view (§7 MVP): shows crowns and what was unlocked.</summary>
    public interface IReviewView
    {
        void ShowResult(int crowns, IReadOnlyList<string> unlockedItems);
        void Hide();
    }

    /// <summary>
    /// Presenter for the post-lesson review (UI-04). Listens for <see cref="LessonPassed"/> and the
    /// <see cref="ItemUnlocked"/> events that follow it on the bus (§3), then shows crowns + unlocks
    /// and fires the success haptic. The reward *sound* is already handled by AudioService reacting
    /// to the same events (CORE-03), so the presenter doesn't duplicate it.
    /// </summary>
    public sealed class ReviewPresenter : IDisposable
    {
        private readonly IReviewView _view;
        private readonly IHaptics _haptics;
        private readonly EventBus _bus;
        private readonly List<string> _pendingUnlocks = new List<string>(4);

        public ReviewPresenter(IReviewView view, IHaptics haptics, EventBus bus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _haptics = haptics ?? throw new ArgumentNullException(nameof(haptics));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _bus.Subscribe<ItemUnlocked>(OnItemUnlocked);
            _bus.Subscribe<LessonPassed>(OnLessonPassed);
        }

        // ItemUnlocked is published during LessonPassed handling, so collect them for the summary.
        private void OnItemUnlocked(ItemUnlocked e) => _pendingUnlocks.Add(e.ItemId);

        private void OnLessonPassed(LessonPassed e)
        {
            _view.ShowResult(e.Crowns, _pendingUnlocks.ToArray());
            _haptics.Success();
            _pendingUnlocks.Clear();
        }

        public void Dispose()
        {
            _bus.Unsubscribe<ItemUnlocked>(OnItemUnlocked);
            _bus.Unsubscribe<LessonPassed>(OnLessonPassed);
        }
    }
}
