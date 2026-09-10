using System;
using PieceBook.Core.Events;
using PieceBook.Core.Services;

namespace PieceBook.MetaGame.Jams
{
    /// <summary>
    /// Community events / "jams" (ARQUITECTURA §11 Fase 4 META-06). The active jam is defined by
    /// Remote Config (DATA-04), so it appears and expires within its window with no app update.
    /// Participating grants the jam reward exactly once. Pure over <see cref="RemoteConfig"/> +
    /// <see cref="ISaveService"/> + <see cref="EventBus"/>, so it is testable without a backend.
    /// </summary>
    public sealed class JamService
    {
        // Remote Config keys that describe the current jam.
        public const string KeyId = "jam_id";
        public const string KeyTheme = "jam_theme";
        public const string KeyStart = "jam_start_unix";
        public const string KeyEnd = "jam_end_unix";
        public const string KeyReward = "jam_reward_item";

        private readonly ISaveService _save;
        private readonly EventBus _bus;

        public JamService(ISaveService save, EventBus bus)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <summary>Build the jam described by Remote Config (may be invalid/empty if none configured).</summary>
        public static JamDef FromConfig(RemoteConfig cfg)
        {
            if (cfg == null) return default;
            return new JamDef(
                cfg.GetString(KeyId),
                cfg.GetString(KeyTheme),
                cfg.GetInt(KeyStart),
                cfg.GetInt(KeyEnd),
                cfg.GetString(KeyReward));
        }

        /// <summary>The jam active at <paramref name="nowUnix"/>, or an invalid <see cref="JamDef"/> if none.</summary>
        public JamDef GetActiveJam(RemoteConfig cfg, long nowUnix)
        {
            var jam = FromConfig(cfg);
            return jam.ContainsTime(nowUnix) ? jam : default;
        }

        private static string ClaimMarker(string jamId) => "jam:" + jamId + ":claimed";

        public bool HasParticipated(JamDef jam) =>
            jam.IsValid && _save.Current.IsUnlocked(ClaimMarker(jam.Id));

        /// <summary>
        /// Participate in <paramref name="jam"/>: grant its reward once. Returns false if the jam is
        /// invalid or already claimed. The claim marker lives in Progress so it survives across sessions.
        /// </summary>
        public bool Participate(JamDef jam)
        {
            if (!jam.IsValid) return false;
            var marker = ClaimMarker(jam.Id);
            if (_save.Current.IsUnlocked(marker)) return false; // already participated

            _save.Current.Unlock(marker);
            if (!string.IsNullOrEmpty(jam.RewardItemId) && _save.Current.Unlock(jam.RewardItemId))
                _bus.Publish(new ItemUnlocked(jam.RewardItemId));
            _save.Flush();
            return true;
        }
    }
}
