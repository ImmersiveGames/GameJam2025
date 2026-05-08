using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Policies;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore;
using _ImmersiveGames.NewScripts.SceneFlow.LoadingFade.Fade.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Interop;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline;

namespace _ImmersiveGames.NewScripts.SceneFlow.Installers
{
    /// <summary>
    /// Runtime composer do SceneFlow.
    /// </summary>
    public static class SceneFlowBootstrap
    {
        private static bool _runtimeComposed;
        private static SceneFlowInputModeBridge _inputModeBridge;
        private static Base11SandboxOperationalRouteTransitionAdapter _base11SandboxOperationalRouteTransitionAdapter;

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
            EnsureRouteActorSetRefContext();
            EnsureBase11SandboxOperationalRouteTransitionAdapter();
            EnsureInputModeBridge();
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
            var completionGate = ResolveRequired<ISceneTransitionCompletionGate>();
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

        private static void EnsureRouteActorSetRefContext()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneFlowRouteActorSetRefContext>(out var existingContext) && existingContext != null)
            {
                return;
            }

            var service = new SceneFlowRouteActorSetRefService();
            DependencyManager.Provider.RegisterGlobal<ISceneFlowRouteActorSetRefContext>(service);
            DependencyManager.Provider.RegisterGlobal(service);

            DebugUtility.Log(typeof(SceneFlowBootstrap),
                "[OBS][ActorsExecution] Route actor-set context composed (SceneFlow owner).",
                DebugUtility.Colors.Info);
        }

        private static void EnsureBase11SandboxOperationalRouteTransitionAdapter()
        {
            if (_base11SandboxOperationalRouteTransitionAdapter != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<Base11SandboxOperationalRouteTransitionAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _base11SandboxOperationalRouteTransitionAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<ISessionOperationalRouteTransitionExecutor>(_base11SandboxOperationalRouteTransitionAdapter);
                return;
            }

            _base11SandboxOperationalRouteTransitionAdapter = new Base11SandboxOperationalRouteTransitionAdapter();
            DependencyManager.Provider.RegisterGlobal(_base11SandboxOperationalRouteTransitionAdapter);
            DependencyManager.Provider.RegisterGlobal<ISessionOperationalRouteTransitionExecutor>(_base11SandboxOperationalRouteTransitionAdapter);
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
            ResolveRequired<ISceneFlowRouteActorSetRefContext>();
            ResolveRequired<IFadeService>();
            ResolveRequired<Base11SandboxOperationalRouteTransitionAdapter>();
            ResolveRequired<SceneFlowInputModeBridge>();

            DebugUtility.Log(typeof(SceneFlowBootstrap),
                "[OBS][SceneFlow] Runtime composition consolidada. scope='transition macro -> loading/fade -> navigation'.",
                DebugUtility.Colors.Info);
        }

        private static T ResolveRequired<T>() where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var service) && service != null)
            {
                return service;
            }

            if (typeof(T) == typeof(ISceneTransitionCompletionGate))
            {
                throw new InvalidOperationException(
                    "[FATAL][Config][SceneFlow] ISceneTransitionCompletionGate obrigatorio ausente no DI global antes da composicao runtime. " +
                    "O gate canonico deve ser registrado por SessionFlow / GameplaySessionFlowCompletionGateComposer antes de SceneFlowBootstrap.");
            }

            throw new InvalidOperationException($"[FATAL][Config][SceneFlow] {typeof(T).Name} obrigatorio ausente no DI global antes da composicao runtime.");
        }
    }
}
