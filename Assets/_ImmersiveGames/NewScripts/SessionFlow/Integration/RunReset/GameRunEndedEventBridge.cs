using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Readiness.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Ownership;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Result;
using UnityEngine;
using UnityEngine.SceneManagement;
using CanonicalRunEndIntent = _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts.RunEndIntent;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset
{
    public interface IRunContinuationOperationalHandoffService
    {
        Task DispatchAsync(RunContinuationSelection selection);
    }

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

            if (evt.Selection.SelectedContinuation is RunContinuationKind.ResetRun or RunContinuationKind.Retry)
            {
                RouteRunResetSelection(evt.Selection);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IRunContinuationOperationalHandoffService>(out var handoffService) || handoffService == null)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    "[FATAL][GameplaySessionFlow] RunContinuationSelection recebida mas IRunContinuationOperationalHandoffService nao foi encontrado no escopo global.");
                return;
            }

            DebugUtility.Log<GameRunEndedEventBridge>(
                $"[OBS][GameplaySessionFlow][Seam] translated continuation='{evt.Selection.SelectedContinuation}' reason='{evt.Selection.Reason}' nextState='{evt.Selection.NextState}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<GameRunEndedEventBridge>(
                $"[OBS][GameplaySessionFlow][Seam] handoff_dispatch target='RunContinuationOperational' continuation='{evt.Selection.SelectedContinuation}' reason='{evt.Selection.Reason}' nextState='{evt.Selection.NextState}'.",
                DebugUtility.Colors.Info);

            _ = DispatchRunContinuationHandoffAsync(handoffService, evt.Selection);
        }

        private static void RouteRunResetSelection(RunContinuationSelection selection)
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameplaySessionRunResetService>(out var runResetService) || runResetService == null)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    "[FATAL][GameplaySessionFlow] RunContinuationSelection de reset recebida mas IGameplaySessionRunResetService nao foi encontrado no escopo global.");
                return;
            }

            PhaseDefinitionAsset targetPhaseRef = ResolveRunResetTargetPhaseOrFail(selection);
            if (targetPhaseRef == null)
            {
                return;
            }

            GameplayRunResetRequest request = new GameplayRunResetRequest(selection, targetPhaseRef, selection.Reason);

            DebugUtility.Log<GameRunEndedEventBridge>(
                $"[OBS][GameplaySessionFlow][RunReset] RunResetRoutedFromRunDecision kind='{request.Kind}' reason='{request.Reason}' targetPhase='{DescribePhase(targetPhaseRef)}'.",
                DebugUtility.Colors.Info);

            _ = runResetService.AcceptAsync(request);
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

                if (!DependencyManager.Provider.TryGetGlobal<ISessionIntegrationContextService>(out var sessionIntegrationService) ||
                    sessionIntegrationService == null)
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        "[FATAL][GameplaySessionFlow] GameRunEndedEvent recebido mas ISessionIntegrationContextService nao foi encontrado no escopo global.");
                    return;
                }

                if (!sessionIntegrationService.TryGetCurrent(out var sessionIntegration) || !sessionIntegration.HasCoreContext)
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        "[FATAL][GameplaySessionFlow] GameRunEndedEvent recebido mas o contexto de Session Integration e invalido.");
                    return;
                }

                GameplayPhaseRuntimeSnapshot phaseRuntime = sessionIntegration.PhaseRuntime;

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
                    runResult));

                string phaseEntrySignature = sessionIntegration.SessionContext.HasSessionSignature
                    ? sessionIntegration.SessionContext.SessionSignature
                    : phaseRuntime.PhaseRuntimeSignature;
                PhaseCompleted phaseCompleted = new PhaseCompleted(
                    phaseRuntime,
                    intent,
                    evt.Outcome,
                    nameof(GameRunEndedEventBridge),
                    phaseRuntime.HasPhaseDefinitionRef ? sessionIntegration.SessionContext.SelectionVersion : 0,
                    phaseEntrySignature);

                if (!phaseCompleted.IsValid)
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        "[FATAL][GameplaySessionFlow] PhaseCompleted invalido montado a partir de GameRunEndedEvent.");
                    return;
                }

                DebugUtility.Log<GameRunEndedEventBridge>(
                    $"[OBS][GameplaySessionFlow][PhaseCompleted] PhaseCompletedCanonical source='{phaseCompleted.Source}' phaseSignature='{phaseCompleted.PhaseSignature}' outcome='{phaseCompleted.RunOutcome}' entrySequence='{phaseCompleted.PhaseLocalEntrySequence}' entrySignature='{phaseCompleted.EntrySignature}' reason='{phaseCompleted.RunEndIntent.Reason}'.",
                    DebugUtility.Colors.Info);

                EventBus<PhaseCompleted>.Raise(phaseCompleted);

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

        private static async Task DispatchRunContinuationHandoffAsync(
            IRunContinuationOperationalHandoffService handoffService,
            RunContinuationSelection selection)
        {
            try
            {
                await handoffService.DispatchAsync(selection);

                DebugUtility.Log<GameRunEndedEventBridge>(
                    $"[OBS][GameplaySessionFlow][Seam] handoff_accepted target='RunContinuationOperational' continuation='{selection.SelectedContinuation}' reason='{selection.Reason}' nextState='{selection.NextState}'.",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    $"[FATAL][GameplaySessionFlow] Falha inesperada no handoff operacional de RunContinuation. ex='{ex.GetType().Name}: {ex.Message}'.");
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

        private static PhaseDefinitionAsset ResolveRunResetTargetPhaseOrFail(RunContinuationSelection selection)
        {
            if (selection.SelectedContinuation == RunContinuationKind.ResetRun)
            {
                if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionCatalog>(out var phaseDefinitionCatalog) || phaseDefinitionCatalog == null)
                {
                    HardFailFastH1.Trigger(typeof(GameRunEndedEventBridge),
                        "[FATAL][H1][GameplaySessionFlow][RunReset] RunContinuationSelection de reset recebida mas IPhaseDefinitionCatalog nao foi encontrado no escopo global.");
                }

                return phaseDefinitionCatalog.ResolveInitialOrFail();
            }

            if (selection.SelectedContinuation == RunContinuationKind.Retry)
            {
                if (!DependencyManager.Provider.TryGetGlobal<IPhaseCatalogRuntimeStateService>(out var phaseCatalogRuntimeStateService) || phaseCatalogRuntimeStateService == null)
                {
                    HardFailFastH1.Trigger(typeof(GameRunEndedEventBridge),
                        "[FATAL][H1][GameplaySessionFlow][RunReset] RunContinuationSelection de retry recebida mas IPhaseCatalogRuntimeStateService nao foi encontrado no escopo global.");
                }

                PhaseDefinitionAsset currentCommitted = phaseCatalogRuntimeStateService.CurrentCommitted;
                if (currentCommitted == null || !currentCommitted.PhaseId.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameRunEndedEventBridge),
                        "[FATAL][H1][GameplaySessionFlow][RunReset] RunContinuationSelection de retry recebida mas o committed current phase e invalido.");
                }

                return currentCommitted;
            }

            HardFailFastH1.Trigger(typeof(GameRunEndedEventBridge),
                $"[FATAL][H1][GameplaySessionFlow][RunReset] RunContinuationSelection invalida para reset. selectedContinuation='{selection.SelectedContinuation}'.");
            return null;
        }

        private static string DescribePhase(PhaseDefinitionAsset phaseDefinition)
        {
            return phaseDefinition != null && phaseDefinition.PhaseId.IsValid
                ? phaseDefinition.PhaseId.Value
                : "<none>";
        }
    }
}

