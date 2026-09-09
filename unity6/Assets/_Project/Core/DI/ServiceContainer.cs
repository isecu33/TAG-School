using System;
using System.Collections.Generic;

namespace PieceBook.Core.DI
{
    /// <summary>
    /// Minimal service locator with real constructor-style registration (ARQUITECTURA §7
    /// "Service Locator ligero (o VContainer)"). This is intentionally tiny so Phase 1 has
    /// dependency injection without pulling an external package into the build; the public
    /// surface (register an interface, resolve an interface) matches what VContainer offers,
    /// so it can be swapped for VContainer later without touching call sites.
    ///
    /// Rules it enforces (so we don't reintroduce the "singletons dispersos" antipattern of §7):
    ///  - services are registered and resolved by <b>interface</b>, never by concrete type;
    ///  - resolution of an unregistered service throws instead of returning null silently.
    /// </summary>
    public sealed class ServiceContainer : IDisposable
    {
        private readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>(32);
        private readonly Dictionary<Type, Func<ServiceContainer, object>> _factories =
            new Dictionary<Type, Func<ServiceContainer, object>>(32);

        /// <summary>Register a ready-made instance as the implementation of <typeparamref name="TService"/>.</summary>
        public ServiceContainer RegisterInstance<TService>(TService instance) where TService : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            _singletons[typeof(TService)] = instance;
            return this;
        }

        /// <summary>
        /// Register a lazily-built singleton. The factory runs at most once, the first time
        /// <typeparamref name="TService"/> is resolved, and can resolve its own dependencies
        /// from the container passed to it.
        /// </summary>
        public ServiceContainer RegisterSingleton<TService>(Func<ServiceContainer, TService> factory)
            where TService : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _factories[typeof(TService)] = c => factory(c);
            return this;
        }

        public bool IsRegistered<TService>() where TService : class =>
            _singletons.ContainsKey(typeof(TService)) || _factories.ContainsKey(typeof(TService));

        /// <summary>Resolve a service. Throws if it was never registered.</summary>
        public TService Resolve<TService>() where TService : class
        {
            var type = typeof(TService);
            if (_singletons.TryGetValue(type, out var existing))
                return (TService)existing;

            if (_factories.TryGetValue(type, out var factory))
            {
                var created = factory(this);
                _singletons[type] = created; // promote to singleton after first build
                return (TService)created;
            }

            throw new InvalidOperationException(
                $"Service '{type.Name}' is not registered. Register it on the root scope before resolving (§7).");
        }

        public void Dispose()
        {
            foreach (var kv in _singletons)
                if (kv.Value is IDisposable d && !ReferenceEquals(d, this)) d.Dispose();
            _singletons.Clear();
            _factories.Clear();
        }
    }
}
