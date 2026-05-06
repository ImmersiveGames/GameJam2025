using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.FrontendRuntime.UI.Bindings
{
    [DisallowMultipleComponent]
    public sealed class SessionActivitySandboxNavigationDebugButton : FrontendButtonBinderBase
    {
        private const string SandboxButtonSource = "Frontend/SandboxButton";
        private const string SandboxButtonReason = "Frontend/SandboxButton";

        [Header("Sandbox")]
        [SerializeField] private SceneRouteDefinitionAsset routeDefinition;

        protected override bool OnClickCore(string _)
        {
            if (!TryResolveRouteDefinition(out SceneRouteDefinitionAsset resolvedRouteDefinition))
            {
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<SessionOperationalNavigationService>(out var navigationService) || navigationService == null)
            {
                DebugUtility.LogWarning<SessionActivitySandboxNavigationDebugButton>(
                    "[OBS][FrontendUI][QA] Sandbox navigation button ignored because SessionOperationalNavigationService is unavailable.");
                return false;
            }

            SceneRouteId resolvedRouteId = resolvedRouteDefinition.RouteId;
            DebugUtility.LogVerbose<SessionActivitySandboxNavigationDebugButton>(
                $"[OBS][FrontendUI][QA] SessionActivitySandbox navigation requested routeId='{resolvedRouteId}' source='{SandboxButtonSource}' reason='{SandboxButtonReason}'.",
                DebugUtility.Colors.Info);

            navigationService.NavigateToRoute(resolvedRouteDefinition, SandboxButtonSource, SandboxButtonReason);
            return true;
        }

        private bool TryResolveRouteDefinition(out SceneRouteDefinitionAsset resolvedRouteDefinition)
        {
            resolvedRouteDefinition = null;
            if (routeDefinition == null)
            {
                DebugUtility.LogWarning<SessionActivitySandboxNavigationDebugButton>(
                    "[OBS][FrontendUI][QA] Sandbox navigation button rejected because routeDefinition is missing.");
                return false;
            }

            if (!routeDefinition.RouteId.IsValid)
            {
                DebugUtility.LogWarning<SessionActivitySandboxNavigationDebugButton>(
                    $"[OBS][FrontendUI][QA] Sandbox navigation button rejected because routeDefinition has invalid routeId. routeId='{routeDefinition.RouteId}'.");
                return false;
            }

            resolvedRouteDefinition = routeDefinition;
            return true;
        }
    }
}
