using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionTransitionOrchestrator
    {
        private readonly IGameplaySessionFlowContinuityService _continuityService;

        public SessionTransitionOrchestrator(IGameplaySessionFlowContinuityService continuityService)
        {
            _continuityService = continuityService ?? throw new ArgumentNullException(nameof(continuityService));
        }

        public async Task ExecuteAsync(SessionTransitionPlan plan, CancellationToken ct = default)
        {
            if (!plan.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] SessionTransitionPlan invalido recebido pelo orquestrador.");
            }

            string normalizedReason = Normalize(plan.Reason);
            bool shouldEmitPhaseLocalEntryReady = plan.EmitsPhaseLocalEntryReady;

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteStarted continuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' phaseLocalEntryReady='{shouldEmitPhaseLocalEntryReady}' reason='{normalizedReason}' nextState='{Normalize(plan.NextState)}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            if (shouldEmitPhaseLocalEntryReady)
            {
                EventBus<SessionTransitionPhaseLocalEntryReadyEvent>.Raise(
                    new SessionTransitionPhaseLocalEntryReadyEvent(plan, nameof(SessionTransitionOrchestrator)));
            }

            if (plan.Execution.Kind == SessionTransitionExecutionKind.ResetCurrentPhase)
            {
                await _continuityService.ResetCurrentPhaseAsync(normalizedReason, ct);
                DebugUtility.Log<SessionTransitionOrchestrator>(
                    $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteCompleted continuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            if (plan.Execution.Kind == SessionTransitionExecutionKind.NextPhase)
            {
                await _continuityService.NextPhaseAsync(normalizedReason, ct);
                DebugUtility.Log<SessionTransitionOrchestrator>(
                    $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteCompleted continuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            if (plan.Execution.Kind == SessionTransitionExecutionKind.ExitToMenu)
            {
                await _continuityService.ExitToMenuAsync(normalizedReason, ct);
                DebugUtility.Log<SessionTransitionOrchestrator>(
                    $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteCompleted continuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] SkipNoContent continuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' phaseLocalEntryReady='{shouldEmitPhaseLocalEntryReady}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Warning);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class RunContinuationOperationalHandoffService : IRunContinuationOperationalHandoffService
    {
        private readonly SessionTransitionPlanResolver _planResolver;
        private readonly SessionTransitionOrchestrator _orchestrator;

        public RunContinuationOperationalHandoffService(
            SessionTransitionPlanResolver planResolver,
            SessionTransitionOrchestrator orchestrator)
        {
            _planResolver = planResolver ?? throw new ArgumentNullException(nameof(planResolver));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        public async Task DispatchAsync(_ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts.RunContinuationSelection selection)
        {
            SessionTransitionContext context = new SessionTransitionContext(selection);
            SessionTransitionPlan plan = _planResolver.Resolve(context);

            DebugUtility.Log<RunContinuationOperationalHandoffService>(
                $"[OBS][GameplaySessionFlow][Operational] handoff_accepted target='SessionTransitionExecution' continuation='{plan.ResolvedContinuation}' reason='{Normalize(plan.Reason)}' nextState='{Normalize(plan.NextState)}'.",
                DebugUtility.Colors.Info);

            await _orchestrator.ExecuteAsync(plan);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

