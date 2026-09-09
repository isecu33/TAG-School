using System;
using PieceBook.DrawingEngine;
using PieceBook.Lessons.Evaluation;
using PieceBook.Lessons.Model;

namespace PieceBook.Lessons.Runner
{
    /// <summary>
    /// Wires the drawing steps of a lesson to the pure Drawing Engine API (ARQUITECTURA §4.3) and
    /// scores them (LES-05). A <c>trace</c> step is evaluated against its template + tolerance; a
    /// <c>freeform</c> step is evaluated only on the metrics it lists (no template, so Precision is
    /// skipped, §5). The resulting crowns are handed to the <see cref="LessonRunner"/>.
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

        /// <summary>
        /// Evaluate the current drawing step from the canvas recording and submit its crowns.
        /// Returns the full result for the UI (per-metric breakdown, overall).
        /// </summary>
        public EvaluationResult EvaluateAndSubmitCurrent()
        {
            var step = _runner.Current;
            var recording = _canvas.GetRecording();
            var last = (recording != null && recording.StrokeCount > 0)
                ? recording.Strokes[recording.StrokeCount - 1]
                : null;

            StrokeSampleAdapter.Extract(last, out var pos, out var times, out var pressures);

            EvaluationResult result;
            if (step is TraceStep trace)
            {
                _templates.TryGet(trace.Template, out var template);
                result = TraceEvaluator.Evaluate(pos, times, pressures, trace.Metrics, template, trace.Tolerance);
            }
            else if (step is FreeformStep free)
            {
                result = TraceEvaluator.Evaluate(pos, times, pressures, free.Evaluate);
            }
            else
            {
                throw new InvalidOperationException("Current step is not a drawing step.");
            }

            _runner.SubmitDrawing(result.Crowns);
            return result;
        }
    }
}
