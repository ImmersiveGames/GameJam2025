#nullable enable
using _ImmersiveGames.Scripts.Utils.DebugSystems;
namespace _ImmersiveGames.Scripts.Utils.BusEventSystems {
    [DebugLevel(DebugLevel.Logs)]
    public static class EventBus<T> where T : IEvent {
        // fallback internal bus
        private static readonly InjectableEventBus<T> _internalBus = new();
        // optional injected bus (registered at bootstrap if desired)
        public static IEventBus<T> GlobalBus { get; set; } = _internalBus;
    
        public static void Register(EventBinding<T> binding) => GlobalBus.Register(binding);
        public static void Unregister(EventBinding<T> binding) => GlobalBus.Unregister(binding);

        public static void Raise(T @event) => GlobalBus.Raise(@event);
        public static void Clear() => GlobalBus.Clear();
    }
    
}