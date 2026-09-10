using System;
using PieceBook.Core.Services;

namespace PieceBook.Core.Data
{
    /// <summary>
    /// Deferred, offline-first progress sync (ARQUITECTURA §1 offline-first, Fase 3 DATA-02). On
    /// <see cref="Sync"/> it merges the local <see cref="ISaveService.Current"/> with the remote
    /// snapshot via <see cref="SyncReconciler"/>, saves the result locally, and pushes it back — but
    /// only when the backend is available. Gameplay never blocks on this; if offline, it is a no-op
    /// that leaves local progress intact.
    /// </summary>
    public sealed class ProgressSyncService
    {
        private readonly ISaveService _save;
        private readonly IRemoteProgressStore _remote;

        public ProgressSyncService(ISaveService save, IRemoteProgressStore remote)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _remote = remote ?? throw new ArgumentNullException(nameof(remote));
        }

        /// <summary>Reconcile with the backend. Returns true if a sync happened (backend available).</summary>
        public bool Sync()
        {
            if (!_remote.IsAvailable) return false;

            var remoteProgress = _remote.Fetch();
            if (remoteProgress != null)
            {
                var merged = SyncReconciler.Merge(_save.Current, remoteProgress);
                _save.Overwrite(merged);
            }
            _remote.Push(_save.Current);
            return true;
        }
    }
}
