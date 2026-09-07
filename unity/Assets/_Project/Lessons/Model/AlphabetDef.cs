using System;
using UnityEngine;

namespace PieceBook.Lessons.Model
{
    /// <summary>Lettering styles (ARQUITECTURA §9 AlphabetDef.style).</summary>
    public enum AlphabetStyle { Handstyle, Blockbuster, Bubble, SemiWild, Wildstyle }

    /// <summary>
    /// Alphabet catalog entry (ARQUITECTURA §9 AlphabetDef): 26 letters, each with a trace template
    /// (a polyline in normalized canvas space), a stroke order and a difficulty. The polyline is the
    /// "precomputed" template the evaluator scores against (§5); real letterform art replaces the
    /// procedurally-generated placeholders (CNT-02, like the spike's generated brick wall).
    /// </summary>
    [CreateAssetMenu(menuName = "PieceBook/Alphabet Definition", fileName = "Alphabet_New")]
    public sealed class AlphabetDef : ScriptableObject
    {
        [Serializable]
        public sealed class Letter
        {
            public char letter = 'A';
            [Tooltip("Stable template id referenced by lessons (§5 TraceStep.template).")]
            public string templateId = "tpl_new";
            [Tooltip("Template polyline in normalized canvas space [0,1].")]
            public Vector2[] points = Array.Empty<Vector2>();
            [Tooltip("Suggested stroke order (indices into a letter's substrokes). Informational for the slice.")]
            public int[] strokeOrder = Array.Empty<int>();
            [Range(0, 5)] public int difficulty = 1;
        }

        public string id = "alphabet_new";
        public AlphabetStyle style = AlphabetStyle.Handstyle;
        public Letter[] letters = Array.Empty<Letter>();
    }
}
