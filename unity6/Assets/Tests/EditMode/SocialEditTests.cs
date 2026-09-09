using NUnit.Framework;
using PieceBook.Social.Export;
using PieceBook.Social.Mockups;
using PieceBook.Social.Share;
using UnityEngine;

namespace PieceBook.Tests.EditMode
{
    /// <summary>EditMode tests for the Social module (Fase 2: SOC-02..05). CPU pixel ops + pure math.</summary>
    public class SocialEditTests
    {
        private static Texture2D SolidTexture(int size, Color c)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            tex.SetPixels(px);
            tex.Apply(false);
            return tex;
        }

        // ---- SOC-02: PNG export -------------------------------------------

        [Test]
        public void PngExporter_ProducesValidPng()
        {
            var tex = SolidTexture(8, Color.red);
            var png = PngExporter.EncodePng(tex);

            Assert.IsNotNull(png);
            Assert.Greater(png.Length, 8);
            // PNG signature: 137 80 78 71 13 10 26 10
            Assert.AreEqual(137, png[0]);
            Assert.AreEqual(80, png[1]);
            Assert.AreEqual(78, png[2]);
            Assert.AreEqual(71, png[3]);

            var back = new Texture2D(2, 2);
            Assert.IsTrue(back.LoadImage(png), "Exported PNG must decode.");
            Assert.AreEqual(8, back.width);
            Assert.AreEqual(8, back.height);
            Object.DestroyImmediate(tex); Object.DestroyImmediate(back);
        }

        [Test]
        public void PngExporter_IsDeterministic()
        {
            var a = PngExporter.EncodePng(SolidTexture(8, Color.green));
            var b = PngExporter.EncodePng(SolidTexture(8, Color.green));
            CollectionAssert.AreEqual(a, b, "Same pixels must encode to the same PNG bytes.");
        }

        // ---- SOC-04: watermark --------------------------------------------

        [Test]
        public void Watermark_MarksBottomBar_LeavesTopUntouched()
        {
            var tex = SolidTexture(64, Color.white);
            Watermark.ApplyDefault(tex, premium: false);

            var bottom = tex.GetPixel(32, 1);      // inside the bar
            var top = tex.GetPixel(32, 63);        // above the bar
            Assert.Less(bottom.r, 0.99f, "Watermark must darken the bottom bar.");
            Assert.AreEqual(1f, top.r, 0.001f, "Area above the bar must be untouched.");
            Object.DestroyImmediate(tex);
        }

        [Test]
        public void Watermark_Premium_SkipsMark()
        {
            var tex = SolidTexture(64, Color.white);
            Watermark.ApplyDefault(tex, premium: true);
            Assert.AreEqual(1f, tex.GetPixel(32, 1).r, 0.001f, "Premium users get no watermark.");
            Object.DestroyImmediate(tex);
        }

        // ---- SOC-03: share ------------------------------------------------

        private sealed class MockShare : INativeShare
        {
            public string Path, Mime, Message;
            public void Share(string filePath, string mimeType, string message)
            { Path = filePath; Mime = mimeType; Message = message; }
        }

        [Test]
        public void ShareService_PicksCorrectMimeTypes()
        {
            var mock = new MockShare();
            var svc = new ShareService(mock);

            svc.ShareImage("/tmp/a.png", "look");
            Assert.AreEqual(ShareService.MimePng, mock.Mime);
            Assert.AreEqual("/tmp/a.png", mock.Path);

            svc.ShareVideo("/tmp/a.mp4");
            Assert.AreEqual(ShareService.MimeMp4, mock.Mime);
        }

        // ---- SOC-05: mockup perspective -----------------------------------

        [Test]
        public void MockupComposite_MapsCornersAndCenter()
        {
            var bl = new Vector2(0.1f, 0.2f);
            var br = new Vector2(0.9f, 0.25f);
            var tl = new Vector2(0.15f, 0.8f);
            var tr = new Vector2(0.85f, 0.78f);

            Assert.AreEqual(bl, MockupComposite.MapPoint(new Vector2(0, 0), bl, br, tl, tr));
            Assert.AreEqual(br, MockupComposite.MapPoint(new Vector2(1, 0), bl, br, tl, tr));
            Assert.AreEqual(tl, MockupComposite.MapPoint(new Vector2(0, 1), bl, br, tl, tr));
            Assert.AreEqual(tr, MockupComposite.MapPoint(new Vector2(1, 1), bl, br, tl, tr));

            var center = MockupComposite.MapPoint(new Vector2(0.5f, 0.5f), bl, br, tl, tr);
            var expected = (bl + br + tl + tr) * 0.25f;
            Assert.That(Vector2.Distance(center, expected), Is.LessThan(1e-5f));
        }
    }
}
