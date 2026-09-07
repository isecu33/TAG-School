using PieceBook.Lessons.Evaluation;

namespace PieceBook.Lessons.Runner
{
    /// <summary>
    /// Resolves a template id (from <see cref="Model.TraceStep.Template"/>) to a
    /// <see cref="TraceTemplate"/>. Backed by the alphabet catalog (CNT-02) in the game; a fake
    /// provider serves tests. Keeps the runner independent of how templates are stored.
    /// </summary>
    public interface ITemplateProvider
    {
        bool TryGet(string templateId, out TraceTemplate template);
    }
}
