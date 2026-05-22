using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using Unity.Cinemachine;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public sealed class CinemachineActivityCameraDirector : IActivityCameraDirector
    {
        private const int ActivityCameraPriority = 100;
        private readonly IOperationalCameraProvider operationalCameraProvider;

        public CinemachineActivityCameraDirector(IOperationalCameraProvider operationalCameraProvider)
        {
            this.operationalCameraProvider = operationalCameraProvider;
        }

        public bool TryPrepareActivityCamera(
            ActivityCameraBindingCommand command,
            out ActivityCameraBindingResult result,
            out string reason)
        {
            if (!ActivityCameraBindingCommandValidator.TryValidate(command, out reason))
            {
                result = ActivityCameraBindingResult.Failed(command, reason);
                return false;
            }

            if (operationalCameraProvider == null)
            {
                reason = "operational_camera_provider_missing";
                result = ActivityCameraBindingResult.Failed(command, reason);
                return false;
            }

            if (!operationalCameraProvider.TryGetCurrent(out OperationalCameraHandle operationalHandle, out reason))
            {
                result = ActivityCameraBindingResult.Failed(command, reason);
                return false;
            }

            if (operationalHandle == null)
            {
                reason = "operational_camera_handle_missing";
                result = ActivityCameraBindingResult.Failed(command, reason);
                return false;
            }

            if (operationalHandle.UnityCamera == null)
            {
                reason = "operational_output_camera_missing";
                result = ActivityCameraBindingResult.Failed(command, reason);
                return false;
            }

            if (!operationalHandle.HasCinemachineBrain)
            {
                reason = "operational_cinemachine_brain_missing";
                result = ActivityCameraBindingResult.Failed(command, reason);
                return false;
            }

            GameObject rigInstance = Object.Instantiate(command.Requirement.CameraRigPrefab);
            rigInstance.name = BuildRigInstanceName(command);

            if (!EnsurePresentationRigHasNoUnityCamera(rigInstance, out reason))
            {
                Object.Destroy(rigInstance);
                result = ActivityCameraBindingResult.Failed(command, reason);
                return false;
            }

            if (!EnsurePresentationRigHasNoCinemachineBrain(rigInstance, out reason))
            {
                Object.Destroy(rigInstance);
                result = ActivityCameraBindingResult.Failed(command, reason);
                return false;
            }

            if (!TryGetSingleCinemachineCamera(rigInstance, out CinemachineCamera cinemachineCamera, out reason))
            {
                Object.Destroy(rigInstance);
                result = ActivityCameraBindingResult.Failed(command, reason);
                return false;
            }

            cinemachineCamera.Target.TrackingTarget = command.Requirement.TrackingTarget;
            cinemachineCamera.Target.LookAtTarget = command.Requirement.LookAtTarget;
            cinemachineCamera.Priority = ActivityCameraPriority;

            DebugUtility.Log(typeof(CinemachineActivityCameraDirector),
                $"[OBS][CameraPresentation][Director] ActivityCameraPrepared outputCamera='{operationalHandle.UnityCamera.name}' hasOperationalBrain='{operationalHandle.HasCinemachineBrain}' presentationRig='{rigInstance.name}' activityIdentity='{command.ActivityIdentity}' requirementId='{command.Requirement.RequirementId}'.",
                DebugUtility.Colors.Info);

            reason = "activity_camera_ready";
            result = ActivityCameraBindingResult.Ready(command, operationalHandle.UnityCamera, rigInstance, reason);
            return true;
        }

        public bool TryReleaseActivityCamera(
            ActivityCameraBindingResult binding,
            out string reason)
        {
            if (binding == null)
            {
                reason = "binding_null";
                return false;
            }

            if (!binding.Success)
            {
                reason = "binding_not_successful";
                return false;
            }

            if (binding.Handle == null)
            {
                reason = "binding_handle_missing";
                return false;
            }

            if (binding.Handle.CameraRigInstance == null)
            {
                reason = "camera_rig_instance_missing";
                return false;
            }

            Object.Destroy(binding.Handle.CameraRigInstance);

            DebugUtility.Log(typeof(CinemachineActivityCameraDirector),
                $"[OBS][CameraPresentation][Director] ActivityCameraReleased outputCamera='{binding.Handle.UnityCamera?.name}' presentationRig='{binding.Handle.CameraRigInstance.name}' activityIdentity='{binding.Handle.ActivityIdentity}'.",
                DebugUtility.Colors.Info);

            reason = "activity_camera_released";
            return true;
        }

        public bool TryRebindActivityCameraTargets(
            ActivityCameraBindingHandle bindingHandle,
            ActivityCameraRebindTargetsCommand command,
            out string reason)
        {
            if (bindingHandle == null)
            {
                reason = "binding_handle_missing";
                return false;
            }

            if (command == null)
            {
                reason = "rebind_command_null";
                return false;
            }

            if (bindingHandle.CameraRigInstance == null)
            {
                reason = "camera_rig_instance_missing";
                return false;
            }

            if (command.TrackingTarget == null)
            {
                reason = "tracking_target_missing";
                return false;
            }

            if (!TryGetSingleCinemachineCamera(bindingHandle.CameraRigInstance, out CinemachineCamera cinemachineCamera, out reason))
            {
                return false;
            }

            cinemachineCamera.Target.TrackingTarget = command.TrackingTarget;
            cinemachineCamera.Target.LookAtTarget = command.LookAtTarget;

            DebugUtility.Log(typeof(CinemachineActivityCameraDirector),
                $"[OBS][CameraPresentation][Director] ActivityCameraTargetsRebound activityIdentity='{command.ActivityIdentity}' presentationRig='{bindingHandle.CameraRigInstance.name}' trackingTarget='{command.TrackingTarget.name}' lookAtTarget='{command.LookAtTarget?.name ?? "<none>"}'.",
                DebugUtility.Colors.Info);

            reason = "activity_camera_targets_rebound";
            return true;
        }

        private static bool EnsurePresentationRigHasNoUnityCamera(
            GameObject rigInstance,
            out string reason)
        {
            Camera[] cameras = rigInstance.GetComponentsInChildren<Camera>(true);
            if (cameras.Length > 0)
            {
                reason = "presentation_rig_must_not_contain_unity_camera";
                return false;
            }

            reason = "presentation_rig_without_unity_camera";
            return true;
        }

        private static bool EnsurePresentationRigHasNoCinemachineBrain(
            GameObject rigInstance,
            out string reason)
        {
            CinemachineBrain[] brains = rigInstance.GetComponentsInChildren<CinemachineBrain>(true);
            if (brains.Length > 0)
            {
                reason = "presentation_rig_must_not_contain_cinemachine_brain";
                return false;
            }

            reason = "presentation_rig_without_cinemachine_brain";
            return true;
        }

        private static bool TryGetSingleCinemachineCamera(
            GameObject rigInstance,
            out CinemachineCamera cinemachineCamera,
            out string reason)
        {
            CinemachineCamera[] cameras = rigInstance.GetComponentsInChildren<CinemachineCamera>(true);

            if (cameras.Length == 0)
            {
                cinemachineCamera = null;
                reason = "cinemachine_camera_missing";
                return false;
            }

            if (cameras.Length > 1)
            {
                cinemachineCamera = null;
                reason = "multiple_cinemachine_cameras_found";
                return false;
            }

            cinemachineCamera = cameras[0];
            reason = "cinemachine_camera_found";
            return true;
        }

        private static string BuildRigInstanceName(ActivityCameraBindingCommand command)
        {
            return $"ActivityCameraRig::{command.ActivityIdentity}::{command.RouteOperationId}";
        }
    }
}
