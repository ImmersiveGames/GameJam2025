using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
namespace _ImmersiveGames.Scripts.Utils.BusEventSystems {
    [DebugLevel(DebugLevel.Logs)]
    public static class EventBus<T> where T : IEvent {
        private static readonly HashSet<IEventBinding<T>> _bindings = new();
    
        public static void Register(EventBinding<T> binding) => _bindings.Add(binding);
        public static void Unregister(EventBinding<T> binding) => _bindings.Remove(binding);

        public static void Raise(T @event) {
            var snapshot = new HashSet<IEventBinding<T>>(_bindings);
            foreach (var binding in snapshot.Where(binding => _bindings.Contains(binding))) {
                binding.OnEvent.Invoke(@event);
                binding.OnEventNoArgs.Invoke();
            }
        }

        private static void Clear() {
            _bindings.Clear();
        }
    }
}