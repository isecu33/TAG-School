using System.Collections.Generic;
using System.IO;
using PieceBook.Core.Audio;
using PieceBook.Core.Services;
using PieceBook.DrawingEngine.Config;
using PieceBook.Lessons.Model;
using UnityEditor;
using UnityEngine;

namespace PieceBook.Content.Editor
{
    /// <summary>
    /// Generates the Chapter-1 content assets for the vertical slice (CNT-01..03): 3+1 caps, paints,
    /// a handstyle alphabet (26 procedural templates), the audio catalog and a lesson catalog wiring
    /// the authored JSON lessons. Same approach as the Phase-0 spike's generated brick wall — real
    /// art/letterforms replace these placeholders later, but the data shape is production-correct
    /// (ARQUITECTURA §9, §7 "ScriptableObject como catálogo").
    /// </summary>
    public static class Chapter1ContentBuilder
    {
        private const string Root = "Assets/_Project/Content";
        private const string GenDir = Root + "/Generated";
        private const string LessonsDir = Root + "/Lessons";

        [MenuItem("TAG-School/Build MVP Content (Ch. 1-3)")]
        public static void Build()
        {
            Directory.CreateDirectory(GenDir);

            var caps = new[]
            {
                Cap("cap_skinny", "Skinny", 4f, 22f, 0.85f, 0.2f, 0.1f, 0),
                Cap("cap_soft",   "Soft",   12f, 18f, 0.6f, 0.25f, 0.25f, 30),
                Cap("cap_fat",    "Fat",    25f, 14f, 0.4f, 0.3f, 0.45f, 60),
                Cap("cap_ny_fat", "NY Fat", 30f, 12f, 0.35f, 0.32f, 0.55f, 120),
            };
            var paint = Paint("paint_default", "All-purpose", 1f, 0.2f, 1.2f);

            var handstyle = BuildAlphabet("alphabet_handstyle", "tpl_hs_", Lessons.Model.AlphabetStyle.Handstyle);
            var bubble = BuildAlphabet("alphabet_bubble", "tpl_bub_", Lessons.Model.AlphabetStyle.Bubble);
            var audio = BuildAudioCatalog();
            var lessons = BuildLessonCatalog();
            var glossary = BuildGlossaryCatalog();
            var mockups = BuildMockups();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var summary = $"MVP content built (chapters 1-3):\n" +
                          $"- {caps.Length} caps + 1 paint\n" +
                          $"- alphabets: {handstyle.letters.Length} handstyle + {bubble.letters.Length} bubble\n" +
                          $"- audio catalog ({audio.entries.Length} sfx)\n" +
                          $"- lesson catalog ({lessons.lessons.Length} lessons)\n" +
                          $"- glossary + {mockups} wall mockups";
            Debug.Log("[Chapter1ContentBuilder] " + summary);
            EditorUtility.DisplayDialog("TAG-School", summary, "OK");
        }

        private static CapDef Cap(string id, string name, float cone, float density,
                                  float falloff, float flow, float overspray, int cost)
        {
            var c = Load<CapDef>($"{GenDir}/{id}.asset") ?? Create<CapDef>($"{GenDir}/{id}.asset");
            c.id = id; c.displayName = name; c.coneAngle = cone; c.density = density;
            c.falloff = falloff; c.flowRate = flow; c.overspray = overspray; c.unlockCost = cost;
            EditorUtility.SetDirty(c);
            return c;
        }

        private static PaintDef Paint(string id, string name, float opacity, float gloss, float drip)
        {
            var p = Load<PaintDef>($"{GenDir}/{id}.asset") ?? Create<PaintDef>($"{GenDir}/{id}.asset");
            p.id = id; p.displayName = name; p.opacity = opacity; p.glossiness = gloss;
            p.dripThreshold = drip; p.colorRange = new[] { Color.white, Color.black };
            EditorUtility.SetDirty(p);
            return p;
        }

