using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PieceBook.CitySim.Editor
{
    /// <summary>
    /// One-click URP wiring for the project (audit WP1). The runtime <see cref="World.MaterialFactory"/>
    /// already falls back to Standard when no pipeline is active (so a clean checkout never renders
    /// magenta), but the gray-box looks best under URP. This menu assigns an existing URP asset to
    /// Graphics + Quality settings and registers the spray shader as Always-Included so a player
    /// build finds it.
    ///
    /// It intentionally uses only core `UnityEngine.Rendering` types (RenderPipelineAsset,
    /// GraphicsSettings, QualitySettings) so this editor assembly does NOT hard-depend on the URP
    /// package assembly. Creating a URP asset from scratch needs that package reference; if none
    /// exists yet, the menu tells you how to make one in one click, then re-run.
    /// </summary>
    public static class RenderPipelineSetup
    {
        [MenuItem("TAG-School/Setup/Configure URP", priority = 40)]
        public static void ConfigureUrp()
        {
            var guids = AssetDatabase.FindAssets("t:RenderPipelineAsset");
            if (guids.Length == 0)
            {
                Debug.LogWarning(
                    "[RenderPipelineSetup] No RenderPipelineAsset found in the project. Create one via " +
                    "Assets ▸ Create ▸ Rendering ▸ URP Asset (with Universal Renderer), then re-run " +
                    "TAG-School ▸ Setup ▸ Configure URP. (The runtime already falls back to Standard, " +
                    "so the scene still renders without this — just not with URP.)");
            }
            else
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var rp = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
                GraphicsSettings.defaultRenderPipeline = rp;
                QualitySettings.renderPipeline = rp;
                Debug.Log($"[RenderPipelineSetup] Assigned '{path}' to Graphics + current Quality level.");
                if (guids.Length > 1)
                    Debug.LogWarning($"[RenderPipelineSetup] {guids.Length} RenderPipelineAssets exist; used the first. " +
                                     "Assign the intended one manually if that's wrong.");
            }

            EnsureShaderIncluded("PieceBook/SprayStamp");
            AssetDatabase.SaveAssets();
        }

        /// <summary>Register a shader in GraphicsSettings' Always-Included list so it survives a
        /// player build even when only referenced by name at runtime (mirrors SpikeSetup).</summary>
        private static void EnsureShaderIncluded(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[RenderPipelineSetup] '{shaderName}' not found; skipping Always-Included.");
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
    }
}
