using System;
using System.Collections.Generic;
using UnityEngine;

namespace PieceBook.Lessons.Model
{
    /// <summary>
    /// Parses the lesson JSON format of ARQUITECTURA §5 into a typed <see cref="LessonDef"/>.
    /// Unity's <see cref="JsonUtility"/> cannot deserialize a heterogeneous step array directly,
    /// so we read a flat DTO (every possible field on one struct) and build the typed steps by
    /// their <c>type</c> discriminator. Unknown step types throw — a lesson must be well-formed.
    /// </summary>
    public static class LessonJsonParser
    {
        [Serializable]
        private struct StepDto
        {
            public string type;
            public string media;
            public float maxSeconds;
            public string template;
            public float tolerance;
            public string[] metrics;
            public string prompt;
            public string[] evaluate;
            public string[] glossaryRefs;
            public int correctIndex;
        }

        [Serializable]
        private struct LessonDto
        {
            public string id;
            public string title;
            public int chapter;
            public string[] unlocks;
            public string[] glossaryRefs;
            public StepDto[] steps;
        }

        public static LessonDef Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("Empty lesson JSON", nameof(json));

            var dto = JsonUtility.FromJson<LessonDto>(json);
            var def = new LessonDef
            {
                Id = dto.id,
                Title = dto.title,
                Chapter = dto.chapter,
                Unlocks = dto.unlocks ?? Array.Empty<string>(),
                GlossaryRefs = dto.glossaryRefs ?? Array.Empty<string>(),
            };

            if (dto.steps != null)
                foreach (var s in dto.steps)
                    def.Steps.Add(BuildStep(s, dto.id));

            return def;
        }

        private static LessonStep BuildStep(in StepDto s, string lessonId)
        {
            switch (s.type)
            {
                case "showcase":
                    return new ShowcaseStep { Media = s.media, MaxSeconds = s.maxSeconds };
                case "trace":
                    return new TraceStep
                    {
                        Template = s.template,
                        Tolerance = s.tolerance,
                        Metrics = ParseMetrics(s.metrics),
                    };
                case "freeform":
                    return new FreeformStep { Prompt = s.prompt, Evaluate = ParseMetrics(s.evaluate) };
                case "quiz":
                    return new QuizStep
                    {
                        GlossaryRefs = s.glossaryRefs ?? Array.Empty<string>(),
                        CorrectIndex = s.correctIndex,
                    };
                default:
                    throw new FormatException($"Lesson '{lessonId}': unknown step type '{s.type}' (§5).");
            }
        }

        private static TraceMetric[] ParseMetrics(string[] names)
        {
            if (names == null || names.Length == 0) return Array.Empty<TraceMetric>();
            var list = new List<TraceMetric>(names.Length);
            foreach (var n in names) list.Add(ParseMetric(n));
            return list.ToArray();
        }

        private static TraceMetric ParseMetric(string name)
        {
            switch (name)
            {
                case "precision": return TraceMetric.Precision;
                case "smoothness": return TraceMetric.Smoothness;
                case "speed_consistency": return TraceMetric.SpeedConsistency;
                case "line_weight": return TraceMetric.LineWeight;
                default: throw new FormatException($"Unknown trace metric '{name}' (§5).");
            }
        }
    }
}
