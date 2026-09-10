using PieceBook.CitySim.Data;
using UnityEngine;

namespace PieceBook.CitySim.World
{
    /// <summary>
    /// Scene-view debug overlay for the CitySim gray-box (Gizmos only — no runtime cost in a
    /// build, since OnDrawGizmos is editor-only). Reads a <see cref="ZoneDef"/> and draws the
    /// street graph (nodes + edges), the patrol waypoint loop, building footprints, surface
    /// facing arrows and each patrol's vision range. The single most useful aid for demoing /
    /// debugging a graph-navigated stealth spike (audit WP2/G4).
    /// </summary>
    public sealed class CitySimGizmos : MonoBehaviour
    {
        public ZoneDef zone;
        [Tooltip("Also draw when the object is not selected.")]
        public bool alwaysDraw = true;

        private const float BuildingHeight = 2.6f;

        private void OnDrawGizmos() { if (alwaysDraw) Draw(); }
        private void OnDrawGizmosSelected() { if (!alwaysDraw) Draw(); }

        private void Draw()
        {
            if (zone == null || zone.streetNodes == null) return;

            // Edges
            Gizmos.color = new Color(0.4f, 0.6f, 0.8f, 0.6f);
            if (zone.streetEdges != null)
                foreach (var e in zone.streetEdges)
                {
                    if (!InRange(e.a) || !InRange(e.b)) continue;
                    Gizmos.DrawLine(zone.NodeToWorld(e.a), zone.NodeToWorld(e.b));
                }

            // Nodes
            Gizmos.color = Color.cyan;
            for (int i = 0; i < zone.streetNodes.Length; i++)
                Gizmos.DrawSphere(zone.NodeToWorld(i) + Vector3.up * 0.05f, 0.14f);

            // Building footprints
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.7f);
            if (zone.buildings != null)
                foreach (var b in zone.buildings)
                    Gizmos.DrawWireCube(
                        new Vector3(b.center.x, BuildingHeight * 0.5f, b.center.y),
                        new Vector3(b.width, BuildingHeight, b.height));

            // Surface facing arrows
            Gizmos.color = new Color(0.6f, 1f, 0.5f);
            if (zone.surfaces != null)
                foreach (var s in zone.surfaces)
                {
                    if (s == null) continue;
                    Vector3 p = new Vector3(s.position.x, 0.8f, s.position.y);
                    Vector3 dir = new Vector3(s.facing.x, 0f, s.facing.y).normalized;
                    Gizmos.DrawLine(p, p + dir * 1.1f);
                    Gizmos.DrawSphere(p + dir * 1.1f, 0.08f);
                }

            // Patrol waypoint loop + vision range
            if (zone.patrolRoutes != null)
                foreach (var patrol in zone.patrolRoutes)
                {
                    if (patrol == null || patrol.waypoints == null || patrol.waypoints.Length == 0) continue;

                    Gizmos.color = new Color(1f, 0.4f, 0.7f);
                    for (int i = 0; i < patrol.waypoints.Length; i++)
                    {
                        int a = patrol.waypoints[i];
                        int b = patrol.waypoints[(i + 1) % patrol.waypoints.Length];
                        if (!InRange(a) || !InRange(b)) continue;
                        Vector3 pa = zone.NodeToWorld(a) + Vector3.up * 0.12f;
                        Vector3 pb = zone.NodeToWorld(b) + Vector3.up * 0.12f;
                        Gizmos.DrawLine(pa, pb);
                        Gizmos.DrawCube(pa, Vector3.one * 0.18f);
                    }

                    if (InRange(patrol.waypoints[0]))
                        DrawWireDisc(zone.NodeToWorld(patrol.waypoints[0]),
                                     patrol.visionCone.range, new Color(1f, 0.35f, 0.35f, 0.5f));
                }
        }

        private static void DrawWireDisc(Vector3 center, float radius, Color color, int seg = 32)
        {
            Gizmos.color = color;
            Vector3 prev = center + new Vector3(radius, 0.05f, 0f);
            for (int i = 1; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                Vector3 cur = center + new Vector3(Mathf.Cos(a) * radius, 0.05f, Mathf.Sin(a) * radius);
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }
        }

        private bool InRange(int i) => i >= 0 && i < zone.streetNodes.Length;
    }
}
