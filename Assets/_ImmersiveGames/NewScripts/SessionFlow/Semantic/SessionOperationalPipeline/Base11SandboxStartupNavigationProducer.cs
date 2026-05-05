using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class Base11SandboxStartupNavigationProducer : IDisposable
    {
        private const string StartupRouteId = "to-menu";
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

            SceneRouteId routeId = SceneRouteId.FromName(StartupRouteId);
            DebugUtility.Log(typeof(Base11SandboxStartupNavigationProducer),
                $"[OBS][SessionOperationalPipeline][Navigation] boot_start_plan observed routeId='{routeId}' source='Base11SandboxStartupNavigationProducer' reason='base11_sandbox_startup_route'.",
                DebugUtility.Colors.Info);

            EventBus<NavigateToRouteCommand>.Raise(new NavigateToRouteCommand(
                routeId,
                source: nameof(Base11SandboxStartupNavigationProducer),
                reason: "base11_sandbox_startup_route"));
        }
    }
}
