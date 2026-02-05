using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Actors
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
