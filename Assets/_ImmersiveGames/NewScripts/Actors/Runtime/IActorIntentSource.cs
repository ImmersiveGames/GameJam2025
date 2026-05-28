using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    public interface IActorIntentSource
    {
        Transform Transform { get; }
        void SetInputEnabled(bool enabled);
        void ClearInput();
    }
}
