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
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
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
        Task ExecuteCurrentAsync(ActorsOperationalMaterializationDispatchMode dispatchMode, string sourceId, ActorsMaterializationExecutionCycle cycle, string preferredSceneName = null);
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
        private bool _phaseLocalEntryReadyPreserveExistingEnabled;
        private string _phaseLocalEntryReadyContinuation = string.Empty;

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

                _phaseLocalEntryReadyPreserveExistingEnabled = ShouldPreserveExistingForPhaseLocalEntryReady(evt);
                _phaseLocalEntryReadyContinuation = ResolvePhaseIntentLabel(evt);

                ActorsDefinitionsSnapshot definitions = RefreshDefinitionsOrFail();
                ActorsEnsembleSnapshot ensemble = RefreshEnsembleOrFail();
                ActorsPresenceSnapshot presence = RefreshPresenceOrFail();
                ActorsMaterializationPlanSnapshot plan = RefreshPlanOrFail();
                ActorsMaterializationExecutionSnapshot execution = ProjectPreserveExistingForPhaseLocalEntryReady(
                    RefreshExecutionPolicyOrFail(),
                    evt,
                    definitions,
                    presence,
                    plan);

                bool hasPlayerDefinition = HasDefinitionForActorSpec(definitions, "actor.player");
                int playerReadyDirectives = CountPlayerReadyDirectives(execution);

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

                if (!execution.IsValid || execution.Count < 1 || playerReadyDirectives < 1)
                {
                    HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                        $"[FATAL][H1][ActorsExecution] Execution policy vazio para phase-local-entry-ready actorSetRef='{evt.ActorSetRef}' executionCount='{execution.Count}' requestMaterialize='{execution.RequestMaterializeCount}' requestRematerialize='{execution.RequestRematerializeCount}' preserveExisting='{CountDirectives(execution, ActorMaterializationExecutionDirective.PreserveExisting)}' readyDirectiveCount='{playerReadyDirectives}' continuation='{evt.Plan.ResolvedContinuation}'.");
                }

                DebugUtility.Log(typeof(ActorsMaterializationOperationalExecutor),
                    $"[OBS][ActorsExecution][Operational] PhaseLocalEntryReady refresh completed actorSetRef='{evt.ActorSetRef}' definitions='{definitions.Count}' ensemble='{ensemble.Count}' presence='{presence.Count}' planEntries='{plan.Count}' executionEntries='{execution.Count}' playerDefinitions='{(hasPlayerDefinition ? 1 : 0)}' readyDirectiveCount='{playerReadyDirectives}' preserveExisting='{CountDirectives(execution, ActorMaterializationExecutionDirective.PreserveExisting)}' continuation='{evt.Plan.ResolvedContinuation}'.",
                    DebugUtility.Colors.Success);
            }
            catch
            {
                _phaseLocalEntryReadyPreserveExistingEnabled = false;
                _phaseLocalEntryReadyContinuation = string.Empty;
                adapter.ClearCanonicalGameplayEntryContext();
                throw;
            }
        }

        public async Task ExecuteCurrentAsync(ActorsOperationalMaterializationDispatchMode dispatchMode, string sourceId, ActorsMaterializationExecutionCycle cycle, string preferredSceneName = null)
        {
            if (_executionInProgress)
            {
                DebugUtility.LogVerbose(typeof(ActorsMaterializationOperationalExecutor),
                    $"[OBS][ActorsExecution][Operational] Execucao ignorada (ja em progresso) sourceId='{AsText(sourceId)}' dispatchMode='{dispatchMode.ToLogToken()}' cycle='{cycle.ToStampKey()}'.",
                    DebugUtility.Colors.Info);

                if (dispatchMode == ActorsOperationalMaterializationDispatchMode.PhaseLocalEntryReady)
                {
                    _phaseLocalEntryReadyPreserveExistingEnabled = false;
                    _phaseLocalEntryReadyContinuation = string.Empty;
                    ClearCanonicalGameplayEntryContextIfAvailable();
                    ReleasePhaseLocalEntryReadyDispatch(cycle);

                    DebugUtility.LogVerbose(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] PhaseLocalEntryReady dispatch rejected reason='busy_in_progress' contextCleared='true' sourceId='{AsText(sourceId)}' cycle='{cycle.ToStampKey()}'.",
                        DebugUtility.Colors.Info);
                }

                return;
            }

            _executionInProgress = true;
            try
            {
                string sceneName = ResolveSceneName(preferredSceneName);
                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    DebugUtility.LogWarning(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] Cena alvo ausente para executar directives sourceId='{AsText(sourceId)}' dispatchMode='{dispatchMode.ToLogToken()}'.");
                    return;
                }

                if (!cycle.IsValid)
                {
                    DebugUtility.LogWarning(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] Ciclo operacional invalido para executar directives sourceId='{AsText(sourceId)}' dispatchMode='{dispatchMode.ToLogToken()}' scene='{sceneName}'.");
                    return;
                }

                ActorsMaterializationExecutionSnapshot snapshot = ProjectPreserveExistingForDispatch(
                    _executionPolicyService.Refresh(),
                    sourceId,
                    dispatchMode);
                if (!snapshot.IsValid)
                {
                    DebugUtility.LogVerbose(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] Snapshot invalido sourceId='{AsText(sourceId)}' dispatchMode='{dispatchMode.ToLogToken()}' scene='{sceneName}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (AlreadyExecuted(sceneName, dispatchMode, cycle, snapshot.ExecutionSignature))
                {
                    DebugUtility.LogVerbose(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] Dispatch ja executado sourceId='{AsText(sourceId)}' dispatchMode='{dispatchMode.ToLogToken()}' scene='{sceneName}' executionSignature='{snapshot.ExecutionSignature}' executionCycle='{cycle.ToStampKey()}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (!_provider.TryGetForScene<IWorldSpawnServiceRegistry>(sceneName, out var spawnRegistry) || spawnRegistry == null)
                {
                    DebugUtility.LogWarning(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] IWorldSpawnServiceRegistry ausente para scene='{sceneName}' sourceId='{AsText(sourceId)}' dispatchMode='{dispatchMode.ToLogToken()}'.");
                    return;
                }

                BuildServiceIndex(spawnRegistry);
                LogDispatchSummary(sceneName, sourceId, dispatchMode, snapshot);

                if (dispatchMode == ActorsOperationalMaterializationDispatchMode.PhaseLocalEntryReady)
                {
                    if (!_provider.TryGetGlobal<SessionFlowActorsSemanticPortsAdapter>(out var adapter) || adapter == null)
                    {
                        HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                            "[FATAL][H1][ActorsExecution] SessionFlowActorsSemanticPortsAdapter ausente para compor o ciclo phase-local-entry-ready.");
                    }

                    if (!adapter.TryGetCurrentCanonicalGameplayEntry(out var canonicalEntry) || !canonicalEntry.IsValid)
                    {
                        HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                            "[FATAL][H1][ActorsExecution] Canonical gameplay entry ausente/invalida para compor o ciclo phase-local-entry-ready.");
                    }

                    ActorsDefinitionsSnapshot definitions = RefreshDefinitionsOrFail();
                    ActorKind[] expectedActorKinds = BuildExpectedActorKinds(definitions);
                    if (expectedActorKinds.Length < 1)
                    {
                        HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                            $"[FATAL][H1][ActorsExecution] Expected actor kinds vazios para phase-local-entry-ready actorSetRef='{canonicalEntry.ActorSetRef}' scene='{canonicalEntry.SceneName}'.");
                    }

                    ActorsOperationalMaterializationCycleState cycleState = new ActorsOperationalMaterializationCycleState(
                        dispatchMode,
                        canonicalEntry.SourceKind,
                        canonicalEntry.SourceId,
                        canonicalEntry.RouteId,
                        canonicalEntry.RouteKind,
                        canonicalEntry.SceneName,
                        canonicalEntry.ActorSetRef,
                        cycle.EntrySignature,
                        snapshot.ExecutionSignature,
                        expectedActorKinds);

                    using (_cycleContext.OpenScope(cycle, cycleState, sourceId))
                    {
                        await DispatchAsync(sceneName, snapshot, sourceId, dispatchMode);

                        if (!_cycleContext.TryBuildCycleCompletedEvent(out ActorsOperationalMaterializationCycleCompletedEvent cycleCompletedEvent))
                        {
                            HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                                "[FATAL][H1][ActorsExecution] Nao foi possivel compor o evento final do ciclo operacional phase-local-entry-ready.");
                        }

                        DebugUtility.Log(typeof(ActorsMaterializationOperationalExecutor),
                            $"[OBS][ActorsExecution][CycleContext] dispatchMode='{cycleCompletedEvent.DispatchMode.ToLogToken()}' actorSetRef='{cycleCompletedEvent.ActorSetRef}' expectedKinds={FormatActorKinds(cycleCompletedEvent.ExpectedActorKinds)} materializedKinds={FormatActorKinds(cycleCompletedEvent.MaterializedActorKinds)} preservedActorKinds={FormatActorKinds(cycleCompletedEvent.PreservedActorKinds)} readyActorKinds={FormatActorKinds(cycleCompletedEvent.ReadyActorKinds)} isGameplayOperationalReady='{cycleCompletedEvent.IsGameplayOperationalReady.ToString().ToLowerInvariant()}' readinessReason='{cycleCompletedEvent.ReadinessReason}'.",
                            DebugUtility.Colors.Info);

                        EventBus<ActorsOperationalMaterializationCycleCompletedEvent>.Raise(cycleCompletedEvent);
                    }
                }
                else
                {
                    await DispatchAsync(sceneName, snapshot, sourceId, dispatchMode);
                }

                _lastExecutionStampByScene[sceneName] = BuildExecutionStamp(cycle, snapshot.ExecutionSignature);
                _lastExecutionStampBySceneAndMode[BuildSceneDispatchKey(sceneName, dispatchMode)] = BuildExecutionStamp(cycle, snapshot.ExecutionSignature);
            }
            finally
            {
                if (dispatchMode == ActorsOperationalMaterializationDispatchMode.PhaseLocalEntryReady)
                {
                    _phaseLocalEntryReadyPreserveExistingEnabled = false;
                    _phaseLocalEntryReadyContinuation = string.Empty;
                    ClearCanonicalGameplayEntryContextIfAvailable();
                }

                if (dispatchMode == ActorsOperationalMaterializationDispatchMode.PhaseLocalEntryReady)
                {
                    ReleasePhaseLocalEntryReadyDispatch(cycle);
                }

                _executionInProgress = false;
            }
        }

        private void ClearCanonicalGameplayEntryContextIfAvailable()
        {
            if (_provider.TryGetGlobal<SessionFlowActorsSemanticPortsAdapter>(out var adapter) && adapter != null)
            {
                adapter.ClearCanonicalGameplayEntryContext();
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

        private bool AlreadyExecuted(string sceneName, ActorsOperationalMaterializationDispatchMode dispatchMode, ActorsMaterializationExecutionCycle cycle, string executionSignature)
        {
            if (dispatchMode == ActorsOperationalMaterializationDispatchMode.Unknown || string.IsNullOrWhiteSpace(executionSignature))
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

        private static string BuildSceneDispatchKey(string sceneName, ActorsOperationalMaterializationDispatchMode dispatchMode)
        {
            return $"{AsText(sceneName)}|dispatchMode:{dispatchMode.ToLogToken()}";
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
            string sourceId,
            ActorsOperationalMaterializationDispatchMode dispatchMode,
            ActorsMaterializationExecutionSnapshot snapshot)
        {
            int playerCount = 0;
            int dummyCount = 0;
            int eaterCount = 0;
            int materializeCount = 0;
            int rematerializeCount = 0;
            int preserveExistingCount = 0;
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
                    case ActorMaterializationExecutionDirective.PreserveExisting:
                        preserveExistingCount += 1;
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
                $"[OBS][ActorsExecution][Operational] Dispatch iniciado sourceId='{AsText(sourceId)}' dispatchMode='{dispatchMode.ToLogToken()}' scene='{sceneName}' entries='{entries.Length}' player='{playerCount}' dummy='{dummyCount}' eater='{eaterCount}' materialize='{materializeCount}' rematerialize='{rematerializeCount}' preserveExisting='{preserveExistingCount}' readyDirectiveCount='{materializeCount + rematerializeCount + preserveExistingCount}' noAction='{noActionCount}' executionSignature='{snapshot.ExecutionSignature}'.",
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

        private static int CountPlayerReadyDirectives(ActorsMaterializationExecutionSnapshot snapshot)
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

                if (IsReadyDirective(entry.Directive))
                {
                    count += 1;
                }
            }

            return count;
        }

        private static int CountDirectives(
            ActorsMaterializationExecutionSnapshot snapshot,
            ActorMaterializationExecutionDirective directive)
        {
            if (!snapshot.IsValid || snapshot.Entries == null || snapshot.Entries.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < snapshot.Entries.Length; index += 1)
            {
                ActorsMaterializationExecutionEntry entry = snapshot.Entries[index];
                if (entry.IsValid && entry.Directive == directive)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static bool IsReadyDirective(ActorMaterializationExecutionDirective directive)
        {
            return directive == ActorMaterializationExecutionDirective.RequestMaterialize ||
                   directive == ActorMaterializationExecutionDirective.RequestRematerialize ||
                   directive == ActorMaterializationExecutionDirective.PreserveExisting;
        }

        private static ActorsMaterializationExecutionSnapshot ProjectPreserveExistingForPhaseLocalEntryReady(
            ActorsMaterializationExecutionSnapshot snapshot,
            SessionTransitionPhaseLocalEntryReadyEvent evt,
            ActorsDefinitionsSnapshot definitions,
            ActorsPresenceSnapshot presence,
            ActorsMaterializationPlanSnapshot plan)
        {
            if (!ShouldPreserveExistingForPhaseLocalEntryReady(evt))
            {
                return snapshot;
            }

            if (!definitions.IsValid || !presence.IsValid || !plan.IsValid)
            {
                return snapshot;
            }

            return ProjectPreserveExisting(
                snapshot,
                "PhaseLocalEntryReady",
                evt.Source,
                ResolvePhaseIntentLabel(evt),
                evt.Plan.IntentKind.ToString(),
                evt.Plan.Context.OrdinalNavigationKind.ToString(),
                evt.ActorSetRef,
                definitions,
                presence,
                plan);
        }

        private ActorsMaterializationExecutionSnapshot ProjectPreserveExistingForDispatch(
            ActorsMaterializationExecutionSnapshot snapshot,
            string sourceId,
            ActorsOperationalMaterializationDispatchMode dispatchMode)
        {
            if (dispatchMode != ActorsOperationalMaterializationDispatchMode.PhaseLocalEntryReady ||
                !_phaseLocalEntryReadyPreserveExistingEnabled)
            {
                return snapshot;
            }

            return ProjectPreserveExisting(
                snapshot,
                "Dispatch",
                sourceId,
                _phaseLocalEntryReadyContinuation,
                _phaseLocalEntryReadyContinuation,
                _phaseLocalEntryReadyContinuation,
                string.Empty,
                RefreshDefinitionsOrFail(),
                RefreshPresenceOrFail(),
                RefreshPlanOrFail());
        }

        private static ActorsMaterializationExecutionSnapshot ProjectPreserveExisting(
            ActorsMaterializationExecutionSnapshot snapshot,
            string stage,
            string sourceId,
            string phaseIntent,
            string intent,
            string ordinalNavigationKind,
            string actorSetRef,
            ActorsDefinitionsSnapshot definitions,
            ActorsPresenceSnapshot presence,
            ActorsMaterializationPlanSnapshot plan)
        {
            if (!snapshot.IsValid || snapshot.Entries == null || snapshot.Entries.Length == 0)
            {
                return snapshot;
            }

            var entries = new ActorsMaterializationExecutionEntry[snapshot.Entries.Length];
            int preserveCount = 0;
            bool changed = false;

            for (int index = 0; index < snapshot.Entries.Length; index += 1)
            {
                ActorsMaterializationExecutionEntry entry = snapshot.Entries[index];
                if (TryPromoteExistingActorEntry(
                        entry,
                        sourceId,
                        phaseIntent,
                        intent,
                        ordinalNavigationKind,
                        actorSetRef,
                        definitions,
                        presence,
                        plan,
                        out ActorsMaterializationExecutionEntry preservedEntry,
                        out string preserveReason))
                {
                    entries[index] = preservedEntry;
                    preserveCount += 1;
                    changed = true;

                    DebugUtility.Log(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] preserve_existing_detected actorSpecId='{AsText(preservedEntry.ActorSpecId)}' actorSetRef='{AsText(preservedEntry.ActorSetRef)}' source='{AsText(sourceId)}' phaseIntent='{AsText(phaseIntent)}' intent='{AsText(intent)}' ordinalNavigationKind='{AsText(ordinalNavigationKind)}' directive='PreserveExisting' reason='{AsText(preserveReason)}' actorKind='{MapRecipeToActorKind(preservedEntry.OperationalRecipeKind)}' axisActorId='{preservedEntry.AxisActorId}' runtimeActorId='{preservedEntry.RuntimeActorId}' semanticParticipantId='{AsText(preservedEntry.SemanticParticipantId)}' executionSignature='{AsText(snapshot.ExecutionSignature)}'.",
                        DebugUtility.Colors.Info);
                    continue;
                }

                entries[index] = entry;
            }

            if (!changed)
            {
                return snapshot;
            }

            DebugUtility.Log(typeof(ActorsMaterializationOperationalExecutor),
                $"[OBS][ActorsExecution][Operational] preserve_existing_detected stage='{AsText(stage)}' sourceId='{AsText(sourceId)}' phaseIntent='{AsText(phaseIntent)}' intent='{AsText(intent)}' ordinalNavigationKind='{AsText(ordinalNavigationKind)}' actorSetRef='{AsText(actorSetRef)}' preserveExisting='{preserveCount}' readyDirectiveCount='{CountReadyDirectives(entries)}' preservedActorKinds={FormatActorKinds(BuildActorKindsForDirective(entries, ActorMaterializationExecutionDirective.PreserveExisting))} readyActorKinds={FormatActorKinds(BuildReadyActorKinds(entries))} executionSignature='{snapshot.ExecutionSignature}'.",
                DebugUtility.Colors.Info);

            return new ActorsMaterializationExecutionSnapshot(
                snapshot.PlanSignature,
                snapshot.ExecutionSignature,
                entries,
                CountDirectives(entries, ActorMaterializationExecutionDirective.NoActionStable),
                snapshot.NoActionObserveCount,
                snapshot.RequestMaterializeCount,
                snapshot.RequestRematerializeCount,
                snapshot.FlagInconsistentCount,
                snapshot.FlagRuntimeOrphanToleratedCount,
                snapshot.FlagRuntimeOrphanProblematicCount,
                snapshot.Reason);
        }

        private static bool ShouldPreserveExistingForPhaseLocalEntryReady(SessionTransitionPhaseLocalEntryReadyEvent evt)
        {
            return evt.IsValid &&
                   evt.IsPhaseLocalEntry &&
                   evt.Plan.IsValid &&
                   evt.RouteKind == SceneRouteKind.Gameplay &&
                   !string.IsNullOrWhiteSpace(evt.ActorSetRef) &&
                   (evt.Plan.ResolvedContinuation == RunContinuationKind.AdvancePhase ||
                    (evt.Plan.IntentKind == SessionTransitionIntentKind.PhaseOrdinalNavigation &&
                     string.Equals(evt.Source, PhaseFlowSignalVocabulary.SessionTransitionPhaseOrdinalNavigationSource, StringComparison.Ordinal)));
        }

        private static string ResolvePhaseIntentLabel(SessionTransitionPhaseLocalEntryReadyEvent evt)
        {
            if (!evt.IsValid || !evt.Plan.IsValid)
            {
                return string.Empty;
            }

            return evt.Plan.IntentKind == SessionTransitionIntentKind.PhaseOrdinalNavigation
                ? evt.Plan.IntentKind.ToString()
                : evt.Plan.ResolvedContinuation.ToString();
        }

        private static bool TryPromoteExistingActorEntry(
            ActorsMaterializationExecutionEntry entry,
            string sourceId,
            string phaseIntent,
            string intent,
            string ordinalNavigationKind,
            string actorSetRef,
            ActorsDefinitionsSnapshot definitions,
            ActorsPresenceSnapshot presence,
            ActorsMaterializationPlanSnapshot plan,
            out ActorsMaterializationExecutionEntry preservedEntry,
            out string reason)
        {
            preservedEntry = entry;
            reason = string.Empty;

            if (!entry.IsValid ||
                entry.Directive != ActorMaterializationExecutionDirective.NoActionStable ||
                !entry.RuntimeActorId.IsValid ||
                entry.SpecKind != ActorMaterializationSpecKind.AxisActor ||
                string.IsNullOrWhiteSpace(entry.ActorSpecId) ||
                string.IsNullOrWhiteSpace(entry.ActorSetRef) ||
                entry.OperationalRecipeKind == ActorOperationalRecipeKind.Unknown)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(actorSetRef) &&
                !string.Equals(entry.ActorSetRef, actorSetRef, StringComparison.Ordinal))
            {
                return false;
            }

            if (!TryResolveCompatibleExistingActor(definitions, presence, plan, entry, out string compatibilityReason))
            {
                return false;
            }

            preservedEntry = new ActorsMaterializationExecutionEntry(
                entry.SpecKind,
                entry.AxisActorId,
                entry.RuntimeActorId,
                entry.Role,
                entry.OperationalRecipeKind,
                entry.Intent,
                entry.Classification,
                ActorMaterializationExecutionDirective.PreserveExisting,
                entry.SemanticParticipantId,
                entry.ActorSpecId,
                entry.ActorSetRef,
                BuildPreserveExistingReason(entry, sourceId, phaseIntent, intent, ordinalNavigationKind));

            reason = string.Equals(intent, SessionTransitionIntentKind.PhaseOrdinalNavigation.ToString(), StringComparison.Ordinal)
                ? "existing_actor_valid_for_phase_ordinal_navigation"
                : "existing_actor_valid_for_advance_phase";
            if (!string.IsNullOrWhiteSpace(compatibilityReason))
            {
                reason = compatibilityReason;
            }
            return true;
        }

        private static bool TryResolveCompatibleExistingActor(
            ActorsDefinitionsSnapshot definitions,
            ActorsPresenceSnapshot presence,
            ActorsMaterializationPlanSnapshot plan,
            ActorsMaterializationExecutionEntry entry,
            out string reason)
        {
            reason = string.Empty;

            if (!definitions.IsValid || !presence.IsValid || !plan.IsValid)
            {
                reason = "invalid_dependencies";
                return false;
            }

            if (!TryResolveCompatibleDefinition(definitions, entry, out ActorDefinitionRecord definition))
            {
                reason = "definition_mismatch";
                return false;
            }

            if (!TryResolveCompatiblePresence(presence, entry))
            {
                reason = "presence_mismatch";
                return false;
            }

            if (!TryResolveCompatiblePlanEntry(plan, entry))
            {
                reason = "plan_mismatch";
                return false;
            }

            if (entry.Role == ActorRole.Player)
            {
                if (string.IsNullOrWhiteSpace(entry.SemanticParticipantId))
                {
                    reason = "player_missing_semantic_participant";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(definition.SemanticParticipantId) ||
                    !string.Equals(definition.SemanticParticipantId, entry.SemanticParticipantId, StringComparison.Ordinal))
                {
                    reason = "player_definition_semantic_participant_mismatch";
                    return false;
                }
            }

            reason = "existing_actor_compatible";
            return true;
        }

        private static bool TryResolveCompatibleDefinition(
            ActorsDefinitionsSnapshot definitions,
            ActorsMaterializationExecutionEntry entry,
            out ActorDefinitionRecord compatibleDefinition)
        {
            compatibleDefinition = default;

            for (int index = 0; index < definitions.Entries.Length; index += 1)
            {
                ActorDefinitionRecord definition = definitions.Entries[index];
                if (!definition.IsValid ||
                    definition.AxisActorId != entry.AxisActorId ||
                    definition.Role != entry.Role ||
                    !string.Equals(definition.ActorSpecId, entry.ActorSpecId, StringComparison.Ordinal) ||
                    !string.Equals(definition.ActorSetRef, entry.ActorSetRef, StringComparison.Ordinal) ||
                    definition.OperationalRecipeKind != entry.OperationalRecipeKind)
                {
                    continue;
                }

                compatibleDefinition = definition;
                return true;
            }

            return false;
        }

        private static bool TryResolveCompatiblePresence(
            ActorsPresenceSnapshot presence,
            ActorsMaterializationExecutionEntry entry)
        {
            for (int index = 0; index < presence.Entries.Length; index += 1)
            {
                ActorsPresenceRecord presenceRecord = presence.Entries[index];
                if (!presenceRecord.IsValid ||
                    presenceRecord.AxisActorId != entry.AxisActorId ||
                    presenceRecord.Role != entry.Role ||
                    !presenceRecord.IsMaterialized ||
                    presenceRecord.IsInconsistent ||
                    !presenceRecord.RuntimeActorId.IsValid ||
                    presenceRecord.RuntimeActorId != entry.RuntimeActorId ||
                    !string.Equals(presenceRecord.SemanticParticipantId, entry.SemanticParticipantId, StringComparison.Ordinal))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool TryResolveCompatiblePlanEntry(
            ActorsMaterializationPlanSnapshot plan,
            ActorsMaterializationExecutionEntry entry)
        {
            for (int planIndex = 0; planIndex < plan.Entries.Length; planIndex += 1)
            {
                ActorsMaterializationSpecEntry planEntry = plan.Entries[planIndex];
                if (!planEntry.IsValid ||
                    planEntry.Kind != ActorMaterializationSpecKind.AxisActor ||
                    planEntry.AxisActorId != entry.AxisActorId ||
                    !string.Equals(planEntry.ActorSpecId, entry.ActorSpecId, StringComparison.Ordinal) ||
                    !string.Equals(planEntry.ActorSetRef, entry.ActorSetRef, StringComparison.Ordinal) ||
                    planEntry.OperationalRecipeKind != entry.OperationalRecipeKind ||
                    !string.Equals(planEntry.SemanticParticipantId, entry.SemanticParticipantId, StringComparison.Ordinal))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static string BuildPreserveExistingReason(
            ActorsMaterializationExecutionEntry entry,
            string sourceId,
            string phaseIntent,
            string intent,
            string ordinalNavigationKind)
        {
            string reason = string.Equals(intent, SessionTransitionIntentKind.PhaseOrdinalNavigation.ToString(), StringComparison.Ordinal)
                ? "existing_actor_valid_for_phase_ordinal_navigation"
                : "existing_actor_valid_for_advance_phase";

            return $"directive='{ActorMaterializationExecutionDirective.PreserveExisting}' reason='{reason}' source='{AsText(sourceId)}' phaseIntent='{AsText(phaseIntent)}' intent='{AsText(intent)}' ordinalNavigationKind='{AsText(ordinalNavigationKind)}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' previousReason='{AsText(entry.Reason)}'";
        }

        private static int CountReadyDirectives(ActorsMaterializationExecutionEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < entries.Length; index += 1)
            {
                ActorsMaterializationExecutionEntry entry = entries[index];
                if (entry.IsValid && IsReadyDirective(entry.Directive))
                {
                    count += 1;
                }
            }

            return count;
        }

        private static int CountDirectives(
            ActorsMaterializationExecutionEntry[] entries,
            ActorMaterializationExecutionDirective directive)
        {
            if (entries == null || entries.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < entries.Length; index += 1)
            {
                ActorsMaterializationExecutionEntry entry = entries[index];
                if (entry.IsValid && entry.Directive == directive)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static ActorKind[] BuildReadyActorKinds(ActorsMaterializationExecutionEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
            {
                return Array.Empty<ActorKind>();
            }

            var kinds = new List<ActorKind>(entries.Length);
            for (int index = 0; index < entries.Length; index += 1)
            {
                ActorsMaterializationExecutionEntry entry = entries[index];
                ActorKind kind = MapRecipeToActorKind(entry.OperationalRecipeKind);
                if (!entry.IsValid || kind == ActorKind.Unknown || !IsReadyDirective(entry.Directive) || kinds.Contains(kind))
                {
                    continue;
                }

                kinds.Add(kind);
            }

            return kinds.ToArray();
        }

        private static ActorKind[] BuildActorKindsForDirective(
            ActorsMaterializationExecutionEntry[] entries,
            ActorMaterializationExecutionDirective directive)
        {
            if (entries == null || entries.Length == 0)
            {
                return Array.Empty<ActorKind>();
            }

            var kinds = new List<ActorKind>(entries.Length);
            for (int index = 0; index < entries.Length; index += 1)
            {
                ActorsMaterializationExecutionEntry entry = entries[index];
                ActorKind kind = MapRecipeToActorKind(entry.OperationalRecipeKind);
                if (!entry.IsValid || kind == ActorKind.Unknown || entry.Directive != directive || kinds.Contains(kind))
                {
                    continue;
                }

                kinds.Add(kind);
            }

            return kinds.ToArray();
        }

        private async Task DispatchAsync(string sceneName, ActorsMaterializationExecutionSnapshot snapshot, string sourceId, ActorsOperationalMaterializationDispatchMode dispatchMode)
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
                        $"[OBS][ActorsExecution][Operational] Directive ignorada pelo modo de dispatch dispatchMode='{dispatchMode.ToLogToken()}' actorKind='{targetKind}' axisActorId='{entry.AxisActorId}' semanticParticipantId='{AsText(entry.SemanticParticipantId)}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' directive='{entry.Directive}'.",
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

                    case ActorMaterializationExecutionDirective.PreserveExisting:
                        RecordPreservedActorOrFail(entry, sceneName, sourceId, snapshot.ExecutionSignature);
                        break;

                    case ActorMaterializationExecutionDirective.NoActionStable:
                    case ActorMaterializationExecutionDirective.NoActionObserve:
                    case ActorMaterializationExecutionDirective.FlagInconsistentNoAutoRemediation:
                    case ActorMaterializationExecutionDirective.FlagRuntimeOrphanTolerated:
                    case ActorMaterializationExecutionDirective.FlagRuntimeOrphanProblematic:
                        DebugUtility.LogVerbose(typeof(ActorsMaterializationOperationalExecutor),
                            $"[OBS][ActorsExecution][Operational] Directive sem acao automatica directive='{entry.Directive}' axisActorId='{entry.AxisActorId}' runtimeActorId='{entry.RuntimeActorId}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' scene='{sceneName}' sourceId='{AsText(sourceId)}'.",
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
                        $"[OBS][ActorsExecution][Operational] Service ausente para rematerializar actorKind='{kind}' axisActorId='{entry.AxisActorId}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' scene='{sceneName}' sourceId='{AsText(sourceId)}'.");
                    continue;
                }

                await service.DespawnAsync();
                await service.SpawnAsync(CreateSpawnRequest(sceneName, sourceId, service, entry, snapshot.ExecutionSignature));

                DebugUtility.Log(typeof(ActorsMaterializationOperationalExecutor),
                    $"[OBS][ActorsExecution][Operational] Rematerialize executado actorKind='{kind}' service='{service.Name}' axisActorId='{entry.AxisActorId}' runtimeActorId='{entry.RuntimeActorId}' semanticParticipantId='{AsText(entry.SemanticParticipantId)}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' scene='{sceneName}' sourceId='{AsText(sourceId)}'.",
                    DebugUtility.Colors.Info);
            }

            foreach (KeyValuePair<ActorKind, ActorsMaterializationExecutionEntry> pair in materializeEntries)
            {
                ActorKind kind = pair.Key;
                ActorsMaterializationExecutionEntry entry = pair.Value;
                if (!_serviceByKind.TryGetValue(kind, out IWorldSpawnService service) || service == null)
                {
                    DebugUtility.LogWarning(typeof(ActorsMaterializationOperationalExecutor),
                        $"[OBS][ActorsExecution][Operational] Service ausente para materializar actorKind='{kind}' axisActorId='{entry.AxisActorId}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' scene='{sceneName}' sourceId='{AsText(sourceId)}'.");
                    continue;
                }

                await service.SpawnAsync(CreateSpawnRequest(sceneName, sourceId, service, entry, snapshot.ExecutionSignature));

                DebugUtility.Log(typeof(ActorsMaterializationOperationalExecutor),
                    $"[OBS][ActorsExecution][Operational] Materialize executado actorKind='{kind}' service='{service.Name}' axisActorId='{entry.AxisActorId}' runtimeActorId='{entry.RuntimeActorId}' semanticParticipantId='{AsText(entry.SemanticParticipantId)}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' scene='{sceneName}' sourceId='{AsText(sourceId)}'.",
                    DebugUtility.Colors.Info);
            }
        }

        private void RecordPreservedActorOrFail(
            ActorsMaterializationExecutionEntry entry,
            string sceneName,
            string sourceId,
            string executionSignature)
        {
            if (!_cycleContext.TryGetCurrent(out ActorsMaterializationExecutionCycle cycle) || !cycle.IsValid)
            {
                HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                    $"[FATAL][H1][ActorsExecution] PreserveExisting sem ciclo operacional valido. axisActorId='{entry.AxisActorId}' runtimeActorId='{entry.RuntimeActorId}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' sourceId='{AsText(sourceId)}'.");
                return;
            }

            ActorKind actorKind = MapRecipeToActorKind(entry.OperationalRecipeKind);
            if (actorKind == ActorKind.Unknown || !entry.RuntimeActorId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(ActorsMaterializationOperationalExecutor),
                    $"[FATAL][H1][ActorsExecution] PreserveExisting invalido. actorKind='{actorKind}' axisActorId='{entry.AxisActorId}' runtimeActorId='{entry.RuntimeActorId}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' sourceId='{AsText(sourceId)}'.");
                return;
            }

            var completedEvent = new ActorsOperationalMaterializationCompletedEvent(
                entry,
                actorKind,
                cycle,
                sceneName,
                sourceId,
                executionSignature,
                "GameplayRuntime/ActorsMaterializationOperationalExecutor/PreserveExisting");

            DebugUtility.Log(typeof(ActorsMaterializationOperationalExecutor),
                $"[OBS][ActorsExecution][Operational] preserve_existing_detected actorKind='{actorKind}' axisActorId='{entry.AxisActorId}' runtimeActorId='{entry.RuntimeActorId}' semanticParticipantId='{AsText(entry.SemanticParticipantId)}' actorSpecId='{AsText(entry.ActorSpecId)}' actorSetRef='{AsText(entry.ActorSetRef)}' scene='{AsText(sceneName)}' sourceId='{AsText(sourceId)}' phaseIntent='{AsText(_phaseLocalEntryReadyContinuation)}' executionSignature='{AsText(executionSignature)}'.",
                DebugUtility.Colors.Info);

            _cycleContext.RecordCompletedActor(completedEvent);
            EventBus<ActorsOperationalMaterializationCompletedEvent>.Raise(completedEvent);
        }

        private static bool IsDispatchAllowedForMode(ActorsOperationalMaterializationDispatchMode dispatchMode, ActorKind actorKind)
        {
            if (actorKind == ActorKind.Unknown)
            {
                return false;
            }

            if (dispatchMode == ActorsOperationalMaterializationDispatchMode.PhaseLocalEntryReady)
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
            string sourceId,
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
                sourceId,
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

        private static ActorKind[] BuildExpectedActorKinds(ActorsDefinitionsSnapshot snapshot)
        {
            if (!snapshot.IsValid || snapshot.Entries == null || snapshot.Entries.Length == 0)
            {
                return Array.Empty<ActorKind>();
            }

            var expectedKinds = new List<ActorKind>(snapshot.Entries.Length);
            for (int index = 0; index < snapshot.Entries.Length; index += 1)
            {
                ActorDefinitionRecord entry = snapshot.Entries[index];
                if (!entry.IsValid)
                {
                    continue;
                }

                ActorKind kind = MapRecipeToActorKind(entry.OperationalRecipeKind);
                if (kind == ActorKind.Unknown || expectedKinds.Contains(kind))
                {
                    continue;
                }

                expectedKinds.Add(kind);
            }

            return expectedKinds.ToArray();
        }

        private static string FormatActorKinds(ActorKind[] kinds)
        {
            if (kinds == null || kinds.Length == 0)
            {
                return "[]";
            }

            var builder = new System.Text.StringBuilder();
            builder.Append('[');
            for (int index = 0; index < kinds.Length; index += 1)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append(kinds[index]);
            }

            builder.Append(']');
            return builder.ToString();
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
            // For InitialEntry, ContinuationContext may not be valid; only check plan.IsValid.
            // ContinuationContext is only valid when HasRunContinuationSelection == true.
            // Comentário: InitialEntry não permite acesso a ContinuationContext.
            if (!plan.IsValid)
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
                HardFailFastH1.Trigger(typeof(SessionTransitionPhaseLocalEntryReadyMaterializationBridge),
                    $"[FATAL][H1][ActorsExecution] SessionTransitionPhaseLocalEntryReady sem payload canonico obrigatorio para gameplay. routeId='{evt.RouteId}' routeKind='{evt.RouteKind}' scene='{evt.SceneName}' actorSetRef='{AsText(evt.ActorSetRef)}' reason='{AsText(evt.Reason)}' cycleSignature='{AsText(evt.CycleSignature)}'.");
                return;
            }

            // For InitialEntry, frame is always 1. For post-run continuations, use ContinuationContext.
            // Comentário: frame seguro - não acessa ContinuationContext para InitialEntry.
            int cycleFrame = plan.HasRunContinuationSelection ? plan.ContinuationContext.Frame : 1;
            ActorsMaterializationExecutionCycle cycle = ActorsMaterializationExecutionCycle.CreateOrFail(
                Math.Max(1, cycleFrame),
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
                // For InitialEntry, frame is always 1. For post-run continuations, use the ContinuationContext frame.
                // Comentário: frame seguro - não acessa ContinuationContext para InitialEntry.
                int frameValue = plan.HasRunContinuationSelection ? plan.ContinuationContext.Frame : 1;
                string frameLabel = plan.HasRunContinuationSelection ? $"'{frameValue}'" : "'1'";
                DebugUtility.Log(typeof(SessionTransitionPhaseLocalEntryReadyMaterializationBridge),
                    $"[OBS][ActorsExecution][Operational] SessionTransitionPhaseLocalEntryReady recebido routeId='{evt.RouteId}' routeKind='{evt.RouteKind}' scene='{evt.SceneName}' actorSetRef='{AsText(evt.ActorSetRef)}' continuation='{plan.ResolvedContinuation}' frame={frameLabel} reason='{plan.Reason}' source='{evt.Source}' sessionSignature='{AsText(evt.SessionSignature)}' phaseSignature='{AsText(evt.PhaseSignature)}' participationSignature='{AsText(evt.ParticipationSignature)}' entrySignature='{cycle.EntrySignature}'.",
                    DebugUtility.Colors.Info);

                _executor.PrimeCanonicalGameplayEntry(evt);
                _ = ObservePhaseLocalEntryReadyMaterializationAsync(evt, cycle);
            }
            catch
            {
                _executor.ReleasePhaseLocalEntryReadyDispatch(cycle);
                throw;
            }
        }

        private async Task ObservePhaseLocalEntryReadyMaterializationAsync(
            SessionTransitionPhaseLocalEntryReadyEvent evt,
            ActorsMaterializationExecutionCycle cycle)
        {
            try
            {
                DebugUtility.Log(typeof(SessionTransitionPhaseLocalEntryReadyMaterializationBridge),
                    BuildMaterializationDispatchLogMessage(evt, cycle, "started", null),
                    DebugUtility.Colors.Info);

                await _executor.ExecuteCurrentAsync(
                    ActorsOperationalMaterializationDispatchMode.PhaseLocalEntryReady,
                    evt.Source,
                    cycle,
                    evt.SceneName);

                DebugUtility.Log(typeof(SessionTransitionPhaseLocalEntryReadyMaterializationBridge),
                    BuildMaterializationDispatchLogMessage(evt, cycle, "completed", null),
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(SessionTransitionPhaseLocalEntryReadyMaterializationBridge),
                    BuildMaterializationDispatchLogMessage(evt, cycle, "failed", ex));

                HardFailFastH1.Trigger(typeof(SessionTransitionPhaseLocalEntryReadyMaterializationBridge),
                    $"[FATAL][H1][ActorsExecution] PhaseLocalEntryReady actor materialization dispatch failed. operation='PhaseLocalEntryReadyActorMaterialization' source='{AsText(evt.Source)}' reason='{AsText(evt.Reason)}' sceneName='{AsText(evt.SceneName)}' cycleSignature='{AsText(evt.CycleSignature)}' entrySignature='{AsText(cycle.EntrySignature)}' contextSignature='{AsText(evt.SessionSignature)}' executionSignature='{AsText(cycle.EntrySignature)}' phaseLocalEntrySequence='{cycle.PhaseLocalEntrySequence}' dispatchMode='{ActorsOperationalMaterializationDispatchMode.PhaseLocalEntryReady.ToLogToken()}' exceptionType='{ex.GetType().Name}' exceptionMessage='{AsText(ex.Message)}'.",
                    ex);
            }
        }

        private static string BuildMaterializationDispatchLogMessage(
            SessionTransitionPhaseLocalEntryReadyEvent evt,
            ActorsMaterializationExecutionCycle cycle,
            string status,
            Exception exception)
        {
            string exceptionFields = exception == null
                ? string.Empty
                : $" exceptionType='{exception.GetType().Name}' exceptionMessage='{AsText(exception.Message)}'";

            return $"[OBS][ActorsExecution][Operational] materialization_dispatch_{status} operation='PhaseLocalEntryReadyActorMaterialization' source='{AsText(evt.Source)}' reason='{AsText(evt.Reason)}' sceneName='{AsText(evt.SceneName)}' cycleSignature='{AsText(evt.CycleSignature)}' entrySignature='{AsText(cycle.EntrySignature)}' contextSignature='{AsText(evt.SessionSignature)}' executionSignature='{AsText(cycle.EntrySignature)}' phaseLocalEntrySequence='{cycle.PhaseLocalEntrySequence}' dispatchMode='{ActorsOperationalMaterializationDispatchMode.PhaseLocalEntryReady.ToLogToken()}' status='{status}' continuation='{evt.Plan.ResolvedContinuation}' routeId='{evt.RouteId}' routeKind='{evt.RouteKind}' actorSetRef='{AsText(evt.ActorSetRef)}' sessionSignature='{AsText(evt.SessionSignature)}' phaseSignature='{AsText(evt.PhaseSignature)}' participationSignature='{AsText(evt.ParticipationSignature)}'{exceptionFields}.";
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
