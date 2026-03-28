using System;
using System.Collections;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Config;
using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Contracts;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Runtime
{
    /// <summary>
    /// Runtime canônico de SFX global (F4/F5: direto + pooled).
    /// </summary>
    public sealed class AudioGlobalSfxService : MonoBehaviour, IGlobalAudioService
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
            IAudioRoutingResolver routing)
        {
            _ = defaults ?? throw new InvalidOperationException("[FATAL][Audio] AudioDefaultsAsset obrigatorio ausente para AudioGlobalSfxService.");
            _settings = settings ?? throw new InvalidOperationException("[FATAL][Audio] IAudioSettingsService obrigatorio ausente para AudioGlobalSfxService.");
            _routing = routing ?? throw new InvalidOperationException("[FATAL][Audio] IAudioRoutingResolver obrigatorio ausente para AudioGlobalSfxService.");
            TryResolvePoolService(out _);

            DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                "[Audio][BOOT] IGlobalAudioService runtime created (F4/F5 direct + pooled).",
                DebugUtility.Colors.Info);
        }

        private bool TryCreatePooledHandle(
            AudioSfxCueAsset cue,
            AudioClip clip,
            AudioPlaybackContext context,
            ResolvedEmission resolvedEmission,
            ResolvedExecution resolvedExecution,
            string reason,
            out AudioSfxPlaybackHandle handle,
            out string mode,
            out string path)
        {
            handle = null;
            mode = resolvedEmission.UseSpatial ? "3D" : "2D";
            path = "pooled";

            ResolveVoiceProfile(context, resolvedExecution, out var profile, out var profileSource);
            var profileDecision = AudioSfxPooledPolicyEngine.EvaluateProfile(profile);
            if (profileDecision.Type == AudioSfxPooledDecisionType.FallbackToDirect)
            {
                path = "direct";
                DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Pooled bypass: no voice profile resolved cue='{cue.name}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            var poolDefinition = profile.PooledVoicePoolDefinition;
            var poolDefinitionDecision = AudioSfxPooledPolicyEngine.EvaluatePoolDefinition(profile);
            if (poolDefinitionDecision.Type != AudioSfxPooledDecisionType.Proceed)
            {
                if (poolDefinitionDecision.Type == AudioSfxPooledDecisionType.FallbackToDirect)
                {
                    path = "fallback_direct";
                    DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Pooled fallback path='fallback_direct' cue='{cue.name}' profile='{profile.name}' reason='missing_pool_definition' source='{profileSource}'.",
                        DebugUtility.Colors.Info);
                    return false;
                }

                DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play blocked policy='block_budget' cue='{cue.name}' path='pooled' reason='missing_pool_definition' profile='{profile.name}'.",
                    DebugUtility.Colors.Info);
                return true;
            }

            bool hasPoolService = TryResolvePoolService(out var poolService);
            var poolServiceDecision = AudioSfxPooledPolicyEngine.EvaluatePoolService(profile, hasPoolService);
            if (poolServiceDecision.Type != AudioSfxPooledDecisionType.Proceed)
            {
                if (poolServiceDecision.Type == AudioSfxPooledDecisionType.FallbackToDirect)
                {
                    path = "fallback_direct";
                    DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Pooled fallback path='fallback_direct' cue='{cue.name}' profile='{profile.name}' reason='pool_service_unavailable'.",
                        DebugUtility.Colors.Info);
                    return false;
                }

                DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play blocked policy='block_budget' cue='{cue.name}' path='pooled' reason='pool_service_unavailable'.",
                    DebugUtility.Colors.Info);
                return true;
            }

            int budget = Mathf.Max(0, profile.DefaultVoiceBudget);
            int activeForProfile = GetActivePooledForProfile(profile);
            var budgetDecision = AudioSfxPooledPolicyEngine.EvaluateBudget(profile, activeForProfile);
            if (budgetDecision.Type != AudioSfxPooledDecisionType.Proceed)
            {
                if (budgetDecision.Type == AudioSfxPooledDecisionType.FallbackToDirect)
                {
                    path = "fallback_direct";
                    DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Pooled fallback path='fallback_direct' cue='{cue.name}' profile='{profile.name}' policy='block_budget' active={activeForProfile} budget={budget}.",
                        DebugUtility.Colors.Info);
                    return false;
                }

                DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play blocked policy='block_budget' cue='{cue.name}' path='pooled' profile='{profile.name}' active={activeForProfile} budget={budget}.",
                    DebugUtility.Colors.Info);
                return true;
            }

            try
            {
                poolService.EnsureRegistered(poolDefinition);
                if (poolDefinition.Prewarm && _prewarmedDefinitions.Add(poolDefinition))
                {
                    poolService.Prewarm(poolDefinition);
                }
            }
            catch (Exception ex)
            {
                if (profile.AllowDirectFallback)
                {
                    path = "fallback_direct";
                    DebugUtility.LogWarning(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Pooled fallback path='fallback_direct' cue='{cue.name}' profile='{profile.name}' reason='pool_registration_failed' message='{ex.Message}'.");
                    return false;
                }

                DebugUtility.LogError(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play blocked policy='block_budget' cue='{cue.name}' path='pooled' reason='pool_registration_failed' message='{ex.Message}'.");
                return true;
            }

            GameObject rentedInstance;
            try
            {
                rentedInstance = poolService.Rent(poolDefinition, transform);
            }
            catch (Exception ex)
            {
                if (profile.AllowDirectFallback)
                {
                    path = "fallback_direct";
                    DebugUtility.LogWarning(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Pooled fallback path='fallback_direct' cue='{cue.name}' profile='{profile.name}' policy='block_budget' rentFailed='{ex.Message}'.");
                    return false;
                }

                DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play blocked policy='block_budget' cue='{cue.name}' path='pooled' rentFailed='{ex.Message}'.",
                    DebugUtility.Colors.Info);
                return true;
            }

            if (rentedInstance == null)
            {
                if (profile.AllowDirectFallback)
                {
                    path = "fallback_direct";
                    DebugUtility.LogWarning(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Pooled fallback path='fallback_direct' cue='{cue.name}' profile='{profile.name}' reason='rent_returned_null'.");
                    return false;
                }

                DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play blocked policy='block_budget' cue='{cue.name}' path='pooled' reason='rent_returned_null'.",
                    DebugUtility.Colors.Info);
                return true;
            }

            var source = rentedInstance.GetComponent<AudioSource>();
            if (source == null)
            {
                source = rentedInstance.AddComponent<AudioSource>();
            }

            ConfigureSource(source, cue, clip, context, resolvedEmission, resolvedExecution, reason);

            handle = rentedInstance.GetComponent<AudioSfxPlaybackHandle>();
            if (handle == null)
            {
                handle = rentedInstance.AddComponent<AudioSfxPlaybackHandle>();
            }

            mode = source.spatialBlend > 0f ? "3D" : "2D";
            handle.Initialize(
                cueId: cue.GetInstanceID(),
                cueName: cue.name,
                source: source,
                followTarget: context.FollowTarget,
                modeLabel: mode,
                reason: reason,
                destroyOwnerOnComplete: false,
                onCompleted: OnPlaybackCompleted);

            RegisterHandle(cue.GetInstanceID(), handle);
            RegisterPooledHandle(handle, profile, poolDefinition, rentedInstance, profile.ReleaseGraceSeconds);

            source.Play();

            DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                $"[Audio][SFX] Pool rent cue='{cue.name}' profile='{profile.name}' source='{profileSource}' pool='{poolDefinition.name}' mode='{mode}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return true;
        }

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
            path = string.IsNullOrWhiteSpace(fallbackPath) ? "direct" : fallbackPath;

            var playbackObject = new GameObject($"SFX_{cue.name}");
            playbackObject.transform.SetParent(transform, false);

            var source = playbackObject.AddComponent<AudioSource>();
            ConfigureSource(source, cue, clip, context, resolvedEmission, default, reason);

            handle = playbackObject.AddComponent<AudioSfxPlaybackHandle>();
            mode = source.spatialBlend > 0f ? "3D" : "2D";
            handle.Initialize(
                cueId: cue.GetInstanceID(),
                cueName: cue.name,
                source: source,
                followTarget: context.FollowTarget,
                modeLabel: mode,
                reason: reason,
                destroyOwnerOnComplete: true,
                onCompleted: OnPlaybackCompleted);

            RegisterHandle(cue.GetInstanceID(), handle);
            source.Play();
            return true;
        }

        private void ConfigureSource(
            AudioSource source,
            AudioSfxCueAsset cue,
            AudioClip clip,
            AudioPlaybackContext context,
            ResolvedEmission resolvedEmission,
            ResolvedExecution resolvedExecution,
            string reason)
        {
            source.clip = clip;
            source.loop = cue.Loop;

            // Guardrail canonico: pooled one-shot nunca deve operar em loop.
            if (resolvedExecution.Mode == AudioSfxExecutionMode.PooledOneShot && source.loop)
            {
                source.loop = false;
                DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] loopPolicy='forced_off_for_pooled_oneshot' cue='{cue.name}' path='pooled' reason='{reason}'.",
                    DebugUtility.Colors.Info);
            }

            source.outputAudioMixerGroup = _routing.ResolveSfxMixerGroup(cue);

            float jitter = cue.RandomVolumeJitter > 0f
                ? UnityEngine.Random.Range(-cue.RandomVolumeJitter, cue.RandomVolumeJitter)
                : 0f;
            float cueVolume = Mathf.Clamp01(cue.BaseVolume + jitter);
            float contextVolume = Mathf.Max(0f, context.VolumeScale <= 0f ? 1f : context.VolumeScale);

            float master = Mathf.Max(0f, _settings.MasterVolume);
            float sfx = Mathf.Max(0f, _settings.SfxVolume);
            float category = Mathf.Max(0f, _settings.SfxCategoryMultiplier);

            source.volume = Mathf.Clamp01(cueVolume * contextVolume * master * sfx * category);
            source.pitch = UnityEngine.Random.Range(cue.PitchMin, cue.PitchMax);

            bool useSpatial = resolvedEmission.UseSpatial;
            if (useSpatial)
            {
                source.spatialBlend = Mathf.Clamp01(resolvedEmission.SpatialBlend);
                source.minDistance = Mathf.Max(0f, resolvedEmission.MinDistance);
                source.maxDistance = Mathf.Max(source.minDistance, resolvedEmission.MaxDistance);
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

        private void ResolveVoiceProfile(
            AudioPlaybackContext context,
            ResolvedExecution resolvedExecution,
            out AudioSfxVoiceProfileAsset profile,
            out string source)
        {
            if (context.VoiceProfile != null)
            {
                profile = context.VoiceProfile;
                source = "context";
                return;
            }

            if (resolvedExecution.Profile != null && resolvedExecution.Profile.PooledVoiceProfile != null)
            {
                profile = resolvedExecution.Profile.PooledVoiceProfile;
                source = "execution_profile";
                return;
            }

            profile = null;
            source = "none";
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

            if (_pooledPlaybackByHandle.TryGetValue(handle, out var pooledState))
            {
                _pooledPlaybackByHandle.Remove(handle);
                DecrementActivePooledForProfile(pooledState.Profile);
                SchedulePooledReturn(pooledState, completionReason);
            }

            DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                $"[Audio][SFX] Playback complete cue='{cueName}' cueId={cueId} mode='{modeLabel}' completion='{completionReason}'.",
                DebugUtility.Colors.Info);
        }

        private void SchedulePooledReturn(PooledPlaybackState state, string completionReason)
        {
            float grace = Mathf.Max(0f, state.ReleaseGraceSeconds);
            if (grace <= 0f)
            {
                ReturnPooledInstance(state, completionReason, delayed: false);
                return;
            }

            StartCoroutine(ReturnPooledAfterDelay(state, grace, completionReason));
        }

        private IEnumerator ReturnPooledAfterDelay(PooledPlaybackState state, float delaySeconds, string completionReason)
        {
            yield return new WaitForSeconds(delaySeconds);
            ReturnPooledInstance(state, completionReason, delayed: true);
        }

        private void ReturnPooledInstance(PooledPlaybackState state, string completionReason, bool delayed)
        {
            if (state.Instance == null || state.Definition == null)
            {
                return;
            }

            if (!TryResolvePoolService(out var poolService))
            {
                DebugUtility.LogWarning(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Pool return skipped cueInstance='{state.Instance.name}' reason='pool_service_unavailable' delayed={delayed} completion='{completionReason}'.");
                return;
            }

            try
            {
                poolService.Return(state.Definition, state.Instance);
                DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Pool return cueInstance='{state.Instance.name}' pool='{state.Definition.name}' delayed={delayed} completion='{completionReason}'.",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                if (ex.Message != null && ex.Message.IndexOf("not currently rented", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Pool return skipped cueInstance='{state.Instance.name}' pool='{state.Definition.name}' delayed={delayed} completion='{completionReason}' reason='already_returned'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                DebugUtility.LogWarning(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Pool return failed pool='{state.Definition.name}' delayed={delayed} message='{ex.Message}'.");
            }
        }

        private void RegisterPooledHandle(
            AudioSfxPlaybackHandle handle,
            AudioSfxVoiceProfileAsset profile,
            PoolDefinitionAsset definition,
            GameObject instance,
            float releaseGraceSeconds)
        {
            if (handle == null || profile == null || definition == null || instance == null)
            {
                return;
            }

            _pooledPlaybackByHandle[handle] = new PooledPlaybackState
            {
                Definition = definition,
                Profile = profile,
                Instance = instance,
                ReleaseGraceSeconds = Mathf.Max(0f, releaseGraceSeconds)
            };

            int profileId = profile.GetInstanceID();
            int current = 0;
            _activePooledByProfileId.TryGetValue(profileId, out current);
            _activePooledByProfileId[profileId] = current + 1;
        }

        private int GetActivePooledForProfile(AudioSfxVoiceProfileAsset profile)
        {
            if (profile == null)
            {
                return 0;
            }

            int profileId = profile.GetInstanceID();
            return _activePooledByProfileId.TryGetValue(profileId, out int active)
                ? Mathf.Max(0, active)
                : 0;
        }

        private void DecrementActivePooledForProfile(AudioSfxVoiceProfileAsset profile)
        {
            if (profile == null)
            {
                return;
            }

            int profileId = profile.GetInstanceID();
            if (!_activePooledByProfileId.TryGetValue(profileId, out int active))
            {
                return;
            }

            active = Mathf.Max(0, active - 1);
            if (active == 0)
            {
                _activePooledByProfileId.Remove(profileId);
            }
            else
            {
                _activePooledByProfileId[profileId] = active;
            }
        }

        private bool TryResolvePoolService(out IPoolService poolService)
        {
            if (_poolService != null)
            {
                poolService = _poolService;
                return true;
            }

            poolService = null;
            if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
            {
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPoolService>(out poolService) || poolService == null)
            {
                return false;
            }

            _poolService = poolService;
            return true;
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

        private static string ResolveReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason.Trim();
        }
    }

    internal enum AudioSfxBlockPolicy
    {
        None = 0,
        Cooldown = 1,
        SimultaneousLimit = 2
    }

    internal readonly struct AudioSfxDirectPolicyDecision
    {
        public AudioSfxDirectPolicyDecision(
            bool shouldBlock,
            AudioSfxBlockPolicy blockPolicy,
            bool restartedExisting,
            bool previousHandleStopped)
        {
            ShouldBlock = shouldBlock;
            BlockPolicy = blockPolicy;
            RestartedExisting = restartedExisting;
            PreviousHandleStopped = previousHandleStopped;
        }

        public bool ShouldBlock { get; }
        public AudioSfxBlockPolicy BlockPolicy { get; }
        public bool RestartedExisting { get; }
        public bool PreviousHandleStopped { get; }
    }

    /// <summary>
    /// Regras de retrigger/cooldown/limit para trilho SFX direto.
    /// </summary>
    internal static class AudioSfxDirectPolicyEngine
    {
        public static AudioSfxDirectPolicyDecision Evaluate(
            AudioSfxCueAsset cue,
            bool useSpatial,
            float now,
            float? lastPlayTime,
            int activeInstances,
            bool hasActive2dHandle,
            bool previousHandleStopped)
        {
            bool isGlobal2dRequest = !useSpatial;
            bool restartedExisting = isGlobal2dRequest && hasActive2dHandle;

            float cooldown = Mathf.Max(0f, cue.SfxRetriggerCooldownSeconds);
            if (!restartedExisting && cooldown > 0f && lastPlayTime.HasValue)
            {
                float elapsed = now - lastPlayTime.Value;
                if (elapsed < cooldown)
                {
                    return new AudioSfxDirectPolicyDecision(
                        shouldBlock: true,
                        blockPolicy: AudioSfxBlockPolicy.Cooldown,
                        restartedExisting: false,
                        previousHandleStopped: false);
                }
            }

            int maxSimultaneous = Mathf.Max(1, cue.MaxSimultaneousInstances);
            if (!restartedExisting && activeInstances >= maxSimultaneous)
            {
                return new AudioSfxDirectPolicyDecision(
                    shouldBlock: true,
                    blockPolicy: AudioSfxBlockPolicy.SimultaneousLimit,
                    restartedExisting: false,
                    previousHandleStopped: false);
            }

            return new AudioSfxDirectPolicyDecision(
                shouldBlock: false,
                blockPolicy: AudioSfxBlockPolicy.None,
                restartedExisting: restartedExisting,
                previousHandleStopped: restartedExisting && previousHandleStopped);
        }
    }

    internal enum AudioSfxPooledDecisionType
    {
        Proceed = 0,
        FallbackToDirect = 1,
        Blocked = 2
    }

    internal readonly struct AudioSfxPooledPolicyDecision
    {
        public AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType type, string reason)
        {
            Type = type;
            Reason = reason;
        }

        public AudioSfxPooledDecisionType Type { get; }
        public string Reason { get; }
    }

    /// <summary>
    /// Regras de budget/fallback para trilho pooled de SFX.
    /// </summary>
    internal static class AudioSfxPooledPolicyEngine
    {
        public static AudioSfxPooledPolicyDecision EvaluateProfile(AudioSfxVoiceProfileAsset profile)
        {
            if (profile != null)
            {
                return new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.Proceed, "profile_ok");
            }

            return new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.FallbackToDirect, "missing_profile");
        }

        public static AudioSfxPooledPolicyDecision EvaluatePoolDefinition(AudioSfxVoiceProfileAsset profile)
        {
            if (profile == null)
            {
                return new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.FallbackToDirect, "missing_profile");
            }

            if (profile.PooledVoicePoolDefinition != null)
            {
                return new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.Proceed, "pool_ok");
            }

            return profile.AllowDirectFallback
                ? new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.FallbackToDirect, "missing_pool_definition")
                : new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.Blocked, "missing_pool_definition");
        }

        public static AudioSfxPooledPolicyDecision EvaluatePoolService(AudioSfxVoiceProfileAsset profile, bool hasPoolService)
        {
            if (hasPoolService)
            {
                return new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.Proceed, "pool_service_ok");
            }

            return profile != null && profile.AllowDirectFallback
                ? new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.FallbackToDirect, "pool_service_unavailable")
                : new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.Blocked, "pool_service_unavailable");
        }

        public static AudioSfxPooledPolicyDecision EvaluateBudget(AudioSfxVoiceProfileAsset profile, int activeForProfile)
        {
            if (profile == null)
            {
                return new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.FallbackToDirect, "missing_profile");
            }

            int budget = Mathf.Max(0, profile.DefaultVoiceBudget);
            if (budget <= 0 || activeForProfile < budget)
            {
                return new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.Proceed, "budget_ok");
            }

            return profile.AllowDirectFallback
                ? new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.FallbackToDirect, "block_budget")
                : new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.Blocked, "block_budget");
        }
    }
}
