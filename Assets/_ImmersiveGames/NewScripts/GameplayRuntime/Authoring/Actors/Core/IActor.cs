using UnityEngine;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core
{
    /// <summary>
    /// Contrato mínimo para atores controlados pelo novo pipeline.
    /// Mantém identificadores, metadados e o ponto de recepcao da identidade atribuida pelo Spawn.
    /// </summary>
    public interface IActor
    {
        string ActorId { get; }

        string DisplayName { get; }

        Transform Transform { get; }

        bool IsActive { get; }

        /// <summary>
        /// Recebe a identidade atribuida pelo trilho de Spawn.
        /// O actor nao gera a identidade; apenas valida/porta o valor recebido.
        /// </summary>
        void Initialize(string actorId);
    }
}

