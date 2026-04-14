using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;
using _ImmersiveGames.NewScripts.Experience.PostRun.Ownership;
using _ImmersiveGames.NewScripts.Experience.PostRun.Result;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Readiness.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SessionTransition.Runtime;
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
        private EventBinding<RunContinuationSelectionResolvedEvent> _runContinuationSelectionResolvedBinding;
        private bool _registered;
        private bool _postStagePending;

        private void Awake()
        {
            _binding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);
            _runContinuationSelectionResolvedBinding = new EventBinding<RunContinuationSelectionResolvedEvent>(OnRunContinuationSelectionResolved);
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
            EventBus<RunContinuationSelectionResolvedEvent>.Register(_runContinuationSelectionResolvedBinding);
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
            EventBus<RunContinuationSelectionResolvedEvent>.Unregister(_runContinuationSelectionResolvedBinding);
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

            if (DependencyManager.Provider.TryGetGlobal<IRunContinuationOwnershipService>(out var continuationOwner) &&
                continuationOwner != null)
            {
                continuationOwner.ClearCurrentContext("GameRunStarted");
            }
        }

        private void OnRunContinuationSelectionResolved(RunContinuationSelectionResolvedEvent evt)
        {
            if (!evt.Selection.IsValid)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    "[FATAL][GameplaySessionFlow] RunContinuationSelection recebida com estado invalido.");
                return;
            }

            DebugUtility.Log<GameRunEndedEventBridge>(
                $"[OBS][GameplaySessionFlow][RunDecision] RunContinuationSelectionResolved continuation='{evt.Selection.SelectedContinuation}' reason='{evt.Selection.Reason}' nextState='{evt.Selection.NextState}'.",
                DebugUtility.Colors.Info);

            if (!DependencyManager.Provider.TryGetGlobal<SessionTransitionPlanResolver>(out var planResolver) || planResolver == null)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    "[FATAL][GameplaySessionFlow] RunContinuationSelection recebida mas SessionTransitionPlanResolver nao foi encontrado no escopo global.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<SessionTransitionOrchestrator>(out var orchestrator) || orchestrator == null)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    "[FATAL][GameplaySessionFlow] RunContinuationSelection recebida mas SessionTransitionOrchestrator nao foi encontrado no escopo global.");
                return;
            }

            _ = ExecuteTransitionAsync(planResolver, orchestrator, evt.Selection);
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

                if (!DependencyManager.Provider.TryGetGlobal<IRunContinuationOwnershipService>(out var continuationOwner) || continuationOwner == null)
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        "[FATAL][GameplaySessionFlow] GameRunEndedEvent recebido mas IRunContinuationOwnershipService nao foi encontrado no escopo global.");
                    return;
                }

                continuationOwner.AcceptTerminalFact(new RunContinuationTerminalFact(
                    intent,
                    runResult,
                    phaseRuntime.HasRunResultStage));

                DebugUtility.Log<GameRunEndedEventBridge>(
                    $"[OBS][GameplaySessionFlow][RunResultStage] RunResultStageDispatchRequested outcome='{evt?.Outcome}' result='{runResult}' reason='{reason}' scene='{sceneName}' frame={Time.frameCount} isGameplayScene='{isGameplayScene}' phaseSignature='{phaseRuntime.PhaseRuntimeSignature}'.",
                    DebugUtility.Colors.Info);

                runResultStageOwner.EnterRunResultStage(continuationOwner.CurrentContext);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    $"[FATAL][GameplaySessionFlow] Falha inesperada ao executar RunResultStage. ex='{ex.GetType().Name}: {ex.Message}'.");
                _postStagePending = false;
            }
        }

        private static async Task ExecuteTransitionAsync(
            SessionTransitionPlanResolver planResolver,
            SessionTransitionOrchestrator orchestrator,
            RunContinuationSelection selection)
        {
            try
            {
                SessionTransitionContext context = new SessionTransitionContext(selection);
                SessionTransitionPlan plan = planResolver.Resolve(context);
                await orchestrator.ExecuteAsync(plan);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    $"[FATAL][GameplaySessionFlow] Falha inesperada ao executar SessionTransition. ex='{ex.GetType().Name}: {ex.Message}'.");
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
