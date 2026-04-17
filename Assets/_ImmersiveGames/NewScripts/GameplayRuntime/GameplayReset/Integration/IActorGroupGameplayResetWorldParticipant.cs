using System.Threading.Tasks;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Domain;
namespace ImmersiveGames.GameJam2025.Game.Gameplay.GameplayReset.Integration
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




