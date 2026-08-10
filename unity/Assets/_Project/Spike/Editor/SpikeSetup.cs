using System.IO;
using System.Linq;
using PieceBook.DrawingEngine.Config;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PieceBook.Spike.Editor
{
    /// <summary>
    /// One-click setup for the Phase-0 spike. Generates the Content ScriptableObjects
    /// (3 caps + paint), makes sure the spray shader survives player builds, and builds +
    /// saves the demo scene. Run: menu "TAG-School ▸ Setup Phase 0 Spike".
    /// </summary>
    public static class SpikeSetup
    {
        private const string ContentDir = "Assets/_Project/Content";
        private const string SceneDir = "Assets/_Project/Spike/Scenes";
        private const string ScenePath = SceneDir + "/SpraySpike.unity";

        [MenuItem("TAG-School/Setup Phase 0 Spike", priority = 0)]
        public static void Setup()
        {
            var (caps, paint) = CreateContent();
            EnsureShaderIncluded();
            BuildScene(caps, paint);

            EditorUtility.DisplayDialog(
                "TAG-School — Spike ready",
                "Content assets created in _Project/Content, spray shader added to Always " +
                "Included Shaders, and the demo scene saved to:\n\n" + ScenePath +
                "\n\nPress Play to test. See README.md to build on device.",
                "OK");
        }

        [MenuItem("TAG-School/Create Content Assets Only", priority = 1)]
        public static void CreateContentOnly() => CreateContent();

        [MenuItem("TAG-School/Open Spike Scene", priority = 2)]
        public static void OpenScene()
        {
            if (File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else
                Setup();
        }

        private static (CapDef[] caps, PaintDef paint) CreateContent()
        {
            Directory.CreateDirectory(ContentDir);

            var skinny = SaveAsset(SpikeDefaults.MakeSkinny(), ContentDir + "/Cap_Skinny.asset");
            var soft = SaveAsset(SpikeDefaults.MakeSoft(), ContentDir + "/Cap_Soft.asset");
            var fat = SaveAsset(SpikeDefaults.MakeFat(), ContentDir + "/Cap_Fat.asset");
            var paint = SaveAsset(SpikeDefaults.MakePaint(), ContentDir + "/Paint_Default.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return (new[] { skinny, soft, fat }, paint);
        }

        private static T SaveAsset<T>(T instance, string path) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                Object.DestroyImmediate(instance); // avoid leaking the throwaway instance
                return existing;
            }
            AssetDatabase.CreateAsset(instance, path);
            return instance;
        }

        private static void EnsureShaderIncluded()
        {
            var shader = Shader.Find("PieceBook/SprayStamp");
            if (shader == null)
            {
                Debug.LogWarning("[SpikeSetup] PieceBook/SprayStamp not found; skipping Always-Included registration.");
                return;
            }

            var so = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            for (int i = 0; i < arr.arraySize; i++)
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;

            int idx = arr.arraySize;
            arr.InsertArrayElementAtIndex(idx);
            arr.GetArrayElementAtIndex(idx).objectReferenceValue = shader;
            so.ApplyModifiedProperties();
        }

        private static void BuildScene(CapDef[] caps, PaintDef paint)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var go = new GameObject("SprayDemo");
            var boot = go.AddComponent<SprayDemoBootstrap>();
            boot.caps = caps;
            boot.paint = paint;

            Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, ScenePath);

            // Make the spike scene the first entry in Build Settings.
            var list = EditorBuildSettings.scenes.ToList();
            if (list.All(s => s.path != ScenePath))
                list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
