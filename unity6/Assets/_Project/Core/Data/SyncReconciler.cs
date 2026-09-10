using System.Collections.Generic;
using PieceBook.Core.Save;

namespace PieceBook.Core.Data
{
    /// <summary>
    /// Reconciles two <see cref="Progress"/> snapshots (local vs remote) into one, for deferred
    /// Firebase sync (ARQUITECTURA §7 Repository, Fase 3 DATA-02). Pure and deterministic, so it is
    /// fully testable without any network.
    ///
    /// Strategy — achievements are monotonic (never lost), currency is last-write-wins:
    ///  - crowns: per lesson, the best (max) of both sides;
    ///  - unlockedItems / blackbookPages: union (order-preserving, deduped);
    ///  - streak: max;
    ///  - coins: from whichever side has the newer <see cref="Progress.LastModifiedUnix"/> (LWW),
    ///    since coins are spent and a plain max would resurrect spent currency;
    ///  - LastModifiedUnix: max of both.
    /// </summary>
    public static class SyncReconciler
    {
        public static Progress Merge(Progress local, Progress remote)
        {
            if (local == null) return remote ?? new Progress();
            if (remote == null) return local;

            var merged = new Progress();

            // crowns: max per lesson
            foreach (var kv in local.Crowns) merged.Crowns[kv.Key] = kv.Value;
            foreach (var kv in remote.Crowns)
                merged.Crowns[kv.Key] = merged.Crowns.TryGetValue(kv.Key, out var cur)
                    ? (cur >= kv.Value ? cur : kv.Value)
                    : kv.Value;

            merged.UnlockedItems = Union(local.UnlockedItems, remote.UnlockedItems);
            merged.BlackbookPages = Union(local.BlackbookPages, remote.BlackbookPages);
            merged.Streak = local.Streak >= remote.Streak ? local.Streak : remote.Streak;

            // coins: last-write-wins by timestamp (tie → the larger balance)
            merged.Coins = local.LastModifiedUnix == remote.LastModifiedUnix
                ? (local.Coins >= remote.Coins ? local.Coins : remote.Coins)
                : (local.LastModifiedUnix > remote.LastModifiedUnix ? local.Coins : remote.Coins);

            merged.LastModifiedUnix = local.LastModifiedUnix >= remote.LastModifiedUnix
                ? local.LastModifiedUnix : remote.LastModifiedUnix;

            return merged;
        }

        private static List<string> Union(List<string> a, List<string> b)
        {
            var result = new List<string>(a != null ? a.Count : 0);
            var seen = new HashSet<string>();
            void Add(List<string> src)
            {
                if (src == null) return;
                foreach (var s in src) if (seen.Add(s)) result.Add(s);
            }
            Add(a); Add(b);
            return result;
        }
    }
}
