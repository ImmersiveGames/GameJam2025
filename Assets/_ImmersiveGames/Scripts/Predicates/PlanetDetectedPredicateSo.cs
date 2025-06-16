using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Predicates
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Predicates/PlanetDetected")]
    public class PlanetDetectedPredicateSo : PredicateSo
    {
        private PlanetsMaster _planetsMaster;
        private bool _planetDetected;
        private bool _initialized;

        public void Initialize(PlanetsMaster planetsMaster)
        {
            if (_initialized)
                return;

            _planetsMaster = planetsMaster;
            if (_planetsMaster == null)
            {
                DebugUtility.LogError<PlanetDetectedPredicateSo>("PlanetsMaster não fornecido.", this);
                return;
            }

            // Inscrever-se nos eventos
            _planetsMaster.EventPlanetDetected += OnPlanetDetected;
            _planetsMaster.EventPlanetLost += OnPlanetLost;
            _initialized = true;
        }

        private void OnPlanetDetected(IDetector detector, SensorTypes sensor)
        {
            _planetDetected = true;
            DebugUtility.LogVerbose<PlanetDetectedPredicateSo>(
                $"Planeta detectado pelo sensor {sensor}. Predicado ativado.", "green", this);
        }

        private void OnPlanetLost(IDetector detector, SensorTypes sensor)
        {
            _planetDetected = false;
            DebugUtility.LogVerbose<PlanetDetectedPredicateSo>(
                $"Planeta perdido pelo sensor {sensor}. Predicado desativado.", "red'");
        }

        public override bool Evaluate()
        {
            if (!isActive || !_initialized || _planetsMaster == null)
                return false;

            if (_planetDetected)
            {
                _planetDetected = false; // Resetar após avaliação para disparo único por evento
                return true;
            }

            return false;
        }

        public override void SetActive(bool active)
        {
            base.SetActive(active);
            if (!active)
                _planetDetected = false; // Garantir que não dispare enquanto inativo
        }

        public override void Reset()
        {
            _planetDetected = false;
            isActive = true;
        }

        // Limpeza para evitar vazamentos de memória
        private void OnDisable()
        {
            if (_initialized && _planetsMaster != null)
            {
                _planetsMaster.EventPlanetDetected -= OnPlanetDetected;
                _planetsMaster.EventPlanetLost -= OnPlanetLost;
            }
            _initialized = false;
        }
    }
}