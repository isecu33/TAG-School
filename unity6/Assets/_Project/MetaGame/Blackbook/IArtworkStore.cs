namespace PieceBook.MetaGame.Blackbook
{
    /// <summary>
    /// Storage boundary for blackbook artworks (§7 Repository). Metadata + a thumbnail per artwork.
    /// A JSON/PNG file store serves locally; Firebase Storage backs it later (Fase 3 DATA-03).
    /// </summary>
    public interface IArtworkStore
    {
        void Save(ArtworkMeta meta, byte[] thumbnailPng);
        ArtworkMeta Load(string id);
        byte[] LoadThumbnail(string id);
        bool Exists(string id);
    }
}
