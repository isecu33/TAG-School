using System.Collections.Generic;
using NUnit.Framework;
using PieceBook.Core.Events;
using PieceBook.Core.Services;
using PieceBook.DrawingEngine;
using PieceBook.Lessons.Model;
using PieceBook.Lessons.Runner;
using PieceBook.UI.App;
using PieceBook.UI.Lesson;
using PieceBook.UI.Paint;
using PieceBook.UI.Review;
using UnityEngine;

namespace PieceBook.Tests.EditMode
{
    /// <summary>EditMode tests for the MVP presenters (Fase 1: UI-01..04). No scene, fake views.</summary>
    public class UiEditTests
    {
        // ---- UI-01: app state machine -------------------------------------

        [Test]
        public void AppStateMachine_LegalTransition_ChangesState()
        {
            var sm = new AppStateMachine();
            AppState from = default, to = default;
            sm.StateChanged += (f, t) => { from = f; to = t; };

            sm.GoTo(AppState.Lesson);
            Assert.AreEqual(AppState.Lesson, sm.Current);
            Assert.AreEqual(AppState.Menu, from);
            Assert.AreEqual(AppState.Lesson, to);
        }

        [Test]
        public void AppStateMachine_IllegalTransition_Throws()
        {
            var sm = new AppStateMachine();
            Assert.Throws<System.InvalidOperationException>(() => sm.GoTo(AppState.Review));
        }

        // ---- UI-02: lesson presenter --------------------------------------

        private sealed class FakeLessonView : ILessonView
        {
            public string Media; public float MaxSeconds;
            public string DrawingPrompt; public string[] QuizRefs; public bool Hidden;
            public void ShowShowcase(string media, float maxSeconds) { Media = media; MaxSeconds = maxSeconds; }
            public void ShowDrawingStep(string prompt) { DrawingPrompt = prompt; }
            public void ShowQuiz(string[] glossaryRefs) { QuizRefs = glossaryRefs; }
            public void Hide() { Hidden = true; }
        }

        [Test]
        public void LessonPresenter_RendersEachStepKind()
        {
            var lesson = new LessonDef { Id = "l" };
            lesson.Steps.Add(new ShowcaseStep { Media = "vid_intro", MaxSeconds = 12f });
            lesson.Steps.Add(new TraceStep { Template = "tpl_A" });
            lesson.Steps.Add(new QuizStep { GlossaryRefs = new[] { "flow" }, CorrectIndex = 0 });

            var bus = new EventBus();
            var runner = new LessonRunner(bus);
            var view = new FakeLessonView();
            using (new LessonPresenter(runner, view))
            {
                runner.Start(lesson);
                Assert.AreEqual("vid_intro", view.Media);
                Assert.AreEqual(12f, view.MaxSeconds);

                runner.CompleteShowcase();
                Assert.AreEqual("Traza: tpl_A", view.DrawingPrompt);

                runner.SubmitDrawing(3);
                CollectionAssert.Contains(view.QuizRefs, "flow");
            }
        }

        // ---- UI-03: paint presenter ---------------------------------------

        private sealed class MockCanvas : IDrawingCanvas
        {
            public int UndoCalls; public StrokeConfig LastConfig; public bool Began;
            public LayerId AddLayer() => new LayerId(0);
            public void SetActiveLayer(LayerId id) { }
            public void BeginStroke(StrokeConfig cfg) { LastConfig = cfg; Began = true; }
            public void UpdateStroke(Vector2 pos, float pressure, float tilt) { }
            public void EndStroke() { }
            public void Undo() { UndoCalls++; }
            public void Redo() { }
            public Texture2D Flatten() => null;
            public StrokeRecording GetRecording() => new StrokeRecording();
        }

        [Test]
        public void PaintPresenter_Undo_CallsCanvasOnce()
        {
            var canvas = new MockCanvas();
            var p = new PaintPresenter(canvas);
            p.Undo();
            Assert.AreEqual(1, canvas.UndoCalls);
        }

        [Test]
        public void PaintPresenter_ToolSelection_FlowsIntoStroke()
        {
            var canvas = new MockCanvas();
            var p = new PaintPresenter(canvas);
            p.SetTool(ToolKind.Marker);
            p.SetColor(Color.red);
            p.BeginStroke();

            Assert.IsTrue(canvas.Began);
            Assert.AreEqual(ToolKind.Marker, canvas.LastConfig.Tool);
            Assert.AreEqual(Color.red, canvas.LastConfig.Color);
        }

        // ---- UI-04: review presenter --------------------------------------

        private sealed class FakeReviewView : IReviewView
        {
            public int Crowns = -1; public List<string> Unlocks;
            public void ShowResult(int crowns, IReadOnlyList<string> unlockedItems)
            { Crowns = crowns; Unlocks = new List<string>(unlockedItems); }
            public void Hide() { }
        }

        private sealed class SpyHaptics : IHaptics
        {
            public int SuccessCalls;
            public void Light() { }
            public void Success() { SuccessCalls++; }
        }

        [Test]
        public void ReviewPresenter_ShowsCrownsAndUnlocks_AndBuzzes()
        {
            var bus = new EventBus();
            var view = new FakeReviewView();
            var haptics = new SpyHaptics();
            using (new ReviewPresenter(view, haptics, bus))
            {
                // ItemUnlocked is published during the lesson-pass handling, before the review handler.
                bus.Publish(new ItemUnlocked("cap_ny_fat"));
                bus.Publish(new LessonPassed("lesson_tag_01", 3, new[] { "cap_ny_fat" }));
            }

            Assert.AreEqual(3, view.Crowns);
            CollectionAssert.Contains(view.Unlocks, "cap_ny_fat");
            Assert.AreEqual(1, haptics.SuccessCalls);
        }
    }
}
