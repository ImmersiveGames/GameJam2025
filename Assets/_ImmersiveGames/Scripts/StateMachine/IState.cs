using UnityEngine;
namespace _ImmersiveGames.Scripts.StateMachine {
    public interface IState
    {
        void Update();
        void FixedUpdate();
        void OnEnter();
        void OnExit();
    }
    public interface IHandleMomentum {
        void HandleMomentum(Vector3 normalGround);
    }
}