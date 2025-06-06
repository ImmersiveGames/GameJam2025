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
        public event Action<PlanetsMaster> OnEatPlanet;
        public event Action<Transform> OnTargetUpdated;
        
        private PlanetsMaster _targetPlanetMaster;
        private bool _isEating;
        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;

        private void Awake()
        {
            _isEating = false;
            _targetPlanetMaster = null;
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

        public void OnPlanetDetected(PlanetsMaster planetMaster)
        {
            DebugUtility.LogVerbose<EaterDetectable>($"Planeta detectado pelo Eater: {planetMaster.name}", "green");

            if (!PlanetsManager.Instance.IsMarkedPlanet(planetMaster) || _isEating) return;
            DebugUtility.LogVerbose<EaterDetectable>($"Planeta Elegível para comer: {planetMaster.name}", "green");
            var motion = planetMaster.GetComponent<PlanetMotion>();
            motion?.PauseOrbit();

            _isEating = true;
            _targetPlanetMaster = planetMaster;
            OnEatPlanet?.Invoke(planetMaster);
            EventBus<PlanetConsumedEvent>.Raise(new PlanetConsumedEvent(planetMaster));
            DebugUtility.LogVerbose<EaterDetectable>($"Eater iniciou consumo do planeta: {planetMaster.name}", "magenta");
        }

        public void OnPlanetLost(PlanetsMaster planetMaster)
        {
            DebugUtility.LogVerbose<EaterDetectable>($"Planeta perdido: {planetMaster.name}", "red");

            var motion = planetMaster.GetComponent<PlanetMotion>();
            motion?.ResumeOrbit();
        }

        public void OnRecognitionRangeEntered(PlanetsMaster planetMaster, PlanetResourcesSo resources)
        {
            DebugUtility.LogVerbose<EaterDetectable>($"Reconheceu planeta: {planetMaster.name}, Recursos: {resources?.name ?? "nenhum"}", "blue");
        }

        private void OnPlanetMarked(PlanetMarkedEvent evt)
        {
            _targetPlanetMaster = evt.PlanetMaster;
            _isEating = false;
            OnTargetUpdated?.Invoke(_targetPlanetMaster?.transform);
            DebugUtility.LogVerbose<EaterDetectable>($"Novo alvo recebido: {_targetPlanetMaster?.name ?? "nulo"}", "yellow");
        }

        private void OnPlanetUnmarked(PlanetUnmarkedEvent evt)
        {
            if (_targetPlanetMaster != evt.PlanetMaster) return;
            _targetPlanetMaster = null;
            _isEating = false;
            OnTargetUpdated?.Invoke(null);
            DebugUtility.LogVerbose<EaterDetectable>($"Alvo removido: {evt.PlanetMaster.name}", "red");
        }

        public void ResetEatingState()
        {
            _isEating = false;
            DebugUtility.LogVerbose<EaterDetectable>("Estado de comer resetado.");
        }
    }
}