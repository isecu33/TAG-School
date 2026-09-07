using System.Collections.Generic;
using UnityEngine;

namespace PieceBook.DrawingEngine
{
    /// <summary>
    /// Tracks which square tiles of the canvas a stroke has touched (ARQUITECTURA §4.1 "undo:
    /// snapshots por tiles — solo tiles modificados"). This is the pure, allocation-light core of
    /// the tiled-undo system: mark the tiles a stamp covers, then enumerate exactly the dirty tiles
    /// so undo copies only those (instead of the whole RenderTexture, which does not scale to more
    /// layers / iPad 4096²).
    ///
    /// Coordinates are normalized canvas space [0,1] (same convention as <see cref="IDrawingCanvas"/>).
    /// The GPU copy-on-write that reads/writes the tile pixels using these indices (via
    /// <c>Graphics.CopyTexture</c> region copies) is wired in <c>DrawingCanvas</c> and validated
    /// in-editor; the index math it relies on is what this class makes correct and testable.
    /// </summary>
    public sealed class TileDirtyTracker
    {
        public int TilesPerSide { get; }
        public int TileCount => TilesPerSide * TilesPerSide;

        private readonly bool[] _dirty;
        private readonly List<int> _dirtyList; // insertion-ordered unique dirty tile indices
        public IReadOnlyList<int> DirtyTiles => _dirtyList;

        public TileDirtyTracker(int tilesPerSide)
        {
            TilesPerSide = Mathf.Max(1, tilesPerSide);
            _dirty = new bool[TilesPerSide * TilesPerSide];
            _dirtyList = new List<int>(TilesPerSide * TilesPerSide);
        }

        public int TileIndex(int tx, int ty) => ty * TilesPerSide + tx;

        public bool IsDirty(int tx, int ty) =>
            InRange(tx, ty) && _dirty[TileIndex(tx, ty)];

        /// <summary>Mark every tile overlapped by a disc of <paramref name="radius01"/> at <paramref name="center01"/>.</summary>
        public void MarkDisc(Vector2 center01, float radius01)
        {
            MarkRect(center01.x - radius01, center01.y - radius01,
                     center01.x + radius01, center01.y + radius01);
        }

        /// <summary>Mark every tile overlapped by the axis-aligned rect (normalized coords).</summary>
        public void MarkRect(float minX, float minY, float maxX, float maxY)
        {
            int tx0 = TileCoord(minX), tx1 = TileCoord(maxX);
            int ty0 = TileCoord(minY), ty1 = TileCoord(maxY);
            for (int ty = ty0; ty <= ty1; ty++)
                for (int tx = tx0; tx <= tx1; tx++)
                    MarkTile(tx, ty);
        }

        private void MarkTile(int tx, int ty)
        {
            if (!InRange(tx, ty)) return;
            int idx = TileIndex(tx, ty);
            if (_dirty[idx]) return;
            _dirty[idx] = true;
            _dirtyList.Add(idx);
        }

        /// <summary>Clear all dirty flags for the next stroke. Keeps capacity (no GC churn).</summary>
        public void Reset()
        {
            for (int i = 0; i < _dirtyList.Count; i++) _dirty[_dirtyList[i]] = false;
            _dirtyList.Clear();
        }

        private int TileCoord(float n01) => Mathf.Clamp(Mathf.FloorToInt(n01 * TilesPerSide), 0, TilesPerSide - 1);

        private bool InRange(int tx, int ty) => tx >= 0 && ty >= 0 && tx < TilesPerSide && ty < TilesPerSide;
    }
}
