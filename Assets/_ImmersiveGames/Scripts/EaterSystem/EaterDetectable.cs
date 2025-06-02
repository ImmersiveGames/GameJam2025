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
        [SerializeField, Tooltip("Distância para comer o planeta")]
        private float eatDistance = 5f;

        public event Action<Planets> OnEatPlanet;
        public event Action<Transform> OnTargetUpdated;

        private Transform _self;
        private Planets _targetPlanet;
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
            if (_targetPlanet == null || !_targetPlanet.IsActive) return;

            float distance = Vector3.Distance(
                new Vector3(_self.position.x, 0, _self.position.z),
                new Vector3(_targetPlanet.transform.position.x, 0, _targetPlanet.transform.position.z));

            if (distance <= eatDistance)
            {
                OnEatPlanet?.Invoke(_targetPlanet);
                DebugUtility.LogVerbose<EaterDetectable>($"EaterDetectable comeu planeta: {_targetPlanet.name}", "magenta");
            }
        }

        private void OnPlanetMarked(PlanetMarkedEvent evt)
        {
            _targetPlanet = evt.Planet;
            OnTargetUpdated?.Invoke(_targetPlanet.transform);
            DebugUtility.LogVerbose<EaterDetectable>($"Novo alvo recebido: {_targetPlanet.name}", "yellow");
        }

        private void OnPlanetUnmarked(PlanetUnmarkedEvent evt)
        {
            if (_targetPlanet == evt.Planet)
            {
                _targetPlanet = null;
                DebugUtility.LogVerbose<EaterDetectable>($"Alvo removido: {evt.Planet.name}", "red");
            }
        }

        public void OnPlanetDetected(Planets planet)
        {
            DebugUtility.LogVerbose<EaterDetectable>($"Planeta detectado: {planet.name}", "green");
            
            //só vai parar se ele estiver marcado como alvo do Eater
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
            DebugUtility.LogVerbose<EaterDetectable>($"Reconheceu planeta: {planet.name}, Recursos: {resources}", "blue");
        }
    }
}
