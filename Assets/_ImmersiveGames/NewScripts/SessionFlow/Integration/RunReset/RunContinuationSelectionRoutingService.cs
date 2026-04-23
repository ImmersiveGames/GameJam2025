using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset
{
    public interface IRunResetTargetPhaseResolver
    {
        PhaseDefinitionAsset ResolveOrFail(RunContinuationSelection selection);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class RunContinuationSelectionRoutingService : IRunContinuationSelectionRoutingService
    {
        private readonly IRunContinuationOperationalHandoffService _handoffService;
        private readonly IGameplaySessionRunResetService _runResetService;
        private readonly IRunResetTargetPhaseResolver _runResetTargetPhaseResolver;

        public RunContinuationSelectionRoutingService(
            IRunContinuationOperationalHandoffService handoffService,
            IGameplaySessionRunResetService runResetService,
            IRunResetTargetPhaseResolver runResetTargetPhaseResolver)
        {
            _handoffService = handoffService ?? throw new ArgumentNullException(nameof(handoffService));
            _runResetService = runResetService ?? throw new ArgumentNullException(nameof(runResetService));
            _runResetTargetPhaseResolver = runResetTargetPhaseResolver ?? throw new ArgumentNullException(nameof(runResetTargetPhaseResolver));
        }

        public void RouteSelection(RunContinuationSelection selection)
        {
            if (selection.SelectedContinuation is RunContinuationKind.ResetRun or RunContinuationKind.Retry)
            {
                RouteRunResetSelection(selection);
                return;
            }

            DebugUtility.Log<GameRunEndedEventBridge>(
                $"[OBS][GameplaySessionFlow][Seam] translated continuation='{selection.SelectedContinuation}' reason='{selection.Reason}' nextState='{selection.NextState}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<GameRunEndedEventBridge>(
                $"[OBS][GameplaySessionFlow][Seam] handoff_dispatch target='RunContinuationOperational' continuation='{selection.SelectedContinuation}' reason='{selection.Reason}' nextState='{selection.NextState}'.",
                DebugUtility.Colors.Info);

            _ = DispatchRunContinuationHandoffAsync(_handoffService, selection);
        }

        private void RouteRunResetSelection(RunContinuationSelection selection)
        {
            PhaseDefinitionAsset targetPhaseRef = _runResetTargetPhaseResolver.ResolveOrFail(selection);
            if (targetPhaseRef == null)
            {
                return;
            }

            GameplayRunResetRequest request = new GameplayRunResetRequest(selection, targetPhaseRef, selection.Reason);

            DebugUtility.Log<GameRunEndedEventBridge>(
                $"[OBS][GameplaySessionFlow][RunReset] RunResetRoutedFromRunDecision kind='{request.Kind}' reason='{request.Reason}' targetPhase='{DescribePhase(targetPhaseRef)}'.",
                DebugUtility.Colors.Info);

            _ = _runResetService.AcceptAsync(request);
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

        private static string DescribePhase(PhaseDefinitionAsset phaseDefinition)
        {
            return phaseDefinition != null && phaseDefinition.PhaseId.IsValid
                ? phaseDefinition.PhaseId.Value
                : "<none>";
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class RunResetTargetPhaseResolver : IRunResetTargetPhaseResolver
    {
        private readonly IPhaseDefinitionCatalog _phaseDefinitionCatalog;
        private readonly IPhaseCatalogRuntimeStateService _phaseCatalogRuntimeStateService;

        public RunResetTargetPhaseResolver(
            IPhaseDefinitionCatalog phaseDefinitionCatalog,
            IPhaseCatalogRuntimeStateService phaseCatalogRuntimeStateService)
        {
            _phaseDefinitionCatalog = phaseDefinitionCatalog ?? throw new ArgumentNullException(nameof(phaseDefinitionCatalog));
            _phaseCatalogRuntimeStateService = phaseCatalogRuntimeStateService ?? throw new ArgumentNullException(nameof(phaseCatalogRuntimeStateService));
        }

        public PhaseDefinitionAsset ResolveOrFail(RunContinuationSelection selection)
        {
            if (selection.SelectedContinuation == RunContinuationKind.ResetRun)
            {
                return _phaseDefinitionCatalog.ResolveInitialOrFail();
            }

            if (selection.SelectedContinuation == RunContinuationKind.Retry)
            {
                PhaseDefinitionAsset currentCommitted = _phaseCatalogRuntimeStateService.CurrentCommitted;
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
    }
}
