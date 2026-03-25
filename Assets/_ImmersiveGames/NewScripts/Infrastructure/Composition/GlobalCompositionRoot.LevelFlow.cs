using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.SceneComposition;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Runtime;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void InstallLevelFlowServices()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var navigationService) || navigationService == null)
            {
                throw new InvalidOperationException("IGameNavigationService obrigatorio ausente no DI global. Garanta RegisterGameNavigationService no pipeline antes de InstallLevelFlowServices.");
            }

            var bootstrapConfig = GetRequiredBootstrapConfig(out _);
            var sceneRouteCatalogAsset = bootstrapConfig.SceneRouteCatalog;
            if (sceneRouteCatalogAsset == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] Missing required SceneRouteCatalogAsset in BootstrapConfigAsset.sceneRouteCatalog.");
                throw new InvalidOperationException("Missing required BootstrapConfigAsset.sceneRouteCatalog (SceneRouteCatalogAsset).");
            }

            var catalogAsset = bootstrapConfig.NavigationCatalog;
            if (catalogAsset == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] Missing required GameNavigationCatalogAsset in BootstrapConfigAsset.navigationCatalog.");
                throw new InvalidOperationException("Missing required BootstrapConfigAsset.navigationCatalog (GameNavigationCatalogAsset).");
            }

            var restartContextService = ResolveOrRegisterRestartContextService();
            if (!DependencyManager.Provider.TryGetGlobal<ILevelStagePresentationService>(out var stagePresentationService) || stagePresentationService == null)
            {
                stagePresentationService = new LevelStagePresentationService(restartContextService);
                DependencyManager.Provider.RegisterGlobal(stagePresentationService);

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[OBS][LevelFlow] ILevelStagePresentationService registrado no DI global (LevelStagePresentationService).",
                    DebugUtility.Colors.Info);
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILevelPostGameHookService>(out var levelPostGameHookService) || levelPostGameHookService == null)
            {
                levelPostGameHookService = new LevelPostGameHookService(stagePresentationService);
                DependencyManager.Provider.RegisterGlobal(levelPostGameHookService);

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[OBS][LevelFlow] ILevelPostGameHookService registrado no DI global (LevelPostGameHookService).",
                    DebugUtility.Colors.Info);
            }

            IWorldResetCommands worldResetCommands = null;
            DependencyManager.Provider.TryGetGlobal(out worldResetCommands);
            if (worldResetCommands == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] Missing required IWorldResetCommands in global DI before LevelFlow services registration.");
                throw new InvalidOperationException("Missing required IWorldResetCommands in global DI.");
            }

            ISimulationGateService simulationGateService = null;
            DependencyManager.Provider.TryGetGlobal(out simulationGateService);

            ISceneCompositionExecutor sceneCompositionExecutor = null;
            DependencyManager.Provider.TryGetGlobal(out sceneCompositionExecutor);
            if (sceneCompositionExecutor == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] Missing required ISceneCompositionExecutor in global DI before LevelFlow services registration.");
                throw new InvalidOperationException("Missing required ISceneCompositionExecutor in global DI.");
            }

            ILevelSwapLocalService levelSwapLocalService = null;
            if (!DependencyManager.Provider.TryGetGlobal(out levelSwapLocalService) || levelSwapLocalService == null)
            {
                levelSwapLocalService = new LevelSwapLocalService(
                    restartContextService,
                    worldResetCommands,
                    sceneCompositionExecutor,
                    simulationGateService,
                    sceneRouteCatalogAsset);

                DependencyManager.Provider.RegisterGlobal(levelSwapLocalService);

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
                    "[OBS][LevelFlow] LevelFlowRuntimeService registrado (trilho canonico StartGameplayDefaultAsync(reason,...)).",
                    DebugUtility.Colors.Info);
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPostLevelActionsService>(out var postLevelActions) || postLevelActions == null)
            {
                if (!DependencyManager.Provider.TryGetGlobal<ILevelFlowRuntimeService>(out var levelFlowRuntime) || levelFlowRuntime == null)
                {
                    throw new InvalidOperationException("ILevelFlowRuntimeService obrigatorio ausente para registrar IPostLevelActionsService.");
                }

                postLevelActions = new PostLevelActionsService(
                    levelFlowRuntime,
                    levelSwapLocalService,
                    restartContextService,
                    sceneRouteCatalogAsset,
                    navigationService);

                DependencyManager.Provider.RegisterGlobal(postLevelActions);

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[OBS][LevelFlow] IPostLevelActionsService registrado (PostLevelActionsService).",
                    DebugUtility.Colors.Info);
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILevelMacroPrepareService>(out var existingPrepareService) || existingPrepareService == null)
            {
                var prepareService = new LevelMacroPrepareService(
                    restartContextService,
                    worldResetCommands,
                    sceneCompositionExecutor,
                    sceneRouteCatalogAsset);

                DependencyManager.Provider.RegisterGlobal<ILevelMacroPrepareService>(prepareService);

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[OBS][LevelFlow] ILevelMacroPrepareService registrado (LevelMacroPrepareService).",
                    DebugUtility.Colors.Info);
            }

            RegisterLevelFlowCompletionGate();
        }

        private static void RegisterLevelStageOrchestrator()
        {
            RegisterIfMissing(
                () => new LevelStageOrchestrator(),
                "[LevelFlow] LevelStageOrchestrator ja registrado no DI global.",
                "[LevelFlow] LevelStageOrchestrator registrado (SceneFlowCompleted + LevelSwapLocalApplied).");
        }

        private static void RegisterLevelFlowCompletionGate()
        {
            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionCompletionGate>(out var existingGate) || existingGate == null)
            {
                throw new InvalidOperationException(
                    "[FATAL][Config][LevelFlow] ISceneTransitionCompletionGate obrigatorio ausente para composicao de LevelPrepare/Clear.");
            }

            if (existingGate is not MacroLevelPrepareCompletionGate macroGate)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][LevelFlow] ISceneTransitionCompletionGate invalido para composicao de LevelFlow (tipo='{existingGate.GetType().Name}').");
            }

            macroGate.ConfigureLevelFlowGate(new LevelFlowMacroPrepareCompletionGate());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][LevelFlow] Gate de LevelPrepare/Clear acoplado ao completion gate macro do SceneFlow.",
                DebugUtility.Colors.Info);
        }

        private sealed class LevelFlowMacroPrepareCompletionGate : ISceneTransitionCompletionGate
        {
            public async System.Threading.Tasks.Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
            {
                if (!context.RouteId.IsValid)
                {
                    return;
                }

                if (DependencyManager.Provider == null)
                {
                    FailFastConfig(context, "DependencyManager.Provider unavailable.");
                }

                if (!DependencyManager.Provider.TryGetGlobal<ILevelMacroPrepareService>(out var prepareService) || prepareService == null)
                {
                    FailFastConfig(context, "ILevelMacroPrepareService missing.");
                }

                string reason = string.IsNullOrWhiteSpace(context.Reason)
                    ? "SceneFlow/LevelPrepare"
                    : context.Reason.Trim();
                string signature = SceneTransitionSignature.Compute(context);

                DebugUtility.Log<LevelFlowMacroPrepareCompletionGate>(
                    $"[OBS][LevelFlow] MacroLoadingPhase='LevelPrepare' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
                    DebugUtility.Colors.Info);

                await prepareService.PrepareOrClearAsync(context.RouteId, reason);
            }

            private static void FailFastConfig(SceneTransitionContext context, string detail)
            {
                HardFailFastH1.Trigger(typeof(LevelFlowMacroPrepareCompletionGate),
                    $"[FATAL][H1][LevelFlow] Macro completion gate misconfigured: {detail} routeId='{context.RouteId}' signature='{SceneTransitionSignature.Compute(context)}' reason='{context.Reason}'.");
            }
        }
    }
}
