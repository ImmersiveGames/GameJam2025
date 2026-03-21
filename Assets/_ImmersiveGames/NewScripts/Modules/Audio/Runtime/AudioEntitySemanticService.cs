using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Runtime
{
    /// <summary>
    /// Serviço semântico standalone de áudio por entidade (F6).
    /// Traduz purpose em request canônico de SFX sem duplicar engine.
    /// </summary>
    public sealed class AudioEntitySemanticService : IEntityAudioService
    {
        private readonly IGlobalAudioService _globalAudioService;
        private EntityAudioSemanticMapAsset _semanticMap;

        public AudioEntitySemanticService(IGlobalAudioService globalAudioService, EntityAudioSemanticMapAsset semanticMap)
        {
            _globalAudioService = globalAudioService;
            _semanticMap = semanticMap;
        }

        public void SetSemanticMap(EntityAudioSemanticMapAsset semanticMap, string source)
        {
            _semanticMap = semanticMap;

            DebugUtility.LogVerbose(typeof(AudioEntitySemanticService),
                $"[Audio][Entity] Semantic map updated source='{(string.IsNullOrWhiteSpace(source) ? "unspecified" : source)}' map='{(_semanticMap != null ? _semanticMap.name : "null")}'.",
                DebugUtility.Colors.Info);
        }

        public IAudioPlaybackHandle PlayCue(AudioSfxCueAsset cue, AudioPlaybackContext context)
        {
            if (_globalAudioService == null)
            {
                DebugUtility.LogWarning(typeof(AudioEntitySemanticService),
                    "[Audio][Entity] PlayCue blocked: IGlobalAudioService is unavailable.");
                return NullAudioPlaybackHandle.Instance;
            }

            if (cue == null)
            {
                DebugUtility.LogWarning(typeof(AudioEntitySemanticService),
                    "[Audio][Entity] PlayCue blocked: cue is null.");
                return NullAudioPlaybackHandle.Instance;
            }

            return _globalAudioService.Play(cue, context);
        }

        public IAudioPlaybackHandle PlayPurpose(string purpose, Transform owner, AudioPlaybackContext context)
        {
            if (_globalAudioService == null)
            {
                DebugUtility.LogWarning(typeof(AudioEntitySemanticService),
                    "[Audio][Entity] PlayPurpose blocked: IGlobalAudioService is unavailable.");
                return NullAudioPlaybackHandle.Instance;
            }

            if (string.IsNullOrWhiteSpace(purpose))
            {
                DebugUtility.LogWarning(typeof(AudioEntitySemanticService),
                    "[Audio][Entity] PlayPurpose blocked: purpose is null/empty.");
                return NullAudioPlaybackHandle.Instance;
            }

            if (_semanticMap == null)
            {
                DebugUtility.LogWarning(typeof(AudioEntitySemanticService),
                    $"[Audio][Entity] PlayPurpose blocked: semantic map is null purpose='{purpose}'.");
                return NullAudioPlaybackHandle.Instance;
            }

            if (!_semanticMap.TryResolve(purpose, out var entry) || entry == null || entry.Cue == null)
            {
                DebugUtility.LogVerbose(typeof(AudioEntitySemanticService),
                    $"[Audio][Entity] PlayPurpose no-op: missing mapping purpose='{purpose}' map='{_semanticMap.name}'.",
                    DebugUtility.Colors.Info);
                return NullAudioPlaybackHandle.Instance;
            }

            var resolvedContext = context;

            if (entry.EmissionProfileOverride != null)
            {
                resolvedContext.EmissionProfile = entry.EmissionProfileOverride;
            }

            if (entry.ExecutionProfileOverride != null)
            {
                resolvedContext.ExecutionProfile = entry.ExecutionProfileOverride;
            }

            if (entry.VoiceProfileOverride != null)
            {
                resolvedContext.VoiceProfile = entry.VoiceProfileOverride;
            }

            float contextVolume = resolvedContext.VolumeScale > 0f ? resolvedContext.VolumeScale : 1f;
            float multiplier = Mathf.Max(0f, entry.VolumeScaleMultiplier);
            resolvedContext.VolumeScale = contextVolume * multiplier;

            bool expectsSpatial = ResolveSpatialIntent(entry.Cue, resolvedContext);
            if (entry.UseOwnerAsFollowTarget && owner != null && expectsSpatial)
            {
                resolvedContext.UseSpatial = true;
                resolvedContext.FollowTarget = owner;
                resolvedContext.WorldPosition = owner.position;
            }

            string mappedReason = string.IsNullOrWhiteSpace(entry.ReasonTag)
                ? $"entity_purpose:{purpose}"
                : entry.ReasonTag;
            resolvedContext.Reason = string.IsNullOrWhiteSpace(resolvedContext.Reason)
                ? mappedReason
                : $"{resolvedContext.Reason}|{mappedReason}";

            DebugUtility.LogVerbose(typeof(AudioEntitySemanticService),
                $"[Audio][Entity] PlayPurpose resolved purpose='{purpose}' cue='{entry.Cue.name}' map='{_semanticMap.name}' owner='{(owner != null ? owner.name : "null")}' emissionOverride='{(entry.EmissionProfileOverride != null ? entry.EmissionProfileOverride.name : "none")}' executionOverride='{(entry.ExecutionProfileOverride != null ? entry.ExecutionProfileOverride.name : "none")}' voiceOverride='{(entry.VoiceProfileOverride != null ? entry.VoiceProfileOverride.name : "none")}'.",
                DebugUtility.Colors.Info);

            return _globalAudioService.Play(entry.Cue, resolvedContext);
        }

        private static bool ResolveSpatialIntent(AudioSfxCueAsset cue, AudioPlaybackContext context)
        {
            if (context.EmissionProfile != null)
            {
                return context.EmissionProfile.EmissionMode == AudioSfxPlaybackMode.Spatial;
            }

            if (cue != null && cue.EmissionProfile != null)
            {
                return cue.EmissionProfile.EmissionMode == AudioSfxPlaybackMode.Spatial;
            }

            return context.UseSpatial || (cue != null && cue.PlaybackMode == AudioSfxPlaybackMode.Spatial);
        }
    }
}
