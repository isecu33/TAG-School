using UnityEngine;

namespace PieceBook.CitySim.Data
{
    /// <summary>
    /// Paintable surface catalog entry (ARQUITECTURA §9):
    ///   SurfaceDef {id, zoneId, size, texture, isSafeWall (spot ganado), position}
    /// A "safe wall" is a won spot: paint without pressure/time/police (GD-02 §1). The
    /// spike uses street surfaces (isSafeWall = false) to validate the risk loop.
    /// </summary>
    [CreateAssetMenu(menuName = "PieceBook/CitySim/Surface Definition", fileName = "Surface_New")]
    public sealed class SurfaceDef : ScriptableObject
    {
        public string id = "surface_new";
        public string zoneId = "zone_polygon";

        [Tooltip("World-space footprint (width, height) of the wall in units.")]
        public Vector2 size = new Vector2(2.4f, 2.0f);

        [Tooltip("XZ position of the surface's base in the zone (texture is placeholder gray).")]
        public Vector2 position = Vector2.zero;

        [Tooltip("Facing direction on the XZ plane (unit vector) the wall paints toward.")]
        public Vector2 facing = new Vector2(0f, -1f);

        [Tooltip("A won spot: paint with no risk (GD-02). False for the risky street surfaces.")]
        public bool isSafeWall = false;

        [Tooltip("Fame reward for a clean piece here (informational for the spike).")]
        public int fame = 100;
    }
}
