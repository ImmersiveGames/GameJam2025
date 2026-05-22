using System;
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
        private SessionOperationalPipeline _subscribedPipeline;
        private bool _awaitingAcceptedRouteCompletion;

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

            string normalizedReason = ResolveReasonOrFallback(reasonOverride, actionReason, resolvedRouteDefinition);
            if (!DependencyManager.Provider.TryGetGlobal<SessionOperationalPipeline>(out var operationalPipeline) || operationalPipeline == null)
            {
                DebugUtility.LogError<SessionOperationalRouteButtonBinder>(
                    "[OBS][FrontendUI][RouteButton] routeDefinition accepted but SessionOperationalPipeline is unavailable.");
                return false;
            }

            DebugUtility.Log(typeof(SessionOperationalRouteButtonBinder),
                $"[OBS][SessionOperationalPipeline][RouteButton] routeIdentity='{resolvedRouteDefinition.RouteIdentity}' source='{RouteButtonSource}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            EnsurePipelineSubscription(operationalPipeline);
            RouteRequestSubmissionResult submission = operationalPipeline.SubmitRouteRequest(
                resolvedRouteDefinition,
                RouteButtonSource,
                normalizedReason);

            if (submission.Kind == RouteRequestSubmissionKind.Accepted)
            {
                _awaitingAcceptedRouteCompletion = true;
                return true;
            }

            if (submission.Kind == RouteRequestSubmissionKind.RejectedByPolicy)
            {
                DebugUtility.Log(typeof(SessionOperationalRouteButtonBinder),
                    $"[OBS][FrontendUI][RouteButton] RouteRequestRejectedByPolicy routeIdentity='{resolvedRouteDefinition.RouteIdentity}' source='{RouteButtonSource}' reason='{normalizedReason}' blockedReason='{submission.Reason}' detail='{submission.Detail}'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            if (submission.Kind == RouteRequestSubmissionKind.IgnoredAlreadyInFlight)
            {
                DebugUtility.LogWarning<SessionOperationalRouteButtonBinder>(
                    $"[OBS][FrontendUI][RouteButton] RouteRequestIgnoredAlreadyInFlight routeIdentity='{resolvedRouteDefinition.RouteIdentity}' source='{RouteButtonSource}' reason='{normalizedReason}' detail='{submission.Detail}'.");
                return false;
            }

            DebugUtility.LogError<SessionOperationalRouteButtonBinder>(
                $"[OBS][FrontendUI][RouteButton] RouteRequestFailedInvalidConfig routeIdentity='{resolvedRouteDefinition.RouteIdentity}' source='{RouteButtonSource}' reason='{normalizedReason}' detail='{submission.Detail}'.");
            return false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _awaitingAcceptedRouteCompletion = false;
            if (_subscribedPipeline != null)
            {
                _subscribedPipeline.RouteOperationCompleted -= OnRouteOperationCompleted;
                _subscribedPipeline = null;
            }
        }

        private void EnsurePipelineSubscription(SessionOperationalPipeline operationalPipeline)
        {
            if (_subscribedPipeline == operationalPipeline)
            {
                return;
            }

            if (_subscribedPipeline != null)
            {
                _subscribedPipeline.RouteOperationCompleted -= OnRouteOperationCompleted;
            }

            _subscribedPipeline = operationalPipeline;
            _subscribedPipeline.RouteOperationCompleted += OnRouteOperationCompleted;
        }

        private void OnRouteOperationCompleted(RouteOperationCompletionSignal signal)
        {
            if (!_awaitingAcceptedRouteCompletion || routeDefinition == null)
            {
                return;
            }

            if (!string.Equals(signal.RouteIdentity, routeDefinition.RouteIdentity, StringComparison.Ordinal))
            {
                return;
            }

            _awaitingAcceptedRouteCompletion = false;
            if (button != null)
            {
                button.interactable = true;
            }
        }

        private static string ResolveReasonOrFallback(string configuredReason, string actionReason, OperationalRouteAsset resolvedRouteDefinition)
        {
            string normalized = Normalize(configuredReason);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = Normalize(actionReason);
            }

            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = resolvedRouteDefinition.RouteIdentity;
            }

            return normalized;
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
