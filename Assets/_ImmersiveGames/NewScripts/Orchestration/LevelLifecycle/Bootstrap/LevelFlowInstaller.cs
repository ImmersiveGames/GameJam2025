using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Orchestration.SceneComposition;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Bootstrap
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

            DebugUtility.Log(typeof(LevelFlowInstaller),
                "[LevelFlow] Module installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterLevelFlowPrerequisites(BootstrapConfigAsset bootstrapConfig)
        {
            var contentService = ResolveOrRegisterLevelFlowContentService();
            var restartContextService = ResolveOrRegisterRestartContextService();
            RegisterLevelIntroStageSessionService();
            RegisterLevelIntroStagePresenterScopeResolver();
            RegisterLevelIntroStagePresenterRegistry();
            RegisterLevelStagePresentationService();
            RegisterLevelPostRunHookService();
            RegisterLevelSwapLocalService(restartContextService, contentService);
            RegisterLevelMacroPrepareService(restartContextService, contentService);
        }

        private static ILevelFlowContentService ResolveOrRegisterLevelFlowContentService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelFlowContentService>(out var existing) && existing != null)
            {
                return existing;
            }

            var service = new LevelFlowContentService();
            DependencyManager.Provider.RegisterGlobal<ILevelFlowContentService>(service);

            DebugUtility.LogVerbose(typeof(LevelFlowInstaller),
                "[OBS][LevelFlow] ILevelFlowContentService registrado no DI global (LevelFlowContentService).",
                DebugUtility.Colors.Info);

            return service;
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

        private static void RegisterLevelIntroStageSessionService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelIntroStageSessionService>(out var existing) && existing != null)
            {
                return;
            }

            var service = new LevelIntroStageSessionService();
            DependencyManager.Provider.RegisterGlobal<ILevelIntroStageSessionService>(service);

            DebugUtility.LogVerbose(typeof(LevelFlowInstaller),
                "[OBS][LevelFlow] ILevelIntroStageSessionService registrado no DI global (LevelIntroStageSessionService).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterLevelStagePresentationService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelStagePresentationService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILevelIntroStageSessionService>(out var sessionService) || sessionService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelFlow] ILevelIntroStageSessionService obrigatorio ausente ao registrar ILevelStagePresentationService.");
            }

            var service = new LevelStagePresentationService(sessionService);
            DependencyManager.Provider.RegisterGlobal(service);

            DebugUtility.LogVerbose(typeof(LevelFlowInstaller),
                "[OBS][LevelFlow] ILevelStagePresentationService registrado no DI global (LevelStagePresentationService).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterLevelIntroStagePresenterRegistry()
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelIntroStagePresenterRegistry>(out var existing) && existing != null)
            {
                return;
            }

            var service = new LevelIntroStagePresenterHost();
            DependencyManager.Provider.RegisterGlobal<ILevelIntroStagePresenterRegistry>(service);

            DebugUtility.LogVerbose(typeof(LevelFlowInstaller),
                "[OBS][LevelFlow] ILevelIntroStagePresenterRegistry registrado no DI global (LevelIntroStagePresenterHost).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterLevelIntroStagePresenterScopeResolver()
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelIntroStagePresenterScopeResolver>(out var existing) && existing != null)
            {
                return;
            }

            var service = new LevelIntroStagePresenterScopeResolver();
            DependencyManager.Provider.RegisterGlobal<ILevelIntroStagePresenterScopeResolver>(service);

            DebugUtility.LogVerbose(typeof(LevelFlowInstaller),
                "[OBS][LevelFlow] ILevelIntroStagePresenterScopeResolver registrado no DI global (LevelIntroStagePresenterScopeResolver).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterLevelPostRunHookService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelPostRunHookService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILevelStagePresentationService>(out var stagePresentationService) || stagePresentationService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelFlow] ILevelStagePresentationService obrigatorio ausente ao registrar ILevelPostRunHookService.");
            }

            var service = new LevelPostRunHookService(stagePresentationService);
            DependencyManager.Provider.RegisterGlobal(service);

            DebugUtility.LogVerbose(typeof(LevelFlowInstaller),
                "[OBS][LevelFlow] ILevelPostRunHookService registrado no DI global (LevelPostRunHookService).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterLevelSwapLocalService(IRestartContextService restartContextService, ILevelFlowContentService contentService)
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
                contentService,
                simulationGateService);

            DependencyManager.Provider.RegisterGlobal(service);

            DebugUtility.LogVerbose(typeof(LevelFlowInstaller),
                "[OBS][LevelFlow] ILevelSwapLocalService registrado (LevelSwapLocalService).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterLevelMacroPrepareService(IRestartContextService restartContextService, ILevelFlowContentService contentService)
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
                contentService);

            DependencyManager.Provider.RegisterGlobal<ILevelMacroPrepareService>(service);

            DebugUtility.LogVerbose(typeof(LevelFlowInstaller),
                "[OBS][LevelFlow] ILevelMacroPrepareService registrado (LevelMacroPrepareService).",
                DebugUtility.Colors.Info);
        }

    }
}

