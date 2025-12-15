using System.Collections.Generic;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Values;

namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation
{
    public interface IRuntimeAttributeCanvasBinder : IInjectableComponent
    {
        string CanvasId { get; }
        AttributeCanvasType Type { get; }
        AttributeCanvasInitializationState State { get; }

        void ScheduleBind(string actorId, RuntimeAttributeType runtimeAttributeType, IRuntimeAttributeValue data);
        bool CanAcceptBinds();
        IReadOnlyDictionary<string, Dictionary<RuntimeAttributeType, RuntimeAttributeUISlot>> GetActorSlots();
    }

    public interface IInjectableComponent
    {
        string GetObjectId();
        void OnDependenciesInjected();
        DependencyInjectionState InjectionState { get; set; }
    }

    public enum DependencyInjectionState
    {
        Pending,
        Injecting,
        Ready,
        Failed
    }

    public enum AttributeCanvasTargetMode
    {
        Default, // "MainUI"
        ActorSpecific, // "{actorId}_Canvas"
        Custom // customCanvasId
    }

    public enum AttributeCanvasType { Scene, Dynamic }
    public enum AttributeCanvasInitializationState { Pending, Injecting, Ready, Failed }
}
