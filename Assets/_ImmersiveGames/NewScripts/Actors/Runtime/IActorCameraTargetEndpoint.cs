using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    public interface IActorCameraTargetEndpoint
    {
        Transform Transform { get; }
        Transform FollowTarget { get; }
        Transform LookAtTarget { get; }
        bool HasValidTargets { get; }
    }
}
