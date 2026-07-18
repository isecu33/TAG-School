using System.Collections.Generic;
using UnityEngine;

namespace PieceBook.CitySim.Graph
{
    /// <summary>
    /// Runtime street graph built from a ZoneDef's nodes + edges. Patrols navigate this
    /// graph (waypoints are node indices) — ARQUITECTURA §7: "navegación por grafo de
    /// calles 2D", explicitly NOT a 3D navmesh. Provides Dijkstra shortest paths so a
    /// patrol can route between non-adjacent waypoints along real streets.
    /// </summary>
    public sealed class StreetGraph
    {
        private readonly Vector3[] _nodes;              // world positions (XZ plane)
        private readonly List<int>[] _adj;              // adjacency list
        public int NodeCount => _nodes.Length;

        public StreetGraph(Vector2[] nodes2D, IList<Data.StreetEdge> edges)
        {
            _nodes = new Vector3[nodes2D.Length];
            for (int i = 0; i < nodes2D.Length; i++)
                _nodes[i] = new Vector3(nodes2D[i].x, 0f, nodes2D[i].y);

            _adj = new List<int>[_nodes.Length];
            for (int i = 0; i < _adj.Length; i++) _adj[i] = new List<int>(4);

            for (int e = 0; e < edges.Count; e++)
            {
                int a = edges[e].a, b = edges[e].b;
                if (!InRange(a) || !InRange(b) || a == b) continue;
                if (!_adj[a].Contains(b)) _adj[a].Add(b);
                if (!_adj[b].Contains(a)) _adj[b].Add(a);
            }
        }

        public Vector3 Position(int node) => _nodes[node];
        public IReadOnlyList<int> Neighbors(int node) => _adj[node];

        private bool InRange(int i) => i >= 0 && i < _nodes.Length;
        private float Cost(int a, int b) => Vector3.Distance(_nodes[a], _nodes[b]);

        /// <summary>Dijkstra shortest path (inclusive of both ends). Empty if unreachable.</summary>
        public List<int> ShortestPath(int from, int to)
        {
            var result = new List<int>();
            if (!InRange(from) || !InRange(to)) return result;
            if (from == to) { result.Add(from); return result; }

            int n = _nodes.Length;
            var dist = new float[n];
            var prev = new int[n];
            var done = new bool[n];
            for (int i = 0; i < n; i++) { dist[i] = float.MaxValue; prev[i] = -1; }
            dist[from] = 0f;

            // n is tiny (~10) for the spike; a linear scan beats a heap in complexity/clarity.
            for (int it = 0; it < n; it++)
            {
                int u = -1;
                float best = float.MaxValue;
                for (int i = 0; i < n; i++)
                    if (!done[i] && dist[i] < best) { best = dist[i]; u = i; }
                if (u < 0) break;
                done[u] = true;
                if (u == to) break;

                var neigh = _adj[u];
                for (int k = 0; k < neigh.Count; k++)
                {
                    int v = neigh[k];
                    float nd = dist[u] + Cost(u, v);
                    if (nd < dist[v]) { dist[v] = nd; prev[v] = u; }
                }
            }

            if (prev[to] == -1 && to != from) return result; // unreachable
            for (int at = to; at != -1; at = prev[at]) result.Add(at);
            result.Reverse();
            return result;
        }

        /// <summary>
        /// Expand a waypoint loop (node indices) into the full ordered node route the patrol
        /// walks, following shortest paths between consecutive waypoints and closing the loop.
        /// </summary>
        public List<int> BuildLoopRoute(int[] waypoints)
        {
            var route = new List<int>(32);
            if (waypoints == null || waypoints.Length == 0) return route;
            if (waypoints.Length == 1) { route.Add(waypoints[0]); return route; }

            for (int i = 0; i < waypoints.Length; i++)
            {
                int a = waypoints[i];
                int b = waypoints[(i + 1) % waypoints.Length];
                var seg = ShortestPath(a, b);
                // Avoid duplicating the shared endpoint between consecutive segments.
                int start = route.Count > 0 ? 1 : 0;
                for (int s = start; s < seg.Count; s++) route.Add(seg[s]);
            }
            // Drop the trailing node if it equals the first (closed loop), keeps stepping clean.
            if (route.Count > 1 && route[route.Count - 1] == route[0]) route.RemoveAt(route.Count - 1);
            return route;
        }
    }
}
