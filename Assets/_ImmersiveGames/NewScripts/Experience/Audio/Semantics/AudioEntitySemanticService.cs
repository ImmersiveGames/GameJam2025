using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.Audio.Config;
using _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Core;
using _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Models;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Experience.Audio.Semantics
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

            DebugUtility.LogVerbose(typeof(AudioEntitySemanticService),
                $"[Audio][Entity] PlayCue resolved semanticKey='direct_cue' cue='{cue.name}' source='direct' owner='none' reason='{ResolveReason(context.Reason)}'.",
                DebugUtility.Colors.Info);

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
                    "[Audio][Entity] PlayPurpose blocked: semanticKey is null/empty.");
                return NullAudioPlaybackHandle.Instance;
            }

            string semanticKey = NormalizeSemanticKey(purpose);

            if (_semanticMap == null)
            {
                DebugUtility.LogWarning(typeof(AudioEntitySemanticService),
                    $"[Audio][Entity] PlayPurpose blocked: missing semantic map semanticKey='{semanticKey}' owner='{ResolveOwnerName(owner)}'.");
                return NullAudioPlaybackHandle.Instance;
            }

            if (!_semanticMap.TryResolve(purpose, out var entry) || entry == null || entry.Cue == null)
            {
                DebugUtility.LogVerbose(typeof(AudioEntitySemanticService),
                    $"[Audio][Entity] PlayPurpose no-op: missing mapping semanticKey='{semanticKey}' map='{_semanticMap.name}' owner='{ResolveOwnerName(owner)}' reason='missing_mapping'.",
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

            if (!AudioPlaybackResolutionHelper.TryResolveEmissionProfile(entry.Cue, resolvedContext, out var emissionProfile, out _))
            {
                DebugUtility.LogWarning(typeof(AudioEntitySemanticService),
                    $"[Audio][Entity] PlayPurpose blocked: emission profile missing purpose='{purpose}' cue='{entry.Cue.name}'.");
                return NullAudioPlaybackHandle.Instance;
            }

            bool expectsSpatial = AudioPlaybackResolutionHelper.ResolveUseSpatial(emissionProfile);
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
                $"[Audio][Entity] PlayPurpose resolved semanticKey='{semanticKey}' cue='{entry.Cue.name}' map='{_semanticMap.name}' owner='{ResolveOwnerName(owner)}' source='semantic_map' emissionOverride='{(entry.EmissionProfileOverride != null ? entry.EmissionProfileOverride.name : "none")}' executionOverride='{(entry.ExecutionProfileOverride != null ? entry.ExecutionProfileOverride.name : "none")}' voiceOverride='{(entry.VoiceProfileOverride != null ? entry.VoiceProfileOverride.name : "none")}' reason='{ResolveReason(context.Reason)}'.",
                DebugUtility.Colors.Info);

            return _globalAudioService.Play(entry.Cue, resolvedContext);
        }

        private static string NormalizeSemanticKey(string purpose)
        {
            return string.IsNullOrWhiteSpace(purpose) ? "<none>" : purpose.Trim();
        }

        private static string ResolveOwnerName(Transform owner)
        {
            return owner != null ? owner.name : "none";
        }

        private static string ResolveReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason.Trim();
        }
    }
}
