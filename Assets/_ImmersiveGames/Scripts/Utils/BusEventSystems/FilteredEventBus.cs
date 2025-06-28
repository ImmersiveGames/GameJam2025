using System.Collections.Generic;
using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
namespace _ImmersiveGames.Scripts.Utils.BusEventSystems
{
    [DebugLevel(DebugLevel.Logs)]
    public static class FilteredEventBus
    {
        private static readonly Dictionary<object, IEventBinding<SpawnRequestEvent>> _spawnRequestBindings = new();

        public static void Register(IEventBinding<SpawnRequestEvent> binding, SpawnPoint spawnPoint)
        {
            if (!_spawnRequestBindings.TryAdd(spawnPoint, binding))
            {
                DebugUtility.LogVerbose(typeof(FilteredEventBus),$"Listener já registrado para SpawnRequestEvent por '{spawnPoint.name}'. Ignorando.", "yellow");
                return;
            }
            EventBus<SpawnRequestEvent>.Register((EventBinding<SpawnRequestEvent>)binding);
            DebugUtility.LogVerbose(typeof(FilteredEventBus),$"Registrado listener para SpawnRequestEvent por '{spawnPoint.name}'. Total de listeners: {_spawnRequestBindings.Count}.", "blue");
        }

        public static void Unregister(SpawnPoint spawnPoint)
        {
            if (_spawnRequestBindings.Remove(spawnPoint, out IEventBinding<SpawnRequestEvent> binding))
            {
                if (binding is EventBinding<SpawnRequestEvent> eventBinding)
                {
                    EventBus<SpawnRequestEvent>.Unregister(eventBinding);
                    DebugUtility.LogVerbose(typeof(FilteredEventBus),$"Removido listener para SpawnRequestEvent por '{spawnPoint.name}'. Total de listeners: {_spawnRequestBindings.Count}.", "blue");
                }
                else
                {
                    DebugUtility.LogWarning(typeof(FilteredEventBus),$"Binding para '{spawnPoint.name}' não é do tipo EventBinding<SpawnRequestEvent>.");
                }
            }
            else
            {
                DebugUtility.LogVerbose(typeof(FilteredEventBus),$"Nenhum listener encontrado para '{spawnPoint.name}'.", "yellow");
            }
        }

        public static void RaiseFiltered(SpawnRequestEvent evt)
        {
            DebugUtility.LogVerbose(typeof(FilteredEventBus),$"Disparando evento filtrado SpawnRequestEvent para PoolKey={evt.PoolKey}, Source={evt.SourceGameObject?.name}.", "cyan");
            foreach (KeyValuePair<object, IEventBinding<SpawnRequestEvent>> pair in _spawnRequestBindings)
            {
                var spawnPoint = pair.Key as SpawnPoint;
                if (spawnPoint != null && spawnPoint.GetPoolKey() == evt.PoolKey && spawnPoint.gameObject == evt.SourceGameObject)
                {
                    pair.Value.OnEvent?.Invoke(evt);
                    pair.Value.OnEventNoArgs?.Invoke();
                    DebugUtility.LogVerbose(typeof(FilteredEventBus),$"Evento enviado para '{spawnPoint.name}'.", "cyan");
                }
            }
        }
    }
}