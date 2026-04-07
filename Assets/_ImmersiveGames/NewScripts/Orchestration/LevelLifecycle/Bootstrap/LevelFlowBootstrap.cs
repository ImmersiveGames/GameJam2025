using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Experience.PostRun.Ownership;
using _ImmersiveGames.NewScripts.Experience.PostRun.Result;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.Navigation;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Bootstrap
{
    /// <summary>
    /// Compositor operacional do LevelLifecycle.
    /// O nome historico do arquivo permanece apenas por compatibilidade.
    ///
    /// Responsabilidade:
    /// - compor as partes operacionais do LevelLifecycle que viabilizam o GameplaySessionFlow depois que o DI ja esta preenchido;
    /// - nao registrar contratos de boot nem ownership semantico do gameplay.
    /// </summary>
    public static class LevelLifecycleBootstrap
    {
        private static bool _runtimeComposed;
        private static bool _levelFlowGateComposed;

        public static void ComposeRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(LevelLifecycleBootstrap));

            if (_runtimeComposed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] BootstrapConfigAsset obrigatorio ausente para compor o runtime.");
            }

            var navigationService = ResolveRequiredNavigationService();
            var restartContextService = ResolveRequiredRestartContextService();
            var levelSwapLocalService = ResolveRequiredLevelSwapLocalService();
            var levelFlowContentService = ResolveRequiredLevelFlowContentService();

            EnsureLevelFlowCompletionGate();
            var levelFlowRuntime = EnsureLevelFlowRuntimeService(
                navigationService,
                restartContextService,
                levelSwapLocalService);
            EnsurePostLevelActionsService(
                levelFlowRuntime,
                levelSwapLocalService,
                restartContextService,
                navigationService,
                levelFlowContentService);
            EnsureLevelStageOrchestrator();
            EnsureGameplaySessionFlowComposition();
            EnsureGameplaySessionContinuityComposition();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(LevelLifecycleBootstrap),
                "[LevelLifecycle] Runtime composition concluida.",
                DebugUtility.Colors.Info);
        }

        private static ILevelFlowRuntimeService EnsureLevelFlowRuntimeService(
            IGameNavigationService navigationService,
            IRestartContextService restartContextService,
            ILevelSwapLocalService levelSwapLocalService)
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelFlowRuntimeService>(out var existingRuntime) && existingRuntime != null)
            {
                return existingRuntime;
            }

            var runtimeService = new LevelLifecycleRuntimeService(
                navigationService,
                restartContextService,
                levelSwapLocalService);

            DependencyManager.Provider.RegisterGlobal<ILevelFlowRuntimeService>(runtimeService);

            DebugUtility.LogVerbose(typeof(LevelLifecycleBootstrap),
                "[OBS][LevelLifecycle][Operational] LevelLifecycleRuntimeService registrado no runtime como ponte operacional para GameplaySessionFlow.",
                DebugUtility.Colors.Info);

            return runtimeService;
        }

        private static void EnsureLevelFlowCompletionGate()
        {
            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionCompletionGate>(out var existingGate) || existingGate == null)
            {
                throw new InvalidOperationException(
                    "[FATAL][Config][LevelLifecycle] ISceneTransitionCompletionGate obrigatorio ausente para composicao de LevelPrepare/Clear.");
            }

            if (existingGate is not MacroLevelPrepareCompletionGate macroGate)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][LevelLifecycle] ISceneTransitionCompletionGate invalido para composicao de LevelLifecycle (tipo='{existingGate.GetType().Name}').");
            }

            if (_levelFlowGateComposed)
            {
                return;
            }

            macroGate.ConfigureLevelFlowGate(new LevelFlowMacroPrepareCompletionGate());
            _levelFlowGateComposed = true;

            DebugUtility.LogVerbose(typeof(LevelLifecycleBootstrap),
                "[OBS][LevelLifecycle][Operational] Gate de LevelPrepare/Clear acoplado ao completion gate macro do SceneFlow como viabilizacao operacional.",
                DebugUtility.Colors.Info);
        }

        private static void EnsurePostLevelActionsService(
            ILevelFlowRuntimeService levelFlowRuntime,
            ILevelSwapLocalService levelSwapLocalService,
            IRestartContextService restartContextService,
            IGameNavigationService navigationService,
            ILevelFlowContentService levelFlowContentService)
        {
            if (DependencyManager.Provider.TryGetGlobal<IPostLevelActionsService>(out var existingPostActions) && existingPostActions != null)
            {
                return;
            }

            var postLevelActions = new PostLevelActionsService(
                levelFlowRuntime,
                levelSwapLocalService,
                restartContextService,
                navigationService,
                levelFlowContentService);

            DependencyManager.Provider.RegisterGlobal(postLevelActions);

            DebugUtility.LogVerbose(typeof(LevelLifecycleBootstrap),
                "[OBS][LevelLifecycle][Operational] IPostLevelActionsService registrado no runtime como support service de continuidade.",
                DebugUtility.Colors.Info);
        }

        private static IGameNavigationService ResolveRequiredNavigationService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var navigationService) && navigationService != null)
            {
                return navigationService;
            }

            throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] IGameNavigationService ausente no DI global antes da composicao runtime.");
        }

        private static ILevelFlowContentService ResolveRequiredLevelFlowContentService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelFlowContentService>(out var levelFlowContentService) && levelFlowContentService != null)
            {
                return levelFlowContentService;
            }

            throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] ILevelFlowContentService ausente no DI global antes da composicao runtime.");
        }

        private static IRestartContextService ResolveRequiredRestartContextService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var restartContextService) && restartContextService != null)
            {
                return restartContextService;
            }

            throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] IRestartContextService ausente no DI global antes da composicao runtime.");
        }

        private static ILevelSwapLocalService ResolveRequiredLevelSwapLocalService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelSwapLocalService>(out var levelSwapLocalService) && levelSwapLocalService != null)
            {
                return levelSwapLocalService;
            }

            throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] ILevelSwapLocalService ausente no DI global antes da composicao runtime.");
        }

        private static void EnsureLevelStageOrchestrator()
        {
            RegisterIfMissing(
                () => new LevelLifecycleStageOrchestrator(),
                typeof(LevelLifecycleStageOrchestrator),
                "[LevelLifecycle] LevelLifecycleStageOrchestrator ja registrado no DI global.",
                "[LevelLifecycle] LevelLifecycleStageOrchestrator registrado (LevelEntered hook).");
        }

        private static void EnsureGameplaySessionFlowComposition()
        {
            RequireGlobal<IPhaseDefinitionSelectionService>("IPhaseDefinitionSelectionService");
            RequireGlobal<IGameplaySessionContextService>("IGameplaySessionContextService");
            RequireGlobal<IGameplayPhaseRuntimeService>("IGameplayPhaseRuntimeService");
            RequireGlobal<IGameplayPhasePlayerParticipationService>("IGameplayPhasePlayerParticipationService");
            RequireGlobal<IGameplayPhaseRulesObjectivesService>("IGameplayPhaseRulesObjectivesService");
            RequireGlobal<IGameplayPhaseInitialStateService>("IGameplayPhaseInitialStateService");
            RequireGlobal<GameplaySessionFlowPhaseConsumptionService>("GameplaySessionFlowPhaseConsumptionService");
            RequireGlobal<ILevelMacroPrepareService>("ILevelMacroPrepareService");
            RequireGlobal<LevelLifecycleStageOrchestrator>("LevelLifecycleStageOrchestrator");
            RequireGlobal<IIntroStageCoordinator>("IIntroStageCoordinator");
            RequireGlobal<IIntroStageControlService>("IIntroStageControlService");
            RequireGlobal<IGameRunOutcomeService>("IGameRunOutcomeService");
            RequireGlobal<IRestartContextService>("IRestartContextService");
            RequireGlobal<ILevelFlowRuntimeService>("ILevelFlowRuntimeService");
            RequireGlobal<IRunEndIntentOwnershipService>("IRunEndIntentOwnershipService");
            RequireGlobal<IPostLevelActionsService>("IPostLevelActionsService");

            DebugUtility.Log(typeof(LevelLifecycleBootstrap),
                "[OBS][GameplaySessionFlow] Runtime composition consolidada. scope='SessionContext -> PhaseRuntime -> Players -> RulesObjectives -> InitialState -> Prepare -> Intro -> Playing -> Outcome -> RunEndRail -> Continuity'.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureGameplaySessionContinuityComposition()
        {
            RequireGlobal<IRunEndIntentOwnershipService>("IRunEndIntentOwnershipService");
            RequireGlobal<IPostRunResultService>("RunEndRailResultService");
            RequireGlobal<IPostLevelActionsService>("IPostLevelActionsService");

            DebugUtility.Log(typeof(LevelLifecycleBootstrap),
                "[OBS][GameplaySessionFlow][Continuity] Runtime composition consolidada. scope='RunEndRail -> Continuity -> exit/menu handoff'.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIfMissing<T>(Func<T> factory, Type contextType, string alreadyRegisteredMessage, string registeredMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(contextType, alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(contextType, registeredMessage, DebugUtility.Colors.Info);
        }

        private static void RequireGlobal<T>(string serviceName)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                return;
            }

            throw new InvalidOperationException($"[FATAL][Config][LevelLifecycle] {serviceName} obrigatorio ausente para compor o GameplaySessionFlow runtime.");
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
                    $"[OBS][LevelLifecycle] MacroLoadingPhase='LevelPrepare' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
                    DebugUtility.Colors.Info);

                if (context.RouteRef == null)
                {
                    FailFastConfig(context, "SceneTransitionContext sem RouteRef canonica.");
                }

                await prepareService.PrepareOrClearAsync(context.RouteId, context.RouteRef, reason);
            }

            private static void FailFastConfig(SceneTransitionContext context, string detail)
            {
                HardFailFastH1.Trigger(typeof(LevelFlowMacroPrepareCompletionGate),
                    $"[FATAL][H1][LevelLifecycle] Macro completion gate misconfigured: {detail} routeId='{context.RouteId}' signature='{SceneTransitionSignature.Compute(context)}' reason='{context.Reason}'.");
            }
        }
    }

}
