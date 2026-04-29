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

            SessionTransitionPlan plan = context.IntentKind switch
            {
                SessionTransitionIntentKind.InitialEntry => BuildInitialEntryPlan(ValidateInitialEntryContract(context)),
                SessionTransitionIntentKind.AdvancePhase => BuildPhaseNavigationPlan(ValidatePostRunContinuationContract(context)),
                SessionTransitionIntentKind.RestartCurrentPhase => BuildPhaseResetPlan(ValidatePostRunContinuationContract(context)),
                SessionTransitionIntentKind.RestartFromFirstPhase => BuildRestartFromFirstPhasePlan(ValidatePostRunContinuationContract(context)),
                SessionTransitionIntentKind.ExitToMenu => BuildExitToMenuPlan(ValidatePostRunContinuationContract(context)),
                SessionTransitionIntentKind.TerminateRun => BuildTerminateRunPlan(ValidatePostRunContinuationContract(context)),
                _ => throw new InvalidOperationException(
                    $"[FATAL][Config][SessionTransition] Intent nao suportada no plano minimo. origin='{context.Origin}' intent='{context.IntentKind}' legacyContinuation='{context.ResolvedContinuation}' reason='{Normalize(context.Reason)}'."),
            };

            DebugUtility.Log<SessionTransitionPlanResolver>(
                $"[OBS][GameplaySessionFlow][SessionTransition] PlanResolved origin='{plan.Context.Origin}' intent='{plan.IntentKind}' legacyContinuation='{plan.LegacyRunContinuation}' composition='{plan.Composition}' phaseLocalEntryReady='{plan.EmitsPhaseLocalEntryReady}' execution='{plan.Execution}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' reason='{Normalize(plan.Reason)}' nextState='{Normalize(plan.NextState)}'.",
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
                    $"[FATAL][H1][SessionTransition] InitialEntry nao pode carregar RunContinuationSelection nem RunContinuationKind. legacyContinuation='{context.ResolvedContinuation}' reason='{Normalize(context.Reason)}'.");
            }

            if (string.IsNullOrWhiteSpace(context.ContextSignature) || string.IsNullOrWhiteSpace(context.SceneName))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                    $"[FATAL][H1][SessionTransition] InitialEntry sem payload local valido. signature='{Normalize(context.ContextSignature)}' scene='{Normalize(context.SceneName)}' reason='{Normalize(context.Reason)}'.");
            }

            return context;
        }

        private static SessionTransitionContext ValidatePostRunContinuationContract(SessionTransitionContext context)
        {
            if (context.Origin != SessionTransitionOrigin.PostRunContinuation && context.Origin != SessionTransitionOrigin.PhaseNavigation)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                    $"[FATAL][H1][SessionTransition] RunContinuationSelection so pode ser usado por PostRunContinuation/PhaseNavigation. origin='{context.Origin}' intent='{context.IntentKind}' reason='{Normalize(context.Reason)}'.");
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
                    legacyRunContinuation: RunContinuationKind.Unknown,
                    phaseTransition: SessionTransitionPhaseAction.NextPhase,
                    worldReset: SessionTransitionResetAction.None,
                    reconstruction: false,
                    contentSpawn: true,
                    carryOver: true),
                SessionTransitionExecutionKind.InitialEntry,
                emitsPhaseLocalEntryReady: true,
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
                    legacyRunContinuation: context.ResolvedContinuation,
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
                    legacyRunContinuation: context.ResolvedContinuation,
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
                    legacyRunContinuation: context.ResolvedContinuation,
                    phaseTransition: SessionTransitionPhaseAction.RestartFromFirstPhase,
                    worldReset: SessionTransitionResetAction.PhaseReset,
                    reconstruction: true,
                    contentSpawn: true,
                    carryOver: false),
                SessionTransitionExecutionKind.RestartFromFirstPhase,
                emitsPhaseLocalEntryReady: true,
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
                    legacyRunContinuation: context.ResolvedContinuation,
                    phaseTransition: SessionTransitionPhaseAction.None,
                    worldReset: SessionTransitionResetAction.None,
                    reconstruction: false,
                    contentSpawn: false,
                    carryOver: false),
                SessionTransitionExecutionKind.ExitToMenu,
                emitsPhaseLocalEntryReady: false,
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
                    legacyRunContinuation: context.ResolvedContinuation,
                    phaseTransition: SessionTransitionPhaseAction.None,
                    worldReset: SessionTransitionResetAction.None,
                    reconstruction: false,
                    contentSpawn: false,
                    carryOver: false),
                SessionTransitionExecutionKind.NoOp,
                emitsPhaseLocalEntryReady: false,
                SessionTransitionHandoffAction.None,
                SessionTransitionAxisId.Continuity);
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
            ValidatePlanContract(context, axisMap, executionKind, emitsPhaseLocalEntryReady);

            var composition = new SessionTransitionComposition(axisMap, continuityShape, reconstructionShape, emitsPhaseLocalEntryReady, orderedAxes);
            var execution = new SessionTransitionExecution(executionKind, handoffAction);
            return new SessionTransitionPlan(context, composition, execution);
        }

        private static void ValidatePlanContract(
            SessionTransitionContext context,
            SessionTransitionAxisMap axisMap,
            SessionTransitionExecutionKind executionKind,
            bool emitsPhaseLocalEntryReady)
        {
            if (axisMap.IntentKind != context.IntentKind)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                    $"[FATAL][H1][SessionTransition] AxisMap intent incompativel com contexto. contextIntent='{context.IntentKind}' axisIntent='{axisMap.IntentKind}' origin='{context.Origin}' reason='{Normalize(context.Reason)}'.");
            }

            if (context.Origin == SessionTransitionOrigin.InitialEntry)
            {
                if (executionKind != SessionTransitionExecutionKind.InitialEntry || !emitsPhaseLocalEntryReady)
                {
                    HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                        $"[FATAL][H1][SessionTransition] InitialEntry deve gerar execution InitialEntry e PhaseLocalEntryReady. execution='{executionKind}' emits='{emitsPhaseLocalEntryReady}' reason='{Normalize(context.Reason)}'.");
                }

                if (axisMap.LegacyRunContinuation != RunContinuationKind.Unknown)
                {
                    HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                        $"[FATAL][H1][SessionTransition] InitialEntry nao pode usar RunContinuationKind como contrato ou compat implicita. legacyContinuation='{axisMap.LegacyRunContinuation}' reason='{Normalize(context.Reason)}'.");
                }

                return;
            }

            if (!context.HasRunContinuationSelection || axisMap.LegacyRunContinuation == RunContinuationKind.Unknown)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPlanResolver),
                    $"[FATAL][H1][SessionTransition] PostRunContinuation deve carregar RunContinuationSelection valida e legacyRunContinuation explicito. origin='{context.Origin}' intent='{context.IntentKind}' legacyContinuation='{axisMap.LegacyRunContinuation}' reason='{Normalize(context.Reason)}'.");
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
