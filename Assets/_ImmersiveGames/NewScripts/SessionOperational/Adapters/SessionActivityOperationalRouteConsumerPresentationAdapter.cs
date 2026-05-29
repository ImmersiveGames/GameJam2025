using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public sealed class SessionActivityOperationalRouteConsumerPresentationAdapter : IOperationalRouteConsumerPresentationPort
    {
        private readonly Func<ISessionOperationalActivityCameraAdapter> activityCameraAdapterResolver;

        public SessionActivityOperationalRouteConsumerPresentationAdapter(
            Func<ISessionOperationalActivityCameraAdapter> activityCameraAdapterResolver)
        {
            this.activityCameraAdapterResolver = activityCameraAdapterResolver ?? throw new ArgumentNullException(nameof(activityCameraAdapterResolver));
        }

        public bool TryReleasePrevious(
            OperationalRouteConsumerPresentationRequest request,
            out OperationalRouteConsumerPresentationResult result,
            out string reason)
        {
            result = default;
            reason = string.Empty;

            if (!request.IsValid)
            {
                reason = "consumer_presentation_release_request_invalid";
                result = OperationalRouteConsumerPresentationResult.Failed(
                    request.ConsumerIdentity,
                    request.RouteOperationId,
                    reason,
                    "consumer_presentation_release_request_invalid");
                return false;
            }

            var activityCameraAdapter = ResolveActivityCameraAdapterOrFail();

            DebugUtility.Log(
                typeof(SessionActivityOperationalRouteConsumerPresentationAdapter),
                $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationReleaseStarted routeIdentity='{request.RouteIdentity}' routeOperationId='{request.RouteOperationId}' transitionId='{request.TransitionId}' routeSequence='{request.RouteSequence}' previousRouteIdentity='{request.PreviousRouteIdentity}' consumerIdentity='{request.ConsumerIdentity}' source='{request.Source}' reason='{request.Reason}'.",
                DebugUtility.Colors.Info);

            SessionOperationalActivityCameraReleaseCommand releaseCommand = new(
                request.RouteIdentity,
                request.PreviousRouteIdentity,
                request.Source,
                request.Reason);

            if (!activityCameraAdapter.TryReleaseActivityCamera(
                    releaseCommand,
                    out var releaseResult,
                    out string releaseReason))
            {
                reason = string.IsNullOrWhiteSpace(releaseReason)
                    ? "consumer_presentation_release_failed"
                    : releaseReason;
                result = OperationalRouteConsumerPresentationResult.Failed(
                    request.ConsumerIdentity,
                    request.RouteOperationId,
                    reason,
                    "consumer_presentation_release_failed");

                DebugUtility.LogError(
                    typeof(SessionActivityOperationalRouteConsumerPresentationAdapter),
                    $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationReleaseFailed routeIdentity='{request.RouteIdentity}' routeOperationId='{request.RouteOperationId}' transitionId='{request.TransitionId}' routeSequence='{request.RouteSequence}' previousRouteIdentity='{request.PreviousRouteIdentity}' consumerIdentity='{request.ConsumerIdentity}' reason='{reason}' source='{request.Source}' reasonDetail='{request.Reason}'.");
                return false;
            }

            if (releaseResult.IsSkipped)
            {
                reason = string.IsNullOrWhiteSpace(releaseResult.SkipReason)
                    ? "consumer_presentation_release_not_required"
                    : releaseResult.SkipReason;
                result = OperationalRouteConsumerPresentationResult.Skipped(
                    request.ConsumerIdentity,
                    request.RouteOperationId,
                    reason,
                    "consumer_presentation_release_skipped");

                DebugUtility.Log(
                    typeof(SessionActivityOperationalRouteConsumerPresentationAdapter),
                    $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationReleaseSkipped routeIdentity='{request.RouteIdentity}' routeOperationId='{request.RouteOperationId}' transitionId='{request.TransitionId}' routeSequence='{request.RouteSequence}' previousRouteIdentity='{request.PreviousRouteIdentity}' consumerIdentity='{request.ConsumerIdentity}' skipReason='{reason}' source='{request.Source}' reasonDetail='{request.Reason}'.",
                    DebugUtility.Colors.Info);
                return true;
            }

            reason = string.IsNullOrWhiteSpace(releaseResult.Reason)
                ? "consumer_presentation_release_completed"
                : releaseResult.Reason;
            result = OperationalRouteConsumerPresentationResult.Completed(
                request.ConsumerIdentity,
                request.RouteOperationId,
                reason,
                "consumer_presentation_release_completed");

            DebugUtility.Log(
                typeof(SessionActivityOperationalRouteConsumerPresentationAdapter),
                $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationReleaseCompleted routeIdentity='{request.RouteIdentity}' routeOperationId='{request.RouteOperationId}' transitionId='{request.TransitionId}' routeSequence='{request.RouteSequence}' previousRouteIdentity='{request.PreviousRouteIdentity}' consumerIdentity='{request.ConsumerIdentity}' resultKind='{result.Kind}' resultReason='{result.Reason}' source='{request.Source}' reasonDetail='{request.Reason}'.",
                DebugUtility.Colors.Success);
            return true;
        }

        public bool TryPrepare(
            OperationalRouteConsumerPresentationRequest request,
            out OperationalRouteConsumerPresentationResult result,
            out string reason)
        {
            result = default;
            reason = string.Empty;

            if (!request.IsValid)
            {
                reason = "consumer_presentation_prepare_request_invalid";
                result = OperationalRouteConsumerPresentationResult.Failed(
                    request.ConsumerIdentity,
                    request.RouteOperationId,
                    reason,
                    "consumer_presentation_prepare_request_invalid");
                return false;
            }

            if (request.CompletionHandoff != SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry)
            {
                reason = "consumer_presentation_not_required";
                result = OperationalRouteConsumerPresentationResult.Skipped(
                    request.ConsumerIdentity,
                    request.RouteOperationId,
                    reason,
                    "consumer_presentation_skipped");

                DebugUtility.Log(
                    typeof(SessionActivityOperationalRouteConsumerPresentationAdapter),
                    $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationPrepareSkipped routeIdentity='{request.RouteIdentity}' routeOperationId='{request.RouteOperationId}' transitionId='{request.TransitionId}' routeSequence='{request.RouteSequence}' consumerIdentity='{request.ConsumerIdentity}' skipReason='{reason}' source='{request.Source}' reasonDetail='{request.Reason}'.",
                    DebugUtility.Colors.Info);
                return true;
            }

            var activityCameraAdapter = ResolveActivityCameraAdapterOrFail();

            DebugUtility.Log(
                typeof(SessionActivityOperationalRouteConsumerPresentationAdapter),
                $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationPrepareStarted routeIdentity='{request.RouteIdentity}' routeOperationId='{request.RouteOperationId}' transitionId='{request.TransitionId}' routeSequence='{request.RouteSequence}' consumerIdentity='{request.ConsumerIdentity}' source='{request.Source}' reason='{request.Reason}'.",
                DebugUtility.Colors.Info);

            SessionOperationalActivityCameraPrepareCommand prepareCommand = new(
                request.RouteIdentity,
                request.RouteOperationId,
                request.TransitionId,
                request.RouteSequence,
                request.ConsumerIdentity,
                request.CompletionHandoff,
                request.ActiveSceneName,
                request.ConsumerPresentationProfile,
                request.Source,
                request.Reason);

            if (!activityCameraAdapter.TryPrepareActivityCamera(
                    prepareCommand,
                    out var prepareResult,
                    out string prepareReason))
            {
                bool required = request.ConsumerPresentationProfile != null && request.ConsumerPresentationProfile.Required;
                reason = string.IsNullOrWhiteSpace(prepareReason)
                    ? "consumer_presentation_prepare_failed"
                    : prepareReason;
                result = OperationalRouteConsumerPresentationResult.Failed(
                    request.ConsumerIdentity,
                    request.RouteOperationId,
                    reason,
                    required ? "consumer_presentation_required_prepare_failed" : "consumer_presentation_prepare_failed");

                DebugUtility.LogError(
                    typeof(SessionActivityOperationalRouteConsumerPresentationAdapter),
                    $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationPrepareFailed routeIdentity='{request.RouteIdentity}' routeOperationId='{request.RouteOperationId}' transitionId='{request.TransitionId}' routeSequence='{request.RouteSequence}' consumerIdentity='{request.ConsumerIdentity}' required='{required}' reason='{reason}' source='{request.Source}' reasonDetail='{request.Reason}'.");
                return false;
            }

            if (prepareResult.IsSkipped)
            {
                reason = string.IsNullOrWhiteSpace(prepareResult.SkipReason)
                    ? "consumer_presentation_prepare_skipped"
                    : prepareResult.SkipReason;
                result = OperationalRouteConsumerPresentationResult.Skipped(
                    request.ConsumerIdentity,
                    request.RouteOperationId,
                    reason,
                    "consumer_presentation_prepare_skipped");

                DebugUtility.Log(
                    typeof(SessionActivityOperationalRouteConsumerPresentationAdapter),
                    $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationPrepareSkipped routeIdentity='{request.RouteIdentity}' routeOperationId='{request.RouteOperationId}' transitionId='{request.TransitionId}' routeSequence='{request.RouteSequence}' consumerIdentity='{request.ConsumerIdentity}' skipReason='{reason}' source='{request.Source}' reasonDetail='{request.Reason}'.",
                    DebugUtility.Colors.Info);
                return true;
            }

            if (prepareResult.IsPrepared)
            {
                reason = string.IsNullOrWhiteSpace(prepareResult.Reason)
                    ? "consumer_presentation_prepare_completed"
                    : prepareResult.Reason;
                result = OperationalRouteConsumerPresentationResult.Completed(
                    request.ConsumerIdentity,
                    request.RouteOperationId,
                    reason,
                    "consumer_presentation_prepare_completed");

                DebugUtility.Log(
                    typeof(SessionActivityOperationalRouteConsumerPresentationAdapter),
                    $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationPrepareCompleted routeIdentity='{request.RouteIdentity}' routeOperationId='{request.RouteOperationId}' transitionId='{request.TransitionId}' routeSequence='{request.RouteSequence}' consumerIdentity='{request.ConsumerIdentity}' resultKind='{result.Kind}' resultReason='{result.Reason}' source='{request.Source}' reasonDetail='{request.Reason}'.",
                    DebugUtility.Colors.Success);
                return true;
            }

            reason = "consumer_presentation_prepare_failed";
            result = OperationalRouteConsumerPresentationResult.Failed(
                request.ConsumerIdentity,
                request.RouteOperationId,
                reason,
                "consumer_presentation_prepare_failed");
            return false;
        }

        private ISessionOperationalActivityCameraAdapter ResolveActivityCameraAdapterOrFail()
        {
            var adapter = activityCameraAdapterResolver();
            if (adapter == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline][ConsumerPresentation] ISessionOperationalActivityCameraAdapter obrigatorio ausente para compor route consumer presentation adapter.");
            }

            return adapter;
        }
    }
}
