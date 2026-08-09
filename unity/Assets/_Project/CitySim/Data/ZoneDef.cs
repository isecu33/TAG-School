using System;
using UnityEngine;

namespace PieceBook.CitySim.Data
{
    /// <summary>Day/night behaviour (ARQUITECTURA §9 ZoneDef.dayNightProfile).
    /// Declared for data fidelity; the spike does NOT implement the day/night cycle
    /// (out of scope per the brief's NO HACER), so these values are informational.</summary>
    [Serializable]
    public struct DayNightProfile
    {
        public int nightSurveillanceDelta;   // e.g. -1 at night (GD-02 §3)
        public float dayWindowScale;
        public float nightWindowScale;

        public static DayNightProfile Default => new DayNightProfile
        {
            nightSurveillanceDelta = -1,
            dayWindowScale = 1f,
            nightWindowScale = 0.7f
        };
    }

    /// <summary>An undirected street-graph edge, by node index.</summary>
    [Serializable]
    public struct StreetEdge
    {
        public int a;
        public int b;
        public StreetEdge(int a, int b) { this.a = a; this.b = b; }
    }

    /// <summary>
    /// Zone catalog entry (ARQUITECTURA §9):
    ///   ZoneDef {id, name, surveillance (1-5), fameMultiplier, dayNightProfile,
    ///            patrolRoutes[], hideSpots[], unlockFame}
    /// Extended for the spike with the actual street-graph layout (nodes/edges) and the
    /// surface list, so a designer can reshape the map, routes and hideouts with no code.
    /// </summary>
    [CreateAssetMenu(menuName = "PieceBook/CitySim/Zone Definition", fileName = "Zone_New")]
    public sealed class ZoneDef : ScriptableObject
    {
        [Header("§9 core")]
        public string id = "zone_polygon";
        public string displayName = "El Polígono";
        [Range(1, 5)] public int surveillance = 1;
        public float fameMultiplier = 1f;
        public int unlockFame = 0;
        public DayNightProfile dayNightProfile = DayNightProfile.Default;

        [Header("Patrols & hideouts (§9)")]
        public PatrolDef[] patrolRoutes = new PatrolDef[0];
        [Tooltip("Container hideout positions on the XZ plane (GD-02 §3).")]
        public Vector2[] hideSpots = new Vector2[0];

        [Header("Street graph (spike map — grafo, NO navmesh)")]
        [Tooltip("Street-graph node positions on the XZ plane (~10 for the spike).")]
        public Vector2[] streetNodes = new Vector2[0];
        public StreetEdge[] streetEdges = new StreetEdge[0];

        [Header("Blockout & surfaces (spike)")]
        [Tooltip("Building block footprints (center xz, size xz) — pure gray occluders.")]
        public Rect[] buildings = new Rect[0];
        public SurfaceDef[] surfaces = new SurfaceDef[0];

        public Vector3 NodeToWorld(int i)
        {
            var n = streetNodes[i];
            return new Vector3(n.x, 0f, n.y);
        }
    }
}
