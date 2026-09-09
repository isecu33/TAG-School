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

        [Header("Undo tiles (ENG-06, §4.1)")]
        [Tooltip("Grid resolution for tile-based undo dirty tracking (tiles per side).")]
        [SerializeField] private int undoTilesPerSide = 16;

        // --- runtime state ---
        private readonly List<RenderTexture> _layers = new List<RenderTexture>(8);
        private readonly List<bool> _visible = new List<bool>(8);
        private int _activeLayer;

        // Multilayer compositing (ENG-05, §4.1).
        [SerializeField] private Shader compositeShader;
        private Material _compositeMat;
        private RenderTexture _composite;
        private bool _compositeDirty = true;

        private Mesh _quad;
        private GpuStamper _stamper;
        private SprayEmitter _spray;
        private MarkerEmitter _marker;
        private IStampStrategy _emitter;   // active tool strategy (§7), swapped per stroke
        private DripSystem _drips;

        private readonly List<StrokeSample> _pending = new List<StrokeSample>(512);
        private readonly StrokeRecording _recording = new StrokeRecording();
        private RecordedStroke _current;

        private bool _drawing;
        private bool _endPending;
        private StrokeConfig _cfg;
        private float _strokeStartTime;

        // Tile dirty tracking — the foundation of tile-based undo (ENG-06, §4.1). The GPU
        // copy-on-write of dirty tiles builds on these indices; validated in-editor.
        private TileDirtyTracker _tiles;

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

        /// <summary>Tiles touched by the current/last stroke (ENG-06 diagnostics).</summary>
        public int DirtyTilesThisStroke => _tiles != null ? _tiles.DirtyTiles.Count : 0;

        /// <summary>Nozzle distance 0 (on the wall) .. 1 (far). The teaching dial (§4.1).</summary>
        [Range(0f, 1f)] public float NozzleDistance = 0.25f;

        /// <summary>
        /// Texture the wall renders: the full composited layer stack (ENG-05, §4.1). Refreshed
        /// lazily — only when a layer, its visibility, or the history changed since last read.
        /// </summary>
        public Texture DisplayTexture
        {
            get { if (_compositeDirty) RefreshComposite(); return _composite; }
        }

        private RenderTexture ActiveRT => _layers[_activeLayer];

        private void Awake()
        {
            _bus = EventBus.Default;
            _quad = QuadMeshFactory.Create();

            if (stampShader == null) stampShader = Shader.Find("PieceBook/SprayStamp");
            _stamper = new GpuStamper(_quad, stampShader);
            _spray = new SprayEmitter();
            _marker = new MarkerEmitter();
            _emitter = _spray;
            _drips = new DripSystem(_bus, dripAccumResolution, prewarm: 64);
            _tiles = new TileDirtyTracker(undoTilesPerSide);

            if (compositeShader == null) compositeShader = Shader.Find("PieceBook/LayerComposite");
            if (compositeShader != null) _compositeMat = new Material(compositeShader) { hideFlags = HideFlags.HideAndDontSave };

            // Layer 0 always exists.
            _layers.Add(NewRenderTexture());
            _visible.Add(true);
            _activeLayer = 0;
            _composite = NewRenderTexture();
            _compositeDirty = true;
        }

        // ---------------------------------------------------------------- IDrawingCanvas

        public LayerId AddLayer()
        {
            if (_layers.Count >= maxLayers) return new LayerId(_layers.Count - 1);
            _layers.Add(NewRenderTexture());
            _visible.Add(true);
            _compositeDirty = true;
            return new LayerId(_layers.Count - 1);
        }

        public void SetActiveLayer(LayerId id)
        {
            if (id.Value >= 0 && id.Value < _layers.Count) _activeLayer = id.Value;
        }

        /// <summary>Show/hide a layer in the composite (§4.1). Not in the §4.3 contract — an engine extra.</summary>
        public void SetLayerVisible(LayerId id, bool visible)
        {
            if (id.Value < 0 || id.Value >= _visible.Count) return;
            _visible[id.Value] = visible;
            _compositeDirty = true;
        }

        public bool IsLayerVisible(LayerId id) =>
            id.Value >= 0 && id.Value < _visible.Count && _visible[id.Value];

        public void BeginStroke(StrokeConfig cfg)
        {
            if (_drawing) FinalizeStroke();

            _cfg = cfg;
            PushUndoSnapshot();

            // §7 Strategy: pick the tool for this stroke over the shared GpuStamper.
            _emitter = cfg.Tool == ToolKind.Marker ? (IStampStrategy)_marker : _spray;
            _emitter.Begin(cfg);

            _current = new RecordedStroke(512);
            _current.Reset(cfg.Cap != null ? cfg.Cap.id : "?",
                           cfg.Paint != null ? cfg.Paint.id : "?",
                           cfg.Color);

            _tiles.Reset(); // start tracking which tiles this stroke touches (ENG-06)

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

            // Mark the tiles this sample's stamp footprint covers (ENG-06). Radius grows with the
            // nozzle distance dial (§4.1); a small pad covers overspray.
            float mark = 0.02f * (1f + NozzleDistance * 2.5f);
            _tiles.MarkDisc(pos, mark);
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
            _compositeDirty = true;
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
            _compositeDirty = true;
        }

        public Texture2D Flatten()
        {
            // ENG-05: composite every visible layer bottom→top, then read back (§4.1).
            RefreshComposite();
            var prev = RenderTexture.active;
            RenderTexture.active = _composite;
            var tex = new Texture2D(canvasSize, canvasSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, canvasSize, canvasSize), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }

        /// <summary>Recomposite the visible layer stack into <see cref="_composite"/> (ENG-05).</summary>
        private void RefreshComposite()
        {
            CompositeInto(_composite);
            _compositeDirty = false;
        }

        private void CompositeInto(RenderTexture dest)
        {
            ClearRenderTexture(dest);
            if (_compositeMat == null)
            {
                // No composite shader available: fall back to the active layer so the wall is never blank.
                Graphics.Blit(ActiveRT, dest);
                return;
            }
            for (int i = 0; i < _layers.Count; i++)
            {
                if (i < _visible.Count && !_visible[i]) continue;
                Graphics.Blit(_layers[i], dest, _compositeMat); // src-over, accumulates onto dest
            }
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
                    _compositeDirty = true;

                    if (ending) FinalizeStroke();
                }
            }
            else if (_drips.ActiveDrips > 0)
            {
                // Drips keep running after the stroke ends.
                _stamper.Begin(ActiveRT);
                _drips.Tick(dt, _stamper);
                _stamper.Flush();
                _compositeDirty = true;
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
            _compositeDirty = true;
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
            ReleaseRt(_composite);
            if (_compositeMat != null) Destroy(_compositeMat);
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
