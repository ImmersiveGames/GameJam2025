using _ImmersiveGames.Scripts.ActorSystems;
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
        public string ActorId { get; } // Novo: ID da entidade (ex.: Player1, Enemy_42)

        public ResourceValueChangedEvent(string uniqueId, GameObject source, ResourceType type, float percentage, bool isAscending, string actorId = "")
        {
            UniqueId = uniqueId;
            Source = source;
            Type = type;
            Percentage = percentage;
            IsAscending = isAscending;
            ActorId = actorId;
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
        public string ActorId { get; } // Novo

        public ResourceThresholdCrossedEvent(string uniqueId, GameObject source, ThresholdCrossInfo info, string actorId = "")
        {
            UniqueId = uniqueId;
            Source = source;
            Info = info;
            ActorId = actorId;
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

    /// <summary>
    /// Evento disparado quando um modificador é aplicado ou removido.
    /// </summary>
    public class ModifierAppliedEvent : IEvent
    {
        public readonly string UniqueId;
        public readonly GameObject Source;
        public readonly ResourceModifier Modifier;
        public readonly bool IsAdded;
        public readonly string ActorId;

        public ModifierAppliedEvent(string uniqueId, GameObject source, ResourceModifier modifier, bool isAdded, string actorId)
        {
            UniqueId = uniqueId;
            Source = source;
            Modifier = modifier;
            IsAdded = isAdded;
            ActorId = actorId;
        }
    }
}