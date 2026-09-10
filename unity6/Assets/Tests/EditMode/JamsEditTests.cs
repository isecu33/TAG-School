using System.Collections.Generic;
using NUnit.Framework;
using PieceBook.Core.Events;
using PieceBook.Core.Save;
using PieceBook.Core.Services;
using PieceBook.MetaGame.Jams;

namespace PieceBook.Tests.EditMode
{
    /// <summary>EditMode tests for community jams (Fase 4 core, META-06). Window + reward-once, pure.</summary>
    public class JamsEditTests
    {
        private static RemoteConfig JamConfig() => new RemoteConfig(new Dictionary<string, string>
        {
            { JamService.KeyId, "jam_halloween" },
            { JamService.KeyTheme, "Spooky throw-ups" },
            { JamService.KeyStart, "1000" },
            { JamService.KeyEnd, "2000" },
            { JamService.KeyReward, "cap_pumpkin" },
        });

        [Test]
        public void GetActiveJam_RespectsWindow()
        {
            var save = new SaveService(new InMemoryProgressRepo());
            var svc = new JamService(save, new EventBus());
            var cfg = JamConfig();

            Assert.IsTrue(svc.GetActiveJam(cfg, 1500).IsValid, "inside window → active");
            Assert.IsFalse(svc.GetActiveJam(cfg, 999).IsValid, "before start → inactive");
            Assert.IsFalse(svc.GetActiveJam(cfg, 2001).IsValid, "after end → inactive");
        }

        [Test]
        public void EmptyConfig_YieldsNoJam()
        {
            var save = new SaveService(new InMemoryProgressRepo());
            var svc = new JamService(save, new EventBus());
            var cfg = new RemoteConfig(new Dictionary<string, string>()); // nothing configured
            Assert.IsFalse(svc.GetActiveJam(cfg, 1500).IsValid);
        }

        [Test]
        public void Participate_GrantsRewardOnce()
        {
            var save = new SaveService(new InMemoryProgressRepo());
            var bus = new EventBus();
            int unlocks = 0;
            bus.Subscribe<ItemUnlocked>(_ => unlocks++);
            var svc = new JamService(save, bus);
            var jam = svc.GetActiveJam(JamConfig(), 1500);

            Assert.IsFalse(svc.HasParticipated(jam));
            Assert.IsTrue(svc.Participate(jam), "first participation succeeds");
            Assert.IsTrue(save.Current.IsUnlocked("cap_pumpkin"), "reward granted");
            Assert.AreEqual(1, unlocks, "ItemUnlocked fired once");
            Assert.IsTrue(svc.HasParticipated(jam));

            Assert.IsFalse(svc.Participate(jam), "cannot participate twice");
            Assert.AreEqual(1, unlocks, "no extra reward event");
        }

        [Test]
        public void Participate_Persists()
        {
            var repo = new InMemoryProgressRepo();
            var jam = new JamDef("jam_x", "t", 0, 10, "cap_x");
            var svc = new JamService(new SaveService(repo), new EventBus());
            Assert.IsTrue(svc.Participate(jam));

            // A fresh service over the same repo still sees the claim.
            var svc2 = new JamService(new SaveService(repo), new EventBus());
            Assert.IsTrue(svc2.HasParticipated(jam));
            Assert.IsFalse(svc2.Participate(jam));
        }
    }
}
