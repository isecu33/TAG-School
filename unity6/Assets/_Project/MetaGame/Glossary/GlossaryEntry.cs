using System;

namespace PieceBook.MetaGame.Glossary
{
    /// <summary>
    /// A glossary term (ARQUITECTURA §9 GlossaryEntry {id, term, definition, era, media?,
    /// relatedTerms[]}). Plain data, parsed from JSON authored by design, cross-linked by
    /// <c>relatedTerms</c>. Lessons/quizzes reference these by id via <c>glossaryRefs</c> (§5).
    /// </summary>
    [Serializable]
    public sealed class GlossaryEntry
    {
        public string id;
        public string term;
        public string definition;
        public string era;
        public string media;              // optional
        public string[] relatedTerms = Array.Empty<string>();
    }
}
