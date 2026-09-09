using System;
using System.Collections.Generic;
using PieceBook.Core.Services;

namespace PieceBook.MetaGame.Blackbook
{
    /// <summary>
    /// The blackbook: the player's paginated collection of saved artworks (ARQUITECTURA §3
    /// "blackbook", §9 Progress.blackbookPages). Page ids live in <see cref="Progress"/> (persisted
    /// by <see cref="ISaveService"/>); the artwork metadata + thumbnail live in the
    /// <see cref="IArtworkStore"/>. META-03.
    /// </summary>
    public sealed class BlackbookService
    {
        private readonly ISaveService _save;
        private readonly IArtworkStore _store;

        public BlackbookService(ISaveService save, IArtworkStore store)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public IReadOnlyList<string> Pages => _save.Current.BlackbookPages;

        /// <summary>Persist an artwork and add it as a blackbook page (idempotent on id).</summary>
        public void AddArtwork(ArtworkMeta meta, byte[] thumbnailPng)
        {
            if (meta == null || string.IsNullOrEmpty(meta.id)) throw new ArgumentException("meta.id required");
            _store.Save(meta, thumbnailPng);
            if (!_save.Current.BlackbookPages.Contains(meta.id))
                _save.Current.BlackbookPages.Add(meta.id);
            _save.Flush();
        }

        public ArtworkMeta Get(string id) => _store.Load(id);
        public byte[] Thumbnail(string id) => _store.LoadThumbnail(id);
    }
}
