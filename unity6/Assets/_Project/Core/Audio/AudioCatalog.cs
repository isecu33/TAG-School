using System;
using System.Collections.Generic;
using UnityEngine;

namespace PieceBook.Core.Audio
{
    /// <summary>
    /// Data-driven sound catalog (ARQUITECTURA §7 "ScriptableObject como catálogo"). Maps stable
    /// sfx ids (see <c>SfxId</c>) to clips so design swaps audio without touching code and no call
    /// site hard-codes a clip.
    /// </summary>
    [CreateAssetMenu(menuName = "PieceBook/Audio Catalog", fileName = "AudioCatalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
            public bool loop;
        }

        public Entry[] entries = Array.Empty<Entry>();

        private Dictionary<string, Entry> _index;

        public bool TryGet(string id, out Entry entry)
        {
            if (_index == null)
            {
                _index = new Dictionary<string, Entry>(entries.Length);
                foreach (var e in entries)
                    if (!string.IsNullOrEmpty(e.id)) _index[e.id] = e;
            }
            return _index.TryGetValue(id, out entry);
        }
    }
}
