using System;
using System.Collections.Generic;

namespace PieceBook.Core.Save
{
    /// <summary>
    /// Player progress aggregate (ARQUITECTURA §9 Progress):
    ///   {lessonId → crowns, unlockedItems[], streak, blackbookPages[]}.
    /// Plain serializable data — no Unity types — so it round-trips through any repo
    /// (JSON file today, SQLite/Firestore later) and is trivially testable.
    /// </summary>
    [Serializable]
    public sealed class Progress
    {
        /// <summary>lessonId → best crown count (1..3).</summary>
        public Dictionary<string, int> Crowns = new Dictionary<string, int>();

        /// <summary>Ids of unlocked caps / paints / alphabets (§9 unlockedItems[]).</summary>
        public List<string> UnlockedItems = new List<string>();

        /// <summary>Consecutive-days streak (§9 streak).</summary>
        public int Streak;

        /// <summary>Soft currency earned from crowns; spent in the unlock store (Fase 2 META-05).</summary>
        public int Coins;

        /// <summary>Artwork ids stored in the blackbook (§9 blackbookPages[]).</summary>
        public List<string> BlackbookPages = new List<string>();

        /// <summary>Unix seconds of the last local mutation. Used by sync reconciliation (Fase 3 DATA-02).</summary>
        public long LastModifiedUnix;

        /// <summary>Stamp the aggregate as modified now (called by SaveService on flush).</summary>
        public void Touch() => LastModifiedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>Records a lesson result, keeping the best crown count ever achieved.</summary>
        /// <returns>true if this beat the previous best (or was the first attempt).</returns>
        public bool RecordCrowns(string lessonId, int crowns)
        {
            if (string.IsNullOrEmpty(lessonId)) throw new ArgumentException("lessonId required", nameof(lessonId));
            crowns = crowns < 0 ? 0 : (crowns > 3 ? 3 : crowns);
            if (Crowns.TryGetValue(lessonId, out var best) && best >= crowns) return false;
            Crowns[lessonId] = crowns;
            return true;
        }

        public bool IsUnlocked(string itemId) => UnlockedItems.Contains(itemId);

        public bool Unlock(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || UnlockedItems.Contains(itemId)) return false;
            UnlockedItems.Add(itemId);
            return true;
        }

        public int TotalCrowns
        {
            get { int n = 0; foreach (var c in Crowns.Values) n += c; return n; }
        }
    }
}
