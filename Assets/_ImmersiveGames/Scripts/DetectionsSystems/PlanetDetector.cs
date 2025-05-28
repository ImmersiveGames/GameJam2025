using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PlanetDetector : PlanetSensor
    {
        private IDetectable _detectableEntity;
        [SerializeField] private List<Planets> detectedPlanets = new();

        protected override void Awake()
        {
            base.Awake();
            _detectableEntity = GetComponent<IDetectable>();
            if (_detectableEntity == null)
            {
                DebugUtility.LogError<PlanetDetector>("IDetectable não encontrado no GameObject.", this);
                enabled = false;
            }
        }

        protected override void ProcessPlanets(List<Planets> planets)
        {
            var currentPlanets = new List<Planets>(planets);

            // Adicionar novos planetas detectados
            foreach (var planet in currentPlanets)
            {
                if (!detectedPlanets.Contains(planet))
                {
                    detectedPlanets.Add(planet);
                    _detectableEntity.OnPlanetDetected(planet);
                    planet.GetComponent<IPlanetInteractable>()?.ActivateDefenses(_detectableEntity);
                    if (debugMode)
                    {
                        DebugUtility.LogVerbose<PlanetDetector>($"Planeta detectado: {planet.name}", "green");
                    }
                }
            }

            // Remover planetas que saíram da detecção
            for (int i = detectedPlanets.Count - 1; i >= 0; i--)
            {
                var planet = detectedPlanets[i];
                if (!currentPlanets.Contains(planet))
                {
                    detectedPlanets.RemoveAt(i);
                    _detectableEntity.OnPlanetLost(planet);
                    if (debugMode)
                    {
                        DebugUtility.LogVerbose<PlanetDetector>($"Planeta perdido: {planet.name}", "red");
                    }
                }
            }

            // Ajustar frequência com base no número de planetas
            AdjustDetectionFrequency(detectedPlanets.Count);
        }

        public List<Planets> GetDetectedPlanets()
        {
            return detectedPlanets;
        }

        protected override void OnDrawGizmos()
        {
            if (!debugMode || !cachedTransform) return;

            Gizmos.color = detectedPlanets.Count > 0 ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(cachedTransform.position, radius);

            foreach (var planet in detectedPlanets)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(cachedTransform.position, planet.transform.position);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(planet.transform.position + Vector3.up * 0.5f, $"Detectado: {planet.name}");
#endif
            }
        }
    }
}