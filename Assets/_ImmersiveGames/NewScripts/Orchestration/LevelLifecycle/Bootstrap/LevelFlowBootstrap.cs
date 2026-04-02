using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Interop;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.Navigation;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Bootstrap
{
    /// <summary>
    /// Runtime composer canonico do LevelLifecycle.
    /// O nome historico do arquivo permanece apenas por compatibilidade.
    ///
    /// Responsabilidade:
    /// - compor as partes do LevelLifecycle que dependem de Navigation depois que o DI ja esta preenchido;
    /// - nao registrar contratos de boot.
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

            EnsureLevelFlowCompletionGate();
            EnsureLevelFlowRuntimeService();
            EnsureLevelSelectedRestartSnapshotBridge();
            EnsurePostLevelActionsService(bootstrapConfig);
            EnsureLevelStageOrchestrator();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(LevelLifecycleBootstrap),
                "[LevelLifecycle] Runtime composition concluida.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureLevelFlowRuntimeService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelFlowRuntimeService>(out var existingRuntime) && existingRuntime != null)
            {
                return;
            }

            var navigationService = ResolveRequiredNavigationService();
            var restartContextService = ResolveRequiredRestartContextService();
            var levelSwapLocalService = ResolveRequiredLevelSwapLocalService();

            var runtimeService = new LevelLifecycleRuntimeService(
                navigationService,
                restartContextService,
                levelSwapLocalService);

            DependencyManager.Provider.RegisterGlobal<ILevelFlowRuntimeService>(runtimeService);

            DebugUtility.LogVerbose(typeof(LevelLifecycleBootstrap),
                "[OBS][LevelLifecycle] LevelLifecycleRuntimeService registrado no runtime.",
                DebugUtility.Colors.Info);
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
                "[OBS][LevelLifecycle] Gate de LevelPrepare/Clear acoplado ao completion gate macro do SceneFlow.",
                DebugUtility.Colors.Info);
        }

        private static void EnsurePostLevelActionsService(BootstrapConfigAsset bootstrapConfig)
        {
            if (DependencyManager.Provider.TryGetGlobal<IPostLevelActionsService>(out var existingPostActions) && existingPostActions != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILevelFlowRuntimeService>(out var levelFlowRuntime) || levelFlowRuntime == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelLifecycle] ILevelFlowRuntimeService ausente no DI global antes da composicao runtime.");
            }

            var levelSwapLocalService = ResolveRequiredLevelSwapLocalService();
            var restartContextService = ResolveRequiredRestartContextService();
            var levelFlowContentService = ResolveRequiredLevelFlowContentService();

            var navigationService = ResolveRequiredNavigationService();
            var postLevelActions = new PostLevelActionsService(
                levelFlowRuntime,
                levelSwapLocalService,
                restartContextService,
                navigationService,
                levelFlowContentService);

            DependencyManager.Provider.RegisterGlobal(postLevelActions);

            DebugUtility.LogVerbose(typeof(LevelLifecycleBootstrap),
                "[OBS][LevelLifecycle] IPostLevelActionsService registrado no runtime.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureLevelSelectedRestartSnapshotBridge()
        {
            RegisterIfMissing(
                () => new LevelSelectedRestartSnapshotBridge(),
                typeof(LevelSelectedRestartSnapshotBridge),
                "[LevelLifecycle] LevelSelectedRestartSnapshotBridge ja registrado no DI global.",
                "[LevelLifecycle] LevelSelectedRestartSnapshotBridge registrado no DI global.");
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

    [Obsolete("Historical wrapper. Use LevelLifecycleBootstrap instead.")]
    public static class LevelFlowBootstrap
    {
        public static void ComposeRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            LevelLifecycleBootstrap.ComposeRuntime(bootstrapConfig);
        }
    }
}
