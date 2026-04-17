using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
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
                RunContinuationKind.AdvancePhase => BuildPlan(
                    context,
                    BuildContinuityShape(
                        preservation: SessionTransitionPreservationMask.SessionState |
                                      SessionTransitionPreservationMask.WorldState |
                                      SessionTransitionPreservationMask.ContentState |
                                      SessionTransitionPreservationMask.ActorState |
                                      SessionTransitionPreservationMask.ObjectState,
                        resetScope: SessionTransitionResetScopeKind.None,
                        carryOver: SessionTransitionCarryOverKind.Selective),
                    BuildReconstructionShape(
                        SessionTransitionReconstructionKind.None,
                        SessionTransitionResetScopeKind.None),
                    new SessionTransitionAxisMap(
                        continuity: RunContinuationKind.AdvancePhase,
                        phaseTransition: SessionTransitionPhaseAction.NextPhase,
                        worldReset: SessionTransitionResetAction.None,
                        reconstruction: false,
                        contentSpawn: true,
                        carryOver: true),
                    SessionTransitionExecutionKind.NextPhase,
                    emitsPhaseLocalEntryReady: true,
                    SessionTransitionHandoffAction.None,
                    SessionTransitionAxisId.Continuity,
                    SessionTransitionAxisId.PhaseTransition,
                    SessionTransitionAxisId.ContentSpawn,
                    SessionTransitionAxisId.CarryOver),
                RunContinuationKind.RestartCurrentPhase => BuildPlan(
                    context,
                    BuildContinuityShape(
                        preservation: SessionTransitionPreservationMask.SessionState |
                                      SessionTransitionPreservationMask.WorldState |
                                      SessionTransitionPreservationMask.ContentState |
                                      SessionTransitionPreservationMask.ActorState |
                                      SessionTransitionPreservationMask.ObjectState,
                        resetScope: SessionTransitionResetScopeKind.Phase,
                        carryOver: SessionTransitionCarryOverKind.Selective),
                    BuildReconstructionShape(
                        SessionTransitionReconstructionKind.ReentryAfterReset,
                        SessionTransitionResetScopeKind.Phase),
                    new SessionTransitionAxisMap(
                        continuity: RunContinuationKind.RestartCurrentPhase,
                        phaseTransition: SessionTransitionPhaseAction.StayOnCurrentPhase,
                        worldReset: SessionTransitionResetAction.PhaseReset,
                        reconstruction: false,
                        contentSpawn: true,
                        carryOver: true),
                    SessionTransitionExecutionKind.ResetCurrentPhase,
                    emitsPhaseLocalEntryReady: true,
                    SessionTransitionHandoffAction.None,
                    SessionTransitionAxisId.Continuity,
                    SessionTransitionAxisId.PhaseTransition,
                    SessionTransitionAxisId.WorldReset,
                    SessionTransitionAxisId.ContentSpawn,
                    SessionTransitionAxisId.CarryOver),
                RunContinuationKind.ExitToMenu => BuildPlan(
                    context,
                    BuildContinuityShape(
                        preservation: SessionTransitionPreservationMask.SessionState,
                        resetScope: SessionTransitionResetScopeKind.None,
                        carryOver: SessionTransitionCarryOverKind.None),
                    BuildReconstructionShape(
                        SessionTransitionReconstructionKind.None,
                        SessionTransitionResetScopeKind.None),
                    new SessionTransitionAxisMap(
                        continuity: RunContinuationKind.ExitToMenu,
                        phaseTransition: SessionTransitionPhaseAction.None,
                        worldReset: SessionTransitionResetAction.None,
                        reconstruction: false,
                        contentSpawn: false,
                        carryOver: false),
                    SessionTransitionExecutionKind.ExitToMenu,
                    emitsPhaseLocalEntryReady: false,
                    SessionTransitionHandoffAction.GoToMenu,
                    SessionTransitionAxisId.Continuity),
                RunContinuationKind.TerminateRun => BuildPlan(
                    context,
                    BuildContinuityShape(
                        preservation: SessionTransitionPreservationMask.SessionState,
                        resetScope: SessionTransitionResetScopeKind.None,
                        carryOver: SessionTransitionCarryOverKind.None),
                    BuildReconstructionShape(
                        SessionTransitionReconstructionKind.None,
                        SessionTransitionResetScopeKind.None),
                    new SessionTransitionAxisMap(
                        continuity: RunContinuationKind.TerminateRun,
                        phaseTransition: SessionTransitionPhaseAction.None,
                        worldReset: SessionTransitionResetAction.None,
                        reconstruction: false,
                        contentSpawn: false,
                        carryOver: false),
                    SessionTransitionExecutionKind.NoOp,
                    emitsPhaseLocalEntryReady: false,
                    SessionTransitionHandoffAction.None,
                    SessionTransitionAxisId.Continuity),
                _ => throw new InvalidOperationException(
                    $"[FATAL][Config][SessionTransition] Continuation nao suportada no plano minimo. continuation='{context.ResolvedContinuation}' reason='{Normalize(context.Reason)}'."),
            };

            DebugUtility.Log<SessionTransitionPlanResolver>(
                $"[OBS][GameplaySessionFlow][SessionTransition] PlanResolved continuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' phaseLocalEntryReady='{plan.EmitsPhaseLocalEntryReady}' execution='{plan.Execution}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' reason='{Normalize(plan.Reason)}' nextState='{Normalize(plan.NextState)}'.",
                DebugUtility.Colors.Info);

            return plan;
        }

        private static SessionTransitionPlan BuildPlan(
            SessionTransitionContext context,
            SessionTransitionContinuityShape continuityShape,
            SessionTransitionReconstructionShape reconstructionShape,
            SessionTransitionAxisMap axisMap,
            SessionTransitionExecutionKind executionKind,
            bool emitsPhaseLocalEntryReady,
            SessionTransitionHandoffAction handoffAction,
            params SessionTransitionAxisId[] orderedAxes)
        {
            var composition = new SessionTransitionComposition(axisMap, continuityShape, reconstructionShape, emitsPhaseLocalEntryReady, orderedAxes);
            var execution = new SessionTransitionExecution(executionKind, handoffAction);
            return new SessionTransitionPlan(context, composition, execution);
        }

        private static SessionTransitionContinuityShape BuildContinuityShape(
            SessionTransitionPreservationMask preservation,
            SessionTransitionResetScopeKind resetScope,
            SessionTransitionCarryOverKind carryOver)
        {
            return new SessionTransitionContinuityShape(preservation, resetScope, carryOver);
        }

        private static SessionTransitionReconstructionShape BuildReconstructionShape(
            SessionTransitionReconstructionKind kind,
            SessionTransitionResetScopeKind resetBoundary)
        {
            return new SessionTransitionReconstructionShape(kind, resetBoundary);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

