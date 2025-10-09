using System;
using System.Collections.Generic;

namespace _ImmersiveGames.Scripts.Utils.BusEventSystems
{
    public class InjectableEventBus<T> : IEventBus<T> where T : IEvent
    {
        private readonly HashSet<EventBinding<T>> _bindings = new();

        public void Register(EventBinding<T> binding)
        {
            if (binding == null) return;
            _bindings.Add(binding);
        }

        public void Unregister(EventBinding<T> binding)
        {
            if (binding == null) return;
            _bindings.Remove(binding);
        }

        public void Raise(T evt)
        {
            if (evt == null) return;
            // snapshot for safety in case a binding set changes during iteration
            var snapshot = new List<EventBinding<T>>(_bindings);
            foreach (EventBinding<T> binding in snapshot)
            {
                if (_bindings.Contains(binding))
                {
                    try
                    {
                        binding.OnEvent?.Invoke(evt);
                        binding.OnEventNoArgs?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        // Don't let one handler break the bus
                        UnityEngine.Debug.LogException(ex);
                    }
                }
            }
        }

        public void Clear() => _bindings.Clear();
    }
}