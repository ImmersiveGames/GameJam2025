using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Configs;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Services
{
    /// <summary>
    /// Contexto enviado sempre que um recurso está prestes a ser modificado ou já foi alterado.
    /// </summary>
    public readonly struct RuntimeAttributeChangeContext
    {
        public RuntimeAttributeContext RuntimeAttributeContext { get; }
        public RuntimeAttributeType RuntimeAttributeType { get; }
        public float PreviousValue { get; }
        public float NewValue { get; }
        public float Delta { get; }
        public float MaxValue { get; }
        public RuntimeAttributeChangeSource Source { get; }
        public bool IsLinkedChange { get; }

        public bool IsIncrease => Delta > 0f;
        public bool IsDecrease => Delta < 0f;
        public bool ReachedMax => NewValue >= MaxValue - 0.0001f || Mathf.Approximately(NewValue, MaxValue);

        public RuntimeAttributeChangeContext(
            RuntimeAttributeContext runtimeAttributeContext,
            RuntimeAttributeType runtimeAttributeType,
            float previousValue,
            float newValue,
            float delta,
            float maxValue,
            RuntimeAttributeChangeSource source,
            bool isLinkedChange)
        {
            RuntimeAttributeContext = runtimeAttributeContext;
            RuntimeAttributeType = runtimeAttributeType;
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
    public enum RuntimeAttributeChangeSource
    {
        Manual = 0,
        AutoFlow = 1,
        Link = 2,
        External = 3
    }
}
