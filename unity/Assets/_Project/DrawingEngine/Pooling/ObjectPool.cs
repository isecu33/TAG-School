using System;
using System.Collections.Generic;

namespace PieceBook.DrawingEngine.Pooling
{
    /// <summary>
    /// Prewarmed, allocation-free object pool (ARQUITECTURA §7 "Flyweight + Object Pool:
    /// cero allocs durante un trazo"). Used for drips (spray stamps use flat struct
    /// buffers instead).
    ///
    /// As long as the pool is prewarmed to the peak concurrent count, Get()/Return()
    /// never allocate: Stack&lt;T&gt; only grows when Push exceeds capacity, and we pre-size it.
    /// </summary>
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _free;
        private readonly Func<T> _factory;
        private readonly Action<T> _onReturn;

        public int CountInactive => _free.Count;

        public ObjectPool(Func<T> factory, int prewarm, Action<T> onReturn = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onReturn = onReturn;
            _free = new Stack<T>(Math.Max(1, prewarm));
            for (int i = 0; i < prewarm; i++)
                _free.Push(_factory());
        }

        public T Get() => _free.Count > 0 ? _free.Pop() : _factory();

        public void Return(T item)
        {
            if (item == null) return;
            _onReturn?.Invoke(item);
            _free.Push(item);
        }
    }
}
