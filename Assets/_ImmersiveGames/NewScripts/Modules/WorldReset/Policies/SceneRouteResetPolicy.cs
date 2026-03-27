using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Policies
{
    public sealed partial class SceneRouteResetPolicy : IRouteResetPolicy
    {
        public RouteResetDecision Resolve(SceneRouteId routeId, SceneRouteDefinition? routeDefinition, SceneTransitionContext context)
        {
            _ = context;

            if (!routeId.IsValid)
            {
                HandleFatalConfig("routeId vazio/inválido para RouteResetPolicy.");
                return default;
            }

            if (routeDefinition == null)
            {
                HandleFatalConfig($"routeId='{routeId.Value}' sem SceneRouteDefinition resolvida para RouteResetPolicy.");
                return default;
            }

            SceneRouteDefinition resolvedDefinition = routeDefinition.Value;
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

        private static void HandleFatalConfig(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError(typeof(SceneRouteResetPolicy), message);

#if UNITY_EDITOR
            StopPlayModeInEditor();
#else
            Application.Quit();
#endif
            throw new InvalidOperationException(message);
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static partial void StopPlayModeInEditor();
#endif

    }
}

