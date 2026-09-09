using System;
using System.IO;
using UnityEngine;

namespace PieceBook.Core.Save
{
    /// <summary>
    /// Offline-first <see cref="IProgressRepo"/> backed by a JSON file under
    /// <see cref="Application.persistentDataPath"/>. Robust and portable (ARQUITECTURA §2
    /// "Persistencia local … robusto, portable, backup sencillo").
    ///
    /// Why JSON and not SQLite yet: SQLite needs a native plugin in the build. The Repository
    /// boundary (<see cref="IProgressRepo"/>) means we can drop in a SQLite implementation
    /// without touching any caller. For the vertical slice's single-aggregate progress, a
    /// file write is more than enough and keeps the round-trip fully testable.
    ///
    /// Writes go through a temp file + atomic move so a crash mid-save never corrupts progress.
    /// </summary>
    public sealed class JsonFileProgressRepo : IProgressRepo
    {
        private readonly string _path;

        public JsonFileProgressRepo(string absolutePath)
        {
            _path = absolutePath ?? throw new ArgumentNullException(nameof(absolutePath));
        }

        /// <summary>Default location: <c>{persistentDataPath}/progress.json</c>.</summary>
        public static JsonFileProgressRepo Default() =>
            new JsonFileProgressRepo(Path.Combine(Application.persistentDataPath, "progress.json"));

        public bool Exists => File.Exists(_path);

        public Progress Load()
        {
            if (!File.Exists(_path)) return new Progress();
            try
            {
                return ProgressSerializer.FromJson(File.ReadAllText(_path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] Could not read progress at '{_path}': {e.Message}. Starting fresh.");
                return new Progress();
            }
        }

        public void Save(Progress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var json = ProgressSerializer.ToJson(progress, prettyPrint: true);
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(tmp, _path);
        }
    }
}
