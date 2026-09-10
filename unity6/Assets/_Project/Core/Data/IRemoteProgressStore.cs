using PieceBook.Core.Save;

namespace PieceBook.Core.Data
{
    /// <summary>
    /// Remote persistence boundary for progress (ARQUITECTURA §2 Firebase, Fase 3 DATA-02). The
    /// Firestore implementation lives behind this interface so the sync logic stays pure and
    /// offline-first; a fake serves tests. All calls are best-effort and must never block gameplay.
    /// </summary>
    public interface IRemoteProgressStore
    {
        /// <summary>Fetch the remote snapshot, or null if none exists / offline.</summary>
        Progress Fetch();

        /// <summary>Push the merged snapshot to the backend (deferred, best-effort).</summary>
        void Push(Progress progress);

        /// <summary>Whether the backend is currently reachable. Offline-first: false is normal.</summary>
        bool IsAvailable { get; }
    }
}
