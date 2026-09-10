using System;
using System.Collections.Generic;

namespace PieceBook.Core.Services
{
    /// <summary>
    /// Remote-config source boundary (Firebase Remote Config on device, §2). Returns raw string
    /// values for keys; offline is normal (IsAvailable=false → only defaults apply). A fake serves tests.
    /// </summary>
    public interface IRemoteConfigSource
    {
        bool IsAvailable { get; }
        bool TryGet(string key, out string rawValue);
    }

    /// <summary>
    /// Feature flags / tunables with local defaults and optional remote overrides (ARQUITECTURA §2
    /// Remote Config, Fase 3 DATA-04). Works fully offline: without a reachable source, every getter
    /// returns its local default, so no flag can ever block startup. Pure and testable.
    /// </summary>
    public sealed class RemoteConfig
    {
        private readonly Dictionary<string, string> _defaults;
        private readonly IRemoteConfigSource _source;

        public RemoteConfig(Dictionary<string, string> defaults, IRemoteConfigSource source = null)
        {
            _defaults = defaults ?? new Dictionary<string, string>();
            _source = source;
        }

        public string GetString(string key, string fallback = "")
        {
            if (_source != null && _source.IsAvailable && _source.TryGet(key, out var raw)) return raw;
            return _defaults.TryGetValue(key, out var def) ? def : fallback;
        }

        public bool GetBool(string key, bool fallback = false)
        {
            var s = GetString(key, fallback ? "true" : "false");
            return bool.TryParse(s, out var v) ? v : fallback;
        }

        public int GetInt(string key, int fallback = 0)
        {
            var s = GetString(key, fallback.ToString());
            return int.TryParse(s, out var v) ? v : fallback;
        }
    }
}
