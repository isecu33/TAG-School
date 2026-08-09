using UnityEngine;

namespace PieceBook.DrawingEngine
{
    /// <summary>
    /// Public contract of the Drawing Engine — copied verbatim from ARQUITECTURA §4.3.
    /// The engine exposes a PURE API: it knows nothing about lessons or UI (§3).
    /// Do not add UI/lesson concerns here; extend via events on Core.EventBus instead.
    /// </summary>
    /// <remarks>
    /// Coordinate convention: <see cref="UpdateStroke"/> <c>pos</c> is normalized canvas
    /// space in [0,1] on both axes (0,0 = bottom-left), independent of RenderTexture size.
    /// </remarks>
    public interface IDrawingCanvas
    {
        LayerId AddLayer();
        void SetActiveLayer(LayerId id);

        void BeginStroke(StrokeConfig cfg);                       // cap+pintura+color+presión inicial
        void UpdateStroke(Vector2 pos, float pressure, float tilt);
        void EndStroke();

        void Undo();
        void Redo();

        Texture2D Flatten();
        StrokeRecording GetRecording();
    }
}
