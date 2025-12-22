using System;
using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Events
{
    public class InjectableEventBus<T> : IEventBus<T>
    {
        private readonly HashSet<EventBinding<T>> _bindings = new();

        public void Register(EventBinding<T> binding)
        {
            if (binding == null)
            {
                return;
            }

            _bindings.Add(binding);
        }

        public void Unregister(EventBinding<T> binding)
        {
            if (binding == null)
            {
                return;
            }

            _bindings.Remove(binding);
        }

        public void Raise(T evt)
        {
            if (evt == null)
            {
                return;
            }

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
                        Debug.LogException(ex);
                    }
                }
            }
        }

        public void Clear() => _bindings.Clear();
    }
}
