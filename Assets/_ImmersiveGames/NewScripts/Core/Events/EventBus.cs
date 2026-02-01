#nullable enable

namespace _ImmersiveGames.NewScripts.Core.Events
{
    public static class EventBus<T>
    {
        private static readonly InjectableEventBus<T> InternalBus = new();

        public static IEventBus<T> GlobalBus { get; set; } = InternalBus;

        static EventBus()
        {
            EventBusUtil.RegisterEventType(typeof(T));
        }

        public static void Register(EventBinding<T> binding) => GlobalBus.Register(binding);
        public static void Unregister(EventBinding<T> binding) => GlobalBus.Unregister(binding);
        public static void Raise(T @event) => GlobalBus.Raise(@event);
        public static void Clear() => GlobalBus.Clear();
    }
}
