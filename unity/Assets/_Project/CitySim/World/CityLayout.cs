using PieceBook.CitySim.Data;
using UnityEngine;

namespace PieceBook.CitySim.World
{
    /// <summary>
    /// Default "El Polígono" grey layout for the spike (GD-02 §3 tutorial zone: surveillance 1).
    /// Single source of truth for both the runtime fallback (SpikeBBootstrap) and the editor
    /// asset generator (SpikeBSetup): a ~10-node street grid, 4 building occluders, 3 street
    /// surfaces and 2 container hideouts, plus one perimeter patrol. Everything data-driven (§9).
    /// </summary>
    public static class CityLayout
    {
        private static Rect RectCS(float cx, float cz, float w, float h)
            => new Rect(cx - w * 0.5f, cz - h * 0.5f, w, h);

        public static SurfaceDef[] BuildSurfaces()
        {
            return new[]
            {
                Surface("surface_s1", new Vector2(-2f, 0.8f), new Vector2(0f, -1f)),
                Surface("surface_s2", new Vector2(2f, -0.8f), new Vector2(0f, 1f)),
                Surface("surface_s3", new Vector2(0.8f, 2f), new Vector2(-1f, 0f)),
            };
        }

        private static SurfaceDef Surface(string id, Vector2 pos, Vector2 facing)
        {
            var s = ScriptableObject.CreateInstance<SurfaceDef>();
            s.name = id;
            s.id = id;
            s.zoneId = "zone_polygon";
            s.position = pos;
            s.facing = facing;
            s.size = new Vector2(2.2f, 2.0f);
            s.isSafeWall = false;
            s.fame = 100;
            return s;
        }

        public static PatrolDef BuildPatrol()
        {
            var p = ScriptableObject.CreateInstance<PatrolDef>();
            p.name = "patrol_polygon_loop";
            p.id = "patrol_polygon_loop";
            p.waypoints = new[] { 0, 2, 8, 6 };   // perimeter of the block
            p.speed = 2.2f;
            p.waypointPause = 0.5f;
            p.visionCone = new VisionCone(70f, 7f);
            p.alertProfile = AlertProfile.Default;
            return p;
        }

        public static ZoneDef BuildZone(PatrolDef patrol, SurfaceDef[] surfaces)
        {
            var z = ScriptableObject.CreateInstance<ZoneDef>();
            z.name = "zone_polygon";
            z.id = "zone_polygon";
            z.displayName = "El Polígono";
            z.surveillance = 1;
            z.fameMultiplier = 1f;
            z.unlockFame = 0;
            z.dayNightProfile = DayNightProfile.Default;

            z.streetNodes = new[]
            {
                new Vector2(-4f, -4f), new Vector2(0f, -4f), new Vector2(4f, -4f), // 0,1,2
                new Vector2(-4f,  0f), new Vector2(0f,  0f), new Vector2(4f,  0f), // 3,4,5
                new Vector2(-4f,  4f), new Vector2(0f,  4f), new Vector2(4f,  4f), // 6,7,8
                new Vector2( 8f,  0f),                                              // 9
            };
            z.streetEdges = new[]
            {
                new StreetEdge(0,1), new StreetEdge(1,2),
                new StreetEdge(3,4), new StreetEdge(4,5),
                new StreetEdge(6,7), new StreetEdge(7,8),
                new StreetEdge(0,3), new StreetEdge(3,6),
                new StreetEdge(1,4), new StreetEdge(4,7),
                new StreetEdge(2,5), new StreetEdge(5,8),
                new StreetEdge(5,9),
            };
            z.buildings = new[]
            {
                RectCS(-2f, -2f, 2.4f, 2.4f), RectCS(2f, -2f, 2.4f, 2.4f),
                RectCS(-2f,  2f, 2.4f, 2.4f), RectCS(2f,  2f, 2.4f, 2.4f),
            };
            z.hideSpots = new[] { new Vector2(-4f, -1f), new Vector2(4f, 1f) };

            z.patrolRoutes = new[] { patrol };
            z.surfaces = surfaces;
            return z;
        }

        /// <summary>Convenience: a complete in-memory zone for the zero-setup runtime fallback.</summary>
        public static ZoneDef BuildDefault()
        {
            var surfaces = BuildSurfaces();
            var patrol = BuildPatrol();
            return BuildZone(patrol, surfaces);
        }
    }
}
