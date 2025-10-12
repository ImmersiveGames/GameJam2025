namespace _ImmersiveGames.Scripts.ResourceSystems.Bind {
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
}