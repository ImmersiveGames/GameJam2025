using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.ResetInterop.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Loading.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Contracts;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // --------------------------------------------------------------------
        // Event systems
        // --------------------------------------------------------------------

        private static void PrimeEventSystems()
        {
            EventBus<GameStartRequestedEvent>.Clear();
            EventBus<GamePauseCommandEvent>.Clear();
            EventBus<GameResumeRequestedEvent>.Clear();
            EventBus<GameExitToMenuRequestedEvent>.Clear();
            EventBus<GameResetRequestedEvent>.Clear();
            EventBus<GameLoopActivityChangedEvent>.Clear();
            EventBus<GameRunStartedEvent>.Clear();
            EventBus<GameRunEndedEvent>.Clear();
            EventBus<GameRunEndRequestedEvent>.Clear();
            EventBus<LevelSelectedEvent>.Clear();

            // Scene Flow (NewScripts): evita bindings duplicados quando domain reload está desativado.
            EventBus<SceneTransitionStartedEvent>.Clear();
            EventBus<SceneTransitionFadeInCompletedEvent>.Clear();
            EventBus<SceneTransitionScenesReadyEvent>.Clear();
            EventBus<SceneTransitionBeforeFadeOutEvent>.Clear();
            EventBus<SceneTransitionCompletedEvent>.Clear();

            // WorldReset/ResetInterop (NewScripts): completion gate depende deste evento.
            EventBus<WorldResetCompletedEvent>.Clear();

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[EventBus] EventBus inicializado (GameLoop + SceneFlow + LevelFlow + WorldReset).",
                DebugUtility.Colors.Info);

            EnsureLoadingOrchestratorsRegisteredAfterEventBusReset();
        }

        private static void EnsureLoadingOrchestratorsRegisteredAfterEventBusReset()
        {
            if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<LoadingHudOrchestrator>(out var hudOrchestrator) && hudOrchestrator != null)
            {
                hudOrchestrator.EnsureRegistered();
            }

            if (DependencyManager.Provider.TryGetGlobal<LoadingProgressOrchestrator>(out var progressOrchestrator) && progressOrchestrator != null)
            {
                progressOrchestrator.EnsureRegistered();
            }
        }

    }
}
