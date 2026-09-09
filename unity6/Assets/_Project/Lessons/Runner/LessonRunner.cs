using System;
using System.Collections.Generic;
using PieceBook.Core.Events;
using PieceBook.Lessons.Model;
using UnityEngine;

namespace PieceBook.Lessons.Runner
{
    public enum LessonState { NotStarted, Running, Completed }

    /// <summary>
    /// Hierarchical-ish state machine that walks a lesson's steps in order
    /// (showcase → trace → freeform → quiz, ARQUITECTURA §5, §7 "State Machine"). It is pure C#
    /// (no MonoBehaviour) so it is testable in EditMode; the UI drives it and renders
    /// <see cref="Current"/>. On the last step it publishes <see cref="LessonPassed"/> on the
    /// typed <see cref="EventBus"/> (§3) — the engine never talks to MetaGame/UI directly.
    /// </summary>
    public sealed class LessonRunner
    {
        private readonly EventBus _bus;
        private readonly List<int> _drawingCrowns = new List<int>(8);

        public LessonDef Lesson { get; private set; }
        public LessonState State { get; private set; } = LessonState.NotStarted;
        public int CurrentIndex { get; private set; } = -1;

        /// <summary>Final crown score once <see cref="State"/> is <see cref="LessonState.Completed"/>.</summary>
        public int AwardedCrowns { get; private set; }

        public event Action<LessonStep> StepChanged;
        public event Action<int> Completed; // crowns

        public LessonRunner(EventBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public LessonStep Current =>
            (CurrentIndex >= 0 && Lesson != null && CurrentIndex < Lesson.Steps.Count)
                ? Lesson.Steps[CurrentIndex]
                : null;

        public void Start(LessonDef lesson)
        {
            Lesson = lesson ?? throw new ArgumentNullException(nameof(lesson));
            if (lesson.Steps.Count == 0) throw new ArgumentException($"Lesson '{lesson.Id}' has no steps (§5).");
            _drawingCrowns.Clear();
            AwardedCrowns = 0;
            CurrentIndex = 0;
            State = LessonState.Running;
            StepChanged?.Invoke(Current);
        }

        /// <summary>Advance a non-scored showcase step.</summary>
        public void CompleteShowcase()
        {
            RequireKind(StepKind.Showcase);
            Advance();
        }

        /// <summary>Submit a scored drawing step (trace/freeform). Crowns come from <see cref="Evaluation.TraceEvaluator"/>.</summary>
        public void SubmitDrawing(int crowns)
        {
            if (Current == null || (Current.Kind != StepKind.Trace && Current.Kind != StepKind.Freeform))
                throw new InvalidOperationException("Current step is not a drawing step.");
            _drawingCrowns.Add(Mathf.Clamp(crowns, 1, 3));
            Advance();
        }

        /// <summary>Answer a quiz. Returns true and advances only if correct (§5).</summary>
        public bool AnswerQuiz(int optionIndex)
        {
            RequireKind(StepKind.Quiz);
            var quiz = (QuizStep)Current;
            if (optionIndex != quiz.CorrectIndex) return false;
            Advance();
            return true;
        }

        private void Advance()
        {
            CurrentIndex++;
            if (CurrentIndex >= Lesson.Steps.Count) Finish();
            else StepChanged?.Invoke(Current);
        }

        private void Finish()
        {
            State = LessonState.Completed;
            AwardedCrowns = ComputeCrowns();
            _bus.Publish(new LessonPassed(Lesson.Id, AwardedCrowns, Lesson.Unlocks));
            Completed?.Invoke(AwardedCrowns);
        }

        private int ComputeCrowns()
        {
            if (_drawingCrowns.Count == 0) return 3; // no scored steps: completion earns full marks
            float sum = 0f;
            for (int i = 0; i < _drawingCrowns.Count; i++) sum += _drawingCrowns[i];
            return Mathf.Clamp(Mathf.RoundToInt(sum / _drawingCrowns.Count), 1, 3);
        }

        private void RequireKind(StepKind kind)
        {
            if (Current == null || Current.Kind != kind)
                throw new InvalidOperationException($"Current step is not {kind}.");
        }
    }
}
