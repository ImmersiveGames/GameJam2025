using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow
{
    public static class Base11SandboxOperationalRoutingComposer
    {
        private static bool _runtimeComposed;
        private static SessionOperationalStartupRouteEmitter _startupRouteEmitter;
        private static SessionOperationalPipeline _sessionOperationalPipeline;
        private static Base11SandboxOperationalRouteTransitionAdapter _routeTransitionAdapter;
        private static Base11SandboxSessionOperationalFadeAdapter _fadeAdapter;
        private static Base11SandboxSessionOperationalLoadingAdapter _loadingAdapter;
        private static Base11SandboxSessionOperationalAudioAdapter _audioAdapter;

        public static void Install(RuntimeModeConfig runtimeModeConfig)
        {
            ValidateRuntimeModeConfigOrFail(runtimeModeConfig);
            EnsureStartupRouteEmitter();

            DebugUtility.Log(typeof(Base11SandboxOperationalRoutingComposer),
                "[OBS][SessionOperationalPipeline][Composer] Base11SandboxOperationalRouting installer concluded.",
                DebugUtility.Colors.Info);
        }

        public static void ComposeRuntime(RuntimeModeConfig runtimeModeConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(Base11SandboxOperationalRoutingComposer));

            if (_runtimeComposed)
            {
                return;
            }

            ValidateRuntimeModeConfigOrFail(runtimeModeConfig);
            EnsureStartupRouteEmitter();

            EnsureSessionOperationalPipeline();
            EnsureSandboxAudioAdapter();
            EnsureSandboxFadeAdapter();
            EnsureSandboxLoadingAdapter();
            EnsureSandboxRouteExecutor();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(Base11SandboxOperationalRoutingComposer),
                "[OBS][SessionOperationalPipeline][Composer] runtime composed for Base11Sandbox operational routing.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureStartupRouteEmitter()
        {
            if (_startupRouteEmitter != null)
            {
                _startupRouteEmitter = SessionOperationalStartupRouteEmitter.EnsureInstalled();
                return;
            }

            _startupRouteEmitter = SessionOperationalStartupRouteEmitter.EnsureInstalled();
        }

        private static void ValidateRuntimeModeConfigOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] RuntimeModeConfig obrigatorio ausente para compor o routing do Base11Sandbox.");
            }

            if (runtimeModeConfig.compositionProfile != CompositionProfileKind.Base11Sandbox)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] Base11SandboxOperationalRoutingComposer requer compositionProfile=Base11Sandbox.");
            }
        }

        private static void EnsureSessionOperationalPipeline()
        {
            if (_sessionOperationalPipeline != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<SessionOperationalPipeline>(out var existingPipeline) && existingPipeline != null)
            {
                _sessionOperationalPipeline = existingPipeline;
                return;
            }

            _sessionOperationalPipeline = new SessionOperationalPipeline();
            DependencyManager.Provider.RegisterGlobal(_sessionOperationalPipeline);

            DebugUtility.Log(typeof(Base11SandboxOperationalRoutingComposer),
                "[OBS][SessionOperationalPipeline][Composer] SessionOperationalPipeline registered for Base11Sandbox.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureSandboxRouteExecutor()
        {
            if (_routeTransitionAdapter != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<Base11SandboxOperationalRouteTransitionAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _routeTransitionAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<ISessionOperationalRouteTransitionExecutor>(_routeTransitionAdapter);
                return;
            }

            _routeTransitionAdapter = new Base11SandboxOperationalRouteTransitionAdapter();
            DependencyManager.Provider.RegisterGlobal(_routeTransitionAdapter);
            DependencyManager.Provider.RegisterGlobal<ISessionOperationalRouteTransitionExecutor>(_routeTransitionAdapter);

            DebugUtility.Log(typeof(Base11SandboxOperationalRoutingComposer),
                "[OBS][SessionOperationalPipeline][Composer] adapter='Base11SandboxOperationalRouteTransitionAdapter' registered for Base11Sandbox.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureSandboxAudioAdapter()
        {
            if (_audioAdapter != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<Base11SandboxSessionOperationalAudioAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _audioAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<ISessionOperationalAudioAdapter>(_audioAdapter);
                return;
            }

            _audioAdapter = new Base11SandboxSessionOperationalAudioAdapter();
            DependencyManager.Provider.RegisterGlobal(_audioAdapter);
            DependencyManager.Provider.RegisterGlobal<ISessionOperationalAudioAdapter>(_audioAdapter);

            DebugUtility.Log(typeof(Base11SandboxOperationalRoutingComposer),
                "[OBS][SessionOperationalPipeline][Composer] adapter='Base11SandboxSessionOperationalAudioAdapter' registered for Base11Sandbox.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureSandboxFadeAdapter()
        {
            if (_fadeAdapter != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<Base11SandboxSessionOperationalFadeAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _fadeAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<ISessionOperationalFadeAdapter>(_fadeAdapter);
                return;
            }

            _fadeAdapter = new Base11SandboxSessionOperationalFadeAdapter();
            DependencyManager.Provider.RegisterGlobal(_fadeAdapter);
            DependencyManager.Provider.RegisterGlobal<ISessionOperationalFadeAdapter>(_fadeAdapter);

            DebugUtility.Log(typeof(Base11SandboxOperationalRoutingComposer),
                "[OBS][SessionOperationalPipeline][Composer] adapter='Base11SandboxSessionOperationalFadeAdapter' registered for Base11Sandbox.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureSandboxLoadingAdapter()
        {
            if (_loadingAdapter != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<Base11SandboxSessionOperationalLoadingAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _loadingAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<ISessionOperationalLoadingAdapter>(_loadingAdapter);
                return;
            }

            _loadingAdapter = new Base11SandboxSessionOperationalLoadingAdapter();
            DependencyManager.Provider.RegisterGlobal(_loadingAdapter);
            DependencyManager.Provider.RegisterGlobal<ISessionOperationalLoadingAdapter>(_loadingAdapter);

            DebugUtility.Log(typeof(Base11SandboxOperationalRoutingComposer),
                "[OBS][SessionOperationalPipeline][Composer] adapter='Base11SandboxSessionOperationalLoadingAdapter' registered for Base11Sandbox.",
                DebugUtility.Colors.Info);
        }
    }
}
