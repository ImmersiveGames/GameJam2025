using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Core.Events
{
    /// <summary>
    /// Utilitário de limpeza/gerenciamento do EventBus no NewScripts (sem dependências do legado).
    /// </summary>
    public static class EventBusUtil
    {
        private static readonly HashSet<Type> _eventTypes = new();
        private static readonly HashSet<(Type Scope, Type Event)> _filteredEventTypes = new();

        public static IReadOnlyCollection<Type> EventTypes => _eventTypes;

        internal static void RegisterEventType(Type eventType)
        {
            if (eventType != null)
            {
                _eventTypes.Add(eventType);
            }
        }

        internal static void RegisterFilteredEventType(Type scopeType, Type eventType)
        {
            if (scopeType != null && eventType != null)
            {
                _filteredEventTypes.Add((scopeType, eventType));
            }
        }

        /// <summary>
        /// Limpa todos os buses conhecidos (EventBus e FilteredEventBus).
        /// </summary>
        public static void ClearAllBuses()
        {
            ClearEventBuses();
            ClearFilteredEventBuses();
        }

        /// <summary>
        /// Limpa um escopo específico de FilteredEventBus.
        /// </summary>
        public static void ClearFilteredScope<TScope, TEvent>(TScope scope)
        {
            FilteredEventBus<TScope, TEvent>.Clear(scope);
        }

        private static void ClearEventBuses()
        {
            foreach (var eventType in _eventTypes)
            {
                try
                {
                    var busType = typeof(EventBus<>).MakeGenericType(eventType);
                    var clearMethod = busType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
                    clearMethod?.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private static void ClearFilteredEventBuses()
        {
            foreach (var (scope, evt) in _filteredEventTypes)
            {
                try
                {
                    var busType = typeof(FilteredEventBus<,>).MakeGenericType(scope, evt);
                    var clearAllMethod = busType.GetMethod("ClearAll", BindingFlags.Static | BindingFlags.Public);
                    clearAllMethod?.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
