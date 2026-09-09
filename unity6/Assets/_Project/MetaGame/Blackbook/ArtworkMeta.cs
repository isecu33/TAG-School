using System;

namespace PieceBook.MetaGame.Blackbook
{
    /// <summary>
    /// Metadata for a saved artwork (ARQUITECTURA §9 Artwork {id, …, wallContext, createdAt,
    /// sharedUrl?}). The heavy parts (layers, strokeRecording) live in the drawing/replay data; the
    /// blackbook keeps this light record plus a thumbnail, indexed by id from Progress.blackbookPages.
    /// </summary>
    [Serializable]
    public sealed class ArtworkMeta
    {
        public string id;
        public long createdAtUnix;
        public string wallContext;   // mockup id, if plasmada on a wall
        public string sharedUrl;     // set once uploaded (Fase 3 DATA-03)

        public static ArtworkMeta New(string id, string wallContext = null) => new ArtworkMeta
        {
            id = id,
            createdAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            wallContext = wallContext,
        };
    }
}
