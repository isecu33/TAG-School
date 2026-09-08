using System.IO;
using UnityEngine;

namespace PieceBook.MetaGame.Blackbook
{
    /// <summary>
    /// Offline-first <see cref="IArtworkStore"/>: one JSON (metadata) + one PNG (thumbnail) per
    /// artwork under a directory (typically <c>{persistentDataPath}/blackbook</c>). Portable and
    /// backup-friendly (§2). Firebase Storage backs the same interface in Fase 3 (DATA-03).
    /// </summary>
    public sealed class JsonFileArtworkStore : IArtworkStore
    {
        private readonly string _dir;

        public JsonFileArtworkStore(string dir) { _dir = dir; }

        public static JsonFileArtworkStore Default() =>
            new JsonFileArtworkStore(Path.Combine(Application.persistentDataPath, "blackbook"));

        private string MetaPath(string id) => Path.Combine(_dir, id + ".json");
        private string ThumbPath(string id) => Path.Combine(_dir, id + ".png");

        public void Save(ArtworkMeta meta, byte[] thumbnailPng)
        {
            if (!Directory.Exists(_dir)) Directory.CreateDirectory(_dir);
            File.WriteAllText(MetaPath(meta.id), JsonUtility.ToJson(meta, true));
            if (thumbnailPng != null) File.WriteAllBytes(ThumbPath(meta.id), thumbnailPng);
        }

        public ArtworkMeta Load(string id) =>
            File.Exists(MetaPath(id)) ? JsonUtility.FromJson<ArtworkMeta>(File.ReadAllText(MetaPath(id))) : null;

        public byte[] LoadThumbnail(string id) =>
            File.Exists(ThumbPath(id)) ? File.ReadAllBytes(ThumbPath(id)) : null;

        public bool Exists(string id) => File.Exists(MetaPath(id));
    }
}
