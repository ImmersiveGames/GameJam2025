using System;
using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Core.Events
{
    /// <summary>
    /// EventBus com filtragem por escopo (ex.: playerId).
    /// </summary>
    public static class FilteredEventBus<TScope, TEvent>
    {
        private static readonly Dictionary<TScope, HashSet<EventBinding<TEvent>>> BindingsByScope = new();

        static FilteredEventBus()
        {
            EventBusUtil.RegisterFilteredEventType(typeof(TScope), typeof(TEvent));
        }

        public static void Register(TScope scope, EventBinding<TEvent> binding)
        {
            if (binding == null || scope == null)
            {
                return;
            }

            if (!BindingsByScope.TryGetValue(scope, out HashSet<EventBinding<TEvent>> bindings))
            {
                bindings = new HashSet<EventBinding<TEvent>>();
                BindingsByScope[scope] = bindings;
            }

            bindings.Add(binding);
        }

        public static void Unregister(TScope scope, EventBinding<TEvent> binding)
        {
            if (binding == null || scope == null)
            {
                return;
            }

            if (BindingsByScope.TryGetValue(scope, out HashSet<EventBinding<TEvent>> bindings))
            {
                if (bindings.Remove(binding) && bindings.Count == 0)
                {
                    BindingsByScope.Remove(scope);
                }
            }
        }

        public static void Raise(TScope scope, TEvent evt)
        {
            if (scope == null || evt == null)
            {
                return;
            }

            if (!BindingsByScope.TryGetValue(scope, out HashSet<EventBinding<TEvent>> bindings) || bindings.Count == 0)
            {
                return;
            }

            var snapshot = new List<EventBinding<TEvent>>(bindings);
            foreach (EventBinding<TEvent> binding in snapshot)
            {
                if (!bindings.Contains(binding))
                {
                    continue;
                }

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

        public static void Clear(TScope scope)
        {
            if (scope == null)
            {
                return;
            }

            BindingsByScope.Remove(scope);
        }

        public static void ClearAll()
        {
            BindingsByScope.Clear();
        }
    }
}
