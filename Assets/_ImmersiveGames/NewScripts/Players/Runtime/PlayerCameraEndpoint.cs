using UnityEngine;

namespace _ImmersiveGames.NewScripts.Players.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PlayerCameraEndpoint : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform lookAtTarget;

        public Transform FollowTarget => followTarget;
        public Transform LookAtTarget => lookAtTarget;

        public bool HasValidTargets => followTarget != null;
    }
}
