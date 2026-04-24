using System;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.Spawn;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution
{
    public interface IActorsMaterializationExecutionCycleContext
    {
        bool TryGetCurrent(out ActorsMaterializationExecutionCycle cycle);
        IDisposable OpenScope(ActorsMaterializationExecutionCycle cycle, string source);
    }

    /// <summary>
    /// Mantem o ciclo operacional corrente durante o dispatch canonico de materializacao.
    /// </summary>
    public sealed class ActorsMaterializationExecutionCycleContext : IActorsMaterializationExecutionCycleContext
    {
        private ActorsMaterializationExecutionCycle _current;
        private int _scopeDepth;

        public bool TryGetCurrent(out ActorsMaterializationExecutionCycle cycle)
        {
            cycle = _current;
            return _current.IsValid;
        }

        public IDisposable OpenScope(ActorsMaterializationExecutionCycle cycle, string source)
        {
            if (!cycle.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][ActorsExecution] Ciclo operacional invalido para abrir contexto source='{AsText(source)}'.");
            }

            _scopeDepth += 1;
            _current = cycle;
            return new Scope(this, source);
        }

        private void CloseScope(string source)
        {
            _scopeDepth = Math.Max(0, _scopeDepth - 1);
            if (_scopeDepth > 0)
            {
                return;
            }

            _current = default;
            DebugUtility.LogVerbose(typeof(ActorsMaterializationExecutionCycleContext),
                $"[OBS][ActorsExecution][CycleContext] Escopo operacional finalizado source='{AsText(source)}'.",
                DebugUtility.Colors.Info);
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
            string source,
            string executionSignature)
        {
            SceneName = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
            ExecutionCycle = executionCycle;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            ExecutionSignature = string.IsNullOrWhiteSpace(executionSignature) ? string.Empty : executionSignature.Trim();
        }

        public string SceneName { get; }
        public ActorsMaterializationExecutionCycle ExecutionCycle { get; }
        public string Source { get; }
        public string ExecutionSignature { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(SceneName) && ExecutionCycle.IsValid;
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

            DebugUtility.Log(typeof(ActorsOperationalMaterializationHandoffBridge),
                $"[OBS][ActorsExecution][Operational] ActorSpawnCompleted forward-only actorSpecId='{AsText(evt.ActorSpecId)}' actorSetRef='{AsText(evt.ActorSetRef)}' axisActorId='{evt.AxisActorId}' runtimeActorId='{evt.RuntimeActorId}' semanticParticipantId='{AsText(evt.SemanticParticipantId)}' source='{AsText(evt.Source)}' executionSignature='{AsText(evt.ExecutionSignature)}'.",
                DebugUtility.Colors.Info);

            EventBus<ActorsOperationalMaterializationCompletedEvent>.Raise(
                new ActorsOperationalMaterializationCompletedEvent(
                    evt,
                    cycle,
                    source: "GameplayRuntime/ActorsOperationalMaterializationHandoff"));
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }
}
