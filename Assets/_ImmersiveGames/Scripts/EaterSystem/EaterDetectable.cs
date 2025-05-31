using System;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.ScriptableObjects;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [RequireComponent(typeof(Collider))]
    public class EaterDetectable : MonoBehaviour, IDetectable
    {
        [SerializeField, Tooltip("Distância para comer o planeta")]
        private float eatDistance = 5f;

        public event Action<Planets> OnEatPlanet;
        public event Action<Transform> OnTargetUpdated;

        private Transform _self;
        private Planets _targetPlanet;

        private void Awake()
        {
            _self = transform;
        }

        private void OnEnable()
        {
            GameManager.Instance.OnPlanetMarked += OnPlanetMarked;
            GameManager.Instance.OnPlanetUnmarked += OnPlanetUnmarked;
        }

        private void OnDisable()
        {
            GameManager.Instance.OnPlanetMarked -= OnPlanetMarked;
            GameManager.Instance.OnPlanetUnmarked -= OnPlanetUnmarked;
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
                GameManager.Instance.ClearMarkedPlanet();
            }
        }

        private void OnPlanetMarked(Planets planet)
        {
            _targetPlanet = planet;
            OnTargetUpdated?.Invoke(planet.transform);
            DebugUtility.LogVerbose<EaterDetectable>($"Novo alvo recebido: {planet.name}", "yellow");
        }

        private void OnPlanetUnmarked(Planets planet)
        {
            if (_targetPlanet == planet)
            {
                _targetPlanet = null;
                DebugUtility.LogVerbose<EaterDetectable>($"Alvo removido: {planet.name}", "red");
            }
        }

        public void OnPlanetDetected(Planets planet)
        {
            DebugUtility.LogVerbose<EaterDetectable>($"Planeta detectado: {planet.name}", "green");
            
            //só vai parar se ele estiver marcado como alvo do Eater
            if (!GameManager.Instance.IsMarkedPlanet(planet)) return;
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
