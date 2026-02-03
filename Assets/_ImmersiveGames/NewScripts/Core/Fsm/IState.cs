namespace _ImmersiveGames.NewScripts.Core.Fsm
{
    public interface IState
    {
        void Update();
        void FixedUpdate() { }
        void OnEnter();
        void OnExit() { }
        bool CanPerformGameplayAction(Runtime.Actions.GameplayAction action);
        bool CanPerformUiAction(Runtime.Actions.UiAction action);
        bool CanPerformSystemAction(Runtime.Actions.SystemAction action);
        bool IsGameActive();
    }
}
