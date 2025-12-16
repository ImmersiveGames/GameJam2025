namespace _ImmersiveGames.Scripts.Utils.BusEventSystems {
    public interface IEvent { }
    public interface IEventBus<T> where T : IEvent
    {
        void Register(EventBinding<T> binding);
        void Unregister(EventBinding<T> binding);
        void Raise(T evt);
        void Clear();
    }
}