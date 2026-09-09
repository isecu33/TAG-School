using System.Collections.Generic;
using NUnit.Framework;
using PieceBook.Core.DI;
using PieceBook.Core.Events;
using PieceBook.Core.Save;
using PieceBook.Core.Services;

namespace PieceBook.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the CORE / SERVICES layer (Fase 1: CORE-01..04). Pure logic only —
    /// no scene, no real AudioSource — thanks to the injected backends (§7 MVP).
    /// </summary>
    public class CoreServicesEditTests
    {
        private sealed class FakeAudioBackend : IAudioBackend
        {
            public readonly List<string> OneShots = new List<string>();
            public readonly List<string> Started = new List<string>();
            public readonly List<string> Stopped = new List<string>();
            public void PlayOneShot(string id) => OneShots.Add(id);
            public void StartLoop(string id) => Started.Add(id);
            public void StopLoop(string id) => Stopped.Add(id);
        }

        // ---- CORE-01: DI ----------------------------------------------------

        [Test]
        public void Container_ResolvesSaveService_NonNull()
        {
            var container = new ServiceContainer();
            CoreInstaller.Install(container, new EventBus(), new FakeAudioBackend(), new InMemoryProgressRepo());

            var save = container.Resolve<ISaveService>();
            Assert.IsNotNull(save, "ISaveService must resolve from the root scope (CORE-01).");
        }

        [Test]
        public void Container_UnregisteredService_Throws()
        {
            var container = new ServiceContainer();
            Assert.Throws<System.InvalidOperationException>(() => container.Resolve<ISaveService>());
        }

        [Test]
        public void Container_SingletonFactory_RunsOnce()
        {
            var container = new ServiceContainer();
            CoreInstaller.Install(container, new EventBus(), new FakeAudioBackend(), new InMemoryProgressRepo());
            Assert.AreSame(container.Resolve<ISaveService>(), container.Resolve<ISaveService>(),
                "A registered singleton must return the same instance every time.");
        }

        // ---- CORE-02: SaveService round-trip -------------------------------

        [Test]
        public void SaveService_RoundTripsProgress()
        {
            var repo = new InMemoryProgressRepo();
            var save = new SaveService(repo);
            save.Current.RecordCrowns("lesson_tag_01", 2);
            save.Current.Unlock("cap_ny_fat");
            save.Current.Streak = 3;
            save.Flush();

            // A brand-new service over the same repo must see the persisted state.
            var reopened = new SaveService(repo);
            Assert.AreEqual(2, reopened.Current.Crowns["lesson_tag_01"]);
            Assert.IsTrue(reopened.Current.IsUnlocked("cap_ny_fat"));
            Assert.AreEqual(3, reopened.Current.Streak);
        }

        [Test]
        public void ProgressSerializer_RoundTrip_IsLossless()
        {
            var p = new Progress();
            p.RecordCrowns("a", 1);
            p.RecordCrowns("b", 3);
            p.Unlock("cap_x");
            p.BlackbookPages.Add("art_1");
            p.Coins = 42;
            p.Streak = 7;

            var back = ProgressSerializer.FromJson(ProgressSerializer.ToJson(p));
            Assert.AreEqual(1, back.Crowns["a"]);
            Assert.AreEqual(3, back.Crowns["b"]);
            Assert.IsTrue(back.IsUnlocked("cap_x"));
            CollectionAssert.Contains(back.BlackbookPages, "art_1");
            Assert.AreEqual(42, back.Coins);
            Assert.AreEqual(7, back.Streak);
        }

        [Test]
        public void RecordCrowns_KeepsBestOnly()
        {
            var p = new Progress();
            Assert.IsTrue(p.RecordCrowns("l", 2));
            Assert.IsFalse(p.RecordCrowns("l", 1), "A worse result must not overwrite the best.");
            Assert.IsTrue(p.RecordCrowns("l", 3), "A better result must overwrite.");
            Assert.AreEqual(3, p.Crowns["l"]);
        }

        // ---- CORE-03: AudioService reacts to the bus ----------------------

        [Test]
        public void AudioService_SprayLoop_FollowsStrokeEvents()
        {
            var bus = new EventBus();
            var backend = new FakeAudioBackend();
            using (new AudioService(backend, bus))
            {
                bus.Publish(new StrokeStarted(0));
                CollectionAssert.Contains(backend.Started, SfxId.SprayLoop);

                bus.Publish(new StrokeCompleted(0, 10, 0.5f));
                CollectionAssert.Contains(backend.Stopped, SfxId.SprayLoop);
            }
        }

        [Test]
        public void AudioService_RewardStings_FollowMetaEvents()
        {
            var bus = new EventBus();
            var backend = new FakeAudioBackend();
            using (new AudioService(backend, bus))
            {
                bus.Publish(new LessonPassed("lesson_tag_01", 3, new[] { "cap_x" }));
                bus.Publish(new ItemUnlocked("cap_x"));
                CollectionAssert.Contains(backend.OneShots, SfxId.Crown);
                CollectionAssert.Contains(backend.OneShots, SfxId.Unlock);
            }
        }

        // ---- CORE-04: Haptics is a safe no-op off-device -----------------

        [Test]
        public void NullHaptics_NeverThrows()
        {
            IHaptics h = new NullHaptics();
            Assert.DoesNotThrow(() => { h.Light(); h.Success(); });
        }
    }
}
