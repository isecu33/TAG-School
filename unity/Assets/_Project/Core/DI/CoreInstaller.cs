using PieceBook.Core.Events;
using PieceBook.Core.Save;
using PieceBook.Core.Services;

namespace PieceBook.Core.DI
{
    /// <summary>
    /// Registers the CORE / SERVICES layer (ARQUITECTURA §3) into a <see cref="ServiceContainer"/>:
    /// the typed <see cref="EventBus"/>, <see cref="ISaveService"/>, <see cref="IAudioService"/> and
    /// <see cref="IHaptics"/>. Everything is registered and resolved by interface (§7) — no module
    /// reaches for a static singleton.
    ///
    /// Callers pass the platform-specific pieces (audio backend, progress repo, haptics) so the same
    /// installer serves the game, the editor harness and tests with different backends.
    /// </summary>
    public static class CoreInstaller
    {
        public static ServiceContainer Install(
            ServiceContainer container,
            EventBus bus,
            IAudioBackend audioBackend,
            IProgressRepo progressRepo,
            IHaptics haptics = null)
        {
            container.RegisterInstance(bus);
            container.RegisterInstance(progressRepo);
            container.RegisterInstance(audioBackend);
            container.RegisterInstance<IHaptics>(haptics ?? new NullHaptics());

            container.RegisterSingleton<ISaveService>(c => new SaveService(c.Resolve<IProgressRepo>()));
            container.RegisterSingleton<IAudioService>(c =>
                new AudioService(c.Resolve<IAudioBackend>(), c.Resolve<EventBus>()));

            return container;
        }
    }
}
