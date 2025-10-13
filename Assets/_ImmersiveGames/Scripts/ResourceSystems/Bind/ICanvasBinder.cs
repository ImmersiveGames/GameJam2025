using _ImmersiveGames.Scripts.ResourceSystems.Configs;
namespace _ImmersiveGames.Scripts.ResourceSystems.Bind {
    public interface ICanvasBinder : IInjectableComponent
    {
        string CanvasId { get; }
        CanvasType Type { get; }
        CanvasInitializationState State { get; }
    
        void ScheduleBind(string actorId, ResourceType resourceType, IResourceValue data);
        bool CanAcceptBinds();
        void ForceReady(); // Para debugging
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
        Default,       // "MainUI"
        ActorSpecific, // "{actorId}_Canvas"
        Custom         // customCanvasId
    }
    
    public enum CanvasType { Scene, Dynamic }
    public enum CanvasInitializationState { Pending, Injecting, Ready, Failed }
}