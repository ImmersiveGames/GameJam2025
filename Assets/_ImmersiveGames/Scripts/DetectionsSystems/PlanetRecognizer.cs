using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    [DebugLevel(DebugLevel.None)]
    public class PlanetRecognizer : PlanetSensor
    {
        private IDetectable _detectableEntity;
        [SerializeField] private List<PlanetsMaster> recognizedPlanets = new();

        protected override void Awake()
        {
            base.Awake();
            _detectableEntity = GetComponent<IDetectable>();
            if (_detectableEntity == null)
            {
                DebugUtility.LogError<PlanetRecognizer>("IDetectable não encontrado no GameObject.", this);
                enabled = false;
            }
        }

        protected override void ProcessPlanets(List<PlanetsMaster> planets)
        {
            foreach (var planet in planets)
            {
                if (recognizedPlanets.Contains(planet)) continue;
                recognizedPlanets.Add(planet);
                var planetInteractable = planet.GetComponent<IPlanetInteractable>();
                if (planetInteractable == null) continue;
                _detectableEntity.OnRecognitionRangeEntered(planet, planetInteractable.GetResources());
                planetInteractable.SendRecognitionData(_detectableEntity);
                if (debugMode)
                {
                    DebugUtility.Log<PlanetRecognizer>($"Reconhecimento ativado para: {planet.name}", "blue");
                }
            }

            // Não ajusta a frequência dinamicamente, usa valor fixo
            AdjustDetectionFrequency(recognizedPlanets.Count);
        }

        protected override void AdjustDetectionFrequency(int planetCount)
        {
            // Mantém a frequência fixa (ex.: maxDetectionFrequency) para reconhecimento
            currentDetectionFrequency = maxDetectionFrequency;
        }

        public List<PlanetsMaster> GetRecognizedPlanets()
        {
            return recognizedPlanets;
        }

        public void ClearRecognizedPlanets()
        {
            recognizedPlanets.Clear();
        }

        protected override void OnDrawGizmos()
        {
            if (!debugMode || !cachedTransform) return;

            Gizmos.color = recognizedPlanets.Count > 0 ? Color.blue : Color.cyan;
            Gizmos.DrawWireSphere(cachedTransform.position, radius);

            foreach (var planet in recognizedPlanets)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(cachedTransform.position, planet.transform.position);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(planet.transform.position + Vector3.up * 0.5f, $"Reconhecido: {planet.name}");
#endif
            }
        }
    }
}