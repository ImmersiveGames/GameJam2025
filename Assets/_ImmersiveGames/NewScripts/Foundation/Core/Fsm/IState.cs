namespace _ImmersiveGames.NewScripts.Foundation.Core.Fsm
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


