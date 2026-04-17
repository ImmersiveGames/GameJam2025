using System.Collections;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Experience.Audio.Bindings;
using ImmersiveGames.GameJam2025.Experience.Audio.Config;
using ImmersiveGames.GameJam2025.Experience.Audio.Runtime.Core;
using ImmersiveGames.GameJam2025.Experience.Audio.Runtime.Models;
using UnityEngine;

namespace ImmersiveGames.GameJam2025.Experience.Audio.QA
{
    /// <summary>
    /// Harness de QA para validar o emitter como binding puro de contexto.
    /// </summary>
    public sealed class AudioEntityEmitterQaSceneHarness : MonoBehaviour
    {
        [Header("Emitter Under Test")]
        [SerializeField] private EntityAudioEmitter emitterUnderTest;

        [Header("Cue Under Test")]
        [SerializeField] private AudioSfxCueAsset explicitCue;

        [Header("Owner Context")]
        [SerializeField] private Transform ownerTransform;
        [SerializeField] private Vector3 fallbackSpatialPosition = new Vector3(0f, 1.5f, 2f);

        [Header("Auto Stop QA")]
        [SerializeField] [Min(0f)] private float autoStopDelaySeconds = 0.05f;
        [SerializeField] private bool autoStopUseUnscaledTime = true;

        [SerializeField] private bool verboseLogs = true;

        private IAudioPlaybackHandle _lastHandle = NullAudioPlaybackHandle.Instance;
        private Coroutine _autoStopRoutine;

        [ContextMenu("QA/Audio/EntityEmitter/Validate Setup")]
        private void ValidateSetup()
        {
            Transform effectiveOwner = ResolveOwner(out string ownerSource);
            LogInfo("ValidateSetup",
                $"emitter='{SafeName(emitterUnderTest)}' explicitCue='{SafeName(explicitCue)}' configuredOwner='{SafeName(ownerTransform)}' effectiveOwner='{SafeName(effectiveOwner)}' ownerSource='{ownerSource}'");
        }

        [ContextMenu("QA/Audio/EntityEmitter/Play Cue Local")]
        private void PlayCueLocal()
        {
            if (!TryEnsureEmitter())
            {
                return;
            }

            if (explicitCue == null)
            {
                LogError("PlayCueLocal", "explicitCue is null");
                return;
            }

            var handle = emitterUnderTest.PlayCue(
                cue: explicitCue,
                reason: "qa_entity_emitter_cue_local");

            LogHandle("PlayCueLocal", handle, explicitCue.name);
            ScheduleAutoStop(nameof(PlayCueLocal));
        }

        [ContextMenu("QA/Audio/EntityEmitter/Play Cue Spatial")]
        private void PlayCueSpatial()
        {
            if (!TryEnsureEmitter())
            {
                return;
            }

            if (explicitCue == null)
            {
                LogError("PlayCueSpatial", "explicitCue is null");
                return;
            }

            Transform owner = ResolveOwner(out string ownerSource);
            Vector3 position = owner != null ? owner.position : fallbackSpatialPosition;
            AudioPlaybackContext context = AudioPlaybackContext.Spatial(
                worldPosition: position,
                followTarget: owner,
                reason: "qa_entity_emitter_cue_spatial");

            var handle = emitterUnderTest.PlayCue(explicitCue, context);
            LogHandle("PlayCueSpatial", handle, $"cue='{explicitCue.name}' owner='{SafeName(owner)}' ownerSource='{ownerSource}'");
            ScheduleAutoStop(nameof(PlayCueSpatial));
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
            bool handleValid = _lastHandle != null && _lastHandle.IsValid;
            bool handlePlaying = _lastHandle != null && _lastHandle.IsPlaying;
            Transform effectiveOwner = ResolveOwner(out string ownerSource);

            DebugUtility.Log(typeof(AudioEntityEmitterQaSceneHarness),
                $"[QA][Audio][EntityEmitter] action='LogHarnessState' emitter='{SafeName(emitterUnderTest)}' explicitCue='{SafeName(explicitCue)}' configuredOwner='{SafeName(ownerTransform)}' effectiveOwner='{SafeName(effectiveOwner)}' ownerSource='{ownerSource}' autoStopDelaySeconds={autoStopDelaySeconds:0.###} autoStopUseUnscaledTime={autoStopUseUnscaledTime} lastHandleValid={handleValid} lastHandlePlaying={handlePlaying}.",
                DebugUtility.Colors.Info);
        }

        private bool TryEnsureEmitter()
        {
            if (emitterUnderTest != null)
            {
                return true;
            }

            LogError("ResolveEmitter", "emitterUnderTest is null");
            return false;
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

        private Transform ResolveOwner(out string ownerSource)
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

