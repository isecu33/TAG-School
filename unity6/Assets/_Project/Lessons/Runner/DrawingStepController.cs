using System;
using PieceBook.DrawingEngine;
using PieceBook.Lessons.Evaluation;
using PieceBook.Lessons.Model;

namespace PieceBook.Lessons.Runner
{
    /// <summary>
    /// Wires the drawing steps of a lesson to the pure Drawing Engine API (ARQUITECTURA §4.3) and
    /// scores them (LES-05). A <c>trace</c> step is evaluated against its template + tolerance; a
    /// <c>freeform</c> step is evaluated on the metrics it lists plus its shape against the lesson's
    /// letter (aligned, so size and position are free); without a letter Precision is skipped (§5). The resulting crowns are handed to the <see cref="LessonRunner"/>.
    ///
    /// It reads what the player actually drew from <see cref="IDrawingCanvas.GetRecording"/> — the
    /// last stroke — so evaluation and replay share one source of truth.
    /// </summary>
    public sealed class DrawingStepController
    {
        private readonly IDrawingCanvas _canvas;
        private readonly LessonRunner _runner;
        private readonly ITemplateProvider _templates;

        public DrawingStepController(IDrawingCanvas canvas, LessonRunner runner, ITemplateProvider templates)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _templates = templates ?? throw new ArgumentNullException(nameof(templates));
        }

        /// <summary>Tolerance for a freeform step's Precision, measured after aligning to the template.</summary>
        public const float FreeformPrecisionTolerance = 0.12f;

        /// <summary>
        /// Evaluate the current drawing step from the canvas recording and submit its crowns.
        /// Returns the full result for the UI (per-metric breakdown, overall).
        /// </summary>
        public EvaluationResult EvaluateAndSubmitCurrent()
        {
            var result = EvaluateCurrent();
            _runner.SubmitDrawing(result.Crowns);
            return result;
        }

        /// <summary>
        /// Evaluate the current drawing step WITHOUT submitting, so the UI can show the result and
        /// let the player retry before accepting it (<see cref="LessonRunner.SubmitDrawing"/>).
        /// A freeform step is scored against the lesson's trace template after aligning the drawing to
        /// it (shape, not position) — without a template Precision is skipped (§5).
        /// </summary>
        public EvaluationResult EvaluateCurrent()
        {
            var step = _runner.Current;
            StrokeSampleAdapter.Extract(LastStroke(), out var pos, out var times, out var pressures);

            if (step is TraceStep trace)
            {
                _templates.TryGet(trace.Template, out var template);
                return TraceEvaluator.Evaluate(pos, times, pressures, trace.Metrics, template, trace.Tolerance);
            }
            if (step is FreeformStep free)
            {
                // Shape is always judged when the lesson has a letter, even if the step only lists
                // e.g. smoothness: otherwise any smooth scribble earns three crowns.
                var template = LessonTraceTemplate();
                if (template != null)
                    return TraceEvaluator.Evaluate(StrokeMetrics.AlignToTemplate(pos, template), times, pressures,
                                                   free.Evaluate, template, FreeformPrecisionTolerance);
                return TraceEvaluator.Evaluate(pos, times, pressures, free.Evaluate);
            }
            throw new InvalidOperationException("Current step is not a drawing step.");
        }

        /// <summary>How much of the current trace step's template the last stroke covers (0..1).</summary>
        public float CoverageOfCurrent()
        {
            if (!(_runner.Current is TraceStep trace) || !_templates.TryGet(trace.Template, out var template))
                return 1f;
            StrokeSampleAdapter.Extract(LastStroke(), out var pos, out _, out _);
            return StrokeMetrics.Coverage(pos, template, trace.Tolerance * 0.6f);
        }

        private RecordedStroke LastStroke()
        {
            var recording = _canvas.GetRecording();
            return (recording != null && recording.StrokeCount > 0)
                ? recording.Strokes[recording.StrokeCount - 1]
                : null;
        }

        private TraceTemplate LessonTraceTemplate()
        {
            var lesson = _runner.Lesson;
            if (lesson == null) return null;
            foreach (var s in lesson.Steps)
                if (s is TraceStep t && _templates.TryGet(t.Template, out var template))
                    return template;
            return null;
        }
    }
}
