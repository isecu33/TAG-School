using PieceBook.CitySim.AI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PieceBook.CitySim.UI
{
    /// <summary>
    /// Placeholder screen HUD for the spike (NO final UI). Built in code: phase readout, a
    /// context prompt, a controls legend, and — added in later points — the painting-window
    /// ring and the Caught/Escaped panels. All gray/functional, no art.
    /// </summary>
    public sealed class CitySimHud : MonoBehaviour
    {
        private Font _font;
        private Text _phase;
        private Text _prompt;
        private Text _alert;
        private GameObject _outcome;
        private Text _outcomeTitle;
        private Image _outcomeBg;

        public void Init()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            EnsureEventSystem();
            var root = BuildCanvas();

            _phase = Text(root, "Phase", 20, TextAnchor.UpperLeft);
            Place(_phase.rectTransform, new Vector2(0, 1), new Vector2(16, -14), new Vector2(560, 28));

            _alert = Text(root, "Alert", 18, TextAnchor.UpperLeft);
            Place(_alert.rectTransform, new Vector2(0, 1), new Vector2(16, -44), new Vector2(560, 26));

            var help = Text(root, "Help", 15, TextAnchor.UpperLeft);
            help.color = new Color(1f, 1f, 1f, 0.6f);
            help.text = "WASD/flechas: mover · Espacio: pintar · S/↓ (pintando): vistazo · " +
                        "Shift/H (persecución): esconderse";
            Place(help.rectTransform, new Vector2(0, 1), new Vector2(16, -74), new Vector2(760, 24));

            _prompt = Text(root, "Prompt", 22, TextAnchor.LowerCenter);
            var rt = _prompt.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, 40);
            rt.sizeDelta = new Vector2(900, 32);

            BuildOutcome(root);
        }

        private void BuildOutcome(RectTransform root)
        {
            _outcomeBg = NewImage(root, "Outcome", new Color(0.03f, 0.03f, 0.05f, 0.9f));
            var br = _outcomeBg.rectTransform;
            br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = Vector2.zero; br.offsetMax = Vector2.zero;
            _outcome = _outcomeBg.gameObject;

            _outcomeTitle = Text(br, "OutcomeTitle", 56, TextAnchor.MiddleCenter);
            var tr = _outcomeTitle.rectTransform;
            tr.anchorMin = new Vector2(0.5f, 0.5f); tr.anchorMax = new Vector2(0.5f, 0.5f); tr.pivot = new Vector2(0.5f, 0.5f);
            tr.anchoredPosition = new Vector2(0, 30); tr.sizeDelta = new Vector2(900, 100);

            var sub = Text(br, "OutcomeSub", 22, TextAnchor.MiddleCenter);
            var sr = sub.rectTransform;
            sr.anchorMin = new Vector2(0.5f, 0.5f); sr.anchorMax = new Vector2(0.5f, 0.5f); sr.pivot = new Vector2(0.5f, 0.5f);
            sr.anchoredPosition = new Vector2(0, -50); sr.sizeDelta = new Vector2(900, 40);
            sub.text = "Pulsa Espacio para reintentar";

            _outcome.transform.SetAsLastSibling();
            _outcome.SetActive(false);
        }

        public void ShowOutcome(string title, Color color)
        {
            if (_outcome == null) return;
            _outcomeTitle.text = title;
            _outcomeTitle.color = color;
            _outcome.transform.SetAsLastSibling();
            _outcome.SetActive(true);
        }

        public void HideOutcome() { if (_outcome != null) _outcome.SetActive(false); }

        private Image NewImage(RectTransform parent, string name, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = c;
            return img;
        }

        public void SetPhase(LoopPhase phase) { if (_phase) _phase.text = "FASE: " + PhaseName(phase); }
        public void SetPrompt(string s) { if (_prompt) _prompt.text = s; }

        public void SetAlert(PatrolState state, float suspicion, float heat)
        {
            if (!_alert) return;
            string s = state == PatrolState.Calm ? "CALMA"
                     : state == PatrolState.Suspicious ? "SOSPECHA (?)"
                     : "PERSECUCIÓN (!)";
            _alert.color = state == PatrolState.Calm ? new Color(0.7f, 1f, 0.7f)
                         : state == PatrolState.Suspicious ? new Color(1f, 0.85f, 0.3f)
                         : new Color(1f, 0.4f, 0.4f);
            _alert.text = $"PATRULLA: {s}   sospecha {suspicion:0.00}   calor zona {heat:0.00}";
        }

        private static string PhaseName(LoopPhase p) => p switch
        {
            LoopPhase.Explore => "EXPLORAR",
            LoopPhase.Painting => "PINTANDO (1ª persona)",
            LoopPhase.Chase => "PERSECUCIÓN",
            LoopPhase.Caught => "PILLADO",
            LoopPhase.Escaped => "ESCAPADO",
            _ => p.ToString()
        };

        // ---- construction helpers ----

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
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            go.AddComponent<GraphicRaycaster>();
            return go.GetComponent<RectTransform>();
        }

        private Text Text(RectTransform parent, string name, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = size; t.color = Color.white; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }
    }
}
