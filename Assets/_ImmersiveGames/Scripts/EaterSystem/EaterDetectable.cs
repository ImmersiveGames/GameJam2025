using System;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterDetectable : MonoBehaviour, IDetectable
    {
        public event Action<Planets> OnEatPlanet;
        public event Action<Transform> OnTargetUpdated;
        
        private Planets _targetPlanet;
        private bool _isEating;
        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;

        private void Awake()
        {
            _isEating = false;
            _targetPlanet = null;
        }
        private void OnEnable()
        {   
            _planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(OnPlanetMarked);
            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(OnPlanetUnmarked);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
        }

        public void OnPlanetDetected(Planets planet)
        {
            DebugUtility.LogVerbose<EaterDetectable>($"Planeta detectado pelo Eater: {planet.name}", "green");

            if (!PlanetsManager.Instance.IsMarkedPlanet(planet) || _isEating) return;
            DebugUtility.LogVerbose<EaterDetectable>($"Planeta Elegível para comer: {planet.name}", "green");
            var motion = planet.GetComponent<PlanetMotion>();
            motion?.PauseOrbit();

            _isEating = true;
            _targetPlanet = planet;
            OnEatPlanet?.Invoke(planet);
            EventBus<PlanetConsumedEvent>.Raise(new PlanetConsumedEvent(planet));
            DebugUtility.LogVerbose<EaterDetectable>($"Eater iniciou consumo do planeta: {planet.name}", "magenta");
        }

        public void OnPlanetLost(Planets planet)
        {
            DebugUtility.LogVerbose<EaterDetectable>($"Planeta perdido: {planet.name}", "red");

            var motion = planet.GetComponent<PlanetMotion>();
            motion?.ResumeOrbit();
        }

        public void OnRecognitionRangeEntered(Planets planet, PlanetResourcesSo resources)
        {
            DebugUtility.LogVerbose<EaterDetectable>($"Reconheceu planeta: {planet.name}, Recursos: {resources?.name ?? "nenhum"}", "blue");
        }

        private void OnPlanetMarked(PlanetMarkedEvent evt)
        {
            _targetPlanet = evt.Planet;
            _isEating = false;
            OnTargetUpdated?.Invoke(_targetPlanet?.transform);
            DebugUtility.LogVerbose<EaterDetectable>($"Novo alvo recebido: {_targetPlanet?.name ?? "nulo"}", "yellow");
        }

        private void OnPlanetUnmarked(PlanetUnmarkedEvent evt)
        {
            if (_targetPlanet != evt.Planet) return;
            _targetPlanet = null;
            _isEating = false;
            OnTargetUpdated?.Invoke(null);
            DebugUtility.LogVerbose<EaterDetectable>($"Alvo removido: {evt.Planet.name}", "red");
        }

        public void ResetEatingState()
        {
            _isEating = false;
            DebugUtility.LogVerbose<EaterDetectable>("Estado de comer resetado.");
        }
    }
}