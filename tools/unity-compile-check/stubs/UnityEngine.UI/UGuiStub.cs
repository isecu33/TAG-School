// Stub mínimo de uGUI (com.unity.ugui no está en NuGet). Solo declara lo que usa el proyecto;
// si un fichero nuevo usa más API de uGUI, añádela aquí con la misma firma que en Unity.
using System;
using UnityEngine.Events;

namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour { }
    public class EventSystem : UIBehaviour
    {
        public static EventSystem current { get; set; }
        public bool IsPointerOverGameObject() => false;
        public bool IsPointerOverGameObject(int pointerId) => false;
    }
    public abstract class BaseInputModule : UIBehaviour { }
    public class StandaloneInputModule : BaseInputModule { }
}

namespace UnityEngine.UI
{
    using UnityEngine.EventSystems;

    public abstract class Graphic : UIBehaviour
    {
        public virtual Color color { get; set; }
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform => null;
    }
    public abstract class MaskableGraphic : Graphic { }
    public class Image : MaskableGraphic
    {
        public Sprite sprite { get; set; }
        public enum Type { Simple, Sliced, Tiled, Filled }
        public Type type { get; set; }
    }
    public class RawImage : MaskableGraphic { public Texture texture { get; set; } }
    public class Text : MaskableGraphic
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public TextAnchor alignment { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public FontStyle fontStyle { get; set; }
        public bool supportRichText { get; set; }
    }
    public class Selectable : UIBehaviour
    {
        public Graphic targetGraphic { get; set; }
        public bool interactable { get; set; }
    }
    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEvent { }
        public ButtonClickedEvent onClick { get; set; } = new ButtonClickedEvent();
    }
    public class Slider : Selectable
    {
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
        public class SliderEvent : UnityEvent<float> { }
        public RectTransform fillRect { get; set; }
        public RectTransform handleRect { get; set; }
        public Direction direction { get; set; }
        public float minValue { get; set; }
        public float maxValue { get; set; }
        public float value { get; set; }
        public SliderEvent onValueChanged { get; set; } = new SliderEvent();
    }
    public class CanvasScaler : UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public float matchWidthOrHeight { get; set; }
    }
    public class GraphicRaycaster : UIBehaviour { }
}
