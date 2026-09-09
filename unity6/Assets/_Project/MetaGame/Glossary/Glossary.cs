using System.Collections.Generic;
using UnityEngine;

namespace PieceBook.MetaGame.Glossary
{
    /// <summary>
    /// Runtime, navigable glossary (ARQUITECTURA §3 META-GAME "glosario", META-04). Indexes entries
    /// by id for O(1) lookup and exposes integrity checks the content pipeline runs: no dangling
    /// <c>relatedTerms</c>, and full coverage of the terms lessons reference (§5 glossaryRefs).
    /// </summary>
    public sealed class Glossary
    {
        private readonly Dictionary<string, GlossaryEntry> _byId;
        public IReadOnlyCollection<GlossaryEntry> Entries => _byId.Values;
        public int Count => _byId.Count;

        public Glossary(IEnumerable<GlossaryEntry> entries)
        {
            _byId = new Dictionary<string, GlossaryEntry>(64);
            if (entries == null) return;
            foreach (var e in entries)
                if (e != null && !string.IsNullOrEmpty(e.id)) _byId[e.id] = e;
        }

        public bool TryGet(string id, out GlossaryEntry entry) => _byId.TryGetValue(id, out entry);

        /// <summary>relatedTerms that point to a term not in the glossary (should be empty).</summary>
        public List<string> DanglingRelatedTerms()
        {
            var dangling = new List<string>();
            foreach (var e in _byId.Values)
                foreach (var rel in e.relatedTerms)
                    if (!_byId.ContainsKey(rel)) dangling.Add($"{e.id}→{rel}");
            return dangling;
        }

        /// <summary>Referenced ids (e.g. from lessons' glossaryRefs) that the glossary is missing.</summary>
        public List<string> MissingRefs(IEnumerable<string> requiredIds)
        {
            var missing = new List<string>();
            if (requiredIds == null) return missing;
            foreach (var id in requiredIds)
                if (!string.IsNullOrEmpty(id) && !_byId.ContainsKey(id) && !missing.Contains(id))
                    missing.Add(id);
            return missing;
        }
    }
}
