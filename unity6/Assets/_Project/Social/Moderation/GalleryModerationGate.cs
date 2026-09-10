using System.Collections.Generic;

namespace PieceBook.Social.Moderation
{
    /// <summary>Moderation status of a shared artwork (ARQUITECTURA §10).</summary>
    public enum ModerationState { Pending, Approved, Rejected }

    /// <summary>
    /// Visibility gate for the community gallery (ARQUITECTURA §10, Fase 4 SOC-06). Encodes the rule
    /// that makes the gallery safe: an artwork is visible ONLY once moderation approves it, and any
    /// artwork whose image hash has been reported is hidden automatically. The actual moderation
    /// decisions come from a backend Cloud Function; this pure gate holds the state and the rule, so
    /// it is fully testable without a backend.
    /// </summary>
    public sealed class GalleryModerationGate
    {
        private readonly Dictionary<string, ModerationState> _states = new Dictionary<string, ModerationState>();
        private readonly HashSet<string> _reportedHashes = new HashSet<string>();

        /// <summary>Register a newly shared artwork as awaiting moderation.</summary>
        public void Submit(string artworkId)
        {
            if (!string.IsNullOrEmpty(artworkId) && !_states.ContainsKey(artworkId))
                _states[artworkId] = ModerationState.Pending;
        }

        /// <summary>Backend decision: approve an artwork for public display.</summary>
        public void Approve(string artworkId) => Set(artworkId, ModerationState.Approved);

        /// <summary>Backend decision: reject an artwork.</summary>
        public void Reject(string artworkId) => Set(artworkId, ModerationState.Rejected);

        /// <summary>Flag an image hash as reported → any artwork with that hash is hidden.</summary>
        public void ReportHash(string contentHash)
        {
            if (!string.IsNullOrEmpty(contentHash)) _reportedHashes.Add(contentHash);
        }

        public ModerationState GetState(string artworkId) =>
            _states.TryGetValue(artworkId, out var s) ? s : ModerationState.Pending;

        public bool IsReported(string contentHash) => _reportedHashes.Contains(contentHash);

        /// <summary>Visible only if approved AND its content hash has not been reported (§10).</summary>
        public bool IsVisible(string artworkId, string contentHash) =>
            GetState(artworkId) == ModerationState.Approved && !_reportedHashes.Contains(contentHash);

        private void Set(string artworkId, ModerationState state)
        {
            if (!string.IsNullOrEmpty(artworkId)) _states[artworkId] = state;
        }
    }
}
