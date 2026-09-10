using System;
using PieceBook.Core.Save;

namespace PieceBook.Core.Services
{
    /// <summary>
    /// Default <see cref="ISaveService"/>: keeps the live <see cref="Progress"/> in memory and
    /// writes through an injected <see cref="IProgressRepo"/> (§7 Repository). Offline-first —
    /// nothing here blocks on network; the repo decides where bytes land.
    /// </summary>
    public sealed class SaveService : ISaveService
    {
        private readonly IProgressRepo _repo;

        public SaveService(IProgressRepo repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            Current = _repo.Load();
        }

        public Progress Current { get; private set; }

        public void Flush()
        {
            Current.Touch(); // stamp for sync reconciliation (Fase 3 DATA-02)
            _repo.Save(Current);
        }

        public void Reload() => Current = _repo.Load();

        public void Overwrite(Progress progress)
        {
            Current = progress ?? throw new System.ArgumentNullException(nameof(progress));
            _repo.Save(Current); // persist as-is (preserve merged LastModifiedUnix; no Touch)
        }
    }
}
