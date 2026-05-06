using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.FrontendRuntime.UI.Bindings
{
    [DisallowMultipleComponent]
    public sealed class SessionActivitySandboxNavigationDebugButton : FrontendButtonBinderBase
    {
        private const string SandboxButtonSource = "Frontend/SandboxButton";
        private const string SandboxButtonReason = "Frontend/SandboxButton";

        [Header("Sandbox")]
        [SerializeField] private string routeId = "to-session-activity-sandbox";

        protected override bool OnClickCore(string _)
        {
            if (!TryResolveRouteId(out SceneRouteId resolvedRouteId))
            {
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<SessionOperationalNavigationService>(out var navigationService) || navigationService == null)
            {
                DebugUtility.LogWarning<SessionActivitySandboxNavigationDebugButton>(
                    "[OBS][FrontendUI][QA] Sandbox navigation button ignored because SessionOperationalNavigationService is unavailable.");
                return false;
            }

            DebugUtility.LogVerbose<SessionActivitySandboxNavigationDebugButton>(
                $"[OBS][FrontendUI][QA] SessionActivitySandbox navigation requested routeId='{resolvedRouteId}' source='{SandboxButtonSource}' reason='{SandboxButtonReason}'.",
                DebugUtility.Colors.Info);

            navigationService.NavigateToRoute(resolvedRouteId, SandboxButtonSource, SandboxButtonReason);
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
    }
}
