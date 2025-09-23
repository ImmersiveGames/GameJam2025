using _ImmersiveGames.Scripts.NewResourceSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.NewResourceSystem.Events
{
    /// <summary>
    /// Evento para associar um recurso a um objeto específico.
    /// </summary>
    public class ResourceBindEvent : IEvent
    {
        public GameObject Source { get; }
        public ResourceType Type { get; }
        public string UniqueId { get; }
        public IResourceValue Resource { get; }
        public string ActorId { get; } // Novo: ID da entidade

        public ResourceBindEvent(GameObject source, ResourceType type, string uniqueId, IResourceValue resource, string actorId = "")
        {
            Source = source;
            Type = type;
            UniqueId = uniqueId;
            Resource = resource;
            ActorId = actorId;
        }
    }
}