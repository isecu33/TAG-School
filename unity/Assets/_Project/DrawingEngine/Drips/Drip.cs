using UnityEngine;

namespace PieceBook.DrawingEngine.Drips
{
    /// <summary>
    /// One procedural paint run (ENG-04). Pooled — never allocated during a stroke.
    /// A drip is a small blob that accelerates downward under gravity, leaving a
    /// tapering trail of stamps until it exhausts its budget, then returns to the pool.
    /// </summary>
    public sealed class Drip
    {
        public Vector2 Pos;        // normalized canvas space
        public float Velocity;     // downward speed (canvas units / s)
        public float Remaining;    // trail length left to lay down (canvas units)
        public float Radius;       // current head radius (normalized)
        public Color Color;
        public bool Alive;

        public void Init(Vector2 start, float length, float radius, Color color)
        {
            Pos = start;
            Velocity = 0.02f;      // small initial nudge
            Remaining = length;
            Radius = radius;
            Color = color;
            Alive = true;
        }

        /// <summary>
        /// Advance the drip by dt. Returns true while it still has trail to lay down.
        /// The caller stamps at <see cref="Pos"/> after each step.
        /// </summary>
        public bool Step(float dt, float gravity)
        {
            Velocity += gravity * dt;
            float ds = Velocity * dt;
            Pos.y -= ds;                 // gravity pulls toward the bottom of the canvas
            Remaining -= ds;
            Radius *= 0.995f;            // taper as it runs out of paint

            if (Remaining <= 0f || Pos.y <= 0f || Radius <= 0.0008f)
            {
                Alive = false;
                return false;
            }
            return true;
        }
    }
}
