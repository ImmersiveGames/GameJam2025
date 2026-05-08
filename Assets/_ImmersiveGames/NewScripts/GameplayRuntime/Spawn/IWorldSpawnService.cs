using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
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
        /// Archetype operacional usado para selecionar este serviço via contrato canônico.
        /// </summary>
        string SpawnArchetypeId { get; }

        /// <summary>
        /// Indica se este serviço participa da garantia mínima do ciclo de vida.
        /// </summary>
        bool IsRequiredForLifecycle { get; }

        Task SpawnAsync(ActorSpawnRequest request);

        Task DespawnAsync();

        bool TryGetCurrentRuntimeActorId(out RuntimeActorId runtimeActorId);
    }
}

