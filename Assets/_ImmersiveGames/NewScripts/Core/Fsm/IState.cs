namespace _ImmersiveGames.NewScripts.Core.Fsm
{
    public interface IState
    {
        void Update();
        void FixedUpdate() { }
        void OnEnter();
        void OnExit() { }
        bool CanPerformGameplayAction(Gameplay.Actions.GameplayAction action);
        bool CanPerformUiAction(Gameplay.Actions.UiAction action);
        bool CanPerformSystemAction(Gameplay.Actions.SystemAction action);
        bool IsGameActive();
    }
}

