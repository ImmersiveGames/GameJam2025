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
    }
}
