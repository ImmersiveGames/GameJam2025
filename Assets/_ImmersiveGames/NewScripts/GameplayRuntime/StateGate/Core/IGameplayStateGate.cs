using System;
namespace ImmersiveGames.GameJam2025.Game.Gameplay.State.Core
{
    /// <summary>
    /// Contract for services that gate player or gameplay actions based on global state.
    /// </summary>
    public interface IGameplayStateGate : IDisposable
    {
        bool CanExecuteGameplayAction(GameplayAction action);
        bool CanExecuteUiAction(UiAction action);
        bool CanExecuteSystemAction(SystemAction action);
        bool IsGameActive();
    }
}


