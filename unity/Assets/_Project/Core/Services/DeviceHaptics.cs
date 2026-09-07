using UnityEngine;

namespace PieceBook.Core.Services
{
    /// <summary>
    /// Device <see cref="IHaptics"/>. On iOS/Android it triggers the system haptics; everywhere
    /// else (editor, desktop) it is a safe no-op so gameplay code can call it unconditionally.
    ///
    /// The Phase-1 implementation uses <see cref="Handheld.Vibrate"/> as a portable baseline;
    /// richer taptic patterns (light vs success) are a device-only refinement tracked as a
    /// checklist item (CORE-04) since they can't be verified in this build environment.
    /// </summary>
    public sealed class DeviceHaptics : IHaptics
    {
        public void Light() => Vibrate();

        public void Success() => Vibrate();

        private static void Vibrate()
        {
#if UNITY_IOS || UNITY_ANDROID
            if (!Application.isEditor)
            {
                Handheld.Vibrate();
            }
#endif
            // Editor / desktop: intentionally no-op.
        }
    }

    /// <summary>No-op haptics for tests and non-mobile targets.</summary>
    public sealed class NullHaptics : IHaptics
    {
        public void Light() { }
        public void Success() { }
    }
}
