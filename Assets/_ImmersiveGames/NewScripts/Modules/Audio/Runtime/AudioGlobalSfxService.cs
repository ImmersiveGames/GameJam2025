using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Runtime
{
    /// <summary>
    /// Runtime canônico de SFX global direto (F4, sem pooling).
    /// </summary>
    public sealed class AudioGlobalSfxService : MonoBehaviour, IGlobalAudioService
    {
        private const string RuntimeObjectName = "NewScripts_AudioGlobalSfxRuntime";

        private readonly Dictionary<int, int> _activeInstancesByCueId = new Dictionary<int, int>();
        private readonly Dictionary<int, float> _lastPlayRealtimeByCueId = new Dictionary<int, float>();
        private readonly Dictionary<int, List<AudioSfxPlaybackHandle>> _activeHandlesByCueId = new Dictionary<int, List<AudioSfxPlaybackHandle>>();

        private AudioDefaultsAsset _defaults;
        private IAudioSettingsService _settings;
        private IAudioRoutingResolver _routing;

        public static IGlobalAudioService Create(
            AudioDefaultsAsset defaults,
            IAudioSettingsService settings,
            IAudioRoutingResolver routing)
        {
            var runtimeObject = new GameObject(RuntimeObjectName);
            DontDestroyOnLoad(runtimeObject);

            var service = runtimeObject.AddComponent<AudioGlobalSfxService>();
            service.Initialize(defaults, settings, routing);

            return service;
        }

        public IAudioPlaybackHandle Play(AudioSfxCueAsset cue, AudioPlaybackContext context)
        {
            string reason = ResolveReason(context.Reason);

            if (cue == null)
            {
                DebugUtility.LogWarning(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play blocked: cue is null. reason='{reason}'.");
                return NullAudioPlaybackHandle.Instance;
            }

            if (cue.ExecutionMode != AudioSfxExecutionMode.DirectOneShot)
            {
                DebugUtility.LogWarning(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play blocked: cue='{cue.name}' executionMode='{cue.ExecutionMode}' is not supported in F4 direct runtime.");
                return NullAudioPlaybackHandle.Instance;
            }

            if (!cue.ValidateRuntime(out var validationReason))
            {
                DebugUtility.LogWarning(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play blocked: cue='{cue.name}' invalid validation='{validationReason}'. reason='{reason}'.");
                return NullAudioPlaybackHandle.Instance;
            }

            if (!cue.TryPickClip(out var clip) || clip == null)
            {
                DebugUtility.LogWarning(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play blocked: cue='{cue.name}' has no playable clip. reason='{reason}'.");
                return NullAudioPlaybackHandle.Instance;
            }

            int cueId = cue.GetInstanceID();
            bool useSpatial = context.UseSpatial || cue.PlaybackMode == AudioSfxPlaybackMode.Spatial;
            bool isGlobal2dRequest = !useSpatial;

            bool restartedExisting = false;
            bool previousHandleStopped = false;
            if (isGlobal2dRequest && HasActive2dHandle(cueId))
            {
                restartedExisting = true;
                previousHandleStopped = StopActive2dHandles(cueId);

                DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play policy retrigger='restart_existing' cue='{cue.name}' cueId={cueId} mode='2D' previousHandleStopped={previousHandleStopped} reason='{reason}'.",
                    DebugUtility.Colors.Info);
            }

            float now = Time.realtimeSinceStartup;
            float cooldown = Mathf.Max(0f, cue.SfxRetriggerCooldownSeconds);
            if (!restartedExisting && cooldown > 0f && _lastPlayRealtimeByCueId.TryGetValue(cueId, out var lastPlayTime))
            {
                float elapsed = now - lastPlayTime;
                if (elapsed < cooldown)
                {
                    DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Play blocked policy='block_cooldown' cue='{cue.name}' cueId={cueId} elapsed={elapsed:0.###} cooldown={cooldown:0.###} reason='{reason}'.",
                        DebugUtility.Colors.Info);
                    return NullAudioPlaybackHandle.Instance;
                }
            }

            int maxSimultaneous = Mathf.Max(1, cue.MaxSimultaneousInstances);
            int activeInstances = GetActiveInstances(cueId);
            if (!restartedExisting && activeInstances >= maxSimultaneous)
            {
                DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play blocked policy='block_limit' cue='{cue.name}' cueId={cueId} active={activeInstances} max={maxSimultaneous} reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return NullAudioPlaybackHandle.Instance;
            }

            var playbackObject = new GameObject($"SFX_{cue.name}");
            playbackObject.transform.SetParent(transform, false);

            var source = playbackObject.AddComponent<AudioSource>();
            ConfigureSource(source, cue, clip, context);

            var handle = playbackObject.AddComponent<AudioSfxPlaybackHandle>();
            string mode = source.spatialBlend > 0f ? "3D" : "2D";
            handle.Initialize(cueId, cue.name, source, context.FollowTarget, mode, reason, OnPlaybackCompleted);

            _lastPlayRealtimeByCueId[cueId] = now;
            _activeInstancesByCueId[cueId] = activeInstances + 1;
            RegisterHandle(cueId, handle);

            source.Play();

            DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                $"[Audio][SFX] Play start cue='{cue.name}' cueId={cueId} mode='{mode}' retrigger='{(restartedExisting ? "restart_existing" : "none")}' volume={source.volume:0.###} pitch={source.pitch:0.###} reason='{reason}' routing='{ResolveRoutingName(source.outputAudioMixerGroup)}'.",
                DebugUtility.Colors.Info);

            return handle;
        }

        private void Initialize(
            AudioDefaultsAsset defaults,
            IAudioSettingsService settings,
            IAudioRoutingResolver routing)
        {
            _defaults = defaults;
            _settings = settings;
            _routing = routing;

            DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                "[Audio][BOOT] IGlobalAudioService runtime created (F4, direct SFX without pooling).",
                DebugUtility.Colors.Info);
        }

        private void ConfigureSource(AudioSource source, AudioSfxCueAsset cue, AudioClip clip, AudioPlaybackContext context)
        {
            source.clip = clip;
            source.loop = cue.Loop;
            source.outputAudioMixerGroup = _routing != null ? _routing.ResolveSfxMixerGroup(cue) : null;

            float jitter = cue.RandomVolumeJitter > 0f
                ? Random.Range(-cue.RandomVolumeJitter, cue.RandomVolumeJitter)
                : 0f;
            float cueVolume = Mathf.Clamp01(cue.BaseVolume + jitter);
            float contextVolume = Mathf.Max(0f, context.VolumeScale <= 0f ? 1f : context.VolumeScale);

            float master = _settings != null ? Mathf.Max(0f, _settings.MasterVolume) : (_defaults != null ? Mathf.Max(0f, _defaults.MasterVolume) : 1f);
            float sfx = _settings != null ? Mathf.Max(0f, _settings.SfxVolume) : (_defaults != null ? Mathf.Max(0f, _defaults.SfxVolume) : 1f);
            float category = _settings != null ? Mathf.Max(0f, _settings.SfxCategoryMultiplier) : (_defaults != null ? Mathf.Max(0f, _defaults.SfxCategoryMultiplier) : 1f);

            source.volume = Mathf.Clamp01(cueVolume * contextVolume * master * sfx * category);
            source.pitch = Random.Range(cue.PitchMin, cue.PitchMax);

            bool useSpatial = context.UseSpatial || cue.PlaybackMode == AudioSfxPlaybackMode.Spatial;
            if (useSpatial)
            {
                source.spatialBlend = Mathf.Clamp01(cue.SpatialBlend);
                source.minDistance = Mathf.Max(0f, cue.MinDistance);
                source.maxDistance = Mathf.Max(source.minDistance, cue.MaxDistance);
                source.rolloffMode = AudioRolloffMode.Logarithmic;

                if (context.FollowTarget != null)
                {
                    source.transform.position = context.FollowTarget.position;
                }
                else
                {
                    source.transform.position = context.WorldPosition;
                }
            }
            else
            {
                source.spatialBlend = 0f;
            }
        }

        private void OnPlaybackCompleted(AudioSfxPlaybackHandle handle, int cueId, string cueName, string modeLabel, string completionReason)
        {
            UnregisterHandle(cueId, handle);

            if (_activeInstancesByCueId.TryGetValue(cueId, out int active))
            {
                active = Mathf.Max(0, active - 1);
                if (active == 0)
                {
                    _activeInstancesByCueId.Remove(cueId);
                }
                else
                {
                    _activeInstancesByCueId[cueId] = active;
                }
            }

            DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                $"[Audio][SFX] Playback complete cue='{cueName}' cueId={cueId} mode='{modeLabel}' completion='{completionReason}'.",
                DebugUtility.Colors.Info);
        }

        private void RegisterHandle(int cueId, AudioSfxPlaybackHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            if (!_activeHandlesByCueId.TryGetValue(cueId, out var handles) || handles == null)
            {
                handles = new List<AudioSfxPlaybackHandle>(2);
                _activeHandlesByCueId[cueId] = handles;
            }

            handles.Add(handle);
        }

        private void UnregisterHandle(int cueId, AudioSfxPlaybackHandle handle)
        {
            if (!_activeHandlesByCueId.TryGetValue(cueId, out var handles) || handles == null)
            {
                return;
            }

            for (int i = handles.Count - 1; i >= 0; i--)
            {
                var candidate = handles[i];
                if (candidate == null || candidate == handle)
                {
                    handles.RemoveAt(i);
                }
            }

            if (handles.Count == 0)
            {
                _activeHandlesByCueId.Remove(cueId);
            }
        }

        private bool HasActive2dHandle(int cueId)
        {
            if (!_activeHandlesByCueId.TryGetValue(cueId, out var handles) || handles == null)
            {
                return false;
            }

            for (int i = handles.Count - 1; i >= 0; i--)
            {
                var handle = handles[i];
                if (handle == null || !handle.IsValid)
                {
                    handles.RemoveAt(i);
                    continue;
                }

                if (handle.IsPlaying && IsHandle2d(handle))
                {
                    return true;
                }
            }

            if (handles.Count == 0)
            {
                _activeHandlesByCueId.Remove(cueId);
            }

            return false;
        }

        private bool StopActive2dHandles(int cueId)
        {
            if (!_activeHandlesByCueId.TryGetValue(cueId, out var handles) || handles == null)
            {
                return false;
            }

            bool stoppedAny = false;
            for (int i = handles.Count - 1; i >= 0; i--)
            {
                var handle = handles[i];
                if (handle == null || !handle.IsValid)
                {
                    continue;
                }

                if (!IsHandle2d(handle))
                {
                    continue;
                }

                bool wasPlaying = handle.IsPlaying;
                handle.Stop();
                stoppedAny |= wasPlaying;
            }

            return stoppedAny;
        }

        private static bool IsHandle2d(AudioSfxPlaybackHandle handle)
        {
            if (handle == null)
            {
                return false;
            }

            if (!handle.TryGetComponent<AudioSource>(out var source) || source == null)
            {
                return false;
            }

            return source.spatialBlend <= 0f;
        }

        private int GetActiveInstances(int cueId)
        {
            return _activeInstancesByCueId.TryGetValue(cueId, out int active) ? Mathf.Max(0, active) : 0;
        }

        private static string ResolveRoutingName(UnityEngine.Audio.AudioMixerGroup group)
        {
            return group != null ? group.name : "none";
        }

        private static string ResolveReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason.Trim();
        }
    }
}
