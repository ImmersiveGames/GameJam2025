using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [DebugLevel(DebugLevel.Logs)]
    public static class SensorFilteredEventBus
    {
        private static readonly Dictionary<object, IEventBinding<SensorDetectedEvent>> _detectedBindings = new();
        private static readonly Dictionary<object, IEventBinding<SensorLostEvent>> _lostBindings = new();

        public static void RegisterDetected(IEventBinding<SensorDetectedEvent> binding, SpawnPoint spawnPoint)
        {
            if (!_detectedBindings.TryAdd(spawnPoint, binding))
            {
                DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Listener já registrado para SensorDetectedEvent por '{spawnPoint.name}'. Ignorando.", "yellow");
                return;
            }
            EventBus<SensorDetectedEvent>.Register((EventBinding<SensorDetectedEvent>)binding);
            DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Registrado listener para SensorDetectedEvent por '{spawnPoint.name}'. Total de listeners: {_detectedBindings.Count}.", "blue");
        }

        public static void RegisterLost(IEventBinding<SensorLostEvent> binding, SpawnPoint spawnPoint)
        {
            if (!_lostBindings.TryAdd(spawnPoint, binding))
            {
                DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Listener já registrado para SensorLostEvent por '{spawnPoint.name}'. Ignorando.", "yellow");
                return;
            }
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
                else
                {
                    DebugUtility.LogWarning(typeof(SensorFilteredEventBus),$"Binding para SensorDetectedEvent de '{spawnPoint.name}' não é do tipo EventBinding<SensorDetectedEvent>.", null);
                }
            }
            if (!_lostBindings.Remove(spawnPoint, out IEventBinding<SensorLostEvent> lostBinding)) return;
            {
                if (lostBinding is EventBinding<SensorLostEvent> eventBinding)
                {
                    EventBus<SensorLostEvent>.Unregister(eventBinding);
                    DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Removido listener para SensorLostEvent por '{spawnPoint.name}'. Total de listeners: {_lostBindings.Count}.", "blue");
                }
                else
                {
                    DebugUtility.LogWarning(typeof(SensorFilteredEventBus),$"Binding para SensorLostEvent de '{spawnPoint.name}' não é do tipo EventBinding<SensorLostEvent>.", null);
                }
            }
        }

        public static void RaiseFiltered(SensorDetectedEvent evt)
        {
            DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Disparando evento filtrado SensorDetectedEvent para SensorName={evt.SensorName}, Planet={evt.Planet.Detectable.Name}.", "cyan");
            foreach (KeyValuePair<object, IEventBinding<SensorDetectedEvent>> pair in _detectedBindings)
            {
                var spawnPoint = pair.Key as SpawnPoint;
                if (spawnPoint != null &&
                    (evt.Planet.Detectable.Transform.gameObject == spawnPoint.gameObject ||
                     evt.Planet.Detectable.Transform.gameObject.GetComponentInParent<SpawnPoint>() == spawnPoint ||
                     evt.Planet.Detectable.Transform.gameObject.GetComponentInChildren<SpawnPoint>() == spawnPoint))
                {
                    pair.Value.OnEvent?.Invoke(evt);
                    pair.Value.OnEventNoArgs?.Invoke();
                    DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Evento SensorDetectedEvent enviado para '{spawnPoint.name}' (Planet={evt.Planet.Detectable.Name}).", "cyan");
                }
                else
                {
                    DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Evento SensorDetectedEvent ignorado para '{spawnPoint?.name}' (Planet={evt.Planet.Detectable.Name}).", "yellow");
                }
            }
        }

        public static void RaiseFiltered(SensorLostEvent evt)
        {
            DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Disparando evento filtrado SensorLostEvent para SensorName={evt.SensorName}, Planet={evt.Planet.Detectable.Name}.", "cyan");
            foreach (KeyValuePair<object, IEventBinding<SensorLostEvent>> pair in _lostBindings)
            {
                var spawnPoint = pair.Key as SpawnPoint;
                if (spawnPoint != null &&
                    (evt.Planet.Detectable.Transform.gameObject == spawnPoint.gameObject ||
                     evt.Planet.Detectable.Transform.gameObject.GetComponentInParent<SpawnPoint>() == spawnPoint ||
                     evt.Planet.Detectable.Transform.gameObject.GetComponentInChildren<SpawnPoint>() == spawnPoint))
                {
                    pair.Value.OnEvent?.Invoke(evt);
                    pair.Value.OnEventNoArgs?.Invoke();
                    DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Evento SensorLostEvent enviado para '{spawnPoint.name}' (Planet={evt.Planet.Detectable.Name}).", "cyan");
                }
                else
                {
                    DebugUtility.LogVerbose(typeof(SensorFilteredEventBus),$"Evento SensorLostEvent ignorado para '{spawnPoint?.name}' (Planet={evt.Planet.Detectable.Name}).", "yellow");
                }
            }
        }
    }
}