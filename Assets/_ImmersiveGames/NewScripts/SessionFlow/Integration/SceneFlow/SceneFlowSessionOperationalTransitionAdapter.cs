using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow
{
    /// <summary>
    /// Pipeline Adapter tecnico temporario.
    /// Executa a transicao de cena sem conhecer ownership semantico da Base 1.1.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneFlowSessionOperationalTransitionAdapter : ISessionOperationalTransitionPort, IDisposable
    {
        private readonly IGameNavigationService _navigationService;
        private bool _disposed;

        public SceneFlowSessionOperationalTransitionAdapter(IGameNavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        public void RequestRouteTransition(RequestRouteTransitionCommand command)
        {
            if (_disposed || !command.IsValid)
            {
                return;
            }

            DebugUtility.Log(typeof(SceneFlowSessionOperationalTransitionAdapter),
                $"[OBS][SessionOperationalPipeline][Transition] adapter='SceneFlowSessionOperationalTransitionAdapter' command='RequestRouteTransition' routeId='{command.RouteId}' routeProfileId='{command.RouteProfileId}' routeKind='{command.RouteKind}' resolvedRoute='true' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            Task dispatchTask = _navigationService.NavigateToResolvedRoute(command.ResolvedRoute, command.Reason);
            dispatchTask.ContinueWith(completed =>
            {
                if (!completed.IsFaulted)
                {
                    DebugUtility.Log(typeof(SceneFlowSessionOperationalTransitionAdapter),
                        $"[OBS][SessionOperationalPipeline][Transition] adapter='SceneFlowSessionOperationalTransitionAdapter' dispatched routeId='{command.RouteId}' routeProfileId='{command.RouteProfileId}' routeKind='{command.RouteKind}' resolvedRoute='true' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                Exception exception = completed.Exception?.GetBaseException() ?? completed.Exception;
                DebugUtility.LogError(typeof(SceneFlowSessionOperationalTransitionAdapter),
                    $"[OBS][SessionOperationalPipeline][Transition] adapter='SceneFlowSessionOperationalTransitionAdapter' dispatch_failed routeId='{command.RouteId}' routeProfileId='{command.RouteProfileId}' routeKind='{command.RouteKind}' resolvedRoute='true' source='{command.Source}' reason='{command.Reason}' exceptionType='{exception?.GetType().Name}' exceptionMessage='{exception?.Message}'.");
            }, TaskScheduler.Default);
        }
    }
}
