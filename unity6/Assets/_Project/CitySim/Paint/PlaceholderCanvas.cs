using UnityEngine;
using UnityEngine.Rendering;

namespace PieceBook.CitySim.Paint
{
    /// <summary>
    /// A DELIBERATELY trivial, self-contained paint surface for the first-person step. It is
    /// NOT and does not reference the Drawing Engine (Spike A) — the two spikes are validated
    /// independently. Dabs a soft dot into its own RenderTexture via a CommandBuffer and
    /// tracks coarse coverage so the loop knows when a piece is "done".
    /// </summary>
    public sealed class PlaceholderCanvas
    {
        private static readonly Matrix4x4 Proj = Matrix4x4.Ortho(0f, 1f, 0f, 1f, -1f, 1f);

        private readonly RenderTexture _rt;
        private readonly Material _mat;
        private readonly Mesh _quad;
        private readonly CommandBuffer _cb;
        private readonly bool[] _covered;
        private readonly int _covRes;
        private int _coveredCount;

        public RenderTexture Texture => _rt;
        public float Coverage => _covered.Length > 0 ? _coveredCount / (float)_covered.Length : 0f;

        public PlaceholderCanvas(int size = 512, int coverageRes = 40)
        {
            _rt = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32) { name = "SpikeB_Placeholder" };
            _rt.Create();

            _mat = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
            _mat.mainTexture = BuildSoftDot(64);
            _mat.color = new Color(0.1f, 0.1f, 0.12f); // spray ink (fixed for the spike)

            _quad = BuildQuad();
            _cb = new CommandBuffer { name = "CitySim.PlaceholderPaint" };

            _covRes = Mathf.Max(8, coverageRes);
            _covered = new bool[_covRes * _covRes];
            Clear();
        }

        public void Clear()
        {
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            GL.Clear(false, true, new Color(0.52f, 0.50f, 0.46f)); // bare wall
            RenderTexture.active = prev;
            System.Array.Clear(_covered, 0, _covered.Length);
            _coveredCount = 0;
        }

        public void Paint(Vector2 uv, float radius01 = 0.055f)
        {
            uv.x = Mathf.Clamp01(uv.x); uv.y = Mathf.Clamp01(uv.y);
            float d = radius01 * 2f;
            var m = Matrix4x4.TRS(new Vector3(uv.x, uv.y, 0f), Quaternion.identity, new Vector3(d, d, 1f));

            _cb.Clear();
            _cb.SetRenderTarget(_rt);
            _cb.SetViewProjectionMatrices(Matrix4x4.identity, Proj);
            _cb.DrawMesh(_quad, m, _mat);
            Graphics.ExecuteCommandBuffer(_cb);

            MarkCoverage(uv, radius01);
        }

        public void Dispose()
        {
            _cb?.Dispose();
            if (_rt != null) { _rt.Release(); Object.Destroy(_rt); }
            if (_mat != null) Object.Destroy(_mat);
            if (_quad != null) Object.Destroy(_quad);
        }

        private void MarkCoverage(Vector2 uv, float r)
        {
            int cx = Mathf.Clamp((int)(uv.x * _covRes), 0, _covRes - 1);
            int cy = Mathf.Clamp((int)(uv.y * _covRes), 0, _covRes - 1);
            int rad = Mathf.Max(1, Mathf.CeilToInt(r * _covRes));
            for (int dy = -rad; dy <= rad; dy++)
                for (int dx = -rad; dx <= rad; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || y < 0 || x >= _covRes || y >= _covRes) continue;
                    if (dx * dx + dy * dy > rad * rad) continue;
                    int idx = y * _covRes + x;
                    if (!_covered[idx]) { _covered[idx] = true; _coveredCount++; }
                }
        }

        private static Texture2D BuildSoftDot(int s)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            var px = new Color[s * s];
            float r = s * 0.5f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dd = Vector2.Distance(new Vector2(x, y), new Vector2(r, r)) / r;
                    float a = Mathf.Clamp01(1f - dd);
                    px[y * s + x] = new Color(1f, 1f, 1f, a * a);
                }
            t.SetPixels(px);
            t.Apply(false, false);
            return t;
        }

        private static Mesh BuildQuad()
        {
            var m = new Mesh { name = "CitySim_PaintQuad" };
            m.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f)
            };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.RecalculateBounds();
            return m;
        }
    }
}
