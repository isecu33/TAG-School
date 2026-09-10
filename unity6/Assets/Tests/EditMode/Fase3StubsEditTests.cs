using System.Collections.Generic;
using NUnit.Framework;
using PieceBook.Core.Auth;
using PieceBook.Core.Data;
using PieceBook.Core.Save;
using PieceBook.Core.Services;
using PieceBook.Social.Upload;

namespace PieceBook.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the Fase 3 device-boundary stubs: they keep the game running offline-first
    /// (auth anon, sync no-op, config defaults, no upload) until the real Firebase/AR impls are added.
    /// </summary>
    public class Fase3StubsEditTests
    {
        [Test]
        public void AnonymousAuth_SignsInWithStableId()
        {
            var auth = new AnonymousAuthStub();
            Assert.IsFalse(auth.IsSignedIn);
            var id = auth.EnsureSignedIn();
            Assert.IsFalse(string.IsNullOrEmpty(id));
            Assert.IsTrue(auth.IsSignedIn);
            Assert.AreEqual(id, auth.EnsureSignedIn(), "id is stable across calls");
            Assert.IsFalse(auth.IsLinked);
            Assert.IsFalse(auth.LinkTo("apple"), "linking needs the native SDK (device)");
        }

        [Test]
        public void NullRemoteStore_KeepsSyncOffline()
        {
            var save = new SaveService(new InMemoryProgressRepo());
            save.Current.Coins = 25;
            var sync = new ProgressSyncService(save, new NullRemoteProgressStore());
            Assert.IsFalse(sync.Sync(), "no backend → no sync");
            Assert.AreEqual(25, save.Current.Coins);
        }

        [Test]
        public void NullRemoteConfigSource_FallsBackToDefaults()
        {
            var cfg = new RemoteConfig(
                new Dictionary<string, string> { { "watermark_enabled", "true" } },
                new NullRemoteConfigSource());
            Assert.IsTrue(cfg.GetBool("watermark_enabled"), "unavailable source → default");
        }

        [Test]
        public void NullAnalytics_NeverThrows()
        {
            var gate = new AgeGate();
            gate.SetAge(25);
            var svc = new AnalyticsService(new NullAnalytics(), gate);
            Assert.DoesNotThrow(() => svc.LogEvent("app_open", AnalyticsCategory.Essential));
        }

        [Test]
        public void NullArtworkUploader_ReportsUnavailable()
        {
            IArtworkUploader up = new NullArtworkUploader();
            Assert.IsFalse(up.IsAvailable);
            Assert.IsNull(up.Upload("art_1", new byte[] { 1 }, "image/png"));
        }
    }
}
