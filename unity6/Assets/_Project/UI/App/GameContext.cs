using PieceBook.Core.Audio;
using PieceBook.Core.DI;
using PieceBook.Core.Events;
using PieceBook.Core.Save;
using PieceBook.Core.Services;
using PieceBook.MetaGame;
using UnityEngine;

namespace PieceBook.UI.App
{
    /// <summary>
    /// Composition root for the game (the root "LifetimeScope", ARQUITECTURA §7). Attach one to a
    /// bootstrap GameObject in the first scene. On Awake it builds the <see cref="ServiceContainer"/>,
    /// installs the Core and MetaGame services against real backends (JSON save, Unity audio), and
    /// creates the <see cref="AppStateMachine"/>. Presenters resolve what they need from here — no
    /// static singletons (§7). This replaces the spike's ad-hoc bootstrap for the vertical slice.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameContext : MonoBehaviour
    {
        [SerializeField] private AudioCatalog audioCatalog;

        public ServiceContainer Services { get; private set; }
        public EventBus Bus { get; private set; }
        public AppStateMachine Nav { get; private set; }

        private void Awake()
        {
            // Use the shared default bus so the DrawingCanvas (which publishes stroke events on
            // EventBus.Default) and the Core services talk over the same bus with no extra wiring.
            // Tests inject their own bus instead; this is the one pragmatic shared instance (§7).
            Bus = EventBus.Default;

            var audioBackend = gameObject.AddComponent<UnityAudioBackend>();
            if (audioCatalog != null) audioBackend.Configure(audioCatalog);

            IProgressRepo repo = JsonFileProgressRepo.Default();

            Services = new ServiceContainer();
            CoreInstaller.Install(Services, Bus, audioBackend, repo, new DeviceHaptics());
            MetaInstaller.Install(Services);

            // Touch the audio service so it subscribes to the bus from frame one.
            Services.Resolve<IAudioService>();

            Nav = new AppStateMachine();
        }

        private void OnDestroy() => Services?.Dispose();
    }
}
