using System.IO;
using NUnit.Framework;
using PieceBook.Core.DI;
using PieceBook.Core.Events;
using PieceBook.Core.Save;
using PieceBook.Core.Services;
using PieceBook.Lessons.Model;
using PieceBook.Lessons.Runner;
using PieceBook.MetaGame;
using PieceBook.MetaGame.Blackbook;
using PieceBook.MetaGame.Store;
using PieceBook.Social.Export;
using PieceBook.Social.Share;
using UnityEngine;

namespace PieceBook.Tests.EditMode
{
    /// <summary>
    /// MVP-01 — end-to-end smoke test of the MVP loop across the three chapters, headless: play a
    /// lesson from each chapter, earn crowns/coins/unlocks, buy from the store, export + share an
    /// image, save it to the blackbook, and confirm everything persists. GPU canvas, native share
    /// and MP4 are stubbed by fakes (verified in-editor/on-device).
    /// </summary>
    public class MvpSmokeEditTests
    {
        private sealed class FakeAudioBackend : IAudioBackend
        {
            public void PlayOneShot(string id) { }
            public void StartLoop(string id) { }
            public void StopLoop(string id) { }
        }

        private sealed class MockShare : INativeShare
        {
            public string Mime;
            public void Share(string filePath, string mimeType, string message) => Mime = mimeType;
        }

        private static LessonDef Load(string name) =>
            LessonJsonParser.Parse(File.ReadAllText(
                Path.Combine(Application.dataPath, "_Project", "Content", "Lessons", name + ".json")));

        private static void Play(EventBus bus, LessonDef lesson, int quizAnswer)
        {
            var runner = new LessonRunner(bus);
            runner.Start(lesson);
            runner.CompleteShowcase();
            runner.SubmitDrawing(3); // trace
            runner.SubmitDrawing(3); // freeform
            Assert.IsTrue(runner.AnswerQuiz(quizAnswer), $"{lesson.Id}: quiz answer {quizAnswer} should be correct.");
            Assert.AreEqual(LessonState.Completed, runner.State);
        }

        [Test]
        public void MvpLoop_ThreeChapters_Export_Store_Blackbook_Persist()
        {
            var repo = new InMemoryProgressRepo();
            var container = new ServiceContainer();
            CoreInstaller.Install(container, new EventBus(), new FakeAudioBackend(), repo, new NullHaptics());
            MetaInstaller.Install(container);
            var bus = container.Resolve<EventBus>();
            var save = container.Resolve<ISaveService>();

            // --- play one lesson from each chapter (quiz correctIndex: tag_01=0, throwup_01=0, color_02=1) ---
            Play(bus, Load("lesson_tag_01"), 0);      // ch1 → unlocks cap_soft
            Play(bus, Load("lesson_throwup_01"), 0);  // ch2 → unlocks cap_fat
            Play(bus, Load("lesson_color_02"), 1);    // ch3 → unlocks cap_skinny

            Assert.AreEqual(3, save.Current.Crowns.Count, "One crown record per chapter lesson.");
            Assert.AreEqual(90, save.Current.Coins, "3 lessons × 3 crowns × 10 coins.");
            Assert.AreEqual(3, save.Current.Streak);
            Assert.IsTrue(save.Current.IsUnlocked("cap_soft"));
            Assert.IsTrue(save.Current.IsUnlocked("cap_fat"));
            Assert.IsTrue(save.Current.IsUnlocked("cap_skinny"));

            // --- store: buy a paint the lessons don't grant ---
            var store = new StoreService(save, bus, new[] { new StoreItem("paint_gold", 50) });
            Assert.IsTrue(store.Buy("paint_gold"));
            Assert.AreEqual(40, save.Current.Coins);
            Assert.IsTrue(save.Current.IsUnlocked("paint_gold"));

            // --- export image + share (SOC-02/03/04) ---
            var art = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var px = new Color[16 * 16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.cyan;
            art.SetPixels(px); art.Apply(false);
            Watermark.ApplyDefault(art, premium: false);
            var png = PngExporter.EncodePng(art);
            Assert.Greater(png.Length, 8);

            var share = new MockShare();
            new ShareService(share).ShareImage("/tmp/piece.png");
            Assert.AreEqual(ShareService.MimePng, share.Mime);

            // --- blackbook: save the artwork (META-03) ---
            var book = new BlackbookService(save, new InMemoryArtworkStore());
            book.AddArtwork(ArtworkMeta.New("art_1", "mockup_shutter"), png);
            CollectionAssert.Contains(book.Pages, "art_1");

            // --- persistence: a fresh service over the same repo sees everything ---
            var reopened = new SaveService(repo);
            Assert.IsTrue(reopened.Current.IsUnlocked("paint_gold"));
            Assert.IsTrue(reopened.Current.IsUnlocked("cap_fat"));
            CollectionAssert.Contains(reopened.Current.BlackbookPages, "art_1");
            Assert.AreEqual(40, reopened.Current.Coins);

            Object.DestroyImmediate(art);
            container.Dispose();
        }
    }
}
