using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline;
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

            string normalizedReason = ResolveReasonOrFallback(reasonOverride, actionReason, resolvedRouteDefinition);
            if (!TryValidateRouteExitSafety(out string blockedReason, out string safetyDetail))
            {
                DebugUtility.LogWarning<SessionOperationalRouteButtonBinder>(
                    $"[OBS][FrontendUI][RouteButton] RouteButtonRejectedNotRouteExitSafe routeIdentity='{resolvedRouteDefinition.RouteIdentity}' source='{RouteButtonSource}' reason='{normalizedReason}' blockedReason='{blockedReason}' detail='{safetyDetail}'.");
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<SessionOperationalPipeline>(out var operationalPipeline) || operationalPipeline == null)
            {
                DebugUtility.LogError<SessionOperationalRouteButtonBinder>(
                    "[OBS][FrontendUI][RouteButton] routeDefinition accepted but SessionOperationalPipeline is unavailable.");
                return false;
            }

            DebugUtility.Log(typeof(SessionOperationalRouteButtonBinder),
                $"[OBS][SessionOperationalPipeline][RouteButton] routeIdentity='{resolvedRouteDefinition.RouteIdentity}' source='{RouteButtonSource}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            Task routeTask = operationalPipeline.RequestOperationalRouteAsync(
                resolvedRouteDefinition,
                RouteButtonSource,
                normalizedReason);
            routeTask.ContinueWith(completed =>
            {
                if (!completed.IsFaulted && !completed.IsCanceled)
                {
                    return;
                }

                if (button != null)
                {
                    button.interactable = true;
                }

                Exception exception = completed.Exception?.GetBaseException() ?? completed.Exception;
                DebugUtility.LogError<SessionOperationalRouteButtonBinder>(
                    $"[OBS][FrontendUI][RouteButton] routeIdentity='{resolvedRouteDefinition.RouteIdentity}' exceptionType='{exception?.GetType().Name}' exceptionMessage='{exception?.Message}'.");
                DebugUtility.LogWarning<SessionOperationalRouteButtonBinder>(
                    $"[OBS][FrontendUI][RouteButton] RouteButtonReenabledAfterAsyncFailure routeIdentity='{resolvedRouteDefinition.RouteIdentity}' source='{RouteButtonSource}' reason='{normalizedReason}'.");
            }, TaskScheduler.Default);

            return true;
        }

        private bool TryValidateRouteExitSafety(out string blockedReason, out string detail)
        {
            blockedReason = string.Empty;
            detail = string.Empty;

            if (!DependencyManager.Provider.TryGetGlobal<SessionActivityPipeline>(out var activityPipeline) || activityPipeline == null || activityPipeline.State == null)
            {
                return true;
            }

            SessionActivityRuntimeState state = activityPipeline.State;
            string pendingOperation = state.CurrentPendingOperation.IsValid
                ? state.CurrentPendingOperation.ToString()
                : "<none>";
            string activityId = state.CurrentDefinition.IsValid ? state.CurrentDefinition.ActivityId : "<none>";
            int entrySequence = state.CurrentEntrySequence;
            SessionActivityStage stage = state.CurrentStage;

            if (!state.HasStarted)
            {
                return true;
            }

            if (state.CurrentPendingOperation.IsValid)
            {
                blockedReason = "pending_operation_active";
                detail = $"stage='{stage}' activityId='{activityId}' entrySequence='{entrySequence}' pendingOperation='{pendingOperation}'.";
                return false;
            }

            if (stage == SessionActivityStage.ClosedForRouteExit)
            {
                blockedReason = "route_exit_already_closed";
                detail = $"stage='{stage}' activityId='{activityId}' entrySequence='{entrySequence}' pendingOperation='{pendingOperation}'.";
                return false;
            }

            if (state.HasCompleted)
            {
                return true;
            }

            if (IsRouteExitTransitStage(stage))
            {
                blockedReason = "route_exit_in_progress";
                detail = $"stage='{stage}' activityId='{activityId}' entrySequence='{entrySequence}' pendingOperation='{pendingOperation}'.";
                return false;
            }

            if (stage == SessionActivityStage.ActivityRunning ||
                stage == SessionActivityStage.DeactivationWindowReady)
            {
                return true;
            }

            blockedReason = "session_activity_not_route_exit_safe";
            detail = $"stage='{stage}' activityId='{activityId}' entrySequence='{entrySequence}' pendingOperation='{pendingOperation}'.";
            return false;
        }

        private static bool IsRouteExitTransitStage(SessionActivityStage stage)
        {
            return stage == SessionActivityStage.ActivityCompletionRequested ||
                   stage == SessionActivityStage.ActivityCompleting ||
                   stage == SessionActivityStage.PlayerActorParticipationExitStageStarted ||
                   stage == SessionActivityStage.PlayerActorParticipationExitStageCompleted ||
                   stage == SessionActivityStage.DeactivationWindowStarted ||
                   stage == SessionActivityStage.DeactivationWindowSceneLoading ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneLoadStarted ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneLoaded ||
                   stage == SessionActivityStage.DeactivationWindowCompleted ||
                   stage == SessionActivityStage.DeactivationWindowSceneUnloading ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloadStarted ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloaded ||
                   stage == SessionActivityStage.DeactivationWindowSkippedNoContent ||
                   stage == SessionActivityStage.ActivityContentReleaseStarted ||
                   stage == SessionActivityStage.ActivityContentRetentionPlanResolved ||
                   stage == SessionActivityStage.ActivityContentSceneUnloading;
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
