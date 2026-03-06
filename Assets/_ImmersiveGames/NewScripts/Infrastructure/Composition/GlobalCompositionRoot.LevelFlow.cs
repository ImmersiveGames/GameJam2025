using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gates;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void RegisterLevelsServices()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var navigationService) || navigationService == null)
            {
                throw new InvalidOperationException("IGameNavigationService obrigatório ausente no DI global. Garanta RegisterGameNavigationService no pipeline antes de RegisterLevelsServices.");
            }

            var bootstrapConfig = GetRequiredBootstrapConfig(out _);
            var sceneRouteCatalogAsset = bootstrapConfig.SceneRouteCatalog;
            if (sceneRouteCatalogAsset == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] Missing required SceneRouteCatalogAsset in NewScriptsBootstrapConfigAsset.sceneRouteCatalog.");
                throw new InvalidOperationException(
                    "Missing required NewScriptsBootstrapConfigAsset.sceneRouteCatalog (SceneRouteCatalogAsset).");
            }
            var catalogAsset = bootstrapConfig.NavigationCatalog;
            if (catalogAsset == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] Missing required GameNavigationCatalogAsset in NewScriptsBootstrapConfigAsset.navigationCatalog.");
                throw new InvalidOperationException(
                    "Missing required NewScriptsBootstrapConfigAsset.navigationCatalog (GameNavigationCatalogAsset).");
            }

            var styleCatalogAsset = bootstrapConfig.TransitionStyleCatalog;
            if (styleCatalogAsset == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] Missing required TransitionStyleCatalogAsset in NewScriptsBootstrapConfigAsset.transitionStyleCatalog.");
                throw new InvalidOperationException(
                    "Missing required NewScriptsBootstrapConfigAsset.transitionStyleCatalog (TransitionStyleCatalogAsset).");
            }

            var restartContextService = ResolveOrRegisterRestartContextService();
            IWorldResetCommands worldResetCommands = null;
            DependencyManager.Provider.TryGetGlobal(out worldResetCommands);
            if (worldResetCommands == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] Missing required IWorldResetCommands in global DI before LevelFlow services registration.");
                throw new InvalidOperationException(
                    "Missing required IWorldResetCommands in global DI.");
            }

            ISimulationGateService simulationGateService = null;
            DependencyManager.Provider.TryGetGlobal(out simulationGateService);

            ILevelSwapLocalService levelSwapLocalService = null;
            if (!DependencyManager.Provider.TryGetGlobal<ILevelSwapLocalService>(out levelSwapLocalService) || levelSwapLocalService == null)
            {
                levelSwapLocalService = new LevelSwapLocalService(
                    restartContextService,
                    worldResetCommands,
                    catalogAsset,
                    simulationGateService,
                    sceneRouteCatalogAsset);

                DependencyManager.Provider.RegisterGlobal<ILevelSwapLocalService>(levelSwapLocalService);

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[OBS][LevelFlow] ILevelSwapLocalService registrado (LevelSwapLocalService).",
                    DebugUtility.Colors.Info);
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILevelFlowRuntimeService>(out var existing) || existing == null)
            {
                var runtimeService = new LevelFlowRuntimeService(
                    navigationService,
                    restartContextService,
                    levelSwapLocalService);
                DependencyManager.Provider.RegisterGlobal<ILevelFlowRuntimeService>(runtimeService);

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[OBS][LevelFlow] LevelFlowRuntimeService registrado (trilho canônico StartGameplayDefaultAsync(reason,...)).",
                    DebugUtility.Colors.Info);
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPostLevelActionsService>(out var postLevelActions) || postLevelActions == null)
            {
                if (!DependencyManager.Provider.TryGetGlobal<ILevelFlowRuntimeService>(out var levelFlowRuntime) || levelFlowRuntime == null)
                {
                    throw new InvalidOperationException("ILevelFlowRuntimeService obrigatório ausente para registrar IPostLevelActionsService.");
                }

                postLevelActions = new PostLevelActionsService(
                    levelFlowRuntime,
                    levelSwapLocalService,
                    restartContextService,
                    sceneRouteCatalogAsset,
                    navigationService);

                DependencyManager.Provider.RegisterGlobal<IPostLevelActionsService>(postLevelActions);

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[OBS][LevelFlow] IPostLevelActionsService registrado (PostLevelActionsService).",
                    DebugUtility.Colors.Info);
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILevelMacroPrepareService>(out var existingPrepareService) || existingPrepareService == null)
            {
                var prepareService = new LevelMacroPrepareService(
                    restartContextService,
                    worldResetCommands,
                    catalogAsset,
                    sceneRouteCatalogAsset);

                DependencyManager.Provider.RegisterGlobal<ILevelMacroPrepareService>(prepareService);

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[OBS][LevelFlow] ILevelMacroPrepareService registrado (LevelMacroPrepareService).",
                    DebugUtility.Colors.Info);
            }
        }

        private static void RegisterLevelStageOrchestrator()
        {
            RegisterIfMissing(
                () => new LevelStageOrchestrator(),
                "[LevelFlow] LevelStageOrchestrator ja registrado no DI global.",
                "[LevelFlow] LevelStageOrchestrator registrado (SceneFlowCompleted + LevelSwapLocalApplied).");
        }
    }
}
