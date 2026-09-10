using System.Collections.Generic;
using NUnit.Framework;
using PieceBook.Core.Data;
using PieceBook.Core.Save;
using PieceBook.Core.Services;

namespace PieceBook.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the device-independent Fase 3 core (DATA-01/02/04, CI-02): sync
    /// reconciliation, age-gated analytics and remote config with local defaults. All pure, no
    /// network, no Firebase — the backend transports live behind interfaces (validated on device).
    /// </summary>
    public class Fase3CoreEditTests
    {
        // ---- DATA-02: sync reconciliation ---------------------------------

        [Test]
        public void Reconciler_MergesAchievementsMonotonically()
        {
            var local = new Progress();
            local.Crowns["l1"] = 2; local.Unlock("cap_a"); local.BlackbookPages.Add("art1"); local.Streak = 3;
            var remote = new Progress();
            remote.Crowns["l1"] = 3; remote.Crowns["l2"] = 1; remote.Unlock("cap_b"); remote.Streak = 5;

            var m = SyncReconciler.Merge(local, remote);
            Assert.AreEqual(3, m.Crowns["l1"], "crowns take the best of both");
            Assert.AreEqual(1, m.Crowns["l2"]);
            CollectionAssert.AreEquivalent(new[] { "cap_a", "cap_b" }, m.UnlockedItems, "unlocks unioned");
            CollectionAssert.Contains(m.BlackbookPages, "art1");
            Assert.AreEqual(5, m.Streak, "streak takes the max");
        }

        [Test]
        public void Reconciler_Coins_LastWriteWins()
        {
            var older = new Progress { Coins = 100, LastModifiedUnix = 1000 };
            var newer = new Progress { Coins = 40, LastModifiedUnix = 2000 };

            Assert.AreEqual(40, SyncReconciler.Merge(older, newer).Coins, "newer timestamp wins even if fewer coins");
            Assert.AreEqual(40, SyncReconciler.Merge(newer, older).Coins, "order-independent");

            var tieA = new Progress { Coins = 10, LastModifiedUnix = 500 };
            var tieB = new Progress { Coins = 70, LastModifiedUnix = 500 };
            Assert.AreEqual(70, SyncReconciler.Merge(tieA, tieB).Coins, "tie → larger balance");
        }

        private sealed class FakeRemoteStore : IRemoteProgressStore
        {
            public Progress Remote;
            public Progress Pushed;
            public bool Available = true;
            public bool IsAvailable => Available;
            public Progress Fetch() => Remote;
            public void Push(Progress p) => Pushed = p;
        }

        [Test]
        public void SyncService_Offline_IsNoOp()
        {
            var save = new SaveService(new InMemoryProgressRepo());
            save.Current.Coins = 50;
            var store = new FakeRemoteStore { Available = false };
            var sync = new ProgressSyncService(save, store);

            Assert.IsFalse(sync.Sync(), "offline → no sync");
            Assert.IsNull(store.Pushed, "nothing pushed offline");
            Assert.AreEqual(50, save.Current.Coins, "local progress intact");
        }

        [Test]
        public void SyncService_Online_MergesSavesAndPushes()
        {
            var save = new SaveService(new InMemoryProgressRepo());
            save.Current.Crowns["l1"] = 1;
            save.Current.LastModifiedUnix = 2000; save.Current.Coins = 30;

            var remote = new Progress();
            remote.Crowns["l1"] = 3; remote.Crowns["l2"] = 2;
            remote.LastModifiedUnix = 1000; remote.Coins = 999;
            var store = new FakeRemoteStore { Remote = remote, Available = true };

            var sync = new ProgressSyncService(save, store);
            Assert.IsTrue(sync.Sync());

            Assert.AreEqual(3, save.Current.Crowns["l1"], "best crowns kept");
            Assert.AreEqual(2, save.Current.Crowns["l2"], "remote-only lesson merged in");
            Assert.AreEqual(30, save.Current.Coins, "local coins win (newer timestamp)");
            Assert.IsNotNull(store.Pushed, "merged snapshot pushed back");
            Assert.AreEqual(3, store.Pushed.Crowns["l1"]);
        }

        // ---- CI-02 / DATA-01: age-gated analytics -------------------------

        private sealed class FakeAnalytics : IAnalytics
        {
            public readonly List<string> Events = new List<string>();
            public void LogEvent(string name) => Events.Add(name);
        }

        [Test]
        public void Analytics_Under16_DropsBehavioral_KeepsEssential()
        {
            var sink = new FakeAnalytics();
            var gate = new AgeGate();
            gate.SetAge(12);
            var svc = new AnalyticsService(sink, gate);

            svc.LogEvent("lesson_completed", AnalyticsCategory.Behavioral);
            svc.LogEvent("app_open", AnalyticsCategory.Essential);

            CollectionAssert.DoesNotContain(sink.Events, "lesson_completed", "behavioral dropped for under-16 (§10)");
            CollectionAssert.Contains(sink.Events, "app_open", "essential always logged");
        }

        [Test]
        public void Analytics_Adult_KeepsBehavioral()
        {
            var sink = new FakeAnalytics();
            var gate = new AgeGate();
            gate.SetAge(20);
            var svc = new AnalyticsService(sink, gate);

            svc.LogEvent("lesson_completed", AnalyticsCategory.Behavioral);
            CollectionAssert.Contains(sink.Events, "lesson_completed");
        }

        // ---- DATA-04: remote config with local defaults -------------------

        private sealed class FakeConfigSource : IRemoteConfigSource
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public bool Available = true;
            public bool IsAvailable => Available;
            public bool TryGet(string key, out string raw) => Values.TryGetValue(key, out raw);
        }

        [Test]
        public void RemoteConfig_Offline_UsesDefaults()
        {
            var cfg = new RemoteConfig(new Dictionary<string, string>
            {
                { "watermark_enabled", "true" }, { "max_layers", "4" },
            }, source: null);

            Assert.IsTrue(cfg.GetBool("watermark_enabled"));
            Assert.AreEqual(4, cfg.GetInt("max_layers"));
            Assert.AreEqual(0, cfg.GetInt("unknown_key"), "missing key → fallback");
        }

        [Test]
        public void RemoteConfig_OverrideWins_WhenAvailable()
        {
            var source = new FakeConfigSource { Available = true };
            source.Values["watermark_enabled"] = "false"; // premium rollout, e.g.
            var cfg = new RemoteConfig(new Dictionary<string, string> { { "watermark_enabled", "true" } }, source);

            Assert.IsFalse(cfg.GetBool("watermark_enabled"), "remote override wins");

            source.Available = false;
            Assert.IsTrue(cfg.GetBool("watermark_enabled"), "offline → back to default");
        }
    }
}
