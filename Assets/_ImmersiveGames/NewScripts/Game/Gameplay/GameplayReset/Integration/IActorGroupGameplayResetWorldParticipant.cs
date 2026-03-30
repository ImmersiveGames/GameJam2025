using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Integration
{
    public interface IActorGroupGameplayResetWorldParticipant
    {
        WorldResetScope Scope { get; }

        int Order { get; }

        Task ResetAsync(WorldResetContext context);
    }
}



