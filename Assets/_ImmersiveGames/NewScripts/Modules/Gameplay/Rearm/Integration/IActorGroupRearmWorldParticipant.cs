using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Domain;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Integration
{
    public interface IActorGroupRearmWorldParticipant
    {
        WorldResetScope Scope { get; }

        int Order { get; }

        Task ResetAsync(WorldResetContext context);
    }
}


