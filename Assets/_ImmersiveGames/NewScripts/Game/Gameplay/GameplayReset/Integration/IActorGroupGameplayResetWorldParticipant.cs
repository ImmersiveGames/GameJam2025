using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Integration
{
    /// <summary>
    /// Ponte de participacao no reset macro para actors vivos ja materializados.
    /// Nao recria actor, nao assume ownership de runtime e nao substitui o trilho de Spawn.
    /// </summary>
    public interface IActorGroupGameplayResetWorldParticipant
    {
        WorldResetScope Scope { get; }

        int Order { get; }

        Task ResetAsync(WorldResetContext context);
    }
}



