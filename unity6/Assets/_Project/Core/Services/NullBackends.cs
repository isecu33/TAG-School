namespace PieceBook.Core.Services
{
    /// <summary>
    /// Offline defaults for the backend service boundaries, so the game runs fully without Firebase
    /// (Fase 3). Each is replaced by its real SDK-backed implementation when the package is added.
    /// </summary>
    public sealed class NullAnalytics : IAnalytics
    {
        public void LogEvent(string name) { } // no sink until Firebase Analytics is wired (CI-02)
    }

    /// <summary>Remote-config source that is never available → <see cref="RemoteConfig"/> uses defaults (DATA-04).</summary>
    public sealed class NullRemoteConfigSource : IRemoteConfigSource
    {
        public bool IsAvailable => false;
        public bool TryGet(string key, out string rawValue) { rawValue = null; return false; }
    }
}
