using System.IO;
using System.Linq;
using PieceBook.Content.Editor;
using PieceBook.DrawingEngine.Config;
using PieceBook.Lessons.Model;
using PieceBook.MetaGame.Glossary;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PieceBook.Spike.Editor
{
    /// <summary>
    /// One click to a playable build of the game loop: <c>TAG-School ▸ Build Playable Demo</c>.
    /// Generates the MVP content (caps, paint, alphabets, lessons, glossary), registers the
    /// runtime shaders, creates the wall/guide materials and saves a scene whose only object is a
    /// <see cref="DemoGame"/> wired to those assets. The scene goes first in Build Settings, so
    /// the same menu also prepares an iOS/Android build.
    /// </summary>
    public static class DemoSceneBuilder
    {
        private const string SceneDir = "Assets/_Project/Scenes";
        private const string ScenePath = SceneDir + "/DemoPlayable.unity";
        private const string MaterialDir = "Assets/_Project/Spike/Materials";
        private const string WallTexturePath = "Assets/_Project/Art/Textures/Walls/wall_concrete.png";

        [MenuItem("TAG-School/Build Playable Demo", priority = -10)]
        public static void Build()
        {
            string content = Chapter1ContentBuilder.BuildAssets();
            SpikeSetup.EnsureShaderIncluded();

            Directory.CreateDirectory(MaterialDir);
            var wallTex = AssetDatabase.LoadAssetAtPath<Texture2D>(WallTexturePath);
            var wallMat = SaveMaterial($"{MaterialDir}/Demo_Wall.mat", SprayDemoBootstrap.GetUnlitShader(), wallTex);
            var guideMat = SaveMaterial($"{MaterialDir}/Demo_Guide.mat", Shader.Find("Sprites/Default"), null);

            string gen = Chapter1ContentBuilder.GeneratedDir;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("DemoGame");
            // GameContext is the composition root (DI + save + MetaGame + audio). Its catalog is a
            // private serialized field, so it is set through SerializedObject.
            var context = root.AddComponent<PieceBook.UI.App.GameContext>();
            var so = new SerializedObject(context);
            so.FindProperty("audioCatalog").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<PieceBook.Core.Audio.AudioCatalog>($"{gen}/AudioCatalog.asset");
            so.ApplyModifiedPropertiesWithoutUndo();
            var game = root.AddComponent<DemoGame>();
            game.lessonCatalog = AssetDatabase.LoadAssetAtPath<LessonCatalog>($"{gen}/LessonCatalog.asset");
            game.alphabets = new[]
            {
                AssetDatabase.LoadAssetAtPath<AlphabetDef>($"{gen}/alphabet_handstyle.asset"),
                AssetDatabase.LoadAssetAtPath<AlphabetDef>($"{gen}/alphabet_bubble.asset"),
            };
            game.glossaryCatalog = AssetDatabase.LoadAssetAtPath<GlossaryCatalog>($"{gen}/GlossaryCatalog.asset");
            game.caps = new[] { "cap_skinny", "cap_soft", "cap_fat", "cap_ny_fat" }
                .Select(id => AssetDatabase.LoadAssetAtPath<CapDef>($"{gen}/{id}.asset"))
                .Where(c => c != null)
                .ToArray();
            game.paint = AssetDatabase.LoadAssetAtPath<PaintDef>($"{gen}/paint_default.asset");
            game.wallTexture = wallTex;
            game.wallMaterial = wallMat;
            game.guideMaterial = guideMat;

            Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, ScenePath);

            var list = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();

            string missing = game.lessonCatalog == null || game.glossaryCatalog == null || game.caps.Length < 4
                ? "\n\nAVISO: falta algún asset generado; revisa la consola."
                : "";
            Debug.Log($"[DemoSceneBuilder] {ScenePath} listo.\n{content}");
            EditorUtility.DisplayDialog("TAG-School",
                $"Demo jugable lista: {ScenePath}\n\n{content}\n\nPulsa Play.{missing}", "OK");
        }

        private static Material SaveMaterial(string path, Shader shader, Texture texture)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (shader != null)
            {
                mat.shader = shader;
            }
            if (texture != null) mat.mainTexture = texture;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
