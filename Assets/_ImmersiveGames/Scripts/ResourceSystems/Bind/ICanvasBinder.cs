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

    public enum CanvasType { Scene, Dynamic }
    public enum CanvasInitializationState { Pending, Injecting, Ready, Failed }
}