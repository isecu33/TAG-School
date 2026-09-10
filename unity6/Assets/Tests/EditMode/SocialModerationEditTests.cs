using NUnit.Framework;
using PieceBook.Social.Moderation;

namespace PieceBook.Tests.EditMode
{
    /// <summary>EditMode tests for the gallery moderation gate (Fase 4 core, SOC-06, §10).</summary>
    public class SocialModerationEditTests
    {
        [Test]
        public void Submitted_NotVisibleUntilApproved()
        {
            var gate = new GalleryModerationGate();
            gate.Submit("art1");
            Assert.AreEqual(ModerationState.Pending, gate.GetState("art1"));
            Assert.IsFalse(gate.IsVisible("art1", "hashA"), "pending art must not show");

            gate.Approve("art1");
            Assert.IsTrue(gate.IsVisible("art1", "hashA"), "approved art shows");
        }

        [Test]
        public void Rejected_NeverVisible()
        {
            var gate = new GalleryModerationGate();
            gate.Submit("art2");
            gate.Reject("art2");
            Assert.IsFalse(gate.IsVisible("art2", "hashB"));
        }

        [Test]
        public void ReportedHash_HidesEvenIfApproved()
        {
            var gate = new GalleryModerationGate();
            gate.Submit("art3");
            gate.Approve("art3");
            Assert.IsTrue(gate.IsVisible("art3", "hashC"));

            gate.ReportHash("hashC");
            Assert.IsFalse(gate.IsVisible("art3", "hashC"), "reported hash is hidden automatically (§10)");
            Assert.IsTrue(gate.IsReported("hashC"));
        }

        [Test]
        public void UnknownArtwork_NotVisible()
        {
            var gate = new GalleryModerationGate();
            Assert.AreEqual(ModerationState.Pending, gate.GetState("never_submitted"));
            Assert.IsFalse(gate.IsVisible("never_submitted", "h"));
        }
    }
}
