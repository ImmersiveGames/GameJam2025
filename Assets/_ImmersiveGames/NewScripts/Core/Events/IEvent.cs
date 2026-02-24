namespace _ImmersiveGames.NewScripts.Core.Events
{
    public interface IEvent { }

    public interface IEventBus<T>
    {
        void Register(EventBinding<T> binding);
        void Unregister(EventBinding<T> binding);
        void Raise(T evt);
        void Clear();
    }
}
