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
            if (!context.RouteId.IsValid || context.RouteRef == null || context.RouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                return;
            }

            // Acceitar both InitialEntry e Reentry para materializar actors.
            // Comentário: os dois caminhos precisam de SessionTransitionPhaseLocalEntryReadyEvent.
            SessionTransitionOrigin origin;
            if (context.IsGameplayInitialEntry)
            {
                origin = SessionTransitionOrigin.InitialEntry;
            }
            else if (context.IsGameplayReentry)
            {
                origin = SessionTransitionOrigin.PhaseNavigation;
            }
            else
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowPrepareOperationalHandoffService),
                    $"[FATAL][H1][GameplaySessionFlow] Handoff requires typed GameplayInitialEntry or GameplayReentry. routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{SceneTransitionSignature.Compute(context)}' reason='{context.Reason}'.");
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

            DebugUtility.Log<GameplaySessionFlowPrepareOperationalHandoffService>(
                $"[OBS][GameplaySessionFlow][Operational] handoff_received rail='GameplaySessionPrepare' routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' origin='{origin}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<GameplaySessionFlowPrepareOperationalHandoffService>(
                $"[OBS][GameplaySessionFlow][Operational] handoff_delegated target='SessionTransitionOrchestrator' origin='{origin}' routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            await orchestrator.ExecuteAsync(context, origin);

            DebugUtility.Log<GameplaySessionFlowPrepareOperationalHandoffService>(
                $"[OBS][GameplaySessionFlow][Operational] handoff_accepted target='SessionTransitionOrchestrator' origin='{origin}' routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Success);
        }

        private static void FailFastOperational(SceneTransitionContext context, string detail)
        {
            HardFailFastH1.Trigger(typeof(GameplaySessionFlowPrepareOperationalHandoffService),
                $"[FATAL][H1][SceneFlow] GameplaySessionFlow prepare operational handoff misconfigured: {detail} routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{SceneTransitionSignature.Compute(context)}' reason='{context.Reason}'.");
        }
    }
}
