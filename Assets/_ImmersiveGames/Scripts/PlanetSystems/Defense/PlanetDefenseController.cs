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
        private readonly Dictionary<IDetector, DefenseRole> _activeDetectorRoles = new();

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

            if (_activeDetectorRoles.ContainsKey(detector))
            {
                return;
            }

            var targetRole = ResolveTargetRole(detector);
            _activeDetectorRoles.Add(detector, targetRole);
            int activeCount = _activeDetectorRoles.Count;

            DebugUtility.LogVerbose<PlanetDefenseController>(
                $"Planeta {GetPlanetName()} iniciou defesas contra {FormatDetector(detector, targetRole)}.",
                DebugUtility.Colors.CrucialInfo,
                this);

            EventBus<PlanetDefenseEngagedEvent>.Raise(
                new PlanetDefenseEngagedEvent(
                    planetsMaster,
                    detector,
                    detectionType,
                    targetRole,
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

            if (_activeDetectorRoles.Remove(detector, out var targetRole))
            {
                int activeCount = _activeDetectorRoles.Count;
                DebugUtility.LogVerbose<PlanetDefenseController>(
                    $"Planeta {GetPlanetName()} encerrou defesas contra {FormatDetector(detector, targetRole)}.",
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
            if (_activeDetectorRoles.Count > 0 && planetsMaster != null)
            {
                _activeDetectorRoles.Clear();
                EventBus<PlanetDefenseDisabledEvent>.Raise(new PlanetDefenseDisabledEvent(planetsMaster));
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

        private DefenseRole ResolveTargetRole(IDetector detector)
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
                var targetRole = NormalizeRole(provider.GetDefenseRole());
                if (targetRole != DefenseRole.Unknown)
                {
                    DebugUtility.LogVerbose<PlanetDefenseController>(
                        $"Role resolvido via provider no detector: {targetRole}");
                }

                return targetRole;
            }

            if (detector is Component detectorComponent &&
                detectorComponent.TryGetComponent(out IDefenseRoleProvider componentProvider))
            {
                var targetRole = NormalizeRole(componentProvider.GetDefenseRole());
                if (targetRole != DefenseRole.Unknown)
                {
                    DebugUtility.LogVerbose<PlanetDefenseController>(
                        $"Role resolvido via provider no detector (componente): {targetRole}");
                }

                return targetRole;
            }

            return DefenseRole.Unknown;
        }

        private static DefenseRole TryResolveFromOwner(IDetector detector)
        {
            if (detector?.Owner is IDefenseRoleProvider provider)
            {
                var targetRole = NormalizeRole(provider.GetDefenseRole());
                if (targetRole != DefenseRole.Unknown)
                {
                    DebugUtility.LogVerbose<PlanetDefenseController>(
                        $"Role resolvido via provider no Owner: {targetRole}");
                }

                return targetRole;
            }

            if (detector?.Owner is Component ownerComponent &&
                ownerComponent.TryGetComponent(out IDefenseRoleProvider providerComponent))
            {
                var targetRole = NormalizeRole(providerComponent.GetDefenseRole());
                if (targetRole != DefenseRole.Unknown)
                {
                    DebugUtility.LogVerbose<PlanetDefenseController>(
                        $"Role resolvido via provider no Owner (componente): {targetRole}");
                }

                return targetRole;
            }

            return DefenseRole.Unknown;
        }

        private static DefenseRole NormalizeRole(DefenseRole role)
        {
            return role == DefenseRole.Unknown ? DefenseRole.Unknown : role;
        }

        private static string FormatDetector(IDetector detector, DefenseRole targetRole)
        {
            string detectorName = GetDetectorName(detector);

            return targetRole switch
            {
                DefenseRole.Player => $"o Player ({detectorName})",
                DefenseRole.Eater => $"o Eater ({detectorName})",
                _ => detectorName
            };
        }
    }
}