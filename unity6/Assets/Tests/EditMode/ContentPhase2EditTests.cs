using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PieceBook.Lessons.Model;
using PieceBook.MetaGame.Glossary;
using UnityEngine;

namespace PieceBook.Tests.EditMode
{
    /// <summary>
    /// EditMode integrity tests for the MVP content across the 3 chapters (Fase 2: CNT-04..06).
    /// Every lesson parses, every trace template and unlock resolves, and the glossary covers every
    /// referenced term with no dangling links.
    /// </summary>
    public class ContentPhase2EditTests
    {
        private static readonly string[] LessonFiles =
        {
            "lesson_tag_01", "lesson_tag_02", "lesson_tag_03", "lesson_tag_04", "lesson_tag_05",
            "lesson_throwup_01", "lesson_throwup_02", "lesson_throwup_03",
            "lesson_color_01", "lesson_color_02",
        };

        // Cap ids the content builder generates (Chapter1ContentBuilder).
        private static readonly HashSet<string> KnownCaps = new HashSet<string>
        { "cap_skinny", "cap_soft", "cap_fat", "cap_ny_fat" };

        private static string ContentDir => Path.Combine(Application.dataPath, "_Project", "Content");
        private static string LessonPath(string name) => Path.Combine(ContentDir, "Lessons", name + ".json");

        private static List<LessonDef> LoadAll()
        {
            var list = new List<LessonDef>();
            foreach (var f in LessonFiles)
            {
                Assert.IsTrue(File.Exists(LessonPath(f)), $"Missing {f}.json");
                list.Add(LessonJsonParser.Parse(File.ReadAllText(LessonPath(f))));
            }
            return list;
        }

        [Test]
        public void AllLessons_ParseWithFourSteps_AcrossThreeChapters()
        {
            var lessons = LoadAll();
            Assert.AreEqual(10, lessons.Count);
            var chapters = new HashSet<int>();
            foreach (var l in lessons)
            {
                Assert.AreEqual(4, l.StepCount, $"{l.Id} must have 4 steps.");
                chapters.Add(l.Chapter);
            }
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, chapters, "MVP spans chapters 1-3 (§11).");
        }

        [Test]
        public void EveryTraceTemplate_And_Unlock_Resolves()
        {
            foreach (var l in LoadAll())
            {
                foreach (var s in l.Steps)
                    if (s is TraceStep t)
                        StringAssert.StartsWith("tpl_", t.Template, $"{l.Id}: template '{t.Template}'");

                foreach (var u in l.Unlocks)
                    Assert.IsTrue(KnownCaps.Contains(u), $"{l.Id}: unlock '{u}' is not a known cap.");
            }
        }

        [Test]
        public void Glossary_CoversAllReferencedTerms_NoDangling()
        {
            var glossary = GlossaryJsonParser.Parse(
                File.ReadAllText(Path.Combine(ContentDir, "Glossary", "glossary.json")));

            var refs = new HashSet<string>();
            foreach (var l in LoadAll())
            {
                foreach (var r in l.GlossaryRefs) refs.Add(r);
                foreach (var s in l.Steps)
                    if (s is QuizStep q)
                        foreach (var r in q.GlossaryRefs) refs.Add(r);
            }

            var missing = glossary.MissingRefs(refs);
            CollectionAssert.IsEmpty(missing, "Glossary must define every referenced term (§5): " + string.Join(",", missing));
            CollectionAssert.IsEmpty(glossary.DanglingRelatedTerms(), "No dangling relatedTerms.");
        }
    }
}
