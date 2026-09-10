using System;
using UnityEngine;

namespace PieceBook.ARModule
{
    /// <summary>
    /// AR placement session boundary (ARQUITECTURA §6): detect a vertical plane, anchor the finished
    /// artwork on it, scale/rotate it, and capture a photo/video. The AR Foundation implementation
    /// (ARKit/ARCore) satisfies this on device; a stub reports "not supported" so the rest of the game
    /// degrades to the wall mockups (SOC-05) with no AR dependency (§3: nadie referencia ARModule).
    /// </summary>
    public interface IArPlacementSession
    {
        bool IsSupported { get; }

        /// <summary>Raised when a usable vertical plane is detected (AR-01).</summary>
        event Action<ArPlaneInfo> PlaneDetected;

        void StartScan();
        void StopScan();

        /// <summary>Anchor the artwork texture onto the given plane (AR-02).</summary>
        void PlaceArtwork(Texture2D artwork, ArPlaneInfo plane, ArPlacement placement);

        /// <summary>Capture the AR scene with the anchored piece as PNG bytes (AR-04).</summary>
        byte[] CaptureFrame();
    }
}
