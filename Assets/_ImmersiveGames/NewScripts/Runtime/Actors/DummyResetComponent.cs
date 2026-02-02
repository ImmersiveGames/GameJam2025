using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.Reset;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Runtime.Actors
{
    public class DummyResetComponent: MonoBehaviour, IGameplayResettable
    {

        public Task ResetCleanupAsync(GameplayResetContext ctx)
        {
            Debug.Log($"Reset cleanup called");
            return Task.CompletedTask;
        }
        public Task ResetRestoreAsync(GameplayResetContext ctx)
        {
            Debug.Log($"Reset restore called");
            return Task.CompletedTask;
        }
        public Task ResetRebindAsync(GameplayResetContext ctx)
        {
            Debug.Log($"Reset rebind called");
            return Task.CompletedTask;
        }
    }
}
