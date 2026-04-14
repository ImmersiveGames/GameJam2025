using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;

namespace _ImmersiveGames.NewScripts.Orchestration.SessionTransition.Runtime
{
    public enum SessionTransitionPhaseAction
    {
        None = 0,
        NextPhase = 1,
        StayOnCurrentPhase = 2,
    }

    public enum SessionTransitionResetAction
    {
        None = 0,
        PhaseReset = 1,
    }

    public enum SessionTransitionHandoffAction
    {
        None = 0,
        GoToMenu = 1,
    }

    public readonly struct SessionTransitionPlan
    {
        public SessionTransitionPlan(
            SessionTransitionContext context,
            SessionTransitionPhaseAction phaseAction,
            SessionTransitionResetAction resetAction,
            SessionTransitionHandoffAction handoffAction)
        {
            Context = context;
            PhaseAction = phaseAction;
            ResetAction = resetAction;
            HandoffAction = handoffAction;
        }

        public SessionTransitionContext Context { get; }
        public RunContinuationSelection ResolvedSelection => Context.ResolvedSelection;
        public RunContinuationContext ContinuationContext => Context.ContinuationContext;
        public RunContinuationKind ResolvedContinuation => Context.ResolvedContinuation;
        public SessionTransitionPhaseAction PhaseAction { get; }
        public SessionTransitionResetAction ResetAction { get; }
        public SessionTransitionHandoffAction HandoffAction { get; }
        public string Reason => Context.Reason;
        public string NextState => Context.NextState;
        public bool IsValid => Context.IsValid && ResolvedContinuation != RunContinuationKind.Unknown;

        public override string ToString()
        {
            return $"Continuation='{ResolvedContinuation}', PhaseAction='{PhaseAction}', ResetAction='{ResetAction}', HandoffAction='{HandoffAction}', Reason='{Reason}', NextState='{NextState}'";
        }
    }
}
