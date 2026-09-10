namespace PieceBook.Social.Upload
{
    /// <summary>
    /// Upload boundary for shared artwork (ARQUITECTURA §2 Firebase Storage, Fase 3 DATA-03).
    /// The Firebase Storage implementation lives behind this on device; a stub keeps the export/share
    /// flow working offline (no upload, no sharedUrl).
    /// </summary>
    public interface IArtworkUploader
    {
        bool IsAvailable { get; }

        /// <summary>Upload the encoded artwork and return its shared URL, or null if unavailable.</summary>
        string Upload(string artworkId, byte[] pngOrMp4, string mimeType);
    }

    /// <summary>Offline default: never uploads, returns null. Replaced by the Storage impl on device.</summary>
    public sealed class NullArtworkUploader : IArtworkUploader
    {
        public bool IsAvailable => false;
        public string Upload(string artworkId, byte[] pngOrMp4, string mimeType) => null;
    }
}
