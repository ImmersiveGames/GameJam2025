using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Values;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems
{
    public class RuntimeAttributeUpdateEvent : IEvent
    {
        public string ActorId { get; }
        public RuntimeAttributeType RuntimeAttributeType { get; }
        public IRuntimeAttributeValue NewValue { get; }

        public RuntimeAttributeUpdateEvent(string actorId, RuntimeAttributeType runtimeAttributeType, IRuntimeAttributeValue newValue)
        {
            ActorId = actorId;
            RuntimeAttributeType = runtimeAttributeType;
            NewValue = newValue;
        }
    }
    // Evento disparado quando um threshold é cruzado
    public class RuntimeAttributeThresholdEvent : IEvent
    {
        public string ActorId { get; }
        public RuntimeAttributeType RuntimeAttributeType { get; }
        public float Threshold { get; } // valor do threshold (0..1)
        public bool IsAscending { get; } // true = subindo (ex: 0.20 -> 0.30 cruzou 0.25 subindo)
        public float CurrentPercentage { get; } // valor atual do recurso (0..1)

        public RuntimeAttributeThresholdEvent(string actorId, RuntimeAttributeType runtimeAttributeType, float threshold, bool isAscending, float currentPercentage)
        {
            ActorId = actorId;
            RuntimeAttributeType = runtimeAttributeType;
            Threshold = threshold;
            IsAscending = isAscending;
            CurrentPercentage = currentPercentage;
        }
    }
    public class RuntimeAttributeVisualFeedbackEvent : IEvent
    {
        public string ActorId { get; }
        public RuntimeAttributeType RuntimeAttributeType { get; }
        public float Threshold { get; }
        public bool IsAscending { get; }
        public RuntimeAttributeVisualFeedbackEvent(string actorId, RuntimeAttributeType runtimeAttributeType, float threshold, bool isAscending)
        {
            ActorId = actorId;
            RuntimeAttributeType = runtimeAttributeType;
            Threshold = threshold;
            IsAscending = isAscending;

        }
    }
    
}