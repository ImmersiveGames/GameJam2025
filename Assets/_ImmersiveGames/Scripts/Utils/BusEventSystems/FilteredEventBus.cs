using System.Collections.Generic;

namespace _ImmersiveGames.Scripts.Utils.BusEventSystems
{
    public static class FilteredEventBus<T> where T : IEvent
    {
        private static readonly Dictionary<object, IEventBinding<T>> _bindings = new();

        public static void Register(IEventBinding<T> binding, object scope)
        {
            if (!_bindings.TryAdd(scope, binding)) return;

            EventBus<T>.Register((EventBinding<T>)binding);
        }

        public static void Unregister(object scope)
        {
            if (_bindings.TryGetValue(scope, out IEventBinding<T> binding))
            {
                EventBus<T>.Unregister((EventBinding<T>)binding);
                _bindings.Remove(scope);
            }
        }

        public static void RaiseFiltered(T evt, object targetScope)
        {
            if (_bindings.TryGetValue(targetScope, out IEventBinding<T> binding))
            {
                binding.OnEvent?.Invoke(evt);
                binding.OnEventNoArgs?.Invoke();
            }
        }

        public static void Clear()
        {
            foreach (IEventBinding<T> binding in _bindings.Values)
                EventBus<T>.Unregister((EventBinding<T>)binding);

            _bindings.Clear();
        }
    }
}