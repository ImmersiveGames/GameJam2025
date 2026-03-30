using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Bindings
{
    /// <summary>
    /// Binding estrutural mínimo de contexto local para EntityAudio.
    /// Não resolve semântica e não faz authoring de comportamento.
    /// </summary>
    public sealed class EntityAudioEmitter : MonoBehaviour
    {
        private const string LogPrefix = "[Audio][EntityEmitter]";

        [Header("Binding Context")]
        [SerializeField] private Transform spatialAnchor;
        [SerializeField] [Min(0f)] private float defaultVolumeScale = 1f;
        [SerializeField] private AudioSfxVoiceProfileAsset defaultVoiceProfile;

        private IEntityAudioService _entityAudioService;

        public Transform SpatialAnchor => ResolveSpatialAnchor();
        public float DefaultVolumeScale => Mathf.Max(0f, defaultVolumeScale);
        public AudioSfxVoiceProfileAsset DefaultVoiceProfile => defaultVoiceProfile;

        private void Reset()
        {
            spatialAnchor = transform;
            defaultVolumeScale = 1f;
        }

        public bool TryResolveService(out IEntityAudioService entityAudioService)
        {
            if (_entityAudioService != null)
            {
                entityAudioService = _entityAudioService;
                return true;
            }

            entityAudioService = null;

            if (!Application.isPlaying)
            {
                return false;
            }

            if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
            {
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal(out _entityAudioService) || _entityAudioService == null)
            {
                _entityAudioService = null;
                return false;
            }

            entityAudioService = _entityAudioService;
            return true;
        }

        public AudioPlaybackContext CreateLocalContext(string reason = null)
        {
            return CreateContext(useSpatial: true, reason);
        }

        public AudioPlaybackContext CreateGlobalContext(string reason = null)
        {
            return CreateContext(useSpatial: false, reason);
        }

        public AudioPlaybackContext ApplyEmitterDefaults(AudioPlaybackContext context)
        {
            var resolvedContext = context;
            resolvedContext.VolumeScale = ResolveVolumeScale(context.VolumeScale);
            ApplyVoiceProfileDefaults(ref resolvedContext);
            ApplySpatialDefaults(ref resolvedContext);
            return resolvedContext;
        }

        public IAudioPlaybackHandle PlayPurpose(string purpose, string reason = null)
        {
            return PlayPurpose(purpose, CreateLocalContext(reason));
        }

        public IAudioPlaybackHandle PlayPurpose(string purpose, AudioPlaybackContext context)
        {
            if (!TryPreparePlayback("PlayPurpose", "purpose", purpose, context, out var entityAudioService, out var resolvedContext))
            {
                return NullAudioPlaybackHandle.Instance;
            }

            return entityAudioService.PlayPurpose(purpose, ResolveSpatialAnchor(), resolvedContext);
        }

        public IAudioPlaybackHandle PlayCue(AudioSfxCueAsset cue, string reason = null)
        {
            return PlayCue(cue, CreateLocalContext(reason));
        }

        public IAudioPlaybackHandle PlayCue(AudioSfxCueAsset cue, AudioPlaybackContext context)
        {
            if (!TryPreparePlayback("PlayCue", "cue", cue != null ? cue.name : null, context, out var entityAudioService, out var resolvedContext))
            {
                return NullAudioPlaybackHandle.Instance;
            }

            return entityAudioService.PlayCue(cue, resolvedContext);
        }

        private Transform ResolveSpatialAnchor()
        {
            return spatialAnchor != null ? spatialAnchor : transform;
        }

        private AudioPlaybackContext CreateContext(bool useSpatial, string reason)
        {
            if (useSpatial)
            {
                Transform anchor = ResolveSpatialAnchor();
                return AudioPlaybackContext.Spatial(
                    worldPosition: anchor.position,
                    followTarget: anchor,
                    reason: reason,
                    volumeScale: 1f,
                    voiceProfile: null);
            }

            return AudioPlaybackContext.Global(
                reason: reason,
                volumeScale: 1f);
        }

        private bool TryPreparePlayback(
            string action,
            string subjectName,
            string subjectValue,
            AudioPlaybackContext context,
            out IEntityAudioService entityAudioService,
            out AudioPlaybackContext resolvedContext)
        {
            resolvedContext = default;

            if (!TryResolveService(out entityAudioService) || entityAudioService == null)
            {
                LogPlaybackBlocked(action, subjectName, subjectValue);
                return false;
            }

            resolvedContext = ApplyEmitterDefaults(context);
            return true;
        }

        private float ResolveVolumeScale(float contextVolumeScale)
        {
            return Mathf.Max(0f, contextVolumeScale) * DefaultVolumeScale;
        }

        private void ApplyVoiceProfileDefaults(ref AudioPlaybackContext context)
        {
            if (context.VoiceProfile == null && defaultVoiceProfile != null)
            {
                context.VoiceProfile = defaultVoiceProfile;
            }
        }

        private void ApplySpatialDefaults(ref AudioPlaybackContext context)
        {
            if (!context.UseSpatial)
            {
                return;
            }

            Transform anchor = ResolveSpatialAnchor();
            context.WorldPosition = anchor.position;

            if (context.FollowTarget == null)
            {
                context.FollowTarget = anchor;
            }
        }

        private void LogPlaybackBlocked(string action, string subjectName, string subjectValue)
        {
            DebugUtility.LogWarning(typeof(EntityAudioEmitter),
                $"{LogPrefix} {action} blocked: IEntityAudioService unavailable emitter='{name}' {subjectName}='{subjectValue ?? "null"}'.");
        }
    }
}
