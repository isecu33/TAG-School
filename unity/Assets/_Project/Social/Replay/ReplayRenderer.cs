using System;

namespace PieceBook.Social.Replay
{
    /// <summary>
    /// Drives an offline replay export (SOC-01): walks the deterministic frame plan
    /// (<see cref="ReplayTimeline"/>), pulls each frame from an <see cref="IReplaySource"/> and feeds
    /// it to an <see cref="IVideoEncoder"/>. Because inputs (not frames) are the source of truth
    /// (§4.1), re-rendering the same recording yields byte-identical frames and the same frame count.
    /// </summary>
    public static class ReplayRenderer
    {
        /// <summary>Render <paramref name="source"/> to video at <paramref name="fps"/>. Returns the output path.</summary>
        public static string Render(IReplaySource source, IVideoEncoder encoder, int fps, int width, int height)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (encoder == null) throw new ArgumentNullException(nameof(encoder));

            var times = ReplayTimeline.FrameTimes(source.Duration, fps);
            encoder.Begin(width, height, fps);
            for (int i = 0; i < times.Length; i++)
                encoder.AddFrame(source.RenderAt(times[i]));
            return encoder.End();
        }
    }
}
