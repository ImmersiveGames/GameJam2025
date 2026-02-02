using System;
using _ImmersiveGames.NewScripts.Runtime.Actions;
namespace _ImmersiveGames.NewScripts.Runtime.State
{
    /// <summary>
    /// Contract for services that gate player or gameplay actions based on global state.
    /// </summary>
    public interface IStateDependentService : IDisposable
    {
        bool CanExecuteAction(ActionType action);
        bool IsGameActive();
    }
}
