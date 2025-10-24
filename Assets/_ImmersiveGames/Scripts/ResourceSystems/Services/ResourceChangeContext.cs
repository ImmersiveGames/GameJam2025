using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    /// <summary>
    /// Contexto enviado sempre que um recurso está prestes a ser modificado ou já foi alterado.
    /// </summary>
    public readonly struct ResourceChangeContext
    {
        public ResourceSystem ResourceSystem { get; }
        public ResourceType ResourceType { get; }
        public float PreviousValue { get; }
        public float NewValue { get; }
        public float Delta { get; }
        public float MaxValue { get; }
        public ResourceChangeSource Source { get; }
        public bool IsLinkedChange { get; }

        public bool IsIncrease => Delta > 0f;
        public bool IsDecrease => Delta < 0f;
        public bool ReachedMax => NewValue >= MaxValue - 0.0001f || Mathf.Approximately(NewValue, MaxValue);

        public ResourceChangeContext(
            ResourceSystem resourceSystem,
            ResourceType resourceType,
            float previousValue,
            float newValue,
            float delta,
            float maxValue,
            ResourceChangeSource source,
            bool isLinkedChange)
        {
            ResourceSystem = resourceSystem;
            ResourceType = resourceType;
            PreviousValue = previousValue;
            NewValue = newValue;
            Delta = delta;
            MaxValue = maxValue;
            Source = source;
            IsLinkedChange = isLinkedChange;
        }
    }

    /// <summary>
    /// Origem da modificação aplicada ao recurso.
    /// </summary>
    public enum ResourceChangeSource
    {
        Manual = 0,
        AutoFlow = 1,
        Link = 2,
        External = 3
    }
}
