using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ResourceUpdateEvent : IEvent
    {
        public string ActorId { get; }
        public ResourceType ResourceType { get; }
        public IResourceValue NewValue { get; }

        public ResourceUpdateEvent(string actorId, ResourceType resourceType, IResourceValue newValue)
        {
            ActorId = actorId;
            ResourceType = resourceType;
            NewValue = newValue;
        }
    }
    // Evento disparado quando um threshold é cruzado
    public class ResourceThresholdEvent : IEvent
    {
        public string ActorId { get; }
        public ResourceType ResourceType { get; }
        public float Threshold { get; } // valor do threshold (0..1)
        public bool IsAscending { get; } // true = subindo (ex: 0.20 -> 0.30 cruzou 0.25 subindo)
        public float CurrentPercentage { get; } // valor atual do recurso (0..1)

        public ResourceThresholdEvent(string actorId, ResourceType resourceType, float threshold, bool isAscending, float currentPercentage)
        {
            ActorId = actorId;
            ResourceType = resourceType;
            Threshold = threshold;
            IsAscending = isAscending;
            CurrentPercentage = currentPercentage;
        }
    }
    public class ResourceVisualFeedbackEvent : IEvent
    {
        public string ActorId { get; }
        public ResourceType ResourceType { get; }
        public float Threshold { get; }
        public bool IsAscending { get; }
        public ResourceVisualFeedbackEvent(string actorId, ResourceType resourceType, float threshold, bool isAscending)
        {
            ActorId = actorId;
            ResourceType = resourceType;
            Threshold = threshold;
            IsAscending = isAscending;

        }
    }
    
}