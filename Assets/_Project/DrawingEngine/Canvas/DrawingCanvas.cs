using System.Collections.Generic;
using PieceBook.Core.Events;
using PieceBook.DrawingEngine.Diagnostics;
using PieceBook.DrawingEngine.Drips;
using PieceBook.DrawingEngine.Stamping;
using UnityEngine;

namespace PieceBook.DrawingEngine
{
    /// <summary>
    /// The canvas (ARQUITECTURA §4). A stack of RenderTexture layers stamped on the GPU.
    /// Implements the exact §4.3 contract and stays a PURE engine object: it publishes
    /// events on the Core EventBus but knows nothing about UI or lessons (§3).
    ///
    /// Input capture (UpdateStroke) and GPU submission (LateUpdate) are separated so all
    /// stamps for a frame go out in one batched pass — that is what the LatencyProbe times.
    /// </summary>
    public sealed class DrawingCanvas : MonoBehaviour, IDrawingCanvas
    {
        [Header("Canvas")]
        [Tooltip("RenderTexture edge in pixels. 2048 on mid-range, 4096 on iPad Pro (§4.1).")]
        [SerializeField] private int canvasSize = 2048;
        [Tooltip("Max simultaneous layers (§4.1: 4 on mobile, 8 on iPad).")]
        [SerializeField] private int maxLayers = 4;
        [SerializeField] private Color clearColor = new Color(0f, 0f, 0f, 0f);

        [Header("Stamping")]
        [Tooltip("Instanced spray shader. Auto-resolves to 'PieceBook/SprayStamp' if left empty.")]
        [SerializeField] private Shader stampShader;

        [Header("Undo (§4.1 ring buffer)")]
        [Tooltip("Snapshots kept for undo. Each is canvasSize² RGBA32 — keep modest on mobile. " +
                 "Production replaces this with tile-compressed snapshots.")]
        [SerializeField] private int undoSteps = 8;

        [Header("Drips (ENG-04)")]
        [SerializeField] private int dripAccumResolution = 96;

        // --- runtime state ---
        private readonly List<RenderTexture> _layers = new List<RenderTexture>(8);
        private int _activeLayer;

        private Mesh _quad;
        private GpuStamper _stamper;
        private SprayEmitter _emitter;
        private DripSystem _drips;

        private readonly List<StrokeSample> _pending = new List<StrokeSample>(512);
        private readonly StrokeRecording _recording = new StrokeRecording();
        private RecordedStroke _current;

        private bool _drawing;
        private bool _endPending;
        private StrokeConfig _cfg;
        private float _strokeStartTime;

        // undo / redo snapshot pools
        private readonly List<RenderTexture> _undo = new List<RenderTexture>(16);
        private readonly List<RenderTexture> _redo = new List<RenderTexture>(16);
        private readonly Stack<RenderTexture> _snapPool = new Stack<RenderTexture>(16);

        private EventBus _bus;
        private readonly FrameStats _frameStats = new FrameStats(120);
        private readonly LatencyProbe _latency = new LatencyProbe(120);

        // --- public diagnostics (read by the spike HUD; not part of the §4.3 contract) ---
        public FrameStats FrameStats => _frameStats;
        public LatencyProbe Latency => _latency;
        public int ActiveDrips => _drips != null ? _drips.ActiveDrips : 0;
        public int InstancesLastFrame => _stamper != null ? _stamper.InstancesThisFrame : 0;
        public int LayerCount => _layers.Count;
        public bool IsDrawing => _drawing;

        /// <summary>Nozzle distance 0 (on the wall) .. 1 (far). The teaching dial (§4.1).</summary>
        [Range(0f, 1f)] public float NozzleDistance = 0.25f;

        /// <summary>Texture the wall renders. Spike shows the active layer directly.</summary>
        public Texture DisplayTexture => ActiveRT;

        private RenderTexture ActiveRT => _layers[_activeLayer];

        private void Awake()
        {
            _bus = EventBus.Default;
            _quad = QuadMeshFactory.Create();

            if (stampShader == null) stampShader = Shader.Find("PieceBook/SprayStamp");
            _stamper = new GpuStamper(_quad, stampShader);
            _emitter = new SprayEmitter();
            _drips = new DripSystem(_bus, dripAccumResolution, prewarm: 64);

            // Layer 0 always exists.
            _layers.Add(NewRenderTexture());
            _activeLayer = 0;
        }

        // ---------------------------------------------------------------- IDrawingCanvas

        public LayerId AddLayer()
        {
            if (_layers.Count >= maxLayers) return new LayerId(_layers.Count - 1);
            _layers.Add(NewRenderTexture());
            return new LayerId(_layers.Count - 1);
        }

        public void SetActiveLayer(LayerId id)
        {
            if (id.Value >= 0 && id.Value < _layers.Count) _activeLayer = id.Value;
        }

        public void BeginStroke(StrokeConfig cfg)
        {
            if (_drawing) FinalizeStroke();

            _cfg = cfg;
            PushUndoSnapshot();
            _emitter.Begin(cfg);

            _current = new RecordedStroke(512);
            _current.Reset(cfg.Cap != null ? cfg.Cap.id : "?",
                           cfg.Paint != null ? cfg.Paint.id : "?",
                           cfg.Color);

            _drawing = true;
            _endPending = false;
            _strokeStartTime = Time.realtimeSinceStartup;

            _bus.Publish(new StrokeStarted(_activeLayer));
        }

