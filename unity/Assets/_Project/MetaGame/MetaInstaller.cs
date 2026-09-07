using PieceBook.Core.DI;
using PieceBook.Core.Events;
using PieceBook.Core.Services;

namespace PieceBook.MetaGame
{
    /// <summary>
    /// Registers the meta-game services (ARQUITECTURA §3). They self-subscribe to the EventBus on
    /// construction, so instantiating them here is what makes progression and unlocks live for the
    /// session. Registered as instances so the container disposes (unsubscribes) them.
    /// </summary>
    public static class MetaInstaller
    {
        public static ServiceContainer Install(ServiceContainer container)
        {
            var save = container.Resolve<ISaveService>();
            var bus = container.Resolve<EventBus>();

            container.RegisterInstance(new ProgressionService(save, bus));
            container.RegisterInstance(new UnlockService(save, bus));
            return container;
        }
    }
}
