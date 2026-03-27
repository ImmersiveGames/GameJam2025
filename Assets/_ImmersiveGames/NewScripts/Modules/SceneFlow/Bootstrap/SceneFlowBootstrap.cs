using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Modules.ResetInterop.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Interop;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Loading.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Adapters;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Policies;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Bootstrap
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

            _runtimeComposed = true;

            DebugUtility.LogVerbose(typeof(SceneFlowBootstrap),
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
                if (existingGate is MacroLevelPrepareCompletionGate macroGate)
                {
                    return macroGate;
                }

                DebugUtility.LogVerbose(typeof(SceneFlowBootstrap),
                    $"[SceneFlow] ISceneTransitionCompletionGate existente sera substituido por MacroLevelPrepareCompletionGate (tipo='{existingGate.GetType().Name}').",
                    DebugUtility.Colors.Info);
            }

            var fallbackGate = new WorldResetCompletionGate(timeoutMs: 20000);
            var composedGate = new MacroLevelPrepareCompletionGate(fallbackGate);
            DependencyManager.Provider.RegisterGlobal<ISceneTransitionCompletionGate>(composedGate, allowOverride: true);
            return composedGate;
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
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(SceneFlowBootstrap),
                    $"[ERROR][Fade] Failed to preload FadeScene during SceneFlow runtime bootstrap. ex='{ex.GetType().Name}: {ex.Message}'");
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
