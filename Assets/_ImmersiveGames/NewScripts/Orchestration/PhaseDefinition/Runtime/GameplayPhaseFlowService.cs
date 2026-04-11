using System;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayPhaseFlowService :
        IGameplaySessionContextService,
        IGameplayPhaseRuntimeService,
        IGameplayPhasePlayerParticipationService,
        IGameplayPhaseRulesObjectivesService,
        IGameplayPhaseInitialStateService,
        IDisposable
    {
        private readonly object _sync = new();
        private readonly EventBinding<PhaseDefinitionSelectedEvent> _phaseSelectedBinding;
        private readonly EventBinding<PhaseContentAppliedEvent> _phaseContentAppliedBinding;
        private readonly EventBinding<IntroStageEntryEvent> _introStageEntryBinding;
        private PhaseDefinitionSelectedEvent _currentSelectionEvent;
        private PhaseDefinitionSelectedEvent _lastSelectionEvent;
        private GameplaySessionContextSnapshot _currentSessionContext = GameplaySessionContextSnapshot.Empty;
        private GameplaySessionContextSnapshot _lastSessionContext = GameplaySessionContextSnapshot.Empty;
        private GameplayPhaseRuntimeSnapshot _currentPhaseRuntime = GameplayPhaseRuntimeSnapshot.Empty;
        private GameplayPhaseRuntimeSnapshot _lastPhaseRuntime = GameplayPhaseRuntimeSnapshot.Empty;
        private GameplayPhasePlayerParticipationSnapshot _currentPhasePlayers = GameplayPhasePlayerParticipationSnapshot.Empty;
        private GameplayPhasePlayerParticipationSnapshot _lastPhasePlayers = GameplayPhasePlayerParticipationSnapshot.Empty;
        private GameplayPhaseRulesObjectivesSnapshot _currentPhaseRulesObjectives = GameplayPhaseRulesObjectivesSnapshot.Empty;
        private GameplayPhaseRulesObjectivesSnapshot _lastPhaseRulesObjectives = GameplayPhaseRulesObjectivesSnapshot.Empty;
        private GameplayPhaseInitialStateSnapshot _currentPhaseInitialState = GameplayPhaseInitialStateSnapshot.Empty;
        private GameplayPhaseInitialStateSnapshot _lastPhaseInitialState = GameplayPhaseInitialStateSnapshot.Empty;
        private bool _disposed;

        public GameplayPhaseFlowService()
        {
            RegisterSelfInGlobalDi();

            _phaseSelectedBinding = new EventBinding<PhaseDefinitionSelectedEvent>(OnPhaseDefinitionSelected);
            _phaseContentAppliedBinding = new EventBinding<PhaseContentAppliedEvent>(OnPhaseContentApplied);
            _introStageEntryBinding = new EventBinding<IntroStageEntryEvent>(OnIntroStageEntry);
            EventBus<PhaseDefinitionSelectedEvent>.Register(_phaseSelectedBinding);
            EventBus<PhaseContentAppliedEvent>.Register(_phaseContentAppliedBinding);
            EventBus<IntroStageEntryEvent>.Register(_introStageEntryBinding);

            DebugUtility.LogVerbose<GameplayPhaseFlowService>(
                "[OBS][GameplaySessionFlow][PhaseDefinition] GameplayPhaseFlowService registrado como owner explicito phase-side.",
                DebugUtility.Colors.Info);
        }

        public GameplaySessionContextSnapshot Current => GetSnapshot(_currentSessionContext);
        public void PublishPhaseDefinitionSelected(PhaseDefinitionSelectedEvent evt)
        {
            if (!evt.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    "[FATAL][H1][GameplaySessionFlow] Invalid PhaseDefinitionSelectedEvent requested for phase-owned publication.");
            }

            SyncRestartContextFromPhaseSelection(evt);
            EventBus<PhaseDefinitionSelectedEvent>.Raise(evt);
        }
        public PhaseDefinitionSelectedEvent PublishPhaseDefinitionSelected(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId macroRouteId,
            SceneRouteDefinitionAsset routeRef,
            int selectionVersion,
            string reason)
        {
            PhaseDefinitionSelectedEvent evt = new PhaseDefinitionSelectedEvent(
                phaseDefinitionRef,
                macroRouteId,
                routeRef,
                selectionVersion,
                reason);

            if (!evt.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    "[FATAL][H1][GameplaySessionFlow] Invalid phase selection requested for phase-owned publication.");
            }

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionSelectedCanonical rail='phase' owner='GameplayPhaseFlowService' phaseId='{evt.PhaseId}' routeId='{evt.MacroRouteId}' v='{evt.SelectionVersion}' reason='{evt.Reason}' signature='{evt.SelectionSignature}'.",
                DebugUtility.Colors.Info);

            SyncRestartContextFromPhaseSelection(evt);
            EventBus<PhaseDefinitionSelectedEvent>.Raise(evt);
            return evt;
        }

        public PhaseDefinitionSelectedEvent PublishPhaseDefinitionSelected(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId macroRouteId,
            SceneRouteDefinitionAsset routeRef,
            string reason)
        {
            int selectionVersion = ResolveNextSelectionVersionOrFail(phaseDefinitionRef, macroRouteId, routeRef, reason);
            return PublishPhaseDefinitionSelected(phaseDefinitionRef, macroRouteId, routeRef, selectionVersion, reason);
        }

        public bool TryGetCurrent(out GameplaySessionContextSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _currentSessionContext;
                return _currentSessionContext.IsValid;
            }
        }

        public bool TryGetLast(out GameplaySessionContextSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _lastSessionContext;
                return _lastSessionContext.IsValid;
            }
        }

        public GameplaySessionContextSnapshot Update(GameplaySessionContextSnapshot snapshot)
        {
            return UpdateSessionContext(snapshot, source: "manual_update");
        }

        public GameplaySessionContextSnapshot UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            return UpdateSessionContext(GameplaySessionContextSnapshot.FromPhaseDefinitionSelectedEvent(evt), source: "phase_selected_event");
        }

        public void Clear(string reason = null)
        {
            string normalizedReason = Normalize(reason);

            lock (_sync)
            {
                _currentSelectionEvent = default;
                _currentSessionContext = GameplaySessionContextSnapshot.Empty;
                _currentPhaseRuntime = GameplayPhaseRuntimeSnapshot.Empty;
                _currentPhasePlayers = GameplayPhasePlayerParticipationSnapshot.Empty;
                _currentPhaseRulesObjectives = GameplayPhaseRulesObjectivesSnapshot.Empty;
                _currentPhaseInitialState = GameplayPhaseInitialStateSnapshot.Empty;
            }

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseFlowCleared reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
        }

        public GameplayPhaseRuntimeSnapshot CurrentPhaseRuntime => GetSnapshot(_currentPhaseRuntime);
        GameplayPhaseRuntimeSnapshot IGameplayPhaseRuntimeService.Current => CurrentPhaseRuntime;

        public bool TryGetCurrent(out GameplayPhaseRuntimeSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _currentPhaseRuntime;
                return _currentPhaseRuntime.IsValid;
            }
        }

        public bool TryGetLast(out GameplayPhaseRuntimeSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _lastPhaseRuntime;
                return _lastPhaseRuntime.IsValid;
            }
        }

        public GameplayPhaseRuntimeSnapshot Update(GameplayPhaseRuntimeSnapshot snapshot)
        {
            return UpdatePhaseRuntime(snapshot, source: "manual_update");
        }

        GameplayPhaseRuntimeSnapshot IGameplayPhaseRuntimeService.UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            return UpdatePhaseRuntime(GameplayPhaseRuntimeSnapshot.FromPhaseDefinitionSelectedEvent(evt), source: "phase_selected_event");
        }

        public GameplayPhasePlayerParticipationSnapshot CurrentPhasePlayers => GetSnapshot(_currentPhasePlayers);
        GameplayPhasePlayerParticipationSnapshot IGameplayPhasePlayerParticipationService.Current => CurrentPhasePlayers;

        public bool TryGetCurrent(out GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _currentPhasePlayers;
                return _currentPhasePlayers.IsValid;
            }
        }

        public bool TryGetLast(out GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _lastPhasePlayers;
                return _lastPhasePlayers.IsValid;
            }
        }

        public GameplayPhasePlayerParticipationSnapshot Update(GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            return UpdatePhasePlayers(snapshot, source: "manual_update");
        }

        GameplayPhasePlayerParticipationSnapshot IGameplayPhasePlayerParticipationService.UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            return UpdatePhasePlayers(GameplayPhasePlayerParticipationSnapshot.FromPhaseDefinitionSelectedEvent(evt), source: "phase_selected_event");
        }

        public GameplayPhaseRulesObjectivesSnapshot CurrentPhaseRulesObjectives => GetSnapshot(_currentPhaseRulesObjectives);
        GameplayPhaseRulesObjectivesSnapshot IGameplayPhaseRulesObjectivesService.Current => CurrentPhaseRulesObjectives;

        public bool TryGetCurrent(out GameplayPhaseRulesObjectivesSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _currentPhaseRulesObjectives;
                return _currentPhaseRulesObjectives.IsValid;
            }
        }

        public bool TryGetLast(out GameplayPhaseRulesObjectivesSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _lastPhaseRulesObjectives;
                return _lastPhaseRulesObjectives.IsValid;
            }
        }

        public GameplayPhaseRulesObjectivesSnapshot Update(GameplayPhaseRulesObjectivesSnapshot snapshot)
        {
            return UpdateRulesObjectives(snapshot, source: "manual_update");
        }

        GameplayPhaseRulesObjectivesSnapshot IGameplayPhaseRulesObjectivesService.UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            return UpdateRulesObjectives(GameplayPhaseRulesObjectivesSnapshot.FromPhaseDefinitionSelectedEvent(evt), source: "phase_selected_event");
        }

        public GameplayPhaseInitialStateSnapshot CurrentPhaseInitialState => GetSnapshot(_currentPhaseInitialState);
        GameplayPhaseInitialStateSnapshot IGameplayPhaseInitialStateService.Current => CurrentPhaseInitialState;

        public bool TryGetCurrent(out GameplayPhaseInitialStateSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _currentPhaseInitialState;
                return _currentPhaseInitialState.IsValid;
            }
        }

        public bool TryGetLast(out GameplayPhaseInitialStateSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _lastPhaseInitialState;
                return _lastPhaseInitialState.IsValid;
            }
        }

        public GameplayPhaseInitialStateSnapshot Update(GameplayPhaseInitialStateSnapshot snapshot)
        {
            return UpdateInitialState(snapshot, source: "manual_update");
        }

        GameplayPhaseInitialStateSnapshot IGameplayPhaseInitialStateService.UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            return UpdateInitialState(GameplayPhaseInitialStateSnapshot.FromPhaseDefinitionSelectedEvent(evt), source: "phase_selected_event");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<PhaseDefinitionSelectedEvent>.Unregister(_phaseSelectedBinding);
            EventBus<PhaseContentAppliedEvent>.Unregister(_phaseContentAppliedBinding);
            EventBus<IntroStageEntryEvent>.Unregister(_introStageEntryBinding);
        }

        private void RegisterSelfInGlobalDi()
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    "[FATAL][H1][GameplaySessionFlow] DependencyManager.Provider unavailable while registering phase flow owner.");
            }

            RegisterOwnerBinding<GameplayPhaseFlowService>(this);
            RegisterOwnerBinding<IGameplaySessionContextService>(this);
            RegisterOwnerBinding<IGameplayPhaseRuntimeService>(this);
            RegisterOwnerBinding<IGameplayPhasePlayerParticipationService>(this);
            RegisterOwnerBinding<IGameplayPhaseRulesObjectivesService>(this);
            RegisterOwnerBinding<IGameplayPhaseInitialStateService>(this);
        }

        private static void RegisterOwnerBinding<T>(T instance)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                if (!ReferenceEquals(existing, instance))
                {
                    HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                        $"[FATAL][H1][GameplaySessionFlow] Conflicting global binding for '{typeof(T).Name}' while registering phase flow owner.");
                }

                return;
            }

            DependencyManager.Provider.RegisterGlobal<T>(instance);
        }

        private void OnPhaseDefinitionSelected(PhaseDefinitionSelectedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            if (!evt.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    "[FATAL][H1][GameplaySessionFlow] Invalid PhaseDefinitionSelectedEvent received by explicit phase owner.");
            }

            GameplaySessionContextSnapshot sessionContext = GameplaySessionContextSnapshot.FromPhaseDefinitionSelectedEvent(evt);

            lock (_sync)
            {
                _lastSelectionEvent = _currentSelectionEvent;
                _currentSelectionEvent = evt;
                _lastSessionContext = _currentSessionContext;
                _currentSessionContext = sessionContext;
                _lastPhaseRuntime = _currentPhaseRuntime;
                _currentPhaseRuntime = GameplayPhaseRuntimeSnapshot.Empty;
                _lastPhasePlayers = _currentPhasePlayers;
                _currentPhasePlayers = GameplayPhasePlayerParticipationSnapshot.Empty;
                _lastPhaseRulesObjectives = _currentPhaseRulesObjectives;
                _currentPhaseRulesObjectives = GameplayPhaseRulesObjectivesSnapshot.Empty;
                _lastPhaseInitialState = _currentPhaseInitialState;
                _currentPhaseInitialState = GameplayPhaseInitialStateSnapshot.Empty;
            }

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseSelectedConsumed owner='GameplayPhaseFlowService' phaseId='{evt.PhaseId}' routeId='{evt.MacroRouteId}' v='{evt.SelectionVersion}' reason='{evt.Reason}' sessionSignature='{sessionContext.SessionSignature}'.",
                DebugUtility.Colors.Info);
        }

        private void OnPhaseContentApplied(PhaseContentAppliedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            if (evt.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    "[FATAL][H1][GameplaySessionFlow] Invalid PhaseContentAppliedEvent received by explicit phase owner.");
            }

            PhaseDefinitionSelectedEvent selectionEvent;
            GameplaySessionContextSnapshot sessionContext;

            lock (_sync)
            {
                selectionEvent = _currentSelectionEvent;
                sessionContext = _currentSessionContext;
            }

            if (!selectionEvent.IsValid || selectionEvent.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    "[FATAL][H1][GameplaySessionFlow] PhaseContentAppliedEvent received before a valid phase selection was cached.");
            }

            if (!ReferenceEquals(selectionEvent.PhaseDefinitionRef, evt.PhaseDefinitionRef))
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    $"[FATAL][H1][GameplaySessionFlow] PhaseContentAppliedEvent mismatch with cached selection. cachedPhase='{selectionEvent.PhaseId}' appliedPhase='{evt.PhaseDefinitionRef.PhaseId}' source='{evt.Source}'.");
            }

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentApplied owner='GameplayPhaseFlowService' phaseId='{selectionEvent.PhaseId}' routeId='{selectionEvent.MacroRouteId}' v='{selectionEvent.SelectionVersion}' reason='{selectionEvent.Reason}' sessionSignature='{sessionContext.SessionSignature}' source='{evt.Source}' activeScene='{evt.ActiveSceneName}'.",
                DebugUtility.Colors.Info);

            GameplayPhaseRuntimeSnapshot phaseRuntime = GameplayPhaseRuntimeSnapshot.FromPhaseDefinitionSelectedEvent(selectionEvent);
            GameplayPhasePlayerParticipationSnapshot phasePlayers = GameplayPhasePlayerParticipationSnapshot.FromPhaseDefinitionSelectedEvent(selectionEvent);
            GameplayPhaseRulesObjectivesSnapshot phaseRulesObjectives = GameplayPhaseRulesObjectivesSnapshot.FromPhaseDefinitionSelectedEvent(selectionEvent);

            UpdatePhaseRuntime(phaseRuntime, source: "phase_content_applied");
            UpdatePhasePlayers(phasePlayers, source: "phase_content_applied");
            UpdateRulesObjectives(phaseRulesObjectives, source: "phase_content_applied");

            GameplayPhaseInitialStateSnapshot phaseInitialState = GameplayPhaseInitialStateSnapshot.FromPhaseDefinitionSelectedEvent(selectionEvent);
            UpdateInitialState(phaseInitialState, source: "phase_content_applied");

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDerivationCompleted owner='GameplayPhaseFlowService' phaseSignature='{phaseRuntime.PhaseRuntimeSignature}' sessionSignature='{sessionContext.SessionSignature}' playersSignature='{phasePlayers.ParticipationSignature}' rulesSignature='{phaseRulesObjectives.RulesSignature}' initialStateSignature='{phaseInitialState.InitialStateSignature}' source='{evt.Source}'.",
                DebugUtility.Colors.Success);

            EventBus<GameplayPhaseRuntimeMaterializedEvent>.Raise(
                new GameplayPhaseRuntimeMaterializedEvent(phaseRuntime, evt.Source));

            if (string.Equals(evt.Source, "GameplaySessionFlow", StringComparison.Ordinal))
            {
                PhaseDefinitionSelectedEvent currentSelectionEvent;

                lock (_sync)
                {
                    currentSelectionEvent = _currentSelectionEvent;
                }

                if (currentSelectionEvent.IsValid && currentSelectionEvent.PhaseDefinitionRef != null)
                {
                    string sessionSignature = BuildPhaseSignature(currentSelectionEvent);
                    string localContentId = currentSelectionEvent.PhaseDefinitionRef.BuildCanonicalIntroContentId();
                    IntroStageSession introSession = phaseRuntime.CreateIntroStageSession(
                        localContentId,
                        currentSelectionEvent.Reason,
                        currentSelectionEvent.SelectionVersion,
                        sessionSignature);

                    DebugUtility.Log<GameplayPhaseFlowService>(
                        $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseIntroStageQueued owner='GameplayPhaseFlowService' phaseId='{currentSelectionEvent.PhaseId}' routeId='{currentSelectionEvent.MacroRouteId}' v='{currentSelectionEvent.SelectionVersion}' reason='{currentSelectionEvent.Reason}' phaseSignature='{phaseRuntime.PhaseRuntimeSignature}' hasIntroStage='{introSession.HasIntroStage}' hasRunResultStage='{introSession.HasRunResultStage}'.",
                        DebugUtility.Colors.Info);

                    EventBus<IntroStageEntryEvent>.Raise(new IntroStageEntryEvent(
                        introSession,
                        evt.Source,
                        currentSelectionEvent.MacroRouteRef != null ? currentSelectionEvent.MacroRouteRef.RouteKind : default));
                }
            }
        }

        private void OnIntroStageEntry(IntroStageEntryEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            if (!evt.Session.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    "[FATAL][H1][GameplaySessionFlow] Invalid IntroStageEntryEvent received by explicit phase owner.");
            }

            PhaseDefinitionSelectedEvent selectionEvent;
            GameplaySessionContextSnapshot sessionContext;

            lock (_sync)
            {
                selectionEvent = _currentSelectionEvent;
                sessionContext = _currentSessionContext;
            }

            if (!selectionEvent.IsValid || selectionEvent.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    "[FATAL][H1][GameplaySessionFlow] IntroStageEntryEvent received before a valid phase selection was cached.");
            }

            if (!ReferenceEquals(selectionEvent.PhaseDefinitionRef, evt.Session.PhaseDefinitionRef) ||
                selectionEvent.SelectionVersion != evt.Session.SelectionVersion)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                $"[FATAL][H1][GameplaySessionFlow] IntroStageEntryEvent mismatch with cached selection. cachedPhase='{selectionEvent.PhaseId}' cachedVersion='{selectionEvent.SelectionVersion}' entryPhase='{(evt.Session.PhaseDefinitionRef != null ? evt.Session.PhaseDefinitionRef.PhaseId.Value : "<none>")}' entryVersion='{evt.Session.SelectionVersion}'.");
            }

            if (!_currentPhaseRuntime.IsValid || !_currentPhasePlayers.IsValid || !_currentPhaseRulesObjectives.IsValid || !_currentPhaseInitialState.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    $"[FATAL][H1][GameplaySessionFlow] IntroStageEntryEvent received before phase derivation completed. phaseId='{selectionEvent.PhaseId}' routeId='{selectionEvent.MacroRouteId}' v='{selectionEvent.SelectionVersion}' reason='{selectionEvent.Reason}'.");
            }

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseIntroStageReady owner='GameplayPhaseFlowService' phaseId='{selectionEvent.PhaseId}' routeId='{selectionEvent.MacroRouteId}' v='{selectionEvent.SelectionVersion}' reason='{selectionEvent.Reason}' sessionSignature='{sessionContext.SessionSignature}' entrySource='{evt.Source}' phaseSignature='{_currentPhaseRuntime.PhaseRuntimeSignature}'.",
                DebugUtility.Colors.Info);
        }

        private GameplaySessionContextSnapshot UpdateSessionContext(GameplaySessionContextSnapshot snapshot, string source)
        {
            lock (_sync)
            {
                if (!snapshot.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                        "[FATAL][H1][GameplaySessionFlow] Invalid gameplay session context snapshot received by explicit phase owner.");
                }

                _lastSessionContext = _currentSessionContext;
                _currentSessionContext = snapshot;
            }

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][SessionContext] SessionContextUpdated owner='GameplayPhaseFlowService' source='{source}' phaseId='{snapshot.PhaseId}' routeId='{snapshot.MacroRouteId}' v='{snapshot.SelectionVersion}' reason='{snapshot.Reason}' signature='{snapshot.SessionSignature}'.",
                DebugUtility.Colors.Info);

            return snapshot;
        }

        private GameplayPhaseRuntimeSnapshot UpdatePhaseRuntime(GameplayPhaseRuntimeSnapshot snapshot, string source)
        {
            lock (_sync)
            {
                if (!snapshot.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                        "[FATAL][H1][GameplaySessionFlow] Invalid gameplay phase runtime snapshot received by explicit phase owner.");
                }

                _lastPhaseRuntime = _currentPhaseRuntime;
                _currentPhaseRuntime = snapshot;
            }

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][PhaseRuntime] PhaseRuntimeUpdated owner='GameplayPhaseFlowService' source='{source}' sessionSignature='{snapshot.SessionContext.SessionSignature}' phaseRef='{(snapshot.PhaseDefinitionRef != null ? snapshot.PhaseDefinitionRef.name : "<none>")}' contentCount='{snapshot.ContentEntryCount}' playerCount='{snapshot.PlayerEntryCount}' ruleCount='{snapshot.RuleEntryCount}' objectiveCount='{snapshot.ObjectiveEntryCount}' initialStateCount='{snapshot.InitialStateEntryCount}' runResultStage='{snapshot.HasRunResultStage}' phaseSignature='{snapshot.PhaseRuntimeSignature}'.",
                DebugUtility.Colors.Info);

            return snapshot;
        }

        private GameplayPhasePlayerParticipationSnapshot UpdatePhasePlayers(GameplayPhasePlayerParticipationSnapshot snapshot, string source)
        {
            lock (_sync)
            {
                if (!snapshot.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                        "[FATAL][H1][GameplaySessionFlow] Invalid gameplay phase player participation snapshot received by explicit phase owner.");
                }

                _lastPhasePlayers = _currentPhasePlayers;
                _currentPhasePlayers = snapshot;
            }

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][Players] PlayersUpdated owner='GameplayPhaseFlowService' source='{source}' phaseSignature='{snapshot.PhaseRuntime.PhaseRuntimeSignature}' participationMode='{snapshot.ParticipationMode}' participantCount='{snapshot.ParticipatingPlayerCount}' primaryId='{snapshot.PrimaryParticipantId}' participationSignature='{snapshot.ParticipationSignature}'.",
                DebugUtility.Colors.Info);

            return snapshot;
        }

        private GameplayPhaseRulesObjectivesSnapshot UpdateRulesObjectives(GameplayPhaseRulesObjectivesSnapshot snapshot, string source)
        {
            lock (_sync)
            {
                if (!snapshot.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                        "[FATAL][H1][GameplaySessionFlow] Invalid gameplay phase rules/objectives snapshot received by explicit phase owner.");
                }

                _lastPhaseRulesObjectives = _currentPhaseRulesObjectives;
                _currentPhaseRulesObjectives = snapshot;
            }

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][RulesObjectives] RulesObjectivesUpdated owner='GameplayPhaseFlowService' source='{source}' phaseSignature='{snapshot.PhaseRuntime.PhaseRuntimeSignature}' ruleCount='{snapshot.RuleEntryCount}' objectiveCount='{snapshot.ObjectiveEntryCount}' primaryObjectiveId='{snapshot.PrimaryObjectiveId}' rulesSignature='{snapshot.RulesSignature}' objectivesSignature='{snapshot.ObjectivesSignature}'.",
                DebugUtility.Colors.Info);

            return snapshot;
        }

        private GameplayPhaseInitialStateSnapshot UpdateInitialState(GameplayPhaseInitialStateSnapshot snapshot, string source)
        {
            lock (_sync)
            {
                if (!snapshot.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                        "[FATAL][H1][GameplaySessionFlow] Invalid gameplay phase initial state snapshot received by explicit phase owner.");
                }

                _lastPhaseInitialState = _currentPhaseInitialState;
                _currentPhaseInitialState = snapshot;
            }

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][InitialState] InitialStateUpdated owner='GameplayPhaseFlowService' source='{source}' phaseSignature='{snapshot.PhaseRuntime.PhaseRuntimeSignature}' seedSource='{snapshot.SeedSource}' rulesSignature='{snapshot.RulesObjectives.RulesSignature}' objectivesSignature='{snapshot.RulesObjectives.ObjectivesSignature}' initialStateSignature='{snapshot.InitialStateSignature}'.",
                DebugUtility.Colors.Info);

            return snapshot;
        }

        private static TSnapshot GetSnapshot<TSnapshot>(TSnapshot snapshot)
        {
            return snapshot;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<null>" : value.Trim();
        }

        private static string BuildPhaseSignature(PhaseDefinitionSelectedEvent selectionEvent)
        {
            string phaseName = selectionEvent.PhaseDefinitionRef != null ? selectionEvent.PhaseDefinitionRef.name : "<null>";
            string normalizedReason = string.IsNullOrWhiteSpace(selectionEvent.Reason) ? string.Empty : selectionEvent.Reason.Trim();
            return $"phase:{phaseName}|route:{selectionEvent.MacroRouteId}|reason:{normalizedReason}";
        }

        private static void SyncRestartContextFromPhaseSelection(PhaseDefinitionSelectedEvent evt)
        {
            if (!TryResolveRestartContextService(out var restartContextService))
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    "[FATAL][H1][GameplaySessionFlow] RestartContextService ausente durante publicacao phase-owned de PhaseDefinitionSelectedEvent.");
            }

            GameplayStartSnapshot gameplayStartSnapshot = new GameplayStartSnapshot(
                evt.PhaseDefinitionRef,
                evt.MacroRouteId,
                evt.MacroRouteRef,
                evt.PhaseDefinitionRef.BuildCanonicalIntroContentId(),
                evt.Reason,
                evt.SelectionVersion,
                evt.SelectionSignature);

            restartContextService.RegisterGameplayStart(gameplayStartSnapshot);

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] GameplayStartSnapshotLinked owner='GameplayPhaseFlowService' phaseId='{evt.PhaseId}' routeId='{evt.MacroRouteId}' v='{evt.SelectionVersion}' reason='{evt.Reason}' signature='{gameplayStartSnapshot.PhaseSignature}'.",
                DebugUtility.Colors.Info);
        }

        private static int ResolveNextSelectionVersionOrFail(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId macroRouteId,
            SceneRouteDefinitionAsset routeRef,
            string reason)
        {
            if (phaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    $"[FATAL][H1][GameplaySessionFlow] Cannot resolve selection version for null phaseDefinitionRef. routeId='{macroRouteId}' reason='{reason}'.");
            }

            if (routeRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    $"[FATAL][H1][GameplaySessionFlow] Cannot resolve selection version for null routeRef. routeId='{macroRouteId}' phaseRef='{phaseDefinitionRef.name}' reason='{reason}'.");
            }

            if (TryResolveRestartContextService(out var restartContextService) &&
                restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot lastSnapshot) &&
                lastSnapshot.IsValid)
            {
                return Math.Max(lastSnapshot.SelectionVersion + 1, 1);
            }

            return 1;
        }

        private static bool TryResolveRestartContextService(out IRestartContextService restartContextService)
        {
            restartContextService = null;

            if (DependencyManager.Provider == null)
            {
                return false;
            }

            return DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out restartContextService) &&
                   restartContextService != null;
        }
    }
}





