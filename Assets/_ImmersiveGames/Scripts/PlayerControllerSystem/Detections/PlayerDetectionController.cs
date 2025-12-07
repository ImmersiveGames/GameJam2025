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
        [SerializeField] private DetectionType planetDefenseDetectionType;

        private readonly HashSet<DetectionType> _registeredDetectionTypes = new();
        private SensorController _sensorController;
        private readonly HashSet<IDetectable> _activeDefenseDetections = new();

        protected override void Awake()
        {
            base.Awake();

            // Como o Player pode operar múltiplos sensores, guardamos a referência do SensorController
            // para mapear os DetectionType configurados via coleção em tempo de execução.
            _sensorController = GetComponent<SensorController>() ??
                                GetComponentInChildren<SensorController>(includeInactive: true);
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

            if (detectionType == planetDefenseDetectionType)
            {
                HandlePlanetDefenseDetection(detectable);
                return;
            }

            DebugUtility.LogVerbose<PlayerDetectionController>(
                $"Detecção recebida sem manipulador específico: {detectionType.TypeName}",
                null,
                this);
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

            if (detectionType == planetDefenseDetectionType)
            {
                HandlePlanetDefenseLost(detectable);
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

            _registeredDetectionTypes.Clear();

            foreach (var sensor in collection.Sensors)
            {
                if (sensor?.DetectionType == null) continue;

                _registeredDetectionTypes.Add(sensor.DetectionType);

                if (planetResourcesDetectionType == null &&
                    sensor.DetectionType.TypeName == "PlanetResourcesDetector")
                {
                    planetResourcesDetectionType = sensor.DetectionType;
                }

                if (planetDefenseDetectionType == null &&
                    sensor.DetectionType.TypeName == "PlanetDefenseDetector")
                {
                    planetDefenseDetectionType = sensor.DetectionType;
                }
            }

            if (planetResourcesDetectionType == null)
            {
                DebugUtility.LogWarning<PlayerDetectionController>(
                    "DetectionType PlanetResourcesDetector não encontrado na coleção do Player.", this);
            }

            if (planetDefenseDetectionType == null)
            {
                DebugUtility.LogWarning<PlayerDetectionController>(
                    "DetectionType PlanetDefenseDetector não encontrado na coleção do Player.", this);
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
                    $"Planeta {planetMaster.ActorName} já estava revelado.",
                    null,
                    this);
                return;
            }

            planetMaster.RevealResource();

            DebugUtility.LogVerbose<PlayerDetectionController>(
                $"Recurso do planeta {planetMaster.ActorName} revelado pelo Player.",
                DebugUtility.Colors.Success,
                this);
        }

        private void HandlePlanetDefenseDetection(IDetectable detectable)
        {
            if (!_activeDefenseDetections.Add(detectable))
            {
                return;
            }

            if (!TryResolvePlanetMaster(detectable, out PlanetsMaster planetMaster))
            {
                _activeDefenseDetections.Remove(detectable);
                DebugUtility.LogWarning<PlayerDetectionController>(
                    "Detecção defensiva sem PlanetsMaster associado.", this);
                return;
            }

            string detectorName = Owner?.ActorName ?? name;

            DebugUtility.LogVerbose<PlayerDetectionController>(
                $"Planeta {planetMaster.ActorName} ativou defesas contra {detectorName}.",
                DebugUtility.Colors.CrucialInfo,
                this);
        }

        private void HandlePlanetDefenseLost(IDetectable detectable)
        {
            if (!_activeDefenseDetections.Remove(detectable))
            {
                return;
            }

            if (!TryResolvePlanetMaster(detectable, out PlanetsMaster planetMaster))
            {
                // Mesmo sem o PlanetsMaster válido, garantimos que o cache foi limpo.
                return;
            }

            string detectorName = Owner?.ActorName ?? name;

            DebugUtility.LogVerbose<PlayerDetectionController>(
                $"Planeta {planetMaster.ActorName} desativou defesas contra {detectorName}.",
                null,
                this);
        }

        protected override void OnCacheCleared()
        {
            _activeDefenseDetections.Clear();
            base.OnCacheCleared();
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
