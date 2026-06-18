using System;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Audio
{
    public sealed class ActorProjectileFireAudioAdapter : IActorProjectileFireAudioAdapter
    {
        private readonly IGlobalAudioService _audioService;

        public ActorProjectileFireAudioAdapter(IGlobalAudioService audioService)
        {
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        }

        public string AdapterName => nameof(ActorProjectileFireAudioAdapter);

        public IAudioPlaybackHandle PlayFireCue(
            AudioSfxCueAsset cue,
            Vector3 worldPosition,
            float volumeScale,
            string source,
            string reason)
        {
            string normalizedSource = source.TrimToEmpty();
            string normalizedReason = reason.TrimToEmpty();

            if (cue == null)
            {
                DebugUtility.LogVerbose(
                    typeof(ActorProjectileFireAudioAdapter),
                    $"event='ActorProjectileFireAudioCueSkipped' adapter='{AdapterName}' source='{normalizedSource}' reason='fire_audio_cue_not_configured' detailReason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);
                return NullAudioPlaybackHandle.Instance;
            }

            float resolvedVolumeScale = volumeScale < 0f ? 0f : volumeScale;

            var context = AudioPlaybackContext.Spatial(
                worldPosition,
                followTarget: null,
                reason: normalizedReason,
                volumeScale: resolvedVolumeScale);

            var handle = _audioService.Play(cue, context);
            bool valid = handle != null && handle.IsValid;

            DebugUtility.LogVerbose(
                typeof(ActorProjectileFireAudioAdapter),
                $"event='ActorProjectileFireAudioCuePlayed' adapter='{AdapterName}' cue='{cue.name}' position='{FormatVector(worldPosition)}' volumeScale='{resolvedVolumeScale:0.###}' playbackHandleValid='{valid}' source='{normalizedSource}' reason='{normalizedReason}'.",
                valid ? DebugUtility.Colors.Success : DebugUtility.Colors.Info);

            return valid ? handle : NullAudioPlaybackHandle.Instance;
        }

        private static string FormatVector(Vector3 value)
        {
            return $"{value.x:0.###},{value.y:0.###},{value.z:0.###}";
        }
}
}
