using PieceBook.DrawingEngine;
using PieceBook.DrawingEngine.Config;
using PieceBook.DrawingEngine.Input;
using UnityEngine;

namespace PieceBook.Spike
{
    /// <summary>
    /// Phase-0 spike harness. Assembles the whole test scene in code (camera, textured
    /// wall, drawing canvas, input, placeholder HUD) so the project runs with zero manual
    /// wiring: open the scene and press Play, or just drop this component on an empty
    /// GameObject. This lives in a spike-only assembly and is meant to be deleted once the
    /// vertical slice replaces it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SprayDemoBootstrap : MonoBehaviour
    {
        [Header("Content (auto-created if empty)")]
        [Tooltip("The three caps. Left empty, defaults (skinny/soft/fat) are built at runtime.")]
        public CapDef[] caps;
        public PaintDef paint;
        public Color inkColor = new Color(0.05f, 0.05f, 0.06f);

        [Header("Scene")]
        [SerializeField] private int canvasSize = 2048;
        [SerializeField] private Color wallTint = new Color(0.72f, 0.70f, 0.66f);

        private DrawingCanvas _canvas;
        private Camera _cam;
        private StrokeInputController _input;
        private DemoHud _hud;
        private int _selected;

        public DrawingCanvas Canvas => _canvas;
        public int CapCount => caps != null ? caps.Length : 0;
        public int SelectedCap => _selected;

        private void Awake()
        {
            EnsureContent();
            BuildCamera();
            var wallCollider = BuildWall();
            BuildCanvas();
            AttachPaintSurface(wallCollider);
            BuildInput(wallCollider);
            BuildHud();
        }

        // ---------------------------------------------------------------- harness API (HUD calls these)

        public void SelectCap(int index)
        {
            if (caps == null || caps.Length == 0) return;
            _selected = Mathf.Clamp(index, 0, caps.Length - 1);
        }

        public string CapName(int index)
        {
            if (caps == null || index < 0 || index >= caps.Length || caps[index] == null) return "?";
            return caps[index].displayName;
        }

        public void SetNozzleDistance(float d) { if (_canvas != null) _canvas.NozzleDistance = Mathf.Clamp01(d); }
        public float NozzleDistance => _canvas != null ? _canvas.NozzleDistance : 0f;
        public void SetInk(Color c) => inkColor = c;
        public void ClearCanvas() { if (_canvas != null) _canvas.ClearActiveLayer(); }
        public void Undo() { if (_canvas != null) ((IDrawingCanvas)_canvas).Undo(); }
        public void Redo() { if (_canvas != null) ((IDrawingCanvas)_canvas).Redo(); }

        private StrokeConfig CurrentConfig()
        {
            var cap = caps[_selected];
            return new StrokeConfig(cap, paint, inkColor, 1f);
        }

        // ---------------------------------------------------------------- scene assembly

        private void EnsureContent()
        {
            if (caps == null || caps.Length == 0) caps = SpikeDefaults.MakeAllCaps();
            if (paint == null) paint = SpikeDefaults.MakePaint();
        }

        private void BuildCamera()
        {
            var camGo = new GameObject("SpikeCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.position = new Vector3(0f, 0f, -2f);
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = 0.62f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            _cam.nearClipPlane = 0.01f;
            _cam.farClipPlane = 10f;
            camGo.tag = "MainCamera";
        }

        private Collider BuildWall()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = "Wall";
            wall.transform.SetParent(transform, false);
            wall.transform.position = Vector3.zero;

            var tex = WallTextureFactory.Create(512, wallTint);
            var mat = new Material(GetUnlitShader()) { mainTexture = tex };
            wall.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // The wall quad's MeshCollider is what input rays hit for UV lookup.
            return wall.GetComponent<Collider>();
        }

        private void BuildCanvas()
        {
            var go = new GameObject("DrawingCanvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<DrawingCanvas>();
            // canvasSize is serialized on DrawingCanvas; the default (2048) matches §4.1.
        }

        /// <summary>Front quad that shows the transparent paint RT over the wall.</summary>
        private void AttachPaintSurface(Collider wallCollider)
        {
            var surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = "PaintSurface";
            surface.transform.SetParent(transform, false);
            surface.transform.position = new Vector3(0f, 0f, -0.01f); // just in front of the wall
            Destroy(surface.GetComponent<Collider>()); // input uses the wall collider

            var mat = new Material(GetTransparentUnlitShader());
            mat.mainTexture = _canvas.DisplayTexture;
            surface.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private void BuildInput(Collider wallCollider)
        {
            var go = new GameObject("StrokeInput");
            go.transform.SetParent(transform, false);
            _input = go.AddComponent<StrokeInputController>();
            _input.Configure(_canvas, _cam, wallCollider);
            _input.StrokeConfigProvider = CurrentConfig;
            _input.PointerBlocked = () =>
                UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }

        private void BuildHud()
        {
            var go = new GameObject("DemoHud");
            go.transform.SetParent(transform, false);
            _hud = go.AddComponent<DemoHud>();
            _hud.Init(this, _canvas);
        }

        // ---------------------------------------------------------------- shader resolution

        internal static Shader GetUnlitShader()
        {
            return Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Sprites/Default");
        }

        internal static Shader GetTransparentUnlitShader()
        {
            // Built-in Unlit/Transparent blends the RT's alpha over the wall and works under
            // URP too. Sprites/Default is the last-resort fallback.
            return Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default");
        }
    }
}
