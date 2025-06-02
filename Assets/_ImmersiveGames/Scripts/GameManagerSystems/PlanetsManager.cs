using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    public class PlanetsManager : Singleton<PlanetsManager>
    {
        [SerializeField] private Planets targetToEater; // Configurado no Inspector
        
        public bool IsMarkedPlanet(Planets planet) => targetToEater == planet; // Compatibilidade

        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;

        private void OnEnable()
        {
            _planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(MarkPlanet);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);

            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(ClearMarkedPlanet);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
        }

        private void MarkPlanet(PlanetMarkedEvent evt)
        {
            if (targetToEater == evt.Planet) return;

            if (targetToEater != null)
            {
                EventBus<PlanetUnmarkedEvent>.Raise(new PlanetUnmarkedEvent(targetToEater));
            }

            targetToEater = evt.Planet; // Configura targetToEater aqui
            Debug.Log($"Planeta marcado: {evt.Planet.name}");
            // Não dispara PlanetMarkedEvent para evitar loop
        }

        private void ClearMarkedPlanet(PlanetUnmarkedEvent evt)
        {
            if (targetToEater == evt.Planet)
            {
                targetToEater = null; // Configura targetToEater como null aqui
                Debug.Log($"Planeta desmarcado: {evt.Planet.name}");
                // Não dispara PlanetUnmarkedEvent para evitar loop
            }
        }
    }
}