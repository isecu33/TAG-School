using System.Collections.Generic;
using PieceBook.Lessons.Evaluation;
using PieceBook.Lessons.Model;

namespace PieceBook.Lessons.Runner
{
    /// <summary>
    /// <see cref="ITemplateProvider"/> backed by one or more <see cref="AlphabetDef"/> assets (CNT-02).
    /// Indexes every letter's template by its id so a <c>trace</c> step resolves its template in O(1).
    /// </summary>
    public sealed class AlphabetTemplateProvider : ITemplateProvider
    {
        private readonly Dictionary<string, TraceTemplate> _byId = new Dictionary<string, TraceTemplate>(64);

        public AlphabetTemplateProvider(params AlphabetDef[] alphabets)
        {
            if (alphabets == null) return;
            foreach (var a in alphabets)
            {
                if (a == null) continue;
                foreach (var letter in a.letters)
                    if (!string.IsNullOrEmpty(letter.templateId) && letter.points != null && letter.points.Length > 0)
                        _byId[letter.templateId] = new TraceTemplate(letter.points);
            }
        }

        public bool TryGet(string templateId, out TraceTemplate template) =>
            _byId.TryGetValue(templateId, out template);
    }
}
