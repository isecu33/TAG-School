using NUnit.Framework;
using PieceBook.Core.Events;
using PieceBook.Lessons.Evaluation;
using PieceBook.Lessons.Model;
using PieceBook.Lessons.Runner;
using UnityEngine;

namespace PieceBook.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the Lesson System (Fase 1: LES-01..05). Parser, geometric evaluator and
    /// the runner state machine — all pure, no scene.
    /// </summary>
    public class LessonsEditTests
    {
        // The §5 example lesson, verbatim shape (plus a correctIndex on the quiz).
        private const string LessonTag03Json = @"
{
  ""id"": ""lesson_tag_03"",
  ""title"": ""Flow: conecta las letras"",
  ""chapter"": 1,
  ""unlocks"": [""cap_ny_fat""],
  ""steps"": [
    {""type"": ""showcase"", ""media"": ""vid_flow_intro"", ""maxSeconds"": 12},
    {""type"": ""trace"", ""template"": ""tpl_tag_flow_A"", ""tolerance"": 0.15,
     ""metrics"": [""smoothness"", ""speed_consistency"", ""line_weight""]},
    {""type"": ""freeform"", ""prompt"": ""Ahora tu tag, sin guía"", ""evaluate"": [""smoothness""]},
    {""type"": ""quiz"", ""glossaryRefs"": [""flow"", ""handstyle""], ""correctIndex"": 1}
  ]
}";

        // ---- LES-01: parser ------------------------------------------------

        [Test]
        public void Parser_BuildsFourTypedSteps()
        {
            var lesson = LessonJsonParser.Parse(LessonTag03Json);

            Assert.AreEqual("lesson_tag_03", lesson.Id);
            Assert.AreEqual(4, lesson.StepCount);
            Assert.IsInstanceOf<ShowcaseStep>(lesson.Steps[0]);
            Assert.IsInstanceOf<TraceStep>(lesson.Steps[1]);
            Assert.IsInstanceOf<FreeformStep>(lesson.Steps[2]);
            Assert.IsInstanceOf<QuizStep>(lesson.Steps[3]);

            var trace = (TraceStep)lesson.Steps[1];
            Assert.AreEqual("tpl_tag_flow_A", trace.Template);
            Assert.AreEqual(3, trace.Metrics.Length);
            CollectionAssert.Contains(lesson.Unlocks, "cap_ny_fat");
        }

        [Test]
        public void Parser_RejectsUnknownStepType()
        {
            Assert.Throws<System.FormatException>(
                () => LessonJsonParser.Parse(@"{""id"":""x"",""steps"":[{""type"":""bogus""}]}"));
        }

        // ---- LES-03: evaluator gives 3 / 2 / 1 crowns ---------------------

        private static readonly TraceTemplate Line =
            new TraceTemplate(new[] { new Vector2(0f, 0.5f), new Vector2(1f, 0.5f) });

        private static readonly TraceMetric[] AllMetrics =
        {
            TraceMetric.Precision, TraceMetric.Smoothness,
            TraceMetric.SpeedConsistency, TraceMetric.LineWeight,
        };

        [Test]
        public void Evaluator_PerfectTrace_ThreeCrowns()
        {
            var pos = new[]
            {
                new Vector2(0f, 0.5f), new Vector2(0.25f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.75f, 0.5f), new Vector2(1f, 0.5f),
            };
            var times = new[] { 0f, 1f, 2f, 3f, 4f };
            var pressures = new[] { 1f, 1f, 1f, 1f, 1f };

            var r = TraceEvaluator.Evaluate(pos, times, pressures, AllMetrics, Line, 0.1f);
            Assert.AreEqual(3, r.Crowns, $"overall={r.Overall}");
        }

        [Test]
        public void Evaluator_OnLineButErratic_TwoCrowns()
        {
            // On the template (precision & smoothness perfect) but jerky pacing + pressure.
            var pos = new[]
            {
                new Vector2(0f, 0.5f), new Vector2(0.25f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.75f, 0.5f), new Vector2(1f, 0.5f),
            };
            var times = new[] { 0f, 0.01f, 1.0f, 1.01f, 2.0f };
            var pressures = new[] { 1f, 0f, 1f, 0f, 1f };

            var r = TraceEvaluator.Evaluate(pos, times, pressures, AllMetrics, Line, 0.1f);
            Assert.AreEqual(2, r.Crowns, $"overall={r.Overall}");
        }

        [Test]
        public void Evaluator_OffTemplateJagged_OneCrown()
        {
            var pos = new[]
            {
                new Vector2(0f, 0.9f), new Vector2(0.25f, 0.1f), new Vector2(0.5f, 0.9f),
                new Vector2(0.75f, 0.1f), new Vector2(1f, 0.9f),
            };
            var times = new[] { 0f, 0.01f, 1.0f, 1.01f, 2.0f };
            var pressures = new[] { 1f, 0f, 1f, 0f, 1f };

            var r = TraceEvaluator.Evaluate(pos, times, pressures, AllMetrics, Line, 0.1f);
            Assert.AreEqual(1, r.Crowns, $"overall={r.Overall}");
        }

        [Test]
        public void Evaluator_Freeform_SkipsPrecisionWithoutTemplate()
        {
            var pos = new[] { new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f) };
            var times = new[] { 0f, 1f, 2f };
            var pressures = new[] { 1f, 1f, 1f };

            var r = TraceEvaluator.Evaluate(pos, times, pressures, new[] { TraceMetric.Precision });
            Assert.AreEqual(0, r.Scores.Count, "Precision needs a template; without one it is skipped (§5).");
            Assert.AreEqual(0, r.Crowns);
        }

        // ---- LES-02 / LES-04: runner ---------------------------------------

        [Test]
        public void Runner_WalksAllSteps_PublishesLessonPassedOnce()
        {
            var bus = new EventBus();
            int passed = 0;
            LessonPassed captured = default;
            bus.Subscribe<LessonPassed>(e => { passed++; captured = e; });

            var lesson = LessonJsonParser.Parse(LessonTag03Json);
            var runner = new LessonRunner(bus);
            runner.Start(lesson);

            Assert.AreEqual(StepKind.Showcase, runner.Current.Kind);
            runner.CompleteShowcase();
            Assert.AreEqual(StepKind.Trace, runner.Current.Kind);
            runner.SubmitDrawing(3);
            Assert.AreEqual(StepKind.Freeform, runner.Current.Kind);
            runner.SubmitDrawing(3);
            Assert.AreEqual(StepKind.Quiz, runner.Current.Kind);

            Assert.IsFalse(runner.AnswerQuiz(0), "Wrong answer must not advance.");
            Assert.IsTrue(runner.AnswerQuiz(1), "Correct answer (index 1) advances and finishes.");

            Assert.AreEqual(LessonState.Completed, runner.State);
            Assert.AreEqual(1, passed, "LessonPassed must fire exactly once.");
            Assert.AreEqual("lesson_tag_03", captured.LessonId);
            Assert.AreEqual(3, captured.Crowns);
            CollectionAssert.Contains(captured.Unlocks, "cap_ny_fat");
        }
    }
}
