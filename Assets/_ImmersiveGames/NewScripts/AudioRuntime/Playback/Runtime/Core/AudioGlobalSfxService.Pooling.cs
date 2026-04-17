using System;
using System.Collections;
using System.Collections.Generic;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Experience.Audio.Config;
using ImmersiveGames.GameJam2025.Experience.Audio.Runtime.Models;
using ImmersiveGames.GameJam2025.Infrastructure.Pooling.Config;
using UnityEngine;

namespace ImmersiveGames.GameJam2025.Experience.Audio.Runtime.Core
{
    public sealed partial class AudioGlobalSfxService
    {
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

            bool hasPoolService = _poolService != null;
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
                _poolService.EnsureRegistered(poolDefinition);
                if (poolDefinition.Prewarm && _prewarmedDefinitions.Add(poolDefinition))
                {
                    _poolService.Prewarm(poolDefinition);
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
                rentedInstance = _poolService.Rent(poolDefinition, transform);
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

            ConfigureSource(source, cue, clip, context, resolvedEmission, reason);

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

            try
            {
                _poolService.Return(state.Definition, state.Instance);
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
    }
}

