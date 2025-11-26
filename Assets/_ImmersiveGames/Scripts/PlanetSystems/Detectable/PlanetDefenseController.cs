using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Defense;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Detectable
{
    public class PlanetDefenseController : MonoBehaviour
    {
        [SerializeField] private PlanetsMaster planetsMaster;

        private readonly HashSet<IDetector> _activeDetectors = new();
        private readonly Dictionary<IDetector, int> _detectorTypeCount = new();
        private readonly Dictionary<DetectionType, HashSet<IDetector>> _detectorsByType = new();
        private readonly Dictionary<IDetector, DefenseRole> _detectorRoles = new();

        private void Awake()
        {
            if (planetsMaster == null && !TryGetComponent(out planetsMaster))
            {
                planetsMaster = GetComponentInParent<PlanetsMaster>();
            }

            if (planetsMaster == null)
            {
                DefenseUtils.LogMissingPlanetMaster(this, gameObject.name);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (planetsMaster == null)
            {
                planetsMaster = GetComponentInParent<PlanetsMaster>();
            }
        }
#endif

        /// <summary>
        /// Entrada principal a partir do sensor. Publica eventos com metadados
        /// de contagem para que outros serviços (ex.: spawner) não precisem
        /// reimplementar a mesma lógica de rastrear detectores.
        /// </summary>
        public void EngageDefense(IDetector detector, DetectionType detectionType)
        {
            if (detector == null)
            {
                DefenseUtils.LogIgnoredNullDetector(this);
                return;
            }

            if (detectionType == null)
            {
                DebugUtility.LogWarning<PlanetDefenseController>(
                    $"DetectionType ausente para detector {DefenseUtils.GetDetectorName(detector)}.", this);
                return;
            }

            if (!DefenseUtils.TryAddToLookup(_detectorsByType, detectionType, detector))
            {
                DefenseUtils.LogDuplicateDetector(detector, detectionType);
                return;
            }

            DefenseRole role = CacheRole(detector);
            _activeDetectors.Add(detector);
            _detectorTypeCount[detector] = _detectorTypeCount.TryGetValue(detector, out int count)
                ? count + 1
                : 1;

            int activeCount = _activeDetectors.Count;

            DebugUtility.LogVerbose<PlanetDefenseController>(
                $"Planeta {GetPlanetName()} iniciou defesas contra {DefenseUtils.FormatDetector(detector, role)}.",
                DebugUtility.Colors.CrucialInfo,
                this);

            EventBus<PlanetDefenseEngagedEvent>.Raise(
                new PlanetDefenseEngagedEvent(
                    planetsMaster,
                    detector,
                    detectionType,
                    isFirstEngagement: activeCount == 1,
                    activeDetectors: activeCount));
        }

        /// <summary>
        /// Complementa o fluxo de entrada, garantindo que a contagem e o flag
        /// de última saída sejam emitidos para os listeners responderem uma
        /// única vez ao desligamento.
        /// </summary>
        public void DisengageDefense(IDetector detector, DetectionType detectionType)
        {
            if (detector == null)
            {
                DefenseUtils.LogIgnoredNullDetector(this);
                return;
            }

            if (detectionType == null)
            {
                DebugUtility.LogWarning<PlanetDefenseController>(
                    $"DetectionType ausente para detector {DefenseUtils.GetDetectorName(detector)}.", this);
                return;
            }

            if (!DefenseUtils.TryRemoveFromLookup(_detectorsByType, detectionType, detector))
            {
                return;
            }

            if (_detectorTypeCount.TryGetValue(detector, out int count))
            {
                count--;
                if (count <= 0)
                {
                    _detectorTypeCount.Remove(detector);
                    _activeDetectors.Remove(detector);
                }
                else
                {
                    _detectorTypeCount[detector] = count;
                }
            }

            _detectorRoles.TryGetValue(detector, out DefenseRole role);

            int activeCount = _activeDetectors.Count;
            DebugUtility.LogVerbose<PlanetDefenseController>(
                $"Planeta {GetPlanetName()} encerrou defesas contra {DefenseUtils.FormatDetector(detector, role)}.",
                null,
                this);

            EventBus<PlanetDefenseDisengagedEvent>.Raise(
                new PlanetDefenseDisengagedEvent(
                    planetsMaster,
                    detector,
                    detectionType,
                    isLastDisengagement: activeCount == 0,
                    activeDetectors: activeCount));
        }

        private void OnDisable()
        {
            if (_activeDetectors.Count > 0 && planetsMaster != null)
            {
                int activeCount = _activeDetectors.Count;
                _activeDetectors.Clear();
                _detectorsByType.Clear();
                _detectorTypeCount.Clear();
                _detectorRoles.Clear();
                EventBus<PlanetDefenseDisabledEvent>.Raise(
                    new PlanetDefenseDisabledEvent(planetsMaster, activeCount));
            }
        }

        private string GetPlanetName()
        {
            return planetsMaster?.ActorName ?? gameObject.name;
        }

        private DefenseRole CacheRole(IDetector detector)
        {
            if (!_detectorRoles.TryGetValue(detector, out var role))
            {
                role = DefenseUtils.ResolveDefenseRole(detector);
                _detectorRoles[detector] = role;
            }

            return role;
        }
    }
}
