namespace PieceBook.MetaGame.Jams
{
    /// <summary>
    /// A time-boxed community event ("jam", ARQUITECTURA §11 Fase 4 META-06). Driven by Remote Config
    /// so it can start/stop without an app update. Plain data.
    /// </summary>
    public readonly struct JamDef
    {
        public readonly string Id;
        public readonly string Theme;
        public readonly long StartUnix;
        public readonly long EndUnix;
        public readonly string RewardItemId;

        public JamDef(string id, string theme, long startUnix, long endUnix, string rewardItemId)
        {
            Id = id;
            Theme = theme;
            StartUnix = startUnix;
            EndUnix = endUnix;
            RewardItemId = rewardItemId;
        }

        public bool IsValid => !string.IsNullOrEmpty(Id);
        public bool ContainsTime(long nowUnix) => IsValid && nowUnix >= StartUnix && nowUnix <= EndUnix;
    }
}
