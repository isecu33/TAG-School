namespace PieceBook.Core.Events
{
    /// <summary>
    /// Cross-module meta-game signals (ARQUITECTURA §3, §7 Observer: "LessonPassed",
    /// "ItemUnlocked"). Declared in Core so Lessons can raise them and MetaGame / UI / Audio
    /// can react without any of them referencing each other.
    /// </summary>
    public readonly struct LessonPassed : IEvent
    {
        public readonly string LessonId;
        public readonly int Crowns;          // 1..3
        public readonly string[] Unlocks;    // ids to unlock (§9 LessonDef.unlocks[])

        public LessonPassed(string lessonId, int crowns, string[] unlocks)
        {
            LessonId = lessonId;
            Crowns = crowns;
            Unlocks = unlocks;
        }
    }

    /// <summary>Raised when the meta-game grants an item (§7 "ItemUnlocked").</summary>
    public readonly struct ItemUnlocked : IEvent
    {
        public readonly string ItemId;
        public ItemUnlocked(string itemId) { ItemId = itemId; }
    }
}
