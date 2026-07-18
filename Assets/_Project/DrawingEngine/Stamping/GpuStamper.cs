using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace PieceBook.DrawingEngine.Stamping
{
    /// <summary>
    /// GPU stamping core (ARQUITECTURA §4.1: "stamping por GPU ... vía CommandBuffer /
    /// Graphics.DrawMeshInstanced. Nunca SetPixels").
    ///
    /// A whole burst of spray dots is written to the target RenderTexture in a single
    /// instanced draw call. All buffers are preallocated to <see cref="MaxPerBatch"/> and
    /// reused every frame, so stamping causes ZERO managed allocations during a stroke.
    /// </summary>
    public sealed class GpuStamper : IDisposable
    {
        // DrawMeshInstanced hard limit is 1023 instances per call.
        public const int MaxPerBatch = 1023;

        private static readonly Matrix4x4 s_proj = Matrix4x4.Ortho(0f, 1f, 0f, 1f, -1f, 1f);
        private static readonly int s_colorId = Shader.PropertyToID("_Color");
        private static readonly int s_hardId = Shader.PropertyToID("_Hardness");

        private readonly Mesh _quad;
        private readonly Material _material;
        private readonly CommandBuffer _cb;
        private readonly MaterialPropertyBlock _mpb;

        private readonly Matrix4x4[] _matrices = new Matrix4x4[MaxPerBatch];
        private readonly Vector4[] _colors = new Vector4[MaxPerBatch];
        private readonly float[] _hardness = new float[MaxPerBatch];

        private int _count;
        private RenderTexture _target;

        /// <summary>Total instances submitted since the last <see cref="Begin"/> (diagnostics).</summary>
        public int InstancesThisFrame { get; private set; }

        public GpuStamper(Mesh quad, Shader shader)
        {
            _quad = quad != null ? quad : throw new ArgumentNullException(nameof(quad));
            if (shader == null) throw new ArgumentNullException(nameof(shader));

            _material = new Material(shader) { enableInstancing = true, hideFlags = HideFlags.HideAndDontSave };
            _cb = new CommandBuffer { name = "PieceBook.SprayStamps" };
            _mpb = new MaterialPropertyBlock();
        }

        /// <summary>Start a frame's worth of stamps aimed at <paramref name="target"/>.</summary>
        public void Begin(RenderTexture target)
        {
            _target = target;
            _count = 0;
            InstancesThisFrame = 0;
        }

        /// <summary>
        /// Queue one soft spray dot. Auto-flushes when the instance buffer fills, so callers
        /// can add any number of stamps without worrying about the 1023 limit.
        /// </summary>
        public void Add(Vector2 center01, float radius01, Color color, float hardness01)
        {
            if (_count >= MaxPerBatch) Flush();

            float diameter = radius01 * 2f; // unit quad -> full footprint = scale
            _matrices[_count] = Matrix4x4.TRS(
                new Vector3(center01.x, center01.y, 0f),
                Quaternion.identity,
                new Vector3(diameter, diameter, 1f));
            _colors[_count] = color;
            _hardness[_count] = hardness01;
            _count++;
        }

        /// <summary>Submit whatever is queued as one instanced draw into the target RT.</summary>
        public void Flush()
        {
            if (_count == 0 || _target == null) return;

            _mpb.Clear();
            _mpb.SetVectorArray(s_colorId, _colors);
            _mpb.SetFloatArray(s_hardId, _hardness);

            _cb.Clear();
            _cb.SetRenderTarget(_target);
            _cb.SetViewProjectionMatrices(Matrix4x4.identity, s_proj);
            _cb.DrawMeshInstanced(_quad, 0, _material, 0, _matrices, _count, _mpb);
            Graphics.ExecuteCommandBuffer(_cb);

            InstancesThisFrame += _count;
            _count = 0;
        }

        public void Dispose()
        {
            _cb?.Dispose();
            if (_material != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_material);
                else UnityEngine.Object.DestroyImmediate(_material);
            }
        }
    }
}
