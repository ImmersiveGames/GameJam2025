using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using Unity.Cinemachine;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public sealed class CinemachineRouteCameraDirector : IRouteCameraDirector
    {
        private readonly IOperationalCameraProvider _operationalCameraProvider;

        public CinemachineRouteCameraDirector(
            IOperationalCameraProvider operationalCameraProvider)
        {
            this._operationalCameraProvider = operationalCameraProvider;
        }

        public bool TryPrepareRouteCamera(
            RouteCameraPresentationCommand command,
            out RouteCameraBindingResult result,
            out string reason)
        {
            result = null;

            if (command == null)
            {
                reason = "route_camera_command_null";
                return false;
            }

            RouteCameraPresentationRequirement requirement = command.Requirement;

            if (requirement == null)
            {
                reason = "route_camera_requirement_missing";
                result = RouteCameraBindingResult.Failed(command, reason);
                return false;
            }

            if (_operationalCameraProvider == null)
            {
                reason = "operational_camera_provider_missing";
                result = RouteCameraBindingResult.Failed(command, reason);
                return false;
            }

            if (!_operationalCameraProvider.TryGetCurrent(
                    out OperationalCameraHandle operationalCamera,
                    out string providerReason))
            {
                reason = providerReason;
                result = RouteCameraBindingResult.Failed(command, reason);
                return false;
            }

            if (operationalCamera == null)
            {
                reason = "operational_camera_handle_null";
                result = RouteCameraBindingResult.Failed(command, reason);
                return false;
            }

            if (operationalCamera.UnityCamera == null)
            {
                reason = "operational_unity_camera_missing";
                result = RouteCameraBindingResult.Failed(command, reason);
                return false;
            }

            if (!operationalCamera.HasCinemachineBrain || operationalCamera.CinemachineBrain == null)
            {
                reason = "operational_cinemachine_brain_missing";
                result = RouteCameraBindingResult.Failed(command, reason);
                return false;
            }

            if (requirement.PresentationRigPrefab == null)
            {
                reason = "route_camera_presentation_rig_prefab_missing";
                result = RouteCameraBindingResult.Failed(command, reason);
                return false;
            }

            GameObject rigInstance = Object.Instantiate(requirement.PresentationRigPrefab);
            rigInstance.name = BuildRigInstanceName(command);

            if (!ValidatePresentationRig(
                    rigInstance,
                    out CinemachineCamera cinemachineCamera,
                    out reason))
            {
                SafeDestroy(rigInstance);
                result = RouteCameraBindingResult.Failed(command, reason);
                return false;
            }

            cinemachineCamera.Follow = requirement.TrackingTarget;

            if (requirement.LookAtTarget != null)
            {
                cinemachineCamera.LookAt = requirement.LookAtTarget;
            }

            cinemachineCamera.Priority = requirement.Priority;

            RouteCameraBindingHandle handle = new RouteCameraBindingHandle(
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.SurfaceKind,
                requirement.RequirementId,
                operationalCamera.UnityCamera,
                rigInstance);

            reason = "route_camera_ready";

            result = RouteCameraBindingResult.Ready(
                command,
                handle,
                reason);

            DebugUtility.Log(typeof(CinemachineRouteCameraDirector),
                $"[OBS][CameraPresentation][RouteDirector] RouteCameraPrepared " +
                $"outputCamera='{operationalCamera.UnityCamera.name}' " +
                $"hasOperationalBrain='{operationalCamera.HasCinemachineBrain}' " +
                $"presentationRig='{rigInstance.name}' " +
                $"surfaceKind='{command.SurfaceKind}' " +
                $"routeIdentity='{command.RouteIdentity}' " +
                $"routeOperationId='{command.RouteOperationId}' " +
                $"requirementId='{requirement.RequirementId}'.");

            return true;
        }

        public bool TryReleaseRouteCamera(
            RouteCameraBindingResult activeBinding,
            out string reason)
        {
            if (activeBinding == null)
            {
                reason = "route_camera_active_binding_missing";
                return false;
            }

            if (!activeBinding.Success)
            {
                reason = "route_camera_active_binding_not_successful";
                return false;
            }

            RouteCameraBindingHandle handle = activeBinding.Handle;

            if (handle == null)
            {
                reason = "route_camera_active_binding_handle_missing";
                return false;
            }

            string outputCameraName = handle.UnityCamera != null
                ? handle.UnityCamera.name
                : "<none>";

            string presentationRigName = handle.PresentationRigInstance != null
                ? handle.PresentationRigInstance.name
                : "<none>";

            if (handle.PresentationRigInstance != null)
            {
                SafeDestroy(handle.PresentationRigInstance);
            }

            reason = "route_camera_released";

            DebugUtility.Log(typeof(CinemachineRouteCameraDirector),
                $"[OBS][CameraPresentation][RouteDirector] RouteCameraReleased " +
                $"outputCamera='{outputCameraName}' " +
                $"presentationRig='{presentationRigName}' " +
                $"surfaceKind='{handle.SurfaceKind}' " +
                $"routeIdentity='{handle.RouteIdentity}' " +
                $"routeOperationId='{handle.RouteOperationId}' " +
                $"requirementId='{handle.RequirementId}'.");

            return true;
        }

        private static bool ValidatePresentationRig(
            GameObject rigInstance,
            out CinemachineCamera cinemachineCamera,
            out string reason)
        {
            cinemachineCamera = null;

            if (rigInstance == null)
            {
                reason = "route_camera_presentation_rig_instance_null";
                return false;
            }

            Camera[] unityCameras = rigInstance.GetComponentsInChildren<Camera>(
                includeInactive: true);

            if (unityCameras != null && unityCameras.Length > 0)
            {
                reason = "route_presentation_rig_must_not_contain_unity_camera";
                return false;
            }

            CinemachineBrain[] brains = rigInstance.GetComponentsInChildren<CinemachineBrain>(
                includeInactive: true);

            if (brains != null && brains.Length > 0)
            {
                reason = "route_presentation_rig_must_not_contain_cinemachine_brain";
                return false;
            }

            CinemachineCamera[] cinemachineCameras = rigInstance.GetComponentsInChildren<CinemachineCamera>(
                includeInactive: true);

            if (cinemachineCameras == null || cinemachineCameras.Length == 0)
            {
                reason = "route_presentation_rig_cinemachine_camera_missing";
                return false;
            }

            if (cinemachineCameras.Length > 1)
            {
                reason = "route_presentation_rig_multiple_cinemachine_cameras";
                return false;
            }

            cinemachineCamera = cinemachineCameras[0];

            reason = "route_presentation_rig_valid";
            return true;
        }

        private static string BuildRigInstanceName(
            RouteCameraPresentationCommand command)
        {
            string surfaceKind = string.IsNullOrWhiteSpace(command.SurfaceKind)
                ? "surface"
                : command.SurfaceKind;

            string routeOperationId = string.IsNullOrWhiteSpace(command.RouteOperationId)
                ? "route-operation"
                : command.RouteOperationId;

            return $"RouteCameraRig::{surfaceKind}::{routeOperationId}";
        }

        private static void SafeDestroy(
            GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            Object.Destroy(instance);
        }
    }
}
