using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Domain;
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
                return ResolveForInvalidRouteId(context);
            }

            if (TryResolveDefinition(routeId, routeDefinition, out var resolvedDefinition))
            {
                return ResolveFromRouteDefinition(routeId, resolvedDefinition.Value, context);
            }

            return ResolveForMissingRoute(routeId, context);
        }

        private RouteResetDecision ResolveFromRouteDefinition(SceneRouteId routeId, SceneRouteDefinition routeDefinition, SceneTransitionContext context)
        {
            if (routeDefinition.RouteKind == SceneRouteKind.Unspecified)
            {
                string detail = $"routeId='{routeId.Value}' com RouteKind='{SceneRouteKind.Unspecified}' é inválido para RouteResetPolicy.";
                if (ShouldDegradeForConfigError())
                {
                    bool fallbackReset = InferFallbackForUnknownRouteKind(context);
                    DebugUtility.LogError<SceneRouteResetPolicy>(
                        $"[ERROR][DEGRADED][Config] {detail} Fallback requiresWorldReset={fallbackReset}.");

                    return new RouteResetDecision(
                        fallbackReset,
                        decisionSource: "routeKind:Unknown",
                        reason: "UnknownRouteKind");
                }

                HandleFatalConfig(detail);
            }

            return new RouteResetDecision(
                routeDefinition.RequiresWorldReset,
                decisionSource: $"routeKind:{routeDefinition.RouteKind}",
                reason: "RoutePolicy");
        }

        private RouteResetDecision ResolveForInvalidRouteId(SceneTransitionContext context)
        {
            bool canSkip = context.TransitionProfileId.IsStartup || context.TransitionProfileId.IsFrontend;
            if (canSkip)
            {
                return new RouteResetDecision(
                    shouldReset: false,
                    decisionSource: "routeId:empty",
                    reason: "RouteIdEmptyStartupFrontend");
            }

            if (ShouldDegradeForConfigError())
            {
                DebugUtility.LogError<SceneRouteResetPolicy>(
                    "[ERROR][DEGRADED][Config] routeId vazio/inválido fora de startup/frontend. Fallback requiresWorldReset=true.");
                return new RouteResetDecision(
                    shouldReset: true,
                    decisionSource: "routeId:empty",
                    reason: "RouteIdEmptyDegradedFallback");
            }

            HandleFatalConfig("routeId vazio/inválido fora de startup/frontend.");
            return default;
        }

        private RouteResetDecision ResolveForMissingRoute(SceneRouteId routeId, SceneTransitionContext context)
        {
            string detail = $"routeId='{routeId.Value}' não foi resolvida no catálogo.";
            if (ShouldDegradeForConfigError())
            {
                bool fallbackReset = InferFallbackForUnknownRouteKind(context);
                DebugUtility.LogError<SceneRouteResetPolicy>(
                    $"[ERROR][DEGRADED][Config] {detail} Fallback requiresWorldReset={fallbackReset}.");
                return new RouteResetDecision(
                    fallbackReset,
                    decisionSource: "route:missing",
                    reason: "RouteNotFound");
            }

            HandleFatalConfig(detail);
            return default;
        }

        private bool TryResolveDefinition(SceneRouteId routeId, SceneRouteDefinition? routeDefinition, out SceneRouteDefinition resolved)
        {
            resolved = default;

            if (routeDefinition.HasValue)
            {
                resolved = routeDefinition.Value;
                return true;
            }

            if (_routeResolver == null)
            {
                return false;
            }

            return _routeResolver.TryResolve(routeId, out resolved);
        }

        private static bool InferFallbackForUnknownRouteKind(SceneTransitionContext context)
        {
            if (context.TransitionProfileId.IsGameplay)
            {
                return true;
            }

            return false;
        }

        private static bool ShouldDegradeForConfigError()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }

        private static void HandleFatalConfig(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError(typeof(SceneRouteResetPolicy), message);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            if (!Application.isEditor)
            {
                Application.Quit();
            }

            throw new InvalidOperationException(message);
        }
    }
}