        private static AlphabetDef BuildAlphabet(string id, string templatePrefix, AlphabetStyle style)
        {
            var a = Load<AlphabetDef>($"{GenDir}/{id}.asset") ?? Create<AlphabetDef>($"{GenDir}/{id}.asset");
            a.id = id;
            a.style = style;

            var letters = new AlphabetDef.Letter[26];
            for (int i = 0; i < 26; i++)
            {
                char ch = (char)('A' + i);
                letters[i] = new AlphabetDef.Letter
                {
                    letter = ch,
                    templateId = $"{templatePrefix}{ch}",
                    points = GenerateLetterPolyline(i),
                    strokeOrder = new[] { 0 },
                    difficulty = 1 + (i % 3),
                };
            }
            a.letters = letters;
            EditorUtility.SetDirty(a);
            return a;
        }

        // A deterministic, non-empty placeholder polyline per letter inside [0.2,0.8]².
        private static Vector2[] GenerateLetterPolyline(int index)
        {
            const int n = 6;
            var pts = new Vector2[n];
            float phase = index * 0.7f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float x = Mathf.Lerp(0.25f, 0.75f, t);
                float y = 0.5f + 0.22f * Mathf.Sin(phase + t * Mathf.PI * (1 + index % 3));
                pts[i] = new Vector2(x, y);
            }
            return pts;
        }

        private static AudioCatalog BuildAudioCatalog()
        {
            var cat = Load<AudioCatalog>($"{GenDir}/AudioCatalog.asset")
                      ?? Create<AudioCatalog>($"{GenDir}/AudioCatalog.asset");
            cat.entries = new[]
            {
                new AudioCatalog.Entry { id = SfxId.SprayLoop, clip = null, volume = 0.8f, loop = true },
                new AudioCatalog.Entry { id = SfxId.UiClick,   clip = null, volume = 1f,   loop = false },
                new AudioCatalog.Entry { id = SfxId.Crown,     clip = null, volume = 1f,   loop = false },
                new AudioCatalog.Entry { id = SfxId.Unlock,    clip = null, volume = 1f,   loop = false },
            };
            EditorUtility.SetDirty(cat);
            return cat;
        }

        // All authored lessons across the three MVP chapters, in play order.
        private static readonly string[] LessonFiles =
        {
            "lesson_tag_01", "lesson_tag_02", "lesson_tag_03", "lesson_tag_04", "lesson_tag_05",
            "lesson_throwup_01", "lesson_throwup_02", "lesson_throwup_03",
            "lesson_color_01", "lesson_color_02",
        };

        private static LessonCatalog BuildLessonCatalog()
        {
            var cat = Load<LessonCatalog>($"{GenDir}/LessonCatalog.asset")
                      ?? Create<LessonCatalog>($"{GenDir}/LessonCatalog.asset");

            var list = new List<TextAsset>();
            foreach (var name in LessonFiles)
            {
                var ta = AssetDatabase.LoadAssetAtPath<TextAsset>($"{LessonsDir}/{name}.json");
                if (ta != null) list.Add(ta);
                else Debug.LogWarning($"[Chapter1ContentBuilder] Missing {name}.json");
            }
            cat.lessons = list.ToArray();
            EditorUtility.SetDirty(cat);
            return cat;
        }

        private static PieceBook.MetaGame.Glossary.GlossaryCatalog BuildGlossaryCatalog()
        {
            var cat = Load<PieceBook.MetaGame.Glossary.GlossaryCatalog>($"{GenDir}/GlossaryCatalog.asset")
                      ?? Create<PieceBook.MetaGame.Glossary.GlossaryCatalog>($"{GenDir}/GlossaryCatalog.asset");
            cat.glossaryJson = AssetDatabase.LoadAssetAtPath<TextAsset>($"{Root}/Glossary/glossary.json");
            if (cat.glossaryJson == null) Debug.LogWarning("[Chapter1ContentBuilder] Missing glossary.json");
            EditorUtility.SetDirty(cat);
            return cat;
        }

        private static int BuildMockups()
        {
            var ids = new[] { "mockup_shutter", "mockup_train", "mockup_hall" };
            foreach (var id in ids)
            {
                var m = Load<PieceBook.Social.Mockups.WallMockup>($"{GenDir}/{id}.asset")
                        ?? Create<PieceBook.Social.Mockups.WallMockup>($"{GenDir}/{id}.asset");
                m.id = id;
                m.displayName = id.Replace("mockup_", "");
                EditorUtility.SetDirty(m);
            }
            return ids.Length;
        }

        private static T Create<T>(string path) where T : ScriptableObject
        {
            var so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static T Load<T>(string path) where T : ScriptableObject =>
            AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
