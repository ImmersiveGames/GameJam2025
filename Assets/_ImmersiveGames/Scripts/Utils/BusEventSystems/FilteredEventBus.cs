using System.Collections.Generic;

namespace _ImmersiveGames.Scripts.Utils.BusEventSystems
{
    /// <summary>
    /// Permite registrar e disparar eventos com um "escopo" (ex: ActorId).
    /// Assim, apenas objetos vinculados ao mesmo escopo recebem o evento.
    /// </summary>
    public static class FilteredEventBus<T> where T : IEvent
    {
        // Agora cada escopo pode ter múltiplos bindings (ex: um ActorMaster + um UIListener)
        private static readonly Dictionary<object, List<IEventBinding<T>>> _bindingsByScope = new();

        /// <summary>
        /// Registra um binding no escopo especificado.
        /// </summary>
        public static void Register(IEventBinding<T> binding, object scope)
        {
            if (binding == null || scope == null)
                return;

            if (!_bindingsByScope.TryGetValue(scope, out var list))
            {
                list = new List<IEventBinding<T>>();
                _bindingsByScope[scope] = list;
            }

            if (list.Contains(binding))
                return;

            list.Add(binding);
            EventBus<T>.Register((EventBinding<T>)binding);
        }

        /// <summary>
        /// Remove um binding específico de um escopo.
        /// </summary>
        public static void Unregister(IEventBinding<T> binding, object scope)
        {
            if (binding == null || scope == null)
                return;

            if (_bindingsByScope.TryGetValue(scope, out var list))
            {
                if (list.Remove(binding))
                    EventBus<T>.Unregister((EventBinding<T>)binding);

                if (list.Count == 0)
                    _bindingsByScope.Remove(scope);
            }
        }

        /// <summary>
        /// Remove todos os bindings de um determinado escopo.
        /// </summary>
        public static void Unregister(object scope)
        {
            if (scope == null)
                return;

            if (_bindingsByScope.TryGetValue(scope, out var list))
            {
                foreach (var binding in list)
                    EventBus<T>.Unregister((EventBinding<T>)binding);

                _bindingsByScope.Remove(scope);
            }
        }

        /// <summary>
        /// Envia um evento apenas para os bindings registrados sob o escopo alvo.
        /// </summary>
        public static void RaiseFiltered(T evt, object targetScope)
        {
            if (targetScope == null)
                return;

            if (_bindingsByScope.TryGetValue(targetScope, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var binding = list[i];
                    binding.OnEvent?.Invoke(evt);
                    binding.OnEventNoArgs?.Invoke();
                }
            }
        }

        /// <summary>
        /// Limpa todos os escopos e bindings registrados.
        /// </summary>
        public static void Clear()
        {
            foreach (var list in _bindingsByScope.Values)
            {
                foreach (var binding in list)
                    EventBus<T>.Unregister((EventBinding<T>)binding);
            }

            _bindingsByScope.Clear();
        }
    }
}
