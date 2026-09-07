using System;

namespace PieceBook.Lessons.Model
{
    /// <summary>The four step kinds a lesson is made of (ARQUITECTURA §5).</summary>
    public enum StepKind { Showcase, Trace, Freeform, Quiz }

    /// <summary>Geometric evaluation metrics over the stroke polyline (ARQUITECTURA §5).</summary>
    public enum TraceMetric { Precision, Smoothness, SpeedConsistency, LineWeight }

    /// <summary>Base class for a lesson step. Concrete kinds carry their own payload (§5).</summary>
    [Serializable]
    public abstract class LessonStep
    {
        public abstract StepKind Kind { get; }
    }

    /// <summary>Play an intro clip for up to <see cref="MaxSeconds"/> (§5 "showcase").</summary>
    [Serializable]
    public sealed class ShowcaseStep : LessonStep
    {
        public string Media;
        public float MaxSeconds;
        public override StepKind Kind => StepKind.Showcase;
    }

    /// <summary>Trace a template within a tolerance; scored on the listed metrics (§5 "trace").</summary>
    [Serializable]
    public sealed class TraceStep : LessonStep
    {
        public string Template;
        public float Tolerance;
        public TraceMetric[] Metrics = Array.Empty<TraceMetric>();
        public override StepKind Kind => StepKind.Trace;
    }

    /// <summary>Free drawing with a prompt; scored only on the listed metrics (§5 "freeform").</summary>
    [Serializable]
    public sealed class FreeformStep : LessonStep
    {
        public string Prompt;
        public TraceMetric[] Evaluate = Array.Empty<TraceMetric>();
        public override StepKind Kind => StepKind.Freeform;
    }

    /// <summary>Glossary quiz referencing entries by id (§5 "quiz", §9 GlossaryEntry).</summary>
    [Serializable]
    public sealed class QuizStep : LessonStep
    {
        public string[] GlossaryRefs = Array.Empty<string>();
        /// <summary>Index of the correct option among the presented glossary terms.</summary>
        public int CorrectIndex;
        public override StepKind Kind => StepKind.Quiz;
    }
}
