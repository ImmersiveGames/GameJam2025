using Unity.Cinemachine;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public sealed class OperationalCameraHandle
    {
        public Camera UnityCamera { get; }
        public CinemachineBrain CinemachineBrain { get; }
        public bool HasCinemachineBrain => CinemachineBrain != null;
        public string Source { get; }
        public string Reason { get; }

        public OperationalCameraHandle(
            Camera unityCamera,
            CinemachineBrain cinemachineBrain,
            string source,
            string reason)
        {
            UnityCamera = unityCamera;
            CinemachineBrain = cinemachineBrain;
            Source = source;
            Reason = reason;
        }
    }
}
