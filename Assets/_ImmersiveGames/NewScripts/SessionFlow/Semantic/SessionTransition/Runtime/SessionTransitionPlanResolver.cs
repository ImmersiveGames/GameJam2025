using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;
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

            SessionTransitionPlan plan = context.IntentKind switch
            {
                SessionTransitionIntentKind.InitialEntry => BuildInitialEntryPlan(ValidateInitialEntryContract(context)),
                SessionTransitionIntentKind.AdvancePhase => BuildPhaseNavigationPlan(ValidatePostRunContinuationContract(context)),
                SessionTransitionIntentKind.PhaseOrdinalNavigation => BuildPhaseOrdinalNavigationPlan(ValidatePhaseOrdinalNavigationContract(context)),
                SessionTransitionIntentKind.RestartCurrentPhase => BuildPhaseResetPlan(ValidatePostRunContinuationContract(context)),
                SessionTransitionIntentKind.RestartFromFirstPhase => BuildRestartFromFirstPhasePlan(ValidatePostRunContinuationContract(context)),
                SessionTransitionIntentKind.ExitToMenu => BuildExitToMenuPlan(ValidatePostRunContinuationContract(context)),
                SessionTransitionIntentKind.TerminateRun => BuildTerminateRunPlan(ValidatePostRunContinuationContract(context)),
                _ => throw new InvalidOperationException(
                $"[FATAL][Config][SessionTransition] Intent nao suportada no plano minimo. origin='{context.Origin}' intent='{context.IntentKind}' runContinuation='{context.ResolvedContinuation}' reason='{Normalize(context.Reason)}'."),
            };

            DebugUtility.Log<SessionTransitionPlanResolver>(
                $"[OBS][GameplaySessionFlow][SessionTransition] PlanResolved origin='{plan.Context.Origin}' intent='{plan.IntentKind}' runContinuation='{context.ResolvedContinuation}' composition='{plan.Composition}' expectedPhaseLocalEntryReady='{plan.RequiresPhaseLocalEntryReady}' execution='{plan.Execution}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' reason='{Normalize(plan.Reason)}' nextState='{Normalize(plan.NextState)}'.",
                DebugUtility.Colors.Info);

            return plan;
        }

        private static SessionTransitionContext ValidateInitialEntryContract(SessionTransitionContext context)
        {
            if (context.Origin != SessionTransitionOrigin.InitialEntry || context.IntentKind != SessionTransitionIntentKind.InitialEntry)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                    $"[FATAL][H1][SessionTransition] InitialEntry requer origin/intent locais explicitos. origin='{context.Origin}' intent='{context.IntentKind}' reason='{Normalize(context.Reason)}'.");
            }

            if (context.ResolvedSelection.IsValid || context.HasRunContinuationSelection || context.ResolvedContinuation != RunContinuationKind.Unknown)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                $"[FATAL][H1][SessionTransition] InitialEntry nao pode carregar RunContinuationSelection nem RunContinuationKind. runContinuation='{context.ResolvedContinuation}' reason='{Normalize(context.Reason)}'.");
            }

            if (string.IsNullOrWhiteSpace(context.ContextSignature) || string.IsNullOrWhiteSpace(context.SceneName))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                    $"[FATAL][H1][SessionTransition] InitialEntry sem payload local valido. signature='{Normalize(context.ContextSignature)}' scene='{Normalize(context.SceneName)}' reason='{Normalize(context.Reason)}'.");
            }

            return context;
        }

        private static SessionTransitionContext ValidatePhaseOrdinalNavigationContract(SessionTransitionContext context)
        {
            if (context.Origin != SessionTransitionOrigin.PhaseNavigation ||
                context.IntentKind != SessionTransitionIntentKind.PhaseOrdinalNavigation)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                    $"[FATAL][H1][SessionTransition] PhaseOrdinalNavigation requer origin/intent locais explicitos. origin='{context.Origin}' intent='{context.IntentKind}' reason='{Normalize(context.Reason)}'.");
            }

            if (context.ResolvedSelection.IsValid || context.HasRunContinuationSelection || context.ResolvedContinuation != RunContinuationKind.Unknown)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                $"[FATAL][H1][SessionTransition] PhaseOrdinalNavigation nao pode carregar RunContinuationSelection nem RunContinuationKind. runContinuation='{context.ResolvedContinuation}' reason='{Normalize(context.Reason)}'.");
            }

            if (string.IsNullOrWhiteSpace(context.ContextSignature) ||
                string.IsNullOrWhiteSpace(context.SceneName) ||
                string.IsNullOrWhiteSpace(context.OrdinalNavigationTargetPhaseId) ||
                context.OrdinalNavigationKind == PhaseOrdinalNavigationKind.Unknown)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                    $"[FATAL][H1][SessionTransition] PhaseOrdinalNavigation sem payload ordinal valido. signature='{Normalize(context.ContextSignature)}' scene='{Normalize(context.SceneName)}' kind='{context.OrdinalNavigationKind}' target='{Normalize(context.OrdinalNavigationTargetPhaseId)}' reason='{Normalize(context.Reason)}'.");
            }

            return context;
        }

        private static SessionTransitionContext ValidatePostRunContinuationContract(SessionTransitionContext context)
        {
            if (context.Origin != SessionTransitionOrigin.PostRunContinuation)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                    $"[FATAL][H1][SessionTransition] RunContinuationSelection so pode ser usado por PostRunContinuation. origin='{context.Origin}' intent='{context.IntentKind}' reason='{Normalize(context.Reason)}'.");
            }

            if (!context.HasRunContinuationSelection || context.ResolvedContinuation == RunContinuationKind.Unknown)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                    $"[FATAL][H1][SessionTransition] PostRunContinuation sem RunContinuationSelection valida. origin='{context.Origin}' intent='{context.IntentKind}' reason='{Normalize(context.Reason)}'.");
            }

            if (context.IntentKind != SessionTransitionContext.MapContinuationKind(context.ResolvedContinuation))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                    $"[FATAL][H1][SessionTransition] Intent local nao corresponde ao RunContinuationKind selecionado. intent='{context.IntentKind}' continuation='{context.ResolvedContinuation}' reason='{Normalize(context.Reason)}'.");
            }

            return context;
        }

        private static SessionTransitionPlan BuildInitialEntryPlan(SessionTransitionContext context)
        {
            return BuildPlan(
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
                    intentKind: SessionTransitionIntentKind.InitialEntry,
                    phaseTransition: SessionTransitionPhaseAction.NextPhase,
                    worldReset: SessionTransitionResetAction.None,
                    reconstruction: false,
                    contentSpawn: true,
                    carryOver: true),
                SessionTransitionExecutionKind.InitialEntry,
                SessionTransitionHandoffAction.None,
                SessionTransitionAxisId.Continuity,
                SessionTransitionAxisId.PhaseTransition,
                SessionTransitionAxisId.ContentSpawn,
                SessionTransitionAxisId.CarryOver);
        }

        private static SessionTransitionPlan BuildPhaseNavigationPlan(SessionTransitionContext context)
        {
            return BuildPlan(
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
                    intentKind: SessionTransitionIntentKind.AdvancePhase,
                    phaseTransition: SessionTransitionPhaseAction.NextPhase,
                    worldReset: SessionTransitionResetAction.None,
                    reconstruction: false,
                    contentSpawn: true,
                    carryOver: true),
                SessionTransitionExecutionKind.NextPhase,
                SessionTransitionHandoffAction.None,
                SessionTransitionAxisId.Continuity,
                SessionTransitionAxisId.PhaseTransition,
                SessionTransitionAxisId.ContentSpawn,
                SessionTransitionAxisId.CarryOver);
        }

        private static SessionTransitionPlan BuildPhaseOrdinalNavigationPlan(SessionTransitionContext context)
        {
            return BuildPlan(
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
                    intentKind: SessionTransitionIntentKind.PhaseOrdinalNavigation,
                    phaseTransition: SessionTransitionPhaseAction.OrdinalNavigation,
                    worldReset: SessionTransitionResetAction.None,
                    reconstruction: false,
                    contentSpawn: true,
                    carryOver: true),
                SessionTransitionExecutionKind.PhaseOrdinalNavigation,
                SessionTransitionHandoffAction.None,
                SessionTransitionAxisId.Continuity,
                SessionTransitionAxisId.PhaseTransition,
                SessionTransitionAxisId.ContentSpawn,
                SessionTransitionAxisId.CarryOver);
        }

        private static SessionTransitionPlan BuildPhaseResetPlan(SessionTransitionContext context)
        {
            return BuildPlan(
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
                    intentKind: SessionTransitionIntentKind.RestartCurrentPhase,
                    phaseTransition: SessionTransitionPhaseAction.StayOnCurrentPhase,
                    worldReset: SessionTransitionResetAction.PhaseReset,
                    reconstruction: false,
                    contentSpawn: true,
                    carryOver: true),
                SessionTransitionExecutionKind.ResetCurrentPhase,
                SessionTransitionHandoffAction.None,
                SessionTransitionAxisId.Continuity,
                SessionTransitionAxisId.PhaseTransition,
                SessionTransitionAxisId.WorldReset,
                SessionTransitionAxisId.ContentSpawn,
                SessionTransitionAxisId.CarryOver);
        }

        private static SessionTransitionPlan BuildRestartFromFirstPhasePlan(SessionTransitionContext context)
        {
            return BuildPlan(
                context,
                BuildContinuityShape(
                    preservation: SessionTransitionPreservationMask.SessionState,
                    resetScope: SessionTransitionResetScopeKind.Phase,
                    carryOver: SessionTransitionCarryOverKind.None),
                BuildReconstructionShape(
                    SessionTransitionReconstructionKind.RebuildAndReentry,
                    SessionTransitionResetScopeKind.Phase),
                new SessionTransitionAxisMap(
                    intentKind: SessionTransitionIntentKind.RestartFromFirstPhase,
                    phaseTransition: SessionTransitionPhaseAction.RestartFromFirstPhase,
                    worldReset: SessionTransitionResetAction.PhaseReset,
                    reconstruction: true,
                    contentSpawn: true,
                    carryOver: false),
                SessionTransitionExecutionKind.RestartFromFirstPhase,
                SessionTransitionHandoffAction.None,
                SessionTransitionAxisId.Continuity,
                SessionTransitionAxisId.PhaseTransition,
                SessionTransitionAxisId.WorldReset,
                SessionTransitionAxisId.Reconstruction,
                SessionTransitionAxisId.ContentSpawn);
        }

        private static SessionTransitionPlan BuildExitToMenuPlan(SessionTransitionContext context)
        {
            return BuildPlan(
                context,
                BuildContinuityShape(
                    preservation: SessionTransitionPreservationMask.SessionState,
                    resetScope: SessionTransitionResetScopeKind.None,
                    carryOver: SessionTransitionCarryOverKind.None),
                BuildReconstructionShape(
                    SessionTransitionReconstructionKind.None,
                    SessionTransitionResetScopeKind.None),
                new SessionTransitionAxisMap(
                    intentKind: SessionTransitionIntentKind.ExitToMenu,
                    phaseTransition: SessionTransitionPhaseAction.None,
                    worldReset: SessionTransitionResetAction.None,
                    reconstruction: false,
                    contentSpawn: false,
                    carryOver: false),
                SessionTransitionExecutionKind.ExitToMenu,
                SessionTransitionHandoffAction.GoToMenu,
                SessionTransitionAxisId.Continuity);
        }

        private static SessionTransitionPlan BuildTerminateRunPlan(SessionTransitionContext context)
        {
            return BuildPlan(
                context,
                BuildContinuityShape(
                    preservation: SessionTransitionPreservationMask.SessionState,
                    resetScope: SessionTransitionResetScopeKind.None,
                    carryOver: SessionTransitionCarryOverKind.None),
                BuildReconstructionShape(
                    SessionTransitionReconstructionKind.None,
                    SessionTransitionResetScopeKind.None),
                new SessionTransitionAxisMap(
                    intentKind: SessionTransitionIntentKind.TerminateRun,
                    phaseTransition: SessionTransitionPhaseAction.None,
                    worldReset: SessionTransitionResetAction.None,
                    reconstruction: false,
                    contentSpawn: false,
                    carryOver: false),
                SessionTransitionExecutionKind.NoOp,
                SessionTransitionHandoffAction.None,
                SessionTransitionAxisId.Continuity);
        }

        private static SessionTransitionPlan BuildPlan(
            SessionTransitionContext context,
            SessionTransitionContinuityShape continuityShape,
            SessionTransitionReconstructionShape reconstructionShape,
            SessionTransitionAxisMap axisMap,
            SessionTransitionExecutionKind executionKind,
            SessionTransitionHandoffAction handoffAction,
            params SessionTransitionAxisId[] orderedAxes)
        {
            ValidatePlanContract(context, axisMap, executionKind);

            var composition = new SessionTransitionComposition(axisMap, continuityShape, reconstructionShape, orderedAxes);
            var execution = new SessionTransitionExecution(executionKind, handoffAction);
            return new SessionTransitionPlan(context, composition, execution);
        }

        private static void ValidatePlanContract(
            SessionTransitionContext context,
            SessionTransitionAxisMap axisMap,
            SessionTransitionExecutionKind executionKind)
        {
            if (axisMap.IntentKind != context.IntentKind)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                    $"[FATAL][H1][SessionTransition] AxisMap intent incompativel com contexto. contextIntent='{context.IntentKind}' axisIntent='{axisMap.IntentKind}' origin='{context.Origin}' reason='{Normalize(context.Reason)}'.");
            }

            if (context.Origin == SessionTransitionOrigin.InitialEntry)
            {
                if (executionKind != SessionTransitionExecutionKind.InitialEntry ||
                    !SessionTransitionExecution.RequiresPhaseLocalEntryReadyFor(executionKind))
                {
                    HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                        $"[FATAL][H1][SessionTransition] InitialEntry deve gerar execution InitialEntry com contrato derivado de PhaseLocalEntryReady. execution='{executionKind}' expectedPhaseLocalEntryReady='{SessionTransitionExecution.RequiresPhaseLocalEntryReadyFor(executionKind)}' reason='{Normalize(context.Reason)}'.");
                }

                if (context.ResolvedContinuation != RunContinuationKind.Unknown)
                {
                    HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                        $"[FATAL][H1][SessionTransition] InitialEntry nao pode usar RunContinuationKind como contrato ou compat implicita. runContinuation='{context.ResolvedContinuation}' reason='{Normalize(context.Reason)}'.");
                }

                return;
            }

            if (context.Origin == SessionTransitionOrigin.PhaseNavigation)
            {
                if (executionKind != SessionTransitionExecutionKind.PhaseOrdinalNavigation ||
                    !SessionTransitionExecution.RequiresPhaseLocalEntryReadyFor(executionKind))
                {
                    HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                        $"[FATAL][H1][SessionTransition] PhaseOrdinalNavigation deve gerar execution PhaseOrdinalNavigation com contrato derivado de PhaseLocalEntryReady. execution='{executionKind}' expectedPhaseLocalEntryReady='{SessionTransitionExecution.RequiresPhaseLocalEntryReadyFor(executionKind)}' reason='{Normalize(context.Reason)}'.");
                }

                if (context.ResolvedContinuation != RunContinuationKind.Unknown)
                {
                    HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                        $"[FATAL][H1][SessionTransition] PhaseOrdinalNavigation nao pode usar RunContinuationKind como contrato ou compat implicita. runContinuation='{context.ResolvedContinuation}' reason='{Normalize(context.Reason)}'.");
                }

                return;
            }

            if (!context.HasRunContinuationSelection || context.ResolvedContinuation == RunContinuationKind.Unknown)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                    $"[FATAL][H1][SessionTransition] PostRunContinuation deve carregar RunContinuationSelection valida e runContinuation explicito. origin='{context.Origin}' intent='{context.IntentKind}' runContinuation='{context.ResolvedContinuation}' reason='{Normalize(context.Reason)}'.");
            }
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
