#nullable enable
using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Context
{
    public enum GameplayInteractionIntroStageStatus
    {
        Unknown = 0,
        Completed = 1,
        Skipped = 2,
        NoContent = 3
    }

    public enum GameplayInteractionReadinessReasonKind
    {
        Unknown = 0,
        WaitingForActorsOperationalReady = 1,
        WaitingForSceneTransitionCompleted = 2,
        WaitingForIntroStageDone = 3,
        ContextMismatch = 4,
        Ready = 5
    }

    public readonly struct GameplayInteractionReadinessSnapshot
    {
        public GameplayInteractionReadinessSnapshot(
            string sessionSignature,
            SceneRouteId routeId,
            SceneRouteKind routeKind,
            string sceneName,
            string actorSetRef,
            string cycleSignature,
            string phaseSignature,
            string participationSignature,
            GameplayInteractionIntroStageStatus introStageStatus,
            bool hasActorsOperationalReadyObservation,
            bool actorsOperationalReady,
            bool hasSceneTransitionCompletedObservation,
            bool sceneTransitionCompleted,
            bool hasIntroStageStatusObservation,
            GameplayInteractionReadinessReasonKind readinessReasonKind,
            string readinessReason)
        {
            SessionSignature = Normalize(sessionSignature);
            RouteId = routeId;
            RouteKind = routeKind;
            SceneName = Normalize(sceneName);
            ActorSetRef = Normalize(actorSetRef);
            CycleSignature = Normalize(cycleSignature);
            PhaseSignature = Normalize(phaseSignature);
            ParticipationSignature = Normalize(participationSignature);
            IntroStageStatus = introStageStatus;
            HasActorsOperationalReadyObservation = hasActorsOperationalReadyObservation;
            ActorsOperationalReady = actorsOperationalReady;
            HasSceneTransitionCompletedObservation = hasSceneTransitionCompletedObservation;
            SceneTransitionCompleted = sceneTransitionCompleted;
            HasIntroStageStatusObservation = hasIntroStageStatusObservation;
            ReadinessReasonKind = readinessReasonKind;
            ReadinessReason = NormalizeReason(readinessReason, readinessReasonKind);
            InteractionSignature = BuildInteractionSignature(
                SessionSignature,
                RouteId,
                RouteKind,
                SceneName,
                ActorSetRef,
                CycleSignature,
                PhaseSignature,
                ParticipationSignature,
                IntroStageStatus);
        }

        public string SessionSignature { get; }
        public SceneRouteId RouteId { get; }
        public SceneRouteKind RouteKind { get; }
        public string SceneName { get; }
        public string ActorSetRef { get; }
        public string CycleSignature { get; }
        public string PhaseSignature { get; }
        public string PhaseRuntimeSignature => PhaseSignature;
        public string ParticipationSignature { get; }
        public GameplayInteractionIntroStageStatus IntroStageStatus { get; }
        public bool HasActorsOperationalReadyObservation { get; }
        public bool ActorsOperationalReady { get; }
        public bool HasSceneTransitionCompletedObservation { get; }
        public bool SceneTransitionCompleted { get; }
        public bool HasIntroStageStatusObservation { get; }
        public GameplayInteractionReadinessReasonKind ReadinessReasonKind { get; }
        public string ReadinessReason { get; }
        public string InteractionSignature { get; }

        public bool HasCanonicalPayload =>
            !string.IsNullOrWhiteSpace(SessionSignature) &&
            RouteId.IsValid &&
            RouteKind != SceneRouteKind.Unspecified &&
            !string.IsNullOrWhiteSpace(SceneName) &&
            !string.IsNullOrWhiteSpace(ActorSetRef) &&
            !string.IsNullOrWhiteSpace(CycleSignature) &&
            !string.IsNullOrWhiteSpace(PhaseSignature) &&
            !string.IsNullOrWhiteSpace(ParticipationSignature) &&
            HasActorsOperationalReadyObservation &&
            HasSceneTransitionCompletedObservation &&
            HasIntroStageStatusObservation;

        public bool IsGameplayInteractionReady =>
            HasCanonicalPayload &&
            RouteKind == SceneRouteKind.Gameplay &&
            ActorsOperationalReady &&
            SceneTransitionCompleted &&
            IntroStageStatus != GameplayInteractionIntroStageStatus.Unknown &&
            ReadinessReasonKind == GameplayInteractionReadinessReasonKind.Ready;

        public static GameplayInteractionReadinessSnapshot Empty =>
            new(
                string.Empty,
                default,
                SceneRouteKind.Unspecified,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                GameplayInteractionIntroStageStatus.Unknown,
                hasActorsOperationalReadyObservation: false,
                actorsOperationalReady: false,
                hasSceneTransitionCompletedObservation: false,
                sceneTransitionCompleted: false,
                hasIntroStageStatusObservation: false,
                GameplayInteractionReadinessReasonKind.Unknown,
                "waiting_for_gameplay_interaction_ready");

        private static string BuildInteractionSignature(
            string sessionSignature,
            SceneRouteId routeId,
            SceneRouteKind routeKind,
            string sceneName,
            string actorSetRef,
            string cycleSignature,
            string phaseSignature,
            string participationSignature,
            GameplayInteractionIntroStageStatus introStageStatus)
        {
            return $"session:{sessionSignature}|route:{routeId.Value}|rk:{routeKind}|scene:{sceneName}|actors:{actorSetRef}|cycle:{cycleSignature}|phase:{phaseSignature}|participation:{participationSignature}|intro:{introStageStatus}";
        }

        private static string Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private static string NormalizeReason(string? reason, GameplayInteractionReadinessReasonKind reasonKind)
        {
            if (!string.IsNullOrWhiteSpace(reason))
            {
                return reason.Trim();
            }

            return reasonKind switch
            {
                GameplayInteractionReadinessReasonKind.Ready => "ActorsOperationalReady+SceneTransitionCompleted+IntroStageDone",
                GameplayInteractionReadinessReasonKind.WaitingForActorsOperationalReady => "waiting_for_actors_operational_ready",
                GameplayInteractionReadinessReasonKind.WaitingForSceneTransitionCompleted => "waiting_for_scene_transition_completed",
                GameplayInteractionReadinessReasonKind.WaitingForIntroStageDone => "waiting_for_intro_stage_done",
                GameplayInteractionReadinessReasonKind.ContextMismatch => "interaction_context_mismatch",
                _ => "waiting_for_gameplay_interaction_ready"
            };
        }
    }

    public interface IGameplayInteractionReadinessService : IDisposable
    {
        bool IsGameplayInteractionReady { get; }
        event Action<GameplayInteractionReadinessSnapshot> Changed;
        bool TryGetCurrent(out GameplayInteractionReadinessSnapshot snapshot);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayInteractionReadinessService : IGameplayInteractionReadinessService
    {
        private readonly IActorsGameplayOperationalReadinessService _actorsOperationalReadinessService;
        private readonly ISessionIntegrationInputModeEmitter _inputModeEmitter;
        private readonly object _sync = new();

        private readonly EventBinding<SceneTransitionStartedEvent> _sceneTransitionStartedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _sceneTransitionCompletedBinding;
        private readonly EventBinding<IntroStageCompletedEvent> _introStageCompletedBinding;
        private readonly EventBinding<SessionTransitionPhaseLocalEntryReadyEvent> _phaseLocalEntryReadyBinding;
        private readonly EventBinding<SessionTransitionGameplayInputModeCommandEvent> _gameplayInputModeCommandBinding;

        private Action<ActorsGameplayOperationalReadinessSnapshot> _actorsReadinessChangedHandler;
        private ActorsGameplayOperationalReadinessSnapshot _currentActorsSnapshot;
        private SceneTransitionContext _currentSceneTransitionContext;
        private IntroStageCompletedEvent _currentIntroStageCompletedEvent;
        private PhaseEntryIdentity _activePhaseEntryIdentity;
        private SessionTransitionPhaseLocalEntryReadyEvent _activePhaseLocalEntryReadyEvent;

        private bool _hasActorsSnapshot;
        private bool _hasSceneTransitionCompleted;
        private bool _hasIntroStageStatus;
        private bool _hasActivePhaseEntryIdentity;
        private bool _hasActivePhaseLocalEntryReadyEvent;
        private bool _disposed;
        private bool _hasLastPublishedSnapshot;
        private GameplayInteractionReadinessSnapshot _lastPublishedSnapshot;
        private string _lastBridgedGameplayInputModeCommandSignature = string.Empty;

        public GameplayInteractionReadinessService(
            IActorsGameplayOperationalReadinessService actorsOperationalReadinessService,
            ISessionIntegrationInputModeEmitter inputModeEmitter)
        {
            _actorsOperationalReadinessService = actorsOperationalReadinessService ?? throw new ArgumentNullException(nameof(actorsOperationalReadinessService));
            _inputModeEmitter = inputModeEmitter ?? throw new ArgumentNullException(nameof(inputModeEmitter));

            _sceneTransitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(OnSceneTransitionStarted);
            _sceneTransitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);
            _introStageCompletedBinding = new EventBinding<IntroStageCompletedEvent>(OnIntroStageCompleted);
            _phaseLocalEntryReadyBinding = new EventBinding<SessionTransitionPhaseLocalEntryReadyEvent>(OnPhaseLocalEntryReady);
            _gameplayInputModeCommandBinding = new EventBinding<SessionTransitionGameplayInputModeCommandEvent>(OnGameplayInputModeCommand);

            EventBus<SceneTransitionStartedEvent>.Register(_sceneTransitionStartedBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_sceneTransitionCompletedBinding);
            EventBus<IntroStageCompletedEvent>.Register(_introStageCompletedBinding);
            EventBus<SessionTransitionPhaseLocalEntryReadyEvent>.Register(_phaseLocalEntryReadyBinding);
            EventBus<SessionTransitionGameplayInputModeCommandEvent>.Register(_gameplayInputModeCommandBinding);

            _actorsReadinessChangedHandler = OnActorsReadinessChanged;
            _actorsOperationalReadinessService.Changed += _actorsReadinessChangedHandler;

            if (_actorsOperationalReadinessService.TryGetCurrent(out var currentActorsSnapshot))
            {
                lock (_sync)
                {
                    _currentActorsSnapshot = currentActorsSnapshot;
                    _hasActorsSnapshot = currentActorsSnapshot.HasCurrentContext;
                }
            }

            DebugUtility.LogVerbose<GameplayInteractionReadinessService>(
                "[OBS][SessionIntegration][GameplayInteractionReady] GameplayInteractionReadinessService registrado.",
                DebugUtility.Colors.Info);

            PublishSnapshot(force: true);
        }

        public event Action<GameplayInteractionReadinessSnapshot> Changed;

        public bool IsGameplayInteractionReady
        {
            get
            {
                return TryGetCurrent(out var snapshot) && snapshot.IsGameplayInteractionReady;
            }
        }

        public bool TryGetCurrent(out GameplayInteractionReadinessSnapshot snapshot)
        {
            lock (_sync)
            {
                if (TryBuildSnapshotLocked(out snapshot))
                {
                    return true;
                }

                snapshot = GameplayInteractionReadinessSnapshot.Empty;
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            EventBus<SceneTransitionStartedEvent>.Unregister(_sceneTransitionStartedBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_sceneTransitionCompletedBinding);
            EventBus<IntroStageCompletedEvent>.Unregister(_introStageCompletedBinding);
            EventBus<SessionTransitionPhaseLocalEntryReadyEvent>.Unregister(_phaseLocalEntryReadyBinding);
            EventBus<SessionTransitionGameplayInputModeCommandEvent>.Unregister(_gameplayInputModeCommandBinding);

            if (_actorsReadinessChangedHandler != null)
            {
                _actorsOperationalReadinessService.Changed -= _actorsReadinessChangedHandler;
            }
        }

        private void OnActorsReadinessChanged(ActorsGameplayOperationalReadinessSnapshot snapshot)
        {
            if (_disposed)
            {
                return;
            }

            lock (_sync)
            {
                if (_hasIntroStageStatus &&
                    snapshot.HasCurrentContext &&
                    !ActorsSnapshotMatchesIntroStage(_currentIntroStageCompletedEvent, snapshot))
                {
                    DebugUtility.LogVerbose<GameplayInteractionReadinessService>(
                        $"[OBS][SessionIntegration][GameplayInteractionReady] Actors snapshot ignored reason='intro_stage_context_mismatch' introSessionSignature='{_currentIntroStageCompletedEvent.Session.SessionSignature}' introPhaseRuntimeSignature='{_currentIntroStageCompletedEvent.Session.PhaseRuntimeSignature}' introEntrySignature='{_currentIntroStageCompletedEvent.Session.EntrySignature}' actorSessionSignature='{snapshot.PhaseLocalEntryReadyEvent.SessionSignature}' actorPhaseRuntimeSignature='{snapshot.PhaseLocalEntryReadyEvent.PhaseSignature}' actorEntrySignature='{snapshot.PhaseLocalEntryReadyEvent.EntrySignature}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                _currentActorsSnapshot = snapshot;
                _hasActorsSnapshot = snapshot.HasCurrentContext;
            }

            PublishSnapshot(force: false);
        }

        private void OnSceneTransitionStarted(SceneTransitionStartedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            lock (_sync)
            {
                _currentSceneTransitionContext = default;
                _currentIntroStageCompletedEvent = default;
                _activePhaseEntryIdentity = default;
                _activePhaseLocalEntryReadyEvent = default;
                _hasActorsSnapshot = false;
                _hasSceneTransitionCompleted = false;
                _hasIntroStageStatus = false;
                _hasActivePhaseEntryIdentity = false;
                _hasActivePhaseLocalEntryReadyEvent = false;
                _currentActorsSnapshot = default;
                _lastBridgedGameplayInputModeCommandSignature = string.Empty;
                _lastPublishedSnapshot = GameplayInteractionReadinessSnapshot.Empty;
                _hasLastPublishedSnapshot = false;
            }

            DebugUtility.LogVerbose<GameplayInteractionReadinessService>(
                $"[OBS][SessionIntegration][GameplayInteractionReady] SceneTransitionStarted reset routeKind='{evt.context.RouteKind}' routeId='{evt.context.RouteId}' reason='{evt.context.Reason}'.",
                DebugUtility.Colors.Info);

            PublishSnapshot(force: true);
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            lock (_sync)
            {
                if (evt.context.RouteKind != SceneRouteKind.Gameplay)
                {
                    _currentSceneTransitionContext = evt.context;
                    _hasSceneTransitionCompleted = false;
                }
                else
                {
                    _currentSceneTransitionContext = evt.context;
                    _hasSceneTransitionCompleted = true;
                }
            }

            DebugUtility.LogVerbose<GameplayInteractionReadinessService>(
                $"[OBS][SessionIntegration][GameplayInteractionReady] SceneTransitionCompleted observed routeKind='{evt.context.RouteKind}' routeId='{evt.context.RouteId}' scene='{evt.context.TargetActiveScene}' reason='{evt.context.Reason}'.",
                DebugUtility.Colors.Info);

            PublishSnapshot(force: false);
        }

        private void OnIntroStageCompleted(IntroStageCompletedEvent evt)
        {
            if (_disposed || !evt.Session.IsValid)
            {
                return;
            }

            bool clearedStaleActorsSnapshot = false;
            lock (_sync)
            {
                _currentIntroStageCompletedEvent = evt;
                _hasIntroStageStatus = true;

                if (_hasActorsSnapshot &&
                    !ActorsSnapshotMatchesIntroStage(evt, _currentActorsSnapshot))
                {
                    _currentActorsSnapshot = default;
                    _hasActorsSnapshot = false;
                    clearedStaleActorsSnapshot = true;
                }
            }

            DebugUtility.LogVerbose<GameplayInteractionReadinessService>(
                $"[OBS][SessionIntegration][GameplayInteractionReady] IntroStageStatus observed sessionSignature='{evt.Session.SessionSignature}' phaseRuntimeSignature='{evt.Session.PhaseRuntimeSignature}' entrySignature='{evt.Session.EntrySignature}' wasSkipped='{evt.WasSkipped.ToString().ToLowerInvariant()}' reason='{evt.Reason}' clearedStaleActorsSnapshot='{clearedStaleActorsSnapshot.ToString().ToLowerInvariant()}'.",
                DebugUtility.Colors.Info);

            PublishSnapshot(force: false);
        }

        private void OnPhaseLocalEntryReady(SessionTransitionPhaseLocalEntryReadyEvent evt)
        {
            if (_disposed || !evt.HasCanonicalPayload || !evt.PhaseEntryIdentity.IsValid)
            {
                return;
            }

            bool hasGameplaySceneTransitionContext;
            bool sceneTransitionMatches = true;
            bool hasActiveIdentity;
            PhaseEntryIdentity currentActiveIdentity;

            lock (_sync)
            {
                hasGameplaySceneTransitionContext =
                    _currentSceneTransitionContext.RouteKind == SceneRouteKind.Gameplay &&
                    _currentSceneTransitionContext.RouteId.IsValid;

                if (hasGameplaySceneTransitionContext)
                {
                    sceneTransitionMatches =
                        _currentSceneTransitionContext.RouteId == evt.RouteId &&
                        string.Equals(_currentSceneTransitionContext.TargetActiveScene, evt.SceneName, StringComparison.Ordinal);
                }

                hasActiveIdentity = _hasActivePhaseEntryIdentity;
                currentActiveIdentity = _activePhaseEntryIdentity;

                if (hasActiveIdentity && currentActiveIdentity.IsValid && currentActiveIdentity != evt.PhaseEntryIdentity)
                {
                    sceneTransitionMatches = false;
                }

                if (sceneTransitionMatches)
                {
                    _activePhaseEntryIdentity = evt.PhaseEntryIdentity;
                    _activePhaseLocalEntryReadyEvent = evt;
                    _hasActivePhaseEntryIdentity = true;
                    _hasActivePhaseLocalEntryReadyEvent = true;
                    _lastBridgedGameplayInputModeCommandSignature = string.Empty;
                }
            }

            if (!sceneTransitionMatches)
            {
                DebugUtility.LogVerbose<GameplayInteractionReadinessService>(
                    $"[OBS][SessionIntegration][InputModes] ActivePhaseEntryIdentity ignored reason='foreign_or_stale_phase_local_entry_ready' expectedPhaseEntryIdentity='{currentActiveIdentity}' receivedPhaseEntryIdentity='{evt.PhaseEntryIdentity}' expectedSessionSignature='{_activePhaseLocalEntryReadyEvent.SessionSignature}' receivedSessionSignature='{evt.SessionSignature}' expectedPhaseSignature='{_activePhaseLocalEntryReadyEvent.PhaseSignature}' receivedPhaseSignature='{evt.PhaseSignature}' expectedCycleSignature='{_activePhaseLocalEntryReadyEvent.CycleSignature}' receivedCycleSignature='{evt.CycleSignature}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.LogVerbose<GameplayInteractionReadinessService>(
                $"[OBS][SessionIntegration][InputModes] ActivePhaseEntryIdentity opened source='{evt.Source}' phaseEntryIdentity='{evt.PhaseEntryIdentity}' sessionSignature='{evt.SessionSignature}' phaseSignature='{evt.PhaseSignature}' cycleSignature='{evt.CycleSignature}'.",
                DebugUtility.Colors.Info);
        }

        private void OnGameplayInputModeCommand(SessionTransitionGameplayInputModeCommandEvent evt)
        {
            if (_disposed || !evt.IsValid)
            {
                return;
            }

            bool shouldBridge;
            bool hasActiveIdentity;
            PhaseEntryIdentity activeIdentity;
            SessionTransitionPhaseLocalEntryReadyEvent activeEntryReadyEvent;

            lock (_sync)
            {
                hasActiveIdentity = _hasActivePhaseEntryIdentity;
                activeIdentity = _activePhaseEntryIdentity;
                activeEntryReadyEvent = _activePhaseLocalEntryReadyEvent;

                if (!hasActiveIdentity || !activeIdentity.IsValid || !_hasActivePhaseLocalEntryReadyEvent || !activeEntryReadyEvent.HasCanonicalPayload)
                {
                    shouldBridge = false;
                }
                else if (evt.PhaseEntryIdentity != activeIdentity ||
                         !string.Equals(evt.SessionSignature, activeEntryReadyEvent.SessionSignature, StringComparison.Ordinal) ||
                         !string.Equals(evt.PhaseSignature, activeEntryReadyEvent.PhaseSignature, StringComparison.Ordinal) ||
                         !string.Equals(evt.CycleSignature, activeEntryReadyEvent.CycleSignature, StringComparison.Ordinal))
                {
                    shouldBridge = false;
                }
                else if (string.Equals(_lastBridgedGameplayInputModeCommandSignature, evt.CycleSignature, StringComparison.Ordinal))
                {
                    shouldBridge = false;
                }
                else
                {
                    _lastBridgedGameplayInputModeCommandSignature = evt.CycleSignature;
                    shouldBridge = true;
                }
            }

            if (!hasActiveIdentity || !activeIdentity.IsValid)
            {
                DebugUtility.LogVerbose<GameplayInteractionReadinessService>(
                    $"[OBS][SessionIntegration][InputModes] GameplayInputModeCommand ignored reason='no_active_phase_entry_identity' commandPhaseEntryIdentity='{evt.PhaseEntryIdentity}' sessionSignature='{evt.SessionSignature}' phaseSignature='{evt.PhaseSignature}' cycleSignature='{evt.CycleSignature}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!_hasActivePhaseLocalEntryReadyEvent || !activeEntryReadyEvent.HasCanonicalPayload)
            {
                DebugUtility.LogVerbose<GameplayInteractionReadinessService>(
                    $"[OBS][SessionIntegration][InputModes] GameplayInputModeCommand ignored reason='no_active_phase_local_entry_ready_event' commandPhaseEntryIdentity='{evt.PhaseEntryIdentity}' sessionSignature='{evt.SessionSignature}' phaseSignature='{evt.PhaseSignature}' cycleSignature='{evt.CycleSignature}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (evt.PhaseEntryIdentity != activeIdentity ||
                !string.Equals(evt.SessionSignature, activeEntryReadyEvent.SessionSignature, StringComparison.Ordinal) ||
                !string.Equals(evt.PhaseSignature, activeEntryReadyEvent.PhaseSignature, StringComparison.Ordinal) ||
                !string.Equals(evt.CycleSignature, activeEntryReadyEvent.CycleSignature, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<GameplayInteractionReadinessService>(
                    $"[OBS][SessionIntegration][InputModes] GameplayInputModeCommand ignored reason='foreign_or_stale_command' expectedPhaseEntryIdentity='{activeIdentity}' receivedPhaseEntryIdentity='{evt.PhaseEntryIdentity}' expectedSessionSignature='{activeEntryReadyEvent.SessionSignature}' receivedSessionSignature='{evt.SessionSignature}' expectedPhaseSignature='{activeEntryReadyEvent.PhaseSignature}' receivedPhaseSignature='{evt.PhaseSignature}' expectedCycleSignature='{activeEntryReadyEvent.CycleSignature}' receivedCycleSignature='{evt.CycleSignature}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!shouldBridge)
            {
                DebugUtility.LogVerbose<GameplayInteractionReadinessService>(
                    $"[OBS][SessionIntegration][InputModes] GameplayInputModeCommand deduped phaseEntryIdentity='{evt.PhaseEntryIdentity}' cycleSignature='{evt.CycleSignature}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.Log(typeof(GameplayInteractionReadinessService),
                $"[OBS][SessionIntegration][InputModes] InputModeRequested kind='Gameplay' source='SessionTransition' reason='{evt.Reason}' phaseEntryIdentity='{evt.PhaseEntryIdentity}' sessionSignature='{evt.SessionSignature}' phaseSignature='{evt.PhaseSignature}' cycleSignature='{evt.CycleSignature}'.",
                DebugUtility.Colors.Info);

            _inputModeEmitter.RequestGameplayInputMode(
                evt.Reason,
                "SessionTransition",
                evt.ContextSignature);
        }

        private void PublishSnapshot(bool force)
        {
            GameplayInteractionReadinessSnapshot snapshot;

            lock (_sync)
            {
                if (!TryBuildSnapshotLocked(out snapshot))
                {
                    snapshot = GameplayInteractionReadinessSnapshot.Empty;
                }
                else if (!force && _hasLastPublishedSnapshot && SnapshotsEqual(_lastPublishedSnapshot, snapshot))
                {
                    return;
                }

                _lastPublishedSnapshot = snapshot;
                _hasLastPublishedSnapshot = true;
            }

            Changed?.Invoke(snapshot);
        }

        private bool TryBuildSnapshotLocked(out GameplayInteractionReadinessSnapshot snapshot)
        {
            snapshot = GameplayInteractionReadinessSnapshot.Empty;

            if (!_hasSceneTransitionCompleted && !_hasIntroStageStatus && !_hasActorsSnapshot)
            {
                return false;
            }

            GameplayInteractionIntroStageStatus introStageStatus = ResolveIntroStageStatus(_currentIntroStageCompletedEvent);
            bool hasIntroStageStatus = _hasIntroStageStatus;
            bool hasSceneTransitionCompleted = _hasSceneTransitionCompleted;
            bool hasActorsOperationalReady = _hasActorsSnapshot;
            bool actorsOperationalReady = _hasActorsSnapshot && _currentActorsSnapshot.IsGameplayOperationalReady;
            bool sceneTransitionCompleted = hasSceneTransitionCompleted &&
                _currentSceneTransitionContext.RouteKind == SceneRouteKind.Gameplay;

            SceneRouteId routeId = hasActorsOperationalReady
                ? _currentActorsSnapshot.PhaseLocalEntryReadyEvent.RouteId
                : (_currentSceneTransitionContext.RouteId.IsValid ? _currentSceneTransitionContext.RouteId : default);
            SceneRouteKind routeKind = hasActorsOperationalReady
                ? _currentActorsSnapshot.PhaseLocalEntryReadyEvent.RouteKind
                : _currentSceneTransitionContext.RouteKind;
            string sceneName = hasActorsOperationalReady
                ? _currentActorsSnapshot.PhaseLocalEntryReadyEvent.SceneName
                : _currentSceneTransitionContext.TargetActiveScene;
            string actorSetRef = hasActorsOperationalReady
                ? _currentActorsSnapshot.PhaseLocalEntryReadyEvent.ActorSetRef
                : string.Empty;
            string cycleSignature = hasActorsOperationalReady
                ? _currentActorsSnapshot.CycleCompletedEvent.CycleSignature
                : string.Empty;
            string sessionSignature = hasActorsOperationalReady
                ? _currentActorsSnapshot.PhaseLocalEntryReadyEvent.SessionSignature
                : string.Empty;
            string phaseSignature = hasActorsOperationalReady
                ? _currentActorsSnapshot.PhaseLocalEntryReadyEvent.PhaseSignature
                : string.Empty;
            string participationSignature = hasActorsOperationalReady
                ? _currentActorsSnapshot.PhaseLocalEntryReadyEvent.ParticipationSignature
                : string.Empty;

            if (!hasActorsOperationalReady || !actorsOperationalReady)
            {
                snapshot = new GameplayInteractionReadinessSnapshot(
                    sessionSignature,
                    routeId,
                    routeKind,
                    sceneName,
                    actorSetRef,
                    cycleSignature,
                    phaseSignature,
                    participationSignature,
                    introStageStatus,
                    hasActorsOperationalReady,
                    actorsOperationalReady,
                    hasSceneTransitionCompleted,
                    sceneTransitionCompleted,
                    hasIntroStageStatus,
                    GameplayInteractionReadinessReasonKind.WaitingForActorsOperationalReady,
                    "waiting_for_actors_operational_ready");
                return true;
            }

            if (!hasSceneTransitionCompleted)
            {
                snapshot = new GameplayInteractionReadinessSnapshot(
                    sessionSignature,
                    routeId,
                    routeKind,
                    sceneName,
                    actorSetRef,
                    cycleSignature,
                    phaseSignature,
                    participationSignature,
                    introStageStatus,
                    hasActorsOperationalReady,
                    actorsOperationalReady,
                    hasSceneTransitionCompleted,
                    sceneTransitionCompleted,
                    hasIntroStageStatus,
                    GameplayInteractionReadinessReasonKind.WaitingForSceneTransitionCompleted,
                    "waiting_for_scene_transition_completed");
                return true;
            }

            if (!hasIntroStageStatus)
            {
                snapshot = new GameplayInteractionReadinessSnapshot(
                    sessionSignature,
                    routeId,
                    routeKind,
                    sceneName,
                    actorSetRef,
                    cycleSignature,
                    phaseSignature,
                    participationSignature,
                    introStageStatus,
                    hasActorsOperationalReady,
                    actorsOperationalReady,
                    hasSceneTransitionCompleted,
                    sceneTransitionCompleted,
                    hasIntroStageStatus,
                    GameplayInteractionReadinessReasonKind.WaitingForIntroStageDone,
                    "waiting_for_intro_stage_done");
                return true;
            }

            bool contextMatches = _currentSceneTransitionContext.RouteKind == SceneRouteKind.Gameplay &&
                                  _currentSceneTransitionContext.RouteId.IsValid &&
                                  _currentSceneTransitionContext.RouteId == _currentActorsSnapshot.PhaseLocalEntryReadyEvent.RouteId &&
                                  string.Equals(_currentSceneTransitionContext.TargetActiveScene, _currentActorsSnapshot.PhaseLocalEntryReadyEvent.SceneName, StringComparison.Ordinal) &&
                                  ActorsSnapshotMatchesIntroStage(_currentIntroStageCompletedEvent, _currentActorsSnapshot);

            if (!contextMatches)
            {
                snapshot = new GameplayInteractionReadinessSnapshot(
                    sessionSignature,
                    routeId,
                    routeKind,
                    sceneName,
                    actorSetRef,
                    cycleSignature,
                    phaseSignature,
                    participationSignature,
                    introStageStatus,
                    hasActorsOperationalReady,
                    actorsOperationalReady,
                    hasSceneTransitionCompleted,
                    sceneTransitionCompleted,
                    hasIntroStageStatus,
                    GameplayInteractionReadinessReasonKind.ContextMismatch,
                    "interaction_context_mismatch");
                return true;
            }

            snapshot = new GameplayInteractionReadinessSnapshot(
                sessionSignature,
                routeId,
                routeKind,
                sceneName,
                actorSetRef,
                cycleSignature,
                phaseSignature,
                participationSignature,
                introStageStatus,
                hasActorsOperationalReady,
                actorsOperationalReady,
                hasSceneTransitionCompleted,
                sceneTransitionCompleted,
                hasIntroStageStatus,
                GameplayInteractionReadinessReasonKind.Ready,
                "ActorsOperationalReady+SceneTransitionCompleted+IntroStageDone");
            return true;
        }

        private static bool SnapshotsEqual(GameplayInteractionReadinessSnapshot left, GameplayInteractionReadinessSnapshot right)
        {
            return string.Equals(left.SessionSignature, right.SessionSignature, StringComparison.Ordinal) &&
                   left.RouteId == right.RouteId &&
                   left.RouteKind == right.RouteKind &&
                   string.Equals(left.SceneName, right.SceneName, StringComparison.Ordinal) &&
                   string.Equals(left.ActorSetRef, right.ActorSetRef, StringComparison.Ordinal) &&
                   string.Equals(left.CycleSignature, right.CycleSignature, StringComparison.Ordinal) &&
                   string.Equals(left.PhaseSignature, right.PhaseSignature, StringComparison.Ordinal) &&
                   string.Equals(left.ParticipationSignature, right.ParticipationSignature, StringComparison.Ordinal) &&
                   left.IntroStageStatus == right.IntroStageStatus &&
                   left.HasActorsOperationalReadyObservation == right.HasActorsOperationalReadyObservation &&
                   left.ActorsOperationalReady == right.ActorsOperationalReady &&
                   left.HasSceneTransitionCompletedObservation == right.HasSceneTransitionCompletedObservation &&
                   left.SceneTransitionCompleted == right.SceneTransitionCompleted &&
                   left.HasIntroStageStatusObservation == right.HasIntroStageStatusObservation &&
                   left.ReadinessReasonKind == right.ReadinessReasonKind &&
                   string.Equals(left.ReadinessReason, right.ReadinessReason, StringComparison.Ordinal);
        }

        private static bool ActorsSnapshotMatchesIntroStage(
            IntroStageCompletedEvent introStageCompletedEvent,
            ActorsGameplayOperationalReadinessSnapshot actorsSnapshot)
        {
            if (!introStageCompletedEvent.Session.IsValid || !actorsSnapshot.HasCurrentContext)
            {
                return false;
            }

            return string.Equals(introStageCompletedEvent.Session.SessionSignature, actorsSnapshot.PhaseLocalEntryReadyEvent.SessionSignature, StringComparison.Ordinal) &&
                   string.Equals(introStageCompletedEvent.Session.PhaseRuntimeSignature, actorsSnapshot.PhaseLocalEntryReadyEvent.PhaseSignature, StringComparison.Ordinal);
        }

        private static GameplayInteractionIntroStageStatus ResolveIntroStageStatus(IntroStageCompletedEvent evt)
        {
            if (!evt.Session.IsValid)
            {
                return GameplayInteractionIntroStageStatus.Unknown;
            }

            if (!evt.WasSkipped)
            {
                return GameplayInteractionIntroStageStatus.Completed;
            }

            if (string.Equals(evt.Reason, PhaseFlowSignalVocabulary.NoContentReason, StringComparison.OrdinalIgnoreCase))
            {
                return GameplayInteractionIntroStageStatus.NoContent;
            }

            return GameplayInteractionIntroStageStatus.Skipped;
        }
    }
}
