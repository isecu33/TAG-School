using System;

namespace PieceBook.Social.Share
{
    /// <summary>
    /// Native share sheet boundary (ARQUITECTURA §2 "Native Share (plugin)"). The platform plugin
    /// implements this; a fake implementation lets tests assert what was shared. MVP only shares
    /// OUTWARD — there is no in-app gallery (§10).
    /// </summary>
    public interface INativeShare
    {
        void Share(string filePath, string mimeType, string message);
    }

    /// <summary>
    /// Shares exported artwork (PNG) and process videos (MP4) through the native sheet. Picks the
    /// right MIME type so the OS offers the right targets. Pure logic over <see cref="INativeShare"/>,
    /// so it is testable with a mock; the actual sheet is device-only (checklist).
    /// </summary>
    public sealed class ShareService
    {
        public const string MimePng = "image/png";
        public const string MimeMp4 = "video/mp4";

        private readonly INativeShare _native;

        public ShareService(INativeShare native)
        {
            _native = native ?? throw new ArgumentNullException(nameof(native));
        }

        public void ShareImage(string pngPath, string message = null) =>
            _native.Share(pngPath, MimePng, message);

        public void ShareVideo(string mp4Path, string message = null) =>
            _native.Share(mp4Path, MimeMp4, message);
    }
}
