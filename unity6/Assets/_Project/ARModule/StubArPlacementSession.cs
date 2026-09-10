using System;
using UnityEngine;

namespace PieceBook.ARModule
{
    /// <summary>
    /// "Not supported" stub for <see cref="IArPlacementSession"/>. Used on devices without AR and as
    /// the default until the AR Foundation implementation is added. Everything is a safe no-op so
    /// callers fall back to the photo mockups (§6 "Fallback sin RA").
    /// </summary>
    public sealed class StubArPlacementSession : IArPlacementSession
    {
        public bool IsSupported => false;
#pragma warning disable 67 // event never invoked in the stub (by design)
        public event Action<ArPlaneInfo> PlaneDetected;
#pragma warning restore 67
        public void StartScan() { }
        public void StopScan() { }
        public void PlaceArtwork(Texture2D artwork, ArPlaneInfo plane, ArPlacement placement) { }
        public byte[] CaptureFrame() => null;
    }
}
