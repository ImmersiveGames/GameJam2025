using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalRouteCameraPresentationResultKind
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2,
    }

    public readonly struct OperationalRouteCameraPresentationResult
    {
        public OperationalRouteCameraPresentationResult(
            OperationalRouteCameraPresentationResultKind kind,
            string reason)
        {
            Kind = kind;
            Reason = Normalize(reason);
        }

        public OperationalRouteCameraPresentationResultKind Kind { get; }
        public string Reason { get; }
        public bool IsCompleted => Kind == OperationalRouteCameraPresentationResultKind.Completed;

        public static OperationalRouteCameraPresentationResult Completed(string reason)
        {
            return new OperationalRouteCameraPresentationResult(
                OperationalRouteCameraPresentationResultKind.Completed,
                reason);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct OperationalRouteCameraPresentationCommand
    {
        public OperationalRouteCameraPresentationCommand(
            ISessionOperationalRouteCameraAdapter routeCameraAdapter,
            SessionOperationalRouteCommand routeCommand,
            string previousRouteIdentity,
            string activeSceneName,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            RouteCameraAdapter = routeCameraAdapter;
            RouteCommand = routeCommand;
            PreviousRouteIdentity = Normalize(previousRouteIdentity);
            ActiveSceneName = Normalize(activeSceneName);
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ISessionOperationalRouteCameraAdapter RouteCameraAdapter { get; }
        public SessionOperationalRouteCommand RouteCommand { get; }
        public string PreviousRouteIdentity { get; }
        public string ActiveSceneName { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            RouteCameraAdapter != null &&
            RouteCommand.IsValid &&
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalRouteCameraPresentationStage
    {
        public OperationalRouteCameraPresentationResult Execute(OperationalRouteCameraPresentationCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("OperationalRouteCameraPresentationCommand is invalid.");
            }

            LogStageStarted(command);
            ReleasePreviousRouteCameraOrFail(command);
            PrepareRouteCameraOrFail(command);

            return OperationalRouteCameraPresentationResult.Completed("completed");
        }

        private static void ReleasePreviousRouteCameraOrFail(OperationalRouteCameraPresentationCommand command)
        {
            SessionOperationalRouteCameraReleaseCommand releaseCommand = new(
                command.RouteIdentity,
                command.PreviousRouteIdentity,
                command.Source,
                command.Reason);

            DebugUtility.Log(typeof(OperationalRouteCameraPresentationStage),
                $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationReleasePreviousStarted currentRouteIdentity='{command.RouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (!command.RouteCameraAdapter.TryReleaseRouteCamera(releaseCommand, out SessionOperationalRouteCameraReleaseResult releaseResult, out string releaseReason))
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][RouteCamera] release_previous_failed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' reason='{Normalize(releaseReason)}'.");
            }

            if (releaseResult.IsSkipped)
            {
                DebugUtility.Log(typeof(OperationalRouteCameraPresentationStage),
                    $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationReleasePreviousSkipped currentRouteIdentity='{command.RouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' skipReason='{releaseResult.SkipReason}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (releaseResult.IsReleased)
            {
                DebugUtility.Log(typeof(OperationalRouteCameraPresentationStage),
                    $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationReleasePreviousCompleted currentRouteIdentity='{command.RouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' releaseRouteIdentity='{releaseResult.ReleasedFact.RouteIdentity}' releaseRequirementId='{releaseResult.ReleasedFact.RequirementId}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][RouteCamera] release_previous_failed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' reason='{Normalize(releaseReason)}'.");
        }

        private static void PrepareRouteCameraOrFail(OperationalRouteCameraPresentationCommand command)
        {
            SessionOperationalRouteCommand routeCommand = command.RouteCommand;
            SessionOperationalRouteCameraPrepareCommand prepareCommand = new(
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                routeCommand.SurfaceKind.ToString(),
                routeCommand.CompletionHandoff,
                command.ActiveSceneName,
                routeCommand.SurfacePresentationProfile,
                routeCommand.ActivityPresentationProfile,
                command.Source,
                command.Reason);

            if (!command.RouteCameraAdapter.TryPrepareRouteCamera(prepareCommand, out SessionOperationalRouteCameraPrepareResult prepareResult, out string prepareReason))
            {
                bool required = routeCommand.SurfacePresentationProfile != null && routeCommand.SurfacePresentationProfile.Required;
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][RouteCamera] prepare_failed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{routeCommand.SurfaceKind}' required='{required}' reason='{Normalize(prepareReason)}'.");
            }

            if (prepareResult.IsSkipped)
            {
                DebugUtility.Log(typeof(OperationalRouteCameraPresentationStage),
                    $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationSkipped routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{routeCommand.SurfaceKind}' completionHandoff='{routeCommand.CompletionHandoff}' reason='{prepareResult.SkipReason}' source='{command.Source}' reasonDetail='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (prepareResult.IsPrepared)
            {
                DebugUtility.Log(typeof(OperationalRouteCameraPresentationStage),
                    $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationStagePrepared routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{routeCommand.SurfaceKind}' requirementId='{prepareResult.ReadyFact.RequirementId}' outputCamera='{prepareResult.ReadyFact.OutputCameraName}' presentationRig='{prepareResult.ReadyFact.PresentationRigName}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            string failureReason = prepareResult.FailureFact != null
                ? prepareResult.FailureFact.FailureReason
                : prepareReason;
            bool profileRequired = routeCommand.SurfacePresentationProfile != null && routeCommand.SurfacePresentationProfile.Required;

            DebugUtility.LogError(typeof(OperationalRouteCameraPresentationStage),
                $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationFailed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{routeCommand.SurfaceKind}' profileRequired='{profileRequired}' reason='{Normalize(failureReason)}' source='{command.Source}' reasonDetail='{command.Reason}'.");

            throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][RouteCamera] prepare_failed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{routeCommand.SurfaceKind}' required='{profileRequired}' reason='{Normalize(failureReason)}'.");
        }

        private static void LogStageStarted(OperationalRouteCameraPresentationCommand command)
        {
            DebugUtility.Log(typeof(OperationalRouteCameraPresentationStage),
                $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationStageStarted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activeScene='{Normalize(command.ActiveSceneName)}' operationalSurfaceKind='{command.RouteCommand.SurfaceKind}' completionHandoff='{command.RouteCommand.CompletionHandoff}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
