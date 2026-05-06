using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class Base11SandboxStartupNavigationProducer : IDisposable
    {
        private readonly EventBinding<BootStartPlanRequestedEvent> _binding;
        private bool _disposed;
        private bool _hasPublished;

        public Base11SandboxStartupNavigationProducer()
        {
            _binding = new EventBinding<BootStartPlanRequestedEvent>(OnBootStartPlanRequested);
            EventBus<BootStartPlanRequestedEvent>.Register(_binding);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<BootStartPlanRequestedEvent>.Unregister(_binding);
        }

        private void OnBootStartPlanRequested(BootStartPlanRequestedEvent evt)
        {
            if (_disposed || _hasPublished)
            {
                return;
            }

            _hasPublished = true;

            RuntimeModeConfig runtimeModeConfig = ResolveRuntimeModeConfigOrFail();
            SceneRouteDefinitionAsset startupRoute = ResolveStartupRouteOrFail(runtimeModeConfig);
            SceneRouteId routeId = startupRoute.RouteId;
            DebugUtility.Log(typeof(Base11SandboxStartupNavigationProducer),
                $"[OBS][SessionOperationalPipeline][Navigation] boot_start_plan observed routeId='{routeId}' source='Base11SandboxStartupNavigationProducer' reason='base11_sandbox_startup_route'.",
                DebugUtility.Colors.Info);

            EventBus<NavigateToRouteCommand>.Raise(new NavigateToRouteCommand(
                startupRoute,
                source: nameof(Base11SandboxStartupNavigationProducer),
                reason: "base11_sandbox_startup_route"));
        }

        private static RuntimeModeConfig ResolveRuntimeModeConfigOrFail()
        {
            if (DependencyManager.Provider == null)
            {
                string message = "[FATAL][Config][SessionOperationalPipeline] DependencyManager indisponivel para resolver RuntimeModeConfig do Base11Sandbox.";
                DebugUtility.LogError(typeof(Base11SandboxStartupNavigationProducer), message);
                throw new InvalidOperationException(message);
            }

            if (!DependencyManager.Provider.TryGetGlobal<RuntimeModeConfig>(out var runtimeModeConfig) || runtimeModeConfig == null)
            {
                string message = "[FATAL][Config][SessionOperationalPipeline] RuntimeModeConfig obrigatorio ausente para resolver startupRouteDefinition do Base11Sandbox.";
                DebugUtility.LogError(typeof(Base11SandboxStartupNavigationProducer), message);
                throw new InvalidOperationException(message);
            }

            if (runtimeModeConfig.compositionProfile != CompositionProfileKind.Base11Sandbox)
            {
                string message = $"[FATAL][Config][SessionOperationalPipeline] Base11SandboxStartupNavigationProducer requerido fora do Base11Sandbox. compositionProfile='{runtimeModeConfig.compositionProfile}'.";
                DebugUtility.LogError(typeof(Base11SandboxStartupNavigationProducer), message);
                throw new InvalidOperationException(message);
            }

            return runtimeModeConfig;
        }

        private static SceneRouteDefinitionAsset ResolveStartupRouteOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new ArgumentNullException(nameof(runtimeModeConfig));
            }

            if (runtimeModeConfig.StartupRouteDefinition == null)
            {
                string message = "[FATAL][Config][SessionOperationalPipeline] startupRouteDefinition obrigatoria ausente para Base11Sandbox.";
                DebugUtility.LogError(typeof(Base11SandboxStartupNavigationProducer), message);
                throw new InvalidOperationException(message);
            }

            if (!runtimeModeConfig.StartupRouteDefinition.RouteId.IsValid)
            {
                string message = "[FATAL][Config][SessionOperationalPipeline] startupRouteDefinition asset invalida para Base11Sandbox.";
                DebugUtility.LogError(typeof(Base11SandboxStartupNavigationProducer), message);
                throw new InvalidOperationException(message);
            }

            if (runtimeModeConfig.StartupRouteDefinition.RouteProfile == null)
            {
                string message = "[FATAL][Config][SessionOperationalPipeline] startupRouteDefinition asset sem SceneRouteProfile para Base11Sandbox.";
                DebugUtility.LogError(typeof(Base11SandboxStartupNavigationProducer), message);
                throw new InvalidOperationException(message);
            }

            return runtimeModeConfig.StartupRouteDefinition;
        }
    }
}
