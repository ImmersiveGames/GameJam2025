using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Installers.PhaseDefinition
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplaySessionFlowPrepareOperationalHandoffService : IGameplaySessionFlowPrepareOperationalHandoffService
    {
        public async System.Threading.Tasks.Task ExecuteAsync(SceneTransitionContext context)
        {
            ValidateOperationalHandoffContextOrFail(context);

            if (!context.IsGameplayInitialEntry)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowPrepareOperationalHandoffService),
                    $"[FATAL][H1][GameplaySessionFlow] GameplaySessionFlow prepare handoff is InitialEntry-only. Reentry/PostRun/PhaseNavigation must use SessionTransitionContext. routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{SceneTransitionSignature.Compute(context)}' reason='{context.Reason}'.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<SessionTransitionOrchestrator>(out var orchestrator) || orchestrator == null)
            {
                FailFastOperational(context, "SessionTransitionOrchestrator missing.");
                return;
            }

            string reason = string.IsNullOrWhiteSpace(context.Reason)
                ? "SceneFlow/GameplaySessionPrepare"
                : context.Reason.Trim();
            string signature = SceneTransitionSignature.Compute(context);
            SessionTransitionContext sessionTransitionContext = SessionTransitionContext.CreateInitialEntry(context);

            DebugUtility.Log<GameplaySessionFlowPrepareOperationalHandoffService>(
                $"[OBS][GameplaySessionFlow][Operational] handoff_received source='SceneTransitionContext' target='SessionTransitionContext' origin='{sessionTransitionContext.Origin}' intent='{sessionTransitionContext.IntentKind}' routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<GameplaySessionFlowPrepareOperationalHandoffService>(
                $"[OBS][GameplaySessionFlow][Operational] handoff_delegated target='SessionTransitionOrchestrator' source='SceneTransitionContext' convertedTo='SessionTransitionContext' routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            await orchestrator.ExecuteAsync(sessionTransitionContext);

            DebugUtility.Log<GameplaySessionFlowPrepareOperationalHandoffService>(
                $"[OBS][GameplaySessionFlow][Operational] handoff_accepted target='SessionTransitionOrchestrator' source='SessionTransitionContext' origin='{sessionTransitionContext.Origin}' intent='{sessionTransitionContext.IntentKind}' routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Success);
        }

        private static void ValidateOperationalHandoffContextOrFail(SceneTransitionContext context)
        {
            if (context == null)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowPrepareOperationalHandoffService),
                    "[FATAL][H1][GameplaySessionFlow] GameplaySessionFlow prepare operational handoff recebeu SceneTransitionContext nulo.");
                return;
            }

            if (!context.RouteId.IsValid)
            {
                FailFastOperational(context, "RouteId obrigatorio ausente.");
                return;
            }

            if (context.RouteRef == null)
            {
                FailFastOperational(context, "RouteRef obrigatorio ausente.");
                return;
            }

            if (context.RouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                FailFastOperational(context, $"RouteKind invalido para handoff gameplay. routeKind='{context.RouteRef.RouteKind}'.");
                return;
            }
        }

        private static void FailFastOperational(SceneTransitionContext context, string detail)
        {
            string routeId = context != null ? context.RouteId.ToString() : "<null>";
            string gameplayEntryKind = context != null ? context.GameplayEntryKind.ToString() : "<null>";
            string signature = context != null ? SceneTransitionSignature.Compute(context) : "<null>";
            string reason = context != null ? context.Reason : "<null>";

            HardFailFastH1.Trigger(typeof(GameplaySessionFlowPrepareOperationalHandoffService),
                $"[FATAL][H1][SceneFlow] GameplaySessionFlow prepare operational handoff misconfigured: {detail} routeId='{routeId}' gameplayEntryKind='{gameplayEntryKind}' signature='{signature}' reason='{reason}'.");
        }
    }
}
