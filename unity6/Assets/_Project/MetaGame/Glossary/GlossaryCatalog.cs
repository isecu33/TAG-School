using UnityEngine;

namespace PieceBook.MetaGame.Glossary
{
    /// <summary>
    /// Data-driven glossary catalog (§7 catalog). Holds the authored glossary JSON as a
    /// <see cref="TextAsset"/> and builds a runtime <see cref="Glossary"/> on demand (META-04).
    /// </summary>
    [CreateAssetMenu(menuName = "PieceBook/Glossary Catalog", fileName = "GlossaryCatalog")]
    public sealed class GlossaryCatalog : ScriptableObject
    {
        [Tooltip("Authored glossary JSON (§9 GlossaryEntry array under an 'entries' key).")]
        public TextAsset glossaryJson;

        public Glossary Build() =>
            GlossaryJsonParser.Parse(glossaryJson != null ? glossaryJson.text : null);
    }
}
