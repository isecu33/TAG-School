using System.Collections.Generic;

namespace PieceBook.Lessons.Model
{
    /// <summary>
    /// A lesson: an ordered list of steps plus meta (ARQUITECTURA §5, §9 LessonDef
    /// {id, chapter, steps[], unlocks[], glossaryRefs[]}). Plain data so it round-trips through
    /// JSON (the authoring format of §5) and is trivially testable. Authored JSON lives under
    /// <c>Content/Lessons</c> and is loaded via <see cref="LessonCatalog"/>.
    /// </summary>
    public sealed class LessonDef
    {
        public string Id;
        public string Title;
        public int Chapter;
        public readonly List<LessonStep> Steps = new List<LessonStep>();
        public string[] Unlocks = System.Array.Empty<string>();
        public string[] GlossaryRefs = System.Array.Empty<string>();

        public int StepCount => Steps.Count;
    }
}
