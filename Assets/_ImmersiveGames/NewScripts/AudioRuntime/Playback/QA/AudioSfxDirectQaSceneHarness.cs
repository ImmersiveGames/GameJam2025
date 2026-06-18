using System.Collections;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.AudioRuntime.Playback.QA
{
    /// <summary>
    /// Harness dedicado ao trilho direto de SFX (F4).
    /// </summary>
    public sealed class AudioSfxDirectQaSceneHarness : MonoBehaviour
    {
        [Header("Direct Cues")]
        [SerializeField] private AudioSfxCueAsset direct2DCue;
        [SerializeField] private AudioSfxCueAsset direct3DCue;

        [Header("3D QA")]
        [SerializeField] private Transform spatialFollowTarget;
        [SerializeField] private Vector3 spatialProbePosition = new(0f, 1.5f, 2f);

        [Header("Burst/Cooldown QA")]
        [SerializeField] [Min(1)] private int burstCount = 4;
        [SerializeField] [Min(0f)] private float burstStepDelaySeconds = 0.05f;

        [SerializeField] private bool verboseLogs = true;

        private IGlobalAudioService _globalAudioService;
        private IAudioPlaybackHandle _lastHandle = NullAudioPlaybackHandle.Instance;
        private Coroutine _burstRoutine;

        [ContextMenu("QA/Audio/SFX/Direct/Validate Setup")]
        private void ValidateSetup()
        {
            if (!TryEnsureService())
            {
                LogError("ValidateSetup", "IGlobalAudioService not available in global DI");
                return;
            }

            LogInfo("ValidateSetup",
                $"ok direct2d='{SafeName(direct2DCue)}' direct2dEmission='{SafeName(direct2DCue != null ? direct2DCue.EmissionProfile : null)}' direct2dExecution='{SafeName(direct2DCue != null ? direct2DCue.ExecutionProfile : null)}' direct3d='{SafeName(direct3DCue)}' direct3dEmission='{SafeName(direct3DCue != null ? direct3DCue.EmissionProfile : null)}' direct3dExecution='{SafeName(direct3DCue != null ? direct3DCue.ExecutionProfile : null)}' followTarget='{SafeName(spatialFollowTarget)}'");
        }

        [ContextMenu("QA/Audio/SFX/Direct/Play 2D")]
        private void PlayDirect2D()
        {
            if (!TryEnsureService() || !TryValidateCue(direct2DCue, "PlayDirect2d"))
            {
                return;
            }

            PlayAndLog(
                direct2DCue,
                AudioPlaybackContext.Global(reason: "qa_direct_play_2d"),
                "PlayDirect2d");
        }

        [ContextMenu("QA/Audio/SFX/Direct/Play 3D Position")]
        private void PlayDirect3DPosition()
        {
            if (!TryEnsureService() || !TryValidateCue(direct3DCue, "PlayDirect3dPosition"))
            {
                return;
            }

            PlayAndLog(
                direct3DCue,
                AudioPlaybackContext.Spatial(
                    spatialProbePosition,
                    reason: "qa_direct_play_3d_position"),
                "PlayDirect3dPosition");
        }

        [ContextMenu("QA/Audio/SFX/Direct/Play 3D Follow")]
        private void PlayDirect3DFollow()
        {
            if (!TryEnsureService() || !TryValidateCue(direct3DCue, "PlayDirect3dFollow"))
            {
                return;
            }

            if (spatialFollowTarget == null)
            {
                LogError("PlayDirect3dFollow", "spatialFollowTarget is null");
                return;
            }

            PlayAndLog(
                direct3DCue,
                AudioPlaybackContext.Spatial(
                    spatialFollowTarget.position,
                    spatialFollowTarget,
                    "qa_direct_play_3d_follow"),
                "PlayDirect3dFollow");
        }

        [ContextMenu("QA/Audio/SFX/Direct/Burst Simultaneous")]
        private void BurstSimultaneous()
        {
            if (!TryEnsureService() || !TryValidateCue(direct2DCue, "BurstSimultaneous"))
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

        [ContextMenu("QA/Audio/SFX/Direct/Stop Last Handle")]
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

        [ContextMenu("QA/Audio/SFX/Direct/Log Harness State")]
        private void LogHarnessState()
        {
            bool serviceResolved = TryEnsureService();
            bool lastValid = _lastHandle is { IsValid: true };
            bool lastPlaying = _lastHandle is { IsPlaying: true };

            DebugUtility.LogVerbose(typeof(AudioSfxDirectQaSceneHarness),
                $"[QA][Audio][SFX][Direct] action='LogHarnessState' serviceResolved={serviceResolved} direct2d='{SafeName(direct2DCue)}' direct3d='{SafeName(direct3DCue)}' lastHandleValid={lastValid} lastHandlePlaying={lastPlaying}.",
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
                    direct2DCue,
                    AudioPlaybackContext.Global(reason: $"qa_direct_burst_{i + 1}"));

                if (handle is { IsValid: true })
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

            LogInfo("BurstSimultaneous", $"complete valid={validCount} blocked={blockedCount} cue='{direct2DCue.name}'");
            _burstRoutine = null;
        }

        private void PlayAndLog(AudioSfxCueAsset cue, AudioPlaybackContext context, string action)
        {
            var handle = _globalAudioService.Play(cue, context);
            _lastHandle = handle ?? NullAudioPlaybackHandle.Instance;

            bool valid = handle is { IsValid: true };
            bool playing = handle is { IsPlaying: true };
            LogInfo(action,
                $"cue='{cue.name}' handleValid={valid} isPlaying={playing} reason='{(string.IsNullOrWhiteSpace(context.reason) ? "unspecified" : context.reason)}'");
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

        private static string SafeName(Object obj)
        {
            return obj != null ? obj.name : "null";
        }

        private void LogInfo(string action, string detail)
        {
            if (!verboseLogs)
            {
                return;
            }

            DebugUtility.LogVerbose(typeof(AudioSfxDirectQaSceneHarness),
                $"[QA][Audio][SFX][Direct] action='{action}' detail='{detail}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogError(string action, string detail)
        {
            DebugUtility.LogError(typeof(AudioSfxDirectQaSceneHarness),
                $"[QA][Audio][SFX][Direct] action='{action}' detail='{detail}'.");
        }
    }
}
