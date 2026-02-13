using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Bridges;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // --------------------------------------------------------------------
        // Coordinator (production start)
        // --------------------------------------------------------------------

        private static void RegisterGameLoopSceneFlowCoordinatorIfAvailable()
        {
            if (AbortBootstrapIfFatalLatched("RegisterGameLoopSceneFlowCoordinatorIfAvailable.begin"))
            {
                return;
            }

            if (_sceneFlowCoordinator != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoopSceneFlow] Coordinator já está registrado (static reference).",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoopSceneFlow] ISceneTransitionService indisponível. Coordinator não será registrado.",
                    DebugUtility.Colors.Info);
                return;
            }

            var bootstrapConfig = GetRequiredBootstrapConfig();
            var essentialScenes = bootstrapConfig.EssentialScenes;

            var menuSceneName = essentialScenes.MenuScene.SceneName;
            var uiGlobalSceneName = essentialScenes.UiGlobalScene.SceneName;
            var bootEntrySceneName = essentialScenes.BootEntryScene.SceneName;

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[OBS][Config] EssentialScenes startPlan menuPath='{essentialScenes.MenuScene.ScenePath}', uiGlobalPath='{essentialScenes.UiGlobalScene.ScenePath}', bootEntryPath='{essentialScenes.BootEntryScene.ScenePath}'.",
                DebugUtility.Colors.Info);

            // Plano de produção:
            // bootEntry -> (Fade) -> Load(menu + uiGlobal) -> Active=menu -> Unload(bootEntry) -> (FadeOut) -> Completed
            var startPlan = new SceneTransitionRequest(
                scenesToLoad: new[] { menuSceneName, uiGlobalSceneName },
                scenesToUnload: new[] { bootEntrySceneName },
                targetActiveScene: menuSceneName,
                useFade: true,
                transitionProfileId: StartProfileId);

            _sceneFlowCoordinator = new GameLoopSceneFlowCoordinator(sceneFlow, startPlan);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[GameLoopSceneFlow] Coordinator registrado (startPlan production, profile='{StartProfileId}').",
                DebugUtility.Colors.Info);
        }

    }
}
