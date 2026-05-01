using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Context
{
    public enum PhaseEntryReadinessStatus
    {
        Unknown = 0,
        Blocked = 1,
        Ready = 2,
        Stale = 3
    }

    public enum PhaseEntryReadinessReasonKind
    {
        Unknown = 0,
        WaitingForPhaseLocalEntryReady = 1,
        WaitingForActorsCycleCompleted = 2,
        WaitingForSceneTransitionCompleted = 3,
        WaitingForIntroStageCompleted = 4,
        WaitingForParticipationSnapshot = 5,
        WaitingForPhaseRuntimeSnapshot = 6,
        ContextMismatch = 7,
        StaleFactIgnored = 8,
        Ready = 9
    }

    public readonly struct PhaseEntryCanonicalSignature
    {
        public PhaseEntryCanonicalSignature(
            PhaseEntryIdentity phaseEntryIdentity,
            string cycleSignature,
            string sessionSignature,
            string phaseSignature,
            string participationSignature,
            string actorSetRef,
            SceneRouteId routeId,
            SceneRouteKind routeKind,
            string sceneName)
        {
            PhaseEntryIdentity = phaseEntryIdentity;
            CycleSignature = Normalize(cycleSignature);
            SessionSignature = Normalize(sessionSignature);
            PhaseSignature = Normalize(phaseSignature);
            ParticipationSignature = Normalize(participationSignature);
            ActorSetRef = Normalize(actorSetRef);
            RouteId = routeId;
            RouteKind = routeKind;
            SceneName = Normalize(sceneName);
            Signature = BuildSignature(
                CycleSignature,
                SessionSignature,
                PhaseSignature,
                ParticipationSignature,
                ActorSetRef,
                RouteId,
                RouteKind,
                SceneName,
                phaseEntryIdentity.PhaseEntryId);
        }

        public PhaseEntryIdentity PhaseEntryIdentity { get; }
        public string CycleSignature { get; }
        public string SessionSignature { get; }
        public string PhaseSignature { get; }
        public string ParticipationSignature { get; }
        public string ActorSetRef { get; }
        public SceneRouteId RouteId { get; }
        public SceneRouteKind RouteKind { get; }
        public string SceneName { get; }
        public string Signature { get; }

        public bool IsValid =>
            PhaseEntryIdentity.IsValid &&
            !string.IsNullOrWhiteSpace(CycleSignature) &&
            !string.IsNullOrWhiteSpace(PhaseSignature) &&
            !string.IsNullOrWhiteSpace(ActorSetRef) &&
            RouteId.IsValid &&
            RouteKind != SceneRouteKind.Unspecified &&
            !string.IsNullOrWhiteSpace(SceneName);

        public static PhaseEntryCanonicalSignature Empty => default;

        private static string BuildSignature(
            string cycleSignature,
            string sessionSignature,
            string phaseSignature,
            string participationSignature,
            string actorSetRef,
            SceneRouteId routeId,
            SceneRouteKind routeKind,
            string sceneName,
            string phaseEntryId)
        {
            return $"phaseEntry:{AsText(phaseEntryId)}|cycle:{AsText(cycleSignature)}|session:{AsText(sessionSignature)}|phase:{AsText(phaseSignature)}|participation:{AsText(participationSignature)}|actors:{AsText(actorSetRef)}|routeId:{routeId}|routeKind:{routeKind}|scene:{AsText(sceneName)}";
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private static string AsText(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }

    public readonly struct PhaseEntryPermissionSnapshot
    {
        public PhaseEntryPermissionSnapshot(
            PhaseEntryCanonicalSignature signature,
            PhaseEntryReadinessStatus status,
            PhaseEntryReadinessReasonKind reasonKind,
            string reason,
            bool hasPhaseLocalEntryReady,
            bool hasMatchingActorsReady,
            bool hasMatchingSceneTransitionCompleted,
            bool hasMatchingIntroCompleted,
            bool hasParticipationSnapshot,
            bool participationAllowsGameplay,
            bool hasPhaseRuntimeSnapshot,
            bool gameLoopAlreadyPlaying,
            string staleFactSource)
        {
            CanonicalSignature = signature;
            Status = status;
            ReasonKind = reasonKind;
            Reason = Normalize(reason);
            HasPhaseLocalEntryReady = hasPhaseLocalEntryReady;
            HasMatchingActorsReady = hasMatchingActorsReady;
            HasMatchingSceneTransitionCompleted = hasMatchingSceneTransitionCompleted;
            HasMatchingIntroCompleted = hasMatchingIntroCompleted;
            HasParticipationSnapshot = hasParticipationSnapshot;
            ParticipationAllowsGameplay = participationAllowsGameplay;
            HasPhaseRuntimeSnapshot = hasPhaseRuntimeSnapshot;
            GameLoopAlreadyPlaying = gameLoopAlreadyPlaying;
            StaleFactSource = Normalize(staleFactSource);
        }

        public PhaseEntryCanonicalSignature CanonicalSignature { get; }
        public PhaseEntryReadinessStatus Status { get; }
        public PhaseEntryReadinessReasonKind ReasonKind { get; }
        public string Reason { get; }
        public bool HasPhaseLocalEntryReady { get; }
        public bool HasMatchingActorsReady { get; }
        public bool HasMatchingSceneTransitionCompleted { get; }
        public bool HasMatchingIntroCompleted { get; }
        public bool HasParticipationSnapshot { get; }
        public bool ParticipationAllowsGameplay { get; }
        public bool HasPhaseRuntimeSnapshot { get; }
        public bool GameLoopAlreadyPlaying { get; }
        public string StaleFactSource { get; }

        public bool IsValid => Status != PhaseEntryReadinessStatus.Unknown && CanonicalSignature.IsValid;

        public static PhaseEntryPermissionSnapshot Empty =>
            new(
                PhaseEntryCanonicalSignature.Empty,
                PhaseEntryReadinessStatus.Unknown,
                PhaseEntryReadinessReasonKind.Unknown,
                "waiting_for_phase_entry",
                hasPhaseLocalEntryReady: false,
                hasMatchingActorsReady: false,
                hasMatchingSceneTransitionCompleted: false,
                hasMatchingIntroCompleted: false,
                hasParticipationSnapshot: false,
                participationAllowsGameplay: false,
                hasPhaseRuntimeSnapshot: false,
                gameLoopAlreadyPlaying: false,
                staleFactSource: string.Empty);

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct PhaseEntryReadinessChangedEvent : IEvent
    {
        public PhaseEntryReadinessChangedEvent(PhaseEntryPermissionSnapshot snapshot, string source)
        {
            Snapshot = snapshot;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        public PhaseEntryPermissionSnapshot Snapshot { get; }
        public string Source { get; }
        public bool IsValid => Snapshot.IsValid;
    }

    public interface IPhaseEntryReadinessCoordinator : IDisposable
    {
        event Action<PhaseEntryPermissionSnapshot> Changed;
        bool TryGetCurrent(out PhaseEntryPermissionSnapshot snapshot);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseEntryReadinessCoordinator : IPhaseEntryReadinessCoordinator
    {
        private readonly object _sync = new();
        private readonly EventBinding<SessionTransitionPhaseLocalEntryReadyEvent> _phaseLocalEntryReadyBinding;
        private readonly EventBinding<ActorsOperationalMaterializationCycleCompletedEvent> _actorsCycleCompletedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _sceneTransitionCompletedBinding;
        private readonly EventBinding<IntroStageCompletedEvent> _introStageCompletedBinding;
        private readonly EventBinding<GameplayPhaseRuntimeMaterializedEvent> _phaseRuntimeMaterializedBinding;

        private readonly IGameplayParticipationFlowService _participationFlowService;
        private readonly IGameplayPhaseRuntimeService _phaseRuntimeService;
        private readonly IGameLoopService _gameLoopService;
        private PhaseEntryIdentity _activePhaseEntryIdentity;
        private bool _hasActivePhaseEntryIdentity;

        private SessionTransitionPhaseLocalEntryReadyEvent _currentPhaseLocalEntryReady;
        private ActorsOperationalMaterializationCycleCompletedEvent _currentActorsCycleCompleted;
        private SceneTransitionCompletedEvent _currentSceneTransitionCompleted;
        private IntroStageCompletedEvent _currentIntroStageCompleted;
        private GameplayPhaseRuntimeMaterializedEvent _currentPhaseRuntimeMaterialized;
        private bool _hasPhaseLocalEntryReady;
        private bool _hasActorsCycleCompleted;
        private bool _hasSceneTransitionCompleted;
        private bool _hasIntroStageCompleted;
        private bool _hasPhaseRuntimeMaterialized;
        private bool _disposed;
        private PhaseEntryPermissionSnapshot _lastPublishedSnapshot;
        private bool _hasLastPublishedSnapshot;
        private string _lastStaleFactSource;

        public PhaseEntryReadinessCoordinator()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameplayParticipationFlowService>(out _participationFlowService) || _participationFlowService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IGameplayParticipationFlowService ausente para compor PhaseEntryReadinessCoordinator.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayPhaseRuntimeService>(out _phaseRuntimeService) || _phaseRuntimeService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IGameplayPhaseRuntimeService ausente para compor PhaseEntryReadinessCoordinator.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out _gameLoopService) || _gameLoopService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IGameLoopService ausente para compor PhaseEntryReadinessCoordinator.");
            }

            _phaseLocalEntryReadyBinding = new EventBinding<SessionTransitionPhaseLocalEntryReadyEvent>(OnPhaseLocalEntryReady);
            _actorsCycleCompletedBinding = new EventBinding<ActorsOperationalMaterializationCycleCompletedEvent>(OnActorsCycleCompleted);
            _sceneTransitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);
            _introStageCompletedBinding = new EventBinding<IntroStageCompletedEvent>(OnIntroStageCompleted);
            _phaseRuntimeMaterializedBinding = new EventBinding<GameplayPhaseRuntimeMaterializedEvent>(OnPhaseRuntimeMaterialized);

            EventBus<SessionTransitionPhaseLocalEntryReadyEvent>.Register(_phaseLocalEntryReadyBinding);
            EventBus<ActorsOperationalMaterializationCycleCompletedEvent>.Register(_actorsCycleCompletedBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_sceneTransitionCompletedBinding);
            EventBus<IntroStageCompletedEvent>.Register(_introStageCompletedBinding);
            EventBus<GameplayPhaseRuntimeMaterializedEvent>.Register(_phaseRuntimeMaterializedBinding);

            PublishSnapshot("bootstrap");
        }

        public event Action<PhaseEntryPermissionSnapshot> Changed;

        public bool TryGetCurrent(out PhaseEntryPermissionSnapshot snapshot)
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
            EventBus<ActorsOperationalMaterializationCycleCompletedEvent>.Unregister(_actorsCycleCompletedBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_sceneTransitionCompletedBinding);
            EventBus<IntroStageCompletedEvent>.Unregister(_introStageCompletedBinding);
            EventBus<GameplayPhaseRuntimeMaterializedEvent>.Unregister(_phaseRuntimeMaterializedBinding);
        }

        private void OnPhaseLocalEntryReady(SessionTransitionPhaseLocalEntryReadyEvent evt)
        {
            if (_disposed || !evt.IsValid || !evt.HasCanonicalPayload || !evt.IsPhaseLocalEntry)
            {
                return;
            }

            lock (_sync)
            {
                OpenActiveEntryLocked(
                    evt.PhaseEntryIdentity,
                    evt.RouteId,
                    evt.RouteKind,
                    evt.SceneName,
                    "phase_local_entry_ready");
                _currentPhaseLocalEntryReady = evt;
                _hasPhaseLocalEntryReady = true;
                _lastStaleFactSource = string.Empty;
            }

            PublishSnapshot("phase_local_entry_ready");
        }

        private void OnActorsCycleCompleted(ActorsOperationalMaterializationCycleCompletedEvent evt)
        {
            if (_disposed || !evt.IsValid)
            {
                return;
            }

            bool isStale;
            lock (_sync)
            {
                isStale = !_hasActivePhaseEntryIdentity ||
                          !_hasPhaseLocalEntryReady ||
                          !MatchesCurrentIdentity(evt.PhaseEntryIdentity) ||
                          !MatchesCurrentEntry(evt, _currentPhaseLocalEntryReady);
                if (isStale)
                {
                    _lastStaleFactSource = "actors_cycle_completed";
                    LogStaleFact("actors_cycle_completed", evt.CycleSignature);
                }
                else
                {
                    _currentActorsCycleCompleted = evt;
                    _hasActorsCycleCompleted = true;
                    _lastStaleFactSource = string.Empty;
                }
            }

            PublishSnapshot(isStale ? "actors_cycle_completed_stale" : "actors_cycle_completed");
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            bool isStale;
            lock (_sync)
            {
                if (!_hasActivePhaseEntryIdentity)
                {
                    isStale = true;
                }
                else
                {
                    bool sceneMatches = MatchesSceneAgainstActiveIdentity(evt.context, _activePhaseEntryIdentity);
                    if (sceneMatches)
                    {
                        _currentSceneTransitionCompleted = evt;
                        _hasSceneTransitionCompleted = true;
                        _lastStaleFactSource = string.Empty;
                        isStale = false;
                    }
                    else
                    {
                        isStale = true;
                    }
                }

                if (isStale)
                {
                    _lastStaleFactSource = "scene_transition_completed";
                    LogStaleFact("scene_transition_completed", evt.context.ContextSignature);
                }
            }

            PublishSnapshot(isStale ? "scene_transition_completed_stale" : "scene_transition_completed");
        }

        private void OnIntroStageCompleted(IntroStageCompletedEvent evt)
        {
            if (_disposed || !evt.Session.IsValid)
            {
                return;
            }

            bool isStale;
            lock (_sync)
            {
                isStale = !_hasActivePhaseEntryIdentity ||
                          !MatchesCurrentIdentity(evt.Session.PhaseEntryIdentity) ||
                          !_hasPhaseLocalEntryReady ||
                          !MatchesCurrentEntry(evt, _currentPhaseLocalEntryReady);
                if (isStale)
                {
                    _lastStaleFactSource = "intro_stage_completed";
                    LogStaleFact("intro_stage_completed", evt.Session.EntrySignature);
                }
                else
                {
                    _currentIntroStageCompleted = evt;
                    _hasIntroStageCompleted = true;
                    _lastStaleFactSource = string.Empty;
                }
            }

            PublishSnapshot(isStale ? "intro_stage_completed_stale" : "intro_stage_completed");
        }

        private void OnPhaseRuntimeMaterialized(GameplayPhaseRuntimeMaterializedEvent evt)
        {
            if (_disposed || !evt.Runtime.IsValid || !evt.PhaseEntryIdentity.IsValid)
            {
                return;
            }

            lock (_sync)
            {
                OpenActiveEntryLocked(
                    evt.PhaseEntryIdentity,
                    evt.PhaseEntryIdentity.RouteId,
                    evt.PhaseEntryIdentity.RouteKind,
                    evt.PhaseEntryIdentity.SceneName,
                    "phase_runtime_materialized");
                _currentPhaseRuntimeMaterialized = evt;
                _hasPhaseRuntimeMaterialized = true;
                _lastStaleFactSource = string.Empty;
            }

            PublishSnapshot("phase_runtime_materialized");
        }

        private void PublishSnapshot(string source)
        {
            PhaseEntryPermissionSnapshot snapshot;
            bool shouldPublish;

            lock (_sync)
            {
                if (!TryBuildSnapshotLocked(out snapshot))
                {
                    snapshot = PhaseEntryPermissionSnapshot.Empty;
                }

                shouldPublish = !_hasLastPublishedSnapshot || !AreEquivalent(_lastPublishedSnapshot, snapshot);
                if (!shouldPublish)
                {
                    return;
                }

                _lastPublishedSnapshot = snapshot;
                _hasLastPublishedSnapshot = true;
            }

            DebugUtility.Log<PhaseEntryReadinessCoordinator>(
                $"[OBS][SessionIntegration][PhaseEntryReadiness] status='{snapshot.Status}' reason='{AsText(snapshot.Reason)}' phaseEntryId='{AsText(snapshot.CanonicalSignature.PhaseEntryIdentity.PhaseEntryId)}' cycle='{ToShortCycleSignature(snapshot.CanonicalSignature.CycleSignature)}' routeId='{snapshot.CanonicalSignature.RouteId}' scene='{AsText(snapshot.CanonicalSignature.SceneName)}' actorsMatched='{snapshot.HasMatchingActorsReady.ToString().ToLowerInvariant()}' introMatched='{snapshot.HasMatchingIntroCompleted.ToString().ToLowerInvariant()}' sceneMatched='{snapshot.HasMatchingSceneTransitionCompleted.ToString().ToLowerInvariant()}' gameLoopAlreadyPlaying='{snapshot.GameLoopAlreadyPlaying.ToString().ToLowerInvariant()}' staleFactSource='{AsText(snapshot.StaleFactSource)}'.",
                DebugUtility.Colors.Info);

            Changed?.Invoke(snapshot);
            EventBus<PhaseEntryReadinessChangedEvent>.Raise(new PhaseEntryReadinessChangedEvent(snapshot, source));
        }

        private bool TryBuildSnapshotLocked(out PhaseEntryPermissionSnapshot snapshot)
        {
            snapshot = PhaseEntryPermissionSnapshot.Empty;

            if (!_hasActivePhaseEntryIdentity)
            {
                snapshot = new PhaseEntryPermissionSnapshot(
                    PhaseEntryCanonicalSignature.Empty,
                    PhaseEntryReadinessStatus.Unknown,
                    PhaseEntryReadinessReasonKind.WaitingForPhaseLocalEntryReady,
                    "waiting_for_phase_local_entry_ready",
                    hasPhaseLocalEntryReady: false,
                    hasMatchingActorsReady: false,
                    hasMatchingSceneTransitionCompleted: false,
                    hasMatchingIntroCompleted: false,
                    hasParticipationSnapshot: false,
                    participationAllowsGameplay: false,
                    hasPhaseRuntimeSnapshot: false,
                    gameLoopAlreadyPlaying: IsGameLoopPlaying(),
                    staleFactSource: _lastStaleFactSource);
                return true;
            }

            if (!_hasPhaseLocalEntryReady || !_currentPhaseLocalEntryReady.HasCanonicalPayload)
            {
                snapshot = new PhaseEntryPermissionSnapshot(
                    new PhaseEntryCanonicalSignature(
                        _activePhaseEntryIdentity,
                        string.Empty,
                        _activePhaseEntryIdentity.SessionSignature,
                        _activePhaseEntryIdentity.PhaseRuntimeSignature,
                        string.Empty,
                        string.Empty,
                        _activePhaseEntryIdentity.RouteId,
                        _activePhaseEntryIdentity.RouteKind,
                        _activePhaseEntryIdentity.SceneName),
                    PhaseEntryReadinessStatus.Blocked,
                    PhaseEntryReadinessReasonKind.WaitingForPhaseLocalEntryReady,
                    "waiting_for_phase_local_entry_ready",
                    hasPhaseLocalEntryReady: false,
                    hasMatchingActorsReady: false,
                    hasMatchingSceneTransitionCompleted: _hasSceneTransitionCompleted,
                    hasMatchingIntroCompleted: false,
                    hasParticipationSnapshot: false,
                    participationAllowsGameplay: false,
                    hasPhaseRuntimeSnapshot: _hasPhaseRuntimeMaterialized,
                    gameLoopAlreadyPlaying: IsGameLoopPlaying(),
                    staleFactSource: _lastStaleFactSource);
                return true;
            }

            PhaseEntryCanonicalSignature signature = BuildCanonicalSignature(_currentPhaseLocalEntryReady);
            bool hasParticipationSnapshot = _participationFlowService.TryGetCurrentReadiness(out var participationReadiness) &&
                                            participationReadiness.IsValid;
            bool participationAllowsGameplay = hasParticipationSnapshot && participationReadiness.CanEnterGameplay;
            bool hasPhaseRuntimeSnapshotFromEvent = _hasPhaseRuntimeMaterialized &&
                                                    MatchesCurrentEntry(_currentPhaseRuntimeMaterialized, _currentPhaseLocalEntryReady);
            bool hasPhaseRuntimeSnapshotFromService = _phaseRuntimeService.TryGetCurrent(out var phaseRuntimeSnapshot) &&
                                                      phaseRuntimeSnapshot.IsValid &&
                                                      MatchesCurrentEntry(phaseRuntimeSnapshot, _currentPhaseLocalEntryReady);
            bool hasPhaseRuntimeSnapshot = hasPhaseRuntimeSnapshotFromEvent || hasPhaseRuntimeSnapshotFromService;
            bool hasMatchingActors = _hasActorsCycleCompleted &&
                                     _currentActorsCycleCompleted.IsGameplayOperationalReady &&
                                     MatchesCurrentEntry(_currentActorsCycleCompleted, _currentPhaseLocalEntryReady);
            bool hasMatchingScene = _hasSceneTransitionCompleted &&
                                    MatchesCurrentEntry(_currentSceneTransitionCompleted.context, _currentPhaseLocalEntryReady);
            bool hasMatchingIntro = _hasIntroStageCompleted &&
                                    MatchesCurrentEntry(_currentIntroStageCompleted, _currentPhaseLocalEntryReady) &&
                                    IsIntroCompleted(_currentIntroStageCompleted);
            bool gameLoopAlreadyPlaying = IsGameLoopPlaying();

            if (!hasParticipationSnapshot)
            {
                snapshot = BuildBlockedSnapshot(signature, PhaseEntryReadinessReasonKind.WaitingForParticipationSnapshot, "waiting_for_participation_snapshot", hasMatchingActors, hasMatchingScene, hasMatchingIntro, hasParticipationSnapshot, participationAllowsGameplay, hasPhaseRuntimeSnapshot, gameLoopAlreadyPlaying);
                return true;
            }

            if (!participationAllowsGameplay)
            {
                snapshot = BuildBlockedSnapshot(signature, PhaseEntryReadinessReasonKind.ContextMismatch, "participation_not_ready_for_gameplay", hasMatchingActors, hasMatchingScene, hasMatchingIntro, hasParticipationSnapshot, participationAllowsGameplay, hasPhaseRuntimeSnapshot, gameLoopAlreadyPlaying);
                return true;
            }

            if (!hasPhaseRuntimeSnapshot)
            {
                snapshot = BuildBlockedSnapshot(signature, PhaseEntryReadinessReasonKind.WaitingForPhaseRuntimeSnapshot, "waiting_for_phase_runtime_snapshot", hasMatchingActors, hasMatchingScene, hasMatchingIntro, hasParticipationSnapshot, participationAllowsGameplay, hasPhaseRuntimeSnapshot, gameLoopAlreadyPlaying);
                return true;
            }

            if (!hasMatchingActors)
            {
                snapshot = BuildBlockedSnapshot(signature, PhaseEntryReadinessReasonKind.WaitingForActorsCycleCompleted, "waiting_for_matching_actors_cycle_completed", hasMatchingActors, hasMatchingScene, hasMatchingIntro, hasParticipationSnapshot, participationAllowsGameplay, hasPhaseRuntimeSnapshot, gameLoopAlreadyPlaying);
                return true;
            }

            if (!hasMatchingScene)
            {
                snapshot = BuildBlockedSnapshot(signature, PhaseEntryReadinessReasonKind.WaitingForSceneTransitionCompleted, "waiting_for_matching_scene_transition_completed", hasMatchingActors, hasMatchingScene, hasMatchingIntro, hasParticipationSnapshot, participationAllowsGameplay, hasPhaseRuntimeSnapshot, gameLoopAlreadyPlaying);
                return true;
            }

            if (!hasMatchingIntro)
            {
                snapshot = BuildBlockedSnapshot(signature, PhaseEntryReadinessReasonKind.WaitingForIntroStageCompleted, "waiting_for_matching_intro_stage_completed", hasMatchingActors, hasMatchingScene, hasMatchingIntro, hasParticipationSnapshot, participationAllowsGameplay, hasPhaseRuntimeSnapshot, gameLoopAlreadyPlaying);
                return true;
            }

            snapshot = new PhaseEntryPermissionSnapshot(
                signature,
                PhaseEntryReadinessStatus.Ready,
                PhaseEntryReadinessReasonKind.Ready,
                "phase_entry_ready",
                hasPhaseLocalEntryReady: true,
                hasMatchingActorsReady: true,
                hasMatchingSceneTransitionCompleted: true,
                hasMatchingIntroCompleted: true,
                hasParticipationSnapshot: true,
                participationAllowsGameplay: true,
                hasPhaseRuntimeSnapshot: true,
                gameLoopAlreadyPlaying: gameLoopAlreadyPlaying,
                staleFactSource: string.Empty);
            return true;
        }

        private PhaseEntryPermissionSnapshot BuildBlockedSnapshot(
            PhaseEntryCanonicalSignature signature,
            PhaseEntryReadinessReasonKind reasonKind,
            string reason,
            bool hasMatchingActors,
            bool hasMatchingScene,
            bool hasMatchingIntro,
            bool hasParticipationSnapshot,
            bool participationAllowsGameplay,
            bool hasPhaseRuntimeSnapshot,
            bool gameLoopAlreadyPlaying)
        {
            return new PhaseEntryPermissionSnapshot(
                signature,
                PhaseEntryReadinessStatus.Blocked,
                reasonKind,
                reason,
                hasPhaseLocalEntryReady: true,
                hasMatchingActorsReady: hasMatchingActors,
                hasMatchingSceneTransitionCompleted: hasMatchingScene,
                hasMatchingIntroCompleted: hasMatchingIntro,
                hasParticipationSnapshot: hasParticipationSnapshot,
                participationAllowsGameplay: participationAllowsGameplay,
                hasPhaseRuntimeSnapshot: hasPhaseRuntimeSnapshot,
                gameLoopAlreadyPlaying: gameLoopAlreadyPlaying,
                staleFactSource: _lastStaleFactSource);
        }

        private bool IsGameLoopPlaying()
            => string.Equals(_gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal);

        private static bool MatchesCurrentEntry(
            ActorsOperationalMaterializationCycleCompletedEvent actorsCycle,
            SessionTransitionPhaseLocalEntryReadyEvent entry)
        {
            return actorsCycle.PhaseEntryIdentity == entry.PhaseEntryIdentity &&
                   string.Equals(actorsCycle.CycleSignature, entry.CycleSignature, StringComparison.Ordinal) &&
                   string.Equals(actorsCycle.ActorSetRef, entry.ActorSetRef, StringComparison.Ordinal) &&
                   actorsCycle.RouteId == entry.RouteId &&
                   actorsCycle.RouteKind == entry.RouteKind &&
                   string.Equals(actorsCycle.SceneName, entry.SceneName, StringComparison.Ordinal);
        }

        private static bool MatchesCurrentEntry(
            SceneTransitionContext context,
            SessionTransitionPhaseLocalEntryReadyEvent entry)
        {
            return context.RouteKind == entry.RouteKind &&
                   context.RouteId == entry.RouteId &&
                   string.Equals(context.TargetActiveScene, entry.SceneName, StringComparison.Ordinal);
        }

        private static bool MatchesSceneAgainstActiveIdentity(
            SceneTransitionContext context,
            PhaseEntryIdentity activeIdentity)
        {
            return context.RouteKind == activeIdentity.RouteKind &&
                   context.RouteId == activeIdentity.RouteId &&
                   string.Equals(context.TargetActiveScene, activeIdentity.SceneName, StringComparison.Ordinal);
        }

        private static bool MatchesCurrentEntry(
            IntroStageCompletedEvent introCompleted,
            SessionTransitionPhaseLocalEntryReadyEvent entry)
        {
            return introCompleted.Session.PhaseEntryIdentity == entry.PhaseEntryIdentity;
        }

        private static bool MatchesCurrentEntry(
            GameplayPhaseRuntimeMaterializedEvent runtimeEvt,
            SessionTransitionPhaseLocalEntryReadyEvent entry)
        {
            return runtimeEvt.Runtime.IsValid && runtimeEvt.PhaseEntryIdentity == entry.PhaseEntryIdentity;
        }

        private static bool MatchesCurrentEntry(
            GameplayPhaseRuntimeSnapshot runtimeSnapshot,
            SessionTransitionPhaseLocalEntryReadyEvent entry)
        {
            return runtimeSnapshot.IsValid && runtimeSnapshot.PhaseEntryIdentity == entry.PhaseEntryIdentity;
        }

        private static bool IsIntroCompleted(IntroStageCompletedEvent evt)
        {
            if (!evt.WasSkipped)
            {
                return true;
            }

            return string.Equals(evt.Reason, PhaseFlowSignalVocabulary.NoContentReason, StringComparison.OrdinalIgnoreCase);
        }

        private static PhaseEntryCanonicalSignature BuildCanonicalSignature(SessionTransitionPhaseLocalEntryReadyEvent evt)
        {
            return new PhaseEntryCanonicalSignature(
                evt.PhaseEntryIdentity,
                evt.CycleSignature,
                evt.SessionSignature,
                evt.PhaseSignature,
                evt.ParticipationSignature,
                evt.ActorSetRef,
                evt.RouteId,
                evt.RouteKind,
                evt.SceneName);
        }

        private static bool AreEquivalent(PhaseEntryPermissionSnapshot left, PhaseEntryPermissionSnapshot right)
        {
            return left.Status == right.Status &&
                   left.ReasonKind == right.ReasonKind &&
                   string.Equals(left.Reason, right.Reason, StringComparison.Ordinal) &&
                   string.Equals(left.CanonicalSignature.Signature, right.CanonicalSignature.Signature, StringComparison.Ordinal) &&
                   left.HasPhaseLocalEntryReady == right.HasPhaseLocalEntryReady &&
                   left.HasMatchingActorsReady == right.HasMatchingActorsReady &&
                   left.HasMatchingSceneTransitionCompleted == right.HasMatchingSceneTransitionCompleted &&
                   left.HasMatchingIntroCompleted == right.HasMatchingIntroCompleted &&
                   left.HasParticipationSnapshot == right.HasParticipationSnapshot &&
                   left.ParticipationAllowsGameplay == right.ParticipationAllowsGameplay &&
                   left.HasPhaseRuntimeSnapshot == right.HasPhaseRuntimeSnapshot &&
                   left.GameLoopAlreadyPlaying == right.GameLoopAlreadyPlaying &&
                   string.Equals(left.StaleFactSource, right.StaleFactSource, StringComparison.Ordinal);
        }

        private static string AsText(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private bool MatchesCurrentIdentity(PhaseEntryIdentity identity)
        {
            return _hasActivePhaseEntryIdentity &&
                   identity.IsValid &&
                   identity == _activePhaseEntryIdentity;
        }

        private void OpenActiveEntryLocked(
            PhaseEntryIdentity identity,
            SceneRouteId routeId,
            SceneRouteKind routeKind,
            string sceneName,
            string source)
        {
            if (!identity.IsValid)
            {
                return;
            }

            bool isNewIdentity = !_hasActivePhaseEntryIdentity || identity != _activePhaseEntryIdentity;
            if (!isNewIdentity)
            {
                return;
            }

            _activePhaseEntryIdentity = identity;
            _hasActivePhaseEntryIdentity = true;

            _currentPhaseLocalEntryReady = default;
            _currentActorsCycleCompleted = default;
            _currentIntroStageCompleted = default;
            _hasPhaseLocalEntryReady = false;
            _hasActorsCycleCompleted = false;
            _hasIntroStageCompleted = false;

            if (_hasSceneTransitionCompleted &&
                !MatchesSceneAgainstRouteScene(_currentSceneTransitionCompleted.context, routeId, routeKind, sceneName))
            {
                _currentSceneTransitionCompleted = default;
                _hasSceneTransitionCompleted = false;
            }

            if (_hasPhaseRuntimeMaterialized &&
                _currentPhaseRuntimeMaterialized.PhaseEntryIdentity != identity)
            {
                _currentPhaseRuntimeMaterialized = default;
                _hasPhaseRuntimeMaterialized = false;
            }

            _lastStaleFactSource = string.Empty;

            DebugUtility.LogVerbose<PhaseEntryReadinessCoordinator>(
                $"[OBS][SessionIntegration][PhaseEntryReadiness] ActivePhaseEntryOpened source='{AsText(source)}' phaseEntryId='{AsText(identity.PhaseEntryId)}' routeId='{routeId}' routeKind='{routeKind}' scene='{AsText(sceneName)}'.",
                DebugUtility.Colors.Info);
        }

        private static bool MatchesSceneAgainstRouteScene(
            SceneTransitionContext context,
            SceneRouteId routeId,
            SceneRouteKind routeKind,
            string sceneName)
        {
            return context.RouteKind == routeKind &&
                   context.RouteId == routeId &&
                   string.Equals(context.TargetActiveScene, sceneName, StringComparison.Ordinal);
        }

        private void LogStaleFact(string source, string signature)
        {
            string activePhaseEntryId = _hasActivePhaseEntryIdentity
                ? _activePhaseEntryIdentity.PhaseEntryId
                : _currentPhaseLocalEntryReady.PhaseEntryIdentity.PhaseEntryId;
            DebugUtility.LogVerbose<PhaseEntryReadinessCoordinator>(
                $"[OBS][SessionIntegration][PhaseEntryReadiness] StaleFactIgnored source='{source}' signature='{AsText(signature)}' activePhaseEntryId='{AsText(activePhaseEntryId)}' activeCycleSignature='{AsText(_currentPhaseLocalEntryReady.CycleSignature)}'.",
                DebugUtility.Colors.Info);
        }

        private static string ToShortCycleSignature(string cycleSignature)
        {
            if (string.IsNullOrWhiteSpace(cycleSignature))
            {
                return "<none>";
            }

            string normalized = cycleSignature.Trim();
            const int maxLength = 24;
            return normalized.Length <= maxLength
                ? normalized
                : normalized.Substring(0, maxLength);
        }
    }
}
