using System;
using PieceBook.Core.Events;
using PieceBook.Core.Services;

namespace PieceBook.MetaGame
{
    /// <summary>
    /// Grants items the meta-game unlocks (ARQUITECTURA §3 "desbloqueos"). On <see cref="LessonPassed"/>
    /// it applies the lesson's <c>unlocks[]</c> (§9 LessonDef.unlocks) and publishes
    /// <see cref="ItemUnlocked"/> once per newly granted item (§7 Observer). Purchasing with the soft
    /// currency (using CapDef.unlockCost) is Fase 2 (META-05); the entry point lives here already.
    /// </summary>
    public sealed class UnlockService : IDisposable
    {
        private readonly ISaveService _save;
        private readonly EventBus _bus;

        public UnlockService(ISaveService save, EventBus bus)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _bus.Subscribe<LessonPassed>(OnLessonPassed);
        }

        private void OnLessonPassed(LessonPassed e)
        {
            if (e.Unlocks == null || e.Unlocks.Length == 0) return;

            bool changed = false;
            foreach (var id in e.Unlocks)
            {
                if (_save.Current.Unlock(id))
                {
                    changed = true;
                    _bus.Publish(new ItemUnlocked(id));
                }
            }
            if (changed) _save.Flush();
        }

        /// <summary>
        /// Try to buy an item for <paramref name="cost"/> coins (Fase 2 store entry point).
        /// Returns true and grants + persists if the player can afford it and doesn't own it.
        /// </summary>
        public bool TryPurchase(string itemId, int cost)
        {
            var p = _save.Current;
            if (string.IsNullOrEmpty(itemId) || p.IsUnlocked(itemId) || p.Coins < cost) return false;
            p.Coins -= cost;
            p.Unlock(itemId);
            _bus.Publish(new ItemUnlocked(itemId));
            _save.Flush();
            return true;
        }

        public void Dispose() => _bus.Unsubscribe<LessonPassed>(OnLessonPassed);
    }
}
