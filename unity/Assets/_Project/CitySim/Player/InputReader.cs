using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PieceBook.CitySim.Player
{
    /// <summary>
    /// Backend-agnostic input for the spike. Keyboard/mouse in the editor, basic touch on
    /// device. Works under the new Input System (preferred) or the legacy manager, so the
    /// project's active-input setting never blocks Play.
    ///
    /// Gestures:
    /// - Move: WASD / arrows / left stick (or drag on touch)
    /// - Interact (start painting / restart): Space / left-click / tap
    /// - Hide (hold): Left Shift / right-click / second-finger hold
    /// - Glance (hold, while painting): S / Down arrow / swipe-down hold
    /// </summary>
    public sealed class InputReader
    {
        public Vector2 Move { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool HideHeld { get; private set; }
        public bool GlanceHeld { get; private set; }

        private Vector2 _touchStart;
        private bool _touching;

        public void Tick()
        {
            Move = Vector2.zero;
            InteractPressed = false;
            HideHeld = false;
            GlanceHeld = false;

#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                Vector2 m = Vector2.zero;
                if (k.wKey.isPressed || k.upArrowKey.isPressed) m.y += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) m.y -= 1f;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) m.x -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) m.x += 1f;
                Move = m;
                InteractPressed |= k.spaceKey.wasPressedThisFrame;
                HideHeld |= k.leftShiftKey.isPressed || k.hKey.isPressed;
                GlanceHeld |= k.sKey.isPressed || k.downArrowKey.isPressed;
            }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                InteractPressed |= mouse.leftButton.wasPressedThisFrame;
                HideHeld |= mouse.rightButton.isPressed;
            }
            var gp = Gamepad.current;
            if (gp != null)
            {
                if (Move == Vector2.zero) Move = gp.leftStick.ReadValue();
                InteractPressed |= gp.buttonSouth.wasPressedThisFrame;
                HideHeld |= gp.buttonEast.isPressed;
            }
            ReadTouchNew();
#elif ENABLE_LEGACY_INPUT_MANAGER
            Vector2 m = Vector2.zero;
            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow)) m.y += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow)) m.y -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow)) m.x -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow)) m.x += 1f;
            Move = m;
            InteractPressed |= UnityEngine.Input.GetKeyDown(KeyCode.Space) || UnityEngine.Input.GetMouseButtonDown(0);
            HideHeld |= UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.H) || UnityEngine.Input.GetMouseButton(1);
            GlanceHeld |= UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow);
#endif
            if (Move.sqrMagnitude > 1f) Move = Move.normalized;
        }

#if ENABLE_INPUT_SYSTEM
        private void ReadTouchNew()
        {
            var ts = Touchscreen.current;
            if (ts == null) return;
            var t = ts.primaryTouch;
            if (!t.press.isPressed)
            {
                if (_touching) { InteractPressed |= true; _touching = false; } // tap release = interact
                return;
            }
            Vector2 pos = t.position.ReadValue();
            if (!_touching) { _touching = true; _touchStart = pos; }
            Vector2 d = pos - _touchStart;
            if (d.magnitude > 40f) Move = d.normalized;          // drag to move
            if (d.y < -80f) GlanceHeld = true;                    // swipe down = glance
        }
#endif
    }
}
