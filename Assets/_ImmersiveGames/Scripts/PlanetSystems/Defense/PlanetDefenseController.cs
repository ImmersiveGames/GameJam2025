using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [DebugLevel(level: DebugLevel.Verbose)]
    public class PlanetDefenseController : MonoBehaviour
    {
        [SerializeField] private PlanetsMaster planetsMaster;
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
                return;
            }

            // 🔧 Cria sub-serviços separados para orquestração e eventos,
            // mantendo SRP e permitindo DI explícita por ActorId.
            var orchestrator = new PlanetDefenseOrchestrationService();
            planetsMaster.ConfigureDefenseService(orchestrator);
            DependencyManager.Provider.RegisterForObject(planetsMaster.ActorId, orchestrator);
            DependencyManager.Provider.InjectDependencies(orchestrator, planetsMaster.ActorId);
            orchestrator.OnDependenciesInjected();

            var eventService = new PlanetDefenseEventService();
            eventService.SetOwnerObjectId(planetsMaster.ActorId);
            DependencyManager.Provider.RegisterForObject(planetsMaster.ActorId, eventService);
            DependencyManager.Provider.InjectDependencies(eventService, planetsMaster.ActorId);
            eventService.OnDependenciesInjected();
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

            var role = ResolveDefenseRole(detector);
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
                    role,
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

            if (_activeDetectors.Remove(detector, out var role))
            {
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
            var explicitRole = TryResolveFromDetector(detector);
            if (explicitRole != DefenseRole.Unknown)
            {
                return explicitRole;
            }

            var ownerRole = TryResolveFromOwner(detector);
            if (ownerRole != DefenseRole.Unknown)
            {
                return ownerRole;
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
            var provider = detector as IDefenseRoleProvider;
            if (provider != null)
            {
                var role = NormalizeRole(provider.GetDefenseRole());
                if (role != DefenseRole.Unknown)
                {
                    DebugUtility.LogVerbose<PlanetDefenseController>(
                        $"Role resolvido via provider no detector: {role}");
                }

                return role;
            }

            if (detector is Component detectorComponent &&
                detectorComponent.TryGetComponent(out IDefenseRoleProvider componentProvider))
            {
                var role = NormalizeRole(componentProvider.GetDefenseRole());
                if (role != DefenseRole.Unknown)
                {
                    DebugUtility.LogVerbose<PlanetDefenseController>(
                        $"Role resolvido via provider no detector (componente): {role}");
                }

                return role;
            }

            return DefenseRole.Unknown;
        }

        private static DefenseRole TryResolveFromOwner(IDetector detector)
        {
            if (detector?.Owner is IDefenseRoleProvider provider)
            {
                var role = NormalizeRole(provider.GetDefenseRole());
                if (role != DefenseRole.Unknown)
                {
                    DebugUtility.LogVerbose<PlanetDefenseController>(
                        $"Role resolvido via provider no Owner: {role}");
                }

                return role;
            }

            if (detector?.Owner is Component ownerComponent &&
                ownerComponent.TryGetComponent(out IDefenseRoleProvider providerComponent))
            {
                var role = NormalizeRole(providerComponent.GetDefenseRole());
                if (role != DefenseRole.Unknown)
                {
                    DebugUtility.LogVerbose<PlanetDefenseController>(
                        $"Role resolvido via provider no Owner (componente): {role}");
                }

                return role;
            }

            return DefenseRole.Unknown;
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