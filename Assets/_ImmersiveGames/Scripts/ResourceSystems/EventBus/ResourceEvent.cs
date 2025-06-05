using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.EventBus
{
    // Evento genérico para recursos (cheio, esgotado, etc.)
    public class ResourceEvent: IEvent
    {
        public GameObject Source { get; } // Objeto que disparou o evento
        public ResourceType Type { get; } // Tipo do recurso
        public float Percentage { get; } // Porcentagem atual do recurso
        public string UniqueId { get; }

        public ResourceEvent(string uniqueId, GameObject source, ResourceType type, float percentage)
        {
            Source = source;
            Type = type;
            Percentage = percentage;
            UniqueId = uniqueId;
        }
    }
    
    public class ResourceThresholdDirectionEvent : IEvent
    {
        public ThresholdCrossInfo Info { get; }
        public string UniqueId { get; }

        public ResourceThresholdDirectionEvent(ThresholdCrossInfo info, string uniqueId)
        {
            Info = info;
            UniqueId = uniqueId;
        }
    }
    public class ResourceBindEvent : IEvent
    {
        public GameObject Source { get; }
        public ResourceType Type { get; }
        public string UniqueId { get; }
        public IResource Resource { get; }

        public ResourceBindEvent(GameObject source, ResourceType type, string uniqueId, IResource resource)
        {
            Source = source;
            Type = type;
            UniqueId = uniqueId;
            Resource = resource;
        }
    }
}