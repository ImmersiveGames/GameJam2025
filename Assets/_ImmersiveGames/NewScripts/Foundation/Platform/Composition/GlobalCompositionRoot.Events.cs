using ImmersiveGames.GameJam2025.Core.Events;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Orchestration.GameLoop.RunLifecycle.Core;
using ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Loading.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Transition.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Contracts;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Runtime;
namespace ImmersiveGames.GameJam2025.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // --------------------------------------------------------------------
        // Event systems
        // --------------------------------------------------------------------

        private static void PrimeEventSystems()
        {
            EventBus<BootStartPlanRequestedEvent>.Clear();
            EventBus<GamePlayRequestedEvent>.Clear();
            EventBus<GamePauseCommandEvent>.Clear();
            EventBus<GameResumeRequestedEvent>.Clear();
            EventBus<PauseWillEnterEvent>.Clear();
            EventBus<PauseWillExitEvent>.Clear();
            EventBus<PauseStateChangedEvent>.Clear();
            EventBus<GameResetRequestedEvent>.Clear();
            EventBus<GameLoopActivityChangedEvent>.Clear();
            EventBus<GameRunStartedEvent>.Clear();
            EventBus<GameRunEndedEvent>.Clear();
            EventBus<GameRunEndRequestedEvent>.Clear();
            EventBus<PhaseDefinitionSelectedEvent>.Clear();
            EventBus<PhaseResetCompletedEvent>.Clear();
            PhaseContentSceneRuntimeApplier.RecordCleared();

            // Scene Flow (NewScripts): evita bindings duplicados quando domain reload está desativado.
            EventBus<SceneTransitionStartedEvent>.Clear();
            EventBus<SceneTransitionFadeInCompletedEvent>.Clear();
            EventBus<SceneTransitionScenesReadyEvent>.Clear();
            EventBus<SceneTransitionBeforeFadeOutEvent>.Clear();
            EventBus<SceneTransitionCompletedEvent>.Clear();

            // WorldReset/ResetInterop (NewScripts): completion gate depende deste evento.
            EventBus<WorldResetCompletedEvent>.Clear();

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[EventBus] EventBus inicializado (GameLoop + GameplaySessionFlow + SceneFlow + WorldReset).",
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

