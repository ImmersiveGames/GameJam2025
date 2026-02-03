using UnityEngine;
namespace _ImmersiveGames.Scripts.StateMachineSystems
{
    public interface OldIState
    {
        void Update();
        void FixedUpdate(){}
        void OnEnter();
        void OnExit(){}
        bool CanPerformAction(OldActionType action);
        bool IsGameActive();
    }
    public interface IHandleMomentum {
        void HandleMomentum(Vector3 normalGround);
    }
    public enum OldActionType
    {
        Spawn,
        Move,
        Shoot,
        Interact,
        Navigate,
        None,
        UiSubmit,
        UiCancel,
        RequestReset,
        RequestQuit
    }
}

