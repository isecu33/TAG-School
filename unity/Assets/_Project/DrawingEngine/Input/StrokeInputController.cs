using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PieceBook.DrawingEngine.Input
{
    /// <summary>
    /// Translates raw pointer input (Apple Pencil pressure/tilt, touch, or mouse) into
    /// <see cref="IDrawingCanvas"/> calls. Casts a ray from the camera onto the wall's
    /// MeshCollider and forwards the hit's texture UV as normalized canvas coordinates.
    ///
    /// Supports the new Input System (preferred, gives pressure/tilt — ARQUITECTURA §2) and
    /// falls back to the legacy Input manager so the spike runs under any project setting.
    /// The engine stays UI-agnostic: the current StrokeConfig is supplied by a provider the
    /// demo/harness sets, so this class never references caps, colors or UI (§3).
    /// </summary>
    public sealed class StrokeInputController : MonoBehaviour
    {
        [SerializeField] private DrawingCanvas canvas;
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private Collider wallCollider;
        [Tooltip("Pressure reported when the device has no pressure sensor (touch/mouse).")]
        [SerializeField, Range(0f, 1f)] private float pressureFallback = 1f;
        [SerializeField] private float maxRayDistance = 100f;

        /// <summary>Supplies the StrokeConfig for a new stroke (set by the harness).</summary>
        public Func<StrokeConfig> StrokeConfigProvider;

        /// <summary>
        /// Optional predicate the harness sets so a press over UI (a button/slider) does not
        /// start a stroke. Kept as a delegate so the engine never references UnityEngine.UI (§3).
        /// </summary>
        public Func<bool> PointerBlocked;

        private bool _strokeActive;

        public void Configure(DrawingCanvas c, Camera cam, Collider wall)
        {
            canvas = c;
            raycastCamera = cam;
            wallCollider = wall;
        }

        private void Update()
        {
            if (canvas == null || raycastCamera == null) return;

            bool pressed = ReadPointer(out Vector2 screenPos, out float pressure, out float tilt);

            if (pressed)
            {
                bool onWall = TryGetCanvasUv(screenPos, out Vector2 uv);

                if (!_strokeActive)
                {
                    if (!onWall) return; // only start when the press lands on the wall
                    if (PointerBlocked != null && PointerBlocked()) return; // press is on UI
                    var cfg = StrokeConfigProvider != null ? StrokeConfigProvider() : default;
                    canvas.BeginStroke(cfg);
                    _strokeActive = true;
                }

                if (onWall)
                    canvas.UpdateStroke(uv, pressure, tilt);
            }
            else if (_strokeActive)
            {
                canvas.EndStroke();
                _strokeActive = false;
            }
        }

        private bool TryGetCanvasUv(Vector2 screenPos, out Vector2 uv)
        {
            uv = default;
            var ray = raycastCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance)) return false;
            if (wallCollider != null && hit.collider != wallCollider) return false;
            uv = hit.textureCoord; // requires a MeshCollider on the wall
            return true;
        }

        /// <summary>Reads the active pointer. Returns true while the surface is being pressed.</summary>
        private bool ReadPointer(out Vector2 screenPos, out float pressure, out float tilt)
        {
            screenPos = default;
            pressure = pressureFallback;
            tilt = 0f;

#if ENABLE_INPUT_SYSTEM
            var pen = Pen.current;
            if (pen != null && pen.tip.isPressed)
            {
                screenPos = pen.position.ReadValue();
                pressure = Mathf.Clamp01(pen.pressure.ReadValue());
                if (pressure <= 0f) pressure = pressureFallback;
                tilt = pen.tilt.ReadValue().magnitude;
                return true;
            }

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                screenPos = touch.primaryTouch.position.ReadValue();
                float p = touch.primaryTouch.pressure.ReadValue();
                pressure = p > 0f ? Mathf.Clamp01(p) : pressureFallback;
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                screenPos = mouse.position.ReadValue();
                return true;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.touchCount > 0)
            {
                var t = UnityEngine.Input.GetTouch(0);
                screenPos = t.position;
                pressure = t.maximumPossiblePressure > 0f
                    ? Mathf.Clamp01(t.pressure / t.maximumPossiblePressure)
                    : pressureFallback;
                return t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
            }
            if (UnityEngine.Input.GetMouseButton(0))
            {
                screenPos = UnityEngine.Input.mousePosition;
                return true;
            }
            return false;
#else
            return false;
#endif
        }
    }
}
