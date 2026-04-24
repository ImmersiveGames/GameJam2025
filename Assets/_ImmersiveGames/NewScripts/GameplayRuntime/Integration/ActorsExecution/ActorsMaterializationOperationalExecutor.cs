using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorsSystem.Integration.SessionFlow;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.ActorsSystem.Semantic;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.Spawn;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution
{
    public interface IActorsMaterializationOperationalExecutor
    {
        bool TryBeginPhaseLocalEntryReadyDispatch(ActorsMaterializationExecutionCycle cycle, out string status);
        void ReleasePhaseLocalEntryReadyDispatch(ActorsMaterializationExecutionCycle cycle);
        void PrimeCanonicalGameplayEntry(SessionTransitionPhaseLocalEntryReadyEvent evt);
        Task ExecuteCurrentAsync(string source, ActorsMaterializationExecutionCycle cycle, string preferredSceneName = null);
    }

    /// <summary>
    /// Operational consumer of Actors materialization execution directives.
    /// Executes only RequestMaterialize/RequestRematerialize and keeps Flag/NoAction as signal-only.
    /// </summary>
    public sealed class ActorsMaterializationOperationalExecutor : IActorsMaterializationOperationalExecutor
    {
        private readonly IActorsMaterializationExecutionPolicyService _executionPolicyService;
        private readonly IDependencyProvider _provider;
        private readonly IActorsMaterializationExecutionCycleContext _cycleContext;
        private readonly Dictionary<string, string> _lastExecutionStampByScene = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _lastExecutionStampBySceneAndMode = new(StringComparer.Ordinal);
        private readonly List<IWorldSpawnService> _servicesBuffer = new(16);
        private readonly Dictionary<ActorKind, IWorldSpawnService> _serviceByKind = new();
        private readonly object _phaseLocalEntryReadySync = new();
        private string _activePhaseLocalEntryReadyCycleStamp = string.Empty;
        private bool _phaseLocalEntryReadyDispatchReserved;
        private bool _executionInProgress;

        public ActorsMaterializationOperationalExecutor(
            IActorsMaterializationExecutionPolicyService executionPolicyService,
            IDependencyProvider provider,
            IActorsMaterializationExecutionCycleContext cycleContext)
        {
            _executionPolicyService = executionPolicyService ?? throw new ArgumentNullException(nameof(executionPolicyService));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _cycleContext = cycleContext ?? throw new ArgumentNullException(nameof(cycleContext));
        }

        public bool TryBeginPhaseLocalEntryReadyDispatch(ActorsMaterializationExecutionCycle cycle, out string status)
        {
            status = string.Empty;

            if (!cycle.IsValid)
            {
                status = "invalid_cycle";
                return false;
            }

            string cycleStamp = cycle.ToStampKey();
            lock (_phaseLocalEntryReadySync)
            {
                if (_phaseLocalEntryReadyDispatchReserved)
                {
                    status = string.Equals(_activePhaseLocalEntryReadyCycleStamp, cycleStamp, StringComparison.Ordinal)
                        ? "duplicate_in_progress"
                        : "busy_in_progress";
                    return false;
                }

                _phaseLocalEntryReadyDispatchReserved = true;
                _activePhaseLocalEntryReadyCycleStamp = cycleStamp;
                status = "accepted";
                return true;
            }
        }

        public void ReleasePhaseLocalEntryReadyDispatch(ActorsMaterializationExecutionCycle cycle)
        {
            string cycleStamp = cycle.IsValid ? cycle.ToStampKey() : string.Empty;

            lock (_phaseLocalEntryReadySync)
            {
                if (!_phaseLocalEntryReadyDispatchReserved)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(cycleStamp) &&
                    !string.Equals(_activePhaseLocalEntryReadyCycleStamp, cycleStamp, StringComparison.Ordinal))
                {
                    return;
                }

                _phaseLocalEntryReadyDispatchReserved = false;
                _activePhaseLocalEntryReadyCycleStamp = string.Empty;
            }
        }

        public void PrimeCanonicalGameplayEntry(SessionTransitionPhaseLocalEntryReadyEvent evt)
        {
            if (!evt.HasCanonicalPayload)
            {
                HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                    "[FATAL][H1][ActorsExecution] SessionTransitionPhaseLocalEntryReadyEvent sem payload canonico nao pode primar o refresh semantico.");
            }

            if (!_provider.TryGetGlobal<SessionFlowActorsSemanticPortsAdapter>(out var adapter) || adapter == null)
            {
                HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                    "[FATAL][H1][ActorsExecution] SessionFlowActorsSemanticPortsAdapter ausente para primar o refresh semantico phase-local-entry-ready.");
            }

            adapter.PrimeCanonicalGameplayEntry(evt);

            try
            {
                DebugUtility.Log(typeof(ActorsMaterializationOperationalExecutor),
                    $"[OBS][ActorsExecution][Operational] PhaseLocalEntryReady refresh started actorSetRef='{evt.ActorSetRef}' routeId='{evt.RouteId}' routeKind='{evt.RouteKind}' scene='{evt.SceneName}' reason='{evt.Reason}' participationSignature='{AsText(evt.ParticipationSignature)}' cycleSignature='{AsText(evt.CycleSignature)}'.",
                    DebugUtility.Colors.Info);

                ActorsDefinitionsSnapshot definitions = RefreshDefinitionsOrFail();
                ActorsEnsembleSnapshot ensemble = RefreshEnsembleOrFail();
                ActorsPresenceSnapshot presence = RefreshPresenceOrFail();
                ActorsMaterializationPlanSnapshot plan = RefreshPlanOrFail();
                ActorsMaterializationExecutionSnapshot execution = RefreshExecutionPolicyOrFail();

                bool hasPlayerDefinition = HasDefinitionForActorSpec(definitions, "actor.player");
                int playerDirectives = CountPlayerDirectives(execution);

                if (!definitions.IsValid || definitions.Count < 1 || !hasPlayerDefinition)
                {
                    HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                        $"[FATAL][H1][ActorsExecution] Definitions insuficientes para phase-local-entry-ready actorSetRef='{evt.ActorSetRef}' definitionsCount='{definitions.Count}' hasPlayer='{hasPlayerDefinition}'.");
                }

                if (!plan.IsValid || plan.Count < 1)
                {
                    HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                        $"[FATAL][H1][ActorsExecution] Plan materialization vazio para phase-local-entry-ready actorSetRef='{evt.ActorSetRef}' planCount='{plan.Count}' planSignature='{AsText(plan.PlanSignature)}'.");
                }

                if (!execution.IsValid || execution.Count < 1 || playerDirectives < 1)
                {
                    HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                        $"[FATAL][H1][ActorsExecution] Execution policy vazio para phase-local-entry-ready actorSetRef='{evt.ActorSetRef}' executionCount='{execution.Count}' requestMaterialize='{execution.RequestMaterializeCount}' requestRematerialize='{execution.RequestRematerializeCount}'.");
                }

                DebugUtility.Log(typeof(ActorsMaterializationOperationalExecutor),
                    $"[OBS][ActorsExecution][Operational] PhaseLocalEntryReady refresh completed actorSetRef='{evt.ActorSetRef}' definitions='{definitions.Count}' ensemble='{ensemble.Count}' presence='{presence.Count}' planEntries='{plan.Count}' executionEntries='{execution.Count}' playerDefinitions='{(hasPlayerDefinition ? 1 : 0)}' playerDirectives='{playerDirectives}'.",
                    DebugUtility.Colors.Success);
            }
            catch
            {
                adapter.ClearCanonicalGameplayEntryContext();
                throw;
            }
        }

        public async Task ExecuteCurrentAsync(string source, ActorsMaterializationExecutionCycle cycle, string preferredSceneName = null)
        {
            if (_executionInProgress)
            {
                DebugUtility.LogVerbose(typeof(ActorsMaterializationOperationalExecutor),
                    $"[OBS][ActorsExecution][Operational] Execucao ignorada (ja em progresso) source='{AsText(source)}' cycle='{cycle.ToStampKey()}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _executionInProgress = true;
            try
            {
                string sceneName = ResolveSceneName(preferredSceneName);
                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    DebugUtility.LogWarning(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] Cena alvo ausente para executar directives source='{AsText(source)}'.");
                    return;
                }

                if (!cycle.IsValid)
                {
                    DebugUtility.LogWarning(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] Ciclo operacional invalido para executar directives source='{AsText(source)}' scene='{sceneName}'.");
                    return;
                }

                string dispatchMode = ResolveDispatchMode(source);
                ActorsMaterializationExecutionSnapshot snapshot = _executionPolicyService.Refresh();
                if (!snapshot.IsValid)
                {
                    DebugUtility.LogVerbose(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] Snapshot invalido source='{AsText(source)}' scene='{sceneName}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (AlreadyExecuted(sceneName, dispatchMode, cycle, snapshot.ExecutionSignature))
                {
                    DebugUtility.LogVerbose(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] Dispatch ja executado source='{AsText(source)}' dispatchMode='{dispatchMode}' scene='{sceneName}' executionSignature='{snapshot.ExecutionSignature}' executionCycle='{cycle.ToStampKey()}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (!_provider.TryGetForScene<IWorldSpawnServiceRegistry>(sceneName, out var spawnRegistry) || spawnRegistry == null)
                {
                    DebugUtility.LogWarning(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] IWorldSpawnServiceRegistry ausente para scene='{sceneName}' source='{AsText(source)}'.");
                    return;
                }

                BuildServiceIndex(spawnRegistry);
                LogDispatchSummary(sceneName, source, dispatchMode, snapshot);
                using (_cycleContext.OpenScope(cycle, source))
                {
                    await DispatchAsync(sceneName, snapshot, source, dispatchMode);
                }

                EventBus<ActorsOperationalMaterializationCycleCompletedEvent>.Raise(
                    new ActorsOperationalMaterializationCycleCompletedEvent(
                        sceneName,
                        cycle,
                        source,
                        snapshot.ExecutionSignature));

                _lastExecutionStampByScene[sceneName] = BuildExecutionStamp(cycle, snapshot.ExecutionSignature);
                _lastExecutionStampBySceneAndMode[BuildSceneDispatchKey(sceneName, dispatchMode)] = BuildExecutionStamp(cycle, snapshot.ExecutionSignature);
            }
            finally
            {
                if (IsPhaseLocalEntryReadySource(source) &&
                    _provider.TryGetGlobal<SessionFlowActorsSemanticPortsAdapter>(out var adapter) &&
                    adapter != null)
                {
                    adapter.ClearCanonicalGameplayEntryContext();
                }

                if (IsPhaseLocalEntryReadySource(source))
                {
                    ReleasePhaseLocalEntryReadyDispatch(cycle);
                }

                _executionInProgress = false;
            }
        }

        private bool AlreadyExecuted(string sceneName, ActorsMaterializationExecutionCycle cycle, string executionSignature)
        {
            if (string.IsNullOrWhiteSpace(executionSignature))
            {
                return false;
            }

            string currentStamp = BuildExecutionStamp(cycle, executionSignature);
            if (string.IsNullOrWhiteSpace(currentStamp))
            {
                return false;
            }

            return _lastExecutionStampByScene.TryGetValue(sceneName, out string previous) &&
                   string.Equals(previous, currentStamp, StringComparison.Ordinal);
        }

        private bool AlreadyExecuted(string sceneName, string dispatchMode, ActorsMaterializationExecutionCycle cycle, string executionSignature)
        {
            if (string.IsNullOrWhiteSpace(dispatchMode) || string.IsNullOrWhiteSpace(executionSignature))
            {
                return false;
            }

            string currentStamp = BuildExecutionStamp(cycle, executionSignature);
            if (string.IsNullOrWhiteSpace(currentStamp))
            {
                return false;
            }

            return _lastExecutionStampBySceneAndMode.TryGetValue(BuildSceneDispatchKey(sceneName, dispatchMode), out string previous) &&
                   string.Equals(previous, currentStamp, StringComparison.Ordinal);
        }

        private static string BuildExecutionStamp(ActorsMaterializationExecutionCycle cycle, string executionSignature)
        {
            if (!cycle.IsValid || string.IsNullOrWhiteSpace(executionSignature))
            {
                return string.Empty;
            }

            return $"{cycle.ToStampKey()}|executionSignature:{executionSignature.Trim()}";
        }

        private static string BuildSceneDispatchKey(string sceneName, string dispatchMode)
        {
            return $"{AsText(sceneName)}|dispatchMode:{AsText(dispatchMode)}";
        }

        private static string ResolveSceneName(string preferredSceneName)
        {
            if (!string.IsNullOrWhiteSpace(preferredSceneName))
            {
                return preferredSceneName.Trim();
            }

            Scene activeScene = SceneManager.GetActiveScene();
            return activeScene.IsValid() ? activeScene.name : string.Empty;
        }

        private static string ResolveDispatchMode(string source)
        {
            if (IsPhaseLocalEntryReadySource(source))
            {
                return "phase-local-entry-ready";
            }

            return "phase-runtime-materialized";
        }

        private static bool IsPhaseLocalEntryReadySource(string source)
        {
            return !string.IsNullOrWhiteSpace(source) &&
                   source.IndexOf("PhaseLocalEntryReady", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void BuildServiceIndex(IWorldSpawnServiceRegistry spawnRegistry)
        {
            _servicesBuffer.Clear();
            _serviceByKind.Clear();

            IReadOnlyList<IWorldSpawnService> services = spawnRegistry.Services;
            for (int index = 0; index < services.Count; index += 1)
            {
                IWorldSpawnService service = services[index];
                if (service == null)
                {
                    continue;
                }

                _servicesBuffer.Add(service);

                ActorKind kind = service.SpawnedActorKind;
                if (kind == ActorKind.Unknown || _serviceByKind.ContainsKey(kind))
                {
                    continue;
                }

                _serviceByKind.Add(kind, service);
            }
        }

        private void LogDispatchSummary(
            string sceneName,
            string source,
            string dispatchMode,
            ActorsMaterializationExecutionSnapshot snapshot)
        {
            int playerCount = 0;
            int dummyCount = 0;
            int eaterCount = 0;
            int materializeCount = 0;
            int rematerializeCount = 0;
            int noActionCount = 0;

            ActorsMaterializationExecutionEntry[] entries = snapshot.Entries ?? Array.Empty<ActorsMaterializationExecutionEntry>();
            for (int index = 0; index < entries.Length; index += 1)
            {
                ActorsMaterializationExecutionEntry entry = entries[index];
                if (!entry.IsValid)
                {
                    continue;
                }

                switch (entry.Directive)
                {
                    case ActorMaterializationExecutionDirective.RequestMaterialize:
                        materializeCount += 1;
                        break;
                    case ActorMaterializationExecutionDirective.RequestRematerialize:
                        rematerializeCount += 1;
                        break;
                    default:
                        noActionCount += 1;
                        break;
                }

                ActorKind kind = MapRecipeToActorKind(entry.OperationalRecipeKind);
                switch (kind)
                {
                    case ActorKind.Player:
                        playerCount += 1;
                        break;
                    case ActorKind.Dummy:
                        dummyCount += 1;
                        break;
                    case ActorKind.Eater:
                        eaterCount += 1;
                        break;
                }
            }

            DebugUtility.Log(typeof(ActorsMaterializationOperationalExecutor),
                $"[OBS][ActorsExecution][Operational] Dispatch iniciado source='{AsText(source)}' dispatchMode='{dispatchMode}' scene='{sceneName}' entries='{entries.Length}' player='{playerCount}' dummy='{dummyCount}' eater='{eaterCount}' materialize='{materializeCount}' rematerialize='{rematerializeCount}' noAction='{noActionCount}' executionSignature='{snapshot.ExecutionSignature}'.",
                DebugUtility.Colors.Info);
        }

        private ActorsDefinitionsSnapshot RefreshDefinitionsOrFail()
        {
            if (!_provider.TryGetGlobal<IActorsDefinitionsPort>(out var definitionsPort) || definitionsPort == null)
            {
                HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                    "[FATAL][H1][ActorsExecution] IActorsDefinitionsPort ausente para refresh semantico phase-local-entry-ready.");
            }

            ActorsDefinitionsSnapshot snapshot = definitionsPort.TryGetCurrent(out ActorsDefinitionsSnapshot current) && current.IsValid
                ? current
                : ActorsDefinitionsSnapshot.Empty;

            if (!snapshot.IsValid)
            {
                HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                    "[FATAL][H1][ActorsExecution] Definitions invalidas apos refresh semantico phase-local-entry-ready.");
            }

            return snapshot;
        }

        private ActorsEnsembleSnapshot RefreshEnsembleOrFail()
        {
            if (!_provider.TryGetGlobal<IActorsEnsembleService>(out var ensembleService) || ensembleService == null)
            {
                HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                    "[FATAL][H1][ActorsExecution] IActorsEnsembleService ausente para refresh semantico phase-local-entry-ready.");
            }

            ActorsEnsembleSnapshot snapshot = ensembleService.Refresh();
            if (!snapshot.IsValid)
            {
                HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                    "[FATAL][H1][ActorsExecution] Ensemble invalido apos refresh semantico phase-local-entry-ready.");
            }

            return snapshot;
        }

        private ActorsPresenceSnapshot RefreshPresenceOrFail()
        {
            if (!_provider.TryGetGlobal<IActorsPresenceService>(out var presenceService) || presenceService == null)
            {
                HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                    "[FATAL][H1][ActorsExecution] IActorsPresenceService ausente para refresh semantico phase-local-entry-ready.");
            }

            ActorsPresenceSnapshot snapshot = presenceService.Refresh();
            if (!snapshot.IsValid)
            {
                HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                    "[FATAL][H1][ActorsExecution] Presence invalida apos refresh semantico phase-local-entry-ready.");
            }

            return snapshot;
        }

        private ActorsMaterializationPlanSnapshot RefreshPlanOrFail()
        {
            if (!_provider.TryGetGlobal<IActorsMaterializationPlanService>(out var planService) || planService == null)
            {
                HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                    "[FATAL][H1][ActorsExecution] IActorsMaterializationPlanService ausente para refresh semantico phase-local-entry-ready.");
            }

            ActorsMaterializationPlanSnapshot snapshot = planService.Refresh();
            if (!snapshot.IsValid)
            {
                HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                    "[FATAL][H1][ActorsExecution] Plan invalido apos refresh semantico phase-local-entry-ready.");
            }

            return snapshot;
        }

        private ActorsMaterializationExecutionSnapshot RefreshExecutionPolicyOrFail()
        {
            if (!_provider.TryGetGlobal<IActorsMaterializationExecutionPolicyService>(out var executionPolicyService) || executionPolicyService == null)
            {
                HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                    "[FATAL][H1][ActorsExecution] IActorsMaterializationExecutionPolicyService ausente para refresh semantico phase-local-entry-ready.");
            }

            ActorsMaterializationExecutionSnapshot snapshot = executionPolicyService.Refresh();
            if (!snapshot.IsValid)
            {
                HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                    "[FATAL][H1][ActorsExecution] Execution policy invalida apos refresh semantico phase-local-entry-ready.");
            }

            return snapshot;
        }

        private static bool HasDefinitionForActorSpec(ActorsDefinitionsSnapshot snapshot, string actorSpecId)
        {
            if (!snapshot.IsValid || snapshot.Entries == null || snapshot.Entries.Length == 0 || string.IsNullOrWhiteSpace(actorSpecId))
            {
                return false;
            }

            for (int index = 0; index < snapshot.Entries.Length; index += 1)
            {
                ActorDefinitionRecord entry = snapshot.Entries[index];
                if (entry.IsValid && string.Equals(entry.ActorSpecId, actorSpecId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountPlayerDirectives(ActorsMaterializationExecutionSnapshot snapshot)
        {
            if (!snapshot.IsValid || snapshot.Entries == null || snapshot.Entries.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < snapshot.Entries.Length; index += 1)
            {
                ActorsMaterializationExecutionEntry entry = snapshot.Entries[index];
                if (!entry.IsValid || MapRecipeToActorKind(entry.OperationalRecipeKind) != ActorKind.Player)
                {
                    continue;
                }

                if (entry.Directive == ActorMaterializationExecutionDirective.RequestMaterialize ||
                    entry.Directive == ActorMaterializationExecutionDirective.RequestRematerialize)
                {
                    count += 1;
                }
            }

            return count;
        }

        private async Task DispatchAsync(string sceneName, ActorsMaterializationExecutionSnapshot snapshot, string source, string dispatchMode)
        {
            var materializeEntries = new Dictionary<ActorKind, ActorsMaterializationExecutionEntry>();
            var rematerializeEntries = new Dictionary<ActorKind, ActorsMaterializationExecutionEntry>();

            ActorsMaterializationExecutionEntry[] entries = snapshot.Entries ?? Array.Empty<ActorsMaterializationExecutionEntry>();
            for (int index = 0; index < entries.Length; index += 1)
            {
                ActorsMaterializationExecutionEntry entry = entries[index];
                if (!entry.IsValid)
                {
                    continue;
                }

                ActorKind targetKind = MapRecipeToActorKind(entry.OperationalRecipeKind);
                if (!IsDispatchAllowedForMode(dispatchMode, targetKind))
                {
                    DebugUtility.LogVerbose(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] Directive ignorada pelo modo de dispatch dispatchMode='{dispatchMode}' actorKind='{targetKind}' axisActorId='{entry.AxisActorId}' semanticParticipantId='{AsText(entry.SemanticParticipantId)}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' directive='{entry.Directive}'.",
                        DebugUtility.Colors.Info);
                    continue;
                }

                switch (entry.Directive)
                {
                    case ActorMaterializationExecutionDirective.RequestMaterialize:
                        if (TryResolveTargetKind(entry, out ActorKind materializeKind))
                        {
                            if (!rematerializeEntries.ContainsKey(materializeKind))
                            {
                                materializeEntries[materializeKind] = entry;
                            }
                        }
                        break;

                    case ActorMaterializationExecutionDirective.RequestRematerialize:
                        if (TryResolveTargetKind(entry, out ActorKind rematerializeKind))
                        {
                            rematerializeEntries[rematerializeKind] = entry;
                            materializeEntries.Remove(rematerializeKind);
                        }
                        break;

                    case ActorMaterializationExecutionDirective.NoActionStable:
                    case ActorMaterializationExecutionDirective.NoActionObserve:
                    case ActorMaterializationExecutionDirective.FlagInconsistentNoAutoRemediation:
                    case ActorMaterializationExecutionDirective.FlagRuntimeOrphanTolerated:
                    case ActorMaterializationExecutionDirective.FlagRuntimeOrphanProblematic:
                        DebugUtility.LogVerbose(typeof(ActorsMaterializationOperationalExecutor),
                            $"[OBS][ActorsExecution][Operational] Directive sem acao automatica directive='{entry.Directive}' axisActorId='{entry.AxisActorId}' runtimeActorId='{entry.RuntimeActorId}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' scene='{sceneName}' source='{AsText(source)}'.",
                            DebugUtility.Colors.Info);
                        break;
                }
            }

            foreach (KeyValuePair<ActorKind, ActorsMaterializationExecutionEntry> pair in rematerializeEntries)
            {
                ActorKind kind = pair.Key;
                ActorsMaterializationExecutionEntry entry = pair.Value;
                if (!_serviceByKind.TryGetValue(kind, out IWorldSpawnService service) || service == null)
                {
                    DebugUtility.LogWarning(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] Service ausente para rematerializar actorKind='{kind}' axisActorId='{entry.AxisActorId}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' scene='{sceneName}' source='{AsText(source)}'.");
                    continue;
                }

                await service.DespawnAsync();
                await service.SpawnAsync(CreateSpawnRequest(sceneName, source, service, entry, snapshot.ExecutionSignature));

                DebugUtility.Log(typeof(ActorsMaterializationOperationalExecutor),
                    $"[OBS][ActorsExecution][Operational] Rematerialize executado actorKind='{kind}' service='{service.Name}' axisActorId='{entry.AxisActorId}' runtimeActorId='{entry.RuntimeActorId}' semanticParticipantId='{AsText(entry.SemanticParticipantId)}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' scene='{sceneName}' source='{AsText(source)}'.",
                    DebugUtility.Colors.Info);
            }

            foreach (KeyValuePair<ActorKind, ActorsMaterializationExecutionEntry> pair in materializeEntries)
            {
                ActorKind kind = pair.Key;
                ActorsMaterializationExecutionEntry entry = pair.Value;
                if (!_serviceByKind.TryGetValue(kind, out IWorldSpawnService service) || service == null)
                {
                    DebugUtility.LogWarning(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] Service ausente para materializar actorKind='{kind}' axisActorId='{entry.AxisActorId}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' scene='{sceneName}' source='{AsText(source)}'.");
                    continue;
                }

                await service.SpawnAsync(CreateSpawnRequest(sceneName, source, service, entry, snapshot.ExecutionSignature));

                DebugUtility.Log(typeof(ActorsMaterializationOperationalExecutor),
                    $"[OBS][ActorsExecution][Operational] Materialize executado actorKind='{kind}' service='{service.Name}' axisActorId='{entry.AxisActorId}' runtimeActorId='{entry.RuntimeActorId}' semanticParticipantId='{AsText(entry.SemanticParticipantId)}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' scene='{sceneName}' source='{AsText(source)}'.",
                    DebugUtility.Colors.Info);
            }
        }

        private static bool IsDispatchAllowedForMode(string dispatchMode, ActorKind actorKind)
        {
            if (actorKind == ActorKind.Unknown)
            {
                return false;
            }

            if (string.Equals(dispatchMode, "phase-local-entry-ready", StringComparison.Ordinal))
            {
                return true;
            }

            return actorKind != ActorKind.Player;
        }

        private bool TryResolveTargetKind(ActorsMaterializationExecutionEntry entry, out ActorKind kind)
        {
            kind = ActorKind.Unknown;
            kind = MapRecipeToActorKind(entry.OperationalRecipeKind);
            if (kind != ActorKind.Unknown)
            {
                return true;
            }

            DebugUtility.LogWarning(typeof(ActorsMaterializationOperationalExecutor),
                $"[OBS][ActorsExecution][Operational] Recipe operacional ausente/indeterminada recipe='{entry.OperationalRecipeKind}' role='{entry.Role}' axisActorId='{entry.AxisActorId}' semanticParticipantId='{AsText(entry.SemanticParticipantId)}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' directive='{entry.Directive}'.");
            return false;
        }

        private static ActorSpawnRequest CreateSpawnRequest(
            string sceneName,
            string source,
            IWorldSpawnService service,
            ActorsMaterializationExecutionEntry entry,
            string executionSignature)
        {
            return new ActorSpawnRequest(
                service.SpawnedActorKind,
                entry.OperationalRecipeKind,
                entry.AxisActorId,
                RuntimeActorId.None,
                entry.ActorSpecId,
                entry.ActorSetRef,
                entry.SemanticParticipantId,
                service.Name,
                sceneName,
                service.IsRequiredForWorldReset,
                source,
                entry.Reason,
                executionSignature);
        }

        private static ActorKind MapRecipeToActorKind(ActorOperationalRecipeKind recipeKind)
        {
            return recipeKind switch
            {
                ActorOperationalRecipeKind.Player => ActorKind.Player,
                ActorOperationalRecipeKind.Dummy => ActorKind.Dummy,
                ActorOperationalRecipeKind.Eater => ActorKind.Eater,
                _ => ActorKind.Unknown
            };
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }

    public sealed class ActorsMaterializationExecutionRuntimeBridge : IDisposable
    {
        private readonly IActorsMaterializationOperationalExecutor _executor;
        private readonly EventBinding<GameplayPhaseRuntimeMaterializedEvent> _phaseRuntimeMaterializedBinding;
        private bool _disposed;

        public ActorsMaterializationExecutionRuntimeBridge(IActorsMaterializationOperationalExecutor executor)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _phaseRuntimeMaterializedBinding = new EventBinding<GameplayPhaseRuntimeMaterializedEvent>(OnPhaseRuntimeMaterialized);
            EventBus<GameplayPhaseRuntimeMaterializedEvent>.Register(_phaseRuntimeMaterializedBinding);

            DebugUtility.Log(typeof(ActorsMaterializationExecutionRuntimeBridge),
                "[OBS][ActorsExecution][Operational] Runtime bridge registrado (GameplayPhaseRuntimeMaterialized -> ExecutionDirective dispatch).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<GameplayPhaseRuntimeMaterializedEvent>.Unregister(_phaseRuntimeMaterializedBinding);
        }

        private void OnPhaseRuntimeMaterialized(GameplayPhaseRuntimeMaterializedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            if (!evt.Runtime.IsValid || evt.Runtime.PhaseDefinitionRef == null)
            {
                DebugUtility.LogWarning(typeof(ActorsMaterializationExecutionRuntimeBridge),
                    "[OBS][ActorsExecution][Operational] GameplayPhaseRuntimeMaterialized ignorado por runtime invalido/phaseDefinitionRef ausente.");
                return;
            }

            DebugUtility.Log(typeof(ActorsMaterializationExecutionRuntimeBridge),
                $"[OBS][ActorsExecution][Operational] GameplayPhaseRuntimeMaterialized recebido em modo observabilidade-only dispatchMode='phase-runtime-materialized' source='GameplaySessionFlow/PhaseRuntimeMaterialized' phaseLocalEntrySequence='{evt.PhaseLocalEntrySequence}' entrySignature='{(string.IsNullOrWhiteSpace(evt.EntrySignature) ? "<none>" : evt.EntrySignature.Trim())}'.",
                DebugUtility.Colors.Info);
        }
    }

    public sealed class SessionTransitionPhaseLocalEntryReadyMaterializationBridge : IDisposable
    {
        private readonly IActorsMaterializationOperationalExecutor _executor;
        private readonly EventBinding<SessionTransitionPhaseLocalEntryReadyEvent> _phaseLocalEntryReadyBinding;
        private bool _disposed;

        public SessionTransitionPhaseLocalEntryReadyMaterializationBridge(IActorsMaterializationOperationalExecutor executor)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _phaseLocalEntryReadyBinding = new EventBinding<SessionTransitionPhaseLocalEntryReadyEvent>(OnPhaseLocalEntryReady);
            EventBus<SessionTransitionPhaseLocalEntryReadyEvent>.Register(_phaseLocalEntryReadyBinding);

            DebugUtility.Log(typeof(SessionTransitionPhaseLocalEntryReadyMaterializationBridge),
                "[OBS][ActorsExecution][Operational] Runtime bridge registrado (SessionTransitionPhaseLocalEntryReady -> materialization dispatch).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<SessionTransitionPhaseLocalEntryReadyEvent>.Unregister(_phaseLocalEntryReadyBinding);
        }

        private void OnPhaseLocalEntryReady(SessionTransitionPhaseLocalEntryReadyEvent evt)
        {
            if (_disposed || !evt.IsValid || !evt.IsPhaseLocalEntry)
            {
                return;
            }

            SessionTransitionPlan plan = evt.Plan;
            if (!plan.IsValid || !plan.ContinuationContext.IsValid)
            {
                DebugUtility.LogWarning(typeof(SessionTransitionPhaseLocalEntryReadyMaterializationBridge),
                    "[OBS][ActorsExecution][Operational] SessionTransitionPhaseLocalEntryReady ignorado por contexto invalido.");
                return;
            }

            if (!evt.HasCanonicalPayload ||
                evt.RouteKind != SceneRouteKind.Gameplay ||
                string.IsNullOrWhiteSpace(evt.SceneName) ||
                string.IsNullOrWhiteSpace(evt.ActorSetRef) ||
                !evt.RouteId.IsValid ||
                string.IsNullOrWhiteSpace(evt.CycleSignature))
            {
                DebugUtility.LogWarning(typeof(SessionTransitionPhaseLocalEntryReadyMaterializationBridge),
                    $"[OBS][ActorsExecution][Operational] SessionTransitionPhaseLocalEntryReady ignorado por payload canonico ausente routeId='{evt.RouteId}' routeKind='{evt.RouteKind}' scene='{evt.SceneName}' actorSetRef='{AsText(evt.ActorSetRef)}' reason='{AsText(evt.Reason)}' cycleSignature='{AsText(evt.CycleSignature)}'.");
                return;
            }

            ActorsMaterializationExecutionCycle cycle = ActorsMaterializationExecutionCycle.CreateOrFail(
                Math.Max(1, plan.ContinuationContext.Frame),
                BuildEntrySignature(evt),
                nameof(SessionTransitionPhaseLocalEntryReadyEvent));

            if (!_executor.TryBeginPhaseLocalEntryReadyDispatch(cycle, out string status))
            {
                DebugUtility.LogVerbose(typeof(SessionTransitionPhaseLocalEntryReadyMaterializationBridge),
                    $"[OBS][ActorsExecution][Operational] SessionTransitionPhaseLocalEntryReady duplicate_in_progress status='{status}' routeId='{evt.RouteId}' routeKind='{evt.RouteKind}' scene='{evt.SceneName}' actorSetRef='{AsText(evt.ActorSetRef)}' entrySignature='{cycle.EntrySignature}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            try
            {
                DebugUtility.Log(typeof(SessionTransitionPhaseLocalEntryReadyMaterializationBridge),
                    $"[OBS][ActorsExecution][Operational] SessionTransitionPhaseLocalEntryReady recebido routeId='{evt.RouteId}' routeKind='{evt.RouteKind}' scene='{evt.SceneName}' actorSetRef='{AsText(evt.ActorSetRef)}' continuation='{plan.ResolvedContinuation}' frame='{plan.ContinuationContext.Frame}' reason='{plan.Reason}' source='{evt.Source}' sessionSignature='{AsText(evt.SessionSignature)}' phaseSignature='{AsText(evt.PhaseSignature)}' participationSignature='{AsText(evt.ParticipationSignature)}' entrySignature='{cycle.EntrySignature}'.",
                    DebugUtility.Colors.Info);

                _executor.PrimeCanonicalGameplayEntry(evt);
                _ = _executor.ExecuteCurrentAsync("SessionTransition/PhaseLocalEntryReady", cycle, evt.SceneName);
            }
            catch
            {
                _executor.ReleasePhaseLocalEntryReadyDispatch(cycle);
                throw;
            }
        }

        private static string BuildEntrySignature(SessionTransitionPhaseLocalEntryReadyEvent evt)
        {
            if (!string.IsNullOrWhiteSpace(evt.CycleSignature))
            {
                return evt.CycleSignature.Trim();
            }

            SessionTransitionContext context = evt.Plan.Context;
            RunContinuationContext continuationContext = evt.Plan.ContinuationContext;
            return $"phase-local-entry-ready|signature:{AsText(continuationContext.Signature)}|continuation:{evt.Plan.ResolvedContinuation}|scene:{AsText(continuationContext.SceneName)}|reason:{AsText(context.Reason)}|source:{AsText(evt.Source)}";
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }
}
