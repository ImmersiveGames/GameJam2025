using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    [DebugLevel(DebugLevel.Logs)]
    public class PlanetRecognizer : PlanetSensor
    {
        private readonly List<IPlanetInteractable> _recognizedPlanets = new();
        private IDetectable _detectableEntity;

        protected override void Awake()
        {
            base.Awake();
            InitializeDetectableEntity();
        }

        private void InitializeDetectableEntity()
        {
            _detectableEntity = GetComponent<IDetectable>();
            if (_detectableEntity == null)
            {
                DebugUtility.LogError<PlanetRecognizer>("IDetectable não encontrado no GameObject.", this);
                enabled = false;
            }
        }

        protected override void ProcessPlanets(List<IPlanetInteractable> planets)
        {
            foreach (var planet in planets)
            {
                if (_recognizedPlanets.Contains(planet)) continue;

                _recognizedPlanets.Add(planet);
                _detectableEntity.OnRecognitionRangeEntered(planet, planet.GetResources());
                planet.SendRecognitionData(_detectableEntity);

                if (debugMode)
                {
                    DebugUtility.Log<PlanetRecognizer>($"Reconhecimento ativado para: {planet.Name}", "cyan");
                }
            }
        }

        public override void DisableSensor()
        {
            if (!IsEnabled) return;

            // Limpa a lista de planetas reconhecidos
            _recognizedPlanets.Clear();
            base.DisableSensor();
        }

        public void RemoveRecognizedPlanet(PlanetsMaster planet)
        {
            if (_recognizedPlanets.Remove(planet) && debugMode)
            {
                DebugUtility.Log<PlanetRecognizer>($"Planeta removido do reconhecimento: {planet.Name}", "cyan");
            }
        }

        protected override void AdjustDetectionFrequency(int planetCount)
        {
            CurrentDetectionFrequency = maxDetectionFrequency;
        }

        public IReadOnlyList<IPlanetInteractable> GetRecognizedPlanets() => _recognizedPlanets.AsReadOnly();

        public void ClearRecognizedPlanets()
        {
            _recognizedPlanets.Clear();
            if (debugMode)
            {
                DebugUtility.Log<PlanetRecognizer>("Lista de planetas reconhecidos limpa", "cyan");
            }
        }

        protected override void OnDrawGizmos()
        {
            if (!debugMode || !CachedTransform) return;

            Gizmos.color = IsEnabled ? (_recognizedPlanets.Count > 0 ? Color.blue : Color.cyan) : Color.gray;
            Gizmos.DrawWireSphere(CachedTransform.position, radius);

            foreach (var planet in _recognizedPlanets)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(CachedTransform.position, planet.Transform.position);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(planet.Transform.position + Vector3.up * 0.5f, $"Reconhecido: {planet.Name}");
#endif
            }
        }
    }
}