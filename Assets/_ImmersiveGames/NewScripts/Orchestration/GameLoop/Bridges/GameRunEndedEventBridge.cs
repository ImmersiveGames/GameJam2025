using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;
using _ImmersiveGames.NewScripts.Experience.PostRun.Ownership;
using _ImmersiveGames.NewScripts.Experience.PostRun.Result;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Readiness.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using CanonicalRunEndIntent = _ImmersiveGames.NewScripts.Experience.PostRun.Contracts.RunEndIntent;

namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.Bridges
{
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunEndedEventBridge : MonoBehaviour
    {
        private EventBinding<GameRunEndedEvent> _binding;
        private EventBinding<GameRunStartedEvent> _runStartedBinding;
        private bool _registered;
        private bool _postStagePending;

        private void Awake()
        {
            _binding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);
            RegisterBinding();
        }

        private void OnEnable() => RegisterBinding();

        private void OnDisable() => UnregisterBinding();

        private void OnDestroy() => UnregisterBinding();

        private void RegisterBinding()
        {
            if (_registered)
            {
                return;
            }

            EventBus<GameRunEndedEvent>.Register(_binding);
            EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
            _registered = true;
        }

        private void UnregisterBinding()
        {
            if (!_registered)
            {
                return;
            }

            EventBus<GameRunEndedEvent>.Unregister(_binding);
            EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
            _registered = false;
        }

        private void OnGameRunEnded(GameRunEndedEvent evt)
        {
            if (_postStagePending)
            {
                DebugUtility.LogVerbose<GameRunEndedEventBridge>(
                    "[OBS][GameplaySessionFlow][Operational] ExitStageRunEndIgnored reason='already_pending'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _postStagePending = true;
            HandleGameRunEnded(evt);
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            _postStagePending = false;
        }

        private void HandleGameRunEnded(GameRunEndedEvent evt)
        {
            try
            {
                if (!TryMapToRunResult(evt?.Outcome ?? GameRunOutcome.Unknown, out var runResult))
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        $"[FATAL][GameplaySessionFlow] GameRunEndedEvent recebido com outcome nao terminal='{evt?.Outcome}' reason='{GameLoopReasonFormatter.Format(evt?.Reason)}'.");
                    return;
                }

                string reason = GameLoopReasonFormatter.Format(evt?.Reason);
                string sceneName = SceneManager.GetActiveScene().name;
                bool isGameplayScene = IsGameplayScene();

                if (!DependencyManager.Provider.TryGetGlobal<IGameplayPhaseRuntimeService>(out var phaseRuntimeService) ||
                    phaseRuntimeService == null)
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        "[FATAL][GameplaySessionFlow] GameRunEndedEvent recebido mas IGameplayPhaseRuntimeService nao foi encontrado no escopo global.");
                    return;
                }

                if (!phaseRuntimeService.TryGetCurrent(out var phaseRuntime) || !phaseRuntime.IsValid)
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        "[FATAL][GameplaySessionFlow] GameRunEndedEvent recebido mas o runtime da phase e invalido.");
                    return;
                }

                if (!phaseRuntime.HasRunResultStage)
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        $"[FATAL][GameplaySessionFlow] GameRunEndedEvent recebido em phase sem RunResultStage. scene='{sceneName}' frame={Time.frameCount} phaseSignature='{phaseRuntime.PhaseRuntimeSignature}'.");
                    return;
                }

                if (!DependencyManager.Provider.TryGetGlobal<IPostRunResultService>(out var resultService) || resultService == null)
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        "[FATAL][GameplaySessionFlow] GameRunEndedEvent recebido mas IPostRunResultService nao foi encontrado no escopo global.");
                    return;
                }

                if (!DependencyManager.Provider.TryGetGlobal<IRunEndIntentOwnershipService>(out var runEndIntentOwner) || runEndIntentOwner == null)
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        "[FATAL][GameplaySessionFlow] GameRunEndedEvent recebido mas IRunEndIntentOwnershipService nao foi encontrado no escopo global.");
                    return;
                }

                if (!DependencyManager.Provider.TryGetGlobal<IRunResultStageOwnershipService>(out var runResultStageOwner) || runResultStageOwner == null)
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        "[FATAL][GameplaySessionFlow] GameRunEndedEvent recebido mas IRunResultStageOwnershipService nao foi encontrado no escopo global.");
                    return;
                }

                var intent = new CanonicalRunEndIntent(
                    signature: phaseRuntime.PhaseRuntimeSignature,
                    sceneName: sceneName,
                    profile: string.Empty,
                    frame: Time.frameCount,
                    reason: reason,
                    isGameplayScene: isGameplayScene);

                resultService.TrySetRunOutcome(evt.Outcome, reason);
                runEndIntentOwner.AcceptRunEndIntent(intent);

                DebugUtility.Log<GameRunEndedEventBridge>(
                    $"[OBS][GameplaySessionFlow][RunResultStage] RunResultStageDispatchRequested outcome='{evt?.Outcome}' result='{runResult}' reason='{reason}' scene='{sceneName}' frame={Time.frameCount} isGameplayScene='{isGameplayScene}' phaseSignature='{phaseRuntime.PhaseRuntimeSignature}'.",
                    DebugUtility.Colors.Info);

                runResultStageOwner.EnterRunResultStage(new RunResultStage(intent, runResult));
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    $"[FATAL][GameplaySessionFlow] Falha inesperada ao executar RunResultStage. ex='{ex.GetType().Name}: {ex.Message}'.");
                _postStagePending = false;
            }
        }

        private static bool IsGameplayScene()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var classifier) && classifier != null)
            {
                return classifier.IsGameplayScene();
            }

            return false;
        }

        private static bool TryMapToRunResult(GameRunOutcome outcome, out RunResult result)
        {
            result = outcome switch
            {
                GameRunOutcome.Victory => RunResult.Victory,
                GameRunOutcome.Defeat => RunResult.Defeat,
                _ => RunResult.Unknown,
            };

            return result != RunResult.Unknown;
        }
    }
}
