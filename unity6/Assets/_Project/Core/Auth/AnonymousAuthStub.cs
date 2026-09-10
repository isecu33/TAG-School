using System;

namespace PieceBook.Core.Auth
{
    /// <summary>
    /// Offline stub for <see cref="IAuthService"/> (Fase 3 DATA-01). Mints a stable anonymous id for
    /// this session so the rest of the game can key data by user id without Firebase present. Linking
    /// is a device-only flow, so here it is a no-op that reports failure. Replaced by a Firebase Auth
    /// implementation when the SDK is added.
    /// </summary>
    public sealed class AnonymousAuthStub : IAuthService
    {
        public string UserId { get; private set; }
        public bool IsSignedIn => !string.IsNullOrEmpty(UserId);
        public bool IsLinked => false;

        public string EnsureSignedIn()
        {
            if (!IsSignedIn) UserId = "anon_" + Guid.NewGuid().ToString("N");
            return UserId;
        }

        public bool LinkTo(string provider) => false; // requires the native provider SDK (device)
    }
}
