using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.DetectionsSystems.Mono;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.Detections
{
    /// <summary>
    /// Controlador simples responsável por interpretar os sensores do Eater.
    /// Após a simplificação das proximidades, monitora apenas ativações de defesas de planetas.
    /// </summary>
    public class EaterDetectionController : AbstractDetector
    {
        [Header("Detection Types")]
        [SerializeField] private DetectionType planetDefenseDetectionType;

        private readonly HashSet<IDetectable> _activeDefenseDetections = new();

        public override void OnDetected(IDetectable detectable, DetectionType detectionType)
        {
            if (detectionType == null)
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    "Evento de detecção recebido sem DetectionType válido.",
                    this);
                return;
            }

            if (detectionType == planetDefenseDetectionType)
            {
                HandlePlanetDefenseDetection(detectable);
                return;
            }

            DebugUtility.LogVerbose<EaterDetectionController>(
                $"Detecção recebida sem manipulador específico: {detectionType.TypeName}.",
                null,
                this);
        }

        public override void OnLost(IDetectable detectable, DetectionType detectionType)
        {
            if (detectionType == null)
            {
                return;
            }

            if (detectionType == planetDefenseDetectionType)
            {
                HandlePlanetDefenseLost(detectable);
                return;
            }

            DebugUtility.LogVerbose<EaterDetectionController>(
                $"Perda de detecção não tratada para {detectionType.TypeName}.",
                null,
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
                DebugUtility.LogWarning<EaterDetectionController>(
                    "Detecção defensiva sem PlanetsMaster associado.",
                    this);
                return;
            }

            string detectorName = Owner?.ActorName ?? name;

            DebugUtility.LogVerbose<EaterDetectionController>(
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
                return;
            }

            string detectorName = Owner?.ActorName ?? name;

            DebugUtility.LogVerbose<EaterDetectionController>(
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
