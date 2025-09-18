using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Events
{
    /// <summary>
    /// Evento disparado quando o valor de um recurso muda (aumenta ou diminui).
    /// </summary>
    public class ResourceValueChangedEvent : IEvent
    {
        public string UniqueId { get; }
        public GameObject Source { get; }
        public ResourceType Type { get; }
        public float Percentage { get; }
        public bool IsAscending { get; }

        public ResourceValueChangedEvent(string uniqueId, GameObject source, ResourceType type, float percentage, bool isAscending)
        {
            UniqueId = uniqueId;
            Source = source;
            Type = type;
            Percentage = percentage;
            IsAscending = isAscending;
        }
    }

    /// <summary>
    /// Evento disparado quando um recurso cruza um limiar (threshold).
    /// </summary>
    public class ResourceThresholdCrossedEvent : IEvent
    {
        public string UniqueId { get; }
        public GameObject Source { get; }
        public ThresholdCrossInfo Info { get; }

        public ResourceThresholdCrossedEvent(string uniqueId, GameObject source, ThresholdCrossInfo info)
        {
            UniqueId = uniqueId;
            Source = source;
            Info = info;
        }
    }

    /// <summary>
    /// Evento para mudanças de direção em limiares de fome (mantido inalterado).
    /// </summary>
    public class HungryChangeThresholdDirectionEvent : IEvent
    {
        public ThresholdCrossInfo Info { get; }
        public HungryChangeThresholdDirectionEvent(ThresholdCrossInfo info)
        {
            Info = info;
        }
    }

    /// <summary>
    /// Evento para associar um recurso a um objeto específico (mantido inalterado).
    /// </summary>
    public class ResourceBindEvent : IEvent
    {
        public GameObject Source { get; }
        public ResourceType Type { get; }
        public string UniqueId { get; }
        public IResourceValue Resource { get; } // Atualizado para IResourceValue

        public ResourceBindEvent(GameObject source, ResourceType type, string uniqueId, IResourceValue resource)
        {
            Source = source;
            Type = type;
            UniqueId = uniqueId;
            Resource = resource;
        }
    }
}