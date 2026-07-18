using System.Collections.Generic;
using PieceBook.DrawingEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PieceBook.Spike
{
    /// <summary>
    /// Placeholder uGUI HUD for the spike (NO HACER: UI final). Built entirely in code:
    /// a cap selector, the nozzle-distance slider (the teaching dial), undo/clear/color, and
    /// live readouts for the acceptance metrics (FPS + input→submit latency + drips).
    /// </summary>
    public sealed class DemoHud : MonoBehaviour
    {
        private SprayDemoBootstrap _owner;
        private DrawingCanvas _canvas;

        private Text _readout;
        private readonly List<Button> _capButtons = new List<Button>(4);
        private Font _font;

        private static readonly Color Idle = new Color(0.20f, 0.20f, 0.24f, 0.9f);
        private static readonly Color Active = new Color(0.90f, 0.45f, 0.15f, 0.95f);

        public void Init(SprayDemoBootstrap owner, DrawingCanvas canvas)
        {
            _owner = owner;
            _canvas = canvas;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            EnsureEventSystem();
            var root = BuildCanvas();
            BuildReadout(root);
            BuildBottomBar(root);
            RefreshCapHighlight();
        }

        private void Update()
        {
            if (_canvas == null || _readout == null) return;
            var fs = _canvas.FrameStats;
            var lat = _canvas.Latency;
            _readout.text =
                $"FPS   {fs.AverageFps,5:0.}   ({fs.AverageMs,4:0.0} ms avg / {fs.WorstMs,4:0.0} worst)\n" +
                $"LAT   {lat.AverageMs,4:0.0} ms avg   {lat.WorstMs,4:0.0} ms worst  (input→GPU submit)\n" +
                $"DRIPS {_canvas.ActiveDrips,3}     STAMPS/frame {_canvas.InstancesLastFrame,5}\n" +
                $"Cap: {_owner.CapName(_owner.SelectedCap)}   Nozzle: {_owner.NozzleDistance:0.00}";
        }

        // ---------------------------------------------------------------- UI construction

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem");
            es.transform.SetParent(transform, false);
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        private RectTransform BuildCanvas()
        {
            var go = new GameObject("HudCanvas");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go.GetComponent<RectTransform>();
        }

        private void BuildReadout(RectTransform root)
        {
            var panel = NewImage(root, "ReadoutBg", new Color(0f, 0f, 0f, 0.45f));
            Anchor(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12, -12),
                   new Vector2(560, 96), new Vector2(0f, 1f));

            _readout = NewText(panel, "Readout", "", 18, TextAnchor.UpperLeft);
            Stretch(_readout.rectTransform, 10, 6);
        }

        private void BuildBottomBar(RectTransform root)
        {
            var bar = NewImage(root, "BottomBar", new Color(0f, 0f, 0f, 0.45f));
            Anchor(bar, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0, 0),
                   new Vector2(0, 108), new Vector2(0.5f, 0f));
            var barRt = bar.rectTransform;
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(1f, 0f);
            barRt.offsetMin = new Vector2(0f, 0f);
            barRt.offsetMax = new Vector2(0f, 108f);

            // Cap selector
            _capButtons.Clear();
            for (int i = 0; i < _owner.CapCount; i++)
            {
                int idx = i;
                var b = NewButton(barRt, $"Cap{i}", _owner.CapName(i), () => { _owner.SelectCap(idx); RefreshCapHighlight(); });
                Place(b.GetComponent<RectTransform>(), 16 + i * 150, 58, 140, 36);
                _capButtons.Add(b);
            }

            // Nozzle distance slider (the teaching dial)
            var label = NewText(barRt, "NozzleLabel", "Distancia de boquilla", 15, TextAnchor.LowerLeft);
            Place(label.rectTransform, 16, 14, 220, 24);
            var slider = NewSlider(barRt, _owner.NozzleDistance, v => _owner.SetNozzleDistance(v));
            Place(slider.GetComponent<RectTransform>(), 200, 18, 260, 24);

            // Actions
            var clear = NewButton(barRt, "Clear", "Clear", () => _owner.ClearCanvas());
            Place(clear.GetComponent<RectTransform>(), 490, 58, 90, 36);
            var undo = NewButton(barRt, "Undo", "Undo", () => _owner.Undo());
            Place(undo.GetComponent<RectTransform>(), 490, 14, 90, 36);
            var redo = NewButton(barRt, "Redo", "Redo", () => _owner.Redo());
            Place(redo.GetComponent<RectTransform>(), 588, 14, 90, 36);

            // Color swatches
            AddSwatch(barRt, "Ink", new Color(0.05f, 0.05f, 0.06f), 596, 58);
            AddSwatch(barRt, "White", Color.white, 640, 58);
            AddSwatch(barRt, "Red", new Color(0.85f, 0.15f, 0.15f), 684, 58);
            AddSwatch(barRt, "Blue", new Color(0.2f, 0.4f, 0.9f), 728, 58);
            AddSwatch(barRt, "Green", new Color(0.25f, 0.7f, 0.3f), 772, 58);
        }

        private void AddSwatch(RectTransform parent, string name, Color c, float x, float y)
        {
            var img = NewImage(parent, "Sw_" + name, c);
            Place(img.rectTransform, x, y, 36, 36);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => _owner.SetInk(c));
        }

        private void RefreshCapHighlight()
        {
            for (int i = 0; i < _capButtons.Count; i++)
            {
                var img = _capButtons[i].targetGraphic as Image;
                if (img != null) img.color = (i == _owner.SelectedCap) ? Active : Idle;
            }
        }

        // ---------------------------------------------------------------- tiny uGUI helpers

        private Image NewImage(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private Text NewText(Component parent, string name, string content, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.text = content;
            t.color = Color.white;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = false;
            return t;
        }

        private Button NewButton(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
        {
            var img = NewImage(parent, name, Idle);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            var txt = NewText(img.rectTransform, "Label", label, 16, TextAnchor.MiddleCenter);
            Stretch(txt.rectTransform, 2, 2);
            return btn;
        }

        private Slider NewSlider(RectTransform parent, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            var root = new GameObject("NozzleSlider", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var slider = root.AddComponent<Slider>();

            var bg = NewImage(root.GetComponent<RectTransform>(), "Background", new Color(0.25f, 0.25f, 0.28f));
            Stretch(bg.rectTransform, 0, 0);
            bg.rectTransform.anchorMin = new Vector2(0f, 0.35f);
            bg.rectTransform.anchorMax = new Vector2(1f, 0.65f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(root.transform, false);
            fillArea.anchorMin = new Vector2(0f, 0.35f);
            fillArea.anchorMax = new Vector2(1f, 0.65f);
            fillArea.offsetMin = new Vector2(6, 0);
            fillArea.offsetMax = new Vector2(-6, 0);
            var fill = NewImage(fillArea, "Fill", Active);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.sizeDelta = new Vector2(10, 0);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(root.transform, false);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(8, 0);
            handleArea.offsetMax = new Vector2(-8, 0);
            var handle = NewImage(handleArea, "Handle", Color.white);
            handle.rectTransform.sizeDelta = new Vector2(16, 0);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        // anchored positioning from the bottom-left of the parent
        private static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void Stretch(RectTransform rt, float padX, float padY)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }

        private static void Anchor(Image img, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            var rt = img.rectTransform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
