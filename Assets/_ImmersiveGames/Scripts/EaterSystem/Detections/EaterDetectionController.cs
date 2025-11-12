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
    /// Monitora apenas dois tipos de detecção: proximidade de planetas e ativação de defesas.
    /// </summary>
    public class EaterDetectionController : AbstractDetector, IDefenseRoleProvider
    {
        [Header("Detection Types")]
        [SerializeField] private DetectionType planetDefenseDetectionType;
        [SerializeField] private DetectionType planetProximityDetectionType;

        private readonly HashSet<IDetectable> _activeDefenseDetections = new();
        private readonly HashSet<IDetectable> _activeProximityDetections = new();

        public DefenseRole DefenseRole => DefenseRole.Eater;

        internal DetectionType PlanetProximityDetectionType => planetProximityDetectionType;

        internal bool IsTransformWithinProximity(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            foreach (IDetectable detectable in _activeProximityDetections)
            {
                if (detectable == null)
                {
                    continue;
                }

                Transform detectableTransform = ResolveDetectableTransform(detectable);
                if (detectableTransform == null)
                {
                    continue;
                }

                if (ReferenceEquals(detectableTransform, target) || target.IsChildOf(detectableTransform) ||
                    detectableTransform.IsChildOf(target))
                {
                    return true;
                }
            }

            return false;
        }

        public override void OnDetected(IDetectable detectable, DetectionType detectionType)
        {
            if (detectionType == null)
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    "Evento de detecção recebido sem DetectionType válido.",
                    this);
                return;
            }

            if (detectionType == planetProximityDetectionType)
            {
                HandlePlanetProximityDetection(detectable);
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

            if (detectionType == planetProximityDetectionType)
            {
                HandlePlanetProximityLost(detectable);
                return;
            }

            if (detectionType == planetDefenseDetectionType)
            {
                HandlePlanetDefenseLost(detectable);
            }
        }

        private void HandlePlanetProximityDetection(IDetectable detectable)
        {
            if (!_activeProximityDetections.Add(detectable))
            {
                return;
            }

            if (!TryResolvePlanetMaster(detectable, out PlanetsMaster planetMaster))
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    "Detecção de proximidade sem PlanetsMaster associado.",
                    this);
                return;
            }

            DebugUtility.LogVerbose<EaterDetectionController>(
                $"Planeta {planetMaster.ActorName} entrou na área de proximidade do Eater.",
                DebugUtility.Colors.Info,
                this);
        }

        private void HandlePlanetProximityLost(IDetectable detectable)
        {
            if (!_activeProximityDetections.Remove(detectable))
            {
                return;
            }

            if (!TryResolvePlanetMaster(detectable, out PlanetsMaster planetMaster))
            {
                return;
            }

            DebugUtility.LogVerbose<EaterDetectionController>(
                $"Planeta {planetMaster.ActorName} saiu da área de proximidade do Eater.",
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

        private static Transform ResolveDetectableTransform(IDetectable detectable)
        {
            if (detectable is Component component)
            {
                return component.transform;
            }

            if (detectable?.Owner is Component ownerComponent)
            {
                return ownerComponent.transform;
            }

            return null;
        }

        protected override void OnCacheCleared()
        {
            _activeDefenseDetections.Clear();
            _activeProximityDetections.Clear();
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
