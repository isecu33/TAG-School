using NUnit.Framework;
using PieceBook.Core.Events;
using PieceBook.Core.Save;
using PieceBook.Core.Services;
using PieceBook.MetaGame;

namespace PieceBook.Tests.EditMode
{
    /// <summary>EditMode tests for the meta-game (Fase 1: META-01 progression, META-02 unlocks).</summary>
    public class MetaGameEditTests
    {
        private static (ISaveService save, EventBus bus, IProgressRepo repo) NewWorld()
        {
            var repo = new InMemoryProgressRepo();
            return (new SaveService(repo), new EventBus(), repo);
        }

        [Test]
        public void Progression_RecordsCrownsCoinsStreak_AndPersists()
        {
            var (save, bus, repo) = NewWorld();
            using (new ProgressionService(save, bus) { CoinsPerCrown = 10 })
            {
                bus.Publish(new LessonPassed("lesson_tag_01", 2, System.Array.Empty<string>()));
            }

            // Persisted: a fresh service over the same repo sees it.
            var reopened = new SaveService(repo);
            Assert.AreEqual(2, reopened.Current.Crowns["lesson_tag_01"]);
            Assert.AreEqual(20, reopened.Current.Coins);
            Assert.AreEqual(1, reopened.Current.Streak);
        }

        [Test]
        public void Progression_CoinsAwardedOnlyOnImprovement()
        {
            var (save, bus, _) = NewWorld();
            using (new ProgressionService(save, bus) { CoinsPerCrown = 10 })
            {
                bus.Publish(new LessonPassed("l", 2, null)); // +20
                bus.Publish(new LessonPassed("l", 1, null)); // no improvement → no coins
            }
            Assert.AreEqual(20, save.Current.Coins);
            Assert.AreEqual(2, save.Current.Crowns["l"]);
        }

        [Test]
        public void Unlock_AppliesLessonUnlocks_AndPublishesOnce()
        {
            var (save, bus, _) = NewWorld();
            int unlockEvents = 0;
            bus.Subscribe<ItemUnlocked>(_ => unlockEvents++);

            using (new UnlockService(save, bus))
            {
                bus.Publish(new LessonPassed("l", 3, new[] { "cap_x" }));
                bus.Publish(new LessonPassed("l2", 3, new[] { "cap_x" })); // already owned → no event
            }

            Assert.IsTrue(save.Current.IsUnlocked("cap_x"));
            Assert.AreEqual(1, unlockEvents, "ItemUnlocked must fire once per newly granted item.");
        }

        [Test]
        public void Unlock_Purchase_RespectsBalanceAndOwnership()
        {
            var (save, bus, _) = NewWorld();
            save.Current.Coins = 50;
            var unlocks = new UnlockService(save, bus);

            Assert.IsFalse(unlocks.TryPurchase("cap_pricey", 80), "Cannot afford.");
            Assert.IsTrue(unlocks.TryPurchase("cap_ok", 30), "Affordable purchase succeeds.");
            Assert.AreEqual(20, save.Current.Coins);
            Assert.IsFalse(unlocks.TryPurchase("cap_ok", 10), "Already owned.");
            unlocks.Dispose();
        }
    }
}
