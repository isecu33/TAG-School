using UnityEngine;

namespace PieceBook.DrawingEngine.Drips
{
    /// <summary>
    /// Coarse coverage grid that tracks how much paint has piled up per canvas region.
    /// When a cell crosses the paint's dripThreshold (§4.1 / ENG-04) a drip is born there.
    /// Backed by a flat float[] so updates are branch-light and allocation-free.
    /// </summary>
    public sealed class PaintAccumulator
    {
        private readonly int _res;
        private readonly float[] _cells;

        public int Resolution => _res;

        public PaintAccumulator(int resolution = 96)
        {
            _res = Mathf.Max(8, resolution);
            _cells = new float[_res * _res];
        }

        public void Clear() => System.Array.Clear(_cells, 0, _cells.Length);

        /// <summary>Deposit paint at a normalized canvas position; returns the cell index touched.</summary>
        public int Deposit(Vector2 pos01, float amount)
        {
            int cx = Mathf.Clamp((int)(pos01.x * _res), 0, _res - 1);
            int cy = Mathf.Clamp((int)(pos01.y * _res), 0, _res - 1);
            int idx = cy * _res + cx;
            _cells[idx] += amount;
            return idx;
        }

        public float Get(int idx) => _cells[idx];

        /// <summary>Reset a cell after it has spawned a drip so it does not machine-gun drips.</summary>
        public void Consume(int idx) => _cells[idx] = 0f;

        /// <summary>Center (normalized) of a cell index — where the drip head starts.</summary>
        public Vector2 CellCenter01(int idx)
        {
            int cy = idx / _res;
            int cx = idx - cy * _res;
            return new Vector2((cx + 0.5f) / _res, (cy + 0.5f) / _res);
        }
    }
}
