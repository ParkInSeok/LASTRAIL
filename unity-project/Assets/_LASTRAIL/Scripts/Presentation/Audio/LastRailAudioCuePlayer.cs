#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastRail.Presentation.Audio
{
    public sealed class LastRailAudioCuePlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private LastRailAudioClipBinding[] clipBindings = Array.Empty<LastRailAudioClipBinding>();

        private readonly LaunchAudioCueCatalog catalog = LaunchAudioCueCatalog.CreateDefault();
        private readonly HashSet<string> frameCues = new HashSet<string>(StringComparer.Ordinal);
        private int cueFrame = -1;

        public int BindingCount => clipBindings == null ? 0 : clipBindings.Length;
        public bool HasServerTransport => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public bool HasMissingClipBinding
        {
            get
            {
                if (clipBindings == null)
                {
                    return true;
                }

                foreach (var binding in clipBindings)
                {
                    if (binding == null || string.IsNullOrWhiteSpace(binding.CueId) || binding.Clip == null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool HasUnintendedAutoplay
        {
            get
            {
                return (sfxSource != null && sfxSource.playOnAwake) ||
                       (bgmSource != null && bgmSource.playOnAwake);
            }
        }

        public void PlayCueFromUnityEvent(string cueId)
        {
            TryPlayCue(cueId);
        }

        public LastRailAudioCuePlayResult TryPlayCue(string cueId)
        {
            var resolved = catalog.ResolveClipId(cueId);
            if (resolved == null)
            {
                return LastRailAudioCuePlayResult.NotPlayed(cueId, null, "unknown_cue");
            }

            var binding = FindBinding(resolved);
            if (binding == null || binding.Clip == null)
            {
                return LastRailAudioCuePlayResult.NotPlayed(cueId, resolved, "missing_clip_binding");
            }

            return binding.Loop ? PrepareBgm(cueId, resolved, binding) : PlaySfx(cueId, resolved, binding);
        }

        public LastRailAudioCuePlayResult TryPrepareBgmLoop(string cueId)
        {
            return TryPlayCue(cueId);
        }

        public void ConfigureForTests(
            AudioSource testSfxSource,
            AudioSource testBgmSource,
            LastRailAudioClipBinding[] testBindings)
        {
            sfxSource = testSfxSource;
            bgmSource = testBgmSource;
            clipBindings = testBindings ?? Array.Empty<LastRailAudioClipBinding>();
            frameCues.Clear();
            cueFrame = -1;
            ConfigureSourceDefaults();
        }

        private LastRailAudioCuePlayResult PlaySfx(string requested, string resolved, LastRailAudioClipBinding binding)
        {
            if (sfxSource == null)
            {
                return LastRailAudioCuePlayResult.NotPlayed(requested, resolved, "missing_sfx_source");
            }

            if (cueFrame != Time.frameCount)
            {
                frameCues.Clear();
                cueFrame = Time.frameCount;
            }
            if (!frameCues.Add(resolved))
                return LastRailAudioCuePlayResult.NotPlayed(requested, resolved, "same_frame_duplicate");
            ConfigureSourceDefaults();
            sfxSource.loop = false;
            if (Application.isPlaying)
            {
                sfxSource.PlayOneShot(binding.Clip, binding.Volume);
            }

            return LastRailAudioCuePlayResult.PlayedCue(requested, resolved, false);
        }

        private LastRailAudioCuePlayResult PrepareBgm(string requested, string resolved, LastRailAudioClipBinding binding)
        {
            if (bgmSource == null)
            {
                return LastRailAudioCuePlayResult.NotPlayed(requested, resolved, "missing_bgm_source");
            }

            ConfigureSourceDefaults();
            if (bgmSource.clip != binding.Clip) bgmSource.clip = binding.Clip;
            bgmSource.loop = true;
            // The single loop uses its authored AudioSource gain; never reset owner volume/mute.
            if (Application.isPlaying && !bgmSource.isPlaying)
            {
                bgmSource.Play();
            }

            return LastRailAudioCuePlayResult.PlayedCue(requested, resolved, true);
        }

        private LastRailAudioClipBinding FindBinding(string cueId)
        {
            if (clipBindings == null)
            {
                return null;
            }

            foreach (var binding in clipBindings)
            {
                if (binding != null && string.Equals(binding.CueId, cueId, StringComparison.Ordinal))
                {
                    return binding;
                }
            }

            return null;
        }

        private void ConfigureSourceDefaults()
        {
            if (sfxSource != null)
            {
                sfxSource.playOnAwake = false;
            }

            if (bgmSource != null)
            {
                bgmSource.playOnAwake = false;
            }
        }
    }

    [Serializable]
    public sealed class LastRailAudioClipBinding
    {
        [SerializeField] private string cueId;
        [SerializeField] private AudioClip clip;
        [SerializeField] private bool loop;
        [SerializeField] private float volume;

        public LastRailAudioClipBinding(string cueId, AudioClip clip, bool loop, float volume)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                throw new ArgumentException("Value cannot be empty.", nameof(cueId));
            }

            this.cueId = cueId.Trim();
            this.clip = clip;
            this.loop = loop;
            this.volume = Mathf.Clamp01(volume);
        }

        public string CueId => cueId;
        public AudioClip Clip => clip;
        public bool Loop => loop;
        public float Volume => volume;
    }

    public sealed class LastRailAudioCuePlayResult
    {
        private LastRailAudioCuePlayResult(
            bool played,
            string requestedCueId,
            string resolvedClipId,
            bool loop,
            string reason)
        {
            Played = played;
            RequestedCueId = requestedCueId;
            ResolvedClipId = resolvedClipId;
            Loop = loop;
            Reason = reason;
        }

        public bool Played { get; }
        public string RequestedCueId { get; }
        public string ResolvedClipId { get; }
        public bool Loop { get; }
        public string Reason { get; }

        public static LastRailAudioCuePlayResult PlayedCue(string requestedCueId, string resolvedClipId, bool loop)
        {
            return new LastRailAudioCuePlayResult(true, requestedCueId, resolvedClipId, loop, "ready");
        }

        public static LastRailAudioCuePlayResult NotPlayed(string requestedCueId, string resolvedClipId, string reason)
        {
            return new LastRailAudioCuePlayResult(false, requestedCueId, resolvedClipId, false, reason);
        }
    }
}
#endif
