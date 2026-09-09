using NUnit.Framework;
using PieceBook.DrawingEngine;
using UnityEngine;

namespace PieceBook.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the Fase 1 engine hardening that is pure logic: the tile dirty tracker
    /// (ENG-06). Multilayer compositing (ENG-05) and the tool Strategy visual difference (ENG-07)
    /// are GPU paths, verified by PlayMode/in-editor checks, not here.
    /// </summary>
    public class EngineHardeningEditTests
    {
        [Test]
        public void TileTracker_MarksOnlyOverlappedTiles()
        {
            var t = new TileDirtyTracker(10);
            t.MarkDisc(new Vector2(0.5f, 0.5f), 0.05f); // x,y ∈ [0.45,0.55] → tiles 4..5 on each axis

            Assert.AreEqual(4, t.DirtyTiles.Count);
            Assert.IsTrue(t.IsDirty(4, 4));
            Assert.IsTrue(t.IsDirty(5, 5));
            Assert.IsFalse(t.IsDirty(0, 0), "A far tile must not be dirty.");
        }

        [Test]
        public void TileTracker_DirtyListIsUnique()
        {
            var t = new TileDirtyTracker(8);
            var c = new Vector2(0.5f, 0.5f);
            t.MarkDisc(c, 0.01f);
            t.MarkDisc(c, 0.01f); // same tile again
            Assert.AreEqual(1, t.DirtyTiles.Count, "Marking the same tile twice must not duplicate it.");
        }

        [Test]
        public void TileTracker_Reset_Clears()
        {
            var t = new TileDirtyTracker(8);
            t.MarkDisc(new Vector2(0.5f, 0.5f), 0.1f);
            Assert.Greater(t.DirtyTiles.Count, 0);
            t.Reset();
            Assert.AreEqual(0, t.DirtyTiles.Count);
            Assert.IsFalse(t.IsDirty(4, 4));
        }

        [Test]
        public void TileTracker_ClampsAtCanvasEdges()
        {
            var t = new TileDirtyTracker(10);
            t.MarkDisc(new Vector2(0.99f, 0.99f), 0.05f); // spills past 1.0 → must clamp to tile 9
            Assert.IsTrue(t.IsDirty(9, 9));
            Assert.AreEqual(1, t.DirtyTiles.Count);
        }
    }
}
