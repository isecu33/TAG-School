using System;
using System.Collections.Generic;

namespace PieceBook.Core.Events
{
    /// <summary>
    /// Marker interface for every message that flows through <see cref="EventBus"/>.
    /// Keeping events as structs avoids per-publish GC allocations.
    /// </summary>
    public interface IEvent { }

    /// <summary>
    /// Minimal, allocation-free typed pub/sub bus (ARQUITECTURA §3, §7 "Observer").
    /// Modules never reference each other directly; they talk through this bus.
    /// This is intentionally tiny for the Phase-0 spike — production swaps in the
    /// full DI-scoped bus, but the public surface stays the same.
    /// </summary>
    public sealed class EventBus
    {
        private static readonly EventBus s_default = new EventBus();

        /// <summary>Process-wide default bus for the spike harness.</summary>
        public static EventBus Default => s_default;

        private readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>(32);

        public void Subscribe<T>(Action<T> handler) where T : struct, IEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers.TryGetValue(typeof(T), out var existing);
            _handlers[typeof(T)] = (Action<T>)existing + handler;
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct, IEvent
        {
            if (handler == null) return;
            if (_handlers.TryGetValue(typeof(T), out var existing))
            {
                var updated = (Action<T>)existing - handler;
                if (updated == null) _handlers.Remove(typeof(T));
                else _handlers[typeof(T)] = updated;
            }
        }

        /// <summary>Publish by value — no boxing, no allocation for struct events.</summary>
        public void Publish<T>(in T evt) where T : struct, IEvent
        {
            if (_handlers.TryGetValue(typeof(T), out var existing))
            {
                ((Action<T>)existing)?.Invoke(evt);
            }
        }
    }
}
