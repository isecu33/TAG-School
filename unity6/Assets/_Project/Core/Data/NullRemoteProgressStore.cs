using PieceBook.Core.Save;

namespace PieceBook.Core.Data
{
    /// <summary>
    /// Offline default for <see cref="IRemoteProgressStore"/>: always unavailable, so
    /// <see cref="ProgressSyncService"/> is a no-op until a real Firestore store is wired (Fase 3
    /// DATA-02). Lets the game ship offline-first with sync stubbed out.
    /// </summary>
    public sealed class NullRemoteProgressStore : IRemoteProgressStore
    {
        public bool IsAvailable => false;
        public Progress Fetch() => null;
        public void Push(Progress progress) { }
    }
}
