using UnityEngine;
namespace _ImmersiveGames.Scripts.StatesMachines
{
    public interface IState
    {
        void Update();
        void FixedUpdate(){}
        void OnEnter();
        void OnExit(){}
        bool CanPerformAction(ActionType action);
        bool IsGameActive();
    }
    public interface IHandleMomentum {
        void HandleMomentum(Vector3 normalGround);
    }
    public enum ActionType
    {
        Spawn,
        Move,
        Shoot,
        Interact,
        Navigate,
        None
    }
}