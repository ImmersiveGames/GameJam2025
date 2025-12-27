using System;
using _ImmersiveGames.NewScripts.Infrastructure.Actions;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;

namespace _ImmersiveGames.NewScripts.Infrastructure.State
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
