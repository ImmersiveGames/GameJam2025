using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.NewResourceSystem;
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
        public readonly string UniqueId;
        public readonly GameObject Source;
        public readonly ResourceThresholdInfo Info;
        public readonly string ActorId;

        public class ResourceThresholdInfo
        {
            public readonly float Threshold;
            public readonly bool IsAscending;

            public ResourceThresholdInfo(float threshold, bool isAscending)
            {
                Threshold = threshold;
                IsAscending = isAscending;
            }
        }

        public ResourceThresholdCrossedEvent(string uniqueId, GameObject source, float threshold, bool isAscending, string actorId)
        {
            UniqueId = uniqueId;
            Source = source;
            Info = new ResourceThresholdInfo(threshold, isAscending);
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
    /// Evento disparado quando um modificador é aplicado ou removido.
    /// </summary>
    public class ModifierAppliedEvent : IEvent
    {
        public readonly string UniqueId;
        public readonly GameObject Source;
        public readonly ResourceModifier Modifier;
        public readonly bool IsApplied;
        public readonly string ActorId;

        public ModifierAppliedEvent(string uniqueId, GameObject source, ResourceModifier modifier, bool isApplied, string actorId)
        {
            UniqueId = uniqueId;
            Source = source;
            Modifier = modifier;
            IsApplied = isApplied;
            ActorId = actorId;
        }
    }
}