using System.IO;
using NUnit.Framework;
using PieceBook.Lessons.Evaluation;
using PieceBook.Lessons.Model;
using PieceBook.Lessons.Runner;
using UnityEngine;

namespace PieceBook.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for Chapter-1 content (Fase 1: CNT-02, CNT-03). Reads the authored lesson JSON
    /// straight from the project and checks it parses and stays consistent with the alphabet naming,
    /// plus the alphabet→template→evaluator path.
    /// </summary>
    public class ContentEditTests
    {
        private static string LessonsDir =>
            Path.Combine(Application.dataPath, "_Project", "Content", "Lessons");

        [Test]
        public void AllFiveTagLessons_ParseWithFourSteps()
        {
            for (int i = 1; i <= 5; i++)
            {
                var path = Path.Combine(LessonsDir, $"lesson_tag_0{i}.json");
                Assert.IsTrue(File.Exists(path), $"Missing {path}");

                var lesson = LessonJsonParser.Parse(File.ReadAllText(path));
                Assert.AreEqual($"lesson_tag_0{i}", lesson.Id);
                Assert.AreEqual(4, lesson.StepCount, $"{lesson.Id} should have 4 steps (§5).");
                Assert.AreEqual(1, lesson.Chapter);
            }
        }

        [Test]
        public void EveryTraceTemplate_UsesHandstyleNaming()
        {
            // trace templates must resolve against the generated handstyle alphabet (tpl_hs_*).
            for (int i = 1; i <= 5; i++)
            {
                var lesson = LessonJsonParser.Parse(File.ReadAllText(Path.Combine(LessonsDir, $"lesson_tag_0{i}.json")));
                foreach (var step in lesson.Steps)
                    if (step is TraceStep t)
                        StringAssert.StartsWith("tpl_hs_", t.Template, $"{lesson.Id}: template '{t.Template}'");
            }
        }

        [Test]
        public void AlphabetTemplateProvider_ResolvesAndScores()
        {
            var alphabet = ScriptableObject.CreateInstance<AlphabetDef>();
            alphabet.id = "alphabet_handstyle";
            alphabet.style = AlphabetStyle.Handstyle;
            alphabet.letters = new[]
            {
                new AlphabetDef.Letter
                {
                    letter = 'A',
                    templateId = "tpl_hs_A",
                    points = new[] { new Vector2(0f, 0.5f), new Vector2(1f, 0.5f) },
                },
            };

            var provider = new AlphabetTemplateProvider(alphabet);
            Assert.IsTrue(provider.TryGet("tpl_hs_A", out var template));
            Assert.IsFalse(provider.TryGet("tpl_missing", out _));

            // A point exactly on the template line has zero distance.
            Assert.That(template.DistanceTo(new Vector2(0.5f, 0.5f)), Is.LessThan(1e-4f));

            Object.DestroyImmediate(alphabet);
        }
    }
}
