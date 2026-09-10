namespace PieceBook.Core.Auth
{
    /// <summary>
    /// Account boundary (ARQUITECTURA §10: anónima por defecto, link a Apple/Google después).
    /// The Firebase Auth implementation lives behind this interface on device; a stub keeps the
    /// game fully playable offline with an anonymous local id (Fase 3 DATA-01).
    /// </summary>
    public interface IAuthService
    {
        /// <summary>Current user id (anonymous or linked), or null if not signed in.</summary>
        string UserId { get; }

        bool IsSignedIn { get; }

        /// <summary>True once the anonymous account has been linked to Apple/Google.</summary>
        bool IsLinked { get; }

        /// <summary>Sign in anonymously if needed. Returns the (stable) user id.</summary>
        string EnsureSignedIn();

        /// <summary>Link the anonymous account to a provider ("apple"/"google"). Device-only flow.</summary>
        bool LinkTo(string provider);
    }
}
