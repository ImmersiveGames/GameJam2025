using System.Collections;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.Audio.Bindings;
using _ImmersiveGames.NewScripts.Experience.Audio.Config;
using _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Core;
using _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Models;
using _ImmersiveGames.NewScripts.Experience.Audio.Semantics;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Experience.Audio.QA
{
    /// <summary>
    /// Harness de QA para F7: emitter mínimo + prova de uso com e sem emitter.
    /// </summary>
    public sealed class AudioEntityEmitterQaSceneHarness : MonoBehaviour
    {
        [Header("Emitter Under Test")]
        [SerializeField] private EntityAudioEmitter emitterUnderTest;
        [SerializeField] private string emitterPurpose = "semantic_spatial_direct";

        [Header("No Emitter Path")]
        [SerializeField] private Transform ownerTransform;
        [SerializeField] private string noEmitterPurpose = "semantic_spatial_direct";
        [SerializeField] private Vector3 fallbackSpatialPosition = new Vector3(0f, 1.5f, 2f);
        [SerializeField] [Min(0f)] private float noEmitterVolumeScale = 1f;

        [Header("Shared Authoring")]
        [SerializeField] private EntityAudioSemanticMapAsset semanticMap;
        [SerializeField] private AudioSfxCueAsset explicitCue;

        [Header("Auto Stop QA")]
        [SerializeField] [Min(0f)] private float autoStopDelaySeconds = 0.05f;
        [SerializeField] private bool autoStopUseUnscaledTime = true;

        [SerializeField] private bool verboseLogs = true;

        private IEntityAudioService _entityAudioService;
        private IAudioPlaybackHandle _lastHandle = NullAudioPlaybackHandle.Instance;
        private Coroutine _autoStopRoutine;

        [ContextMenu("QA/Audio/EntityEmitter/Validate Setup")]
        private void ValidateSetup()
        {
            bool serviceResolved = TryEnsureService();
            bool emitterResolved = emitterUnderTest != null && emitterUnderTest.TryResolveService(out _);
            Transform effectiveOwner = ResolveNoEmitterOwner(out string ownerSource);

            LogInfo("ValidateSetup",
                $"serviceResolved={serviceResolved} emitter='{SafeName(emitterUnderTest)}' emitterResolved={emitterResolved} configuredOwner='{SafeName(ownerTransform)}' effectiveOwner='{SafeName(effectiveOwner)}' ownerSource='{ownerSource}' semanticMap='{SafeName(semanticMap)}' emitterPurpose='{emitterPurpose}' noEmitterPurpose='{noEmitterPurpose}' explicitCue='{SafeName(explicitCue)}'");
        }

        [ContextMenu("QA/Audio/EntityEmitter/Play Purpose Via Emitter")]
        private void PlayPurposeViaEmitter()
        {
            if (!TryEnsureEmitter())
            {
                return;
            }

            var handle = emitterUnderTest.PlayPurpose(
                purpose: emitterPurpose,
                reason: "qa_entity_emitter_purpose");

            LogHandle("PlayPurposeViaEmitter", handle, emitterPurpose);
        }

        [ContextMenu("QA/Audio/EntityEmitter/Play Purpose Via Emitter And Auto Stop")]
        private void PlayPurposeViaEmitterAndAutoStop()
        {
            if (!TryEnsureEmitter())
            {
                return;
            }

            var handle = emitterUnderTest.PlayPurpose(
                purpose: emitterPurpose,
                reason: "qa_entity_emitter_purpose_auto_stop");

            LogHandle("PlayPurposeViaEmitterAndAutoStop", handle, emitterPurpose);
            ScheduleAutoStop("PlayPurposeViaEmitterAndAutoStop");
        }

        [ContextMenu("QA/Audio/EntityEmitter/Play Cue Via Emitter")]
        private void PlayCueViaEmitter()
        {
            if (!TryEnsureEmitter())
            {
                return;
            }

            if (explicitCue == null)
            {
                LogError("PlayCueViaEmitter", "explicitCue is null");
                return;
            }

            var handle = emitterUnderTest.PlayCue(
                cue: explicitCue,
                reason: "qa_entity_emitter_cue");

            LogHandle("PlayCueViaEmitter", handle, explicitCue.name);
        }

        [ContextMenu("QA/Audio/EntityEmitter/Play Cue Via Emitter And Auto Stop")]
        private void PlayCueViaEmitterAndAutoStop()
        {
            if (!TryEnsureEmitter())
            {
                return;
            }

            if (explicitCue == null)
            {
                LogError("PlayCueViaEmitterAndAutoStop", "explicitCue is null");
                return;
            }

            var handle = emitterUnderTest.PlayCue(
                cue: explicitCue,
                reason: "qa_entity_emitter_cue_auto_stop");

            LogHandle("PlayCueViaEmitterAndAutoStop", handle, explicitCue.name);
            ScheduleAutoStop("PlayCueViaEmitterAndAutoStop");
        }

        [ContextMenu("QA/Audio/EntityEmitter/Play Purpose Without Emitter")]
        private void PlayPurposeWithoutEmitter()
        {
            if (!TryEnsureService())
            {
                LogError("PlayPurposeWithoutEmitter", "IEntityAudioService not available in global DI");
                return;
            }

            Transform owner = ResolveNoEmitterOwner(out string ownerSource);
            Vector3 position = owner != null ? owner.position : fallbackSpatialPosition;
            var context = AudioPlaybackContext.Spatial(
                worldPosition: position,
                followTarget: owner,
                reason: "qa_entity_no_emitter_purpose",
                volumeScale: Mathf.Max(0f, noEmitterVolumeScale));

            var handle = _entityAudioService.PlayPurpose(noEmitterPurpose, owner, context);
            LogHandle("PlayPurposeWithoutEmitter", handle, $"purpose='{noEmitterPurpose}' owner='{SafeName(owner)}' ownerSource='{ownerSource}'");
        }

        [ContextMenu("QA/Audio/EntityEmitter/Play Cue Without Emitter")]
        private void PlayCueWithoutEmitter()
        {
            if (!TryEnsureService())
            {
                LogError("PlayCueWithoutEmitter", "IEntityAudioService not available in global DI");
                return;
            }

            if (explicitCue == null)
            {
                LogError("PlayCueWithoutEmitter", "explicitCue is null");
                return;
            }

            Transform owner = ResolveNoEmitterOwner(out string ownerSource);
            Vector3 position = owner != null ? owner.position : fallbackSpatialPosition;
            var context = AudioPlaybackContext.Spatial(
                worldPosition: position,
                followTarget: owner,
                reason: "qa_entity_no_emitter_cue",
                volumeScale: Mathf.Max(0f, noEmitterVolumeScale));

            var handle = _entityAudioService.PlayCue(explicitCue, context);
            LogHandle("PlayCueWithoutEmitter", handle, $"cue='{explicitCue.name}' owner='{SafeName(owner)}' ownerSource='{ownerSource}'");
        }

        [ContextMenu("QA/Audio/EntityEmitter/Stop Last Handle")]
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

        [ContextMenu("QA/Audio/EntityEmitter/Log Harness State")]
        private void LogHarnessState()
        {
            bool serviceResolved = TryEnsureService();
            bool handleValid = _lastHandle != null && _lastHandle.IsValid;
            bool handlePlaying = _lastHandle != null && _lastHandle.IsPlaying;
            Transform effectiveOwner = ResolveNoEmitterOwner(out string ownerSource);

            DebugUtility.Log(typeof(AudioEntityEmitterQaSceneHarness),
                $"[QA][Audio][EntityEmitter] action='LogHarnessState' serviceResolved={serviceResolved} emitter='{SafeName(emitterUnderTest)}' configuredOwner='{SafeName(ownerTransform)}' effectiveOwner='{SafeName(effectiveOwner)}' ownerSource='{ownerSource}' semanticMap='{SafeName(semanticMap)}' explicitCue='{SafeName(explicitCue)}' autoStopDelaySeconds={autoStopDelaySeconds:0.###} autoStopUseUnscaledTime={autoStopUseUnscaledTime} lastHandleValid={handleValid} lastHandlePlaying={handlePlaying}.",
                DebugUtility.Colors.Info);
        }

        private bool TryEnsureEmitter()
        {
            if (emitterUnderTest == null)
            {
                LogError("ResolveEmitter", "emitterUnderTest is null");
                return false;
            }

            if (!TryEnsureService())
            {
                LogError("ResolveEmitter", "IEntityAudioService not available in global DI");
                return false;
            }

            if (!emitterUnderTest.TryResolveService(out var entityAudioService) || entityAudioService == null)
            {
                LogError("ResolveEmitter", "EntityAudioEmitter could not resolve IEntityAudioService");
                return false;
            }

            return true;
        }

        private bool TryEnsureService()
        {
            if (_entityAudioService != null)
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

            if (!DependencyManager.Provider.TryGetGlobal(out _entityAudioService) || _entityAudioService == null)
            {
                return false;
            }

            if (_entityAudioService is AudioEntitySemanticService semanticService)
            {
                semanticService.SetSemanticMap(semanticMap, "qa_entity_emitter_harness");
            }

            LogInfo("ResolveService", "IEntityAudioService resolved from global DI");
            return true;
        }

        private void LogHandle(string action, IAudioPlaybackHandle handle, string payload)
        {
            _lastHandle = handle ?? NullAudioPlaybackHandle.Instance;
            bool valid = handle != null && handle.IsValid;
            bool playing = handle != null && handle.IsPlaying;
            LogInfo(action, $"payload='{payload}' handleValid={valid} isPlaying={playing}");
        }

        private void ScheduleAutoStop(string originAction)
        {
            if (!Application.isPlaying)
            {
                LogError("ScheduleAutoStop", "application is not playing");
                return;
            }

            if (_autoStopRoutine != null)
            {
                StopCoroutine(_autoStopRoutine);
                _autoStopRoutine = null;
            }

            _autoStopRoutine = StartCoroutine(AutoStopLastHandleAfterDelay(originAction));
        }

        private IEnumerator AutoStopLastHandleAfterDelay(string originAction)
        {
            float delay = Mathf.Max(0f, autoStopDelaySeconds);

            if (delay > 0f)
            {
                if (autoStopUseUnscaledTime)
                {
                    yield return new WaitForSecondsRealtime(delay);
                }
                else
                {
                    yield return new WaitForSeconds(delay);
                }
            }
            else
            {
                yield return null;
            }

            bool handleValidBeforeStop = _lastHandle != null && _lastHandle.IsValid;
            bool handlePlayingBeforeStop = _lastHandle != null && _lastHandle.IsPlaying;

            LogInfo("AutoStopBeforeStop",
                $"origin='{originAction}' delay={delay:0.###} useUnscaledTime={autoStopUseUnscaledTime} handleValid={handleValidBeforeStop} isPlaying={handlePlayingBeforeStop}");

            StopLastHandle();

            bool handleValidAfterStop = _lastHandle != null && _lastHandle.IsValid;
            bool handlePlayingAfterStop = _lastHandle != null && _lastHandle.IsPlaying;

            LogInfo("AutoStopAfterStop",
                $"origin='{originAction}' delay={delay:0.###} useUnscaledTime={autoStopUseUnscaledTime} handleValid={handleValidAfterStop} isPlaying={handlePlayingAfterStop}");

            _autoStopRoutine = null;
        }

        private Transform ResolveNoEmitterOwner(out string ownerSource)
        {
            if (ownerTransform != null)
            {
                ownerSource = "configured_owner";
                return ownerTransform;
            }

            if (transform != null)
            {
                ownerSource = "harness_transform";
                return transform;
            }

            ownerSource = "fallback_position_only";
            return null;
        }

        private static string SafeName(Object target)
        {
            return target != null ? target.name : "null";
        }

        private void LogInfo(string action, string detail)
        {
            if (!verboseLogs)
            {
                return;
            }

            DebugUtility.Log(typeof(AudioEntityEmitterQaSceneHarness),
                $"[QA][Audio][EntityEmitter] action='{action}' detail='{detail}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogError(string action, string detail)
        {
            DebugUtility.LogError(typeof(AudioEntityEmitterQaSceneHarness),
                $"[QA][Audio][EntityEmitter] action='{action}' detail='{detail}'.");
        }
    }
}
