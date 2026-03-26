using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Infrastructure.SceneComposition;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Bootstrap
{
    /// <summary>
    /// Installer do LevelFlow.
    ///
    /// Responsabilidade:
    /// - registrar contratos, servicos e wiring pre-runtime do modulo;
    /// - nao depender de IGameNavigationService;
    /// - nao compor runtime operacional do stack.
    /// </summary>
    public static class LevelFlowInstaller
    {
        private static bool _installed;

        public static void Install(BootstrapConfigAsset bootstrapConfig)
        {
            if (_installed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelFlow] BootstrapConfigAsset obrigatorio ausente para instalar LevelFlow.");
            }

            RegisterLevelFlowPrerequisites(bootstrapConfig);

            _installed = true;

            DebugUtility.LogVerbose(typeof(LevelFlowInstaller),
                "[LevelFlow] Module installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterLevelFlowPrerequisites(BootstrapConfigAsset bootstrapConfig)
        {
            var sceneRouteCatalogAsset = bootstrapConfig.SceneRouteCatalog;
            if (sceneRouteCatalogAsset == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelFlow] Missing required BootstrapConfigAsset.sceneRouteCatalog.");
            }

            var restartContextService = ResolveOrRegisterRestartContextService();
            RegisterLevelStagePresentationService(restartContextService);
            RegisterLevelPostGameHookService();
            RegisterLevelSwapLocalService(restartContextService, sceneRouteCatalogAsset);
            RegisterLevelMacroPrepareService(restartContextService, sceneRouteCatalogAsset);
        }

        private static IRestartContextService ResolveOrRegisterRestartContextService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var existing) && existing != null)
            {
                return existing;
            }

            var service = new RestartContextService();
            DependencyManager.Provider.RegisterGlobal<IRestartContextService>(service);

            DebugUtility.LogVerbose(typeof(LevelFlowInstaller),
                "[OBS][LevelFlow] RestartContextService registrado no DI global.",
                DebugUtility.Colors.Info);

            return service;
        }

        private static void RegisterLevelStagePresentationService(IRestartContextService restartContextService)
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelStagePresentationService>(out var existing) && existing != null)
            {
                return;
            }

            var service = new LevelStagePresentationService(restartContextService);
            DependencyManager.Provider.RegisterGlobal(service);

            DebugUtility.LogVerbose(typeof(LevelFlowInstaller),
                "[OBS][LevelFlow] ILevelStagePresentationService registrado no DI global (LevelStagePresentationService).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterLevelPostGameHookService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelPostGameHookService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILevelStagePresentationService>(out var stagePresentationService) || stagePresentationService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelFlow] ILevelStagePresentationService obrigatorio ausente ao registrar ILevelPostGameHookService.");
            }

            var service = new LevelPostGameHookService(stagePresentationService);
            DependencyManager.Provider.RegisterGlobal(service);

            DebugUtility.LogVerbose(typeof(LevelFlowInstaller),
                "[OBS][LevelFlow] ILevelPostGameHookService registrado no DI global (LevelPostGameHookService).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterLevelSwapLocalService(IRestartContextService restartContextService, SceneRouteCatalogAsset sceneRouteCatalogAsset)
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelSwapLocalService>(out var existing) && existing != null)
            {
                return;
            }

            DependencyManager.Provider.TryGetGlobal(out IWorldResetCommands worldResetCommands);
            if (worldResetCommands == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelFlow] Missing required IWorldResetCommands in global DI before LevelFlow installation.");
            }

            DependencyManager.Provider.TryGetGlobal(out ISimulationGateService simulationGateService);

            DependencyManager.Provider.TryGetGlobal(out ISceneCompositionExecutor sceneCompositionExecutor);
            if (sceneCompositionExecutor == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelFlow] Missing required ISceneCompositionExecutor in global DI before LevelFlow installation.");
            }

            var service = new LevelSwapLocalService(
                restartContextService,
                worldResetCommands,
                sceneCompositionExecutor,
                simulationGateService,
                sceneRouteCatalogAsset);

            DependencyManager.Provider.RegisterGlobal(service);

            DebugUtility.LogVerbose(typeof(LevelFlowInstaller),
                "[OBS][LevelFlow] ILevelSwapLocalService registrado (LevelSwapLocalService).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterLevelMacroPrepareService(IRestartContextService restartContextService, SceneRouteCatalogAsset sceneRouteCatalogAsset)
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelMacroPrepareService>(out var existing) && existing != null)
            {
                return;
            }

            DependencyManager.Provider.TryGetGlobal(out IWorldResetCommands worldResetCommands);
            if (worldResetCommands == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelFlow] Missing required IWorldResetCommands in global DI before LevelFlow installation.");
            }

            DependencyManager.Provider.TryGetGlobal(out ISceneCompositionExecutor sceneCompositionExecutor);
            if (sceneCompositionExecutor == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelFlow] Missing required ISceneCompositionExecutor in global DI before LevelFlow installation.");
            }

            var service = new LevelMacroPrepareService(
                restartContextService,
                worldResetCommands,
                sceneCompositionExecutor,
                sceneRouteCatalogAsset);

            DependencyManager.Provider.RegisterGlobal<ILevelMacroPrepareService>(service);

            DebugUtility.LogVerbose(typeof(LevelFlowInstaller),
                "[OBS][LevelFlow] ILevelMacroPrepareService registrado (LevelMacroPrepareService).",
                DebugUtility.Colors.Info);
        }

    }
}
