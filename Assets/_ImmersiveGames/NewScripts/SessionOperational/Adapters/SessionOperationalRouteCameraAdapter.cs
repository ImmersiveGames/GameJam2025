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

            var profile = command.SurfacePresentationProfile;

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

            if (!TryResolveAnchorHost(command, out var anchorHost, out reason))
            {
                result = SessionOperationalRouteCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            if (!requirementResolver.TryResolve(profile, anchorHost, out var requirement, out reason))
            {
                result = SessionOperationalRouteCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            var presentationCommand = new RouteCameraPresentationCommand(
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

            if (!routeCameraExecutor.TryPrepare(presentationCommand, out var presentationResult, out reason))
            {
                var failureFact = presentationResult?.FailureFact;
                result = SessionOperationalRouteCameraPrepareResult.Failed(failureFact, reason);
                LogFailed(command, failureFact, reason);
                return false;
            }

            var readyFact = presentationResult.ReadyFact;
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

            var releaseCommand = new RouteCameraReleaseCommand(
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

            if (!routeCameraExecutor.TryRelease(releaseCommand, out var releaseResult, out reason))
            {
                var failureFact = releaseResult?.FailureFact;
                result = SessionOperationalRouteCameraReleaseResult.Failed(failureFact, reason);

                DebugUtility.Log(typeof(SessionOperationalRouteCameraAdapter),
                    $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationReleaseFailed currentRouteIdentity='{command.CurrentRouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' releaseRouteIdentity='{releaseCommand.RouteIdentity}' routeOperationId='{releaseCommand.RouteOperationId}' transitionId='{releaseCommand.TransitionId}' routeSequence='{releaseCommand.RouteSequence}' surfaceKind='{releaseCommand.SurfaceKind}' requirementId='{releaseCommand.RequirementId}' source='{command.Source}' reason='{command.Reason}' failureReason='{reason}'.",
                    DebugUtility.Colors.Error);

                return false;
            }

            var releasedFact = releaseResult.ReleasedFact;
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
                dependencyProvider.TryGetForScene<SurfaceCameraAnchorHost>(sceneName, out var registeredHost) &&
                registeredHost != null)
            {
                anchorHost = registeredHost;
                reason = "surface_camera_anchor_host_resolved_from_scene_scope";
                return true;
            }

            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                reason = "surface_camera_scene_not_loaded";
                return false;
            }

            var hosts = new List<SurfaceCameraAnchorHost>(4);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
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

            return string.Empty;
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
