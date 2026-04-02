using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.Spawn
{
    /// <summary>
    /// Serviço responsável por spawn/despawn dos elementos que compõem o mundo atual.
    /// Implementações devem ser puras (sem dependência em MonoBehaviours) e idempotentes
    /// em chamadas repetidas durante o ciclo de vida da cena.
    /// </summary>
    public interface IWorldSpawnService
    {
        string Name { get; }

        /// <summary>
        /// Identidade tipada do actor materializado por este serviço.
        /// O pipeline de reset do mundo deve consumir esta metadata em vez de inferir por nome.
        /// </summary>
        ActorKind SpawnedActorKind { get; }

        /// <summary>
        /// Indica se este serviço participa da garantia mínima do hard reset macro.
        /// </summary>
        bool IsRequiredForWorldReset { get; }

        Task SpawnAsync();

        Task DespawnAsync();
    }
}
