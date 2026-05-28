using System.Collections.Generic;
using _ImmersiveGames.NewScripts.CameraPresentation.Authoring;
using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.CameraPresentation.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public sealed class SessionOperationalActivityCameraAdapter : ISessionOperationalActivityCameraAdapter
    {
        private readonly IActivityCameraPreparationExecutor activityCameraExecutor;
        private readonly ActivityCameraPresentationRequirementResolver requirementResolver;
        private readonly IDependencyProvider dependencyProvider;

        private ActivityCameraReadyFact activeReadyFact;

        public SessionOperationalActivityCameraAdapter(
            IActivityCameraPreparationExecutor activityCameraExecutor,
            ActivityCameraPresentationRequirementResolver requirementResolver,
            IDependencyProvider dependencyProvider)
        {
            this.activityCameraExecutor = activityCameraExecutor;
            this.requirementResolver = requirementResolver;
            this.dependencyProvider = dependencyProvider;
        }

        public bool TryPrepareActivityCamera(
            SessionOperationalActivityCameraPrepareCommand command,
            out SessionOperationalActivityCameraPrepareResult result,
            out string reason)
        {
            if (!command.IsValid)
            {
                reason = "activity_camera_prepare_command_invalid";
                result = SessionOperationalActivityCameraPrepareResult.Failed(null, reason);
                return false;
            }

            if (command.CompletionHandoff != SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry)
            {
                reason = "not_session_activity_entry_handoff";
                result = SessionOperationalActivityCameraPrepareResult.Skipped(reason);
                LogSkipped(command, reason);
                return true;
            }

            OperationalRouteAsset route = command.Route;
            ActivityPresentationProfileAsset profile = route.ActivityPresentationProfile;

            if (profile == null)
            {
                reason = "activity_presentation_profile_missing";
                result = SessionOperationalActivityCameraPrepareResult.Skipped(reason);
                LogSkipped(command, reason);
                return true;
            }

            if (activityCameraExecutor == null)
            {
                reason = "activity_camera_preparation_executor_missing";
                result = SessionOperationalActivityCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            if (requirementResolver == null)
            {
                reason = "activity_camera_requirement_resolver_missing";
                result = SessionOperationalActivityCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            if (!TryResolveAnchorHost(command, out ActivityCameraAnchorHost anchorHost, out reason))
            {
                result = SessionOperationalActivityCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            if (!requirementResolver.TryResolve(
                    profile,
                    anchorHost,
                    out ActivityCameraRequirement requirement,
                    out reason))
            {
                if (reason == "activity_presentation_camera_disabled")
                {
                    result = SessionOperationalActivityCameraPrepareResult.Skipped(reason);
                    LogSkipped(command, reason);
                    return true;
                }

                result = SessionOperationalActivityCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            ActivityCameraBindingCommand bindingCommand = new ActivityCameraBindingCommand(
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.ActivityIdentity,
                requirement,
                command.Source,
                command.Reason);

            DebugUtility.Log(
                typeof(SessionOperationalActivityCameraAdapter),
                $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationPrepareStarted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{command.ActivityIdentity}' profileId='{profile.ProfileId}' requirementId='{requirement.RequirementId}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (!activityCameraExecutor.TryPrepare(
                    bindingCommand,
                    out ActivityCameraPreparationResult preparationResult,
                    out reason))
            {
                ActivityCameraFailureFact failureFact = preparationResult?.FailureFact;

                result = SessionOperationalActivityCameraPrepareResult.Failed(failureFact, reason);
                LogFailed(command, failureFact, reason);
                return false;
            }

            ActivityCameraReadyFact readyFact = preparationResult?.ReadyFact;

            if (readyFact == null)
            {
                reason = "activity_camera_ready_fact_missing";
                result = SessionOperationalActivityCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            activeReadyFact = readyFact;
            result = SessionOperationalActivityCameraPrepareResult.Prepared(readyFact, reason);

            DebugUtility.Log(
                typeof(SessionOperationalActivityCameraAdapter),
                $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationPrepared routeIdentity='{readyFact.RouteIdentity}' routeOperationId='{readyFact.RouteOperationId}' transitionId='{readyFact.TransitionId}' routeSequence='{readyFact.RouteSequence}' activityIdentity='{readyFact.ActivityIdentity}' requirementId='{readyFact.RequirementId}' outputCamera='{readyFact.Handle?.UnityCamera?.name}' presentationRig='{readyFact.Handle?.CameraRigInstance?.name}' source='{readyFact.Source}' reason='{readyFact.Reason}'.",
                DebugUtility.Colors.Success);

            return true;
        }

        public bool TryReleaseActivityCamera(
            SessionOperationalActivityCameraReleaseCommand command,
            out SessionOperationalActivityCameraReleaseResult result,
            out string reason)
        {
            if (!command.IsValid)
            {
                reason = "activity_camera_release_command_invalid";
                result = SessionOperationalActivityCameraReleaseResult.Failed(null, reason);
                return false;
            }

            if (activityCameraExecutor == null)
            {
                reason = "activity_camera_preparation_executor_missing";
                result = SessionOperationalActivityCameraReleaseResult.Failed(null, reason);
                return false;
            }

            if (activeReadyFact == null)
            {
                reason = "no_active_activity_camera_binding";
                result = SessionOperationalActivityCameraReleaseResult.Skipped(reason);

                DebugUtility.Log(
                    typeof(SessionOperationalActivityCameraAdapter),
                    $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationReleaseSkipped currentRouteIdentity='{command.CurrentRouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' source='{command.Source}' reason='{command.Reason}' skipReason='{reason}'.",
                    DebugUtility.Colors.Info);

                return true;
            }

            ActivityCameraReleaseCommand releaseCommand = new ActivityCameraReleaseCommand(
                activeReadyFact.RouteIdentity,
                activeReadyFact.RouteOperationId,
                activeReadyFact.TransitionId,
                activeReadyFact.RouteSequence,
                activeReadyFact.ActivityIdentity,
                command.Source,
                command.Reason);

            DebugUtility.Log(
                typeof(SessionOperationalActivityCameraAdapter),
                $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationReleaseStarted currentRouteIdentity='{command.CurrentRouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' releaseRouteIdentity='{releaseCommand.RouteIdentity}' routeOperationId='{releaseCommand.RouteOperationId}' transitionId='{releaseCommand.TransitionId}' routeSequence='{releaseCommand.RouteSequence}' activityIdentity='{releaseCommand.ActivityIdentity}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (!activityCameraExecutor.TryRelease(
                    releaseCommand,
                    out ActivityCameraReleaseResult releaseResult,
                    out reason))
            {
                ActivityCameraReleaseFailureFact failureFact = releaseResult?.FailureFact;

                result = SessionOperationalActivityCameraReleaseResult.Failed(failureFact, reason);

                DebugUtility.Log(
                    typeof(SessionOperationalActivityCameraAdapter),
                    $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationReleaseFailed currentRouteIdentity='{command.CurrentRouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' releaseRouteIdentity='{releaseCommand.RouteIdentity}' routeOperationId='{releaseCommand.RouteOperationId}' transitionId='{releaseCommand.TransitionId}' routeSequence='{releaseCommand.RouteSequence}' activityIdentity='{releaseCommand.ActivityIdentity}' source='{command.Source}' reason='{command.Reason}' failureReason='{reason}' factFailureReason='{failureFact?.FailureReason}'.",
                    DebugUtility.Colors.Error);

                return false;
            }

            ActivityCameraReleasedFact releasedFact = releaseResult?.ReleasedFact;

            activeReadyFact = null;
            result = SessionOperationalActivityCameraReleaseResult.Released(releasedFact, reason);

            DebugUtility.Log(
                typeof(SessionOperationalActivityCameraAdapter),
                $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationReleased routeIdentity='{releasedFact?.RouteIdentity}' routeOperationId='{releasedFact?.RouteOperationId}' transitionId='{releasedFact?.TransitionId}' routeSequence='{releasedFact?.RouteSequence}' activityIdentity='{releasedFact?.ActivityIdentity}' source='{releasedFact?.Source}' reason='{releasedFact?.Reason}'.",
                DebugUtility.Colors.Success);

            return true;
        }

        private bool TryResolveAnchorHost(
            SessionOperationalActivityCameraPrepareCommand command,
            out ActivityCameraAnchorHost anchorHost,
            out string reason)
        {
            anchorHost = null;

            string sceneName = ResolveSceneName(command);
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                reason = "activity_camera_scene_name_missing";
                return false;
            }

            if (dependencyProvider != null &&
                dependencyProvider.TryGetForScene<ActivityCameraAnchorHost>(
                    sceneName,
                    out ActivityCameraAnchorHost registeredHost) &&
                registeredHost != null)
            {
                anchorHost = registeredHost;
                reason = "activity_camera_anchor_host_resolved_from_scene_scope";
                return true;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                reason = "activity_camera_scene_not_loaded";
                return false;
            }

            List<ActivityCameraAnchorHost> hosts = new List<ActivityCameraAnchorHost>(4);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                ActivityCameraAnchorHost[] rootHosts =
                    root.GetComponentsInChildren<ActivityCameraAnchorHost>(true);

                if (rootHosts == null || rootHosts.Length == 0)
                {
                    continue;
                }

                for (int j = 0; j < rootHosts.Length; j++)
                {
                    if (rootHosts[j] != null)
                    {
                        hosts.Add(rootHosts[j]);
                    }
                }
            }

            if (hosts.Count == 0)
            {
                reason = "activity_camera_anchor_host_not_found";
                return false;
            }

            if (hosts.Count > 1)
            {
                reason = "activity_camera_anchor_host_multiple_found";
                return false;
            }

            anchorHost = hosts[0];
            if (!anchorHost.TryValidate(out reason))
            {
                return false;
            }

            reason = "activity_camera_anchor_host_resolved_from_scene";
            return true;
        }

        private static string ResolveSceneName(
            SessionOperationalActivityCameraPrepareCommand command)
        {
            if (!string.IsNullOrWhiteSpace(command.ActiveSceneName))
            {
                return command.ActiveSceneName;
            }

            if (command.Route != null &&
                command.Route.ActiveSceneKey != null &&
                !string.IsNullOrWhiteSpace(command.Route.ActiveSceneKey.SceneName))
            {
                return command.Route.ActiveSceneKey.SceneName.Trim();
            }

            return string.Empty;
        }

        private static void LogSkipped(
            SessionOperationalActivityCameraPrepareCommand command,
            string skipReason)
        {
            DebugUtility.Log(
                typeof(SessionOperationalActivityCameraAdapter),
                $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationSkipped routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{command.ActivityIdentity}' completionHandoff='{command.CompletionHandoff}' reason='{skipReason}' source='{command.Source}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogFailed(
            SessionOperationalActivityCameraPrepareCommand command,
            ActivityCameraFailureFact failureFact,
            string reason)
        {
            DebugUtility.Log(
                typeof(SessionOperationalActivityCameraAdapter),
                $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationFailed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{command.ActivityIdentity}' failureReason='{reason}' factFailureReason='{failureFact?.FailureReason}' source='{command.Source}' reasonDetail='{command.Reason}'.",
                DebugUtility.Colors.Error);
        }
    }
}
