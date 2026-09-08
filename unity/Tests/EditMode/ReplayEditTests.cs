using System.Collections.Generic;
using NUnit.Framework;
using PieceBook.DrawingEngine;
using PieceBook.Social.Replay;
using UnityEngine;

namespace PieceBook.Tests.EditMode
{
    /// <summary>EditMode tests for the offline replay pipeline (Fase 2: SOC-01). Deterministic core.</summary>
    public class ReplayEditTests
    {
        private static RecordedStroke Stroke(float lastSampleTime)
        {
            var s = new RecordedStroke();
            s.Reset("cap_soft", "paint_default", Color.white);
            s.Samples.Add(new StrokeSample { Pos = new Vector2(0.1f, 0.1f), Pressure = 1f, Time = 0f });
            s.Samples.Add(new StrokeSample { Pos = new Vector2(0.5f, 0.5f), Pressure = 1f, Time = lastSampleTime });
            return s;
        }

        [Test]
        public void Timeline_Duration_SumsStrokesPlusGaps()
        {
            var rec = new StrokeRecording();
            rec.Strokes.Add(Stroke(1.0f));
            rec.Strokes.Add(Stroke(0.5f));
            // 1.0 + gap(0.15) + 0.5
            Assert.That(ReplayTimeline.Duration(rec), Is.EqualTo(1.65f).Within(1e-4f));
        }

        [Test]
        public void Timeline_FrameCount_IsDeterministic()
        {
            Assert.AreEqual(51, ReplayTimeline.FrameCount(1.65f, 30));
            Assert.AreEqual(1, ReplayTimeline.FrameCount(0f, 30), "Empty recording still yields one frame.");
            CollectionAssert.AreEqual(
                ReplayTimeline.FrameTimes(1.65f, 30),
                ReplayTimeline.FrameTimes(1.65f, 30),
                "Frame plan must be reproducible.");
        }

        private sealed class FakeSource : IReplaySource
        {
            public float Duration { get; set; }
            public readonly List<float> RenderedAt = new List<float>();
            public byte[] RenderAt(float t) { RenderedAt.Add(t); return new byte[] { 1, 2, 3 }; }
        }

        private sealed class CountingEncoder : IVideoEncoder
        {
            public int Frames; public int Width, Height, Fps;
            public void Begin(int w, int h, int fps) { Width = w; Height = h; Fps = fps; Frames = 0; }
            public void AddFrame(byte[] f) { Frames++; }
            public string End() => "/fake/out";
        }

        [Test]
        public void Renderer_EmitsOneFramePerTimelineTick()
        {
            var src = new FakeSource { Duration = 1.65f };
            var enc = new CountingEncoder();
            var outPath = ReplayRenderer.Render(src, enc, fps: 30, width: 1024, height: 1024);

            Assert.AreEqual("/fake/out", outPath);
            Assert.AreEqual(ReplayTimeline.FrameCount(1.65f, 30), enc.Frames);
            Assert.AreEqual(enc.Frames, src.RenderedAt.Count);
            Assert.AreEqual(1024, enc.Width);
            Assert.AreEqual(30, enc.Fps);
        }
    }
}
