using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.SpawnSystems.New;

namespace _ImmersiveGames.Scripts.SpawnSystems.Events
{
    [DebugLevel(DebugLevel.Logs)]
    public static class OrbitFilteredEventBus
    {
        private static readonly Dictionary<object, IEventBinding<OrbitsSpawnedEvent>> _bindings = new();

        public static void Register(IEventBinding<OrbitsSpawnedEvent> binding, SpawnSystem spawnSystem)
        {
            if (spawnSystem == null)
            {
                DebugUtility.LogWarning(typeof(OrbitFilteredEventBus), "Tentativa de registrar listener com SpawnSystem nulo. Ignorando.", null);
                return;
            }

            if (!_bindings.TryAdd(spawnSystem, binding))
            {
                DebugUtility.LogVerbose(typeof(OrbitFilteredEventBus), $"Listener já registrado para SpawnSystem '{spawnSystem.name}'. Ignorando.", "yellow");
                return;
            }

            EventBus<OrbitsSpawnedEvent>.Register((EventBinding<OrbitsSpawnedEvent>)binding);
            DebugUtility.LogVerbose(typeof(OrbitFilteredEventBus), $"Registrado listener para OrbitsSpawnedEvent por '{spawnSystem.name}'. Total de listeners: {_bindings.Count}.", "blue");
        }

        public static void Unregister(SpawnSystem spawnSystem)
        {
            if (_bindings.Remove(spawnSystem, out IEventBinding<OrbitsSpawnedEvent> binding))
            {
                if (binding is EventBinding<OrbitsSpawnedEvent> eventBinding)
                {
                    EventBus<OrbitsSpawnedEvent>.Unregister(eventBinding);
                    DebugUtility.LogVerbose(typeof(OrbitFilteredEventBus), $"Removido listener para OrbitsSpawnedEvent por '{spawnSystem.name}'. Total de listeners: {_bindings.Count}.", "blue");
                }
            }
        }

        public static void RaiseFiltered(OrbitsSpawnedEvent evt)
        {
            if (evt.SpawnSystem == null)
            {
                DebugUtility.LogWarning(typeof(OrbitFilteredEventBus), "OrbitsSpawnedEvent com SpawnSystem nulo. Ignorando.", null);
                return;
            }

            if (_bindings.TryGetValue(evt.SpawnSystem, out IEventBinding<OrbitsSpawnedEvent> binding))
            {
                binding.OnEvent?.Invoke(evt);
                binding.OnEventNoArgs?.Invoke();
                DebugUtility.LogVerbose(typeof(OrbitFilteredEventBus), $"Evento OrbitsSpawnedEvent enviado para '{evt.SpawnSystem.name}'.", "cyan");
            }
            else
            {
                DebugUtility.LogVerbose(typeof(OrbitFilteredEventBus), $"Nenhum listener registrado para SpawnSystem '{evt.SpawnSystem.name}'.", "yellow");
            }
        }
    }
}