using PieceBook.Core.Events;
using PieceBook.DrawingEngine;
using PieceBook.DrawingEngine.Config;
using PieceBook.DrawingEngine.Input;
using PieceBook.Lessons.Model;
using PieceBook.Lessons.Runner;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PieceBook.Spike
{
    /// <summary>
    /// Phase-1 test harness: self-assembles a playable scene in two modes.
    ///   SprayOnly  — free drawing with production multicapa DrawingCanvas.
    ///   LessonFlow — loads lesson_tag_01.json and drives LessonRunner step-by-step.
    /// Lives in PieceBook.Spike (to be removed with ENG-08 when Phase-1 goes live).
    /// </summary>
    public sealed class Phase1TestHarness : MonoBehaviour
    {
        public enum TestMode { SprayOnly, LessonFlow }

        [Header("Test Mode")]
        public TestMode mode = TestMode.SprayOnly;

        [Header("Content — auto-created if null")]
        public CapDef[] caps;
        public PaintDef paint;
        public Color inkColor = new Color(0.05f, 0.05f, 0.06f);

        private DrawingCanvas _canvas;
        private LessonRunner  _runner;
        private LessonDef     _lesson;
        private int           _selectedCap;

        private GUIStyle _labelStyle;
        private GUIStyle _btnStyle;
        private string   _statusLine = "Play started";
        private string   _stepInfo   = "";

        private void Awake()
        {
            EnsureContent();
            var cam          = BuildCamera();
            var wallCollider = BuildWall();
            _canvas          = BuildCanvas();
            BuildPaintSurface(wallCollider);
            BuildInput(wallCollider, cam);

            if (mode == TestMode.LessonFlow)
                InitLessonFlow();
        }

        // ── Scene assembly ────────────────────────────────────────────────

        private void EnsureContent()
        {
            if (caps == null || caps.Length == 0) caps = SpikeDefaults.MakeAllCaps();
            if (paint == null)                    paint = SpikeDefaults.MakePaint();
        }

        private Camera BuildCamera()
        {
            var go = new GameObject("MainCamera"); go.tag = "MainCamera";
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, 0f, -2f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = 0.62f;
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = new Color(0.1f, 0.1f, 0.12f);
            return cam;
        }

        private Collider BuildWall()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = "Wall";
            wall.transform.SetParent(transform, false);
            var shader = SprayDemoBootstrap.GetUnlitShader();
            var mat    = new Material(shader);
#if UNITY_EDITOR
            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/_Project/Art/Textures/Walls/wall_brick.png");
            if (tex != null) mat.mainTexture = tex;
            else             mat.color = new Color(0.68f, 0.60f, 0.55f);
#else
            mat.color = new Color(0.68f, 0.60f, 0.55f);
#endif
            wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return wall.GetComponent<Collider>();
        }

        private DrawingCanvas BuildCanvas()
        {
            var go = new GameObject("DrawingCanvas");
            go.transform.SetParent(transform, false);
            return go.AddComponent<DrawingCanvas>();
        }

        private void BuildPaintSurface(Collider wall)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Quad);
            s.name = "PaintSurface";
            s.transform.SetParent(transform, false);
            s.transform.position = new Vector3(0f, 0f, -0.01f);
            Destroy(s.GetComponent<Collider>());
            var mat = new Material(SprayDemoBootstrap.GetTransparentUnlitShader());
            mat.mainTexture = _canvas.DisplayTexture;
            s.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private void BuildInput(Collider wall, Camera cam)
        {
            var go    = new GameObject("StrokeInput");
            go.transform.SetParent(transform, false);
            var input = go.AddComponent<StrokeInputController>();
            input.Configure(_canvas, cam, wall);
            input.StrokeConfigProvider = () =>
                new StrokeConfig(caps[_selectedCap], paint, inkColor, 1f);
            input.PointerBlocked = () =>
                EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        // ── Lesson flow ───────────────────────────────────────────────────

        private void InitLessonFlow()
        {
            var bus = new EventBus();
            _runner = new LessonRunner(bus);

            TextAsset lessonAsset = null;
#if UNITY_EDITOR
            lessonAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Project/Content/Lessons/lesson_tag_01.json");
#endif
            if (lessonAsset == null)
                lessonAsset = Resources.Load<TextAsset>("lesson_tag_01");

            if (lessonAsset == null)
            {
                _statusLine = "lesson_tag_01.json not found. Run 'TAG-School > Build MVP Content' first.";
                return;
            }

            _lesson = LessonJsonParser.Parse(lessonAsset.text);
            _runner.Completed   += crowns => { _statusLine = $"Done! Crowns earned: {crowns}"; _stepInfo = ""; };
            _runner.StepChanged += step   =>
            {
                _statusLine = $"Step {_runner.CurrentIndex + 1}/{_lesson.Steps.Count}  [{step.Kind}]";
                _stepInfo   = step.Kind == StepKind.Quiz
                    ? $"Quiz: {((QuizStep)step).Prompt}"
                    : step.Kind.ToString();
            };
            _runner.Start(_lesson);
        }

        // ── Immediate-mode HUD ────────────────────────────────────────────

        private void OnGUI()
        {
            if (_labelStyle == null) InitStyles();

            // Status bar
            GUI.Box(new Rect(0, 0, Screen.width, 34), "");
            GUI.Label(new Rect(8, 7, Screen.width - 16, 24), _statusLine, _labelStyle);

            // Bottom panel
            float py = Screen.height - 110f;
            GUI.Box(new Rect(0, py, Screen.width, 110), "");

            // Cap buttons
            float bw = 82f;
            for (int i = 0; i < caps.Length; i++)
                if (GUI.Button(new Rect(8 + i * (bw + 4), py + 6, bw, 28), caps[i].displayName, _btnStyle))
                    _selectedCap = i;

            // Undo / Clear
            if (GUI.Button(new Rect(8,  py + 40, 72, 28), "Undo",  _btnStyle)) ((IDrawingCanvas)_canvas).Undo();
            if (GUI.Button(new Rect(86, py + 40, 72, 28), "Clear", _btnStyle)) _canvas.ClearActiveLayer();

            // Lesson controls
            if (mode == TestMode.LessonFlow && _runner != null && _lesson != null)
            {
                GUI.Label(new Rect(8, py + 74, Screen.width - 16, 22), _stepInfo, _labelStyle);
                var cur = _runner.Current;
                if (cur == null) return;

                float rx = Screen.width - 240f;
                if (cur.Kind == StepKind.Showcase)
                {
                    if (GUI.Button(new Rect(rx, py + 40, 100, 28), "Next →", _btnStyle))
                        _runner.CompleteShowcase();
                }
                else if (cur.Kind == StepKind.Trace || cur.Kind == StepKind.Freeform)
                {
                    if (GUI.Button(new Rect(rx,       py + 40, 112, 28), "Submit 3★", _btnStyle)) _runner.SubmitDrawing(3);
                    if (GUI.Button(new Rect(rx + 118, py + 40, 112, 28), "Submit 1★", _btnStyle)) _runner.SubmitDrawing(1);
                }
                else if (cur.Kind == StepKind.Quiz)
                {
                    var quiz = (QuizStep)cur;
                    for (int i = 0; i < quiz.Options.Count; i++)
                        if (GUI.Button(new Rect(rx + i * 116, py + 40, 112, 28), quiz.Options[i], _btnStyle))
                            _runner.AnswerQuiz(i);
                }
            }
        }

        private void InitStyles()
        {
            _labelStyle = new GUIStyle(GUI.skin.label)
                { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 12 };
        }
    }
}
