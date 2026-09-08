using System;
using UnityEngine;

namespace PieceBook.MetaGame.Glossary
{
    /// <summary>
    /// Parses the authored glossary JSON (an array of <see cref="GlossaryEntry"/> under a
    /// <c>entries</c> key) into a <see cref="Glossary"/>. Data-driven so design edits terms without a
    /// recompile (§7 catalog).
    /// </summary>
    public static class GlossaryJsonParser
    {
        [Serializable]
        private sealed class Wrapper
        {
            public GlossaryEntry[] entries = Array.Empty<GlossaryEntry>();
        }

        public static Glossary Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return new Glossary(null);
            var w = JsonUtility.FromJson<Wrapper>(json);
            return new Glossary(w != null ? w.entries : null);
        }
    }
}
