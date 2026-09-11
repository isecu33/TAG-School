using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PieceBook.Content.Editor
{
    /// <summary>
    /// Builds a playable Phase-1 test scene (editor-only).
    /// Menu: TAG-School ▸ Build Phase 1 Test Scene
    ///
    /// The scene contains a single <see cref="PieceBook.Spike.Phase1TestHarness"/> that
    /// self-assembles at Play time. Choose the test mode in the Inspector:
    ///   • SprayOnly  — production DrawingCanvas with multicapa (ENG-05/06/07 verification)
    ///   • LessonFlow — lesson_tag_01 step-by-step via LessonRunner + on-screen step controls
    /// </summary>
    public static class Phase1SceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Phase1Test.unity";

        [MenuItem("TAG-School/Build Phase 1 Test Scene")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes"))
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("Phase1Harness");
            root.AddComponent<PieceBook.Spike.Phase1TestHarness>();

            var evGo = new GameObject("EventSystem");
            evGo.AddComponent<EventSystem>();
            evGo.AddComponent<StandaloneInputModule>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            AddToBuildSettings(ScenePath);

            Debug.Log($"[Phase1SceneBuilder] Scene written → {ScenePath}. Open and press Play.");
            EditorUtility.DisplayDialog("TAG-School",
                $"Phase 1 test scene → {ScenePath}\n\n" +
                "Open it and press Play.\n" +
                "Switch mode in Inspector on Phase1Harness:\n" +
                "  SprayOnly  — free spray (production canvas)\n" +
                "  LessonFlow — lesson_tag_01 step-by-step",
                "OK");
        }

        private static void AddToBuildSettings(string path)
        {
            var existing = EditorBuildSettings.scenes;
            foreach (var s in existing)
                if (s.path == path) return;

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(existing)
            {
                new EditorBuildSettingsScene(path, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
