using System;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Debug
{
    /// <summary>
    /// Listener simples que apenas registra o término da geração inicial de planetas.
    /// </summary>
    [DefaultExecutionOrder(-79), DebugLevel(DebugLevel.Logs)]
    public class PlanetsInitializationLogger : MonoBehaviour
    {
        private EventBinding<PlanetsInitializationCompletedEvent> _initializationBinding;

        private void OnEnable()
        {
            _initializationBinding = new EventBinding<PlanetsInitializationCompletedEvent>(OnPlanetsInitialized);
            EventBus<PlanetsInitializationCompletedEvent>.Register(_initializationBinding);
        }

        private void OnDisable()
        {
            if (_initializationBinding != null)
            {
                EventBus<PlanetsInitializationCompletedEvent>.Unregister(_initializationBinding);
            }
        }

        private void OnPlanetsInitialized(PlanetsInitializationCompletedEvent evt)
        {
            var planets = evt.PlanetActors ?? Array.Empty<IPlanetActor>();
            if (planets.Count == 0)
            {
                DebugUtility.Log<PlanetsInitializationLogger>("Nenhum planeta foi instanciado neste ciclo inicial.");
                return;
            }

            string planetNames = string.Join(", ", planets
                .Where(p => p?.PlanetActor != null)
                .Select(p => p.PlanetActor.ActorName));

            DebugUtility.Log<PlanetsInitializationLogger>($"Total de planetas: {planets.Count}. Objetos: {planetNames}.");
        }
    }
}
