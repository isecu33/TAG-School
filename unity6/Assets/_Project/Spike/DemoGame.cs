using System.Collections.Generic;
using PieceBook.Core.Events;
using PieceBook.Core.Services;
using PieceBook.DrawingEngine;
using PieceBook.DrawingEngine.Config;
using PieceBook.DrawingEngine.Input;
using PieceBook.Lessons.Evaluation;
using PieceBook.Lessons.Model;
using PieceBook.Lessons.Runner;
using PieceBook.MetaGame.Glossary;
using PieceBook.MetaGame.Store;
using PieceBook.UI.App;
using PieceBook.UI.Lesson;
using PieceBook.UI.Review;
using UnityEngine;

namespace PieceBook.Spike
{
    /// <summary>
    /// Playable demo of the full loop (Menu → Lesson → Paint → Review) on top of the PRODUCTION
    /// pieces: <see cref="GameContext"/> (DI + save + MetaGame), <see cref="LessonRunner"/>,
    /// <see cref="LessonPresenter"/>/<see cref="ReviewPresenter"/> (this class is their view),
    /// <see cref="DrawingStepController"/> and the real <see cref="DrawingCanvas"/>.
    ///
    /// The UI is immediate-mode (OnGUI) on purpose: it needs no prefabs, so the scene works the
    /// moment the content is generated. Built by <c>TAG-School ▸ Build Playable Demo</c>.
    /// Replace with the prefab views of the UI module when they exist (UI-02..04, ENG-08).
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)] // after GameContext.Awake when both live in the scene
    public sealed class DemoGame : MonoBehaviour, ILessonView, IReviewView
    {
        [Header("Contenido (lo asigna TAG-School ▸ Build Playable Demo)")]
        public LessonCatalog lessonCatalog;
        public AlphabetDef[] alphabets;
        public GlossaryCatalog glossaryCatalog;
        public CapDef[] caps;
        public PaintDef paint;

        [Header("Escena")]
        public Texture2D wallTexture;
        public Material wallMaterial;
        public Material guideMaterial;
        public Color inkColor = new Color(1f, 0.37f, 0.66f);

        private const float MinCoverage = 0.6f;
        private const float PanelFraction = 0.4f;
        private static readonly string[] ChapterNames = { "", "Tag", "Throw-up", "Color" };
        private static readonly Color[] Inks =
        {
            new Color(1f, 0.37f, 0.66f), new Color(0.08f, 0.08f, 0.08f), new Color(0.96f, 0.95f, 0.91f),
            new Color(0.35f, 0.83f, 0.94f), new Color(0.96f, 0.78f, 0.29f),
        };

        private enum View { Menu, Showcase, Paint, Quiz, Review }

        // production services
        private GameContext _ctx;
        private ISaveService _save;
        private StoreService _store;
        private LessonRunner _runner;
        private LessonPresenter _lessonPresenter;
        private ReviewPresenter _reviewPresenter;
        private DrawingStepController _drawing;
        private ITemplateProvider _templates;
        private Glossary _glossary;
        private List<LessonDef> _lessons = new List<LessonDef>();

        // scene
        private Camera _cam;
        private DrawingCanvas _canvas;
        private StrokeInputController _input;
        private LineRenderer _guide;
        private LineRenderer _startMarker;

        // state
        private View _view = View.Menu;
        private int _lessonIndex = -1;
        private int _cap;
        private bool _inputEnabled;
        private string _mentor = "";
        private string _prompt = "";
        private string _showcaseMedia = "";
        private bool _hasResult;
        private bool _resultFailed;
        private EvaluationResult _result;
        private float _coverage;
        private int _strokesAtStepStart;
        private string[] _quizIds = new string[0];
        private string _quizCorrect;
        private int _reviewCrowns;
        private readonly List<string> _reviewUnlocks = new List<string>();
        private int _coinsBefore;
        private Vector2 _menuScroll;
        private bool _openAll;

        // showcase ghost (the mentor paints the template with the real engine)
        private Vector2[] _ghostPts;
        private int _ghostIndex = -1;
        private bool _ghostCompletionPending; // the canvas finalizes EndStroke in its LateUpdate
        private bool _clearNextFrame;

        private GUIStyle _h1, _h2, _body, _small, _btn, _panel;

        // ------------------------------------------------------------------ boot

        private void Awake()
        {
            _ctx = GetComponent<GameContext>();
            if (_ctx == null) _ctx = gameObject.AddComponent<GameContext>(); // no audio catalog then
            _save = _ctx.Services.Resolve<ISaveService>();

            if (caps == null || caps.Length == 0) caps = SpikeDefaults.MakeAllCaps();
            if (paint == null) paint = SpikeDefaults.MakePaint();

            var storeItems = new List<StoreItem>();
            foreach (var c in caps) if (c != null && c.unlockCost > 0) storeItems.Add(new StoreItem(c.id, c.unlockCost));
            _store = new StoreService(_save, _ctx.Bus, storeItems);

            if (lessonCatalog != null) _lessons = lessonCatalog.BuildAll();
            _templates = new AlphabetTemplateProvider(alphabets);
            _glossary = glossaryCatalog != null ? glossaryCatalog.Build() : new Glossary(null);

            BuildScene();

            _runner = new LessonRunner(_ctx.Bus);
            _lessonPresenter = new LessonPresenter(_runner, this);
            _lessonPresenter.DrawingStepRequested += OnDrawingStep;
            _reviewPresenter = new ReviewPresenter(this, _ctx.Services.Resolve<IHaptics>(), _ctx.Bus);
            _drawing = new DrawingStepController(_canvas, _runner, _templates);

            _ctx.Bus.Subscribe<StrokeStarted>(OnStrokeStarted);
            _ctx.Bus.Subscribe<StrokeCompleted>(OnStrokeCompleted);

            _mentor = _lessons.Count == 0
                ? "No hay lecciones: ejecuta TAG-School ▸ Build Playable Demo."
                : "Soy Meka. Empieza por el capítulo 1: tu primer tag.";
        }

        private void OnDestroy()
        {
            if (_ctx == null || _ctx.Bus == null) return;
            _ctx.Bus.Unsubscribe<StrokeStarted>(OnStrokeStarted);
            _ctx.Bus.Unsubscribe<StrokeCompleted>(OnStrokeCompleted);
            _lessonPresenter?.Dispose();
            _reviewPresenter?.Dispose();
        }

        private void BuildScene()
        {
            var camGo = new GameObject("DemoCamera");
            camGo.transform.SetParent(transform, false);
            camGo.tag = "MainCamera";
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.11f, 0.10f, 0.094f);
            _cam.nearClipPlane = 0.01f;
            _cam.farClipPlane = 10f;
            camGo.transform.position = new Vector3(0f, 0f, -2f);

            var wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = "Wall";
            wall.transform.SetParent(transform, false);
            var wallMat = wallMaterial != null
                ? new Material(wallMaterial)
                : new Material(SprayDemoBootstrap.GetUnlitShader());
            wallMat.mainTexture = wallTexture != null ? wallTexture : WallTextureFactory.Create(512, new Color(0.72f, 0.70f, 0.66f));
            wall.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
            var wallCollider = wall.GetComponent<Collider>();

            var canvasGo = new GameObject("DrawingCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<DrawingCanvas>();

            var surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = "PaintSurface";
            surface.transform.SetParent(transform, false);
            surface.transform.position = new Vector3(0f, 0f, -0.01f);
            Destroy(surface.GetComponent<Collider>());
            var surfMat = new Material(SprayDemoBootstrap.GetTransparentUnlitShader()) { mainTexture = _canvas.DisplayTexture };
            surface.GetComponent<MeshRenderer>().sharedMaterial = surfMat;

            var inputGo = new GameObject("StrokeInput");
            inputGo.transform.SetParent(transform, false);
            _input = inputGo.AddComponent<StrokeInputController>();
            _input.Configure(_canvas, _cam, wallCollider);
            _input.StrokeConfigProvider = () => new StrokeConfig(caps[_cap], paint, inkColor, 1f);
            _input.PointerBlocked = () => !_inputEnabled;

            _guide = NewLine("TraceGuide", new Color(0.35f, 0.83f, 0.94f, 0.85f), 0.008f);
            _startMarker = NewLine("TraceStart", new Color(0.56f, 0.88f, 0.54f, 1f), 0.012f);
            _startMarker.loop = true;
        }

        private LineRenderer NewLine(string name, Color color, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, 0f, -0.02f);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.material = guideMaterial != null ? new Material(guideMaterial) : new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = color;
            lr.startWidth = lr.endWidth = width;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 4;
            lr.positionCount = 0;
            return lr;
        }

        // ------------------------------------------------------------------ frame

        private void Update()
        {
            if (_clearNextFrame)
            {
                // A stroke ended last frame (the mentor's ghost) has been flushed by now.
                _clearNextFrame = false;
                _canvas.ClearActiveLayer();
            }
            if (_ghostIndex >= 0) StepGhost();
        }

        private void LateUpdate()
        {
            // Frame the wall left of the panel (landscape) or above it (portrait).
            float aspect = _cam.aspect;
            if (aspect >= 1f)
            {
                _cam.orthographicSize = 0.62f;
                float visibleW = 2f * _cam.orthographicSize * aspect;
                float wallArea = visibleW * (1f - PanelFraction);
                if (wallArea < 1.1f) _cam.orthographicSize = 0.62f * 1.1f / wallArea;
                visibleW = 2f * _cam.orthographicSize * aspect;
                _cam.transform.position = new Vector3(visibleW * PanelFraction * 0.5f, 0f, -2f);
            }
            else
            {
                _cam.orthographicSize = 0.56f / aspect;
                float visibleH = 2f * _cam.orthographicSize;
                _cam.transform.position = new Vector3(0f, -visibleH * PanelFraction * 0.5f, -2f);
            }
        }

        // ------------------------------------------------------------------ lesson flow

        private void StartLesson(int index)
        {
            _lessonIndex = index;
            _coinsBefore = _save.Current.Coins;
            _reviewUnlocks.Clear();
            _cap = _lessons[index].Chapter == 1 ? 0 : BestOwnedCap();
            if (_ctx.Nav.Current != AppState.Menu && _ctx.Nav.CanGoTo(AppState.Menu)) _ctx.Nav.GoTo(AppState.Menu);
            _ctx.Nav.GoTo(AppState.Lesson);
            _runner.Start(_lessons[index]); // → LessonPresenter renders step 0 through this view
        }

        private void BackToMenu()
        {
            StopGhost();
            SetGuide(null);
            _inputEnabled = false;
            _hasResult = false;
            if (_ctx.Nav.Current == AppState.Paint) _ctx.Nav.GoTo(AppState.Lesson);
            if (_ctx.Nav.Current != AppState.Menu) _ctx.Nav.GoTo(AppState.Menu);
            _view = View.Menu;
        }

        private TraceTemplate LessonTemplate(out float tolerance)
        {
            tolerance = 0.2f;
            if (_runner.Lesson == null) return null;
            foreach (var s in _runner.Lesson.Steps)
                if (s is TraceStep t && _templates.TryGet(t.Template, out var tpl)) { tolerance = t.Tolerance; return tpl; }
            return null;
        }

        // ILessonView ---------------------------------------------------------------------------

        public void ShowShowcase(string media, float maxSeconds)
        {
            if (_ctx.Nav.Current == AppState.Paint) _ctx.Nav.GoTo(AppState.Lesson);
            _view = View.Showcase;
            _inputEnabled = false;
            _showcaseMedia = media;
            var step = _runner.Current as ShowcaseStep;
            _mentor = step != null && !string.IsNullOrEmpty(step.Caption) ? step.Caption : "Mira cómo se hace y luego te toca.";
            PlayGhost();
        }

        public void ShowDrawingStep(string prompt)
        {
            _prompt = prompt;
        }

        public void ShowQuiz(string[] glossaryRefs)
        {
            if (_ctx.Nav.Current == AppState.Paint) _ctx.Nav.GoTo(AppState.Lesson);
            _view = View.Quiz;
            _inputEnabled = false;
            SetGuide(null);
            var quiz = (QuizStep)_runner.Current;
            // Options = the step's glossary refs in order (AnswerQuiz compares by index) + one
            // distractor at the end, which is never the correct index.
            var ids = new List<string>(glossaryRefs);
            foreach (var e in _glossary.Entries)
                if (!ids.Contains(e.id) && System.Array.IndexOf(_runner.Lesson.GlossaryRefs, e.id) < 0) { ids.Add(e.id); break; }
            _quizIds = ids.ToArray();
            _quizCorrect = glossaryRefs.Length > quiz.CorrectIndex ? glossaryRefs[quiz.CorrectIndex] : null;
            _mentor = "Última. Un writer sabe de qué habla.";
        }

        public void Hide() { }

        private void OnDrawingStep(LessonStep step)
        {
            StopGhost();
            if (_ctx.Nav.Current != AppState.Paint) _ctx.Nav.GoTo(AppState.Paint);
            _view = View.Paint;
            _canvas.ClearActiveLayer();
            _clearNextFrame = _ghostCompletionPending;
            _hasResult = false;
            _inputEnabled = true;
            _strokesAtStepStart = _canvas.GetRecording().StrokeCount;
            if (step is TraceStep trace && _templates.TryGet(trace.Template, out var tpl))
            {
                SetGuide(tpl.Points);
                _mentor = "Sigue la guía cian de una sola pasada, empezando en el punto verde.";
                _prompt = "Traza la letra " + trace.Template.Substring(trace.Template.Length - 1);
            }
            else
            {
                SetGuide(null);
                _mentor = "Sin guía. Hazlo de memoria y pulsa Evaluar.";
            }
        }

        private void OnStrokeStarted(StrokeStarted e)
        {
            if (_view == View.Paint && _runner.Current is TraceStep && _hasResult)
            {
                _canvas.ClearActiveLayer();
                _hasResult = false;
            }
        }

        private void OnStrokeCompleted(StrokeCompleted e)
        {
            if (_ghostCompletionPending)
            {
                _ghostCompletionPending = false; // the mentor's stroke, not the player's
                return;
            }
            if (_view != View.Paint || _ghostIndex >= 0 || !(_runner.Current is TraceStep)) return;
            if (e.SampleCount < 3) return;
            _coverage = _drawing.CoverageOfCurrent();
            _hasResult = true;
            _resultFailed = _coverage < MinCoverage;
            if (_resultFailed)
            {
                _mentor = "Te has dejado parte de la letra. Recórrela entera, sin levantar.";
                return;
            }
            _result = _drawing.EvaluateCurrent();
            _mentor = Tip(_result);
        }

        private void EvaluateFreeform()
        {
            _result = _drawing.EvaluateCurrent();
            _coverage = 1f;
            _resultFailed = false;
            _hasResult = _result.Crowns > 0;
            _mentor = _hasResult ? Tip(_result) : "Dibuja algo antes de evaluar.";
        }

        private void AcceptResult()
        {
            _hasResult = false;
            _runner.SubmitDrawing(_result.Crowns); // → next step (presenter) or LessonPassed → review
        }

        private static string Tip(EvaluationResult r)
        {
            if (r.Crowns == 3) return "Limpio. Así se pinta.";
            TraceMetric worst = TraceMetric.Smoothness;
            float v = 2f;
            foreach (var kv in r.Scores) if (kv.Value < v) { v = kv.Value; worst = kv.Key; }
            switch (worst)
            {
                case TraceMetric.Precision: return "Te sales de la guía. Mira un poco por delante del bote.";
                case TraceMetric.SpeedConsistency: return "Cambias de velocidad. Decide el recorrido y mantén el ritmo.";
                case TraceMetric.LineWeight: return "La presión varía. Mantén la misma distancia a la pared.";
                default: return "El trazo tiembla. Mueve el brazo entero, no solo la muñeca.";
            }
        }

        // IReviewView ---------------------------------------------------------------------------

        public void ShowResult(int crowns, IReadOnlyList<string> unlockedItems)
        {
            _reviewCrowns = crowns;
            _reviewUnlocks.Clear();
            _reviewUnlocks.AddRange(unlockedItems);
            if (_ctx.Nav.Current == AppState.Paint) _ctx.Nav.GoTo(AppState.Lesson);
            _ctx.Nav.GoTo(AppState.Review);
            _view = View.Review;
            _inputEnabled = false;
            SetGuide(null);
        }

        // ------------------------------------------------------------------ showcase ghost

        private void PlayGhost()
        {
            StopGhost();
            _canvas.ClearActiveLayer();
            var tpl = LessonTemplate(out _);
            SetGuide(tpl?.Points);
            if (tpl == null) return;
            _ghostPts = Resample(tpl.Points, 0.004f);
            _ghostIndex = 0;
            _canvas.BeginStroke(new StrokeConfig(caps[_cap], paint, inkColor, 1f));
        }

        private void StepGhost()
        {
            for (int k = 0; k < 3 && _ghostIndex < _ghostPts.Length; k++, _ghostIndex++)
                _canvas.UpdateStroke(_ghostPts[_ghostIndex], 1f, 0f);
            if (_ghostIndex >= _ghostPts.Length) StopGhost();
        }

        private void StopGhost()
        {
            if (_ghostIndex < 0) return;
            _canvas.EndStroke();
            _ghostIndex = -1;
            _ghostCompletionPending = true;
        }

        private static Vector2[] Resample(Vector2[] pts, float step)
        {
            var list = new List<Vector2> { pts[0] };
            for (int i = 0; i < pts.Length - 1; i++)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(pts[i], pts[i + 1]) / step));
                for (int k = 1; k <= n; k++) list.Add(Vector2.Lerp(pts[i], pts[i + 1], k / (float)n));
            }
            return list.ToArray();
        }

        private void SetGuide(Vector2[] pts)
        {
            if (pts == null || pts.Length == 0)
            {
                _guide.positionCount = 0;
                _startMarker.positionCount = 0;
                return;
            }
            _guide.positionCount = pts.Length;
            for (int i = 0; i < pts.Length; i++) _guide.SetPosition(i, new Vector3(pts[i].x - 0.5f, pts[i].y - 0.5f, 0f));

            const int seg = 16;
            _startMarker.positionCount = seg;
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                _startMarker.SetPosition(i, new Vector3(pts[0].x - 0.5f + Mathf.Cos(a) * 0.015f, pts[0].y - 0.5f + Mathf.Sin(a) * 0.015f, 0f));
            }
        }

        // ------------------------------------------------------------------ helpers

        private bool Owns(CapDef c) => c != null && (c.unlockCost == 0 || _save.Current.IsUnlocked(c.id));

        private int BestOwnedCap()
        {
            for (int i = caps.Length - 1; i >= 0; i--)
                if (Owns(caps[i]) && caps[i].coneAngle <= 25f) return i;
            return 0;
        }

        private bool LessonOpen(int i) =>
            _openAll || i == 0 || (_save.Current.Crowns.TryGetValue(_lessons[i - 1].Id, out var c) && c > 0);

        private int BestCrowns(string id) => _save.Current.Crowns.TryGetValue(id, out var c) ? c : 0;

        private string Term(string id) => _glossary.TryGet(id, out var e) ? e.term : id;

        private static string Crowns(int n) => n <= 0 ? "sin coronas" : $"{n}/3 coronas";

        private string CapName(string id)
        {
            foreach (var c in caps) if (c != null && c.id == id) return c.displayName;
            return id;
        }

        // ------------------------------------------------------------------ IMGUI

        private void InitStyles()
        {
            var paper = new Color(0.94f, 0.91f, 0.85f);
            _h1 = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, wordWrap = true };
            _h1.normal.textColor = new Color(1f, 0.37f, 0.66f);
            _h2 = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, wordWrap = true };
            _h2.normal.textColor = paper;
            _body = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            _body.normal.textColor = paper;
            _small = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            _small.normal.textColor = new Color(0.66f, 0.62f, 0.56f);
            _btn = new GUIStyle(GUI.skin.button) { fontSize = 16, wordWrap = true, padding = new RectOffset(10, 10, 8, 8) };
            _panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(16, 16, 14, 14) };
        }

        private void OnGUI()
        {
            if (_h1 == null) InitStyles();
            float scale = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 800f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale, h = Screen.height / scale;
            var panel = Screen.width >= Screen.height
                ? new Rect(w * (1f - PanelFraction), 0f, w * PanelFraction, h)
                : new Rect(0f, h * (1f - PanelFraction), w, h * PanelFraction);

            GUILayout.BeginArea(panel, _panel);
            _menuScroll = GUILayout.BeginScrollView(_menuScroll);
            switch (_view)
            {
                case View.Menu: DrawMenu(); break;
                case View.Showcase: DrawLessonHeader(); DrawShowcase(); break;
                case View.Paint: DrawLessonHeader(); DrawPaint(); break;
                case View.Quiz: DrawLessonHeader(); DrawQuiz(); break;
                case View.Review: DrawReview(); break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawMenu()
        {
            var p = _save.Current;
            GUILayout.Label("TAG School", _h1);
            GUILayout.Label($"{p.TotalCrowns} coronas · {p.Coins} monedas · racha {p.Streak}", _small);
            GUILayout.Label(_mentor, _body);
            _openAll = GUILayout.Toggle(_openAll, " Abrir todas las lecciones (revisión)");

            int chapter = -1;
            for (int i = 0; i < _lessons.Count; i++)
            {
                var l = _lessons[i];
                if (l.Chapter != chapter)
                {
                    chapter = l.Chapter;
                    GUILayout.Space(10);
                    GUILayout.Label($"Capítulo {chapter} · {(chapter < ChapterNames.Length ? ChapterNames[chapter] : "")}", _h2);
                }
                bool open = LessonOpen(i);
                GUI.enabled = open;
                string label = $"{l.Title}\n{(open ? Crowns(BestCrowns(l.Id)) : "bloqueada")}";
                if (GUILayout.Button(label, _btn)) StartLesson(i);
                GUI.enabled = true;
            }

            GUILayout.Space(12);
            GUILayout.Label("Tienda de caps", _h2);
            foreach (var item in _store.View())
            {
                GUI.enabled = item.Affordable;
                string label = item.Owned ? $"{CapName(item.Id)} · en tu mochila" : $"Comprar {CapName(item.Id)} · {item.Cost} monedas";
                if (GUILayout.Button(label, _btn) && !item.Owned) _store.Buy(item.Id);
                GUI.enabled = true;
            }
        }

        private void DrawLessonHeader()
        {
            var l = _runner.Lesson;
            GUILayout.Label($"Capítulo {l.Chapter} · paso {_runner.CurrentIndex + 1}/{l.StepCount}", _small);
            GUILayout.Label(l.Title, _h2);
            GUILayout.Space(6);
            GUILayout.Label("Meka: " + _mentor, _body);
            GUILayout.Space(8);
        }

        private void DrawShowcase()
        {
            GUILayout.Label($"Demostración (sustituye al vídeo {_showcaseMedia}, que aún no existe).", _small);
            if (GUILayout.Button("Ver otra vez", _btn)) PlayGhost();
            if (GUILayout.Button("Entendido, me toca", _btn))
            {
                StopGhost();
                _lessonPresenter.CompleteShowcase();
            }
            DrawExit();
        }

        private void DrawPaint()
        {
            GUILayout.Label(_prompt, _h2);
            DrawTools();

            bool isFree = _runner.Current is FreeformStep;
            if (isFree && !_hasResult)
            {
                GUI.enabled = _canvas.GetRecording().StrokeCount > _strokesAtStepStart && !_canvas.IsDrawing;
                if (GUILayout.Button("Evaluar", _btn)) EvaluateFreeform();
                GUI.enabled = true;
            }

            if (_hasResult)
            {
                GUILayout.Space(8);
                if (_resultFailed)
                {
                    GUILayout.Label($"Cobertura {Mathf.RoundToInt(_coverage * 100)}%: necesitas al menos {Mathf.RoundToInt(MinCoverage * 100)}%. Vuelve a pintar encima.", _body);
                }
                else
                {
                    GUILayout.Label($"Resultado: {Crowns(_result.Crowns)}", _h2);
                    foreach (var kv in _result.Scores)
                        GUILayout.Label($"{kv.Key}: {Mathf.RoundToInt(kv.Value * 100)}%", _body);
                    if (!isFree) GUILayout.Label($"Cobertura: {Mathf.RoundToInt(_coverage * 100)}%", _small);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Repetir", _btn))
                    {
                        _canvas.ClearActiveLayer();
                        _hasResult = false;
                        _strokesAtStepStart = _canvas.GetRecording().StrokeCount;
                    }
                    if (GUILayout.Button("Siguiente", _btn)) AcceptResult();
                    GUILayout.EndHorizontal();
                }
            }
            DrawExit();
        }

        private void DrawTools()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < caps.Length; i++)
            {
                GUI.enabled = Owns(caps[i]);
                string label = (i == _cap ? "> " : "") + caps[i].displayName;
                if (GUILayout.Button(label, _btn)) _cap = i;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            for (int i = 0; i < Inks.Length; i++)
            {
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = Inks[i];
                if (GUILayout.Button(inkColor == Inks[i] ? "X" : " ", _btn, GUILayout.Width(40))) inkColor = Inks[i];
                GUI.backgroundColor = prev;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"Distancia de boquilla: {Mathf.RoundToInt(_canvas.NozzleDistance * 100)}", _small);
            _canvas.NozzleDistance = GUILayout.HorizontalSlider(_canvas.NozzleDistance, 0f, 1f);
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Deshacer", _btn)) ((IDrawingCanvas)_canvas).Undo();
            if (GUILayout.Button("Limpiar", _btn)) { _canvas.ClearActiveLayer(); _hasResult = false; }
            GUILayout.EndHorizontal();
        }

        private void DrawQuiz()
        {
            GUILayout.Label("¿Qué término es este?", _small);
            if (_quizCorrect != null && _glossary.TryGet(_quizCorrect, out var entry))
                GUILayout.Label($"“{entry.definition}”", _body);
            for (int i = 0; i < _quizIds.Length; i++)
            {
                if (!GUILayout.Button(Term(_quizIds[i]), _btn)) continue;
                if (!_lessonPresenter.AnswerQuiz(i))
                    _mentor = _glossary.TryGet(_quizIds[i], out var wrong)
                        ? $"No. “{wrong.term}” es otra cosa: {wrong.definition}"
                        : "No. Prueba otra.";
            }
            DrawExit();
        }

        private void DrawReview()
        {
            string[] heads = { "", "Aprobado. Sigue dándole", "Buen trazo", "Pieza de king" };
            GUILayout.Label(heads[Mathf.Clamp(_reviewCrowns, 1, 3)], _h1);
            GUILayout.Label(Crowns(_reviewCrowns), _h2);
            int coins = _save.Current.Coins - _coinsBefore;
            GUILayout.Label(coins > 0 ? $"+{coins} monedas" : "Sin monedas nuevas: no superas tu récord.", _body);
            foreach (var u in _reviewUnlocks) GUILayout.Label($"Desbloqueado: {CapName(u)}", _h2);
            GUILayout.Space(10);
            if (_lessonIndex + 1 < _lessons.Count && GUILayout.Button("Siguiente lección", _btn)) StartLesson(_lessonIndex + 1);
            if (GUILayout.Button("Repetir lección", _btn)) StartLesson(_lessonIndex);
            if (GUILayout.Button("Volver al mapa", _btn)) BackToMenu();
        }

        private void DrawExit()
        {
            GUILayout.Space(12);
            if (GUILayout.Button("Salir al mapa", _btn)) BackToMenu();
        }
    }
}
