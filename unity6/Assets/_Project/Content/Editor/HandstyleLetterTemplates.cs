using UnityEngine;

namespace PieceBook.Content.Editor
{
    /// <summary>
    /// Geometric placeholder polylines for all 26 letters in normalized canvas space [0,1].
    /// Each letter is a single continuous stroke that traces the letter's recognizable shape —
    /// simple enough to trace, different enough per letter to be meaningful.
    /// Real handstyle art replaces these later (CNT-02); the data contract is identical.
    /// </summary>
    public static class HandstyleLetterTemplates
    {
        // All points in [0.15, 0.85] horizontal, [0.1, 0.9] vertical.
        private static readonly Vector2[][] Templates = new Vector2[][]
        {
            // A: two legs + crossbar (single stroke, backtracks for crossbar)
            new[] { v(0.2f,0.1f), v(0.5f,0.9f), v(0.8f,0.1f), v(0.65f,0.5f), v(0.35f,0.5f) },
            // B: vertical + two bumps traced continuously
            new[] { v(0.25f,0.1f), v(0.25f,0.9f), v(0.6f,0.9f), v(0.72f,0.77f), v(0.72f,0.62f),
                    v(0.6f,0.5f), v(0.25f,0.5f), v(0.6f,0.5f), v(0.75f,0.37f), v(0.75f,0.22f),
                    v(0.6f,0.1f), v(0.25f,0.1f) },
            // C: arc opening right
            new[] { v(0.75f,0.72f), v(0.6f,0.9f), v(0.4f,0.9f), v(0.25f,0.72f), v(0.2f,0.5f),
                    v(0.25f,0.28f), v(0.4f,0.1f), v(0.6f,0.1f), v(0.75f,0.28f) },
            // D: vertical + right arc
            new[] { v(0.25f,0.1f), v(0.25f,0.9f), v(0.55f,0.9f), v(0.75f,0.72f), v(0.8f,0.5f),
                    v(0.75f,0.28f), v(0.55f,0.1f), v(0.25f,0.1f) },
            // E: vertical + 3 arms
            new[] { v(0.75f,0.9f), v(0.25f,0.9f), v(0.25f,0.5f), v(0.65f,0.5f),
                    v(0.25f,0.5f), v(0.25f,0.1f), v(0.75f,0.1f) },
            // F: vertical + 2 arms (no bottom bar)
            new[] { v(0.75f,0.9f), v(0.25f,0.9f), v(0.25f,0.5f), v(0.65f,0.5f),
                    v(0.25f,0.5f), v(0.25f,0.1f) },
            // G: like C + horizontal shelf at middle
            new[] { v(0.75f,0.72f), v(0.6f,0.9f), v(0.4f,0.9f), v(0.25f,0.72f), v(0.2f,0.5f),
                    v(0.25f,0.28f), v(0.4f,0.1f), v(0.6f,0.1f), v(0.78f,0.28f), v(0.78f,0.5f),
                    v(0.55f,0.5f) },
            // H: left leg, crossbar, right leg
            new[] { v(0.25f,0.9f), v(0.25f,0.1f), v(0.25f,0.5f), v(0.75f,0.5f),
                    v(0.75f,0.1f), v(0.75f,0.9f) },
            // I: top serif, stem, bottom serif
            new[] { v(0.35f,0.9f), v(0.65f,0.9f), v(0.5f,0.9f), v(0.5f,0.1f),
                    v(0.35f,0.1f), v(0.65f,0.1f) },
            // J: hook on the bottom-left
            new[] { v(0.3f,0.9f), v(0.75f,0.9f), v(0.75f,0.28f), v(0.62f,0.1f),
                    v(0.42f,0.1f), v(0.3f,0.25f) },
            // K: vertical + upper diagonal + lower diagonal (from mid-point)
            new[] { v(0.25f,0.9f), v(0.25f,0.1f), v(0.25f,0.5f), v(0.75f,0.9f),
                    v(0.25f,0.5f), v(0.75f,0.1f) },
            // L: vertical + base
            new[] { v(0.25f,0.9f), v(0.25f,0.1f), v(0.75f,0.1f) },
            // M: two legs + inner V
            new[] { v(0.18f,0.1f), v(0.18f,0.9f), v(0.5f,0.42f), v(0.82f,0.9f), v(0.82f,0.1f) },
            // N: two legs + diagonal
            new[] { v(0.25f,0.1f), v(0.25f,0.9f), v(0.75f,0.1f), v(0.75f,0.9f) },
            // O: closed oval
            new[] { v(0.5f,0.9f), v(0.25f,0.72f), v(0.18f,0.5f), v(0.25f,0.28f), v(0.5f,0.1f),
                    v(0.75f,0.28f), v(0.82f,0.5f), v(0.75f,0.72f), v(0.5f,0.9f) },
            // P: vertical + top bump
            new[] { v(0.25f,0.1f), v(0.25f,0.9f), v(0.6f,0.9f), v(0.75f,0.77f), v(0.75f,0.62f),
                    v(0.6f,0.5f), v(0.25f,0.5f) },
            // Q: oval + tail
            new[] { v(0.5f,0.9f), v(0.25f,0.72f), v(0.18f,0.5f), v(0.25f,0.28f), v(0.5f,0.1f),
                    v(0.6f,0.18f), v(0.82f,0.05f) },
            // R: like P + diagonal leg
            new[] { v(0.25f,0.1f), v(0.25f,0.9f), v(0.6f,0.9f), v(0.75f,0.77f), v(0.75f,0.62f),
                    v(0.6f,0.5f), v(0.25f,0.5f), v(0.6f,0.5f), v(0.78f,0.1f) },
            // S: reverse-Z curve
            new[] { v(0.75f,0.77f), v(0.6f,0.9f), v(0.35f,0.9f), v(0.2f,0.73f), v(0.35f,0.55f),
                    v(0.65f,0.45f), v(0.8f,0.28f), v(0.65f,0.1f), v(0.38f,0.1f), v(0.22f,0.23f) },
            // T: crossbar + vertical stem
            new[] { v(0.2f,0.9f), v(0.8f,0.9f), v(0.5f,0.9f), v(0.5f,0.1f) },
            // U: two legs + bottom arc
            new[] { v(0.25f,0.9f), v(0.25f,0.28f), v(0.35f,0.1f), v(0.65f,0.1f),
                    v(0.75f,0.28f), v(0.75f,0.9f) },
            // V: two diagonals meeting at bottom
            new[] { v(0.18f,0.9f), v(0.5f,0.1f), v(0.82f,0.9f) },
            // W: double V
            new[] { v(0.12f,0.9f), v(0.3f,0.1f), v(0.5f,0.55f), v(0.7f,0.1f), v(0.88f,0.9f) },
            // X: two crossing diagonals (single stroke with backtrack through center)
            new[] { v(0.2f,0.9f), v(0.8f,0.1f), v(0.5f,0.5f), v(0.8f,0.9f), v(0.2f,0.1f) },
            // Y: upper V meeting vertical stem
            new[] { v(0.18f,0.9f), v(0.5f,0.5f), v(0.82f,0.9f), v(0.5f,0.5f), v(0.5f,0.1f) },
            // Z: top bar, diagonal, bottom bar
            new[] { v(0.2f,0.9f), v(0.8f,0.9f), v(0.2f,0.1f), v(0.8f,0.1f) },
        };

        /// <summary>Returns the placeholder polyline for the given letter index (0=A, 25=Z).</summary>
        public static Vector2[] Get(int letterIndex)
        {
            if (letterIndex < 0 || letterIndex >= Templates.Length)
                return new[] { v(0.2f,0.5f), v(0.8f,0.5f) };
            return Templates[letterIndex];
        }

        private static Vector2 v(float x, float y) => new Vector2(x, y);
    }
}
