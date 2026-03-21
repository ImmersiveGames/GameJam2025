using System.Collections;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.QA
{
    /// <summary>
    /// Harness manual para validar F4 (Global SFX direto, sem pooling).
    /// </summary>
    public sealed class AudioSfxQaSceneHarness : MonoBehaviour
    {
        [Header("SFX Cues")]
        [SerializeField] private AudioSfxCueAsset direct2dCue;
        [SerializeField] private AudioSfxCueAsset direct3dCue;

        [Header("3D QA")]
        [SerializeField] private Transform spatialFollowTarget;
        [SerializeField] private Vector3 spatialProbePosition = new Vector3(0f, 1.5f, 2f);

        [Header("Burst/Cooldown QA")]
        [SerializeField] [Min(1)] private int burstCount = 4;
        [SerializeField] [Min(0f)] private float burstStepDelaySeconds = 0.05f;

        [Header("Global Retrigger QA")]
        [SerializeField] [Min(0.01f)] private float globalRetriggerInterruptDelaySeconds = 0.15f;
        [SerializeField] [Min(1)] private int globalRetriggerRepeatCount = 1;

        [SerializeField] private bool verboseLogs = true;

        private IGlobalAudioService _globalAudioService;
        private IAudioPlaybackHandle _lastHandle = NullAudioPlaybackHandle.Instance;
        private Coroutine _burstRoutine;
        private Coroutine _globalRetriggerRoutine;

        [ContextMenu("QA/Audio/SFX/Validate Setup")]
        private void ValidateSetup()
        {
            if (!TryEnsureService())
            {
                LogError("ValidateSetup", "IGlobalAudioService not available in global DI");
                return;
            }

            string cue2d = direct2dCue != null ? direct2dCue.name : "null";
            string cue3d = direct3dCue != null ? direct3dCue.name : "null";
            string follow = spatialFollowTarget != null ? spatialFollowTarget.name : "null";
            LogInfo("ValidateSetup", $"ok cue2d='{cue2d}' cue3d='{cue3d}' followTarget='{follow}'");
        }

        [ContextMenu("QA/Audio/SFX/Play Direct 2D")]
        private void PlayDirect2d()
        {
            if (!TryEnsureService() || !TryValidateCue(direct2dCue, "PlayDirect2d"))
            {
                return;
            }

            PlayAndLog(
                cue: direct2dCue,
                context: AudioPlaybackContext.Global(reason: "qa_sfx_play_2d"),
                action: "PlayDirect2d");
        }

        [ContextMenu("QA/Audio/SFX/Play Direct 3D Position")]
        private void PlayDirect3dPosition()
        {
            if (!TryEnsureService() || !TryValidateCue(direct3dCue, "PlayDirect3dPosition"))
            {
                return;
            }

            PlayAndLog(
                cue: direct3dCue,
                context: AudioPlaybackContext.Spatial(
                    worldPosition: spatialProbePosition,
                    reason: "qa_sfx_play_3d_position"),
                action: "PlayDirect3dPosition");
        }

        [ContextMenu("QA/Audio/SFX/Play Direct 3D Follow")]
        private void PlayDirect3dFollow()
        {
            if (!TryEnsureService() || !TryValidateCue(direct3dCue, "PlayDirect3dFollow"))
            {
                return;
            }

            if (spatialFollowTarget == null)
            {
                LogError("PlayDirect3dFollow", "spatialFollowTarget is null");
                return;
            }

            PlayAndLog(
                cue: direct3dCue,
                context: AudioPlaybackContext.Spatial(
                    worldPosition: spatialFollowTarget.position,
                    followTarget: spatialFollowTarget,
                    reason: "qa_sfx_play_3d_follow"),
                action: "PlayDirect3dFollow");
        }

        [ContextMenu("QA/Audio/SFX/Probe Global Restart Interrupt")]
        private void ProbeGlobalRestartInterrupt()
        {
            if (!TryEnsureService() || !TryValidateCue(direct2dCue, "ProbeGlobalRestartInterrupt"))
            {
                return;
            }

            if (_globalRetriggerRoutine != null)
            {
                StopCoroutine(_globalRetriggerRoutine);
                _globalRetriggerRoutine = null;
                LogInfo("ProbeGlobalRestartInterrupt", "previous probe interrupted");
            }

            _globalRetriggerRoutine = StartCoroutine(ProbeGlobalRestartInterruptRoutine());
        }

        [ContextMenu("QA/Audio/SFX/Burst Simultaneous")]
        private void BurstSimultaneous()
        {
            if (!TryEnsureService() || !TryValidateCue(direct2dCue, "BurstSimultaneous"))
            {
                return;
            }

            if (_burstRoutine != null)
            {
                StopCoroutine(_burstRoutine);
                _burstRoutine = null;
            }

            _burstRoutine = StartCoroutine(BurstRoutine());
        }

        [ContextMenu("QA/Audio/SFX/Stop Last Handle")]
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

        [ContextMenu("QA/Audio/SFX/Log Harness State")]
        private void LogHarnessState()
        {
            bool serviceResolved = TryEnsureService();
            string cue2d = direct2dCue != null ? direct2dCue.name : "null";
            string cue3d = direct3dCue != null ? direct3dCue.name : "null";
            bool lastValid = _lastHandle != null && _lastHandle.IsValid;
            bool lastPlaying = _lastHandle != null && _lastHandle.IsPlaying;

            DebugUtility.Log(typeof(AudioSfxQaSceneHarness),
                $"[QA][Audio][SFX] action='LogHarnessState' serviceResolved={serviceResolved} cue2d='{cue2d}' cue3d='{cue3d}' lastHandleValid={lastValid} lastHandlePlaying={lastPlaying}.",
                DebugUtility.Colors.Info);
        }

        private IEnumerator BurstRoutine()
        {
            int attempts = Mathf.Max(1, burstCount);
            float stepDelay = Mathf.Max(0f, burstStepDelaySeconds);
            int validCount = 0;
            int blockedCount = 0;

            LogInfo("BurstSimultaneous", $"start attempts={attempts} stepDelay={stepDelay:0.###}");

            for (int i = 0; i < attempts; i++)
            {
                var handle = _globalAudioService.Play(
                    direct2dCue,
                    AudioPlaybackContext.Global(reason: $"qa_sfx_burst_{i + 1}"));

                if (handle != null && handle.IsValid)
                {
                    validCount++;
                    _lastHandle = handle;
                }
                else
                {
                    blockedCount++;
                }

                if (stepDelay > 0f)
                {
                    yield return new WaitForSeconds(stepDelay);
                }
            }

            LogInfo("BurstSimultaneous", $"complete valid={validCount} blocked={blockedCount} cue='{direct2dCue.name}'");
            _burstRoutine = null;
        }

        private IEnumerator ProbeGlobalRestartInterruptRoutine()
        {
            if (!TryEnsureService() || !TryValidateCue(direct2dCue, "ProbeGlobalRestartInterrupt"))
            {
                LogError("ProbeGlobalRestartInterrupt", "aborted: missing service or direct2dCue");
                _globalRetriggerRoutine = null;
                yield break;
            }

            float delay = Mathf.Max(0.01f, globalRetriggerInterruptDelaySeconds);
            int repeats = Mathf.Max(1, globalRetriggerRepeatCount);

            LogInfo("ProbeGlobalRestartInterrupt",
                $"start cue='{direct2dCue.name}' delay={delay:0.###} repeatCount={repeats}");

            var first = _globalAudioService.Play(
                direct2dCue,
                AudioPlaybackContext.Global(reason: "qa_sfx_restart_interrupt_first"));
            _lastHandle = first ?? NullAudioPlaybackHandle.Instance;

            LogInfo("ProbeGlobalRestartInterrupt",
                $"step='first_play' firstHandleValid={(first != null && first.IsValid)}");

            for (int i = 0; i < repeats; i++)
            {
                yield return new WaitForSeconds(delay);

                var next = _globalAudioService.Play(
                    direct2dCue,
                    AudioPlaybackContext.Global(reason: $"qa_sfx_restart_interrupt_repeat_{i + 1}"));
                _lastHandle = next ?? NullAudioPlaybackHandle.Instance;

                LogInfo("ProbeGlobalRestartInterrupt",
                    $"step='retrigger_play' index={i + 1} handleValid={(next != null && next.IsValid)} expected='restart_existing'");
            }

            LogInfo("ProbeGlobalRestartInterrupt",
                $"complete cue='{direct2dCue.name}' delay={delay:0.###} repeatCount={repeats} lastHandleValid={(_lastHandle != null && _lastHandle.IsValid)}");
            _globalRetriggerRoutine = null;
        }

        private void PlayAndLog(AudioSfxCueAsset cue, AudioPlaybackContext context, string action)
        {
            var handle = _globalAudioService.Play(cue, context);
            _lastHandle = handle ?? NullAudioPlaybackHandle.Instance;

            bool valid = handle != null && handle.IsValid;
            bool playing = handle != null && handle.IsPlaying;
            LogInfo(action, $"cue='{cue.name}' handleValid={valid} isPlaying={playing} reason='{(string.IsNullOrWhiteSpace(context.Reason) ? "unspecified" : context.Reason)}'");
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

        private void LogInfo(string action, string detail)
        {
            if (!verboseLogs)
            {
                return;
            }

            DebugUtility.Log(typeof(AudioSfxQaSceneHarness),
                $"[QA][Audio][SFX] action='{action}' detail='{detail}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogError(string action, string detail)
        {
            DebugUtility.LogError(typeof(AudioSfxQaSceneHarness),
                $"[QA][Audio][SFX] action='{action}' detail='{detail}'.");
        }
    }
}
