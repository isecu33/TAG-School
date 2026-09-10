using UnityEngine;

namespace PieceBook.ARModule
{
    /// <summary>A detected vertical wall plane (ARQUITECTURA §6 "planos verticales").</summary>
    public readonly struct ArPlaneInfo
    {
        public readonly string Id;
        public readonly Vector3 Center;
        public readonly Vector2 Size;
        public readonly bool IsVertical;
        public ArPlaneInfo(string id, Vector3 center, Vector2 size, bool isVertical)
        { Id = id; Center = center; Size = size; IsVertical = isVertical; }
    }

    /// <summary>An anchored placement of the artwork on a wall (position/rotation/scale).</summary>
    public struct ArPlacement
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Scale;
        public static ArPlacement Default => new ArPlacement { Rotation = Quaternion.identity, Scale = 1f };
    }
}
