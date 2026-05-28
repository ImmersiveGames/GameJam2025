using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    public interface IActorMovementEndpoint
    {
        Transform Transform { get; }
        bool IsMovementEnabled { get; }
        void SetMovementEnabled(bool enabled);
        void ClearMovementState();
    }
}
