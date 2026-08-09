using UnityEngine;

namespace PieceBook.DrawingEngine.Stamping
{
    /// <summary>
    /// Builds the unit quad every spray stamp is drawn with: centered at origin,
    /// spanning [-0.5,0.5] in XY with uv [0,1]. Instance matrices scale/translate it
    /// into canvas space, so one shared mesh serves the whole engine.
    /// </summary>
    public static class QuadMeshFactory
    {
        public static Mesh Create()
        {
            var mesh = new Mesh { name = "PieceBook_StampQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true); // mark no-longer-readable; GPU only
            return mesh;
        }
    }
}
