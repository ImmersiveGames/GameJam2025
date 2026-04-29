using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
{
    public interface ISessionTransitionExecutionPort
    {
        Task<SessionTransitionExecutionDispatchResult> DispatchAsync(SessionTransitionPlan plan, CancellationToken ct = default);
    }

    public enum SessionTransitionExecutionDispatchStatus
    {
        NotExecuted = 0,
        PhaseLocalEntryReadyConfirmed = 1,
        ExecutedWithoutPhaseLocalEntryReady = 2,
        Rejected = 3,
        Unsupported = 4,
        Unconfirmed = 5,
    }

    public readonly struct SessionTransitionExecutionDispatchResult
    {
        private readonly SessionTransitionPhaseLocalEntryReadyEvent _phaseLocalEntryReadyEvent;

        private SessionTransitionExecutionDispatchResult(
            SessionTransitionExecutionKind executionKind,
            SessionTransitionExecutionDispatchStatus status,
            bool wasExecuted,
            bool allowsPhaseLocalEntryReady,
            string failureReason,
            string detail,
            bool hasPhaseLocalEntryReadyEvent,
            SessionTransitionPhaseLocalEntryReadyEvent phaseLocalEntryReadyEvent)
        {
            ExecutionKind = executionKind;
            Status = status;
            WasExecuted = wasExecuted;
            AllowsPhaseLocalEntryReady = allowsPhaseLocalEntryReady;
            FailureReason = Normalize(failureReason);
            Detail = Normalize(detail);
            HasPhaseLocalEntryReadyEvent = hasPhaseLocalEntryReadyEvent;
            _phaseLocalEntryReadyEvent = phaseLocalEntryReadyEvent;
        }

        public SessionTransitionExecutionKind ExecutionKind { get; }
        public SessionTransitionExecutionDispatchStatus Status { get; }
        public bool WasExecuted { get; }
        public bool AllowsPhaseLocalEntryReady { get; }
        public string FailureReason { get; }
        public string Detail { get; }
        public bool HasPhaseLocalEntryReadyEvent { get; }

        public bool HasFailure => Status == SessionTransitionExecutionDispatchStatus.Rejected ||
                                  Status == SessionTransitionExecutionDispatchStatus.Unsupported ||
                                  Status == SessionTransitionExecutionDispatchStatus.Unconfirmed ||
                                  Status == SessionTransitionExecutionDispatchStatus.NotExecuted;

        public static SessionTransitionExecutionDispatchResult PhaseLocalEntryReadyConfirmed(
            SessionTransitionExecutionKind executionKind,
            string detail)
        {
            return new SessionTransitionExecutionDispatchResult(
                executionKind,
                SessionTransitionExecutionDispatchStatus.PhaseLocalEntryReadyConfirmed,
                wasExecuted: true,
                allowsPhaseLocalEntryReady: true,
                failureReason: string.Empty,
                detail: detail,
                hasPhaseLocalEntryReadyEvent: false,
                phaseLocalEntryReadyEvent: default);
        }

        public static SessionTransitionExecutionDispatchResult PhaseLocalEntryReadyConfirmed(
            SessionTransitionExecutionKind executionKind,
            string detail,
            SessionTransitionPhaseLocalEntryReadyEvent phaseLocalEntryReadyEvent)
        {
            return new SessionTransitionExecutionDispatchResult(
                executionKind,
                SessionTransitionExecutionDispatchStatus.PhaseLocalEntryReadyConfirmed,
                wasExecuted: true,
                allowsPhaseLocalEntryReady: true,
                failureReason: string.Empty,
                detail: detail,
                hasPhaseLocalEntryReadyEvent: true,
                phaseLocalEntryReadyEvent: phaseLocalEntryReadyEvent);
        }

        public static SessionTransitionExecutionDispatchResult ExecutedWithoutPhaseLocalEntryReady(
            SessionTransitionExecutionKind executionKind,
            string detail)
        {
            return new SessionTransitionExecutionDispatchResult(
                executionKind,
                SessionTransitionExecutionDispatchStatus.ExecutedWithoutPhaseLocalEntryReady,
                wasExecuted: true,
                allowsPhaseLocalEntryReady: false,
                failureReason: string.Empty,
                detail: detail,
                hasPhaseLocalEntryReadyEvent: false,
                phaseLocalEntryReadyEvent: default);
        }

        public static SessionTransitionExecutionDispatchResult Rejected(
            SessionTransitionExecutionKind executionKind,
            string failureReason,
            string detail)
        {
            return new SessionTransitionExecutionDispatchResult(
                executionKind,
                SessionTransitionExecutionDispatchStatus.Rejected,
                wasExecuted: true,
                allowsPhaseLocalEntryReady: false,
                failureReason: failureReason,
                detail: detail,
                hasPhaseLocalEntryReadyEvent: false,
                phaseLocalEntryReadyEvent: default);
        }

        public static SessionTransitionExecutionDispatchResult Unsupported(
            SessionTransitionExecutionKind executionKind,
            string detail)
        {
            return new SessionTransitionExecutionDispatchResult(
                executionKind,
                SessionTransitionExecutionDispatchStatus.Unsupported,
                wasExecuted: false,
                allowsPhaseLocalEntryReady: false,
                failureReason: "UnsupportedExecutionKind",
                detail: detail,
                hasPhaseLocalEntryReadyEvent: false,
                phaseLocalEntryReadyEvent: default);
        }

        public static SessionTransitionExecutionDispatchResult Unconfirmed(
            SessionTransitionExecutionKind executionKind,
            string detail)
        {
            return new SessionTransitionExecutionDispatchResult(
                executionKind,
                SessionTransitionExecutionDispatchStatus.Unconfirmed,
                wasExecuted: true,
                allowsPhaseLocalEntryReady: false,
                failureReason: "UnconfirmedOperationalResult",
                detail: detail,
                hasPhaseLocalEntryReadyEvent: false,
                phaseLocalEntryReadyEvent: default);
        }

        public bool TryGetPhaseLocalEntryReadyEvent(out SessionTransitionPhaseLocalEntryReadyEvent phaseLocalEntryReadyEvent)
        {
            phaseLocalEntryReadyEvent = _phaseLocalEntryReadyEvent;
            return HasPhaseLocalEntryReadyEvent && phaseLocalEntryReadyEvent.HasCanonicalPayload;
        }

        public override string ToString()
        {
            return $"ExecutionKind='{ExecutionKind}', Status='{Status}', WasExecuted='{WasExecuted}', AllowsPhaseLocalEntryReady='{AllowsPhaseLocalEntryReady}', HasPhaseLocalEntryReadyEvent='{HasPhaseLocalEntryReadyEvent}', FailureReason='{FailureReason}', Detail='{Detail}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class SessionTransitionExecutionPort : ISessionTransitionExecutionPort
    {
        private readonly IGameplaySessionFlowContinuityService _continuityService;
        private readonly ISessionTransitionAdvancePhaseExecutionService _advancePhaseExecutionService;
        private readonly ISessionTransitionPhaseOrdinalNavigationExecutionService _phaseOrdinalNavigationExecutionService;

        public SessionTransitionExecutionPort(
            IGameplaySessionFlowContinuityService continuityService,
            ISessionTransitionAdvancePhaseExecutionService advancePhaseExecutionService,
            ISessionTransitionPhaseOrdinalNavigationExecutionService phaseOrdinalNavigationExecutionService)
        {
            _continuityService = continuityService ?? throw new ArgumentNullException(nameof(continuityService));
            _advancePhaseExecutionService = advancePhaseExecutionService ?? throw new ArgumentNullException(nameof(advancePhaseExecutionService));
            _phaseOrdinalNavigationExecutionService = phaseOrdinalNavigationExecutionService ?? throw new ArgumentNullException(nameof(phaseOrdinalNavigationExecutionService));
        }

        public async Task<SessionTransitionExecutionDispatchResult> DispatchAsync(SessionTransitionPlan plan, CancellationToken ct = default)
        {
            string normalizedReason = Normalize(plan.Reason);
            SessionTransitionExecutionKind executionKind = plan.Execution.Kind;

            if (executionKind == SessionTransitionExecutionKind.ResetCurrentPhase)
            {
                PhaseResetExecutionResult resetResult = await _continuityService.ResetCurrentPhaseAsync(normalizedReason, ct);
                if (!resetResult.Succeeded || !resetResult.AllowsPhaseLocalEntryReady)
                {
                    return SessionTransitionExecutionDispatchResult.Unconfirmed(
                        executionKind,
                        $"ResetCurrentPhase did not confirm PhaseLocalEntryReady. result='{resetResult}'.");
                }

                return SessionTransitionExecutionDispatchResult.PhaseLocalEntryReadyConfirmed(
                    executionKind,
                    $"ResetCurrentPhase confirmed. result='{resetResult}'.");
            }

            if (executionKind == SessionTransitionExecutionKind.RestartFromFirstPhase)
            {
                PhaseResetExecutionResult resetResult = await _continuityService.RestartFromFirstPhaseAsync(normalizedReason, ct);
                if (!resetResult.Succeeded || !resetResult.AllowsPhaseLocalEntryReady)
                {
                    return SessionTransitionExecutionDispatchResult.Unconfirmed(
                        executionKind,
                        $"RestartFromFirstPhase did not confirm PhaseLocalEntryReady. result='{resetResult}'.");
                }

                return SessionTransitionExecutionDispatchResult.PhaseLocalEntryReadyConfirmed(
                    executionKind,
                    $"RestartFromFirstPhase confirmed. result='{resetResult}'.");
            }

            if (executionKind == SessionTransitionExecutionKind.NextPhase)
            {
                PhaseNavigationResult navigationResult = await _advancePhaseExecutionService.AdvanceAsync(plan, ct);
                if (navigationResult.Outcome != PhaseNavigationOutcome.Changed || !navigationResult.HasSelectionContext)
                {
                    return SessionTransitionExecutionDispatchResult.Rejected(
                        executionKind,
                        $"PhaseNavigationOutcome.{navigationResult.Outcome}",
                        $"NextPhase did not commit a phase change. from='{navigationResult.FromPhaseId}' to='{navigationResult.ToPhaseId}' reason='{Normalize(navigationResult.Reason)}'.");
                }

                return SessionTransitionExecutionDispatchResult.PhaseLocalEntryReadyConfirmed(
                    executionKind,
                    $"NextPhase committed by canonical advance execution. from='{navigationResult.FromPhaseId}' to='{navigationResult.ToPhaseId}' outcome='{navigationResult.Outcome}' wasWrapped='{navigationResult.WasWrapped}'.");
            }

            if (executionKind == SessionTransitionExecutionKind.PhaseOrdinalNavigation)
            {
                PhaseNavigationResult navigationResult = await _phaseOrdinalNavigationExecutionService.NavigateAsync(plan, ct);
                if (navigationResult.Outcome != PhaseNavigationOutcome.Changed || !navigationResult.HasSelectionContext)
                {
                    return SessionTransitionExecutionDispatchResult.Rejected(
                        executionKind,
                        $"PhaseNavigationOutcome.{navigationResult.Outcome}",
                        $"PhaseOrdinalNavigation did not apply a phase change. kind='{plan.Context.OrdinalNavigationKind}' target='{Normalize(plan.Context.OrdinalNavigationTargetPhaseId)}' from='{navigationResult.FromPhaseId}' to='{navigationResult.ToPhaseId}' reason='{Normalize(navigationResult.Reason)}'.");
                }

                return SessionTransitionExecutionDispatchResult.PhaseLocalEntryReadyConfirmed(
                    executionKind,
                    $"PhaseOrdinalNavigation applied by canonical execution. kind='{plan.Context.OrdinalNavigationKind}' target='{Normalize(plan.Context.OrdinalNavigationTargetPhaseId)}' from='{navigationResult.FromPhaseId}' to='{navigationResult.ToPhaseId}' outcome='{navigationResult.Outcome}'.");
            }

            if (executionKind == SessionTransitionExecutionKind.ExitToMenu)
            {
                await _continuityService.ExitToMenuAsync(normalizedReason, ct);
                return SessionTransitionExecutionDispatchResult.ExecutedWithoutPhaseLocalEntryReady(
                    executionKind,
                    $"ExitToMenu executed. reason='{normalizedReason}'.");
            }

            return SessionTransitionExecutionDispatchResult.Unsupported(
                executionKind,
                $"SessionTransitionExecutionKind unsupported by SessionTransitionExecutionPort. reason='{normalizedReason}'.");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
