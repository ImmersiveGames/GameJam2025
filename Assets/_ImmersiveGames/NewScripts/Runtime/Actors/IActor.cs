using UnityEngine;
namespace _ImmersiveGames.NewScripts.Runtime.Actors
{
    /// <summary>
    /// Contrato mínimo para atores controlados pelo novo pipeline.
    /// Mantém identificadores e metadados necessários para registrá-los e manipulá-los.
    /// </summary>
    public interface IActor
    {
        string ActorId { get; }

        string DisplayName { get; }

        Transform Transform { get; }

        bool IsActive { get; }
    }
}
