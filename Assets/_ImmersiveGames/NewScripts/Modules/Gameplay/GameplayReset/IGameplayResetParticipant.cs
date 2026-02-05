using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.GameplayReset
{
    public interface IGameplayResetParticipant
    {
        WorldResetScope Scope { get; }

        int Order { get; }

        Task ResetAsync(WorldResetContext context);
    }
}

