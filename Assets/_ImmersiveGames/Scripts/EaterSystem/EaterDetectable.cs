using System;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    public class EaterDetectable : MonoBehaviour, IDetectable
    {
        [SerializeField, Tooltip("Distância para iniciar o consumo do planeta")]
        private float eatDistance = 5f;

        public event Action<Planets> OnEatPlanet;
        public event Action<Transform> OnTargetUpdated;

        private Transform _self;
        private Planets _targetPlanet;
        private bool _isEating; // Controla se o Eater já começou a comer
        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;

        private void Awake()
        {
            _self = transform;
        }

        private void OnEnable()
        {
            _planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(OnPlanetMarked);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);

            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(OnPlanetUnmarked);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
        }

        private void Update()
        {
            if (_targetPlanet == null || !_targetPlanet.IsActive || _isEating) return;

            float distance = Vector3.Distance(
                new Vector3(_self.position.x, 0, _self.position.z),
                new Vector3(_targetPlanet.transform.position.x, 0, _targetPlanet.transform.position.z));

            if (distance <= eatDistance)
            {
                _isEating = true; // Impede disparos repetidos
                OnEatPlanet?.Invoke(_targetPlanet);
                DebugUtility.LogVerbose<EaterDetectable>($"EaterDetectable iniciou consumo do planeta: {_targetPlanet.name} (distância: {distance})", "magenta");
            }
        }

        private void OnPlanetMarked(PlanetMarkedEvent evt)
        {
            _targetPlanet = evt.Planet;
            _isEating = false; // Reseta ao marcar novo alvo
            OnTargetUpdated?.Invoke(_targetPlanet?.transform);
            DebugUtility.LogVerbose<EaterDetectable>($"Novo alvo recebido: {_targetPlanet?.name ?? "nulo"}", "yellow");
        }

        private void OnPlanetUnmarked(PlanetUnmarkedEvent evt)
        {
            if (_targetPlanet == evt.Planet)
            {
                _targetPlanet = null;
                _isEating = false;
                OnTargetUpdated?.Invoke(null);
                DebugUtility.LogVerbose<EaterDetectable>($"Alvo removido: {evt.Planet.name}", "red");
            }
        }

        public void OnPlanetDetected(Planets planet)
        {
            DebugUtility.LogVerbose<EaterDetectable>($"Planeta detectado: {planet.name}", "green");
            
            if (!PlanetsManager.Instance.IsMarkedPlanet(planet)) return;
            var motion = planet.GetComponent<PlanetMotion>();
            motion?.PauseOrbit();
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

        // Chamado pelo EaterAIController/EatingState quando o consumo termina
        public void ResetEatingState()
        {
            _isEating = false;
        }
    }
}