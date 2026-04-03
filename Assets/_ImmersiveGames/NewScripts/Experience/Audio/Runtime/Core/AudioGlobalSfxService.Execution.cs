using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.Audio.Config;
using _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Models;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Core
{
    public sealed partial class AudioGlobalSfxService
    {
        private bool TryCreateDirectHandle(
            AudioSfxCueAsset cue,
            AudioClip clip,
            AudioPlaybackContext context,
            ResolvedEmission resolvedEmission,
            string reason,
            string fallbackPath,
            out AudioSfxPlaybackHandle handle,
            out string mode,
            out string path)
        {
            handle = null;
            mode = resolvedEmission.UseSpatial ? "3D" : "2D";
            path = fallbackPath;

            var runtimeObject = new GameObject($"{cue.name}_AudioSfxDirect");
            runtimeObject.transform.SetParent(transform, false);
            runtimeObject.transform.position = context.FollowTarget != null ? context.FollowTarget.position : context.WorldPosition;

            var source = runtimeObject.AddComponent<AudioSource>();
            ConfigureSource(source, cue, clip, context, resolvedEmission, reason);

            handle = runtimeObject.AddComponent<AudioSfxPlaybackHandle>();
            handle.Initialize(
                cueId: cue.GetInstanceID(),
                cueName: cue.name,
                source: source,
                followTarget: context.FollowTarget,
                modeLabel: mode,
                reason: reason,
                destroyOwnerOnComplete: true,
                onCompleted: OnPlaybackCompleted);

            source.Play();

            DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                $"[Audio][SFX] Direct play cue='{cue.name}' mode='{mode}' path='{path}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        private void ConfigureSource(
            AudioSource source,
            AudioSfxCueAsset cue,
            AudioClip clip,
            AudioPlaybackContext context,
            ResolvedEmission resolvedEmission,
            string reason)
        {
            _ = reason;

            if (source == null)
            {
                throw new InvalidOperationException("[FATAL][Audio] AudioSource obrigatorio ausente para configuracao SFX.");
            }

            if (cue == null)
            {
                throw new InvalidOperationException("[FATAL][Audio] AudioSfxCueAsset obrigatorio ausente para configuracao SFX.");
            }

            if (clip == null)
            {
                throw new InvalidOperationException("[FATAL][Audio] AudioClip obrigatorio ausente para configuracao SFX.");
            }

            float volumeScale = Mathf.Max(0f, context.VolumeScale);
            float masterVolume = _settings != null ? Mathf.Clamp01(_settings.MasterVolume) : 1f;
            float sfxVolume = _settings != null ? Mathf.Clamp01(_settings.SfxVolume) : 1f;
            float categoryMultiplier = _settings != null ? Mathf.Max(0f, _settings.SfxCategoryMultiplier) : 1f;
            float baseVolume = Mathf.Clamp01(cue.BaseVolume);
            float jitter = Mathf.Clamp01(cue.RandomVolumeJitter);
            float volumeJitter = jitter > 0f ? UnityEngine.Random.Range(1f - jitter, 1f + jitter) : 1f;

            source.clip = clip;
            source.outputAudioMixerGroup = _routing.ResolveSfxMixerGroup(cue);
            source.loop = cue.Loop;
            source.spatialBlend = resolvedEmission.UseSpatial ? Mathf.Clamp01(resolvedEmission.SpatialBlend) : 0f;
            source.minDistance = Mathf.Max(0f, resolvedEmission.MinDistance);
            source.maxDistance = Mathf.Max(source.minDistance, resolvedEmission.MaxDistance);
            source.pitch = Mathf.Clamp(UnityEngine.Random.Range(Mathf.Min(cue.PitchMin, cue.PitchMax), Mathf.Max(cue.PitchMin, cue.PitchMax)), 0.01f, 3f);
            source.volume = Mathf.Clamp01(baseVolume * volumeScale * masterVolume * sfxVolume * categoryMultiplier * volumeJitter);
            source.playOnAwake = false;
        }

        private bool ResolveVoiceProfile(
            AudioPlaybackContext context,
            ResolvedExecution resolvedExecution,
            out AudioSfxVoiceProfileAsset profile,
            out string source)
        {
            if (context.VoiceProfile != null)
            {
                profile = context.VoiceProfile;
                source = "context";
                return true;
            }

            if (resolvedExecution.Profile != null && resolvedExecution.Profile.PooledVoiceProfile != null)
            {
                profile = resolvedExecution.Profile.PooledVoiceProfile;
                source = "execution_profile";
                return true;
            }

            profile = null;
            source = "missing_profile";
            return false;
        }
    }
}
