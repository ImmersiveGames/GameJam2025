using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Domain;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Interop
{
    public interface IActorGroupRearmWorldParticipant
    {
        WorldResetScope Scope { get; }

        int Order { get; }

        Task ResetAsync(WorldResetContext context);
    }
}


