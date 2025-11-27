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
        [SerializeField] private DefenseRoleConfig defenseRoleConfig;

        private readonly Dictionary<IDetector, DefenseRole> _activeDetectors = new();

        private void Awake()
        {
            if (planetsMaster == null && !TryGetComponent(out planetsMaster))
            {
                planetsMaster = GetComponentInParent<PlanetsMaster>();
            }

            if (planetsMaster == null)
            {
                DebugUtility.LogError<PlanetDefenseController>(
                    $"PlanetsMaster não encontrado para o controle de defesa em {gameObject.name}.", this);
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
                return;
            }

            if (_activeDetectors.ContainsKey(detector))
            {
                return;
            }

            DefenseRole role = ResolveDefenseRole(detector);
            _activeDetectors.Add(detector, role);
            int activeCount = _activeDetectors.Count;

            DebugUtility.LogVerbose<PlanetDefenseController>(
                $"Planeta {GetPlanetName()} iniciou defesas contra {FormatDetector(detector, role)}.",
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
                return;
            }

            if (_activeDetectors.TryGetValue(detector, out DefenseRole role))
            {
                _activeDetectors.Remove(detector);
                int activeCount = _activeDetectors.Count;
                DebugUtility.LogVerbose<PlanetDefenseController>(
                    $"Planeta {GetPlanetName()} encerrou defesas contra {FormatDetector(detector, role)}.",
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
        }

        private void OnDisable()
        {
            if (_activeDetectors.Count > 0 && planetsMaster != null)
            {
                int activeCount = _activeDetectors.Count;
                _activeDetectors.Clear();
                EventBus<PlanetDefenseDisabledEvent>.Raise(
                    new PlanetDefenseDisabledEvent(planetsMaster, activeCount));
            }
        }

        private string GetPlanetName()
        {
            return planetsMaster?.ActorName ?? gameObject.name;
        }

        private static string GetDetectorName(IDetector detector)
        {
            return detector.Owner?.ActorName ?? detector.ToString();
        }

        private DefenseRole ResolveDefenseRole(IDetector detector)
        {
            DefenseRole explicitRole = TryResolveFromDetector(detector);
            if (explicitRole != DefenseRole.Unknown)
            {
                return explicitRole;
            }

            DefenseRole ownerRole = TryResolveFromOwner(detector);
            if (ownerRole != DefenseRole.Unknown)
            {
                return ownerRole;
            }

            DefenseRole configuredRole = TryResolveFromConfig(detector);
            if (configuredRole != DefenseRole.Unknown)
            {
                return configuredRole;
            }

            DebugUtility.LogVerbose<PlanetDefenseController>(
                "Nenhuma fonte resolveu o role; usando Unknown.",
                null,
                this);

            // TODO: Monitorar logs para Unknown e adicionar providers se necessário.
            // TODO: Remover debug de fontes após validação completa.
            return DefenseRole.Unknown;
        }

        private static DefenseRole TryResolveFromDetector(IDetector detector)
        {
            if (detector is IDefenseRoleProvider provider)
            {
                DefenseRole role = NormalizeRole(provider.GetDefenseRole());
                if (role != DefenseRole.Unknown)
                {
                    DebugUtility.LogVerbose<PlanetDefenseController>(
                        $"Role resolvido via provider no detector: {role}",
                        null);
                }

                return role;
            }

            if (detector is Component detectorComponent &&
                detectorComponent.TryGetComponent(out IDefenseRoleProvider componentProvider))
            {
                DefenseRole role = NormalizeRole(componentProvider.GetDefenseRole());
                if (role != DefenseRole.Unknown)
                {
                    DebugUtility.LogVerbose<PlanetDefenseController>(
                        $"Role resolvido via provider no detector (componente): {role}",
                        null);
                }

                return role;
            }

            return DefenseRole.Unknown;
        }

        private static DefenseRole TryResolveFromOwner(IDetector detector)
        {
            if (detector?.Owner is IDefenseRoleProvider provider)
            {
                DefenseRole role = NormalizeRole(provider.GetDefenseRole());
                if (role != DefenseRole.Unknown)
                {
                    DebugUtility.LogVerbose<PlanetDefenseController>(
                        $"Role resolvido via provider no Owner: {role}",
                        null);
                }

                return role;
            }

            if (detector?.Owner is Component ownerComponent &&
                ownerComponent.TryGetComponent(out IDefenseRoleProvider providerComponent))
            {
                DefenseRole role = NormalizeRole(providerComponent.GetDefenseRole());
                if (role != DefenseRole.Unknown)
                {
                    DebugUtility.LogVerbose<PlanetDefenseController>(
                        $"Role resolvido via provider no Owner (componente): {role}",
                        null);
                }

                return role;
            }

            return DefenseRole.Unknown;
        }

        private DefenseRole TryResolveFromConfig(IDetector detector)
        {
            if (defenseRoleConfig == null)
            {
                return DefenseRole.Unknown;
            }

            string identifier = detector?.Owner?.ActorName;

            if (string.IsNullOrEmpty(identifier) && detector is Component detectorComponent)
            {
                identifier = detectorComponent.gameObject.name;
            }

            DefenseRole role = defenseRoleConfig.ResolveRole(identifier);

            if (role != DefenseRole.Unknown)
            {
                DebugUtility.LogVerbose<PlanetDefenseController>(
                    $"Role via config: {role}",
                    null,
                    this);
            }

            return role;
        }

        private static DefenseRole NormalizeRole(DefenseRole role)
        {
            return role == DefenseRole.Unknown ? DefenseRole.Unknown : role;
        }

        private static string FormatDetector(IDetector detector, DefenseRole role)
        {
            string detectorName = GetDetectorName(detector);

            return role switch
            {
                DefenseRole.Player => $"o Player ({detectorName})",
                DefenseRole.Eater => $"o Eater ({detectorName})",
                _ => detectorName
            };
        }
    }
}
