using System;
using System.Collections.Generic;
using PieceBook.Core.Events;
using PieceBook.Core.Services;

namespace PieceBook.MetaGame.Store
{
    /// <summary>A purchasable item and its soft-currency cost (from CapDef.unlockCost, §9).</summary>
    public readonly struct StoreItem
    {
        public readonly string Id;
        public readonly int Cost;
        public StoreItem(string id, int cost) { Id = id; Cost = cost; }
    }

    /// <summary>An item as shown in the store: owned/affordable computed against current progress.</summary>
    public readonly struct StoreItemView
    {
        public readonly string Id;
        public readonly int Cost;
        public readonly bool Owned;
        public readonly bool Affordable;
        public StoreItemView(string id, int cost, bool owned, bool affordable)
        { Id = id; Cost = cost; Owned = owned; Affordable = affordable; }
    }

    /// <summary>
    /// The unlock store (ARQUITECTURA §3 "tienda de desbloqueos", §9 CapDef.unlockCost). Turns
    /// crown-earned soft currency into caps/paints/alphabets. Buying deducts coins, grants the item,
    /// publishes <see cref="ItemUnlocked"/> and persists — all through Core services (§7), Core-only.
    /// META-05.
    /// </summary>
    public sealed class StoreService
    {
        private readonly ISaveService _save;
        private readonly EventBus _bus;
        private readonly Dictionary<string, int> _catalog = new Dictionary<string, int>(32);

        public StoreService(ISaveService save, EventBus bus, IEnumerable<StoreItem> catalog = null)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            if (catalog != null) foreach (var it in catalog) _catalog[it.Id] = it.Cost;
        }

        public void SetItem(string id, int cost) => _catalog[id] = cost;

        /// <summary>The catalog with per-item owned/affordable flags for the current player.</summary>
        public List<StoreItemView> View()
        {
            var p = _save.Current;
            var list = new List<StoreItemView>(_catalog.Count);
            foreach (var kv in _catalog)
            {
                bool owned = p.IsUnlocked(kv.Key);
                list.Add(new StoreItemView(kv.Key, kv.Value, owned, !owned && p.Coins >= kv.Value));
            }
            return list;
        }

        /// <summary>Buy an item by id. Returns true only if it exists, isn't owned, and is affordable.</summary>
        public bool Buy(string id)
        {
            if (!_catalog.TryGetValue(id, out var cost)) return false;
            var p = _save.Current;
            if (p.IsUnlocked(id) || p.Coins < cost) return false;

            p.Coins -= cost;
            p.Unlock(id);
            _bus.Publish(new ItemUnlocked(id));
            _save.Flush();
            return true;
        }
    }
}
