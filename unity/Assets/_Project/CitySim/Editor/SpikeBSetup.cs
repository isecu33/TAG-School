using System.IO;
using System.Linq;
using PieceBook.CitySim.Bootstrap;
using PieceBook.CitySim.Data;
using PieceBook.CitySim.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PieceBook.CitySim.Editor
{
    /// <summary>
    /// One-click setup for SPIKE-B. Generates the CitySim data assets (§9: ZoneDef, PatrolDef,
    /// SurfaceDefs) into CitySim/Content and builds + saves the demo scene. Run:
    /// menu "TAG-School ▸ Spike-B ▸ Setup". Only writes under CitySim/** and the scene.
    /// </summary>
    public static class SpikeBSetup
    {
        private const string ContentDir = "Assets/_Project/CitySim/Content";
        private const string SceneDir = "Assets/_Project/CitySim/Scenes";
        private const string ScenePath = SceneDir + "/SpikeB.unity";

        [MenuItem("TAG-School/Spike-B/Setup", priority = 20)]
        public static void Setup()
        {
            var zone = CreateContent();
            BuildScene(zone);
            EditorUtility.DisplayDialog(
                "SPIKE-B ready",
                "CitySim data assets created in CitySim/Content and the demo scene saved to:\n\n" +
                ScenePath + "\n\nPress Play to test the bombing loop.",
                "OK");
        }

        [MenuItem("TAG-School/Spike-B/Open Scene", priority = 21)]
        public static void OpenScene()
        {
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else Setup();
        }

        private static ZoneDef CreateContent()
        {
            Directory.CreateDirectory(ContentDir);

            var surfaces = CityLayout.BuildSurfaces();
            var savedSurfaces = surfaces.Select(s => SaveAsset(s, $"{ContentDir}/{s.id}.asset")).ToArray();

            var patrol = SaveAsset(CityLayout.BuildPatrol(), $"{ContentDir}/patrol_polygon_loop.asset");
            var zone = SaveAsset(CityLayout.BuildZone(patrol, savedSurfaces), $"{ContentDir}/zone_polygon.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return zone;
        }

        private static T SaveAsset<T>(T instance, string path) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                Object.DestroyImmediate(instance);
                return existing;
            }
            AssetDatabase.CreateAsset(instance, path);
            return instance;
        }

        private static void BuildScene(ZoneDef zone)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var go = new GameObject("SpikeB");
            var boot = go.AddComponent<SpikeBBootstrap>();
            boot.zone = zone;

            Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, ScenePath);

            var list = EditorBuildSettings.scenes.ToList();
            if (list.All(s => s.path != ScenePath))
                list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
