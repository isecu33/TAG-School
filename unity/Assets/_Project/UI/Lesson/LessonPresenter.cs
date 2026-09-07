using System;
using PieceBook.Lessons.Model;
using PieceBook.Lessons.Runner;

namespace PieceBook.UI.Lesson
{
    /// <summary>
    /// A "dumb" lesson view (ARQUITECTURA §7 MVP). Implemented by a prefab MonoBehaviour in the
    /// scene; the presenter is what holds the logic and what tests exercise.
    /// </summary>
    public interface ILessonView
    {
        void ShowShowcase(string media, float maxSeconds);
        void ShowDrawingStep(string prompt);   // trace / freeform
        void ShowQuiz(string[] glossaryRefs);
        void Hide();
    }

    /// <summary>
    /// Presenter for the lesson flow (UI-02). Renders <see cref="ShowcaseStep"/> and
    /// <see cref="QuizStep"/> steps and hands drawing steps off to the paint screen; it is driven by
    /// the <see cref="LessonRunner"/> and never touches Unity, so it is testable in EditMode.
    /// </summary>
    public sealed class LessonPresenter : IDisposable
    {
        private readonly LessonRunner _runner;
        private readonly ILessonView _view;

        /// <summary>Raised when the current step is a drawing step (the app switches to Paint).</summary>
        public event Action<LessonStep> DrawingStepRequested;

        public LessonPresenter(LessonRunner runner, ILessonView view)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _runner.StepChanged += Render;
        }

        /// <summary>Render whatever step is current (call once after Start too).</summary>
        public void RenderCurrent() => Render(_runner.Current);

        private void Render(LessonStep step)
        {
            switch (step)
            {
                case ShowcaseStep s:
                    _view.ShowShowcase(s.Media, s.MaxSeconds);
                    break;
                case TraceStep t:
                    _view.ShowDrawingStep($"Traza: {t.Template}");
                    DrawingStepRequested?.Invoke(t);
                    break;
                case FreeformStep f:
                    _view.ShowDrawingStep(f.Prompt);
                    DrawingStepRequested?.Invoke(f);
                    break;
                case QuizStep q:
                    _view.ShowQuiz(q.GlossaryRefs);
                    break;
                case null:
                    _view.Hide();
                    break;
            }
        }

        public void CompleteShowcase() => _runner.CompleteShowcase();
        public bool AnswerQuiz(int optionIndex) => _runner.AnswerQuiz(optionIndex);

        public void Dispose() => _runner.StepChanged -= Render;
    }
}
