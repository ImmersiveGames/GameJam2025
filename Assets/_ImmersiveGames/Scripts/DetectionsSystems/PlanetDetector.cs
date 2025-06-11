using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    [DebugLevel(DebugLevel.None)]
    public class PlanetDetector : PlanetSensor
    {
        private readonly List<IPlanetInteractable> _detectedPlanets = new();
        private IDetectable _detectableEntity;
        private GameManager _gameManager;

        protected override void Awake()
        {
            base.Awake();
            InitializeDetectableEntity();
        }

        private void InitializeDetectableEntity()
        {
            _gameManager = GameManager.Instance;
            _detectableEntity = GetComponent<IDetectable>();
            if (_detectableEntity != null) return;

            DebugUtility.LogError<PlanetDetector>("IDetectable não encontrado no GameObject.", this);
            enabled = false;
        }

        protected override void ProcessPlanets(List<IPlanetInteractable> planets)
        {
            if (!_gameManager.ShouldPlayingGame()) return;
            UpdateDetectedPlanets(planets);
            AdjustDetectionFrequency(_detectedPlanets.Count);
        }

        private void UpdateDetectedPlanets(List<IPlanetInteractable> currentPlanets)
        {
            // Adicionar novos planetas
            foreach (var planet in currentPlanets)
            {
                if (_detectedPlanets.Contains(planet)) continue;

                _detectedPlanets.Add(planet);
                _detectableEntity.OnPlanetDetected(planet);
                planet.ActivateDefenses(_detectableEntity);

                if (debugMode)
                {
                    DebugUtility.Log<PlanetDetector>($"Planeta detectado: {planet.Name}", "green");
                }
            }

            // Remover planetas fora de alcance
            _detectedPlanets.RemoveAll(planet =>
            {
                if (currentPlanets.Contains(planet)) return false;

                _detectableEntity.OnPlanetLost(planet);
                planet.DeactivateDefenses(_detectableEntity);
                if (debugMode)
                {
                    DebugUtility.Log<PlanetDetector>($"Planeta perdido: {planet.Name}", "red");
                }
                return true;
            });
        }

        public override void DisableSensor()
        {
            if (!IsEnabled) return;

            // Desativa defesas de todos os planetas detectados
            foreach (var planet in _detectedPlanets)
            {
                _detectableEntity.OnPlanetLost(planet);
                planet.DeactivateDefenses(_detectableEntity);
            }
            _detectedPlanets.Clear();
            base.DisableSensor();
        }

        protected override void OnDrawGizmos()
        {
            if (!debugMode || !CachedTransform) return;

            Gizmos.color = IsEnabled ? (_detectedPlanets.Count > 0 ? Color.green : Color.yellow) : Color.gray;
            Gizmos.DrawWireSphere(CachedTransform.position, radius);

            foreach (var planet in _detectedPlanets)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(CachedTransform.position, planet.Transform.position);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(planet.Transform.position + Vector3.up * 0.5f, $"Detectado: {planet.Name}");
#endif
            }
        }
    }
}