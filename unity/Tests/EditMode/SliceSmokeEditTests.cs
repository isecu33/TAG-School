using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PieceBook.Core.DI;
using PieceBook.Core.Events;
using PieceBook.Core.Save;
using PieceBook.Core.Services;
using PieceBook.Lessons.Model;
using PieceBook.Lessons.Runner;
using PieceBook.MetaGame;
using PieceBook.UI.Review;
using UnityEngine;

namespace PieceBook.Tests.EditMode
{
    /// <summary>
    /// SLICE-01 — end-to-end smoke test of the vertical slice, headless. It wires the real Core +
    /// MetaGame services (fake audio backend, in-memory repo) and drives one lesson from start to
    /// finish, then asserts crowns, unlock, currency, reward audio and persistence — the whole loop
    /// the player experiences, minus the GPU canvas and the scene (verified in-editor).
    /// </summary>
    public class SliceSmokeEditTests
    {
        private sealed class FakeAudioBackend : IAudioBackend
        {
            public readonly List<string> OneShots = new List<string>();
            public void PlayOneShot(string id) => OneShots.Add(id);
            public void StartLoop(string id) { }
            public void StopLoop(string id) { }
        }

        private sealed class FakeReviewView : IReviewView
        {
            public int Crowns = -1; public List<string> Unlocks = new List<string>();
            public void ShowResult(int crowns, IReadOnlyList<string> unlocked)
            { Crowns = crowns; Unlocks = new List<string>(unlocked); }
            public void Hide() { }
        }

        private sealed class SpyHaptics : IHaptics
        {
            public int SuccessCalls;
            public void Light() { }
            public void Success() => SuccessCalls++;
        }

        [Test]
        public void FullLessonLoop_Awards_Unlocks_Persists()
        {
            // --- compose the world (as GameContext does, but with test backends) ---
            var repo = new InMemoryProgressRepo();
            var audio = new FakeAudioBackend();
            var container = new ServiceContainer();
            CoreInstaller.Install(container, new EventBus(), audio, repo, new NullHaptics());
            MetaInstaller.Install(container);
            container.Resolve<IAudioService>(); // subscribe audio to the bus

            var bus = container.Resolve<EventBus>();
            var reviewView = new FakeReviewView();
            var haptics = new SpyHaptics();
            using (new ReviewPresenter(reviewView, haptics, bus))
            {
                // --- load the first authored lesson and play it through ---
                var path = Path.Combine(Application.dataPath, "_Project", "Content", "Lessons", "lesson_tag_01.json");
                var lesson = LessonJsonParser.Parse(File.ReadAllText(path));

                var runner = new LessonRunner(bus);
                runner.Start(lesson);
                runner.CompleteShowcase();  // showcase
                runner.SubmitDrawing(3);    // trace
                runner.SubmitDrawing(3);    // freeform
                Assert.IsTrue(runner.AnswerQuiz(0), "lesson_tag_01 quiz correct index is 0.");
                Assert.AreEqual(LessonState.Completed, runner.State);
            }

            // --- assertions: the whole loop happened ---
            var save = container.Resolve<ISaveService>();
            Assert.AreEqual(3, save.Current.Crowns["lesson_tag_01"], "Crowns recorded.");
            Assert.IsTrue(save.Current.IsUnlocked("cap_soft"), "Lesson unlock granted.");
            Assert.AreEqual(30, save.Current.Coins, "3 crowns × 10 coins.");
            Assert.AreEqual(1, save.Current.Streak);

            CollectionAssert.Contains(audio.OneShots, SfxId.Crown, "Crown sting played.");
            CollectionAssert.Contains(audio.OneShots, SfxId.Unlock, "Unlock sting played.");

            // --- persistence: a fresh service over the same repo sees it all ---
            var reopened = new SaveService(repo);
            Assert.AreEqual(3, reopened.Current.Crowns["lesson_tag_01"]);
            Assert.IsTrue(reopened.Current.IsUnlocked("cap_soft"));

            container.Dispose();
        }
    }
}
