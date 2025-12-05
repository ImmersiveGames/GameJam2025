using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Serviço dedicado ao fluxo de eventos de defesa: registra engajamentos,
    /// orquestra runners via o IPlanetDefenseSetupOrchestrator com logs via DebugUtility.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    public class PlanetDefenseEventService : IInjectableComponent
    {
        private const bool StopWavesOnDisable = true;

        [Inject] private IPlanetDefenseSetupOrchestrator _orchestrator;
        [Inject] private DefenseStateManager _stateManager = new();

        private string _ownerObjectId;

        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => nameof(PlanetDefenseEventService);

        /// <summary>
        /// Injeta o ActorId do planeta para resgatar serviços registrados com o mesmo identificador.
        /// Evita dependência de um "CurrentObjectId" inexistente no DependencyManager.
        /// </summary>
        public void SetOwnerObjectId(string ownerObjectId)
        {
            _ownerObjectId = ownerObjectId;
        }

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            ResolveDependenciesFromProvider();
        }

        public void HandleEngaged(PlanetDefenseEngagedEvent engagedEvent)
        {
            if (engagedEvent.Planet == null || engagedEvent.Detector == null)
            {
                return;
            }

            var state = _stateManager.RegisterEngagement(
                engagedEvent.Planet,
                engagedEvent.DetectionType,
                FormatDetector(engagedEvent.Detector),
                engagedEvent.ActiveDetectors);

            if (state == null)
            {
                return;
            }

            DebugUtility.LogVerbose<PlanetDefenseEventService>(
                $"[Debug] Detectores ativos em {engagedEvent.Planet.ActorName}: {state.ActiveDetectors} após entrada de {FormatDetector(engagedEvent.Detector)}. Primeiro? {engagedEvent.IsFirstEngagement}.");

            var context = _orchestrator?.ResolveEffectiveConfig(state.Planet, state.DetectionType, engagedEvent.TargetRole);
            if (context == null)
            {
                DebugUtility.LogWarning<PlanetDefenseEventService>(
                    $"Contexto não resolvido para {state.Planet?.ActorName ?? "planeta desconhecido"}; engajamento ignorado.");
                return;
            }

            _orchestrator.PrepareRunners(context);

            string targetLabel = engagedEvent.Detector.Owner?.ActorName ?? engagedEvent.Detector.ToString();
            var targetRole = context.Strategy != null
                ? context.Strategy.ResolveTargetRole(targetLabel, engagedEvent.TargetRole)
                : engagedEvent.TargetRole;

            _orchestrator.ConfigurePrimaryTarget(
                state.Planet,
                null,
                targetLabel,
                targetRole);

            if (engagedEvent.IsFirstEngagement)
            {
                _orchestrator.StartWaves(state.Planet, state.DetectionType, context.Strategy);
            }
        }

        public void HandleDisengaged(PlanetDefenseDisengagedEvent disengagedEvent)
        {
            if (disengagedEvent.Planet == null || disengagedEvent.Detector == null)
            {
                return;
            }

            var state = _stateManager.RegisterDisengagement(
                disengagedEvent.Planet,
                disengagedEvent.DetectionType,
                FormatDetector(disengagedEvent.Detector),
                Mathf.Max(disengagedEvent.ActiveDetectors, 0),
                out _);

            DebugUtility.LogVerbose<PlanetDefenseEventService>(
                $"[Debug] Detectores ativos em {disengagedEvent.Planet.ActorName}: {state?.ActiveDetectors ?? 0} após saída de {FormatDetector(disengagedEvent.Detector)}.");

            bool noDetectorsRemaining = disengagedEvent.IsLastDisengagement || state?.ActiveDetectors <= 0;

            if (!noDetectorsRemaining) return;
            DebugUtility.LogVerbose<PlanetDefenseEventService>(
                $"[Debug] Nenhum detector restante em {disengagedEvent.Planet.ActorName}. Encerrando waves e logging.");

            _orchestrator?.StopWaves(disengagedEvent.Planet);
        }

        public void HandleDisabled(PlanetDefenseDisabledEvent disabledEvent)
        {
            if (disabledEvent.Planet == null)
            {
                return;
            }

            _orchestrator?.StopWaves(disabledEvent.Planet);
            if (StopWavesOnDisable)
            {
                _orchestrator?.ReleasePools(disabledEvent.Planet);
            }

            var detectionType = _stateManager?.TryGetDetectionType(disabledEvent.Planet);
            _stateManager?.ClearPlanet(disabledEvent.Planet);
            _orchestrator?.ClearContext(disabledEvent.Planet);
        }

        public void HandleMinionSpawned(PlanetDefenseMinionSpawnedEvent spawnedEvent)
        {
            if (spawnedEvent.Planet == null || spawnedEvent.SpawnedMinion == null)
            {
                return;
            }

            DebugUtility.LogVerbose<PlanetDefenseEventService>(
                $"[SpawnEvent] Minion spawnado em {spawnedEvent.Planet.ActorName} com role '{spawnedEvent.SpawnContext.TargetRole}' " +
                $"e label '{spawnedEvent.SpawnContext.TargetLabel}'. EntryStarted={spawnedEvent.EntryPhaseStarted}.");
        }

        private void ResolveDependenciesFromProvider()
        {
            var provider = DependencyManager.Provider;

            if (string.IsNullOrEmpty(_ownerObjectId))
            {
                DebugUtility.LogWarning<PlanetDefenseEventService>(
                    "OwnerObjectId não definido; não é possível resolver dependências por objeto.");
                return;
            }

            if (_orchestrator == null && provider.TryGetForObject(_ownerObjectId, out IPlanetDefenseSetupOrchestrator resolvedOrchestrator))
            {
                _orchestrator = resolvedOrchestrator;
            }

            if (_stateManager == null && provider.TryGetGlobal(out DefenseStateManager resolvedStateManager))
            {
                _stateManager = resolvedStateManager;
            }

        }

        private static string FormatDetector(IDetector detector)
        {
            if (detector == null)
            {
                return "Um detector desconhecido";
            }

            string actorName = detector.Owner?.ActorName ?? detector.ToString();
            return actorName.Contains("Eater")
                ? $"O Eater ({actorName})"
                : actorName.Contains("Player")
                    ? $"O Player ({actorName})"
                    : actorName;
        }
    }
}
