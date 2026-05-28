using _ImmersiveGames.NewScripts.Actors.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Players.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PlayerCameraEndpoint : MonoBehaviour, IActorCameraTargetEndpoint
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform lookAtTarget;

        public Transform Transform => transform;
        public Transform FollowTarget => followTarget;
        public Transform LookAtTarget => lookAtTarget;

        public bool HasValidTargets => followTarget != null;
    }
}
