using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneRouting;
using _ImmersiveGames.NewScripts.RunPipeline.Contracts;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // --------------------------------------------------------------------
        // Event systems
        // --------------------------------------------------------------------

        private static void PrimeEventSystems()
        {
            EventBus<BootStartPlanRequestedEvent>.Clear();
            EventBus<RunActivationRequestedEvent>.Clear();
            EventBus<RunPauseCommandEvent>.Clear();
            EventBus<GameResumeRequestedEvent>.Clear();
            EventBus<PauseWillEnterEvent>.Clear();
            EventBus<PauseWillExitEvent>.Clear();
            EventBus<RunPauseStateChangedEvent>.Clear();
            EventBus<GameResetRequestedEvent>.Clear();
            EventBus<RunLifecycleActivityChangedEvent>.Clear();
            EventBus<GameRunStartedEvent>.Clear();
            EventBus<RunDeactivationCompletedEvent>.Clear();
            EventBus<RunDeactivationRequestedEvent>.Clear();

            // Scene composition (NewScripts): evita bindings duplicados quando domain reload está desativado.
            EventBus<SceneTransitionStartedEvent>.Clear();
            EventBus<SceneTransitionFadeInCompletedEvent>.Clear();
            EventBus<SceneTransitionScenesReadyEvent>.Clear();
            EventBus<SceneTransitionBeforeFadeOutEvent>.Clear();
            EventBus<SceneTransitionCompletedEvent>.Clear();

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[EventBus] EventBus inicializado (SessionActivityPipeline + SceneComposition).",
                DebugUtility.Colors.Info);
        }

    }
}

