namespace PieceBook.Social.Replay
{
    /// <summary>
    /// Video encoding boundary (ARQUITECTURA §2 "export MP4 (replay del trazado)"). A native/plugin
    /// encoder (FFmpeg, NatCorder, MediaEncoder) implements this to produce an MP4; a PNG-sequence
    /// encoder serves as a dependency-free fallback and makes the pipeline testable. Frames arrive
    /// as encoded PNG bytes so the source stays engine-agnostic.
    /// </summary>
    public interface IVideoEncoder
    {
        void Begin(int width, int height, int fps);
        void AddFrame(byte[] framePng);
        /// <summary>Finish encoding and return the output path (file for MP4, folder for a PNG sequence).</summary>
        string End();
    }

    /// <summary>
    /// Provides rendered frames for a replay at a given timestamp. The in-editor implementation
    /// re-renders the <c>StrokeRecording</c> through the GPU canvas up to time t and flattens it;
    /// tests use a fake that returns deterministic bytes.
    /// </summary>
    public interface IReplaySource
    {
        float Duration { get; }
        /// <summary>Render the artwork as it looked at <paramref name="timeSeconds"/> and return PNG bytes.</summary>
        byte[] RenderAt(float timeSeconds);
    }
}
