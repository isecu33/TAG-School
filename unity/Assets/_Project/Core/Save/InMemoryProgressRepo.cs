namespace PieceBook.Core.Save
{
    /// <summary>
    /// In-memory <see cref="IProgressRepo"/> for tests and for the EditMode smoke test
    /// (SLICE-01). Serializes through <see cref="ProgressSerializer"/> on every Save so it
    /// behaves like a real repo (deep copy, same format), without touching disk.
    /// </summary>
    public sealed class InMemoryProgressRepo : IProgressRepo
    {
        private string _json;

        public bool Exists => _json != null;

        public Progress Load() => _json == null ? new Progress() : ProgressSerializer.FromJson(_json);

        public void Save(Progress progress) => _json = ProgressSerializer.ToJson(progress);
    }
}
