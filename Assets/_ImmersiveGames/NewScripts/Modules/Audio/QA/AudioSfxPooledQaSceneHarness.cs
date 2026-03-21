using System;
using System.Collections;
using System.Reflection;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.QA
{
    /// <summary>
    /// Harness dedicado ao trilho pooled de SFX (F5).
    /// </summary>
    public sealed class AudioSfxPooledQaSceneHarness : MonoBehaviour
    {
        [Header("Pooled Cues")]
        [SerializeField] private AudioSfxCueAsset pooled2dCue;
        [SerializeField] private AudioSfxCueAsset pooled3dCue;
        [SerializeField] private AudioSfxVoiceProfileAsset pooledVoiceProfile;

        [Header("3D QA")]
        [SerializeField] private Transform spatialFollowTarget;
        [SerializeField] private Vector3 spatialProbePosition = new Vector3(0f, 1.5f, 2f);

        [Header("Pooled Diagnostics")]
        [SerializeField] [Min(0.01f)] private float pooledRetriggerInterruptDelaySeconds = 0.15f;
        [SerializeField] [Min(1)] private int pooledRetriggerRepeatCount = 1;
        [SerializeField] [Min(1)] private int pooledBudgetBurstCount = 6;
        [SerializeField] [Min(0f)] private float pooledBudgetStepDelaySeconds = 0.02f;
        [SerializeField] [Min(1)] private int pooledSequenceCount = 5;
        [SerializeField] [Min(0.01f)] private float pooledSequenceStepDelaySeconds = 0.2f;
        [SerializeField] [Min(0.25f)] private float pooledSequenceWaitTimeoutSeconds = 8f;

        [SerializeField] private bool verboseLogs = true;

        private IGlobalAudioService _globalAudioService;
        private IAudioPlaybackHandle _lastHandle = NullAudioPlaybackHandle.Instance;
        private Coroutine _pooledRetriggerRoutine;
        private Coroutine _pooledBudgetRoutine;
        private Coroutine _pooledSequenceRoutine;

        [ContextMenu("QA/Audio/SFX/Pooled/Validate Setup")]
        private void ValidatePooledSetup()
        {
            if (!TryEnsureService())
            {
                LogError("ValidatePooledSetup", "IGlobalAudioService not available in global DI");
                return;
            }

            if (pooled2dCue == null && pooled3dCue == null)
            {
                LogError("ValidatePooledSetup", "pooled2dCue and pooled3dCue are both null");
                return;
            }

            if (pooledVoiceProfile == null)
            {
                LogError("ValidatePooledSetup", "pooledVoiceProfile is null");
                return;
            }

            if (pooledVoiceProfile.PooledVoicePoolDefinition == null)
            {
                LogError("ValidatePooledSetup", $"pooledVoiceProfile='{pooledVoiceProfile.name}' has null pool definition");
                return;
            }

            if (pooled2dCue != null && pooled2dCue.ExecutionMode != AudioSfxExecutionMode.PooledOneShot)
            {
                LogError("ValidatePooledSetup", $"pooled2dCue='{pooled2dCue.name}' must use executionMode='PooledOneShot'");
                return;
            }

            if (pooled3dCue != null && pooled3dCue.ExecutionMode != AudioSfxExecutionMode.PooledOneShot)
            {
                LogError("ValidatePooledSetup", $"pooled3dCue='{pooled3dCue.name}' must use executionMode='PooledOneShot'");
                return;
            }

            var effective2d = ResolveEffectiveProfile(pooled2dCue, pooledVoiceProfile, out string source2d);
            var effective3d = ResolveEffectiveProfile(pooled3dCue, pooledVoiceProfile, out string source3d);

            LogInfo("ValidatePooledSetup",
                $"ok pooled2d='{SafeName(pooled2dCue)}' pooled3d='{SafeName(pooled3dCue)}' profileDefault='{SafeName(pooledVoiceProfile)}' effective2d='{SafeName(effective2d)}' source2d='{source2d}' effective3d='{SafeName(effective3d)}' source3d='{source3d}' pool='{SafeName(pooledVoiceProfile.PooledVoicePoolDefinition)}' allowDirectFallback={pooledVoiceProfile.AllowDirectFallback}");
        }

        [ContextMenu("QA/Audio/SFX/Pooled/Play 2D")]
        private void PlayPooled2d()
        {
            if (!TryEnsureService() || !TryValidateCue(pooled2dCue, "PlayPooled2d"))
            {
                return;
            }

            var context = AttachVoiceProfile(
                AudioPlaybackContext.Global(reason: "qa_pooled_play_2d"),
                pooledVoiceProfile);
            PlayAndLog(pooled2dCue, context, "PlayPooled2d");
        }

        [ContextMenu("QA/Audio/SFX/Pooled/Play 3D")]
        private void PlayPooled3d()
        {
            if (!TryEnsureService() || !TryValidateCue(pooled3dCue, "PlayPooled3d"))
            {
                return;
            }

            var context = AttachVoiceProfile(
                AudioPlaybackContext.Spatial(
                    worldPosition: spatialFollowTarget != null ? spatialFollowTarget.position : spatialProbePosition,
                    followTarget: spatialFollowTarget,
                    reason: "qa_pooled_play_3d"),
                pooledVoiceProfile);
            PlayAndLog(pooled3dCue, context, "PlayPooled3d");
        }

        [ContextMenu("QA/Audio/SFX/Pooled/Probe Restart Existing")]
        private void ProbePooledRestartExisting()
        {
            if (!TryEnsureService() || !TryValidateCue(pooled2dCue, "ProbePooledRestartExisting"))
            {
                return;
            }

            if (_pooledRetriggerRoutine != null)
            {
                StopCoroutine(_pooledRetriggerRoutine);
                _pooledRetriggerRoutine = null;
                LogInfo("ProbePooledRestartExisting", "previous probe interrupted");
            }

            _pooledRetriggerRoutine = StartCoroutine(ProbePooledRestartExistingRoutine());
        }

        [ContextMenu("QA/Audio/SFX/Pooled/Probe Budget (Forced Diagnostic)")]
        private void ProbePooledBudgetForced()
        {
            if (!TryEnsureService())
            {
                return;
            }

            var cue = pooled3dCue != null ? pooled3dCue : pooled2dCue;
            if (!TryValidateCue(cue, "ProbePooledBudgetForced"))
            {
                return;
            }

            if (_pooledBudgetRoutine != null)
            {
                StopCoroutine(_pooledBudgetRoutine);
                _pooledBudgetRoutine = null;
            }

            _pooledBudgetRoutine = StartCoroutine(ProbePooledBudgetForcedRoutine(cue));
        }

        [ContextMenu("QA/Audio/SFX/Pooled/Probe Fallback (Forced Diagnostic)")]
        private void ProbePooledFallbackForced()
        {
            if (!TryEnsureService() || !TryValidateCue(pooled2dCue, "ProbePooledFallbackForced"))
            {
                return;
            }

            var baseEffectiveProfile = ResolveEffectiveProfile(pooled2dCue, pooledVoiceProfile, out _);
            if (baseEffectiveProfile == null)
            {
                LogError("ProbePooledFallbackForced", "cannot resolve base effective profile");
                return;
            }

            var forcedFallbackProfile = CreateRuntimeVoiceProfileClone(
                sourceProfile: baseEffectiveProfile,
                poolDefinition: null,
                allowDirectFallback: true,
                defaultVoiceBudget: Mathf.Max(1, baseEffectiveProfile.DefaultVoiceBudget),
                releaseGraceSeconds: baseEffectiveProfile.ReleaseGraceSeconds);

            var probeCue = CreateRuntimeCueClone(
                sourceCue: pooled2dCue,
                executionMode: AudioSfxExecutionMode.PooledOneShot,
                playbackMode: AudioSfxPlaybackMode.Global,
                voiceProfileOverride: forcedFallbackProfile,
                maxSimultaneousInstances: 64,
                cooldownSeconds: 0f,
                forceLoop: false);

            var context = AudioPlaybackContext.Global(reason: "qa_sfx_probe_pooled_fallback_forced");
            var handle = _globalAudioService.Play(probeCue, context);
            _lastHandle = handle ?? NullAudioPlaybackHandle.Instance;

            bool valid = handle != null && handle.IsValid;
            string inferredPath = InferPathFromHandle(handle);
            LogInfo("ProbePooledFallbackForced",
                $"cue='{pooled2dCue.name}' effectiveProfile='cue_override:{SafeName(forcedFallbackProfile)}' expectedPath='fallback_direct' inferredPath='{inferredPath}' handleValid={valid} (check runtime log path='fallback_direct')");

            if (probeCue != null)
            {
                Destroy(probeCue);
            }

            if (forcedFallbackProfile != null)
            {
                Destroy(forcedFallbackProfile);
            }
        }

        [ContextMenu("QA/Audio/SFX/Pooled/Probe Sequence Reuse")]
        private void ProbePooledSequenceReuse()
        {
            if (!TryEnsureService())
            {
                return;
            }

            var cue = pooled3dCue != null ? pooled3dCue : pooled2dCue;
            if (!TryValidateCue(cue, "ProbePooledSequenceReuse"))
            {
                return;
            }

            if (_pooledSequenceRoutine != null)
            {
                StopCoroutine(_pooledSequenceRoutine);
                _pooledSequenceRoutine = null;
            }

            _pooledSequenceRoutine = StartCoroutine(ProbePooledSequenceReuseRoutine(cue));
        }

        [ContextMenu("QA/Audio/SFX/Pooled/Log State")]
        private void LogPooledState()
        {
            bool serviceResolved = TryEnsureService();
            string pool = pooledVoiceProfile != null && pooledVoiceProfile.PooledVoicePoolDefinition != null
                ? pooledVoiceProfile.PooledVoicePoolDefinition.name
                : "null";
            var effective2d = ResolveEffectiveProfile(pooled2dCue, pooledVoiceProfile, out string source2d);
            var effective3d = ResolveEffectiveProfile(pooled3dCue, pooledVoiceProfile, out string source3d);

            DebugUtility.Log(typeof(AudioSfxPooledQaSceneHarness),
                $"[QA][Audio][SFX][Pooled] action='LogPooledState' serviceResolved={serviceResolved} pooled2d='{SafeName(pooled2dCue)}' pooled3d='{SafeName(pooled3dCue)}' profileDefault='{SafeName(pooledVoiceProfile)}' poolDefault='{pool}' effective2d='{SafeName(effective2d)}' effective2dSource='{source2d}' effective3d='{SafeName(effective3d)}' effective3dSource='{source3d}' budget={(pooledVoiceProfile != null ? pooledVoiceProfile.DefaultVoiceBudget : 0)} releaseGrace={(pooledVoiceProfile != null ? pooledVoiceProfile.ReleaseGraceSeconds : 0f):0.###}.",
                DebugUtility.Colors.Info);
        }

        [ContextMenu("QA/Audio/SFX/Pooled/Stop Last Handle")]
        private void StopLastHandle()
        {
            if (_lastHandle == null || !_lastHandle.IsValid)
            {
                LogInfo("StopLastHandle", "no valid handle to stop");
                return;
            }

            _lastHandle.Stop();
            LogInfo("StopLastHandle", $"requested handleIsPlayingAfterStop={_lastHandle.IsPlaying}");
        }

        private IEnumerator ProbePooledRestartExistingRoutine()
        {
            float delay = Mathf.Max(0.01f, pooledRetriggerInterruptDelaySeconds);
            int repeats = Mathf.Max(1, pooledRetriggerRepeatCount);

            LogInfo("ProbePooledRestartExisting",
                $"start cue='{pooled2dCue.name}' delay={delay:0.###} repeatCount={repeats} profileDefault='{SafeName(pooledVoiceProfile)}'");

            var firstContext = AttachVoiceProfile(
                AudioPlaybackContext.Global(reason: "qa_pooled_restart_first"),
                pooledVoiceProfile);
            var first = _globalAudioService.Play(pooled2dCue, firstContext);
            _lastHandle = first ?? NullAudioPlaybackHandle.Instance;

            for (int i = 0; i < repeats; i++)
            {
                yield return new WaitForSeconds(delay);

                var nextContext = AttachVoiceProfile(
                    AudioPlaybackContext.Global(reason: $"qa_pooled_restart_repeat_{i + 1}"),
                    pooledVoiceProfile);
                var next = _globalAudioService.Play(pooled2dCue, nextContext);
                _lastHandle = next ?? NullAudioPlaybackHandle.Instance;

                LogInfo("ProbePooledRestartExisting",
                    $"step='retrigger_play' index={i + 1} handleValid={(next != null && next.IsValid)} expected='restart_existing'");
            }

            LogInfo("ProbePooledRestartExisting",
                $"complete cue='{pooled2dCue.name}' delay={delay:0.###} repeatCount={repeats} lastHandleValid={(_lastHandle != null && _lastHandle.IsValid)}");
            _pooledRetriggerRoutine = null;
        }

        private IEnumerator ProbePooledBudgetForcedRoutine(AudioSfxCueAsset cue)
        {
            int attempts = Mathf.Max(2, pooledBudgetBurstCount);
            float stepDelay = Mathf.Max(0f, pooledBudgetStepDelaySeconds);

            var baseEffectiveProfile = ResolveEffectiveProfile(cue, pooledVoiceProfile, out string source);
            if (baseEffectiveProfile == null || baseEffectiveProfile.PooledVoicePoolDefinition == null)
            {
                LogError("ProbePooledBudgetForced", $"aborted: missing effective pooled profile for cue='{cue.name}' source='{source}'");
                _pooledBudgetRoutine = null;
                yield break;
            }

            var forcedBudgetProfile = CreateRuntimeVoiceProfileClone(
                sourceProfile: baseEffectiveProfile,
                poolDefinition: baseEffectiveProfile.PooledVoicePoolDefinition,
                allowDirectFallback: false,
                defaultVoiceBudget: 1,
                releaseGraceSeconds: baseEffectiveProfile.ReleaseGraceSeconds);

            var probeCue = CreateRuntimeCueClone(
                sourceCue: cue,
                executionMode: AudioSfxExecutionMode.PooledOneShot,
                playbackMode: AudioSfxPlaybackMode.Spatial,
                voiceProfileOverride: forcedBudgetProfile,
                maxSimultaneousInstances: 64,
                cooldownSeconds: 0f,
                forceLoop: true);

            LogInfo("ProbePooledBudgetForced",
                $"start cue='{cue.name}' attempts={attempts} stepDelay={stepDelay:0.###} expectedPolicy='block_budget' effectiveProfile='cue_override:{forcedBudgetProfile.name}' source='{source}'");

            var firstContext = AudioPlaybackContext.Spatial(
                worldPosition: spatialFollowTarget != null ? spatialFollowTarget.position : spatialProbePosition,
                followTarget: spatialFollowTarget,
                reason: "qa_pooled_budget_forced_first");
            var firstHandle = _globalAudioService.Play(probeCue, firstContext);
            _lastHandle = firstHandle ?? NullAudioPlaybackHandle.Instance;
            bool firstValid = firstHandle != null && firstHandle.IsValid;

            yield return null;

            var secondContext = AudioPlaybackContext.Spatial(
                worldPosition: spatialFollowTarget != null ? spatialFollowTarget.position : spatialProbePosition,
                followTarget: spatialFollowTarget,
                reason: "qa_pooled_budget_forced_second");
            var secondHandle = _globalAudioService.Play(probeCue, secondContext);
            bool secondValid = secondHandle != null && secondHandle.IsValid;
            if (secondValid)
            {
                _lastHandle = secondHandle;
            }

            int additionalValid = 0;
            int additionalBlocked = 0;
            int additionalAttempts = Mathf.Max(0, attempts - 2);
            for (int i = 0; i < additionalAttempts; i++)
            {
                var context = AudioPlaybackContext.Spatial(
                    worldPosition: spatialFollowTarget != null ? spatialFollowTarget.position : spatialProbePosition,
                    followTarget: spatialFollowTarget,
                    reason: $"qa_pooled_budget_forced_extra_{i + 1}");

                var handle = _globalAudioService.Play(probeCue, context);
                if (handle != null && handle.IsValid)
                {
                    additionalValid++;
                    _lastHandle = handle;
                }
                else
                {
                    additionalBlocked++;
                }

                if (stepDelay > 0f)
                {
                    yield return new WaitForSeconds(stepDelay);
                }
            }

            LogInfo("ProbePooledBudgetForced",
                $"complete cue='{cue.name}' firstHandleValid={firstValid} secondHandleValid={secondValid} expectedSecond='blocked_by_budget' additionalValid={additionalValid} additionalBlocked={additionalBlocked} (check runtime log policy='block_budget')");

            if (firstHandle != null && firstHandle.IsValid)
            {
                firstHandle.Stop();
            }

            if (probeCue != null)
            {
                Destroy(probeCue);
            }

            if (forcedBudgetProfile != null)
            {
                Destroy(forcedBudgetProfile);
            }

            _pooledBudgetRoutine = null;
        }

        private IEnumerator ProbePooledSequenceReuseRoutine(AudioSfxCueAsset cue)
        {
            int count = Mathf.Max(1, pooledSequenceCount);
            float stepDelay = Mathf.Max(0.01f, pooledSequenceStepDelaySeconds);
            float waitTimeout = Mathf.Max(0.25f, pooledSequenceWaitTimeoutSeconds);
            int validCount = 0;
            int blockedCount = 0;
            int completedBeforeTimeoutCount = 0;

            LogInfo("ProbePooledSequenceReuse",
                $"start cue='{cue.name}' count={count} stepDelay={stepDelay:0.###} waitTimeout={waitTimeout:0.###} expected='rent_play_complete_return_cycles'");

            for (int i = 0; i < count; i++)
            {
                var context = cue.PlaybackMode == AudioSfxPlaybackMode.Spatial
                    ? AttachVoiceProfile(
                        AudioPlaybackContext.Spatial(
                            worldPosition: spatialFollowTarget != null ? spatialFollowTarget.position : spatialProbePosition,
                            followTarget: spatialFollowTarget,
                            reason: $"qa_pooled_sequence_{i + 1}"),
                        pooledVoiceProfile)
                    : AttachVoiceProfile(
                        AudioPlaybackContext.Global(reason: $"qa_pooled_sequence_{i + 1}"),
                        pooledVoiceProfile);

                var handle = _globalAudioService.Play(cue, context);
                if (handle != null && handle.IsValid)
                {
                    validCount++;
                    _lastHandle = handle;
                }
                else
                {
                    blockedCount++;
                }

                bool completedBeforeTimeout = false;
                if (handle != null && handle.IsValid)
                {
                    float waited = 0f;
                    while (handle.IsValid && handle.IsPlaying && waited < waitTimeout)
                    {
                        waited += Time.unscaledDeltaTime;
                        yield return null;
                    }

                    completedBeforeTimeout = !handle.IsPlaying;
                    if (completedBeforeTimeout)
                    {
                        completedBeforeTimeoutCount++;
                    }

                    LogInfo("ProbePooledSequenceReuse",
                        $"step index={i + 1} handleValid=True completedBeforeTimeout={completedBeforeTimeout} waited={waited:0.###}");
                }
                else
                {
                    LogInfo("ProbePooledSequenceReuse", $"step index={i + 1} handleValid=False");
                }

                yield return new WaitForSeconds(stepDelay);
            }

            yield return new WaitForSeconds(Mathf.Max(0.75f, stepDelay));
            LogInfo("ProbePooledSequenceReuse",
                $"complete cue='{cue.name}' valid={validCount} blocked={blockedCount} completedBeforeTimeout={completedBeforeTimeoutCount} expectedRuntimeLogs='Pool rent + Playback complete + Pool return + Pool rent'");
            _pooledSequenceRoutine = null;
        }

        private void PlayAndLog(AudioSfxCueAsset cue, AudioPlaybackContext context, string action)
        {
            var handle = _globalAudioService.Play(cue, context);
            _lastHandle = handle ?? NullAudioPlaybackHandle.Instance;

            bool valid = handle != null && handle.IsValid;
            bool playing = handle != null && handle.IsPlaying;
            var effectiveProfile = ResolveEffectiveProfile(cue, context.VoiceProfile, out string profileSource);
            LogInfo(action,
                $"cue='{cue.name}' handleValid={valid} isPlaying={playing} contextProfile='{SafeName(context.VoiceProfile)}' effectiveProfile='{SafeName(effectiveProfile)}' effectiveProfileSource='{profileSource}' reason='{(string.IsNullOrWhiteSpace(context.Reason) ? "unspecified" : context.Reason)}'");
        }

        private bool TryEnsureService()
        {
            if (_globalAudioService != null)
            {
                return true;
            }

            if (!Application.isPlaying)
            {
                return false;
            }

            if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
            {
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGlobalAudioService>(out _globalAudioService) || _globalAudioService == null)
            {
                return false;
            }

            LogInfo("ResolveService", "IGlobalAudioService resolved from global DI");
            return true;
        }

        private bool TryValidateCue(AudioSfxCueAsset cue, string action)
        {
            if (cue != null)
            {
                return true;
            }

            LogError(action, "cue is null");
            return false;
        }

        private AudioPlaybackContext AttachVoiceProfile(AudioPlaybackContext context, AudioSfxVoiceProfileAsset profile)
        {
            context.VoiceProfile = profile;
            return context;
        }

        private AudioSfxVoiceProfileAsset ResolveEffectiveProfile(
            AudioSfxCueAsset cue,
            AudioSfxVoiceProfileAsset contextProfile,
            out string source)
        {
            if (cue != null && cue.VoiceProfileOverride != null)
            {
                source = "cue_override";
                return cue.VoiceProfileOverride;
            }

            if (contextProfile != null)
            {
                source = "context";
                return contextProfile;
            }

            source = "none";
            return null;
        }

        private static string InferPathFromHandle(IAudioPlaybackHandle handle)
        {
            var component = handle as Component;
            if (component == null || component.gameObject == null)
            {
                return "unknown";
            }

            string objectName = component.gameObject.name ?? string.Empty;
            if (objectName.StartsWith("SFX_", StringComparison.Ordinal))
            {
                return "fallback_direct";
            }

            return "pooled_or_other";
        }

        private static AudioSfxCueAsset CreateRuntimeCueClone(
            AudioSfxCueAsset sourceCue,
            AudioSfxExecutionMode executionMode,
            AudioSfxPlaybackMode playbackMode,
            AudioSfxVoiceProfileAsset voiceProfileOverride,
            int maxSimultaneousInstances,
            float cooldownSeconds,
            bool forceLoop)
        {
            var clone = Instantiate(sourceCue);
            clone.name = $"{sourceCue.name}_RuntimeProbeClone";
            SetPrivateField(clone, "executionMode", executionMode);
            SetPrivateField(clone, "playbackMode", playbackMode);
            SetPrivateField(clone, "voiceProfileOverride", voiceProfileOverride);
            SetPrivateField(clone, "maxSimultaneousInstances", Mathf.Max(1, maxSimultaneousInstances));
            SetPrivateField(clone, "sfxRetriggerCooldownSeconds", Mathf.Max(0f, cooldownSeconds));
            SetPrivateField(clone, "loop", forceLoop);
            return clone;
        }

        private static AudioSfxVoiceProfileAsset CreateRuntimeVoiceProfileClone(
            AudioSfxVoiceProfileAsset sourceProfile,
            PoolDefinitionAsset poolDefinition,
            bool allowDirectFallback,
            int defaultVoiceBudget,
            float releaseGraceSeconds)
        {
            var clone = Instantiate(sourceProfile);
            clone.name = $"{sourceProfile.name}_RuntimeProbeClone";
            SetPrivateField(clone, "pooledVoicePoolDefinition", poolDefinition);
            SetPrivateField(clone, "allowDirectFallback", allowDirectFallback);
            SetPrivateField(clone, "defaultVoiceBudget", Mathf.Max(0, defaultVoiceBudget));
            SetPrivateField(clone, "releaseGraceSeconds", Mathf.Max(0f, releaseGraceSeconds));
            return clone;
        }

        private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
        {
            if (target == null || string.IsNullOrWhiteSpace(fieldName))
            {
                return;
            }

            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }
        }

        private static string SafeName(UnityEngine.Object obj)
        {
            return obj != null ? obj.name : "null";
        }

        private void LogInfo(string action, string detail)
        {
            if (!verboseLogs)
            {
                return;
            }

            DebugUtility.Log(typeof(AudioSfxPooledQaSceneHarness),
                $"[QA][Audio][SFX][Pooled] action='{action}' detail='{detail}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogError(string action, string detail)
        {
            DebugUtility.LogError(typeof(AudioSfxPooledQaSceneHarness),
                $"[QA][Audio][SFX][Pooled] action='{action}' detail='{detail}'.");
        }
    }
}
