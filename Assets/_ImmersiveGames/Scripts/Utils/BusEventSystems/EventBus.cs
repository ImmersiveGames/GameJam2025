#nullable enable
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.BusEventSystems {
    [DebugLevel(DebugLevel.Verbose)]
    public static class EventBus<T> where T : IEvent {
        private static readonly HashSet<IEventBinding<T>> _bindings = new();
    
        public static void Register(EventBinding<T> binding)
        {
            _bindings.Add(binding);
            DebugUtility.LogVerbose(typeof(T),$"🔊 Registered listener for {typeof(T).Name}. Total: {_bindings.Count}");
        }
        public static void Unregister(EventBinding<T> binding) => _bindings.Remove(binding);

        public static void Raise(T @event) {
            var snapshot = new HashSet<IEventBinding<T>>(_bindings);
            foreach (var binding in snapshot.Where(binding => _bindings.Contains(binding))) {
                binding.OnEvent.Invoke(@event);
                binding.OnEventNoArgs.Invoke();
            }
            DebugUtility.LogVerbose(typeof(T),$"✅ Event {typeof(T).Name} processed by {_bindings.Count} listeners");
        }
        public static void Clear() {
            _bindings.Clear();
        }
    }
    
}