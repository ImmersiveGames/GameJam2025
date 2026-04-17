namespace ImmersiveGames.GameJam2025.Core.Fsm
{
    public interface IState
    {
        void Update();
        void FixedUpdate() { }
        void OnEnter();
        void OnExit() { }
        bool IsGameActive();
    }
}


