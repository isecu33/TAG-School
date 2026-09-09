using System.Collections.Generic;
using UnityEngine;

namespace PieceBook.Lessons.Model
{
    /// <summary>
    /// Data-driven lesson catalog (ARQUITECTURA §7 "ScriptableObject como catálogo"). Holds the
    /// authored lesson JSON files (§5) as <see cref="TextAsset"/>s and parses them into
    /// <see cref="LessonDef"/>s on demand via <see cref="LessonJsonParser"/>. Design adds a lesson
    /// by dropping a JSON into <c>Content/Lessons</c> and referencing it here — no recompile.
    /// </summary>
    [CreateAssetMenu(menuName = "PieceBook/Lesson Catalog", fileName = "LessonCatalog")]
    public sealed class LessonCatalog : ScriptableObject
    {
        [Tooltip("Authored lesson JSON files, in play order (§5).")]
        public TextAsset[] lessons = System.Array.Empty<TextAsset>();

        public List<LessonDef> BuildAll()
        {
            var list = new List<LessonDef>(lessons.Length);
            foreach (var ta in lessons)
                if (ta != null) list.Add(LessonJsonParser.Parse(ta.text));
            return list;
        }

        public LessonDef Build(int index) => LessonJsonParser.Parse(lessons[index].text);
    }
}
