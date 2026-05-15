using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.FrontendRuntime.UI.Bindings
{
    [DisallowMultipleComponent]
    public class SessionOperationalRouteButtonBinder : FrontendButtonBinderBase
    {
        private const string RouteButtonSource = "SessionOperationalRouteButtonBinder";

        [Header("Route")]
        [SerializeField] private OperationalRouteAsset routeDefinition;

        [Header("Route")]
        [SerializeField] private string reasonOverride;

        protected override bool OnClickCore(string actionReason)
        {
            if (!TryResolveRouteDefinition(out OperationalRouteAsset resolvedRouteDefinition))
            {
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<SessionOperationalPipeline>(out var operationalPipeline) || operationalPipeline == null)
            {
                DebugUtility.LogError<SessionOperationalRouteButtonBinder>(
                    "[OBS][FrontendUI][RouteButton] routeDefinition accepted but SessionOperationalPipeline is unavailable.");
                return false;
            }

            string normalize = Normalize(reasonOverride);
            if (string.IsNullOrWhiteSpace(normalize))
            {
                normalize = Normalize(actionReason);
            }

            if (string.IsNullOrWhiteSpace(normalize))
            {
                normalize = resolvedRouteDefinition.RouteIdentity;
            }

            DebugUtility.Log(typeof(SessionOperationalRouteButtonBinder),
                $"[OBS][SessionOperationalPipeline][RouteButton] routeIdentity='{resolvedRouteDefinition.RouteIdentity}' source='{RouteButtonSource}' reason='{normalize}'.",
                DebugUtility.Colors.Info);

            Task routeTask = operationalPipeline.RequestOperationalRouteAsync(
                resolvedRouteDefinition,
                RouteButtonSource,
                normalize);
            routeTask.ContinueWith(completed =>
            {
                if (!completed.IsFaulted)
                {
                    return;
                }

                Exception exception = completed.Exception?.GetBaseException() ?? completed.Exception;
                DebugUtility.LogError<SessionOperationalRouteButtonBinder>(
                    $"[OBS][FrontendUI][RouteButton] routeIdentity='{resolvedRouteDefinition.RouteIdentity}' exceptionType='{exception?.GetType().Name}' exceptionMessage='{exception?.Message}'.");
            }, TaskScheduler.Default);
            return true;
        }

        private bool TryResolveRouteDefinition(out OperationalRouteAsset resolvedRouteDefinition)
        {
            resolvedRouteDefinition = null;

            if (routeDefinition == null)
            {
                DebugUtility.LogError<SessionOperationalRouteButtonBinder>(
                    "[OBS][FrontendUI][RouteButton] routeDefinition is missing.");
                return false;
            }

            if (!routeDefinition.IsValid)
            {
                DebugUtility.LogError<SessionOperationalRouteButtonBinder>(
                    $"[OBS][FrontendUI][RouteButton] routeDefinition is invalid. routeIdentity='{routeDefinition.RouteIdentity}'.");
                return false;
            }

            resolvedRouteDefinition = routeDefinition;
            return true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

