using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunOutcome;
using _ImmersiveGames.NewScripts.Orchestration.SceneComposition;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Readiness.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Bootstrap
{
    /// <summary>
    /// Installer operacional do LevelLifecycle.
    /// O nome historico do arquivo permanece apenas por compatibilidade.
    ///
    /// Responsabilidade:
    /// - registrar contratos, servicos e wiring pre-runtime de suporte ao GameplaySessionFlow;
    /// - nao depender de IGameNavigationService;
    /// - nao compor runtime operacional do stack.
    /// </summary>
    public static class LevelLifecycleInstaller
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
                throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] BootstrapConfigAsset obrigatorio ausente para instalar LevelLifecycle.");
            }

            RegisterLevelFlowPrerequisites(bootstrapConfig);

            _installed = true;

            DebugUtility.Log(typeof(LevelLifecycleInstaller),
                "[OBS][LevelLifecycle][Operational] Module installer concluido como suporte pre-runtime ao GameplaySessionFlow.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterLevelFlowPrerequisites(BootstrapConfigAsset bootstrapConfig)
        {
            var contentService = ResolveOrRegisterLevelFlowContentService();
            var restartContextService = ResolveOrRegisterRestartContextService();
            DependencyManager.Provider.TryGetGlobal(out IWorldResetCommands worldResetCommands);
            if (worldResetCommands == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] Missing required IWorldResetCommands in global DI before LevelLifecycle installation.");
            }

            DependencyManager.Provider.TryGetGlobal(out ISceneCompositionExecutor sceneCompositionExecutor);
            if (sceneCompositionExecutor == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] Missing required ISceneCompositionExecutor in global DI before LevelLifecycle installation.");
            }

            DependencyManager.Provider.TryGetGlobal(out ISimulationGateService simulationGateService);

            RegisterLevelIntroStageSessionService();
            RegisterLevelIntroStagePresenterScopeResolver();
            RegisterLevelIntroStagePresenterRegistry();
            RegisterGameplaySessionFlowServices();
            RegisterLevelSwapLocalService(
                restartContextService,
                contentService,
                worldResetCommands,
                sceneCompositionExecutor,
                simulationGateService);
            RegisterLevelMacroPrepareService(
                restartContextService,
                sceneCompositionExecutor);
        }

        private static ILevelFlowContentService ResolveOrRegisterLevelFlowContentService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelFlowContentService>(out var existing) && existing != null)
            {
                return existing;
            }

            var service = new LevelFlowContentService();
            DependencyManager.Provider.RegisterGlobal<ILevelFlowContentService>(service);

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][LevelLifecycle][Operational] ILevelFlowContentService registrado no DI global (LevelFlowContentService).",
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

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][LevelLifecycle][Operational] RestartContextService registrado no DI global.",
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

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][LevelLifecycle][Operational] ILevelIntroStageSessionService registrado no DI global (LevelIntroStageSessionService).",
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

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][LevelLifecycle][Operational] ILevelIntroStagePresenterRegistry registrado no DI global (LevelIntroStagePresenterHost).",
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

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][LevelLifecycle][Operational] ILevelIntroStagePresenterScopeResolver registrado no DI global (LevelIntroStagePresenterScopeResolver).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameplaySessionFlowServices()
        {
            RegisterGameplaySessionContextService();
            RegisterGameplayPhaseRuntimeService();
            RegisterGameplayPhasePlayerParticipationService();
            RegisterGameplayPhaseRulesObjectivesService();
            RegisterGameplayPhaseInitialStateService();
            RegisterGameplaySessionFlowPhaseConsumptionService();
            RegisterGameRunOutcomeService();
            RegisterIntroStageCoordinator();
            RegisterIntroStageControlService();
            RegisterGameplaySceneClassifier();
            RegisterDefaultIntroStageStep();

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][GameplaySessionFlow] Session rail services registrados no DI global (Intro/Outcome/RulesObjectives/InitialState) como base operacional.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameplaySessionContextService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySessionContextService>(out var existing) && existing != null)
            {
                return;
            }

            var service = new GameplaySessionContextService();
            DependencyManager.Provider.RegisterGlobal<IGameplaySessionContextService>(service);

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][GameplaySessionFlow] IGameplaySessionContextService registrado no DI global (GameplaySessionContextService).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameplayPhaseRuntimeService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplayPhaseRuntimeService>(out var existing) && existing != null)
            {
                return;
            }

            var service = new GameplayPhaseRuntimeService();
            DependencyManager.Provider.RegisterGlobal<IGameplayPhaseRuntimeService>(service);

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][GameplaySessionFlow] IGameplayPhaseRuntimeService registrado no DI global (GameplayPhaseRuntimeService).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameplayPhasePlayerParticipationService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplayPhasePlayerParticipationService>(out var existing) && existing != null)
            {
                return;
            }

            var service = new GameplayPhasePlayerParticipationService();
            DependencyManager.Provider.RegisterGlobal<IGameplayPhasePlayerParticipationService>(service);

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][GameplaySessionFlow] IGameplayPhasePlayerParticipationService registrado no DI global (GameplayPhasePlayerParticipationService).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameplayPhaseRulesObjectivesService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplayPhaseRulesObjectivesService>(out var existing) && existing != null)
            {
                return;
            }

            var service = new GameplayPhaseRulesObjectivesService();
            DependencyManager.Provider.RegisterGlobal<IGameplayPhaseRulesObjectivesService>(service);

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][GameplaySessionFlow] IGameplayPhaseRulesObjectivesService registrado no DI global (GameplayPhaseRulesObjectivesService).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameplayPhaseInitialStateService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplayPhaseInitialStateService>(out var existing) && existing != null)
            {
                return;
            }

            var service = new GameplayPhaseInitialStateService();
            DependencyManager.Provider.RegisterGlobal<IGameplayPhaseInitialStateService>(service);

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][GameplaySessionFlow] IGameplayPhaseInitialStateService registrado no DI global (GameplayPhaseInitialStateService).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterLevelSwapLocalService(
            IRestartContextService restartContextService,
            ILevelFlowContentService contentService,
            IWorldResetCommands worldResetCommands,
            ISceneCompositionExecutor sceneCompositionExecutor,
            ISimulationGateService simulationGateService)
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelSwapLocalService>(out var existing) && existing != null)
            {
                return;
            }

            var service = new LevelSwapLocalService(
                restartContextService,
                worldResetCommands,
                sceneCompositionExecutor,
                contentService,
                simulationGateService);

            DependencyManager.Provider.RegisterGlobal(service);

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][LevelLifecycle][Operational] ILevelSwapLocalService registrado (LevelSwapLocalService).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterLevelMacroPrepareService(
            IRestartContextService restartContextService,
            ISceneCompositionExecutor sceneCompositionExecutor)
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelMacroPrepareService>(out var existing) && existing != null)
            {
                return;
            }

            var service = new LevelMacroPrepareService(
                restartContextService,
                sceneCompositionExecutor,
                ResolveRequiredPhaseDefinitionSelectionService());

            DependencyManager.Provider.RegisterGlobal<ILevelMacroPrepareService>(service);

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][LevelLifecycle][Operational] ILevelMacroPrepareService registrado (LevelMacroPrepareService).",
                DebugUtility.Colors.Info);
        }

        private static IPhaseDefinitionSelectionService ResolveRequiredPhaseDefinitionSelectionService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionSelectionService>(out var phaseDefinitionSelectionService) && phaseDefinitionSelectionService != null)
            {
                return phaseDefinitionSelectionService;
            }

            throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] IPhaseDefinitionSelectionService ausente no DI global antes da composicao do LevelMacroPrepareService.");
        }

        private static void RegisterGameplaySessionFlowPhaseConsumptionService()
        {
            if (DependencyManager.Provider.TryGetGlobal<GameplaySessionFlowPhaseConsumptionService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplaySessionContextService>(out var sessionContextService) || sessionContextService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] IGameplaySessionContextService obrigatorio ausente ao registrar GameplaySessionFlowPhaseConsumptionService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayPhaseRuntimeService>(out var phaseRuntimeService) || phaseRuntimeService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] IGameplayPhaseRuntimeService obrigatorio ausente ao registrar GameplaySessionFlowPhaseConsumptionService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayPhasePlayerParticipationService>(out var phasePlayersService) || phasePlayersService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] IGameplayPhasePlayerParticipationService obrigatorio ausente ao registrar GameplaySessionFlowPhaseConsumptionService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayPhaseRulesObjectivesService>(out var phaseRulesObjectivesService) || phaseRulesObjectivesService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] IGameplayPhaseRulesObjectivesService obrigatorio ausente ao registrar GameplaySessionFlowPhaseConsumptionService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayPhaseInitialStateService>(out var phaseInitialStateService) || phaseInitialStateService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] IGameplayPhaseInitialStateService obrigatorio ausente ao registrar GameplaySessionFlowPhaseConsumptionService.");
            }

            var service = new GameplaySessionFlowPhaseConsumptionService(
                sessionContextService,
                phaseRuntimeService,
                phasePlayersService,
                phaseRulesObjectivesService,
                phaseInitialStateService);
            DependencyManager.Provider.RegisterGlobal(service);

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][GameplaySessionFlow] GameplaySessionFlowPhaseConsumptionService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunOutcomeService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunPlayingStateGuard>(out var gameplayStateGuard) || gameplayStateGuard == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] IGameRunPlayingStateGuard obrigatorio ausente ao registrar IGameRunOutcomeService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] IGameLoopService obrigatorio ausente ao registrar IGameRunOutcomeService.");
            }

            var service = new GameRunOutcomeService(gameplayStateGuard, gameLoopService);
            DependencyManager.Provider.RegisterGlobal<IGameRunOutcomeService>(service);

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][GameplaySessionFlow] IGameRunOutcomeService registrado no DI global (GameRunOutcomeService).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIntroStageCoordinator()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out var existing) && existing != null)
            {
                return;
            }

            var service = new IntroStageCoordinator();
            DependencyManager.Provider.RegisterGlobal<IIntroStageCoordinator>(service);

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][GameplaySessionFlow] IIntroStageCoordinator registrado no DI global (IntroStageCoordinator).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIntroStageControlService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var existing) && existing != null)
            {
                return;
            }

            var service = new IntroStageControlService();
            DependencyManager.Provider.RegisterGlobal<IIntroStageControlService>(service);

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][GameplaySessionFlow] IIntroStageControlService registrado no DI global (IntroStageControlService).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameplaySceneClassifier()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var existing) && existing != null)
            {
                return;
            }

            var service = new DefaultGameplaySceneClassifier();
            DependencyManager.Provider.RegisterGlobal<IGameplaySceneClassifier>(service);

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][GameplaySessionFlow] IGameplaySceneClassifier registrado no DI global (DefaultGameplaySceneClassifier).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterDefaultIntroStageStep()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageStep>(out var existing) && existing != null)
            {
                return;
            }

            var service = new ConfirmToStartIntroStageStep();
            DependencyManager.Provider.RegisterGlobal<IIntroStageStep>(service);

            DebugUtility.LogVerbose(typeof(LevelLifecycleInstaller),
                "[OBS][GameplaySessionFlow] IIntroStageStep registrado no DI global (ConfirmToStartIntroStageStep).",
                DebugUtility.Colors.Info);
        }

    }

}
