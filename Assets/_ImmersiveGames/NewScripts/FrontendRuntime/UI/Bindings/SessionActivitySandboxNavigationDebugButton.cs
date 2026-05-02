using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.FrontendRuntime.UI.Bindings
{
    [DisallowMultipleComponent]
    public sealed class SessionActivitySandboxNavigationDebugButton : FrontendButtonBinderBase
    {
        [Header("Sandbox")]
        [SerializeField] private string routeId = "to-session-activity-sandbox";

        protected override bool OnClickCore(string actionReason)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(actionReason) ? "Menu/QA/SessionActivitySandbox" : actionReason.Trim();
            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var navigationService) || navigationService == null)
            {
                DebugUtility.LogWarning<SessionActivitySandboxNavigationDebugButton>(
                    "[OBS][FrontendUI][QA] Sandbox navigation button ignored because IGameNavigationService is unavailable.");
                return false;
            }

            if (!TryResolveRouteId(out SceneRouteId resolvedRouteId))
            {
                return false;
            }

            DebugUtility.LogVerbose<SessionActivitySandboxNavigationDebugButton>(
                $"[OBS][FrontendUI][QA] SessionActivitySandbox navigation requested routeId='{resolvedRouteId}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            DispatchRoute(navigationService, resolvedRouteId, normalizedReason);
            return true;
        }

        private bool TryResolveRouteId(out SceneRouteId resolvedRouteId)
        {
            resolvedRouteId = SceneRouteId.None;
            if (string.IsNullOrWhiteSpace(routeId))
            {
                DebugUtility.LogWarning<SessionActivitySandboxNavigationDebugButton>(
                    "[OBS][FrontendUI][QA] Sandbox navigation button rejected because routeId is empty.");
                return false;
            }

            resolvedRouteId = SceneRouteId.FromName(routeId);
            if (!resolvedRouteId.IsValid)
            {
                DebugUtility.LogWarning<SessionActivitySandboxNavigationDebugButton>(
                    $"[OBS][FrontendUI][QA] Sandbox navigation button rejected because routeId is invalid. routeId='{routeId}'.");
                return false;
            }

            return true;
        }

        private static void DispatchRoute(IGameNavigationService navigationService, SceneRouteId routeId, string reason)
        {
            Task navigationTask = navigationService.NavigateToRoute(routeId, reason);
            navigationTask.ContinueWith(completed =>
            {
                if (completed.IsFaulted)
                {
                    Exception exception = completed.Exception?.GetBaseException() ?? completed.Exception;
                    DebugUtility.LogError<SessionActivitySandboxNavigationDebugButton>(
                        $"[OBS][FrontendUI][QA] Sandbox navigation failed routeId='{routeId}' reason='{reason}' exceptionType='{exception?.GetType().Name}' exceptionMessage='{exception?.Message}'.");
                }
            }, TaskScheduler.Default);
        }
    }
}
