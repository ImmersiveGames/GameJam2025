using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.DetectionsSystems.Mono;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem.Detections
{
    public class PlayerDetectionController : AbstractDetector
    {
        [Header("Detection Types")]
        [SerializeField] private DetectionType planetResourcesDetectionType;

        private readonly HashSet<DetectionType> _registeredDetectionTypes = new();
        private SensorController _sensorController;

        protected override void Awake()
        {
            base.Awake();

            // Como o Player pode operar múltiplos sensores, guardamos a referência do SensorController
            // para mapear os DetectionType configurados via coleção em tempo de execução.
            _sensorController = GetComponent<SensorController>();
        }

        private void Start()
        {
            CacheDetectionTypesFromSensors();
        }

        public override void OnDetected(IDetectable detectable, DetectionType detectionType)
        {
            if (detectionType == null)
            {
                DebugUtility.LogWarning<PlayerDetectionController>(
                    "Evento de detecção recebido sem DetectionType válido.", this);
                return;
            }

            if (_registeredDetectionTypes.Count == 0)
            {
                CacheDetectionTypesFromSensors();
            }

            if (!_registeredDetectionTypes.Contains(detectionType))
            {
                DebugUtility.LogWarning<PlayerDetectionController>(
                    $"DetectionType não registrado no Player: {detectionType.TypeName}", this);
                return;
            }

            if (detectionType == planetResourcesDetectionType)
            {
                HandlePlanetResourcesDetection(detectable);
                return;
            }

            DebugUtility.LogVerbose<PlayerDetectionController>(
                $"Detecção recebida sem manipulador específico: {detectionType.TypeName}");
        }

        public override void OnLost(IDetectable detectable, DetectionType detectionType)
        {
            if (detectionType == null || !_registeredDetectionTypes.Contains(detectionType))
            {
                return;
            }

            if (detectionType == planetResourcesDetectionType)
            {
                // Este sensor não exige lógica de saída porque o planeta permanece revelado.
                return;
            }
        }

        private void CacheDetectionTypesFromSensors()
        {
            if (_sensorController == null)
            {
                DebugUtility.LogWarning<PlayerDetectionController>(
                    "SensorController não encontrado para mapear DetectionTypes.", this);
                return;
            }

            var collection = _sensorController.Collection;
            if (collection == null)
            {
                DebugUtility.LogWarning<PlayerDetectionController>(
                    "SensorCollection não configurado no SensorController do Player.", this);
                return;
            }

            foreach (var sensor in collection.Sensors)
            {
                if (sensor?.DetectionType == null) continue;

                _registeredDetectionTypes.Add(sensor.DetectionType);

                if (planetResourcesDetectionType == null &&
                    sensor.DetectionType.TypeName == "PlanetResourcesDetector")
                {
                    planetResourcesDetectionType = sensor.DetectionType;
                }
            }

            if (planetResourcesDetectionType == null)
            {
                DebugUtility.LogWarning<PlayerDetectionController>(
                    "DetectionType PlanetResourcesDetector não encontrado na coleção do Player.", this);
            }
        }

        private void HandlePlanetResourcesDetection(IDetectable detectable)
        {
            if (!TryResolvePlanetMaster(detectable, out PlanetsMaster planetMaster))
            {
                DebugUtility.LogWarning<PlayerDetectionController>(
                    "Detecção de recurso planetário sem PlanetsMaster associado.", this);
                return;
            }

            if (planetMaster.IsResourceDiscovered)
            {
                DebugUtility.LogVerbose<PlayerDetectionController>(
                    $"Planeta {planetMaster.ActorName} já estava revelado.");
                return;
            }

            planetMaster.RevealResource();

            DebugUtility.LogVerbose<PlayerDetectionController>(
                $"Recurso do planeta {planetMaster.ActorName} revelado pelo Player.");
        }

        private static bool TryResolvePlanetMaster(IDetectable detectable, out PlanetsMaster planetMaster)
        {
            planetMaster = null;

            if (detectable is Component detectableComponent)
            {
                detectableComponent.TryGetComponent(out planetMaster);
                planetMaster ??= detectableComponent.GetComponentInParent<PlanetsMaster>();
            }

            if (planetMaster == null && detectable?.Owner is Component ownerComponent)
            {
                ownerComponent.TryGetComponent(out planetMaster);
                planetMaster ??= ownerComponent.GetComponentInParent<PlanetsMaster>();
            }

            return planetMaster != null;
        }
    }
}