        public void UpdateStroke(Vector2 pos, float pressure, float tilt)
        {
            if (!_drawing || _endPending) return;

            float now = Time.realtimeSinceStartup;
            _latency.MarkInput(now);

            var s = new StrokeSample
            {
                Pos = pos,
                Pressure = Mathf.Clamp01(pressure),
                Tilt = tilt,
                Time = now - _strokeStartTime
            };
            _pending.Add(s);
            _current.Samples.Add(s);
        }

        public void EndStroke()
        {
            if (!_drawing) return;
            _endPending = true; // LateUpdate flushes the tail, then finalizes
        }

        public void Undo()
        {
            if (_undo.Count == 0) return;
            var redoSnap = RentSnapshot();
            Graphics.Blit(ActiveRT, redoSnap);
            _redo.Add(redoSnap);

            var snap = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);
            Graphics.Blit(snap, ActiveRT);
            ReturnSnapshot(snap);
        }

        public void Redo()
        {
            if (_redo.Count == 0) return;
            var undoSnap = RentSnapshot();
            Graphics.Blit(ActiveRT, undoSnap);
            _undo.Add(undoSnap);

            var snap = _redo[_redo.Count - 1];
            _redo.RemoveAt(_redo.Count - 1);
            Graphics.Blit(snap, ActiveRT);
            ReturnSnapshot(snap);
        }

        public Texture2D Flatten()
        {
            // Spike composites a single layer. Multi-layer flatten is future work (§4.1).
            var rt = ActiveRT;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(canvasSize, canvasSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, canvasSize, canvasSize), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }

        public StrokeRecording GetRecording() => _recording;

        // ---------------------------------------------------------------- frame loop

        private void LateUpdate()
        {
            _frameStats.Sample(Time.unscaledDeltaTime);
            float dt = Time.unscaledDeltaTime;

            if (_drawing)
            {
                bool ending = _endPending;
                bool work = _pending.Count > 0 || ending || _drips.ActiveDrips > 0;
                if (work)
                {
                    _stamper.Begin(ActiveRT);

                    for (int i = 0; i < _pending.Count; i++)
                    {
                        var s = _pending[i];
                        _emitter.Push(s.Pos, s.Pressure, NozzleDistance, _stamper, _drips);
                    }
                    if (ending) _emitter.End(NozzleDistance, _stamper, _drips);

                    _drips.Tick(dt, _stamper);
                    _stamper.Flush();

                    if (_pending.Count > 0) _latency.MarkSubmitted(Time.realtimeSinceStartup);
                    _pending.Clear();

                    if (ending) FinalizeStroke();
                }
            }
            else if (_drips.ActiveDrips > 0)
            {
                // Drips keep running after the stroke ends.
                _stamper.Begin(ActiveRT);
                _drips.Tick(dt, _stamper);
                _stamper.Flush();
            }
        }

        private void FinalizeStroke()
        {
            if (_current != null)
            {
                _recording.Strokes.Add(_current);
                _bus.Publish(new StrokeCompleted(
                    _activeLayer, _current.Samples.Count,
                    Time.realtimeSinceStartup - _strokeStartTime));
            }
            _current = null;
            _drawing = false;
            _endPending = false;
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Clears the active layer and resets drips (spike convenience, not in §4.3).</summary>
        public void ClearActiveLayer()
        {
            ClearRenderTexture(ActiveRT);
            _drips.Reset();
        }

        private RenderTexture NewRenderTexture()
        {
            var rt = new RenderTexture(canvasSize, canvasSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                name = $"PieceBook_Canvas_{_layers.Count}",
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            rt.Create();
            ClearRenderTexture(rt);
            return rt;
        }

        private void ClearRenderTexture(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, clearColor);
            RenderTexture.active = prev;
        }

        private RenderTexture RentSnapshot()
        {
            if (_snapPool.Count > 0) return _snapPool.Pop();
            var rt = new RenderTexture(canvasSize, canvasSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                name = "PieceBook_UndoSnap",
                useMipMap = false,
                autoGenerateMips = false
            };
            rt.Create();
            return rt;
        }

        private void ReturnSnapshot(RenderTexture rt) => _snapPool.Push(rt);

        private void PushUndoSnapshot()
        {
            var snap = RentSnapshot();
            Graphics.Blit(ActiveRT, snap);
            _undo.Add(snap);
            if (_undo.Count > undoSteps)
            {
                ReturnSnapshot(_undo[0]);
                _undo.RemoveAt(0);
            }
            // A new stroke invalidates the redo history.
            for (int i = 0; i < _redo.Count; i++) ReturnSnapshot(_redo[i]);
            _redo.Clear();
        }

        private void OnDestroy()
        {
            _stamper?.Dispose();

            for (int i = 0; i < _layers.Count; i++) ReleaseRt(_layers[i]);
            for (int i = 0; i < _undo.Count; i++) ReleaseRt(_undo[i]);
            for (int i = 0; i < _redo.Count; i++) ReleaseRt(_redo[i]);
            while (_snapPool.Count > 0) ReleaseRt(_snapPool.Pop());

            if (_quad != null) Destroy(_quad);
        }

        private static void ReleaseRt(RenderTexture rt)
        {
            if (rt == null) return;
            rt.Release();
            Destroy(rt);
        }
    }
}
