using System;
using System.Collections;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core
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

            ResolveVoiceProfile(context, resolvedExecution, out var profile, out string profileSource);
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
                    path = "direct";
                    DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Pooled direct path='direct' cue='{cue.name}' profile='{profile.name}' reason='missing_pool_definition' source='{profileSource}'.",
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
                    path = "direct";
                    DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Pooled direct path='direct' cue='{cue.name}' profile='{profile.name}' reason='pool_service_unavailable'.",
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
                    path = "direct";
                    DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Pooled direct path='direct' cue='{cue.name}' profile='{profile.name}' policy='block_budget' active={activeForProfile} budget={budget}.",
                        DebugUtility.Colors.Info);
                    return false;
                }

                DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play blocked policy='block_budget' cue='{cue.name}' path='pooled' profile='{profile.name}' active={activeForProfile} budget={budget}.",
                    DebugUtility.Colors.Info);
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
                    path = "direct";
                    DebugUtility.LogWarning(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Pooled direct path='direct' cue='{cue.name}' profile='{profile.name}' policy='block_budget' rentFailed='{ex.Message}'.");
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
                    path = "direct";
                    DebugUtility.LogWarning(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Pooled direct path='direct' cue='{cue.name}' profile='{profile.name}' reason='rent_returned_null'.");
                    return false;
                }

                DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Play blocked policy='block_budget' cue='{cue.name}' path='pooled' reason='rent_returned_null'.",
                    DebugUtility.Colors.Info);
                return true;
            }

            rentedInstance.transform.position = context.followTarget != null
                ? context.followTarget.position
                : context.worldPosition;

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
                cue.GetEntityId(),
                cue.name,
                source,
                context.followTarget,
                mode,
                reason,
                false,
                OnPlaybackCompleted);

            RegisterHandle(cue.GetEntityId(), handle);
            RegisterPooledHandle(handle, profile, poolDefinition, rentedInstance, profile.ReleaseGraceSeconds);
            int activeAfterProfile = GetActivePooledForProfile(profile);

            source.Play();

            DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                $"[Audio][SFX] Pool rent event='AudioSfxPooledVoiceRented' cue='{cue.name}' cueId='{cue.GetEntityId()}' profile='{profile.name}' profileSource='{profileSource}' pool='{poolDefinition.name}' instance='{rentedInstance.name}' mode='{mode}' path='pooled' activeBefore='{activeForProfile}' activeAfter='{activeAfterProfile}' budget='{budget}' allowDirectFallback='{profile.AllowDirectFallback}' releaseGraceSeconds='{Mathf.Max(0f, profile.ReleaseGraceSeconds):0.###}' position='{rentedInstance.transform.position}' finalVolume='{source.volume:0.###}' volumeScale='{Mathf.Max(0f, context.volumeScale):0.###}' spatialBlend='{source.spatialBlend:0.###}' minDistance='{source.minDistance:0.###}' maxDistance='{source.maxDistance:0.###}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        private void OnPlaybackCompleted(AudioSfxPlaybackHandle handle, EntityId cueId, string cueName, string modeLabel, string completionReason)
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
                DecrementActivePooledForProfile(pooledState.profile);
                SchedulePooledReturn(pooledState, completionReason);
            }

            DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                $"[Audio][SFX] Playback complete cue='{cueName}' cueId={cueId} mode='{modeLabel}' completion='{completionReason}'.",
                DebugUtility.Colors.Info);
        }

        private void SchedulePooledReturn(PooledPlaybackState state, string completionReason)
        {
            float grace = Mathf.Max(0f, state.releaseGraceSeconds);
            if (grace <= 0f)
            {
                ReturnPooledInstance(state, completionReason, false);
                return;
            }

            StartCoroutine(ReturnPooledAfterDelay(state, grace, completionReason));
        }

        private IEnumerator ReturnPooledAfterDelay(PooledPlaybackState state, float delaySeconds, string completionReason)
        {
            yield return new WaitForSeconds(delaySeconds);
            ReturnPooledInstance(state, completionReason, true);
        }

        private void ReturnPooledInstance(PooledPlaybackState state, string completionReason, bool delayed)
        {
            if (state.instance == null || state.definition == null)
            {
                return;
            }

            string profileName = state.profile != null ? state.profile.name : "<none>";
            try
            {
                _poolService.Return(state.definition, state.instance);
                DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Pool return event='AudioSfxPooledVoiceReturned' cueInstance='{state.instance.name}' profile='{profileName}' pool='{state.definition.name}' activeAfter='{GetActivePooledForProfile(state.profile)}' delayed='{delayed}' completion='{completionReason}'.",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                if (ex.Message != null && ex.Message.IndexOf("not currently rented", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    DebugUtility.LogVerbose(typeof(AudioGlobalSfxService),
                        $"[Audio][SFX] Pool return skipped event='AudioSfxPooledVoiceReturnSkipped' cueInstance='{state.instance.name}' profile='{profileName}' pool='{state.definition.name}' delayed='{delayed}' completion='{completionReason}' reason='already_returned'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                DebugUtility.LogWarning(typeof(AudioGlobalSfxService),
                    $"[Audio][SFX] Pool return failed pool='{state.definition.name}' delayed={delayed} message='{ex.Message}'.");
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
                definition = definition,
                profile = profile,
                instance = instance,
                releaseGraceSeconds = Mathf.Max(0f, releaseGraceSeconds)
            };

            var profileId = profile.GetEntityId();
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

            var profileId = profile.GetEntityId();
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

            var profileId = profile.GetEntityId();
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

        private void RegisterHandle(EntityId cueId, AudioSfxPlaybackHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            if (!_activeHandlesByCueId.TryGetValue(cueId, out List<AudioSfxPlaybackHandle> handles) || handles == null)
            {
                handles = new List<AudioSfxPlaybackHandle>(2);
                _activeHandlesByCueId[cueId] = handles;
            }

            handles.Add(handle);
        }

        private void UnregisterHandle(EntityId cueId, AudioSfxPlaybackHandle handle)
        {
            if (!_activeHandlesByCueId.TryGetValue(cueId, out List<AudioSfxPlaybackHandle> handles) || handles == null)
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

        private bool HasActive2DHandle(EntityId cueId)
        {
            if (!_activeHandlesByCueId.TryGetValue(cueId, out List<AudioSfxPlaybackHandle> handles) || handles == null)
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

                if (handle.IsPlaying && IsHandle2D(handle))
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

        private bool StopActive2DHandles(EntityId cueId)
        {
            if (!_activeHandlesByCueId.TryGetValue(cueId, out List<AudioSfxPlaybackHandle> handles) || handles == null)
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

                if (!IsHandle2D(handle))
                {
                    continue;
                }

                bool wasPlaying = handle.IsPlaying;
                handle.Stop();
                stoppedAny |= wasPlaying;
            }

            return stoppedAny;
        }

        private static bool IsHandle2D(AudioSfxPlaybackHandle handle)
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

        private int GetActiveInstances(EntityId cueId)
        {
            return _activeInstancesByCueId.TryGetValue(cueId, out int active) ? Mathf.Max(0, active) : 0;
        }
    }
}
