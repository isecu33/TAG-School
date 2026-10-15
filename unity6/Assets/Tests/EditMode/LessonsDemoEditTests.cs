using System.Collections.Generic;
using NUnit.Framework;
using PieceBook.Core.Events;
using PieceBook.DrawingEngine;
using PieceBook.Lessons.Evaluation;
using PieceBook.Lessons.Model;
using PieceBook.Lessons.Runner;
using UnityEngine;

namespace PieceBook.Tests.EditMode
{
    /// <summary>
    /// Evaluation pieces added for the playable demo: coverage gate, freeform precision against the
    /// lesson's template (aligned), evaluate-without-submit, and the showcase caption.
    /// </summary>
    public class LessonsDemoEditTests
    {
        private static readonly TraceTemplate Line =
            new TraceTemplate(new[] { new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.5f) });

        // An "L": down the left side, then along the bottom. Has 2D extent, unlike Line.
        private static readonly TraceTemplate Ell = new TraceTemplate(new[]
            { new Vector2(0.2f, 0.8f), new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.2f) });

        private static Vector2[] Sample(Vector2 a, Vector2 b, int n)
        {
            var pts = new Vector2[n];
            for (int i = 0; i < n; i++) pts[i] = Vector2.Lerp(a, b, i / (float)(n - 1));
            return pts;
        }

        private static Vector2[] Path(int perSegment, params Vector2[] corners)
        {
            var pts = new List<Vector2>();
            for (int i = 0; i < corners.Length - 1; i++)
            {
                var seg = Sample(corners[i], corners[i + 1], perSegment);
                for (int k = (i == 0 ? 0 : 1); k < seg.Length; k++) pts.Add(seg[k]);
            }
            return pts.ToArray();
        }

        // ---- coverage ------------------------------------------------------

        [Test]
        public void Coverage_StrokeAlongWholeTemplate_IsOne()
        {
            var stroke = Sample(new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.5f), 40);
            Assert.AreEqual(1f, StrokeMetrics.Coverage(stroke, Line, 0.05f), 1e-4f);
        }

        [Test]
        public void Coverage_DotOnTemplate_IsLowEvenThoughPrecisionIsPerfect()
        {
            var dot = new[] { new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f) };
            Assert.AreEqual(1f, StrokeMetrics.Precision(dot, Line, 0.1f), 1e-4f, "precision alone is fooled by a dot");
            Assert.Less(StrokeMetrics.Coverage(dot, Line, 0.05f), 0.2f, "coverage is not");
        }

        [Test]
        public void Coverage_HalfTheTemplate_IsAboutHalf()
        {
            var stroke = Sample(new Vector2(0.1f, 0.5f), new Vector2(0.5f, 0.5f), 20);
            Assert.AreEqual(0.5f, StrokeMetrics.Coverage(stroke, Line, 0.01f), 0.08f);
        }

        // ---- alignment -----------------------------------------------------

        [Test]
        public void AlignToTemplate_MapsDrawingOntoTemplateBox()
        {
            var tpl = new TraceTemplate(new[] { new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.9f) });
            var small = new[] { new Vector2(0f, 0f), new Vector2(0.1f, 0.2f) };
            var aligned = StrokeMetrics.AlignToTemplate(small, tpl);
            Assert.AreEqual(0.2f, aligned[0].x, 1e-4f);
            Assert.AreEqual(0.1f, aligned[0].y, 1e-4f);
            Assert.AreEqual(0.8f, aligned[1].x, 1e-4f);
            Assert.AreEqual(0.9f, aligned[1].y, 1e-4f);
        }

        // ---- DrawingStepController ------------------------------------------

        private const string LessonJson = @"
{
  ""id"": ""lesson_demo"", ""title"": ""Demo"", ""chapter"": 3,
  ""steps"": [
    {""type"": ""showcase"", ""media"": ""vid_x"", ""maxSeconds"": 5, ""caption"": ""Mira la línea.""},
    {""type"": ""trace"", ""template"": ""tpl_l"", ""tolerance"": 0.2, ""metrics"": [""precision""]},
    {""type"": ""freeform"", ""prompt"": ""Otra vez, sin guía"", ""evaluate"": [""precision""]},
    {""type"": ""quiz"", ""glossaryRefs"": [""tag""], ""correctIndex"": 0}
  ]
}";

        private sealed class OneTemplate : ITemplateProvider
        {
            public bool TryGet(string id, out TraceTemplate t)
            {
                t = id == "tpl_l" ? Ell : null;
                return t != null;
            }
        }

        private sealed class RecordingCanvas : IDrawingCanvas
        {
            private readonly StrokeRecording _rec = new StrokeRecording();
            public void Draw(params Vector2[] pts)
            {
                var s = new RecordedStroke();
                s.Reset("cap", "paint", Color.white);
                for (int i = 0; i < pts.Length; i++)
                    s.Samples.Add(new StrokeSample { Pos = pts[i], Pressure = 1f, Time = i * 0.016f });
                _rec.Strokes.Add(s);
            }
            public LayerId AddLayer() => new LayerId(0);
            public void SetActiveLayer(LayerId id) { }
            public void BeginStroke(StrokeConfig cfg) { }
            public void UpdateStroke(Vector2 pos, float pressure, float tilt) { }
            public void EndStroke() { }
            public void Undo() { }
            public void Redo() { }
            public Texture2D Flatten() => null;
            public StrokeRecording GetRecording() => _rec;
        }

        [Test]
        public void Parser_ReadsShowcaseCaption()
        {
            var lesson = LessonJsonParser.Parse(LessonJson);
            Assert.AreEqual("Mira la línea.", ((ShowcaseStep)lesson.Steps[0]).Caption);
        }

        [Test]
        public void EvaluateCurrent_DoesNotSubmit_AndCoverageGatesPartialTrace()
        {
            var canvas = new RecordingCanvas();
            var runner = new LessonRunner(new EventBus());
            var ctrl = new DrawingStepController(canvas, runner, new OneTemplate());
            runner.Start(LessonJsonParser.Parse(LessonJson));
            runner.CompleteShowcase();

            canvas.Draw(Sample(new Vector2(0.2f, 0.8f), new Vector2(0.2f, 0.55f), 10));
            Assert.Less(ctrl.CoverageOfCurrent(), 0.6f, "a fraction of the letter must not pass the gate");

            canvas.Draw(Path(30, new Vector2(0.2f, 0.8f), new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.2f)));
            Assert.Greater(ctrl.CoverageOfCurrent(), 0.95f);
            var r = ctrl.EvaluateCurrent();
            Assert.AreEqual(3, r.Crowns);
            Assert.AreEqual(StepKind.Trace, runner.Current.Kind, "EvaluateCurrent must not advance the runner");
        }

        [Test]
        public void Freeform_Precision_UsesLessonTemplateAfterAlignment()
        {
            var canvas = new RecordingCanvas();
            var runner = new LessonRunner(new EventBus());
            var ctrl = new DrawingStepController(canvas, runner, new OneTemplate());
            runner.Start(LessonJsonParser.Parse(LessonJson));
            runner.CompleteShowcase();
            runner.SubmitDrawing(3); // → freeform

            // Same shape as the template (an L) but small and elsewhere on the wall.
            canvas.Draw(Path(15, new Vector2(0.6f, 0.9f), new Vector2(0.6f, 0.7f), new Vector2(0.8f, 0.7f)));
            var r = ctrl.EvaluateCurrent();
            Assert.IsTrue(r.Scores.ContainsKey(TraceMetric.Precision), "freeform precision must be scored, not skipped");
            Assert.AreEqual(3, r.Crowns, $"overall={r.Overall}");

            // A diagonal has the same bounding box but the wrong shape.
            canvas.Draw(Sample(new Vector2(0.6f, 0.9f), new Vector2(0.8f, 0.7f), 30));
            Assert.Less(ctrl.EvaluateCurrent().Crowns, 3);
        }
    }
}
