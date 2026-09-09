using System;
using PieceBook.DrawingEngine;
using PieceBook.DrawingEngine.Config;
using UnityEngine;

namespace PieceBook.UI.Paint
{
    /// <summary>Dumb paint HUD (§7 MVP): a prefab MonoBehaviour reflects presenter state.</summary>
    public interface IPaintView
    {
        void SetSelectedCap(string capId);
        void SetColor(Color color);
        void SetTool(ToolKind tool);
    }

    /// <summary>
    /// Presenter for the paint screen (UI-03) — the production HUD replacing the spike's DemoHud.
    /// It talks only to the pure <see cref="IDrawingCanvas"/> contract (§4.3), never to the concrete
    /// engine, so it is testable with a mock canvas. Cap/paint/color/tool selection builds the
    /// active <see cref="StrokeConfig"/> that each stroke begins with.
    /// </summary>
    public sealed class PaintPresenter
    {
        private readonly IDrawingCanvas _canvas;
        private readonly IPaintView _view;
        private StrokeConfig _config;

        public PaintPresenter(IDrawingCanvas canvas, IPaintView view = null)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _view = view;
            _config = new StrokeConfig { Color = Color.white, InitialPressure = 1f };
        }

        /// <summary>The stroke config the next <see cref="BeginStroke"/> will use.</summary>
        public StrokeConfig CurrentConfig => _config;

        public void SelectCap(CapDef cap)
        {
            _config.Cap = cap;
            _view?.SetSelectedCap(cap != null ? cap.id : null);
        }

        public void SelectPaint(PaintDef paint) => _config.Paint = paint;

        public void SetColor(Color color)
        {
            _config.Color = color;
            _view?.SetColor(color);
        }

        public void SetTool(ToolKind tool)
        {
            _config.Tool = tool;
            _view?.SetTool(tool);
        }

        public void BeginStroke() => _canvas.BeginStroke(_config);
        public void UpdateStroke(Vector2 pos, float pressure, float tilt) => _canvas.UpdateStroke(pos, pressure, tilt);
        public void EndStroke() => _canvas.EndStroke();

        public void Undo() => _canvas.Undo();
        public void Redo() => _canvas.Redo();
    }
}
