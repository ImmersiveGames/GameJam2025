using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.AudioRuntime.Playback.Bindings
{
    /// <summary>
    /// Binding estrutural mínimo de contexto local para EntityAudio.
    /// Não resolve semântica e não carrega authoring de domínio.
    /// </summary>
    public sealed class EntityAudioEmitter : MonoBehaviour
    {
        private const string LogPrefix = "[Audio][EntityEmitter]";

        [Header("Binding Context")]
        [SerializeField] private Transform spatialAnchor;
        [SerializeField] [Min(0f)] private float defaultVolumeScale = 1f;
        [SerializeField] private AudioSfxVoiceProfileAsset defaultVoiceProfile;

        private IGlobalAudioService _globalAudioService;

        public Transform SpatialAnchor => ResolveSpatialAnchor();
        public float DefaultVolumeScale => Mathf.Max(0f, defaultVolumeScale);
        public AudioSfxVoiceProfileAsset DefaultVoiceProfile => defaultVoiceProfile;

        private void Reset()
        {
            spatialAnchor = transform;
            defaultVolumeScale = 1f;
        }

        public bool TryResolveService(out IGlobalAudioService globalAudioService)
        {
            if (_globalAudioService != null)
            {
                globalAudioService = _globalAudioService;
                return true;
            }

            globalAudioService = null;

            if (!Application.isPlaying)
            {
                return false;
            }

            if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
            {
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal(out _globalAudioService) || _globalAudioService == null)
            {
                _globalAudioService = null;
                return false;
            }

            globalAudioService = _globalAudioService;
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

        public IAudioPlaybackHandle PlayCue(AudioSfxCueAsset cue, string reason = null)
        {
            return PlayCue(cue, CreateLocalContext(reason));
        }

        public IAudioPlaybackHandle PlayCue(AudioSfxCueAsset cue, AudioPlaybackContext context)
        {
            if (!TryPreparePlayback("PlayCue", cue, context, out var globalAudioService, out var resolvedContext))
            {
                return NullAudioPlaybackHandle.Instance;
            }

            return globalAudioService.Play(cue, resolvedContext);
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
            AudioSfxCueAsset cue,
            AudioPlaybackContext context,
            out IGlobalAudioService globalAudioService,
            out AudioPlaybackContext resolvedContext)
        {
            resolvedContext = default;

            if (cue == null)
            {
                LogPlaybackBlocked(action, "cue", "null", "cue_missing");
                globalAudioService = null;
                return false;
            }

            if (!TryResolveService(out globalAudioService) || globalAudioService == null)
            {
                LogPlaybackBlocked(action, "cue", cue.name, "global_audio_service_unavailable");
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

        private void LogPlaybackBlocked(string action, string subjectName, string subjectValue, string reason)
        {
            DebugUtility.LogWarning(typeof(EntityAudioEmitter),
                $"{LogPrefix} {action} blocked: {reason} emitter='{name}' {subjectName}='{subjectValue ?? "null"}'.");
        }
    }
}

