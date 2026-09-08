using System.IO;

namespace PieceBook.Social.Replay
{
    /// <summary>
    /// Dependency-free <see cref="IVideoEncoder"/> that writes each frame as a numbered PNG into a
    /// folder. Not the shipping format (that's MP4 via a plugin), but it keeps the replay pipeline
    /// working and testable without a native encoder — and the frames it writes are exactly what the
    /// MP4 encoder will receive. A tool (ffmpeg) or the plugin muxes these into MP4 in the real build.
    /// </summary>
    public sealed class PngSequenceEncoder : IVideoEncoder
    {
        private readonly string _outDir;
        private int _frame;

        public int FramesWritten => _frame;

        public PngSequenceEncoder(string outDir)
        {
            _outDir = outDir;
        }

        public void Begin(int width, int height, int fps)
        {
            _frame = 0;
            if (!Directory.Exists(_outDir)) Directory.CreateDirectory(_outDir);
        }

        public void AddFrame(byte[] framePng)
        {
            if (framePng == null) return;
            File.WriteAllBytes(Path.Combine(_outDir, $"frame_{_frame:D5}.png"), framePng);
            _frame++;
        }

        public string End() => _outDir;
    }
}
