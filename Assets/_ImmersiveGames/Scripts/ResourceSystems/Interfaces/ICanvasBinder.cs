using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public interface ICanvasBinder : IInjectableComponent
    {
        string CanvasId { get; }
        CanvasType Type { get; }
        CanvasInitializationState State { get; }

        void ScheduleBind(string actorId, ResourceType resourceType, IResourceValue data);
        bool CanAcceptBinds();
        IReadOnlyDictionary<string, Dictionary<ResourceType, ResourceUISlot>> GetActorSlots();
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
    public enum CanvasTargetMode
    {
        Default, // "MainUI"
        ActorSpecific, // "{actorId}_Canvas"
        Custom // customCanvasId
    }

    public enum CanvasType { Scene, Dynamic }
    public enum CanvasInitializationState { Pending, Injecting, Ready, Failed }
}