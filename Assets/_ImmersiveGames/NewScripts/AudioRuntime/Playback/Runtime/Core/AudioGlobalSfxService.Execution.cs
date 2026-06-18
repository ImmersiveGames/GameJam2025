using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;
using Random = UnityEngine.Random;
namespace _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core
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
            runtimeObject.transform.position = context.followTarget != null ? context.followTarget.position : context.worldPosition;

            var source = runtimeObject.AddComponent<AudioSource>();
            ConfigureSource(source, cue, clip, context, resolvedEmission, reason);

            handle = runtimeObject.AddComponent<AudioSfxPlaybackHandle>();
            handle.Initialize(
                cue.GetEntityId(),
                cue.name,
                source,
                context.followTarget,
                mode,
                reason,
                true,
                OnPlaybackCompleted);

            source.Play();

            DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                $"[Audio][SFX] Direct play cue='{cue.name}' mode='{mode}' path='{path}' position='{runtimeObject.transform.position}' finalVolume='{source.volume:0.###}' volumeScale='{Mathf.Max(0f, context.volumeScale):0.###}' spatialBlend='{source.spatialBlend:0.###}' minDistance='{source.minDistance:0.###}' maxDistance='{source.maxDistance:0.###}' reason='{reason}'.",
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

            float volumeScale = Mathf.Max(0f, context.volumeScale);
            float masterVolume = _settings != null ? Mathf.Clamp01(_settings.MasterVolume) : 1f;
            float sfxVolume = _settings != null ? Mathf.Clamp01(_settings.SfxVolume) : 1f;
            float categoryMultiplier = _settings != null ? Mathf.Max(0f, _settings.SfxCategoryMultiplier) : 1f;
            float baseVolume = Mathf.Clamp01(cue.BaseVolume);
            float jitter = Mathf.Clamp01(cue.RandomVolumeJitter);
            float volumeJitter = jitter > 0f ? Random.Range(1f - jitter, 1f + jitter) : 1f;

            source.clip = clip;
            source.outputAudioMixerGroup = _routing.ResolveSfxMixerGroup(cue);
            source.loop = cue.Loop;
            source.spatialBlend = resolvedEmission.UseSpatial ? Mathf.Clamp01(resolvedEmission.SpatialBlend) : 0f;
            source.minDistance = Mathf.Max(0f, resolvedEmission.MinDistance);
            source.maxDistance = Mathf.Max(source.minDistance, resolvedEmission.MaxDistance);
            source.pitch = Mathf.Clamp(Random.Range(Mathf.Min(cue.PitchMin, cue.PitchMax), Mathf.Max(cue.PitchMin, cue.PitchMax)), 0.01f, 3f);
            source.volume = Mathf.Max(0f, baseVolume * volumeScale * masterVolume * sfxVolume * categoryMultiplier * volumeJitter);
            source.playOnAwake = false;

            string outputMixerGroupName = source.outputAudioMixerGroup != null
                ? source.outputAudioMixerGroup.name
                : "<none>";
            DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                $"[Audio][SFX] Source configured cue='{cue.name}' clip='{clip.name}' spatial='{resolvedEmission.UseSpatial}' spatialBlend='{source.spatialBlend:0.###}' minDistance='{source.minDistance:0.###}' maxDistance='{source.maxDistance:0.###}' volumeScale='{volumeScale:0.###}' baseVolume='{baseVolume:0.###}' finalVolume='{source.volume:0.###}' masterVolume='{masterVolume:0.###}' sfxVolume='{sfxVolume:0.###}' categoryMultiplier='{categoryMultiplier:0.###}' volumeJitter='{volumeJitter:0.###}' pitch='{source.pitch:0.###}' outputMixerGroup='{outputMixerGroupName}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private bool ResolveVoiceProfile(
            AudioPlaybackContext context,
            ResolvedExecution resolvedExecution,
            out AudioSfxVoiceProfileAsset profile,
            out string source)
        {
            if (context.voiceProfile != null)
            {
                profile = context.voiceProfile;
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
