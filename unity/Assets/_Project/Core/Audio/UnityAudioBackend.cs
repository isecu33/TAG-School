using System.Collections.Generic;
using PieceBook.Core.Services;
using UnityEngine;

namespace PieceBook.Core.Audio
{
    /// <summary>
    /// Unity <see cref="IAudioBackend"/>: owns the <see cref="AudioSource"/>s and resolves clips
    /// from an <see cref="AudioCatalog"/>. One-shots share a pooled source; loops get a dedicated
    /// source per id so they can be started/stopped independently. This is the only place that
    /// touches Unity audio, keeping <see cref="AudioService"/> pure and testable.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnityAudioBackend : MonoBehaviour, IAudioBackend
    {
        [SerializeField] private AudioCatalog catalog;

        private AudioSource _oneShotSource;
        private readonly Dictionary<string, AudioSource> _loopSources = new Dictionary<string, AudioSource>(4);

        public void Configure(AudioCatalog audioCatalog) => catalog = audioCatalog;

        private void Awake()
        {
            _oneShotSource = gameObject.AddComponent<AudioSource>();
            _oneShotSource.playOnAwake = false;
        }

        public void PlayOneShot(string sfxId)
        {
            if (catalog != null && catalog.TryGet(sfxId, out var e) && e.clip != null)
                _oneShotSource.PlayOneShot(e.clip, e.volume <= 0f ? 1f : e.volume);
        }

        public void StartLoop(string sfxId)
        {
            if (catalog == null || !catalog.TryGet(sfxId, out var e) || e.clip == null) return;

            if (!_loopSources.TryGetValue(sfxId, out var src) || src == null)
            {
                src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = true;
                _loopSources[sfxId] = src;
            }
            src.clip = e.clip;
            src.volume = e.volume <= 0f ? 1f : e.volume;
            if (!src.isPlaying) src.Play();
        }

        public void StopLoop(string sfxId)
        {
            if (_loopSources.TryGetValue(sfxId, out var src) && src != null && src.isPlaying)
                src.Stop();
        }
    }
}
