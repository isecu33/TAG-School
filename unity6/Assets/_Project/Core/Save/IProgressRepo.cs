namespace PieceBook.Core.Save
{
    /// <summary>
    /// Persistence boundary for <see cref="Progress"/> (ARQUITECTURA §7 "Repository").
    /// One interface behind which live SQLite/JSON locally and Firestore later (Fase 3
    /// DATA-02), enabling offline-first with deferred sync. Callers never know which
    /// backend answers.
    /// </summary>
    public interface IProgressRepo
    {
        /// <summary>Load the persisted progress, or a fresh empty <see cref="Progress"/> if none exists.</summary>
        Progress Load();

        /// <summary>Persist the given progress durably.</summary>
        void Save(Progress progress);

        /// <summary>True if any progress has been persisted (used for first-run flows).</summary>
        bool Exists { get; }
    }
}
