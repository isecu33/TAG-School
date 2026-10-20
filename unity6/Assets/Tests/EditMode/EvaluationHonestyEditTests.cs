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
    /// The evaluator must not hand out crowns for drawings that do not look like the letter:
    /// shape (precision + coverage) is always scored when there is a template, and line weight
    /// measures the real line width (pressure AND speed), so a mouse/finger cannot get it for free.
    /// </summary>
    public class EvaluationHonestyEditTests
    {
        // An "L": down the left side, then along the bottom.
        private static readonly TraceTemplate Ell = new TraceTemplate(new[]
            { new Vector2(0.2f, 0.8f), new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.2f) });

        private static readonly TraceMetric[] SmoothOnly = { TraceMetric.Smoothness };

        private static Vector2[] Path(int perSegment, params Vector2[] corners)
        {
            var pts = new List<Vector2>();
            for (int i = 0; i < corners.Length - 1; i++)
                for (int k = (i == 0 ? 0 : 1); k < perSegment; k++)
                    pts.Add(Vector2.Lerp(corners[i], corners[i + 1], k / (float)(perSegment - 1)));
            return pts.ToArray();
        }

        private static float[] Times(int n, float dt = 0.016f)
        {
            var t = new float[n];
            for (int i = 0; i < n; i++) t[i] = i * dt;
            return t;
        }

        private static float[] Ones(int n)
        {
            var p = new float[n];
            for (int i = 0; i < n; i++) p[i] = 1f;
            return p;
        }

        // ---- shape is always scored -------------------------------------------

        [Test]
        public void Trace_SmoothStrokeOffTheLetter_OneCrown_EvenIfLessonOnlyListsSmoothness()
        {
            var pos = Path(40, new Vector2(0.55f, 0.95f), new Vector2(0.95f, 0.55f));
            var r = TraceEvaluator.Evaluate(pos, Times(pos.Length), Ones(pos.Length), SmoothOnly, Ell, 0.2f);

            Assert.IsTrue(r.Scores.ContainsKey(TraceMetric.Precision), "shape must be scored when there is a template");
            Assert.AreEqual(1, r.Crowns, $"overall={r.Overall} coverage={r.Coverage}");
        }

        [Test]
        public void Trace_HalfTheLetter_AtMostTwoCrowns()
        {
            var pos = Path(40, new Vector2(0.2f, 0.8f), new Vector2(0.2f, 0.2f));
            var r = TraceEvaluator.Evaluate(pos, Times(pos.Length), Ones(pos.Length), SmoothOnly, Ell, 0.2f);

            Assert.LessOrEqual(r.Crowns, 2, $"coverage={r.Coverage}");
        }

        [Test]
        public void Trace_WholeLetter_StillThreeCrowns()
        {
            var pos = Path(30, new Vector2(0.2f, 0.8f), new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.2f));
            var r = TraceEvaluator.Evaluate(pos, Times(pos.Length), Ones(pos.Length), SmoothOnly, Ell, 0.2f);

            Assert.AreEqual(3, r.Crowns, $"overall={r.Overall} coverage={r.Coverage}");
        }

        [Test]
        public void Freeform_Scribble_OneCrown_EvenIfLessonOnlyListsSmoothness()
        {
            var canvas = new RecordingCanvas();
            var runner = new LessonRunner(new EventBus());
            var ctrl = new DrawingStepController(canvas, runner, new OneTemplate());
            runner.Start(LessonJsonParser.Parse(SmoothOnlyLessonJson));
            runner.CompleteShowcase();
            runner.SubmitDrawing(3); // → freeform

            // A zigzag: smooth straight segments, nothing like an L.
            canvas.Draw(Path(15, new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.35f),
                                 new Vector2(0.35f, 0.6f), new Vector2(0.65f, 0.7f), new Vector2(0.4f, 0.4f)));
            var r = ctrl.EvaluateCurrent();

            Assert.IsTrue(r.Scores.ContainsKey(TraceMetric.Precision), "freeform shape must be scored");
            Assert.AreEqual(1, r.Crowns, $"overall={r.Overall} coverage={r.Coverage}");
        }

        [Test]
        public void Freeform_SameShapeSmallerElsewhere_ThreeCrowns()
        {
            var canvas = new RecordingCanvas();
            var runner = new LessonRunner(new EventBus());
            var ctrl = new DrawingStepController(canvas, runner, new OneTemplate());
            runner.Start(LessonJsonParser.Parse(SmoothOnlyLessonJson));
            runner.CompleteShowcase();
            runner.SubmitDrawing(3);

            canvas.Draw(Path(15, new Vector2(0.6f, 0.9f), new Vector2(0.6f, 0.7f), new Vector2(0.8f, 0.7f)));
            var r = ctrl.EvaluateCurrent();

            Assert.AreEqual(3, r.Crowns, $"overall={r.Overall} coverage={r.Coverage}");
        }

        // ---- line weight = width stability (pressure + speed) -------------------

        [Test]
        public void LineWeight_SteadyStroke_IsOne()
        {
            var pos = Path(40, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.5f));
            Assert.AreEqual(1f, StrokeMetrics.LineWeight(pos, Times(pos.Length), Ones(pos.Length)), 1e-3f);
        }

        [Test]
        public void LineWeight_ConstantPressureButSpeedSurges_IsNotFree()
        {
            // Mouse/finger: pressure is always 1, but the hand speeds up and slows down → the
            // spray line thins and thickens, so the weight is NOT steady.
            var pos = new List<Vector2>();
            float x = 0.05f;
            for (int i = 0; i < 60; i++)
            {
                pos.Add(new Vector2(x, 0.5f));
                x += (i / 10) % 2 == 0 ? 0.002f : 0.03f; // slow, fast, slow, fast…
            }
            var arr = pos.ToArray();
            Assert.Less(StrokeMetrics.LineWeight(arr, Times(arr.Length), Ones(arr.Length)), 0.8f);
        }

        // ---- fixtures ---------------------------------------------------------

        private const string SmoothOnlyLessonJson = @"
{
  ""id"": ""lesson_smooth"", ""title"": ""Smooth"", ""chapter"": 1,
  ""steps"": [
    {""type"": ""showcase"", ""media"": ""vid_x"", ""maxSeconds"": 5},
    {""type"": ""trace"", ""template"": ""tpl_l"", ""tolerance"": 0.2, ""metrics"": [""smoothness""]},
    {""type"": ""freeform"", ""prompt"": ""Otra vez"", ""evaluate"": [""smoothness""]},
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
    }
}
