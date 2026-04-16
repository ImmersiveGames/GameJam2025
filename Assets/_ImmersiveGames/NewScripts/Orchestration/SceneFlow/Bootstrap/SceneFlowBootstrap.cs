using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.ResetInterop.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Fade.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Interop;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Loading.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneComposition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Policies;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Bootstrap
{
    /// <summary>
    /// Runtime composer do SceneFlow.
    ///
    /// Responsabilidade:
    /// - compor e ativar o runtime do SceneFlow depois que os installers relevantes concluíram;
    /// - nao registrar contratos de boot.
    /// </summary>
    public static class SceneFlowBootstrap
    {
        private static bool _runtimeComposed;
        private static SceneFlowInputModeBridge _inputModeBridge;
        private static LoadingHudOrchestrator _loadingHudOrchestrator;
        private static LoadingProgressOrchestrator _loadingProgressOrchestrator;

        public static void ComposeRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(SceneFlowBootstrap));

            if (_runtimeComposed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SceneFlow] BootstrapConfigAsset obrigatorio ausente para compor o runtime.");
            }

            EnsureSceneTransitionService();
            EnsureInputModeBridge();
            EnsureLoadingOrchestrators();
            EnsureFadeReadyAsync();
            EnsureSceneFlowModuleComposition();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(SceneFlowBootstrap),
                "[SceneFlow] Runtime composition concluida.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureSceneTransitionService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var existing) && existing != null)
            {
                return;
            }

            var loaderAdapter = SceneFlowAdapterFactory.CreateLoaderAdapter();
            var fadeAdapter = SceneFlowAdapterFactory.CreateFadeAdapter(DependencyManager.Provider);
            var completionGate = ResolveOrComposeCompletionGate();
            var navigationPolicy = ResolveRequired<INavigationPolicy>();
            var routeGuard = ResolveRequired<IRouteGuard>();
            var routeResetPolicy = ResolveRequired<IRouteResetPolicy>();

            var service = new SceneTransitionService(
                loaderAdapter,
                fadeAdapter,
                completionGate,
                navigationPolicy,
                routeGuard,
                routeResetPolicy);

            DependencyManager.Provider.RegisterGlobal<ISceneTransitionService>(service);

            DebugUtility.LogVerbose(typeof(SceneFlowBootstrap),
                $"[SceneFlow] SceneTransitionService composto no runtime (Loader={loaderAdapter.GetType().Name}, FadeAdapter={fadeAdapter.GetType().Name}, Gate={completionGate.GetType().Name}, Policy={navigationPolicy.GetType().Name}, RouteGuard={routeGuard.GetType().Name}, RouteResetPolicy={routeResetPolicy.GetType().Name}).",
                DebugUtility.Colors.Info);
        }

        private static ISceneTransitionCompletionGate ResolveOrComposeCompletionGate()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneTransitionCompletionGate>(out var existingGate) && existingGate != null)
            {
                if (existingGate is GameplaySessionFlowCompletionGate sessionFlowGate)
                {
                    EnsureGameplaySessionFlowGate(sessionFlowGate);
                    return sessionFlowGate;
                }

                DebugUtility.LogVerbose(typeof(SceneFlowBootstrap),
                    $"[SceneFlow] ISceneTransitionCompletionGate existente sera substituido por GameplaySessionFlowCompletionGate (tipo='{existingGate.GetType().Name}').",
                    DebugUtility.Colors.Info);
            }

            var fallbackGate = new WorldResetCompletionGate(timeoutMs: 20000);
            var composedGate = new GameplaySessionFlowCompletionGate(fallbackGate);
            EnsureGameplaySessionFlowGate(composedGate);
            DependencyManager.Provider.RegisterGlobal<ISceneTransitionCompletionGate>(composedGate, allowOverride: true);
            return composedGate;
        }

        private static void EnsureGameplaySessionFlowGate(GameplaySessionFlowCompletionGate sessionFlowGate)
        {
            if (sessionFlowGate == null)
            {
                throw new ArgumentNullException(nameof(sessionFlowGate));
            }

            sessionFlowGate.ConfigureGameplaySessionFlowGate(new GameplaySessionFlowPrepareCompletionGate());
        }

        private static void EnsureInputModeBridge()
        {
            if (_inputModeBridge != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<SceneFlowInputModeBridge>(out var existing) && existing != null)
            {
                _inputModeBridge = existing;
                return;
            }

            _inputModeBridge = new SceneFlowInputModeBridge();
            DependencyManager.Provider.RegisterGlobal(_inputModeBridge);
        }

        private static void EnsureLoadingOrchestrators()
        {
            ResolveRequired<ILoadingPresentationService>();
            ResolveRequired<ILoadingHudService>();

            if (_loadingHudOrchestrator == null)
            {
                if (DependencyManager.Provider.TryGetGlobal<LoadingHudOrchestrator>(out var existingHud) && existingHud != null)
                {
                    _loadingHudOrchestrator = existingHud;
                }
                else
                {
                    _loadingHudOrchestrator = new LoadingHudOrchestrator();
                    DependencyManager.Provider.RegisterGlobal(_loadingHudOrchestrator);
                }
            }

            if (_loadingProgressOrchestrator == null)
            {
                if (DependencyManager.Provider.TryGetGlobal<LoadingProgressOrchestrator>(out var existingProgress) && existingProgress != null)
                {
                    _loadingProgressOrchestrator = existingProgress;
                }
                else
                {
                    _loadingProgressOrchestrator = new LoadingProgressOrchestrator();
                    DependencyManager.Provider.RegisterGlobal(_loadingProgressOrchestrator);
                }
            }
        }

        private static async void EnsureFadeReadyAsync()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IFadeService>(out var fadeService) || fadeService == null)
            {
                return;
            }

            try
            {
                await fadeService.EnsureReadyAsync();
                DebugUtility.LogVerbose(typeof(SceneFlowBootstrap),
                    "[OBS][Fade] FadeScene ready (source=SceneFlowBootstrap/ComposeRuntime).",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(SceneFlowBootstrap),
                    $"[ERROR][Fade] Failed to preload FadeScene during SceneFlow runtime bootstrap. ex='{ex.GetType().Name}: {ex.Message}'");
            }
        }

        private static void EnsureSceneFlowModuleComposition()
        {
            ResolveRequired<ISceneTransitionService>();
            ResolveRequired<INavigationPolicy>();
            ResolveRequired<IRouteGuard>();
            ResolveRequired<IRouteResetPolicy>();
            ResolveRequired<ILoadingPresentationService>();
            ResolveRequired<ILoadingHudService>();
            ResolveRequired<IFadeService>();
            ResolveRequired<SceneFlowInputModeBridge>();
            ResolveRequired<LoadingHudOrchestrator>();
            ResolveRequired<LoadingProgressOrchestrator>();

            DebugUtility.Log(typeof(SceneFlowBootstrap),
                "[OBS][SceneFlow] Runtime composition consolidada. scope='transition macro -> loading/fade -> navigation'.",
                DebugUtility.Colors.Info);
        }

        private sealed class GameplaySessionFlowPrepareCompletionGate : ISceneTransitionCompletionGate
        {
            public async System.Threading.Tasks.Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
            {
                if (!context.RouteId.IsValid)
                {
                    return;
                }

                if (context.RouteRef == null)
                {
                    DebugUtility.LogVerbose(typeof(GameplaySessionFlowPrepareCompletionGate),
                        $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareSkipped routeId='{context.RouteId}' reason='routeRef_missing'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (context.RouteRef.RouteKind != SceneRouteKind.Gameplay)
                {
                    DebugUtility.LogVerbose(typeof(GameplaySessionFlowPrepareCompletionGate),
                        $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareSkipped routeId='{context.RouteId}' routeKind='{context.RouteRef.RouteKind}' reason='non_gameplay_route'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (DependencyManager.Provider == null)
                {
                    FailFastConfig(context, "DependencyManager.Provider unavailable.");
                }

                if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionSelectionService>(out var phaseSelectionService) || phaseSelectionService == null)
                {
                    FailFastConfig(context, "IPhaseDefinitionSelectionService missing.");
                }

                if (!DependencyManager.Provider.TryGetGlobal<GameplayPhaseFlowService>(out var gameplayPhaseFlowService) || gameplayPhaseFlowService == null)
                {
                    FailFastConfig(context, "GameplayPhaseFlowService missing.");
                }

                if (!DependencyManager.Provider.TryGetGlobal<ISceneCompositionExecutor>(out var sceneCompositionExecutor) || sceneCompositionExecutor == null)
                {
                    FailFastConfig(context, "ISceneCompositionExecutor missing.");
                }

                string reason = string.IsNullOrWhiteSpace(context.Reason)
                    ? "SceneFlow/GameplaySessionPrepare"
                    : context.Reason.Trim();
                string signature = SceneTransitionSignature.Compute(context);

                DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                    $"[OBS][GameplaySessionFlow][Operational] MacroLoadingPhase='GameplaySessionPrepare' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
                    DebugUtility.Colors.Info);

                PhaseDefinitionAsset selectedPhaseDefinitionRef = phaseSelectionService.ResolveOrFail();
                PhaseDefinitionSelectedEvent phaseSelectedEvent = gameplayPhaseFlowService.PublishPhaseDefinitionSelected(
                    selectedPhaseDefinitionRef,
                    context.RouteId,
                    context.RouteRef,
                    reason);

                SceneCompositionRequest phaseCompositionRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateApplyRequest(
                    selectedPhaseDefinitionRef,
                    reason,
                    phaseSelectedEvent.SelectionSignature,
                    forceFullReload: false);

                SceneCompositionResult compositionResult = await sceneCompositionExecutor.ApplyAsync(phaseCompositionRequest);

                PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                    selectedPhaseDefinitionRef,
                    phaseCompositionRequest.ScenesToLoad,
                    phaseCompositionRequest.ActiveScene,
                    "GameplaySessionFlow");

                DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                    $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareCompleted phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' routeId='{context.RouteId}' signature='{signature}' scenesAdded={compositionResult.ScenesAdded} scenesRemoved={compositionResult.ScenesRemoved} reason='{reason}'.",
                    DebugUtility.Colors.Success);
            }

            private static void FailFastConfig(SceneTransitionContext context, string detail)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowPrepareCompletionGate),
                    $"[FATAL][H1][SceneFlow] GameplaySessionFlow completion gate misconfigured: {detail} routeId='{context.RouteId}' signature='{SceneTransitionSignature.Compute(context)}' reason='{context.Reason}'.");
            }
        }

        private static T ResolveRequired<T>() where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var service) && service != null)
            {
                return service;
            }

            throw new InvalidOperationException($"[FATAL][Config][SceneFlow] {typeof(T).Name} obrigatorio ausente no DI global antes da composicao runtime.");
        }
    }
}
