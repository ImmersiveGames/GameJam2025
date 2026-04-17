using System;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.SessionIntegration.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SessionTransition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayPhaseFlowService :
        IGameplaySessionContextService,
        IGameplayPhaseRuntimeService,
        IDisposable
    {
        private readonly object _sync = new();
        private readonly EventBinding<PhaseDefinitionSelectedEvent> _phaseSelectedBinding;
        private readonly EventBinding<PhaseContentAppliedEvent> _phaseContentAppliedBinding;
        private readonly EventBinding<PhaseResetCompletedEvent> _phaseResetCompletedBinding;
        private readonly EventBinding<IntroStageEntryEvent> _introStageEntryBinding;
        private readonly EventBinding<SessionTransitionPhaseLocalEntryReadyEvent> _sessionTransitionPhaseLocalEntryReadyBinding;
        private PhaseDefinitionSelectedEvent _currentSelectionEvent;
        private PhaseDefinitionSelectedEvent _lastSelectionEvent;
        private GameplaySessionContextSnapshot _currentSessionContext = GameplaySessionContextSnapshot.Empty;
        private GameplaySessionContextSnapshot _lastSessionContext = GameplaySessionContextSnapshot.Empty;
        private GameplayPhaseRuntimeSnapshot _currentPhaseRuntime = GameplayPhaseRuntimeSnapshot.Empty;
        private GameplayPhaseRuntimeSnapshot _lastPhaseRuntime = GameplayPhaseRuntimeSnapshot.Empty;
        private readonly IGameplayParticipationFlowService _participationFlowService;
        private int _phaseLocalEntrySequence;
        private bool _disposed;

        public GameplayPhaseFlowService()
        {
            RegisterSelfInGlobalDi();
            _participationFlowService = ResolveParticipationFlowServiceOrFail();

            _phaseSelectedBinding = new EventBinding<PhaseDefinitionSelectedEvent>(OnPhaseDefinitionSelected);
            _phaseContentAppliedBinding = new EventBinding<PhaseContentAppliedEvent>(OnPhaseContentApplied);
            _phaseResetCompletedBinding = new EventBinding<PhaseResetCompletedEvent>(OnPhaseResetCompleted);
            _introStageEntryBinding = new EventBinding<IntroStageEntryEvent>(OnIntroStageEntry);
            _sessionTransitionPhaseLocalEntryReadyBinding = new EventBinding<SessionTransitionPhaseLocalEntryReadyEvent>(OnSessionTransitionPhaseLocalEntryReady);
            EventBus<PhaseDefinitionSelectedEvent>.Register(_phaseSelectedBinding);
            EventBus<PhaseContentAppliedEvent>.Register(_phaseContentAppliedBinding);
            EventBus<PhaseResetCompletedEvent>.Register(_phaseResetCompletedBinding);
            EventBus<IntroStageEntryEvent>.Register(_introStageEntryBinding);
            EventBus<SessionTransitionPhaseLocalEntryReadyEvent>.Register(_sessionTransitionPhaseLocalEntryReadyBinding);

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
            }

            _participationFlowService.Clear(normalizedReason);

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

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<PhaseDefinitionSelectedEvent>.Unregister(_phaseSelectedBinding);
            EventBus<PhaseContentAppliedEvent>.Unregister(_phaseContentAppliedBinding);
            EventBus<PhaseResetCompletedEvent>.Unregister(_phaseResetCompletedBinding);
            EventBus<IntroStageEntryEvent>.Unregister(_introStageEntryBinding);
            EventBus<SessionTransitionPhaseLocalEntryReadyEvent>.Unregister(_sessionTransitionPhaseLocalEntryReadyBinding);
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
        }

        private static IGameplayParticipationFlowService ResolveParticipationFlowServiceOrFail()
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    "[FATAL][H1][GameplaySessionFlow] DependencyManager.Provider unavailable while resolving participation owner.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayParticipationFlowService>(out var service) || service == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    "[FATAL][H1][GameplaySessionFlow] IGameplayParticipationFlowService missing while wiring GameplayPhaseFlowService.");
            }

            return service;
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
            }

            _participationFlowService.Clear("phase_selected");
            SyncRestartContextFromPhaseSelection(evt);

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

            HandlePhaseRearm(
                selectionEvent,
                sessionContext,
                source: evt.Source,
                activeSceneName: evt.ActiveSceneName,
                operationLabel: "PhaseContentApplied",
                shouldQueueIntro: string.Equals(evt.Source, "GameplaySessionFlow", StringComparison.Ordinal));
        }

        private void OnPhaseResetCompleted(PhaseResetCompletedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            if (!evt.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    "[FATAL][H1][GameplaySessionFlow] Invalid PhaseResetCompletedEvent received by explicit phase owner.");
            }

            if (!TryResolveRestartContextService(out var restartContextService) ||
                restartContextService == null ||
                !restartContextService.TryGetCurrent(out GameplayStartSnapshot restartSnapshot) ||
                !restartSnapshot.IsValid ||
                !restartSnapshot.HasPhaseDefinitionRef ||
                restartSnapshot.PhaseDefinitionRef == null ||
                !ReferenceEquals(restartSnapshot.PhaseDefinitionRef, evt.ResetContext.PhaseDefinitionRef) ||
                restartSnapshot.MacroRouteId != evt.ResetContext.MacroRouteId)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    $"[FATAL][H1][GameplaySessionFlow] PhaseResetCompletedEvent received with stale or mismatched restart snapshot. phaseRef='{evt.ResetContext.PhaseDefinitionRef?.name ?? "<none>"}' routeId='{evt.ResetContext.MacroRouteId}' resetSignature='{evt.ResetContext.ResetSignature}' reason='{evt.Reason}'.");
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
                    "[FATAL][H1][GameplaySessionFlow] PhaseResetCompletedEvent received before a valid phase selection was cached.");
            }

            if (!ReferenceEquals(selectionEvent.PhaseDefinitionRef, evt.ResetContext.PhaseDefinitionRef) ||
                selectionEvent.MacroRouteId != evt.ResetContext.MacroRouteId)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    $"[FATAL][H1][GameplaySessionFlow] PhaseResetCompletedEvent mismatch with cached selection. cachedPhase='{selectionEvent.PhaseId}' cachedVersion='{selectionEvent.SelectionVersion}' resetPhase='{evt.ResetContext.PhaseDefinitionRef?.PhaseId.Value ?? "<none>"}' resetRouteId='{evt.ResetContext.MacroRouteId}'.");
            }

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseResetRearmConsumed owner='GameplayPhaseFlowService' phaseId='{selectionEvent.PhaseId}' routeId='{selectionEvent.MacroRouteId}' v='{selectionEvent.SelectionVersion}' reason='{selectionEvent.Reason}' resetSignature='{evt.ResetContext.ResetSignature}' source='{evt.Source}'.",
                DebugUtility.Colors.Info);

            HandlePhaseRearm(
                selectionEvent,
                sessionContext,
                source: evt.Source,
                activeSceneName: string.Empty,
                operationLabel: "PhaseResetRearm",
                shouldQueueIntro: true);
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

            bool hasParticipationSnapshot = _participationFlowService.TryGetCurrent(out ParticipationSnapshot participationSnapshot);
            bool hasParticipationReadiness = _participationFlowService.TryGetCurrentReadiness(out ParticipationReadinessSnapshot participationReadiness);
            if (!_currentPhaseRuntime.IsValid ||
                !hasParticipationSnapshot ||
                !hasParticipationReadiness ||
                !participationSnapshot.IsValid ||
                !participationReadiness.IsValid ||
                !participationReadiness.CanEnterGameplay)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    $"[FATAL][H1][GameplaySessionFlow] IntroStageEntryEvent received before participation readiness was semantically available. phaseId='{selectionEvent.PhaseId}' routeId='{selectionEvent.MacroRouteId}' v='{selectionEvent.SelectionVersion}' reason='{selectionEvent.Reason}' readinessState='{participationReadiness.State}' readinessCanEnter='{participationReadiness.CanEnterGameplay}'.");
            }

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseIntroStageReady owner='GameplayPhaseFlowService' phaseId='{selectionEvent.PhaseId}' routeId='{selectionEvent.MacroRouteId}' v='{selectionEvent.SelectionVersion}' entrySeq='{evt.Session.PhaseLocalEntrySequence}' reason='{selectionEvent.Reason}' sessionSignature='{sessionContext.SessionSignature}' entrySource='{evt.Source}' entrySignature='{evt.Session.EntrySignature}' phaseSignature='{_currentPhaseRuntime.PhaseRuntimeSignature}' participationSignature='{participationSnapshot.Signature}' participationReadiness='{participationReadiness.State}' canEnterGameplay='{participationReadiness.CanEnterGameplay}'.",
                DebugUtility.Colors.Info);
        }

        private void OnSessionTransitionPhaseLocalEntryReady(SessionTransitionPhaseLocalEntryReadyEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            if (!evt.IsValid || !evt.IsPhaseLocalEntry)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    "[FATAL][H1][GameplaySessionFlow] Invalid SessionTransitionPhaseLocalEntryReadyEvent received by explicit phase owner.");
            }

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseLocalEntryReady owner='GameplayPhaseFlowService' phaseLocalEntryReady='{evt.Plan.EmitsPhaseLocalEntryReady}' continuation='{evt.Plan.ResolvedContinuation}' phaseIntent='{evt.Plan.Composition.PhaseIntent}' worldResetIntent='{evt.Plan.Composition.WorldResetIntent}' continuityShape='{evt.Plan.Composition.ContinuityShape}' reconstructionShape='{evt.Plan.Composition.ReconstructionShape}' composition='{evt.Plan.Composition}' execution='{evt.Plan.Execution}' reason='{evt.Plan.Reason}' nextState='{evt.Plan.NextState}' source='{evt.Source}'.",
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
                $"[OBS][GameplaySessionFlow][PhaseRuntime] PhaseRuntimeUpdated owner='GameplayPhaseFlowService' source='{source}' sessionSignature='{snapshot.SessionContext.SessionSignature}' phaseRef='{(snapshot.PhaseDefinitionRef != null ? snapshot.PhaseDefinitionRef.name : "<none>")}' contentCount='{snapshot.ContentEntryCount}' playerCount='{snapshot.PlayerEntryCount}' phaseSignature='{snapshot.PhaseRuntimeSignature}'.",
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
            if (!selectionEvent.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    "[FATAL][H1][GameplaySessionFlow] Invalid selection event received while building phase signature.");
            }

            // Usa a assinatura canonica da selecao para que a reentrada da mesma phase
            // gere uma identidade nova sem abrir um segundo rail de IntroStage.
            return selectionEvent.SelectionSignature;
        }

        private string ResolveNextPhaseLocalEntrySequenceSignature(PhaseDefinitionSelectedEvent selectionEvent, string source, int phaseLocalEntrySequence)
        {
            if (!selectionEvent.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    "[FATAL][H1][GameplaySessionFlow] Invalid selection event received while building intro stage entry signature.");
            }

            string normalizedSource = string.IsNullOrWhiteSpace(source) ? "<unknown-source>" : source.Trim();
            return $"{selectionEvent.SelectionSignature}|entry:{phaseLocalEntrySequence}|source:{normalizedSource}";
        }

        private int ResolveNextPhaseLocalEntrySequence()
        {
            lock (_sync)
            {
                _phaseLocalEntrySequence += 1;
                return _phaseLocalEntrySequence;
            }
        }

        private void HandlePhaseRearm(
            PhaseDefinitionSelectedEvent selectionEvent,
            GameplaySessionContextSnapshot sessionContext,
            string source,
            string activeSceneName,
            string operationLabel,
            bool shouldQueueIntro)
        {
            GameplayPhaseRuntimeSnapshot phaseRuntime = GameplayPhaseRuntimeSnapshot.FromPhaseDefinitionSelectedEvent(selectionEvent);
            ParticipationSnapshot participationSnapshot = _participationFlowService.UpdateFromPhaseDefinitionSelectedEvent(selectionEvent);
            if (!participationSnapshot.Readiness.IsValid || !participationSnapshot.Readiness.CanEnterGameplay)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                    $"[FATAL][H1][GameplaySessionFlow] Participation snapshot produced an invalid semantic readiness state during rearm. phaseId='{selectionEvent.PhaseId}' routeId='{selectionEvent.MacroRouteId}' v='{selectionEvent.SelectionVersion}' readinessState='{participationSnapshot.Readiness.State}' readinessCanEnter='{participationSnapshot.Readiness.CanEnterGameplay}'.");
            }

            UpdatePhaseRuntime(phaseRuntime, source: operationLabel);

            int phaseLocalEntrySequence = ResolveNextPhaseLocalEntrySequence();
            string entrySignature = ResolveNextPhaseLocalEntrySequenceSignature(selectionEvent, source, phaseLocalEntrySequence);

            DebugUtility.Log<GameplayPhaseFlowService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] {operationLabel} owner='GameplayPhaseFlowService' phaseSignature='{phaseRuntime.PhaseRuntimeSignature}' sessionSignature='{sessionContext.SessionSignature}' participationSignature='{participationSnapshot.Signature}' participationReadiness='{participationSnapshot.Readiness.State}' canEnterGameplay='{participationSnapshot.Readiness.CanEnterGameplay}' source='{source}' activeScene='{activeSceneName}'.",
                DebugUtility.Colors.Success);

            EventBus<GameplayPhaseRuntimeMaterializedEvent>.Raise(
                new GameplayPhaseRuntimeMaterializedEvent(phaseRuntime, source, phaseLocalEntrySequence, entrySignature));

            if (shouldQueueIntro)
            {
                string sessionSignature = BuildPhaseSignature(selectionEvent);
                string localContentId = PhaseDefinitionId.BuildCanonicalIntroContentId(selectionEvent.PhaseDefinitionRef.PhaseId);
                IntroStageSession introSession = phaseRuntime.CreateIntroStageSession(
                    localContentId,
                    selectionEvent.Reason,
                    selectionEvent.SelectionVersion,
                    phaseLocalEntrySequence,
                    sessionSignature,
                    entrySignature);

                DebugUtility.Log<GameplayPhaseFlowService>(
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseIntroStageQueued owner='GameplayPhaseFlowService' phaseId='{selectionEvent.PhaseId}' routeId='{selectionEvent.MacroRouteId}' v='{selectionEvent.SelectionVersion}' entrySeq='{phaseLocalEntrySequence}' reason='{selectionEvent.Reason}' phaseSignature='{phaseRuntime.PhaseRuntimeSignature}' entrySignature='{entrySignature}' hasIntroStage='{introSession.HasIntroStage}'.",
                    DebugUtility.Colors.Info);

                EventBus<IntroStageEntryEvent>.Raise(new IntroStageEntryEvent(
                    introSession,
                    source,
                    selectionEvent.MacroRouteRef != null ? selectionEvent.MacroRouteRef.RouteKind : default));
            }
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
                PhaseDefinitionId.BuildCanonicalIntroContentId(evt.PhaseDefinitionRef.PhaseId),
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





