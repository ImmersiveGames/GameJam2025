using System.Collections;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.QA
{
    /// <summary>
    /// Scene-level manual harness to validate F3 BGM runtime without route/navigation integrations.
    /// </summary>
    public sealed class AudioBgmQaSceneHarness : MonoBehaviour
    {
        [Header("BGM Cues")]
        [SerializeField] private AudioBgmCueAsset primaryCue;
        [SerializeField] private AudioBgmCueAsset alternateCue;

        [Header("QA Runtime")]
        [SerializeField] private bool playPrimaryOnStart;
        [SerializeField] [Min(0.1f)] private float scenarioStepDelaySeconds = 2f;
        [SerializeField] private bool verboseLogs = true;

        private IAudioBgmService _bgmService;
        private Coroutine _scenarioRoutine;

        private void Start()
        {
            if (!playPrimaryOnStart)
            {
                return;
            }

            PlayPrimary();
        }

        [ContextMenu("QA/Audio/BGM/Validate Setup")]
        private void ValidateSetup()
        {
            if (!TryEnsureService())
            {
                LogError("ValidateSetup", "IAudioBgmService not available in global DI");
                return;
            }

            if (primaryCue == null)
            {
                LogError("ValidateSetup", "primaryCue is null");
                return;
            }

            string alternateState = alternateCue != null ? alternateCue.name : "null";
            LogInfo("ValidateSetup",
                $"ok primary='{primaryCue.name}' alternate='{alternateState}' delay={scenarioStepDelaySeconds:0.###}");
        }

        [ContextMenu("QA/Audio/BGM/Play Primary")]
        private void PlayPrimary()
        {
            if (!TryEnsureService() || !TryValidateCue(primaryCue, "PlayPrimary"))
            {
                return;
            }

            _bgmService.Play(primaryCue, reason: "qa_play_primary");
            LogInfo("PlayPrimary", $"cue='{primaryCue.name}'");
        }

        [ContextMenu("QA/Audio/BGM/Play Alternate")]
        private void PlayAlternate()
        {
            if (!TryEnsureService() || !TryValidateCue(alternateCue, "PlayAlternate"))
            {
                return;
            }

            _bgmService.Play(alternateCue, reason: "qa_play_alternate");
            LogInfo("PlayAlternate", $"cue='{alternateCue.name}'");
        }

        [ContextMenu("QA/Audio/BGM/Crossfade To Primary")]
        private void CrossfadeToPrimary()
        {
            if (!TryEnsureService() || !TryValidateCue(primaryCue, "CrossfadeToPrimary"))
            {
                return;
            }

            _bgmService.Play(primaryCue, fadeInSeconds: -1f, reason: "qa_crossfade_to_primary");
            LogInfo("CrossfadeToPrimary", $"cue='{primaryCue.name}'");
        }

        [ContextMenu("QA/Audio/BGM/Crossfade To Alternate")]
        private void CrossfadeToAlternate()
        {
            if (!TryEnsureService() || !TryValidateCue(alternateCue, "CrossfadeToAlternate"))
            {
                return;
            }

            _bgmService.Play(alternateCue, fadeInSeconds: -1f, reason: "qa_crossfade_to_alternate");
            LogInfo("CrossfadeToAlternate", $"cue='{alternateCue.name}'");
        }

        [ContextMenu("QA/Audio/BGM/Stop")]
        private void StopBgm()
        {
            if (!TryEnsureService())
            {
                return;
            }

            _bgmService.Stop(fadeOutSeconds: -1f, reason: "qa_stop");
            LogInfo("Stop", "requested");
        }

        [ContextMenu("QA/Audio/BGM/Stop Immediate")]
        private void StopImmediateBgm()
        {
            if (!TryEnsureService())
            {
                return;
            }

            _bgmService.StopImmediate(reason: "qa_stop_immediate");
            LogInfo("StopImmediate", "requested");
        }

        [ContextMenu("QA/Audio/BGM/Pause Ducking On")]
        private void PauseDuckingOn()
        {
            if (!TryEnsureService())
            {
                return;
            }

            _bgmService.SetPauseDucking(true, "qa_ducking_on");
            LogInfo("PauseDuckingOn", "requested");
        }

        [ContextMenu("QA/Audio/BGM/Pause Ducking Off")]
        private void PauseDuckingOff()
        {
            if (!TryEnsureService())
            {
                return;
            }

            _bgmService.SetPauseDucking(false, "qa_ducking_off");
            LogInfo("PauseDuckingOff", "requested");
        }

        [ContextMenu("QA/Audio/BGM/Run Basic Scenario")]
        private void RunBasicScenario()
        {
            if (_scenarioRoutine != null)
            {
                StopCoroutine(_scenarioRoutine);
                _scenarioRoutine = null;
                LogInfo("RunBasicScenario", "previous scenario interrupted");
            }

            _scenarioRoutine = StartCoroutine(RunBasicScenarioRoutine());
        }

        [ContextMenu("QA/Audio/BGM/Log Harness State")]
        private void LogHarnessState()
        {
            bool serviceResolved = TryEnsureService();
            string activeCue = serviceResolved && _bgmService.ActiveCue != null ? _bgmService.ActiveCue.name : "null";
            string primary = primaryCue != null ? primaryCue.name : "null";
            string alternate = alternateCue != null ? alternateCue.name : "null";
            bool scenarioRunning = _scenarioRoutine != null;

            DebugUtility.Log(typeof(AudioBgmQaSceneHarness),
                $"[QA][Audio][BGM] action='LogHarnessState' serviceResolved={serviceResolved} activeCue='{activeCue}' primaryCue='{primary}' alternateCue='{alternate}' scenarioRunning={scenarioRunning}.",
                DebugUtility.Colors.Info);
        }

        private IEnumerator RunBasicScenarioRoutine()
        {
            if (!TryEnsureService())
            {
                LogError("RunBasicScenario", "aborted: IAudioBgmService not available");
                _scenarioRoutine = null;
                yield break;
            }

            if (!TryValidateCue(primaryCue, "RunBasicScenario") || !TryValidateCue(alternateCue, "RunBasicScenario"))
            {
                LogError("RunBasicScenario", "aborted: missing primary or alternate cue");
                _scenarioRoutine = null;
                yield break;
            }

            float stepDelay = Mathf.Max(0.1f, scenarioStepDelaySeconds);

            LogInfo("RunBasicScenario", "start");
            ValidateSetup();

            _bgmService.Play(primaryCue, reason: "qa_scenario_play_primary");
            LogInfo("RunBasicScenario", $"step='play_primary' cue='{primaryCue.name}'");
            yield return new WaitForSeconds(stepDelay);

            _bgmService.Play(alternateCue, fadeInSeconds: -1f, reason: "qa_scenario_crossfade_to_alternate");
            LogInfo("RunBasicScenario", $"step='crossfade_to_alternate' cue='{alternateCue.name}'");
            yield return new WaitForSeconds(stepDelay);

            _bgmService.SetPauseDucking(true, "qa_scenario_ducking_on");
            LogInfo("RunBasicScenario", "step='ducking_on'");
            yield return new WaitForSeconds(stepDelay);

            _bgmService.SetPauseDucking(false, "qa_scenario_ducking_off");
            LogInfo("RunBasicScenario", "step='ducking_off'");
            yield return new WaitForSeconds(stepDelay);

            _bgmService.Stop(fadeOutSeconds: -1f, reason: "qa_scenario_stop");
            LogInfo("RunBasicScenario", "step='stop'");

            LogInfo("RunBasicScenario", "complete");
            _scenarioRoutine = null;
        }

        private bool TryEnsureService()
        {
            if (_bgmService != null)
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

            if (!DependencyManager.Provider.TryGetGlobal(out _bgmService) || _bgmService == null)
            {
                return false;
            }

            LogInfo("ResolveService", "IAudioBgmService resolved from global DI");
            return true;
        }

        private bool TryValidateCue(AudioBgmCueAsset cue, string action)
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

            DebugUtility.Log(typeof(AudioBgmQaSceneHarness),
                $"[QA][Audio][BGM] action='{action}' detail='{detail}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogError(string action, string detail)
        {
            DebugUtility.LogError(typeof(AudioBgmQaSceneHarness),
                $"[QA][Audio][BGM] action='{action}' detail='{detail}'.");
        }
    }
}
