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
    public sealed class SessionOperationalRouteCameraAdapter : ISessionOperationalRouteCameraAdapter
    {
        private readonly IRouteCameraPreparationExecutor routeCameraExecutor;
        private readonly SurfaceCameraPresentationRequirementResolver requirementResolver;
        private readonly IDependencyProvider dependencyProvider;

        private RouteCameraReadyFact activeReadyFact;

        public SessionOperationalRouteCameraAdapter(
            IRouteCameraPreparationExecutor routeCameraExecutor,
            SurfaceCameraPresentationRequirementResolver requirementResolver,
            IDependencyProvider dependencyProvider)
        {
            this.routeCameraExecutor = routeCameraExecutor;
            this.requirementResolver = requirementResolver;
            this.dependencyProvider = dependencyProvider;
        }

        public bool TryPrepareRouteCamera(
            SessionOperationalRouteCameraPrepareCommand command,
            out SessionOperationalRouteCameraPrepareResult result,
            out string reason)
        {
            if (!command.IsValid)
            {
                reason = "route_camera_prepare_command_invalid";
                result = SessionOperationalRouteCameraPrepareResult.Failed(
                    failureFact: null,
                    reason: reason);
                return false;
            }

            OperationalRouteAsset route = command.Route;
            SurfacePresentationProfileAsset profile = route.SurfacePresentationProfile;
            ActivityPresentationProfileAsset activityProfile = route.ActivityPresentationProfile;

            if (profile == null)
            {
                if (command.CompletionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry &&
                    activityProfile != null &&
                    activityProfile.TryValidate(out _))
                {
                    reason = "activity_camera_has_priority";
                    result = SessionOperationalRouteCameraPrepareResult.Skipped(reason);
                    LogSkipped(command, reason);
                    return true;
                }

                reason = "surface_presentation_profile_missing";
                result = SessionOperationalRouteCameraPrepareResult.Skipped(reason);
                LogSkipped(command, reason);
                return true;
            }

            if (profile.RouteCameraPresentationMode == RouteCameraPresentationMode.None)
            {
                reason = "surface_camera_presentation_mode_none";
                result = SessionOperationalRouteCameraPrepareResult.Skipped(reason);
                LogSkipped(command, reason);
                return true;
            }

            if (profile.RouteCameraPresentationMode == RouteCameraPresentationMode.SkipWhenActivityHandoff &&
                command.CompletionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry)
            {
                reason = "activity_camera_has_priority";
                result = SessionOperationalRouteCameraPrepareResult.Skipped(reason);
                LogSkipped(command, reason);
                return true;
            }

            if (routeCameraExecutor == null)
            {
                reason = "route_camera_preparation_executor_missing";
                result = SessionOperationalRouteCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            if (requirementResolver == null)
            {
                reason = "surface_camera_requirement_resolver_missing";
                result = SessionOperationalRouteCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            if (!TryResolveAnchorHost(command, out SurfaceCameraAnchorHost anchorHost, out reason))
            {
                result = SessionOperationalRouteCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            if (!requirementResolver.TryResolve(profile, anchorHost, out RouteCameraPresentationRequirement requirement, out reason))
            {
                result = SessionOperationalRouteCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            RouteCameraPresentationCommand presentationCommand = new RouteCameraPresentationCommand(
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.SurfaceKind,
                requirement,
                command.Source,
                command.Reason);

            DebugUtility.Log(typeof(SessionOperationalRouteCameraAdapter),
                $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationPrepareStarted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' surfaceKind='{command.SurfaceKind}' profileId='{profile.ProfileId}' requirementId='{requirement.RequirementId}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (!routeCameraExecutor.TryPrepare(presentationCommand, out RouteCameraPresentationResult presentationResult, out reason))
            {
                RouteCameraFailureFact failureFact = presentationResult != null ? presentationResult.FailureFact : null;
                result = SessionOperationalRouteCameraPrepareResult.Failed(failureFact, reason);
                LogFailed(command, failureFact, reason);
                return false;
            }

            RouteCameraReadyFact readyFact = presentationResult.ReadyFact;
            if (readyFact == null)
            {
                reason = "route_camera_ready_fact_missing";
                result = SessionOperationalRouteCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            activeReadyFact = readyFact;
            result = SessionOperationalRouteCameraPrepareResult.Prepared(readyFact, reason);

            DebugUtility.Log(typeof(SessionOperationalRouteCameraAdapter),
                $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationPrepared routeIdentity='{readyFact.RouteIdentity}' routeOperationId='{readyFact.RouteOperationId}' transitionId='{readyFact.TransitionId}' routeSequence='{readyFact.RouteSequence}' surfaceKind='{readyFact.SurfaceKind}' requirementId='{readyFact.RequirementId}' outputCamera='{readyFact.OutputCameraName}' presentationRig='{readyFact.PresentationRigName}' source='{readyFact.Source}' reason='{readyFact.Reason}'.",
                DebugUtility.Colors.Success);

            return true;
        }

        public bool TryReleaseRouteCamera(
            SessionOperationalRouteCameraReleaseCommand command,
            out SessionOperationalRouteCameraReleaseResult result,
            out string reason)
        {
            if (!command.IsValid)
            {
                reason = "route_camera_release_command_invalid";
                result = SessionOperationalRouteCameraReleaseResult.Failed(null, reason);
                return false;
            }

            if (routeCameraExecutor == null)
            {
                reason = "route_camera_preparation_executor_missing";
                result = SessionOperationalRouteCameraReleaseResult.Failed(null, reason);
                return false;
            }

            if (activeReadyFact == null)
            {
                reason = "no_active_route_camera_binding";
                result = SessionOperationalRouteCameraReleaseResult.Skipped(reason);
                DebugUtility.Log(typeof(SessionOperationalRouteCameraAdapter),
                    $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationReleaseSkipped currentRouteIdentity='{command.CurrentRouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' source='{command.Source}' reason='{command.Reason}' skipReason='{reason}'.",
                    DebugUtility.Colors.Info);
                return true;
            }

            RouteCameraReleaseCommand releaseCommand = new RouteCameraReleaseCommand(
                activeReadyFact.RouteIdentity,
                activeReadyFact.RouteOperationId,
                activeReadyFact.TransitionId,
                activeReadyFact.RouteSequence,
                activeReadyFact.SurfaceKind,
                activeReadyFact.RequirementId,
                command.Source,
                command.Reason);

            DebugUtility.Log(typeof(SessionOperationalRouteCameraAdapter),
                $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationReleaseStarted currentRouteIdentity='{command.CurrentRouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' releaseRouteIdentity='{releaseCommand.RouteIdentity}' routeOperationId='{releaseCommand.RouteOperationId}' transitionId='{releaseCommand.TransitionId}' routeSequence='{releaseCommand.RouteSequence}' surfaceKind='{releaseCommand.SurfaceKind}' requirementId='{releaseCommand.RequirementId}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (!routeCameraExecutor.TryRelease(releaseCommand, out RouteCameraReleaseResult releaseResult, out reason))
            {
                RouteCameraReleaseFailureFact failureFact = releaseResult != null ? releaseResult.FailureFact : null;
                result = SessionOperationalRouteCameraReleaseResult.Failed(failureFact, reason);

                DebugUtility.Log(typeof(SessionOperationalRouteCameraAdapter),
                    $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationReleaseFailed currentRouteIdentity='{command.CurrentRouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' releaseRouteIdentity='{releaseCommand.RouteIdentity}' routeOperationId='{releaseCommand.RouteOperationId}' transitionId='{releaseCommand.TransitionId}' routeSequence='{releaseCommand.RouteSequence}' surfaceKind='{releaseCommand.SurfaceKind}' requirementId='{releaseCommand.RequirementId}' source='{command.Source}' reason='{command.Reason}' failureReason='{reason}'.",
                    DebugUtility.Colors.Error);

                return false;
            }

            RouteCameraReleasedFact releasedFact = releaseResult.ReleasedFact;
            activeReadyFact = null;

            result = SessionOperationalRouteCameraReleaseResult.Released(releasedFact, reason);

            DebugUtility.Log(typeof(SessionOperationalRouteCameraAdapter),
                $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationReleased routeIdentity='{releasedFact?.RouteIdentity}' routeOperationId='{releasedFact?.RouteOperationId}' transitionId='{releasedFact?.TransitionId}' routeSequence='{releasedFact?.RouteSequence}' surfaceKind='{releasedFact?.SurfaceKind}' requirementId='{releasedFact?.RequirementId}' source='{releasedFact?.Source}' reason='{releasedFact?.Reason}'.",
                DebugUtility.Colors.Success);

            return true;
        }

        private bool TryResolveAnchorHost(
            SessionOperationalRouteCameraPrepareCommand command,
            out SurfaceCameraAnchorHost anchorHost,
            out string reason)
        {
            anchorHost = null;

            string sceneName = ResolveSceneName(command);
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                reason = "surface_camera_scene_name_missing";
                return false;
            }

            if (dependencyProvider != null &&
                dependencyProvider.TryGetForScene<SurfaceCameraAnchorHost>(sceneName, out SurfaceCameraAnchorHost registeredHost) &&
                registeredHost != null)
            {
                anchorHost = registeredHost;
                reason = "surface_camera_anchor_host_resolved_from_scene_scope";
                return true;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                reason = "surface_camera_scene_not_loaded";
                return false;
            }

            List<SurfaceCameraAnchorHost> hosts = new List<SurfaceCameraAnchorHost>(4);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                SurfaceCameraAnchorHost[] rootHosts = root.GetComponentsInChildren<SurfaceCameraAnchorHost>(true);
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
                reason = "surface_camera_anchor_host_not_found";
                return false;
            }

            if (hosts.Count > 1)
            {
                reason = "surface_camera_anchor_host_multiple_found";
                return false;
            }

            anchorHost = hosts[0];
            if (!anchorHost.TryValidate(out reason))
            {
                return false;
            }

            reason = "surface_camera_anchor_host_resolved_from_scene";
            return true;
        }

        private static string ResolveSceneName(SessionOperationalRouteCameraPrepareCommand command)
        {
            if (!string.IsNullOrWhiteSpace(command.ActiveSceneName))
            {
                return command.ActiveSceneName;
            }

            if (command.Route != null && command.Route.ActiveSceneKey != null && !string.IsNullOrWhiteSpace(command.Route.ActiveSceneKey.SceneName))
            {
                return command.Route.ActiveSceneKey.SceneName.Trim();
            }

            return string.Empty;
        }

        private static void LogSkipped(SessionOperationalRouteCameraPrepareCommand command, string skipReason)
        {
            DebugUtility.Log(typeof(SessionOperationalRouteCameraAdapter),
                $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationSkipped routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' surfaceKind='{command.SurfaceKind}' completionHandoff='{command.CompletionHandoff}' reason='{skipReason}' source='{command.Source}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogFailed(
            SessionOperationalRouteCameraPrepareCommand command,
            RouteCameraFailureFact failureFact,
            string reason)
        {
            DebugUtility.Log(typeof(SessionOperationalRouteCameraAdapter),
                $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationFailed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' surfaceKind='{command.SurfaceKind}' failureReason='{reason}' factFailureReason='{failureFact?.FailureReason}' source='{command.Source}' reasonDetail='{command.Reason}'.",
                DebugUtility.Colors.Error);
        }
    }
}
