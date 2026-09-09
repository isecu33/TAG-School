using System;
using System.Collections.Generic;
using UnityEngine;

namespace PieceBook.Core.Save
{
    /// <summary>
    /// Serializes <see cref="Progress"/> to/from JSON. Unity's <see cref="JsonUtility"/> cannot
    /// serialize a <see cref="Dictionary{TKey,TValue}"/>, so we marshal through a flat DTO with
    /// parallel arrays. Kept separate from the repos so both the file repo and any future
    /// SQLite/Firestore repo (Fase 3 DATA-02) share one canonical format — and so it is testable
    /// without any I/O.
    /// </summary>
    public static class ProgressSerializer
    {
        [Serializable]
        private sealed class Dto
        {
            public string[] crownLessonIds;
            public int[] crownCounts;
            public string[] unlockedItems;
            public string[] blackbookPages;
            public int streak;
            public int coins;
        }

        public static string ToJson(Progress p, bool prettyPrint = false)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            int n = p.Crowns.Count;
            var ids = new string[n];
            var counts = new int[n];
            int i = 0;
            foreach (var kv in p.Crowns) { ids[i] = kv.Key; counts[i] = kv.Value; i++; }

            var dto = new Dto
            {
                crownLessonIds = ids,
                crownCounts = counts,
                unlockedItems = p.UnlockedItems.ToArray(),
                blackbookPages = p.BlackbookPages.ToArray(),
                streak = p.Streak,
                coins = p.Coins,
            };
            return JsonUtility.ToJson(dto, prettyPrint);
        }

        public static Progress FromJson(string json)
        {
            var p = new Progress();
            if (string.IsNullOrEmpty(json)) return p;

            var dto = JsonUtility.FromJson<Dto>(json);
            if (dto == null) return p;

            if (dto.crownLessonIds != null && dto.crownCounts != null)
            {
                int n = Mathf.Min(dto.crownLessonIds.Length, dto.crownCounts.Length);
                for (int i = 0; i < n; i++) p.Crowns[dto.crownLessonIds[i]] = dto.crownCounts[i];
            }
            if (dto.unlockedItems != null) p.UnlockedItems = new List<string>(dto.unlockedItems);
            if (dto.blackbookPages != null) p.BlackbookPages = new List<string>(dto.blackbookPages);
            p.Streak = dto.streak;
            p.Coins = dto.coins;
            return p;
        }
    }
}
