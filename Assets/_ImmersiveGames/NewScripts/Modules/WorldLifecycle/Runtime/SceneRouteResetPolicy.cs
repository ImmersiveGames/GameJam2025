using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime
{
    public sealed class SceneRouteResetPolicy : IRouteResetPolicy
    {
        private readonly ISceneRouteResolver _routeResolver;

        public SceneRouteResetPolicy(ISceneRouteResolver routeResolver = null)
        {
            _routeResolver = routeResolver;
        }

        public RouteResetDecision Resolve(SceneRouteId routeId, SceneRouteDefinition? routeDefinition, SceneTransitionContext context)
        {
            if (!routeId.IsValid)
            {
                HandleFatalConfig("routeId vazio/inválido para RouteResetPolicy.");
                return default;
            }

            if (!TryResolveDefinition(routeId, routeDefinition, out var resolvedDefinition))
            {
                HandleFatalConfig($"routeId='{routeId.Value}' não foi resolvida no catálogo para RouteResetPolicy.");
                return default;
            }

            if (resolvedDefinition.RouteKind == SceneRouteKind.Unspecified)
            {
                HandleFatalConfig($"routeId='{routeId.Value}' com RouteKind='{SceneRouteKind.Unspecified}' é inválido para RouteResetPolicy.");
                return default;
            }

            return new RouteResetDecision(
                shouldReset: resolvedDefinition.RequiresWorldReset,
                decisionSource: $"routePolicy:{resolvedDefinition.RouteKind}",
                reason: "RoutePolicy");
        }

        private bool TryResolveDefinition(SceneRouteId routeId, SceneRouteDefinition? routeDefinition, out SceneRouteDefinition resolved)
        {
            resolved = default;

            if (routeDefinition is { } direct)
            {
                resolved = direct;
                return true;
            }

            return _routeResolver != null && _routeResolver.TryResolve(routeId, out resolved);
        }

        private static void HandleFatalConfig(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError(typeof(SceneRouteResetPolicy), message);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            throw new InvalidOperationException(message);
        }
    }
}
