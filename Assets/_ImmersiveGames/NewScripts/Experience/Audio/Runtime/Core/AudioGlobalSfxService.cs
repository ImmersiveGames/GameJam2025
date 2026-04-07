using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Config;
using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Contracts;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.Audio.Config;
using _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Models;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Core
{
    /// <summary>
    /// Runtime canônico de SFX global (F4/F5: direto + pooled).
    /// </summary>
    public sealed partial class AudioGlobalSfxService : MonoBehaviour, IGlobalAudioService
    {
        private const string RuntimeObjectName = "NewScripts_AudioGlobalSfxRuntime";

        private readonly Dictionary<int, int> _activeInstancesByCueId = new Dictionary<int, int>();
        private readonly Dictionary<int, float> _lastPlayRealtimeByCueId = new Dictionary<int, float>();
        private readonly Dictionary<int, List<AudioSfxPlaybackHandle>> _activeHandlesByCueId = new Dictionary<int, List<AudioSfxPlaybackHandle>>();
        private readonly Dictionary<AudioSfxPlaybackHandle, PooledPlaybackState> _pooledPlaybackByHandle = new Dictionary<AudioSfxPlaybackHandle, PooledPlaybackState>();
        private readonly Dictionary<int, int> _activePooledByProfileId = new Dictionary<int, int>();
        private readonly HashSet<PoolDefinitionAsset> _prewarmedDefinitions = new HashSet<PoolDefinitionAsset>();

        private IAudioSettingsService _settings;
        private IAudioRoutingResolver _routing;
        private IPoolService _poolService;

        private struct PooledPlaybackState
        {
            public PoolDefinitionAsset Definition;
            public AudioSfxVoiceProfileAsset Profile;
            public GameObject Instance;
            public float ReleaseGraceSeconds;
        }

        private readonly struct ResolvedEmission
        {
            public ResolvedEmission(bool useSpatial, float spatialBlend, float minDistance, float maxDistance, string source)
            {
                UseSpatial = useSpatial;
                SpatialBlend = spatialBlend;
                MinDistance = minDistance;
                MaxDistance = maxDistance;
                Source = source;
            }

            public bool UseSpatial { get; }
            public float SpatialBlend { get; }
            public float MinDistance { get; }
            public float MaxDistance { get; }
            public string Source { get; }
        }

        private readonly struct ResolvedExecution
        {
            public ResolvedExecution(AudioSfxExecutionMode mode, AudioSfxExecutionProfileAsset profile, string source)
            {
                Mode = mode;
                Profile = profile;
                Source = source;
            }

            public AudioSfxExecutionMode Mode { get; }
            public AudioSfxExecutionProfileAsset Profile { get; }
            public string Source { get; }
        }

        public static IGlobalAudioService Create(
            AudioDefaultsAsset defaults,
            IAudioSettingsService settings,
            IAudioRoutingResolver routing,
            IPoolService poolService)
        {
            var runtimeObject = new GameObject(RuntimeObjectName);
            DontDestroyOnLoad(runtimeObject);

            var service = runtimeObject.AddComponent<AudioGlobalSfxService>();
            service.Initialize(defaults, settings, routing, poolService);

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

            if (!AudioPlaybackResolutionHelper.TryResolveEmissionProfile(cue, context, out var emissionProfile, out var emissionSource))
            {
                DebugUtility.LogWarning(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play blocked: emission profile missing cue='{cue.name}' reason='{reason}'.");
                return NullAudioPlaybackHandle.Instance;
            }

            if (!AudioPlaybackResolutionHelper.TryResolveExecutionProfile(cue, context, out var executionProfile, out var executionSource))
            {
                DebugUtility.LogWarning(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play blocked: execution profile missing cue='{cue.name}' reason='{reason}'.");
                return NullAudioPlaybackHandle.Instance;
            }

            int cueId = cue.GetInstanceID();
            var resolvedEmission = new ResolvedEmission(
                useSpatial: AudioPlaybackResolutionHelper.ResolveUseSpatial(emissionProfile),
                spatialBlend: emissionProfile.SpatialBlend,
                minDistance: emissionProfile.MinDistance,
                maxDistance: emissionProfile.MaxDistance,
                source: emissionSource);
            var resolvedExecution = new ResolvedExecution(
                mode: AudioPlaybackResolutionHelper.ResolveExecutionMode(executionProfile),
                profile: executionProfile,
                source: executionSource);
            float now = Time.realtimeSinceStartup;
            int activeInstances = GetActiveInstances(cueId);
            bool hasActive2d = !resolvedEmission.UseSpatial && HasActive2dHandle(cueId);
            bool previousHandleStopped = hasActive2d && StopActive2dHandles(cueId);
            float? lastPlayTime = _lastPlayRealtimeByCueId.TryGetValue(cueId, out var lastPlayTimeValue)
                ? lastPlayTimeValue
                : null;

            var directDecision = AudioSfxDirectPolicyEngine.Evaluate(
                cue: cue,
                useSpatial: resolvedEmission.UseSpatial,
                now: now,
                lastPlayTime: lastPlayTime,
                activeInstances: activeInstances,
                hasActive2dHandle: hasActive2d,
                previousHandleStopped: previousHandleStopped);

            if (directDecision.RestartedExisting)
            {
                DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play policy retrigger='restart_existing' cue='{cue.name}' cueId={cueId} mode='2D' previousHandleStopped={directDecision.PreviousHandleStopped} reason='{reason}'.",
                    DebugUtility.Colors.Info);
            }

            if (directDecision.ShouldBlock)
            {
                if (directDecision.BlockPolicy == AudioSfxBlockPolicy.Cooldown)
                {
                    float cooldown = Mathf.Max(0f, cue.SfxRetriggerCooldownSeconds);
                    float elapsed = now - (lastPlayTime ?? now);
                    DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Play blocked policy='block_cooldown' cue='{cue.name}' cueId={cueId} elapsed={elapsed:0.###} cooldown={cooldown:0.###} reason='{reason}'.",
                        DebugUtility.Colors.Info);
                }
                else
                {
                    int maxSimultaneous = Mathf.Max(1, cue.MaxSimultaneousInstances);
                    DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Play blocked policy='block_limit' cue='{cue.name}' cueId={cueId} active={activeInstances} max={maxSimultaneous} reason='{reason}'.",
                        DebugUtility.Colors.Info);
                }

                return NullAudioPlaybackHandle.Instance;
            }

            string path = "direct";
            string mode = resolvedEmission.UseSpatial ? "3D" : "2D";
            AudioSfxPlaybackHandle handle;
            if (resolvedExecution.Mode == AudioSfxExecutionMode.PooledOneShot)
            {
                if (!TryCreatePooledHandle(cue, clip, context, resolvedEmission, resolvedExecution, reason, out handle, out mode, out path))
                {
                    if (!TryCreateDirectHandle(cue, clip, context, resolvedEmission, reason, fallbackPath: path, out handle, out mode, out path))
                    {
                        return NullAudioPlaybackHandle.Instance;
                    }
                }
            }
            else
            {
                if (!TryCreateDirectHandle(cue, clip, context, resolvedEmission, reason, fallbackPath: "direct", out handle, out mode, out path))
                {
                    return NullAudioPlaybackHandle.Instance;
                }
            }

            if (handle == null || !handle.IsValid)
            {
                DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play no-op: no valid handle created cue='{cue.name}' path='{path}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return NullAudioPlaybackHandle.Instance;
            }

            _lastPlayRealtimeByCueId[cueId] = now;
            _activeInstancesByCueId[cueId] = activeInstances + 1;

            DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                $"[Audio][SFX] Play start cue='{cue.name}' cueId={cueId} mode='{mode}' path='{path}' emissionSource='{resolvedEmission.Source}' executionSource='{resolvedExecution.Source}' retrigger='{(directDecision.RestartedExisting ? "restart_existing" : "none")}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return handle;
        }

        private void Initialize(
            AudioDefaultsAsset defaults,
            IAudioSettingsService settings,
            IAudioRoutingResolver routing,
            IPoolService poolService)
        {
            _ = defaults ?? throw new InvalidOperationException("[FATAL][Audio] AudioDefaultsAsset obrigatorio ausente para AudioGlobalSfxService.");
            _settings = settings ?? throw new InvalidOperationException("[FATAL][Audio] IAudioSettingsService obrigatorio ausente para AudioGlobalSfxService.");
            _routing = routing ?? throw new InvalidOperationException("[FATAL][Audio] IAudioRoutingResolver obrigatorio ausente para AudioGlobalSfxService.");
            _poolService = poolService ?? throw new InvalidOperationException("[FATAL][Pooling] IPoolService obrigatorio ausente para AudioGlobalSfxService.");

            DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                "[Audio][BOOT] IGlobalAudioService runtime created (F4/F5 direct + pooled).",
                DebugUtility.Colors.Info);
        }

        private static string ResolveReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason.Trim();
        }
    }
}
