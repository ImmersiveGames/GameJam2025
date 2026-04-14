using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;

namespace _ImmersiveGames.NewScripts.Orchestration.SessionTransition.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionTransitionPlanResolver
    {
        public SessionTransitionPlan Resolve(SessionTransitionContext context)
        {
            if (!context.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                    "[FATAL][H1][SessionTransition] SessionTransitionContext invalido recebido pelo resolver.");
            }

            SessionTransitionPlan plan = context.ResolvedContinuation switch
            {
                RunContinuationKind.AdvancePhase => new SessionTransitionPlan(
                    context,
                    SessionTransitionPhaseAction.NextPhase,
                    SessionTransitionResetAction.None,
                    SessionTransitionHandoffAction.None),
                RunContinuationKind.RestartCurrentPhase => new SessionTransitionPlan(
                    context,
                    SessionTransitionPhaseAction.StayOnCurrentPhase,
                    SessionTransitionResetAction.PhaseReset,
                    SessionTransitionHandoffAction.None),
                RunContinuationKind.ExitToMenu => new SessionTransitionPlan(
                    context,
                    SessionTransitionPhaseAction.None,
                    SessionTransitionResetAction.None,
                    SessionTransitionHandoffAction.GoToMenu),
                RunContinuationKind.EndRun => new SessionTransitionPlan(
                    context,
                    SessionTransitionPhaseAction.None,
                    SessionTransitionResetAction.None,
                    SessionTransitionHandoffAction.None),
                _ => throw new InvalidOperationException(
                    $"[FATAL][Config][SessionTransition] Continuation nao suportada no plano minimo. continuation='{context.ResolvedContinuation}' reason='{Normalize(context.Reason)}'."),
            };

            DebugUtility.Log<SessionTransitionPlanResolver>(
                $"[OBS][GameplaySessionFlow][SessionTransition] PlanResolved continuation='{plan.ResolvedContinuation}' phaseAction='{plan.PhaseAction}' resetAction='{plan.ResetAction}' reason='{Normalize(plan.Reason)}' nextState='{Normalize(plan.NextState)}'.",
                DebugUtility.Colors.Info);

            return plan;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
