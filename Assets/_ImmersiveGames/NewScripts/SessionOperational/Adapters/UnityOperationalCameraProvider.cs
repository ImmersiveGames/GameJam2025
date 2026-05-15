using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using Unity.Cinemachine;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public sealed class UnityOperationalCameraProvider : IOperationalCameraProvider
    {
        public bool TryGetCurrent(
            out OperationalCameraHandle handle,
            out string reason)
        {
            Camera unityCamera = UnityOperationalCameraRuntimeAdapter.CurrentOperationalCamera;

            if (unityCamera == null)
            {
                handle = null;
                reason = "operational_camera_missing";
                return false;
            }

            CinemachineBrain cinemachineBrain = unityCamera.GetComponent<CinemachineBrain>();

            handle = new OperationalCameraHandle(
                unityCamera,
                cinemachineBrain,
                nameof(UnityOperationalCameraProvider),
                "operational_camera_current_resolved");

            reason = "operational_camera_current_resolved";
            return true;
        }
    }
}
