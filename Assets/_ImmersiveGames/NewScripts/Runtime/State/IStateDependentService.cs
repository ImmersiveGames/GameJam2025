using System;
using _ImmersiveGames.NewScripts.Gameplay.Actions;
namespace _ImmersiveGames.NewScripts.Runtime.State
{
    /// <summary>
    /// Contract for services that gate player or gameplay actions based on global state.
    /// </summary>
    public interface IStateDependentService : IDisposable
    {
        bool CanExecuteGameplayAction(GameplayAction action);
        bool CanExecuteUiAction(UiAction action);
        bool CanExecuteSystemAction(SystemAction action);
        bool IsGameActive();
    }
}

