using System.Collections.Generic;
using UnityEngine;

namespace PieceBook.MetaGame.Blackbook
{
    /// <summary>In-memory <see cref="IArtworkStore"/> for tests and the smoke test.</summary>
    public sealed class InMemoryArtworkStore : IArtworkStore
    {
        private readonly Dictionary<string, string> _meta = new Dictionary<string, string>();
        private readonly Dictionary<string, byte[]> _thumbs = new Dictionary<string, byte[]>();

        public void Save(ArtworkMeta meta, byte[] thumbnailPng)
        {
            _meta[meta.id] = JsonUtility.ToJson(meta);
            _thumbs[meta.id] = thumbnailPng;
        }

        public ArtworkMeta Load(string id) =>
            _meta.TryGetValue(id, out var json) ? JsonUtility.FromJson<ArtworkMeta>(json) : null;

        public byte[] LoadThumbnail(string id) => _thumbs.TryGetValue(id, out var b) ? b : null;

        public bool Exists(string id) => _meta.ContainsKey(id);
    }
}
