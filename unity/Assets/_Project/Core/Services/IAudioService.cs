namespace PieceBook.Core.Services
{
    /// <summary>Stable sound ids. Constants, never magic strings on call sites (ARQUITECTURA §7).</summary>
    public static class SfxId
    {
        public const string SprayLoop = "sfx_spray_loop";
        public const string UiClick   = "sfx_ui_click";
        public const string Crown     = "sfx_crown";
        public const string Unlock    = "sfx_unlock";
    }

    /// <summary>
    /// Core audio facade (ARQUITECTURA §3 "AudioService"). Reacts to <c>EventBus</c> signals
    /// (spray loop while a stroke is live, reward stings on crowns/unlocks) and exposes explicit
    /// one-shots for UI. Playback is delegated to an <see cref="IAudioBackend"/> so this service
    /// is testable without a real <c>AudioSource</c>.
    /// </summary>
    public interface IAudioService
    {
        void PlayOneShot(string sfxId);
        void StartLoop(string sfxId);
        void StopLoop(string sfxId);
    }

    /// <summary>
    /// Thin playback boundary. The Unity implementation owns the <c>AudioSource</c>s and the
    /// clip catalog; a fake implementation lets EditMode tests assert what got played (§7 MVP:
    /// keep Unity out of the logic under test).
    /// </summary>
    public interface IAudioBackend
    {
        void PlayOneShot(string sfxId);
        void StartLoop(string sfxId);
        void StopLoop(string sfxId);
    }
}
