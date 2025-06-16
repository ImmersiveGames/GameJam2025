using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Predicates;
using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PlanetDefenses : SpawnPoint
    {
        private PlanetsMaster _planetsMaster;

        protected override void Awake()
        {
            base.Awake();
            if (!TryGetComponent(out _planetsMaster))
            {
                DebugUtility.LogError<PlanetDefenses>("PlanetsMaster não encontrado no objeto.", this);
                enabled = false;
                return;
            }

            // Inicializar o predicado com o PlanetsMaster, se aplicável
            if (spawnData?.TriggerStrategy is PredicateTriggerSo predicateTrigger && 
                predicateTrigger.predicate is PlanetDetectedPredicateSo planetPredicate)
            {
                planetPredicate.Initialize(_planetsMaster);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // Não é necessário inscrever-se nos eventos, pois o PlanetDetectedPredicateSo faz isso
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            // Não é necessário desinscrever-se dos eventos
        }

        // Métodos para depuração
        private void OnPlanetDetected(IDetector obj, SensorTypes sensor)
        {
            DebugUtility.LogVerbose<PlanetDefenses>(
                $"Ativando defesa do planeta {_planetsMaster.name} detectado pelo sensor: {sensor}", "green", this);
        }

        private void OnPlanetLost(IDetector obj, SensorTypes sensor)
        {
            DebugUtility.LogVerbose<PlanetDefenses>(
                $"Desativando defesa do planeta {_planetsMaster.name} detectado pelo sensor: {sensor}", "red");
        }
    }
}