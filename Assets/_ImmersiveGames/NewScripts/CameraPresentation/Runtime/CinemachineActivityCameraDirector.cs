using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using Unity.Cinemachine;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public sealed class CinemachineActivityCameraDirector : IActivityCameraDirector
    {
        private const int ActivityCameraPriority = 100;

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

            GameObject rigInstance = Object.Instantiate(command.Requirement.CameraRigPrefab);
            rigInstance.name = BuildRigInstanceName(command);

            if (!TryGetSingleCamera(rigInstance, out Camera unityCamera, out reason))
            {
                Object.Destroy(rigInstance);
                result = ActivityCameraBindingResult.Failed(command, reason);
                return false;
            }

            if (!TryGetSingleCinemachineBrain(rigInstance, out CinemachineBrain brain, out reason))
            {
                Object.Destroy(rigInstance);
                result = ActivityCameraBindingResult.Failed(command, reason);
                return false;
            }

            if (brain.OutputCamera != unityCamera)
            {
                Object.Destroy(rigInstance);
                reason = "cinemachine_brain_output_camera_mismatch";
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

            reason = "activity_camera_ready";
            result = ActivityCameraBindingResult.Ready(command, unityCamera, rigInstance, reason);
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

            reason = "activity_camera_released";
            return true;
        }

        private static bool TryGetSingleCamera(
            GameObject rigInstance,
            out Camera camera,
            out string reason)
        {
            Camera[] cameras = rigInstance.GetComponentsInChildren<Camera>(true);

            if (cameras.Length == 0)
            {
                camera = null;
                reason = "unity_camera_missing";
                return false;
            }

            if (cameras.Length > 1)
            {
                camera = null;
                reason = "multiple_unity_cameras_found";
                return false;
            }

            camera = cameras[0];
            reason = "unity_camera_found";
            return true;
        }

        private static bool TryGetSingleCinemachineBrain(
            GameObject rigInstance,
            out CinemachineBrain brain,
            out string reason)
        {
            CinemachineBrain[] brains = rigInstance.GetComponentsInChildren<CinemachineBrain>(true);

            if (brains.Length == 0)
            {
                brain = null;
                reason = "cinemachine_brain_missing";
                return false;
            }

            if (brains.Length > 1)
            {
                brain = null;
                reason = "multiple_cinemachine_brains_found";
                return false;
            }

            brain = brains[0];
            reason = "cinemachine_brain_found";
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
