using System;
using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private AudioSfxCueAsset pooledSequenceCue;
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

            if (!IsCueConfiguredForPooled(pooled2dCue))
            {
                LogError("ValidatePooledSetup", $"pooled2dCue='{SafeName(pooled2dCue)}' must resolve execution as pooled (execution profile)");
                return;
            }

            if (!IsCueConfiguredForPooled(pooled3dCue))
            {
                LogError("ValidatePooledSetup", $"pooled3dCue='{SafeName(pooled3dCue)}' must resolve execution as pooled (execution profile)");
                return;
            }

            var effective2d = ResolveEffectiveProfile(pooled2dCue, null, out string source2d);
            var effective3d = ResolveEffectiveProfile(pooled3dCue, null, out string source3d);
            var effectivePool = effective3d != null && effective3d.PooledVoicePoolDefinition != null
                ? effective3d.PooledVoicePoolDefinition
                : (effective2d != null ? effective2d.PooledVoicePoolDefinition : null);

            LogInfo("ValidatePooledSetup",
                $"ok pooled2d='{SafeName(pooled2dCue)}' pooled3d='{SafeName(pooled3dCue)}' profileDefault='{SafeName(pooledVoiceProfile)}' effective2d='{SafeName(effective2d)}' source2d='{source2d}' effective3d='{SafeName(effective3d)}' source3d='{source3d}' pool='{SafeName(effectivePool)}' allowDirectFallback={(effective3d != null ? effective3d.AllowDirectFallback : (effective2d != null && effective2d.AllowDirectFallback))}");
        }

        [ContextMenu("QA/Audio/SFX/Pooled/Play 2D")]
        private void PlayPooled2d()
        {
            if (!TryEnsureService() || !TryValidateCue(pooled2dCue, "PlayPooled2d"))
            {
                return;
            }

            var context = BuildDefaultPooledContext(
                cue: pooled2dCue,
                context: AudioPlaybackContext.Global(reason: "qa_pooled_play_2d"));
            PlayAndLog(pooled2dCue, context, "PlayPooled2d");
        }

        [ContextMenu("QA/Audio/SFX/Pooled/Play 3D")]
        private void PlayPooled3d()
        {
            if (!TryEnsureService() || !TryValidateCue(pooled3dCue, "PlayPooled3d"))
            {
                return;
            }

            var context = BuildDefaultPooledContext(
                cue: pooled3dCue,
                context: AudioPlaybackContext.Spatial(
                    worldPosition: spatialFollowTarget != null ? spatialFollowTarget.position : spatialProbePosition,
                    followTarget: spatialFollowTarget,
                    reason: "qa_pooled_play_3d"));
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

            var baseEffectiveProfile = ResolveEffectiveProfile(pooled2dCue, null, out _);
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

            var context = BuildContextWithForcedProfile(
                cue: pooled2dCue,
                context: AudioPlaybackContext.Global(reason: "qa_sfx_probe_pooled_fallback_forced"),
                forcedProfile: forcedFallbackProfile);
            var handle = _globalAudioService.Play(pooled2dCue, context);
            _lastHandle = handle ?? NullAudioPlaybackHandle.Instance;

            bool valid = handle != null && handle.IsValid;
            string inferredPath = InferPathFromHandle(handle);
            LogInfo("ProbePooledFallbackForced",
                $"cue='{pooled2dCue.name}' effectiveProfile='context:{SafeName(forcedFallbackProfile)}' expectedPath='fallback_direct' inferredPath='{inferredPath}' handleValid={valid} (check runtime log path='fallback_direct')");

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

            var cue = ResolveSequenceCue(out string sequenceCueSource);
            if (!TryValidateCue(cue, "ProbePooledSequenceReuse"))
            {
                return;
            }

            if (!IsCueConfiguredForPooled(cue))
            {
                LogError("ProbePooledSequenceReuse", $"cue='{SafeName(cue)}' is not configured as pooled");
                return;
            }

            if (_pooledSequenceRoutine != null)
            {
                StopCoroutine(_pooledSequenceRoutine);
                _pooledSequenceRoutine = null;
            }

            _pooledSequenceRoutine = StartCoroutine(ProbePooledSequenceReuseRoutine(cue, sequenceCueSource));
        }

        [ContextMenu("QA/Audio/SFX/Pooled/Log Harness State")]
        private void LogPooledState()
        {
            bool serviceResolved = TryEnsureService();
            string pool = pooledVoiceProfile != null && pooledVoiceProfile.PooledVoicePoolDefinition != null
                ? pooledVoiceProfile.PooledVoicePoolDefinition.name
                : "null";
            var effective2d = ResolveEffectiveProfile(pooled2dCue, null, out string source2d);
            var effective3d = ResolveEffectiveProfile(pooled3dCue, null, out string source3d);

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

            var firstContext = BuildDefaultPooledContext(
                cue: pooled2dCue,
                context: AudioPlaybackContext.Global(reason: "qa_pooled_restart_first"));
            var first = _globalAudioService.Play(pooled2dCue, firstContext);
            _lastHandle = first ?? NullAudioPlaybackHandle.Instance;

            for (int i = 0; i < repeats; i++)
            {
                yield return new WaitForSeconds(delay);

                var nextContext = BuildDefaultPooledContext(
                    cue: pooled2dCue,
                    context: AudioPlaybackContext.Global(reason: $"qa_pooled_restart_repeat_{i + 1}"));
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

            var baseEffectiveProfile = ResolveEffectiveProfile(cue, null, out string source);
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

            LogInfo("ProbePooledBudgetForced",
                $"start cue='{cue.name}' attempts={attempts} stepDelay={stepDelay:0.###} expectedPolicy='block_budget' effectiveProfile='context:{forcedBudgetProfile.name}' source='{source}'");

            var firstContext = BuildContextWithForcedProfile(
                cue: cue,
                context: AudioPlaybackContext.Spatial(
                    worldPosition: spatialFollowTarget != null ? spatialFollowTarget.position : spatialProbePosition,
                    followTarget: spatialFollowTarget,
                    reason: "qa_pooled_budget_forced_first"),
                forcedProfile: forcedBudgetProfile);
            var firstHandle = _globalAudioService.Play(cue, firstContext);
            _lastHandle = firstHandle ?? NullAudioPlaybackHandle.Instance;
            bool firstValid = firstHandle != null && firstHandle.IsValid;

            yield return null;

            var secondContext = BuildContextWithForcedProfile(
                cue: cue,
                context: AudioPlaybackContext.Spatial(
                    worldPosition: spatialFollowTarget != null ? spatialFollowTarget.position : spatialProbePosition,
                    followTarget: spatialFollowTarget,
                    reason: "qa_pooled_budget_forced_second"),
                forcedProfile: forcedBudgetProfile);
            var secondHandle = _globalAudioService.Play(cue, secondContext);
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
                var context = BuildContextWithForcedProfile(
                    cue: cue,
                    context: AudioPlaybackContext.Spatial(
                        worldPosition: spatialFollowTarget != null ? spatialFollowTarget.position : spatialProbePosition,
                        followTarget: spatialFollowTarget,
                        reason: $"qa_pooled_budget_forced_extra_{i + 1}"),
                    forcedProfile: forcedBudgetProfile);

                var handle = _globalAudioService.Play(cue, context);
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

            if (forcedBudgetProfile != null)
            {
                Destroy(forcedBudgetProfile);
            }

            _pooledBudgetRoutine = null;
        }

        private IEnumerator ProbePooledSequenceReuseRoutine(AudioSfxCueAsset cue, string sequenceCueSource)
        {
            int count = Mathf.Max(1, pooledSequenceCount);
            float stepDelay = Mathf.Max(0.01f, pooledSequenceStepDelaySeconds);
            int validCount = 0;
            int blockedCount = 0;
            int completedBeforeTimeoutCount = 0;

            var sequenceProbeCue = CreateSequenceProbeCue(cue, out float selectedClipLengthSeconds, out int sourceClipCount);
            var sequenceEffectiveProfile = ResolveEffectiveProfile(sequenceProbeCue, null, out string sequenceProfileSource);
            var sequencePool = sequenceEffectiveProfile != null ? sequenceEffectiveProfile.PooledVoicePoolDefinition : null;
            float profileReleaseGrace = sequenceEffectiveProfile != null ? Mathf.Max(0f, sequenceEffectiveProfile.ReleaseGraceSeconds) : 0f;
            float waitTimeout = Mathf.Max(
                0.25f,
                Mathf.Max(
                    pooledSequenceWaitTimeoutSeconds,
                    selectedClipLengthSeconds > 0f ? selectedClipLengthSeconds + profileReleaseGrace + 0.5f : pooledSequenceWaitTimeoutSeconds));

            LogInfo("ProbePooledSequenceReuse",
                $"start cue='{cue.name}' sequenceCueSource='{sequenceCueSource}' probeCue='{sequenceProbeCue.name}' selectedClipLength={selectedClipLengthSeconds:0.###} sourceClipCount={sourceClipCount} count={count} stepDelay={stepDelay:0.###} waitTimeout={waitTimeout:0.###} effectiveProfile='{SafeName(sequenceEffectiveProfile)}' profileSource='{sequenceProfileSource}' pool='{SafeName(sequencePool)}' poolInitial={(sequencePool != null ? sequencePool.InitialSize : 0)} poolCanExpand={(sequencePool != null && sequencePool.CanExpand)} poolMax={(sequencePool != null ? sequencePool.MaxSize : 0)} poolAutoReturn={(sequencePool != null ? sequencePool.AutoReturnSeconds : 0f):0.###} expected='rent_play_complete_return_cycles'");

            for (int i = 0; i < count; i++)
            {
                var context = IsCueSpatial(sequenceProbeCue)
                    ? BuildDefaultPooledContext(
                        cue: sequenceProbeCue,
                        context: AudioPlaybackContext.Spatial(
                            worldPosition: spatialFollowTarget != null ? spatialFollowTarget.position : spatialProbePosition,
                            followTarget: spatialFollowTarget,
                            reason: $"qa_pooled_sequence_{i + 1}"))
                    : BuildDefaultPooledContext(
                        cue: sequenceProbeCue,
                        context: AudioPlaybackContext.Global(reason: $"qa_pooled_sequence_{i + 1}"));

                var handle = _globalAudioService.Play(sequenceProbeCue, context);
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
                    double waitStartAt = Time.realtimeSinceStartupAsDouble;
                    double waitDeadlineAt = waitStartAt + waitTimeout;
                    while (handle.IsValid && handle.IsPlaying && Time.realtimeSinceStartupAsDouble < waitDeadlineAt)
                    {
                        yield return null;
                    }

                    double waited = Math.Max(0d, Time.realtimeSinceStartupAsDouble - waitStartAt);
                    waited = Math.Min(waited, waitTimeout);
                    completedBeforeTimeout = !(handle.IsValid && handle.IsPlaying);
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
                $"complete cue='{cue.name}' probeCue='{sequenceProbeCue.name}' sequenceCueSource='{sequenceCueSource}' valid={validCount} blocked={blockedCount} completedBeforeTimeout={completedBeforeTimeoutCount} expectedRuntimeLogs='Pool rent + Playback complete + Pool return + Pool rent'");

            if (sequenceProbeCue != null)
            {
                Destroy(sequenceProbeCue);
            }

            _pooledSequenceRoutine = null;
        }

        private AudioSfxCueAsset ResolveSequenceCue(out string source)
        {
            if (pooledSequenceCue != null)
            {
                source = "pooled_sequence_cue";
                return pooledSequenceCue;
            }

            if (pooled2dCue != null)
            {
                source = "pooled2dCue";
                return pooled2dCue;
            }

            source = "pooled3dCue";
            return pooled3dCue;
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

            if (!DependencyManager.Provider.TryGetGlobal(out _globalAudioService) || _globalAudioService == null)
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

        private AudioSfxVoiceProfileAsset ResolveEffectiveProfile(
            AudioSfxCueAsset cue,
            AudioSfxVoiceProfileAsset contextProfile,
            out string source)
        {
            if (contextProfile != null)
            {
                source = "context";
                return contextProfile;
            }

            if (cue != null && cue.ExecutionProfile != null && cue.ExecutionProfile.PooledVoiceProfile != null)
            {
                source = "execution_profile";
                return cue.ExecutionProfile.PooledVoiceProfile;
            }

            source = "none";
            return null;
        }

        private AudioPlaybackContext BuildDefaultPooledContext(AudioSfxCueAsset cue, AudioPlaybackContext context)
        {
            return context;
        }

        private static AudioPlaybackContext BuildContextWithForcedProfile(
            AudioSfxCueAsset cue,
            AudioPlaybackContext context,
            AudioSfxVoiceProfileAsset forcedProfile)
        {
            context.VoiceProfile = forcedProfile;
            return context;
        }

        private static bool IsCueConfiguredForPooled(AudioSfxCueAsset cue)
        {
            return cue != null &&
                   cue.EmissionProfile != null &&
                   cue.ExecutionProfile != null &&
                   cue.ExecutionProfile.ExecutionMode == AudioSfxExecutionMode.PooledOneShot &&
                   cue.ExecutionProfile.PooledVoiceProfile != null;
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

        private static bool IsCueSpatial(AudioSfxCueAsset cue)
        {
            return cue != null &&
                   cue.EmissionProfile != null &&
                   cue.EmissionProfile.EmissionMode == AudioSfxPlaybackMode.Spatial;
        }

        private static AudioSfxCueAsset CreateSequenceProbeCue(
            AudioSfxCueAsset sourceCue,
            out float selectedClipLengthSeconds,
            out int sourceClipCount)
        {
            var probeCue = Instantiate(sourceCue);
            probeCue.name = $"{sourceCue.name}_SequenceProbeClone";

            selectedClipLengthSeconds = 0f;
            sourceClipCount = sourceCue.Clips != null ? sourceCue.Clips.Count : 0;
            AudioClip selectedClip = null;
            float shortestLength = float.MaxValue;

            if (sourceCue.Clips != null)
            {
                for (int i = 0; i < sourceCue.Clips.Count; i++)
                {
                    var clip = sourceCue.Clips[i];
                    if (clip == null || clip.length <= 0f)
                    {
                        continue;
                    }

                    if (clip.length < shortestLength)
                    {
                        shortestLength = clip.length;
                        selectedClip = clip;
                    }
                }
            }

            if (selectedClip != null)
            {
                selectedClipLengthSeconds = selectedClip.length;
                var clips = new List<AudioClip> { selectedClip };
                SetPrivateField(probeCue, "clips", clips);
            }

            SetPrivateField(probeCue, "maxSimultaneousInstances", 1);
            SetPrivateField(probeCue, "sfxRetriggerCooldownSeconds", 0f);
            SetPrivateField(probeCue, "pitchMin", 1f);
            SetPrivateField(probeCue, "pitchMax", 1f);
            SetPrivateField(probeCue, "randomVolumeJitter", 0f);
            SetPrivateField(probeCue, "loop", false);
            return probeCue;
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
