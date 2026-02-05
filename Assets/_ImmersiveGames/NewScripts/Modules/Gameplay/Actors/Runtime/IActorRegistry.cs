using System.Collections.Generic;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Runtime
{
    /// <summary>
    /// Registro básico de atores para o escopo da cena.
    /// Responsável por garantir unicidade de IDs e facilitar consulta/limpeza.
    /// </summary>
    public interface IActorRegistry
    {
        IReadOnlyCollection<IActor> Actors { get; }

        int Count { get; }

        bool TryGetActor(string actorId, out IActor actor);

        bool Register(IActor actor);

        bool Unregister(string actorId);

        void Clear();

        void GetActors(List<IActor> target);
    }
}
