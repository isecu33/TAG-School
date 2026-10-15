using UnityEngine;

namespace PieceBook.Content.Editor
{
    /// <summary>
    /// Bubble (throw-up) placeholder letters: the handstyle skeleton rounded with Chaikin corner
    /// cutting, so the trace path has the soft, inflated curves of a bubble letter. Same algorithm
    /// as the web demo build (demo-web/build.mjs), so both score against identical templates.
    /// Real bubble outlines replace these with art (CNT-02); the data contract is unchanged.
    /// </summary>
    public static class BubbleLetterTemplates
    {
        public const int Iterations = 3;

        public static Vector2[] Get(int letterIndex) => Chaikin(HandstyleLetterTemplates.Get(letterIndex), Iterations);

        /// <summary>Chaikin corner cutting that keeps both endpoints (an open stroke).</summary>
        public static Vector2[] Chaikin(Vector2[] pts, int iterations)
        {
            var current = pts;
            for (int it = 0; it < iterations && current.Length > 2; it++)
            {
                var next = new Vector2[(current.Length - 1) * 2 + 2];
                int k = 0;
                next[k++] = current[0];
                for (int i = 0; i < current.Length - 1; i++)
                {
                    next[k++] = Vector2.Lerp(current[i], current[i + 1], 0.25f);
                    next[k++] = Vector2.Lerp(current[i], current[i + 1], 0.75f);
                }
                next[k] = current[current.Length - 1];
                current = next;
            }
            return current;
        }
    }
}
