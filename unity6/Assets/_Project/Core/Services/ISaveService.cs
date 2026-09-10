using PieceBook.Core.Save;

namespace PieceBook.Core.Services
{
    /// <summary>
    /// Core save facade (ARQUITECTURA §3 "CORE / SERVICES · SaveService"). Holds the live
    /// <see cref="Progress"/> in memory and persists it through an <see cref="IProgressRepo"/>.
    /// Modules mutate <see cref="Current"/> and call <see cref="Flush"/>; they never touch the
    /// repo directly.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>The live progress for this session (loaded on construction).</summary>
        Progress Current { get; }

        /// <summary>Persist <see cref="Current"/> to the underlying repository.</summary>
        void Flush();

        /// <summary>Reload from the repository, discarding unsaved in-memory changes.</summary>
        void Reload();

        /// <summary>Replace the live progress (e.g. after a sync merge) and persist it as-is.</summary>
        void Overwrite(Progress progress);
    }
}
