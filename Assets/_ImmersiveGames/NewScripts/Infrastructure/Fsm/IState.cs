namespace _ImmersiveGames.NewScripts.Infrastructure.Fsm
{
    public interface IState
    {
        void Update();
        void FixedUpdate() { }
        void OnEnter();
        void OnExit() { }
        bool CanPerformAction(ActionType action);
        bool IsGameActive();
    }

    public enum ActionType
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
