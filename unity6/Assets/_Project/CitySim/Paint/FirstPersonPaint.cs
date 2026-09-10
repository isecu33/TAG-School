using PieceBook.CitySim.Loop;
using PieceBook.CitySim.Player;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PieceBook.CitySim.Paint
{
    /// <summary>
    /// First-person painting step (GD-02 §2.3). A 400 ms cinematic zoom into a full-screen
    /// placeholder wall (its own RenderTexture — NOT the Drawing Engine), the window ring on
    /// top, and the street "glance": swipe-down / hold S gives a 1 s peek at the street with
    /// the paint frozen — "congelar el trazo para mirar es LA microdecisión del juego".
    /// </summary>
    public sealed class FirstPersonPaint : MonoBehaviour
    {
        private const float ZoomSeconds = 0.4f;
        private const float GlanceSeconds = 1f;
        private const float DoneCoverage = 0.85f;

        private InputReader _input;
        private PlaceholderCanvas _canvas;

        private CanvasGroup _group;
        private RawImage _wall;
        private Image _ring;
        private Text _secs;
        private Text _hint;
        private PaintingWindow _window;

        private float _enter;         // 0..1 zoom-in progress
        private float _glanceTimer;
        private bool _glancePrev;

        public bool Active { get; private set; }
        public bool Finished { get; private set; }
        public bool GlanceActive { get; private set; }

        public void Init(InputReader input)
        {
            _input = input;
            _canvas = new PlaceholderCanvas(512);
            BuildUi();
            SetVisible(false);
        }

        public void Enter(PaintingWindow window)
        {
            Active = true;
            Finished = false;
            GlanceActive = false;
            _glanceTimer = 0f;
            _enter = 0f;
            _window = window;
            _window.BeginConsume();
            _canvas.Clear();
            _wall.texture = _canvas.Texture;
            SetVisible(true);
        }

        public void Exit()
        {
            Active = false;
            SetVisible(false);
        }

        private void OnDestroy()
        {
            _canvas?.Dispose();
        }

        public void Tick(float dt)
        {
            if (!Active) return;

            _enter = Mathf.Min(1f, _enter + dt / ZoomSeconds);
            _wall.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, _enter);

            // Glance: a rising edge starts a 1 s peek at the street (paint frozen).
            bool glance = _input != null && _input.GlanceHeld;
            if (glance && !_glancePrev && _glanceTimer <= 0f) _glanceTimer = GlanceSeconds;
            _glancePrev = glance;
            if (_glanceTimer > 0f) { _glanceTimer -= dt; GlanceActive = true; }
            else GlanceActive = false;

            _group.alpha = _enter * (GlanceActive ? 0.12f : 1f);

            if (!GlanceActive)
            {
                _window.Tick(dt);
                HandlePaint();
            }

            float f = _window.Fraction;
            _ring.fillAmount = f;
            _ring.color = PaintingWindow.Urgency(f);
            _secs.text = $"{_window.Remaining:0.0}s";
            _hint.text = GlanceActive
                ? "VISTAZO a la calle…"
                : "Arrastra para pintar · mantén S/↓ para vistazo";

            if (_canvas.Coverage >= DoneCoverage) Finished = true;
        }

        private void HandlePaint()
        {
            if (!TryPointer(out Vector2 screen, out bool pressed) || !pressed) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _wall.rectTransform, screen, null, out Vector2 local)) return;
            var r = _wall.rectTransform.rect;
            Vector2 uv = new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
            if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f) return;
            _canvas.Paint(uv);
        }

        private bool TryPointer(out Vector2 pos, out bool pressed)
        {
            pos = default; pressed = false;
#if ENABLE_INPUT_SYSTEM
            var ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.isPressed) { pos = ts.primaryTouch.position.ReadValue(); pressed = true; return true; }
            var m = Mouse.current;
            if (m != null) { pos = m.position.ReadValue(); pressed = m.leftButton.isPressed; return true; }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.touchCount > 0)
            {
                var t = UnityEngine.Input.GetTouch(0);
                pos = t.position;
                pressed = t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
                return true;
            }
            pos = UnityEngine.Input.mousePosition;
            pressed = UnityEngine.Input.GetMouseButton(0);
            return true;
#else
            return false;
#endif
        }

        // ---------------- UI construction ----------------

        private void SetVisible(bool v)
        {
            _group.alpha = v ? 1f : 0f;
            _group.gameObject.SetActive(v);
        }

        private void BuildUi()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                       ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var canvasGo = new GameObject("FpCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasGo.AddComponent<GraphicRaycaster>();
            _group = canvasGo.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            var root = (RectTransform)canvasGo.transform;

            var dim = NewImage(root, "Dim", new Color(0.03f, 0.03f, 0.04f, 0.92f));
            Stretch(dim.rectTransform);

            _wall = new GameObject("Wall", typeof(RectTransform)).AddComponent<RawImage>();
            _wall.transform.SetParent(root, false);
            var wr = _wall.rectTransform;
            wr.anchorMin = new Vector2(0.5f, 0.5f); wr.anchorMax = new Vector2(0.5f, 0.5f);
            wr.pivot = new Vector2(0.5f, 0.5f);
            wr.sizeDelta = new Vector2(560, 480);

            _ring = NewImage(root, "Ring", Color.white);
            _ring.sprite = BuildRingSprite(128);
            _ring.type = Image.Type.Filled;
            _ring.fillMethod = Image.FillMethod.Radial360;
            _ring.fillOrigin = (int)Image.Origin360.Top;
            _ring.fillClockwise = false;
            var rr = _ring.rectTransform;
            rr.anchorMin = new Vector2(0.5f, 1f); rr.anchorMax = new Vector2(0.5f, 1f);
            rr.pivot = new Vector2(0.5f, 1f);
            rr.anchoredPosition = new Vector2(0, -20);
            rr.sizeDelta = new Vector2(96, 96);

            _secs = NewText(root, "Secs", font, 22, TextAnchor.MiddleCenter);
            var sr = _secs.rectTransform;
            sr.anchorMin = new Vector2(0.5f, 1f); sr.anchorMax = new Vector2(0.5f, 1f);
            sr.pivot = new Vector2(0.5f, 1f);
            sr.anchoredPosition = new Vector2(0, -120);
            sr.sizeDelta = new Vector2(200, 30);

            _hint = NewText(root, "Hint", font, 18, TextAnchor.LowerCenter);
            _hint.color = new Color(1, 1, 1, 0.75f);
            var hr = _hint.rectTransform;
            hr.anchorMin = new Vector2(0.5f, 0f); hr.anchorMax = new Vector2(0.5f, 0f);
            hr.pivot = new Vector2(0.5f, 0f);
            hr.anchoredPosition = new Vector2(0, 30);
            hr.sizeDelta = new Vector2(900, 30);
        }

        private static Image NewImage(RectTransform parent, string name, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = c;
            return img;
        }

        private static Text NewText(RectTransform parent, string name, Font font, int size, TextAnchor a)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.color = Color.white; t.alignment = a;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static Sprite BuildRingSprite(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            var px = new Color[s * s];
            float c = s * 0.5f, outer = s * 0.48f, inner = s * 0.34f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    float a = (d <= outer && d >= inner) ? 1f : 0f;
                    px[y * s + x] = new Color(1, 1, 1, a);
                }
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        }
    }
}
