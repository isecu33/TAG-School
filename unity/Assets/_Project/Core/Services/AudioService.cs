using System;
using PieceBook.Core.Events;

namespace PieceBook.Core.Services
{
    /// <summary>
    /// Default <see cref="IAudioService"/>. Wires itself to the typed <see cref="EventBus"/>
    /// (§3, §7 Observer): the spray loop follows <see cref="StrokeStarted"/>/<see cref="StrokeCompleted"/>,
    /// and reward stings follow <see cref="LessonPassed"/>/<see cref="ItemUnlocked"/>. All actual
    /// sound goes through the injected <see cref="IAudioBackend"/>.
    /// </summary>
    public sealed class AudioService : IAudioService, IDisposable
    {
        private readonly IAudioBackend _backend;
        private readonly EventBus _bus;

        public AudioService(IAudioBackend backend, EventBus bus)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));

            _bus.Subscribe<StrokeStarted>(OnStrokeStarted);
            _bus.Subscribe<StrokeCompleted>(OnStrokeCompleted);
            _bus.Subscribe<LessonPassed>(OnLessonPassed);
            _bus.Subscribe<ItemUnlocked>(OnItemUnlocked);
        }

        public void PlayOneShot(string sfxId) => _backend.PlayOneShot(sfxId);
        public void StartLoop(string sfxId) => _backend.StartLoop(sfxId);
        public void StopLoop(string sfxId) => _backend.StopLoop(sfxId);

        private void OnStrokeStarted(StrokeStarted _) => _backend.StartLoop(SfxId.SprayLoop);
        private void OnStrokeCompleted(StrokeCompleted _) => _backend.StopLoop(SfxId.SprayLoop);
        private void OnLessonPassed(LessonPassed _) => _backend.PlayOneShot(SfxId.Crown);
        private void OnItemUnlocked(ItemUnlocked _) => _backend.PlayOneShot(SfxId.Unlock);

        public void Dispose()
        {
            _bus.Unsubscribe<StrokeStarted>(OnStrokeStarted);
            _bus.Unsubscribe<StrokeCompleted>(OnStrokeCompleted);
            _bus.Unsubscribe<LessonPassed>(OnLessonPassed);
            _bus.Unsubscribe<ItemUnlocked>(OnItemUnlocked);
        }
    }
}
