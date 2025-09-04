using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
namespace _ImmersiveGames.Scripts.SpawnSystems.Events
{
    [DebugLevel(DebugLevel.Logs)]
    public static class SensorFilteredEventBus
    {
        private static readonly Dictionary<object, IEventBinding<SensorDetectedEvent>> _detectedBindings = new();
        private static readonly Dictionary<object, IEventBinding<SensorLostEvent>> _lostBindings = new();

        public static void RegisterDetected(IEventBinding<SensorDetectedEvent> binding, SpawnPoint spawnPoint)
        {
            if (!_detectedBindings.TryAdd(spawnPoint, binding))
                return;
            EventBus<SensorDetectedEvent>.Register((EventBinding<SensorDetectedEvent>)binding);
            DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Registrado listener para SensorDetectedEvent por '{spawnPoint.name}'. Total de listeners: {_detectedBindings.Count}.", "blue");
        }

        public static void RegisterLost(IEventBinding<SensorLostEvent> binding, SpawnPoint spawnPoint)
        {
            if (!_lostBindings.TryAdd(spawnPoint, binding))
                return;
            EventBus<SensorLostEvent>.Register((EventBinding<SensorLostEvent>)binding);
            DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Registrado listener para SensorLostEvent por '{spawnPoint.name}'. Total de listeners: {_lostBindings.Count}.", "blue");
        }

        public static void Unregister(SpawnPoint spawnPoint)
        {
            if (_detectedBindings.Remove(spawnPoint, out IEventBinding<SensorDetectedEvent> detectedBinding))
            {
                if (detectedBinding is EventBinding<SensorDetectedEvent> eventBinding)
                {
                    EventBus<SensorDetectedEvent>.Unregister(eventBinding);
                    DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Removido listener para SensorDetectedEvent por '{spawnPoint.name}'. Total de listeners: {_detectedBindings.Count}.", "blue");
                }
            }
            if (!_lostBindings.Remove(spawnPoint, out IEventBinding<SensorLostEvent> lostBinding)) return;
            {
                if (lostBinding is not EventBinding<SensorLostEvent> eventBinding) return;
                EventBus<SensorLostEvent>.Unregister(eventBinding);
                DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Removido listener para SensorLostEvent por '{spawnPoint.name}'. Total de listeners: {_lostBindings.Count}.", "blue");
            }
        }

        public static void RaiseFiltered(SensorDetectedEvent evt)
        {
            foreach (KeyValuePair<object, IEventBinding<SensorDetectedEvent>> pair in _detectedBindings)
            {
                var spawnPoint = pair.Key as SpawnPoint;
                if (spawnPoint == null ||
                    (evt.Planet.Detectable.Transform.gameObject != spawnPoint.gameObject &&
                        evt.Planet.Detectable.Transform.gameObject.GetComponentInParent<SpawnPoint>() != spawnPoint &&
                        evt.Planet.Detectable.Transform.gameObject.GetComponentInChildren<SpawnPoint>() != spawnPoint)) continue;
                pair.Value.OnEvent?.Invoke(evt);
                pair.Value.OnEventNoArgs?.Invoke();
                DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Evento SensorDetectedEvent enviado para '{spawnPoint.name}' (Planet={evt.Planet.Detectable.Name}).", "cyan");
            }
        }

        public static void RaiseFiltered(SensorLostEvent evt)
        {
            foreach (KeyValuePair<object, IEventBinding<SensorLostEvent>> pair in _lostBindings)
            {
                var spawnPoint = pair.Key as SpawnPoint;
                if (spawnPoint == null ||
                    (evt.Planet.Detectable.Transform.gameObject != spawnPoint.gameObject &&
                        evt.Planet.Detectable.Transform.gameObject.GetComponentInParent<SpawnPoint>() != spawnPoint &&
                        evt.Planet.Detectable.Transform.gameObject.GetComponentInChildren<SpawnPoint>() != spawnPoint)) continue;
                pair.Value.OnEvent?.Invoke(evt);
                pair.Value.OnEventNoArgs?.Invoke();
                DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Evento SensorLostEvent enviado para '{spawnPoint.name}' (Planet={evt.Planet.Detectable.Name}).", "cyan");
            }
        }
    }
}