using System.Threading.Tasks;
namespace ImmersiveGames.GameJam2025.Game.Gameplay.Actors.Core
{
    /// <summary>
    /// Hooks opcionais para lifecycle do Actor durante reset do mundo.
    /// Implementação típica: MonoBehaviour no GameObject do Actor.
    /// </summary>
    public interface IActorLifecycleHook
    {
        Task OnBeforeActorDespawnAsync();

        Task OnAfterActorDespawnAsync();

        Task OnBeforeActorSpawnAsync();

        Task OnAfterActorSpawnAsync();
    }
}

