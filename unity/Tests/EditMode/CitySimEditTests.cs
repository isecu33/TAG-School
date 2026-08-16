using System.Collections.Generic;
using NUnit.Framework;
using PieceBook.CitySim.AI;
using PieceBook.CitySim.Data;
using PieceBook.CitySim.Graph;
using PieceBook.CitySim.Loop;
using PieceBook.CitySim.World;
using UnityEngine;

namespace PieceBook.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for CitySim pure logic (audit WP4): street-graph pathfinding, the
    /// vision sensor geometry, the zone blackboard, window urgency, and the default layout.
    /// No scene/GPU needed — these are the deterministic units the spike relies on.
    /// </summary>
    public sealed class CitySimEditTests
    {
        private static StreetGraph SmallGraph()
        {
            // 0-1-2 line, 0-3 branch, 4 isolated (unreachable).
            var nodes = new[]
            {
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(2, 0),
                new Vector2(0, 1), new Vector2(9, 9),
            };
            var edges = new List<StreetEdge>
            {
                new StreetEdge(0, 1), new StreetEdge(1, 2), new StreetEdge(0, 3),
            };
            return new StreetGraph(nodes, edges);
        }

        [Test]
        public void ShortestPath_FindsExpectedRoute()
        {
            var g = SmallGraph();
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, g.ShortestPath(0, 2));
            CollectionAssert.AreEqual(new[] { 3, 0, 1, 2 }, g.ShortestPath(3, 2));
        }

        [Test]
        public void ShortestPath_Unreachable_IsEmpty()
        {
            var g = SmallGraph();
            Assert.IsEmpty(g.ShortestPath(0, 4));
        }

        [Test]
        public void BuildLoopRoute_YieldsAdjacentClosedLoop()
        {
            var g = SmallGraph();
            var route = g.BuildLoopRoute(new[] { 0, 2, 3 });
            Assert.GreaterOrEqual(route.Count, 3);
            // Every consecutive pair (including wrap-around) must be a real graph edge.
            for (int i = 0; i < route.Count; i++)
            {
                int a = route[i];
                int b = route[(i + 1) % route.Count];
                CollectionAssert.Contains((IEnumerable<int>)g.Neighbors(a), b,
                    $"route step {a}->{b} is not an edge");
            }
        }

        [Test]
        public void VisionSensor_RespectsRange_Angle_And_Hidden()
        {
            var cone = new VisionCone(90f, 10f);
            Vector3 eye = Vector3.zero, fwd = Vector3.forward;

            Assert.IsTrue(VisionSensor.CanSee(eye, fwd, cone, new Vector3(0, 0, 5), false, out float d));
            Assert.AreEqual(5f, d, 0.01f);

            Assert.IsFalse(VisionSensor.CanSee(eye, fwd, cone, new Vector3(0, 0, 5), true, out _), "hidden");
            Assert.IsFalse(VisionSensor.CanSee(eye, fwd, cone, new Vector3(0, 0, 20), false, out _), "out of range");
            Assert.IsFalse(VisionSensor.CanSee(eye, fwd, cone, new Vector3(0, 0, -5), false, out _), "behind");
        }

        [Test]
        public void ZoneBlackboard_Heat_Raises_Decays_Resets()
        {
            var bb = new ZoneBlackboard(null); // null EventBus is supported (no publish)
            bb.RaiseHeat(0.5f);
            Assert.AreEqual(0.5f, bb.Heat, 1e-4f);

            bb.Decay(1f, 0.2f);
            Assert.AreEqual(0.3f, bb.Heat, 1e-4f);

            bb.ReportSighting(new Vector3(2, 0, 3));
            Assert.IsTrue(bb.HasLastKnownPlayerPos);
            Assert.AreEqual(new Vector3(2, 0, 3), bb.LastKnownPlayerPos);
            Assert.AreEqual(1f, bb.Heat, 1e-4f);

            bb.Reset();
            Assert.AreEqual(0f, bb.Heat, 1e-4f);
            Assert.IsFalse(bb.HasLastKnownPlayerPos);
        }

        [Test]
        public void PaintingWindow_Urgency_Thresholds()
        {
            Assert.AreEqual(new Color(0.4f, 0.9f, 0.4f), PaintingWindow.Urgency(0.8f)); // green
            Assert.AreEqual(new Color(1f, 0.85f, 0.3f), PaintingWindow.Urgency(0.35f)); // amber
            Assert.AreEqual(new Color(1f, 0.35f, 0.35f), PaintingWindow.Urgency(0.1f)); // red
        }

        [Test]
        public void AlertProfile_Default_IsCoherent()
        {
            var ap = AlertProfile.Default;
            Assert.Less(ap.suspectThreshold, ap.chaseThreshold);
            Assert.Greater(ap.suspicionBuildRate, 0f);
            Assert.GreaterOrEqual(ap.chaseSpeedMultiplier, 1f);
        }

        [Test]
        public void CityLayout_Default_HasExpectedShape()
        {
            var zone = CityLayout.BuildDefault();
            Assert.AreEqual(10, zone.streetNodes.Length);
            Assert.AreEqual(3, zone.surfaces.Length);
            Assert.AreEqual(2, zone.hideSpots.Length);
            Assert.AreEqual(1, zone.patrolRoutes.Length);
            Assert.AreEqual(4, zone.patrolRoutes[0].waypoints.Length);
        }
    }
}
