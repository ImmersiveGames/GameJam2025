using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.GameplayRuntime.Spawn;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution
{
    public enum ActorsOperationalMaterializationDispatchMode
    {
        Unknown = 0,
        PhaseLocalEntryReady = 1,
        PhaseRuntimeMaterialized = 2
    }

    public enum ActorsOperationalMaterializationSourceKind
    {
        Unknown = 0,
        SessionTransitionPhaseLocalEntryReady = 1,
        GameplayRuntime = 2
    }

    public static class ActorsOperationalMaterializationDispatchModeExtensions
    {
        public static string ToLogToken(this ActorsOperationalMaterializationDispatchMode dispatchMode)
        {
            return dispatchMode switch
            {
                ActorsOperationalMaterializationDispatchMode.PhaseLocalEntryReady => "phase-local-entry-ready",
                ActorsOperationalMaterializationDispatchMode.PhaseRuntimeMaterialized => "phase-runtime-materialized",
                _ => "unknown"
            };
        }
    }

    public readonly struct ActorsOperationalMaterializationCycleState
    {
        public ActorsOperationalMaterializationCycleState(
            ActorsOperationalMaterializationDispatchMode dispatchMode,
            ActorsOperationalMaterializationSourceKind sourceKind,
            string sourceId,
            SceneRouteId routeId,
            SceneRouteKind routeKind,
            string sceneName,
            string actorSetRef,
            string cycleSignature,
            string executionSignature,
            ActorKind[] expectedActorKinds)
        {
            DispatchMode = dispatchMode;
            SourceKind = sourceKind;
            SourceId = string.IsNullOrWhiteSpace(sourceId) ? string.Empty : sourceId.Trim();
            RouteId = routeId;
            RouteKind = routeKind;
            SceneName = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
            ActorSetRef = string.IsNullOrWhiteSpace(actorSetRef) ? string.Empty : actorSetRef.Trim();
            CycleSignature = string.IsNullOrWhiteSpace(cycleSignature) ? string.Empty : cycleSignature.Trim();
            ExecutionSignature = string.IsNullOrWhiteSpace(executionSignature) ? string.Empty : executionSignature.Trim();
            ExpectedActorKinds = expectedActorKinds == null ? Array.Empty<ActorKind>() : (ActorKind[])expectedActorKinds.Clone();
        }

        public ActorsOperationalMaterializationDispatchMode DispatchMode { get; }
        public ActorsOperationalMaterializationSourceKind SourceKind { get; }
        public string SourceId { get; }
        public SceneRouteId RouteId { get; }
        public SceneRouteKind RouteKind { get; }
        public string SceneName { get; }
        public string ActorSetRef { get; }
        public string CycleSignature { get; }
        public string ExecutionSignature { get; }
        public ActorKind[] ExpectedActorKinds { get; }

        public int ExpectedActorKindCount => ExpectedActorKinds?.Length ?? 0;
        public bool HasCanonicalPayload =>
            DispatchMode != ActorsOperationalMaterializationDispatchMode.Unknown &&
            SourceKind != ActorsOperationalMaterializationSourceKind.Unknown &&
            !string.IsNullOrWhiteSpace(SourceId) &&
            RouteId.IsValid &&
            RouteKind != SceneRouteKind.Unspecified &&
            !string.IsNullOrWhiteSpace(SceneName) &&
            !string.IsNullOrWhiteSpace(ActorSetRef) &&
            !string.IsNullOrWhiteSpace(CycleSignature) &&
            !string.IsNullOrWhiteSpace(ExecutionSignature) &&
            ExpectedActorKindCount > 0;
        public bool IsValid => HasCanonicalPayload;
    }

    public interface IActorsMaterializationExecutionCycleContext
    {
        bool TryGetCurrent(out ActorsMaterializationExecutionCycle cycle);
        IDisposable OpenScope(ActorsMaterializationExecutionCycle cycle, ActorsOperationalMaterializationCycleState state, string source);
        void RecordCompletedActor(ActorsOperationalMaterializationCompletedEvent evt);
        bool TryBuildCycleCompletedEvent(out ActorsOperationalMaterializationCycleCompletedEvent evt);
    }

    /// <summary>
    /// Mantem o ciclo operacional corrente durante o dispatch canonico de materializacao.
    /// </summary>
    public sealed class ActorsMaterializationExecutionCycleContext : IActorsMaterializationExecutionCycleContext
    {
        private ActorsMaterializationExecutionCycle _current;
        private int _scopeDepth;
        private ActorsOperationalMaterializationCycleState _currentState;
        private readonly List<ActorsOperationalMaterializationCompletedEvent> _completedActors = new();
        private readonly HashSet<string> _completedActorKeys = new(StringComparer.Ordinal);
        private bool _hasActorSetMismatch;

        public bool TryGetCurrent(out ActorsMaterializationExecutionCycle cycle)
        {
            cycle = _current;
            return _current.IsValid;
        }

        public IDisposable OpenScope(ActorsMaterializationExecutionCycle cycle, ActorsOperationalMaterializationCycleState state, string source)
        {
            if (!cycle.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][ActorsExecution] Ciclo operacional invalido para abrir contexto source='{AsText(source)}'.");
            }

            if (!state.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][ActorsExecution] Estado operacional invalido para abrir contexto source='{AsText(source)}' dispatchMode='{state.DispatchMode.ToLogToken()}'.");
            }

            _scopeDepth += 1;
            _current = cycle;
            _currentState = state;
            _completedActors.Clear();
            _completedActorKeys.Clear();
            _hasActorSetMismatch = false;
            return new Scope(this, source);
        }

        public void RecordCompletedActor(ActorsOperationalMaterializationCompletedEvent evt)
        {
            if (!TryGetCurrent(out ActorsMaterializationExecutionCycle cycle) || !evt.IsValid)
            {
                return;
            }

            if (!string.Equals(cycle.ToStampKey(), evt.ExecutionCycle.ToStampKey(), StringComparison.Ordinal))
            {
                return;
            }

            if (!evt.HasCanonicalPayload)
            {
                DebugUtility.LogWarning(typeof(ActorsMaterializationExecutionCycleContext),
                    $"[OBS][ActorsExecution][CycleContext] Actor completion ignorada reason='missing_canonical_payload' traceId='{AsText(evt.ExecutionSignature)}' actorKind='{evt.ActorKind}' actorSpecId='{AsText(evt.ActorSpecId)}' actorSetRef='{AsText(evt.ActorSetRef)}' runtimeActorId='{evt.RuntimeActorId}'.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(_currentState.ActorSetRef) &&
                !string.Equals(_currentState.ActorSetRef, evt.ActorSetRef, StringComparison.Ordinal))
            {
                _hasActorSetMismatch = true;
                DebugUtility.LogWarning(typeof(ActorsMaterializationExecutionCycleContext),
                    $"[OBS][ActorsExecution][CycleContext] Actor completion ignorada reason='actor_set_ref_mismatch' traceId='{AsText(evt.ExecutionSignature)}' expectedActorSetRef='{AsText(_currentState.ActorSetRef)}' actorSetRef='{AsText(evt.ActorSetRef)}' actorKind='{evt.ActorKind}' runtimeActorId='{evt.RuntimeActorId}'.");
                return;
            }

            string completionKey = BuildCompletedActorKey(evt);
            if (_completedActorKeys.Contains(completionKey))
            {
                return;
            }

            _completedActorKeys.Add(completionKey);
            _completedActors.Add(evt);
        }

        public bool TryBuildCycleCompletedEvent(out ActorsOperationalMaterializationCycleCompletedEvent evt)
        {
            evt = default;

            if (!TryGetCurrent(out ActorsMaterializationExecutionCycle cycle) || !_currentState.IsValid)
            {
                return false;
            }

            evt = new ActorsOperationalMaterializationCycleCompletedEvent(
                _currentState.SceneName,
                cycle,
                _currentState.DispatchMode,
                _currentState.SourceKind,
                _currentState.SourceId,
                _currentState.RouteId,
                _currentState.RouteKind,
                _currentState.ActorSetRef,
                _currentState.CycleSignature,
                _currentState.ExecutionSignature,
                _currentState.ExpectedActorKinds,
                _completedActors.ToArray(),
                _hasActorSetMismatch);
            return true;
        }

        private void CloseScope(string source)
        {
            _scopeDepth = Math.Max(0, _scopeDepth - 1);
            if (_scopeDepth > 0)
            {
                return;
            }

            _current = default;
            _currentState = default;
            _completedActors.Clear();
            _completedActorKeys.Clear();
            _hasActorSetMismatch = false;
            DebugUtility.LogVerbose(typeof(ActorsMaterializationExecutionCycleContext),
                $"[OBS][ActorsExecution][CycleContext] Escopo operacional finalizado source='{AsText(source)}'.",
                DebugUtility.Colors.Info);
        }

        private static string BuildCompletedActorKey(ActorsOperationalMaterializationCompletedEvent evt)
        {
            if (evt.HasRuntimeActorId)
            {
                return evt.RuntimeActorId.ToString();
            }

            return $"{evt.ExecutionSignature}|{evt.ActorKind}|{AsText(evt.ActorId)}";
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }

        private sealed class Scope : IDisposable
        {
            private readonly ActorsMaterializationExecutionCycleContext _owner;
            private readonly string _source;
            private bool _disposed;

            public Scope(ActorsMaterializationExecutionCycleContext owner, string source)
            {
                _owner = owner;
                _source = source;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _owner.CloseScope(_source);
            }
        }
    }

    /// <summary>
    /// Handoff explicito do estagio de materializacao operacional concluida.
    /// </summary>
    public readonly struct ActorsOperationalMaterializationCompletedEvent : IEvent
    {
        public ActorsOperationalMaterializationCompletedEvent(
            ActorSpawnCompletedEvent completedEvent,
            ActorsMaterializationExecutionCycle executionCycle,
            string source)
        {
            Actor = completedEvent.Actor;
            ActorKind = completedEvent.ActorKind;
            AxisActorId = completedEvent.AxisActorId;
            RuntimeActorId = completedEvent.RuntimeActorId;
            ActorId = string.IsNullOrWhiteSpace(completedEvent.ActorId) ? string.Empty : completedEvent.ActorId.Trim();
            ActorSpecId = string.IsNullOrWhiteSpace(completedEvent.ActorSpecId) ? string.Empty : completedEvent.ActorSpecId.Trim();
            ActorSetRef = string.IsNullOrWhiteSpace(completedEvent.ActorSetRef) ? string.Empty : completedEvent.ActorSetRef.Trim();
            SemanticParticipantId = string.IsNullOrWhiteSpace(completedEvent.SemanticParticipantId) ? string.Empty : completedEvent.SemanticParticipantId.Trim();
            OperationalRecipeKind = completedEvent.OperationalRecipeKind;
            SpawnServiceName = string.IsNullOrWhiteSpace(completedEvent.SpawnServiceName) ? string.Empty : completedEvent.SpawnServiceName.Trim();
            SceneName = string.IsNullOrWhiteSpace(completedEvent.SceneName) ? string.Empty : completedEvent.SceneName.Trim();
            Source = string.IsNullOrWhiteSpace(completedEvent.Source) ? string.Empty : completedEvent.Source.Trim();
            HandoffSource = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            Reason = string.IsNullOrWhiteSpace(completedEvent.Reason) ? string.Empty : completedEvent.Reason.Trim();
            ExecutionSignature = string.IsNullOrWhiteSpace(completedEvent.ExecutionSignature) ? string.Empty : completedEvent.ExecutionSignature.Trim();
            RequiredForWorldReset = completedEvent.RequiredForWorldReset;
            ExecutionCycle = executionCycle;
        }

        public ActorsOperationalMaterializationCompletedEvent(
            ActorsMaterializationExecutionEntry entry,
            ActorKind actorKind,
            ActorsMaterializationExecutionCycle executionCycle,
            string sceneName,
            string source,
            string executionSignature,
            string handoffSource)
        {
            Actor = null;
            ActorKind = actorKind;
            AxisActorId = entry.AxisActorId;
            RuntimeActorId = entry.RuntimeActorId;
            ActorId = entry.RuntimeActorId.IsValid ? entry.RuntimeActorId.Value : string.Empty;
            ActorSpecId = string.IsNullOrWhiteSpace(entry.ActorSpecId) ? string.Empty : entry.ActorSpecId.Trim();
            ActorSetRef = string.IsNullOrWhiteSpace(entry.ActorSetRef) ? string.Empty : entry.ActorSetRef.Trim();
            SemanticParticipantId = string.IsNullOrWhiteSpace(entry.SemanticParticipantId) ? string.Empty : entry.SemanticParticipantId.Trim();
            OperationalRecipeKind = entry.OperationalRecipeKind;
            SpawnServiceName = "PreserveExisting";
            SceneName = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            HandoffSource = string.IsNullOrWhiteSpace(handoffSource) ? string.Empty : handoffSource.Trim();
            Reason = string.IsNullOrWhiteSpace(entry.Reason) ? "PreserveExisting" : entry.Reason.Trim();
            ExecutionSignature = string.IsNullOrWhiteSpace(executionSignature) ? string.Empty : executionSignature.Trim();
            RequiredForWorldReset = false;
            ExecutionCycle = executionCycle;
        }

        public bool IsPreserveExisting => string.Equals(SpawnServiceName, "PreserveExisting", StringComparison.Ordinal);

        public IActor Actor { get; }
        public ActorKind ActorKind { get; }
        public AxisActorId AxisActorId { get; }
        public RuntimeActorId RuntimeActorId { get; }
        public string ActorId { get; }
        public string ActorSpecId { get; }
        public string ActorSetRef { get; }
        public string SemanticParticipantId { get; }
        public ActorOperationalRecipeKind OperationalRecipeKind { get; }
        public string SpawnServiceName { get; }
        public string SceneName { get; }
        public string Source { get; }
        public string HandoffSource { get; }
        public string Reason { get; }
        public string ExecutionSignature { get; }
        public bool RequiredForWorldReset { get; }
        public ActorsMaterializationExecutionCycle ExecutionCycle { get; }

        public bool HasActor => Actor != null;
        public bool HasAxisActorId => AxisActorId.IsValid;
        public bool HasRuntimeActorId => RuntimeActorId.IsValid;
        public bool HasActorSpecId => !string.IsNullOrWhiteSpace(ActorSpecId);
        public bool HasActorSetRef => !string.IsNullOrWhiteSpace(ActorSetRef);
        public bool HasSemanticParticipantId => !string.IsNullOrWhiteSpace(SemanticParticipantId);
        public bool HasCanonicalPayload =>
            HasAxisActorId &&
            HasRuntimeActorId &&
            HasActorSpecId &&
            HasActorSetRef &&
            !string.IsNullOrWhiteSpace(SpawnServiceName) &&
            !string.IsNullOrWhiteSpace(SceneName) &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(ExecutionSignature) &&
            (ActorKind != ActorKind.Player || HasSemanticParticipantId);
        public bool IsValid => ExecutionCycle.IsValid && !string.IsNullOrWhiteSpace(ActorId) && HasRuntimeActorId;
    }

    /// <summary>
    /// Evento publicado ao final do ciclo operacional de materializacao
    /// (apos o dispatch de directives do ciclo corrente).
    /// </summary>
    public readonly struct ActorsOperationalMaterializationCycleCompletedEvent : IEvent
    {
        public ActorsOperationalMaterializationCycleCompletedEvent(
            string sceneName,
            ActorsMaterializationExecutionCycle executionCycle,
            ActorsOperationalMaterializationDispatchMode dispatchMode,
            ActorsOperationalMaterializationSourceKind sourceKind,
            string sourceId,
            SceneRouteId routeId,
            SceneRouteKind routeKind,
            string actorSetRef,
            string cycleSignature,
            string executionSignature,
            ActorKind[] expectedActorKinds,
            ActorsOperationalMaterializationCompletedEvent[] completedActors,
            bool hasActorSetMismatch)
        {
            SceneName = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
            ExecutionCycle = executionCycle;
            DispatchMode = dispatchMode;
            SourceKind = sourceKind;
            SourceId = string.IsNullOrWhiteSpace(sourceId) ? string.Empty : sourceId.Trim();
            RouteId = routeId;
            RouteKind = routeKind;
            ActorSetRef = string.IsNullOrWhiteSpace(actorSetRef) ? string.Empty : actorSetRef.Trim();
            CycleSignature = string.IsNullOrWhiteSpace(cycleSignature) ? string.Empty : cycleSignature.Trim();
            ExecutionSignature = string.IsNullOrWhiteSpace(executionSignature) ? string.Empty : executionSignature.Trim();
            ExpectedActorKinds = expectedActorKinds == null ? Array.Empty<ActorKind>() : (ActorKind[])expectedActorKinds.Clone();
            CompletedActors = completedActors == null ? Array.Empty<ActorsOperationalMaterializationCompletedEvent>() : (ActorsOperationalMaterializationCompletedEvent[])completedActors.Clone();
            HasActorSetMismatch = hasActorSetMismatch;
            MaterializedActorKinds = BuildMaterializedActorKinds(CompletedActors);
            PreservedActorKinds = BuildPreservedActorKinds(CompletedActors);
            ReadyActorKinds = BuildReadyActorKinds(CompletedActors);
            HasCanonicalPayload = BuildHasCanonicalPayload(
                SceneName,
                ExecutionCycle,
                DispatchMode,
                SourceKind,
                SourceId,
                RouteId,
                RouteKind,
                ActorSetRef,
                CycleSignature,
                ExecutionSignature,
                ExpectedActorKinds,
                CompletedActors,
                HasActorSetMismatch);
            IsGameplayOperationalReady = BuildIsGameplayOperationalReady(
                HasCanonicalPayload,
                HasActorSetMismatch,
                DispatchMode,
                RouteKind,
                ExpectedActorKinds,
                CompletedActors,
                out string readinessReason);
            ReadinessReason = readinessReason;
        }

        public string SceneName { get; }
        public ActorsMaterializationExecutionCycle ExecutionCycle { get; }
        public ActorsOperationalMaterializationDispatchMode DispatchMode { get; }
        public ActorsOperationalMaterializationSourceKind SourceKind { get; }
        public string SourceId { get; }
        public SceneRouteId RouteId { get; }
        public SceneRouteKind RouteKind { get; }
        public string ActorSetRef { get; }
        public string CycleSignature { get; }
        public string ExecutionSignature { get; }
        public ActorKind[] ExpectedActorKinds { get; }
        public ActorKind[] MaterializedActorKinds { get; }
        public ActorKind[] PreservedActorKinds { get; }
        public ActorKind[] ReadyActorKinds { get; }
        public ActorsOperationalMaterializationCompletedEvent[] CompletedActors { get; }
        public bool HasActorSetMismatch { get; }
        public bool HasCanonicalPayload { get; }
        public bool IsGameplayOperationalReady { get; }
        public string ReadinessReason { get; }

        public string Source => SourceId;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SceneName) &&
            ExecutionCycle.IsValid &&
            DispatchMode != ActorsOperationalMaterializationDispatchMode.Unknown &&
            SourceKind != ActorsOperationalMaterializationSourceKind.Unknown &&
            !string.IsNullOrWhiteSpace(SourceId) &&
            RouteId.IsValid &&
            RouteKind != SceneRouteKind.Unspecified &&
            !string.IsNullOrWhiteSpace(ActorSetRef) &&
            !string.IsNullOrWhiteSpace(CycleSignature) &&
            !string.IsNullOrWhiteSpace(ExecutionSignature);

        private static bool BuildHasCanonicalPayload(
            string sceneName,
            ActorsMaterializationExecutionCycle executionCycle,
            ActorsOperationalMaterializationDispatchMode dispatchMode,
            ActorsOperationalMaterializationSourceKind sourceKind,
            string sourceId,
            SceneRouteId routeId,
            SceneRouteKind routeKind,
            string actorSetRef,
            string cycleSignature,
            string executionSignature,
            ActorKind[] expectedActorKinds,
            ActorsOperationalMaterializationCompletedEvent[] completedActors,
            bool hasActorSetMismatch)
        {
            if (string.IsNullOrWhiteSpace(sceneName) ||
                !executionCycle.IsValid ||
                dispatchMode == ActorsOperationalMaterializationDispatchMode.Unknown ||
                sourceKind == ActorsOperationalMaterializationSourceKind.Unknown ||
                string.IsNullOrWhiteSpace(sourceId) ||
                !routeId.IsValid ||
                routeKind == SceneRouteKind.Unspecified ||
                string.IsNullOrWhiteSpace(actorSetRef) ||
                string.IsNullOrWhiteSpace(cycleSignature) ||
                string.IsNullOrWhiteSpace(executionSignature))
            {
                return false;
            }

            if (expectedActorKinds == null || expectedActorKinds.Length < 1)
            {
                return false;
            }

            if (hasActorSetMismatch)
            {
                return false;
            }

            if (completedActors == null || completedActors.Length < 1)
            {
                return false;
            }

            for (int index = 0; index < completedActors.Length; index += 1)
            {
                if (!completedActors[index].HasCanonicalPayload)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool BuildIsGameplayOperationalReady(
            bool hasCanonicalPayload,
            bool hasActorSetMismatch,
            ActorsOperationalMaterializationDispatchMode dispatchMode,
            SceneRouteKind routeKind,
            ActorKind[] expectedActorKinds,
            ActorsOperationalMaterializationCompletedEvent[] completedActors,
            out string readinessReason)
        {
            if (!hasCanonicalPayload)
            {
                readinessReason = "missing_canonical_payload";
                return false;
            }

            if (hasActorSetMismatch)
            {
                readinessReason = "actor_set_ref_mismatch";
                return false;
            }

            if (dispatchMode != ActorsOperationalMaterializationDispatchMode.PhaseLocalEntryReady)
            {
                readinessReason = "dispatch_mode_not_phase_local_entry_ready";
                return false;
            }

            if (routeKind != SceneRouteKind.Gameplay)
            {
                readinessReason = "route_kind_not_gameplay";
                return false;
            }

            if (!HasActorKind(ActorKind.Player, completedActors))
            {
                readinessReason = "player_not_materialized";
                return false;
            }

            for (int index = 0; index < expectedActorKinds.Length; index += 1)
            {
                ActorKind expectedKind = expectedActorKinds[index];
                if (expectedKind == ActorKind.Unknown)
                {
                    continue;
                }

                if (!HasActorKind(expectedKind, completedActors))
                {
                    readinessReason = $"missing_expected_actor_kind:{expectedKind}";
                    return false;
                }
            }

            readinessReason = "GameplayOperationalReady";
            return true;
        }

        private static ActorKind[] BuildMaterializedActorKinds(ActorsOperationalMaterializationCompletedEvent[] completedActors)
        {
            return BuildActorKindsByPreserveState(completedActors, includePreserved: false, includeMaterialized: true);
        }

        private static ActorKind[] BuildPreservedActorKinds(ActorsOperationalMaterializationCompletedEvent[] completedActors)
        {
            return BuildActorKindsByPreserveState(completedActors, includePreserved: true, includeMaterialized: false);
        }

        private static ActorKind[] BuildReadyActorKinds(ActorsOperationalMaterializationCompletedEvent[] completedActors)
        {
            return BuildActorKindsByPreserveState(completedActors, includePreserved: true, includeMaterialized: true);
        }

        private static ActorKind[] BuildActorKindsByPreserveState(
            ActorsOperationalMaterializationCompletedEvent[] completedActors,
            bool includePreserved,
            bool includeMaterialized)
        {
            if (completedActors == null || completedActors.Length == 0)
            {
                return Array.Empty<ActorKind>();
            }

            var kinds = new List<ActorKind>(completedActors.Length);
            for (int index = 0; index < completedActors.Length; index += 1)
            {
                ActorsOperationalMaterializationCompletedEvent completedActor = completedActors[index];
                bool include = (completedActor.IsPreserveExisting && includePreserved) ||
                               (!completedActor.IsPreserveExisting && includeMaterialized);
                ActorKind actorKind = completedActor.ActorKind;
                if (!include || actorKind == ActorKind.Unknown || kinds.Contains(actorKind))
                {
                    continue;
                }

                kinds.Add(actorKind);
            }

            return kinds.ToArray();
        }

        private static bool HasActorKind(ActorKind kind, ActorsOperationalMaterializationCompletedEvent[] completedActors)
        {
            if (kind == ActorKind.Unknown || completedActors == null || completedActors.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < completedActors.Length; index += 1)
            {
                if (completedActors[index].ActorKind == kind)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Bridge transitivo que converte o evento generico de spawn no handoff explicito do estagio operacional.
    /// </summary>
    public sealed class ActorsOperationalMaterializationHandoffBridge : IDisposable
    {
        private readonly IActorsMaterializationExecutionCycleContext _cycleContext;
        private readonly EventBinding<ActorSpawnCompletedEvent> _spawnCompletedBinding;
        private bool _disposed;

        public ActorsOperationalMaterializationHandoffBridge(IActorsMaterializationExecutionCycleContext cycleContext)
        {
            _cycleContext = cycleContext ?? throw new ArgumentNullException(nameof(cycleContext));
            _spawnCompletedBinding = new EventBinding<ActorSpawnCompletedEvent>(OnActorSpawnCompleted);
            EventBus<ActorSpawnCompletedEvent>.Register(_spawnCompletedBinding);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<ActorSpawnCompletedEvent>.Unregister(_spawnCompletedBinding);
        }

        private void OnActorSpawnCompleted(ActorSpawnCompletedEvent evt)
        {
            if (_disposed || string.IsNullOrWhiteSpace(evt.ActorId))
            {
                return;
            }

            if (!_cycleContext.TryGetCurrent(out ActorsMaterializationExecutionCycle cycle) || !cycle.IsValid)
            {
                return;
            }

            var completedEvent = new ActorsOperationalMaterializationCompletedEvent(
                evt,
                cycle,
                source: "GameplayRuntime/ActorsOperationalMaterializationHandoff");

            DebugUtility.Log(typeof(ActorsOperationalMaterializationHandoffBridge),
                ObservabilityTraceFormatter.BuildCompactLogMessage(
                    "[OBS][ActorsExecution][Operational] ActorSpawnCompleted forward-only",
                    ("traceId", evt.ExecutionSignature),
                    ("phaseLocalEntrySequence", cycle.PhaseLocalEntrySequence),
                    ("actorSpecId", evt.ActorSpecId),
                    ("actorSetRef", evt.ActorSetRef),
                    ("axisActorId", evt.AxisActorId),
                    ("runtimeActorId", evt.RuntimeActorId),
                    ("semanticParticipantId", evt.SemanticParticipantId),
                    ("source", evt.Source),
                    ("reason", evt.Reason),
                    ("scene", evt.SceneName)),
                DebugUtility.Colors.Info);

            _cycleContext.RecordCompletedActor(completedEvent);
            EventBus<ActorsOperationalMaterializationCompletedEvent>.Raise(
                completedEvent);
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }
}
