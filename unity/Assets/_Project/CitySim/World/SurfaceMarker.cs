using PieceBook.CitySim.Data;
using UnityEngine;

namespace PieceBook.CitySim.World
{
    /// <summary>
    /// A paintable street surface in the world (from a <see cref="SurfaceDef"/>). Holds the
    /// spot the player stands to paint and the anchor where the painting-window ring is shown.
    /// </summary>
    public sealed class SurfaceMarker : MonoBehaviour
    {
        public SurfaceDef Def { get; private set; }

        /// <summary>Where the player must stand to start painting (in front of the wall).</summary>
        public Vector3 PaintStand { get; private set; }

        /// <summary>World anchor for the window ring / prompt, above the wall.</summary>
        public Transform RingAnchor => transform;

        public void Configure(SurfaceDef def)
        {
            Def = def;
            var facing = new Vector3(def.facing.x, 0f, def.facing.y).normalized;
            PaintStand = transform.position + facing * 1.1f;
        }
    }
}
