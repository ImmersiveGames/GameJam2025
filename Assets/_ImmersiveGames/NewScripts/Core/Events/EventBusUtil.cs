using System;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Core.Events
{
    /// <summary>
    /// Utilitário de limpeza/gerenciamento do EventBus no NewScripts (sem dependências do legado).
    /// </summary>
    public static class EventBusUtil
    {
        private static readonly HashSet<Type> _eventTypes = new();
        private static readonly HashSet<(Type Scope, Type Event)> _filteredEventTypes = new();

        public static IReadOnlyCollection<Type> EventTypes => _eventTypes;

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void InitializeEditor()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                ClearAllBuses();
            }
        }
#endif

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
            foreach (Type eventType in _eventTypes)
            {
                try
                {
                    Type busType = typeof(EventBus<>).MakeGenericType(eventType);
                    MethodInfo clearMethod = busType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
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
            foreach ((Type scope, Type evt) in _filteredEventTypes)
            {
                try
                {
                    Type busType = typeof(FilteredEventBus<,>).MakeGenericType(scope, evt);
                    MethodInfo clearAllMethod = busType.GetMethod("ClearAll", BindingFlags.Static | BindingFlags.Public);
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
