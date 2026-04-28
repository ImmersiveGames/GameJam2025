using System;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution
{
    public interface IActorsGameplayOperationalReadinessService : IDisposable
    {
        bool IsGameplayOperationalReady { get; }
        event Action<ActorsGameplayOperationalReadinessSnapshot> Changed;
        bool TryGetCurrent(out ActorsGameplayOperationalReadinessSnapshot snapshot);
    }

    public readonly struct ActorsGameplayOperationalReadinessSnapshot
    {
        public ActorsGameplayOperationalReadinessSnapshot(
            SessionTransitionPhaseLocalEntryReadyEvent phaseLocalEntryReadyEvent,
            ActorsOperationalMaterializationCycleCompletedEvent cycleCompletedEvent)
        {
            PhaseLocalEntryReadyEvent = phaseLocalEntryReadyEvent;
            CycleCompletedEvent = cycleCompletedEvent;
        }

        public SessionTransitionPhaseLocalEntryReadyEvent PhaseLocalEntryReadyEvent { get; }
        public ActorsOperationalMaterializationCycleCompletedEvent CycleCompletedEvent { get; }

        public SceneRouteId RouteId => PhaseLocalEntryReadyEvent.RouteId;
        public SceneRouteKind RouteKind => PhaseLocalEntryReadyEvent.RouteKind;
        public string SceneName => PhaseLocalEntryReadyEvent.SceneName;
        public string ActorSetRef => PhaseLocalEntryReadyEvent.ActorSetRef;
        public string SessionSignature => PhaseLocalEntryReadyEvent.SessionSignature;
        public string PhaseSignature => PhaseLocalEntryReadyEvent.PhaseSignature;
        public string ParticipationSignature => PhaseLocalEntryReadyEvent.ParticipationSignature;
        public string CycleSignature => PhaseLocalEntryReadyEvent.CycleSignature;

        public bool HasCurrentContext => PhaseLocalEntryReadyEvent.IsValid;
        public bool HasCanonicalPayload => PhaseLocalEntryReadyEvent.IsValid &&
                                          PhaseLocalEntryReadyEvent.HasCanonicalPayload &&
                                          CycleCompletedEvent.IsValid &&
                                          CycleCompletedEvent.HasCanonicalPayload;

        public bool IsGameplayOperationalReady => HasCanonicalPayload &&
                                                   PhaseLocalEntryReadyEvent.RouteKind == SceneRouteKind.Gameplay &&
                                                   CycleCompletedEvent.IsGameplayOperationalReady &&
                                                   CycleCompletedEvent.DispatchMode == ActorsOperationalMaterializationDispatchMode.PhaseLocalEntryReady &&
                                                   CycleCompletedEvent.RouteKind == PhaseLocalEntryReadyEvent.RouteKind &&
                                                   CycleCompletedEvent.RouteId == PhaseLocalEntryReadyEvent.RouteId &&
                                                   string.Equals(CycleCompletedEvent.SceneName, PhaseLocalEntryReadyEvent.SceneName, StringComparison.Ordinal) &&
                                                   string.Equals(CycleCompletedEvent.ActorSetRef, PhaseLocalEntryReadyEvent.ActorSetRef, StringComparison.Ordinal) &&
                                                   string.Equals(CycleCompletedEvent.CycleSignature, PhaseLocalEntryReadyEvent.CycleSignature, StringComparison.Ordinal);

        public string ReadinessReason => CycleCompletedEvent.ReadinessReason;
    }

    /// <summary>
    /// Readiness operacional canônica para liberar gameplay a partir do rail ActorsExecution.
    /// Consome o ciclo phase-local-entry-ready tipado e mantém apenas o último contexto atual.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class ActorsGameplayOperationalReadinessService : IActorsGameplayOperationalReadinessService
    {
        private readonly EventBinding<SessionTransitionPhaseLocalEntryReadyEvent> _phaseLocalEntryReadyBinding;
        private readonly EventBinding<ActorsOperationalMaterializationCycleCompletedEvent> _cycleCompletedBinding;
        private readonly object _sync = new();

        private SessionTransitionPhaseLocalEntryReadyEvent _currentPhaseLocalEntryReadyEvent;
        private ActorsOperationalMaterializationCycleCompletedEvent _currentCycleCompletedEvent;
        private bool _hasCurrentPhaseLocalEntryReadyEvent;
        private bool _hasCurrentCycleCompletedEvent;
        private bool _disposed;

        public ActorsGameplayOperationalReadinessService()
        {
            _phaseLocalEntryReadyBinding = new EventBinding<SessionTransitionPhaseLocalEntryReadyEvent>(OnPhaseLocalEntryReady);
            _cycleCompletedBinding = new EventBinding<ActorsOperationalMaterializationCycleCompletedEvent>(OnCycleCompleted);

            EventBus<SessionTransitionPhaseLocalEntryReadyEvent>.Register(_phaseLocalEntryReadyBinding);
            EventBus<ActorsOperationalMaterializationCycleCompletedEvent>.Register(_cycleCompletedBinding);
        }

        public event Action<ActorsGameplayOperationalReadinessSnapshot> Changed;

        public bool IsGameplayOperationalReady
        {
            get
            {
                lock (_sync)
                {
                    return TryBuildSnapshotLocked(out var snapshot) && snapshot.IsGameplayOperationalReady;
                }
            }
        }

        public bool TryGetCurrent(out ActorsGameplayOperationalReadinessSnapshot snapshot)
        {
            lock (_sync)
            {
                return TryBuildSnapshotLocked(out snapshot);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<SessionTransitionPhaseLocalEntryReadyEvent>.Unregister(_phaseLocalEntryReadyBinding);
            EventBus<ActorsOperationalMaterializationCycleCompletedEvent>.Unregister(_cycleCompletedBinding);
        }

        private void OnPhaseLocalEntryReady(SessionTransitionPhaseLocalEntryReadyEvent evt)
        {
            if (_disposed || !evt.IsValid || !evt.HasCanonicalPayload)
            {
                return;
            }

            lock (_sync)
            {
                _currentPhaseLocalEntryReadyEvent = evt;
                _hasCurrentPhaseLocalEntryReadyEvent = true;
                _currentCycleCompletedEvent = default;
                _hasCurrentCycleCompletedEvent = false;
            }

            PublishChanged();

            DebugUtility.Log(typeof(ActorsGameplayOperationalReadinessService),
                $"[OBS][ActorsExecution][OperationalReadiness] PhaseLocalEntryReady primed sessionSignature='{evt.SessionSignature}' routeId='{evt.RouteId}' routeKind='{evt.RouteKind}' scene='{evt.SceneName}' actorSetRef='{evt.ActorSetRef}' cycleSignature='{evt.CycleSignature}'.",
                DebugUtility.Colors.Info);
        }

        private void OnCycleCompleted(ActorsOperationalMaterializationCycleCompletedEvent evt)
        {
            if (_disposed || !evt.IsValid)
            {
                return;
            }

            bool accepted;
            lock (_sync)
            {
                accepted = _hasCurrentPhaseLocalEntryReadyEvent &&
                           _currentPhaseLocalEntryReadyEvent.RouteKind == evt.RouteKind &&
                           _currentPhaseLocalEntryReadyEvent.RouteId == evt.RouteId &&
                           string.Equals(_currentPhaseLocalEntryReadyEvent.SceneName, evt.SceneName, StringComparison.Ordinal) &&
                           string.Equals(_currentPhaseLocalEntryReadyEvent.ActorSetRef, evt.ActorSetRef, StringComparison.Ordinal) &&
                           string.Equals(_currentPhaseLocalEntryReadyEvent.CycleSignature, evt.CycleSignature, StringComparison.Ordinal);

                if (accepted)
                {
                    _currentCycleCompletedEvent = evt;
                    _hasCurrentCycleCompletedEvent = true;
                }
            }

            if (!accepted)
            {
                return;
            }

            PublishChanged();

            DebugUtility.Log(typeof(ActorsGameplayOperationalReadinessService),
                $"[OBS][ActorsExecution][OperationalReadiness] CycleCompleted consumed dispatchMode='{evt.DispatchMode.ToLogToken()}' actorSetRef='{evt.ActorSetRef}' expectedKinds={FormatActorKinds(evt.ExpectedActorKinds)} materializedKinds={FormatActorKinds(evt.MaterializedActorKinds)} isGameplayOperationalReady='{evt.IsGameplayOperationalReady.ToString().ToLowerInvariant()}' readinessReason='{evt.ReadinessReason}'.",
                DebugUtility.Colors.Info);
        }

        private bool TryBuildSnapshotLocked(out ActorsGameplayOperationalReadinessSnapshot snapshot)
        {
            snapshot = default;

            if (!_hasCurrentPhaseLocalEntryReadyEvent)
            {
                return false;
            }

            snapshot = new ActorsGameplayOperationalReadinessSnapshot(
                _currentPhaseLocalEntryReadyEvent,
                _hasCurrentCycleCompletedEvent ? _currentCycleCompletedEvent : default);
            return snapshot.HasCurrentContext;
        }

        private void PublishChanged()
        {
            ActorsGameplayOperationalReadinessSnapshot snapshot;

            lock (_sync)
            {
                if (!TryBuildSnapshotLocked(out snapshot))
                {
                    return;
                }
            }

            Changed?.Invoke(snapshot);
        }

        private static string FormatActorKinds(ActorKind[] actorKinds)
        {
            if (actorKinds == null || actorKinds.Length == 0)
            {
                return "[]";
            }

            return $"[{string.Join(",", actorKinds)}]";
        }
    }
}
