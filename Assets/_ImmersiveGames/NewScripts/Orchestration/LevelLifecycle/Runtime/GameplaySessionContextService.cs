using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplaySessionContextService : IGameplaySessionContextService, IDisposable
    {
        private readonly object _sync = new();
        private readonly EventBinding<LevelSelectedEvent> _levelSelectedBinding;
        private GameplaySessionContextSnapshot _current = GameplaySessionContextSnapshot.Empty;
        private GameplaySessionContextSnapshot _last = GameplaySessionContextSnapshot.Empty;
        private bool _disposed;

        public GameplaySessionContextService()
        {
            _levelSelectedBinding = new EventBinding<LevelSelectedEvent>(OnLevelSelected);
            EventBus<LevelSelectedEvent>.Register(_levelSelectedBinding);

            DebugUtility.LogVerbose<GameplaySessionContextService>(
                "[OBS][GameplaySessionFlow][SessionContext] GameplaySessionContextService registrado como owner do contexto da sessao.");
        }

        public GameplaySessionContextSnapshot Current
        {
            get
            {
                lock (_sync)
                {
                    return _current;
                }
            }
        }

        public GameplaySessionContextSnapshot UpdateFromLevelSelectedEvent(LevelSelectedEvent evt)
        {
            return Update(GameplaySessionContextSnapshot.FromLevelSelectedEvent(evt));
        }

        public GameplaySessionContextSnapshot Update(GameplaySessionContextSnapshot snapshot)
        {
            lock (_sync)
            {
                if (!snapshot.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplaySessionContextService),
                        "[FATAL][H1][GameplaySessionFlow] Invalid gameplay session context snapshot received.");
                }

                _current = snapshot;
                _last = snapshot;

                DebugUtility.Log<GameplaySessionContextService>(
                    $"[OBS][GameplaySessionFlow][SessionContext] SessionContextUpdated routeId='{snapshot.MacroRouteId}' routeRef='{snapshot.MacroRouteRef.name}' v='{snapshot.SelectionVersion}' reason='{snapshot.Reason}' signature='{snapshot.SessionSignature}'.",
                    DebugUtility.Colors.Info);

                return _current;
            }
        }

        public bool TryGetCurrent(out GameplaySessionContextSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _current;
                return _current.IsValid;
            }
        }

        public bool TryGetLast(out GameplaySessionContextSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _last;
                return _last.IsValid;
            }
        }

        public void Clear(string reason = null)
        {
            int lastSelectionVersion;

            lock (_sync)
            {
                _current = GameplaySessionContextSnapshot.Empty;
                lastSelectionVersion = _last.SelectionVersion;
            }

            DebugUtility.Log<GameplaySessionContextService>(
                $"[OBS][GameplaySessionFlow][SessionContext] SessionContextCleared keepLast='true' lastSelectionV='{lastSelectionVersion}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<LevelSelectedEvent>.Unregister(_levelSelectedBinding);
        }

        private void OnLevelSelected(LevelSelectedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            UpdateFromLevelSelectedEvent(evt);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<null>" : value.Trim();
        }
    }

    public readonly struct GameplayPhaseRuntimeSnapshot
    {
        public static GameplayPhaseRuntimeSnapshot FromLevelSelectedEvent(LevelSelectedEvent evt)
        {
            if (evt.LevelRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseRuntimeSnapshot),
                    "[FATAL][H1][GameplaySessionFlow] LevelSelectedEvent requires a valid levelRef to build the phase runtime snapshot.");
            }

            GameplaySessionContextSnapshot sessionContext = GameplaySessionContextSnapshot.FromLevelSelectedEvent(evt);
            LevelIntroStageSession levelSession = evt.LevelRef.CreateIntroStageSession(
                evt.LocalContentId,
                evt.Reason,
                evt.SelectionVersion,
                evt.LevelSignature);

            return new GameplayPhaseRuntimeSnapshot(sessionContext, levelSession);
        }

        public GameplayPhaseRuntimeSnapshot(
            GameplaySessionContextSnapshot sessionContext,
            LevelIntroStageSession levelSession)
        {
            SessionContext = sessionContext;
            LevelSession = levelSession;
            PhaseRuntimeSignature = BuildPhaseRuntimeSignature(sessionContext, levelSession);
        }

        public GameplaySessionContextSnapshot SessionContext { get; }
        public LevelIntroStageSession LevelSession { get; }
        public string PhaseRuntimeSignature { get; }

        public bool IsValid => SessionContext.IsValid && LevelSession.IsValid;
        public bool HasLevelRef => LevelSession.HasLevelRef;
        public bool HasPhaseRuntimeSignature => !string.IsNullOrWhiteSpace(PhaseRuntimeSignature);

        public static GameplayPhaseRuntimeSnapshot Empty => new(
            GameplaySessionContextSnapshot.Empty,
            LevelIntroStageSession.Empty);

        public override string ToString()
        {
            return $"sessionContext='{SessionContext}', levelSession='{LevelSession}', phaseRuntimeSignature='{(string.IsNullOrWhiteSpace(PhaseRuntimeSignature) ? "<none>" : PhaseRuntimeSignature)}'";
        }

        private static string BuildPhaseRuntimeSignature(
            GameplaySessionContextSnapshot sessionContext,
            LevelIntroStageSession levelSession)
        {
            string sessionSignature = sessionContext.HasSessionSignature ? sessionContext.SessionSignature : "<no-session>";
            string levelSignature = string.IsNullOrWhiteSpace(levelSession.LevelSignature) ? "<no-level>" : levelSession.LevelSignature;
            return $"{sessionSignature}|{levelSignature}";
        }
    }

    public interface IGameplayPhaseRuntimeService
    {
        GameplayPhaseRuntimeSnapshot Current { get; }
        bool TryGetCurrent(out GameplayPhaseRuntimeSnapshot snapshot);
        bool TryGetLast(out GameplayPhaseRuntimeSnapshot snapshot);
        GameplayPhaseRuntimeSnapshot Update(GameplayPhaseRuntimeSnapshot snapshot);
        GameplayPhaseRuntimeSnapshot UpdateFromLevelSelectedEvent(LevelSelectedEvent evt);
        void Clear(string reason = null);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayPhaseRuntimeService : IGameplayPhaseRuntimeService, IDisposable
    {
        private readonly object _sync = new();
        private readonly EventBinding<LevelSelectedEvent> _levelSelectedBinding;
        private GameplayPhaseRuntimeSnapshot _current = GameplayPhaseRuntimeSnapshot.Empty;
        private GameplayPhaseRuntimeSnapshot _last = GameplayPhaseRuntimeSnapshot.Empty;
        private bool _disposed;

        public GameplayPhaseRuntimeService()
        {
            _levelSelectedBinding = new EventBinding<LevelSelectedEvent>(OnLevelSelected);
            EventBus<LevelSelectedEvent>.Register(_levelSelectedBinding);

            DebugUtility.LogVerbose<GameplayPhaseRuntimeService>(
                "[OBS][GameplaySessionFlow][PhaseRuntime] GameplayPhaseRuntimeService registrado como owner do phase / level runtime.");
        }

        public GameplayPhaseRuntimeSnapshot Current
        {
            get
            {
                lock (_sync)
                {
                    return _current;
                }
            }
        }

        public GameplayPhaseRuntimeSnapshot UpdateFromLevelSelectedEvent(LevelSelectedEvent evt)
        {
            return Update(GameplayPhaseRuntimeSnapshot.FromLevelSelectedEvent(evt));
        }

        public GameplayPhaseRuntimeSnapshot Update(GameplayPhaseRuntimeSnapshot snapshot)
        {
            lock (_sync)
            {
                if (!snapshot.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplayPhaseRuntimeService),
                        "[FATAL][H1][GameplaySessionFlow] Invalid gameplay phase runtime snapshot received.");
                }

                _current = snapshot;
                _last = snapshot;

                DebugUtility.Log<GameplayPhaseRuntimeService>(
                    $"[OBS][GameplaySessionFlow][PhaseRuntime] PhaseRuntimeUpdated sessionSignature='{snapshot.SessionContext.SessionSignature}' levelRef='{snapshot.LevelSession.LevelRef.name}' selectionVersion='{snapshot.LevelSession.SelectionVersion}' levelSignature='{snapshot.LevelSession.LevelSignature}' phaseSignature='{snapshot.PhaseRuntimeSignature}'.",
                    DebugUtility.Colors.Info);

                return _current;
            }
        }

        public bool TryGetCurrent(out GameplayPhaseRuntimeSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _current;
                return _current.IsValid;
            }
        }

        public bool TryGetLast(out GameplayPhaseRuntimeSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _last;
                return _last.IsValid;
            }
        }

        public void Clear(string reason = null)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "<null>" : reason.Trim();
            string lastSignature;

            lock (_sync)
            {
                _current = GameplayPhaseRuntimeSnapshot.Empty;
                lastSignature = _last.PhaseRuntimeSignature;
            }

            DebugUtility.Log<GameplayPhaseRuntimeService>(
                $"[OBS][GameplaySessionFlow][PhaseRuntime] PhaseRuntimeCleared keepLast='true' lastPhaseSignature='{Normalize(lastSignature)}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<LevelSelectedEvent>.Unregister(_levelSelectedBinding);
        }

        private void OnLevelSelected(LevelSelectedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            UpdateFromLevelSelectedEvent(evt);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }

    public enum GameplayPhasePlayerParticipationMode
    {
        None = 0,
        SoloCanonical = 1
    }

    public readonly struct GameplayPhasePlayerParticipationSnapshot
    {
        public static GameplayPhasePlayerParticipationSnapshot FromLevelSelectedEvent(LevelSelectedEvent evt)
        {
            return SoloCanonical(GameplayPhaseRuntimeSnapshot.FromLevelSelectedEvent(evt));
        }

        public static GameplayPhasePlayerParticipationSnapshot SoloCanonical(GameplayPhaseRuntimeSnapshot phaseRuntime)
        {
            return new GameplayPhasePlayerParticipationSnapshot(
                phaseRuntime,
                GameplayPhasePlayerParticipationMode.SoloCanonical,
                1,
                "primary_player",
                "Player");
        }

        public GameplayPhasePlayerParticipationSnapshot(
            GameplayPhaseRuntimeSnapshot phaseRuntime,
            GameplayPhasePlayerParticipationMode participationMode,
            int participatingPlayerCount,
            string primaryParticipantId,
            string primaryParticipantLabel)
        {
            PhaseRuntime = phaseRuntime;
            ParticipationMode = participationMode;
            ParticipatingPlayerCount = participatingPlayerCount < 0 ? 0 : participatingPlayerCount;
            PrimaryParticipantId = Normalize(primaryParticipantId);
            PrimaryParticipantLabel = Normalize(primaryParticipantLabel);
            ParticipationSignature = BuildParticipationSignature(
                phaseRuntime,
                participationMode,
                ParticipatingPlayerCount,
                PrimaryParticipantId);
        }

        public GameplayPhaseRuntimeSnapshot PhaseRuntime { get; }
        public GameplayPhasePlayerParticipationMode ParticipationMode { get; }
        public int ParticipatingPlayerCount { get; }
        public string PrimaryParticipantId { get; }
        public string PrimaryParticipantLabel { get; }
        public string ParticipationSignature { get; }

        public bool HasParticipants => ParticipatingPlayerCount > 0;
        public bool HasCanonicalSoloParticipation => ParticipationMode == GameplayPhasePlayerParticipationMode.SoloCanonical && ParticipatingPlayerCount == 1;
        public bool IsValid => PhaseRuntime.IsValid && HasParticipants;

        public static GameplayPhasePlayerParticipationSnapshot Empty => new(
            GameplayPhaseRuntimeSnapshot.Empty,
            GameplayPhasePlayerParticipationMode.None,
            0,
            "<none>",
            "<none>");

        public override string ToString()
        {
            return $"phaseRuntime='{PhaseRuntime}', participationMode='{ParticipationMode}', participantCount='{ParticipatingPlayerCount}', primaryId='{PrimaryParticipantId}', participationSignature='{(string.IsNullOrWhiteSpace(ParticipationSignature) ? "<none>" : ParticipationSignature)}'";
        }

        private static string BuildParticipationSignature(
            GameplayPhaseRuntimeSnapshot phaseRuntime,
            GameplayPhasePlayerParticipationMode participationMode,
            int participatingPlayerCount,
            string primaryParticipantId)
        {
            string phaseSignature = phaseRuntime.HasPhaseRuntimeSignature ? phaseRuntime.PhaseRuntimeSignature : "<no-phase>";
            string mode = participationMode.ToString();
            string participantId = string.IsNullOrWhiteSpace(primaryParticipantId) ? "<none>" : primaryParticipantId;
            return $"{phaseSignature}|players={mode}:{participatingPlayerCount}|primary={participantId}";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }

    public interface IGameplayPhasePlayerParticipationService
    {
        GameplayPhasePlayerParticipationSnapshot Current { get; }
        bool TryGetCurrent(out GameplayPhasePlayerParticipationSnapshot snapshot);
        bool TryGetLast(out GameplayPhasePlayerParticipationSnapshot snapshot);
        GameplayPhasePlayerParticipationSnapshot Update(GameplayPhasePlayerParticipationSnapshot snapshot);
        GameplayPhasePlayerParticipationSnapshot UpdateFromLevelSelectedEvent(LevelSelectedEvent evt);
        void Clear(string reason = null);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayPhasePlayerParticipationService : IGameplayPhasePlayerParticipationService, IDisposable
    {
        private readonly object _sync = new();
        private readonly EventBinding<LevelSelectedEvent> _levelSelectedBinding;
        private GameplayPhasePlayerParticipationSnapshot _current = GameplayPhasePlayerParticipationSnapshot.Empty;
        private GameplayPhasePlayerParticipationSnapshot _last = GameplayPhasePlayerParticipationSnapshot.Empty;
        private bool _disposed;

        public GameplayPhasePlayerParticipationService()
        {
            _levelSelectedBinding = new EventBinding<LevelSelectedEvent>(OnLevelSelected);
            EventBus<LevelSelectedEvent>.Register(_levelSelectedBinding);

            DebugUtility.LogVerbose<GameplayPhasePlayerParticipationService>(
                "[OBS][GameplaySessionFlow][Players] GameplayPhasePlayerParticipationService registrado como owner da participacao de players na fase.");
        }

        public GameplayPhasePlayerParticipationSnapshot Current
        {
            get
            {
                lock (_sync)
                {
                    return _current;
                }
            }
        }

        public GameplayPhasePlayerParticipationSnapshot UpdateFromLevelSelectedEvent(LevelSelectedEvent evt)
        {
            return Update(GameplayPhasePlayerParticipationSnapshot.FromLevelSelectedEvent(evt));
        }

        public GameplayPhasePlayerParticipationSnapshot Update(GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            lock (_sync)
            {
                if (!snapshot.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplayPhasePlayerParticipationService),
                        "[FATAL][H1][GameplaySessionFlow] Invalid gameplay phase player participation snapshot received.");
                }

                _current = snapshot;
                _last = snapshot;

                DebugUtility.Log<GameplayPhasePlayerParticipationService>(
                    $"[OBS][GameplaySessionFlow][Players] PlayersUpdated phaseSignature='{snapshot.PhaseRuntime.PhaseRuntimeSignature}' participationMode='{snapshot.ParticipationMode}' participantCount='{snapshot.ParticipatingPlayerCount}' primaryId='{snapshot.PrimaryParticipantId}' participationSignature='{snapshot.ParticipationSignature}'.",
                    DebugUtility.Colors.Info);

                return _current;
            }
        }

        public bool TryGetCurrent(out GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _current;
                return _current.IsValid;
            }
        }

        public bool TryGetLast(out GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _last;
                return _last.IsValid;
            }
        }

        public void Clear(string reason = null)
        {
            string normalizedReason = Normalize(reason);
            string lastSignature;

            lock (_sync)
            {
                _current = GameplayPhasePlayerParticipationSnapshot.Empty;
                lastSignature = _last.ParticipationSignature;
            }

            DebugUtility.Log<GameplayPhasePlayerParticipationService>(
                $"[OBS][GameplaySessionFlow][Players] PlayersCleared keepLast='true' lastParticipationSignature='{Normalize(lastSignature)}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<LevelSelectedEvent>.Unregister(_levelSelectedBinding);
        }

        private void OnLevelSelected(LevelSelectedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            UpdateFromLevelSelectedEvent(evt);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }

    public static class GameplaySessionFlowSmokeReporter
    {
        public static bool ReportCurrentState(string stage, string reason = null)
        {
            string normalizedStage = Normalize(stage);
            string normalizedReason = Normalize(reason);

            bool hasSession = TryResolveSessionContext(out var sessionContext);
            bool hasPhase = TryResolvePhaseRuntime(out var phaseRuntime);
            bool hasPlayers = TryResolvePhasePlayers(out var players);
            bool hasRulesObjectives = TryResolvePhaseRulesObjectives(out var rulesObjectives);
            bool hasInitialState = TryResolvePhaseInitialState(out var initialState);

            string sessionState = hasSession ? DescribeSessionContext(sessionContext) : "missing";
            string phaseState = hasPhase ? DescribePhaseRuntime(phaseRuntime) : "missing";
            string playersState = hasPlayers ? DescribePhasePlayers(players) : "missing";
            string rulesObjectivesState = hasRulesObjectives ? DescribePhaseRulesObjectives(rulesObjectives) : "missing";
            string initialStateState = hasInitialState ? DescribePhaseInitialState(initialState) : "missing";
            string relation = DescribeSessionPhaseRelation(hasSession, hasPhase, sessionContext, phaseRuntime);
            string playersRelation = DescribePhasePlayersRelation(hasPhase, hasPlayers, phaseRuntime, players);
            string rulesObjectivesRelation = DescribePhaseRulesObjectivesRelation(hasPhase, hasRulesObjectives, phaseRuntime, rulesObjectives);
            string initialStateRelation = DescribePhaseInitialStateRelation(hasPhase, hasRulesObjectives, hasInitialState, phaseRuntime, rulesObjectives, initialState);

            DebugUtility.Log(typeof(GameplaySessionFlowSmokeReporter),
                $"[OBS][GameplaySessionFlow][Smoke] stage='{normalizedStage}' reason='{normalizedReason}' sessionContext='{sessionState}' phaseRuntime='{phaseState}' players='{playersState}' rulesObjectives='{rulesObjectivesState}' initialState='{initialStateState}' relation='{relation}' playersRelation='{playersRelation}' rulesObjectivesRelation='{rulesObjectivesRelation}' initialStateRelation='{initialStateRelation}'.",
                DebugUtility.Colors.Info);

            return hasSession || hasPhase || hasPlayers || hasRulesObjectives || hasInitialState;
        }

        public static void ClearCurrentState(string reason = null)
        {
            string normalizedReason = Normalize(reason);

            if (TryResolveSessionContextService(out var sessionService))
            {
                sessionService.Clear(normalizedReason);
            }

            if (TryResolvePhaseRuntimeService(out var phaseService))
            {
                phaseService.Clear(normalizedReason);
            }

            if (TryResolvePhasePlayersService(out var playersService))
            {
                playersService.Clear(normalizedReason);
            }

            if (TryResolvePhaseRulesObjectivesService(out var rulesObjectivesService))
            {
                rulesObjectivesService.Clear(normalizedReason);
            }

            if (TryResolvePhaseInitialStateService(out var initialStateService))
            {
                initialStateService.Clear(normalizedReason);
            }

            DebugUtility.Log(typeof(GameplaySessionFlowSmokeReporter),
                $"[OBS][GameplaySessionFlow][Smoke] smokeStateCleared reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
        }

        private static bool TryResolveSessionContext(out GameplaySessionContextSnapshot snapshot)
        {
            snapshot = GameplaySessionContextSnapshot.Empty;
            return TryResolveSessionContextService(out var service) && service.TryGetCurrent(out snapshot);
        }

        private static bool TryResolvePhaseRuntime(out GameplayPhaseRuntimeSnapshot snapshot)
        {
            snapshot = GameplayPhaseRuntimeSnapshot.Empty;
            return TryResolvePhaseRuntimeService(out var service) && service.TryGetCurrent(out snapshot);
        }

        private static bool TryResolvePhasePlayers(out GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            snapshot = GameplayPhasePlayerParticipationSnapshot.Empty;
            return TryResolvePhasePlayersService(out var service) && service.TryGetCurrent(out snapshot);
        }

        private static bool TryResolvePhaseRulesObjectives(out GameplayPhaseRulesObjectivesSnapshot snapshot)
        {
            snapshot = GameplayPhaseRulesObjectivesSnapshot.Empty;
            return TryResolvePhaseRulesObjectivesService(out var service) && service.TryGetCurrent(out snapshot);
        }

        private static bool TryResolvePhaseInitialState(out GameplayPhaseInitialStateSnapshot snapshot)
        {
            snapshot = GameplayPhaseInitialStateSnapshot.Empty;
            return TryResolvePhaseInitialStateService(out var service) && service.TryGetCurrent(out snapshot);
        }

        private static bool TryResolveSessionContextService(out IGameplaySessionContextService service)
        {
            service = null;
            return DependencyManager.Provider != null &&
                   DependencyManager.Provider.TryGetGlobal(out service) &&
                   service != null;
        }

        private static bool TryResolvePhaseRuntimeService(out IGameplayPhaseRuntimeService service)
        {
            service = null;
            return DependencyManager.Provider != null &&
                   DependencyManager.Provider.TryGetGlobal(out service) &&
                   service != null;
        }

        private static bool TryResolvePhasePlayersService(out IGameplayPhasePlayerParticipationService service)
        {
            service = null;
            return DependencyManager.Provider != null &&
                   DependencyManager.Provider.TryGetGlobal(out service) &&
                   service != null;
        }

        private static bool TryResolvePhaseRulesObjectivesService(out IGameplayPhaseRulesObjectivesService service)
        {
            service = null;
            return DependencyManager.Provider != null &&
                   DependencyManager.Provider.TryGetGlobal(out service) &&
                   service != null;
        }

        private static bool TryResolvePhaseInitialStateService(out IGameplayPhaseInitialStateService service)
        {
            service = null;
            return DependencyManager.Provider != null &&
                   DependencyManager.Provider.TryGetGlobal(out service) &&
                   service != null;
        }

        private static string DescribeSessionContext(GameplaySessionContextSnapshot snapshot)
        {
            return snapshot.IsValid
                ? $"filled signature='{snapshot.SessionSignature}' routeId='{snapshot.MacroRouteId}' v='{snapshot.SelectionVersion}'"
                : "empty";
        }

        private static string DescribePhaseRuntime(GameplayPhaseRuntimeSnapshot snapshot)
        {
            return snapshot.IsValid
                ? $"filled signature='{snapshot.PhaseRuntimeSignature}' sessionSignature='{snapshot.SessionContext.SessionSignature}' levelSignature='{snapshot.LevelSession.LevelSignature}'"
                : "empty";
        }

        private static string DescribePhasePlayers(GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            return snapshot.IsValid
                ? $"filled participationSignature='{snapshot.ParticipationSignature}' phaseSignature='{snapshot.PhaseRuntime.PhaseRuntimeSignature}' mode='{snapshot.ParticipationMode}' count='{snapshot.ParticipatingPlayerCount}' primaryId='{snapshot.PrimaryParticipantId}'"
                : "empty";
        }

        private static string DescribePhaseRulesObjectives(GameplayPhaseRulesObjectivesSnapshot snapshot)
        {
            return snapshot.IsValid
                ? $"filled rulesSignature='{snapshot.RulesSignature}' objectivesSignature='{snapshot.ObjectivesSignature}' phaseSignature='{snapshot.PhaseRuntime.PhaseRuntimeSignature}' ruleEntryCount='{snapshot.RuleEntryCount}' objectiveEntryCount='{snapshot.ObjectiveEntryCount}' primaryObjectiveId='{snapshot.PrimaryObjectiveId}'"
                : "empty";
        }

        private static string DescribePhaseInitialState(GameplayPhaseInitialStateSnapshot snapshot)
        {
            return snapshot.IsValid
                ? $"filled initialStateSignature='{snapshot.InitialStateSignature}' seedSource='{snapshot.SeedSource}' phaseSignature='{snapshot.PhaseRuntime.PhaseRuntimeSignature}' rulesSignature='{snapshot.RulesObjectives.RulesSignature}' objectivesSignature='{snapshot.RulesObjectives.ObjectivesSignature}'"
                : "empty";
        }

        private static string DescribeSessionPhaseRelation(
            bool hasSession,
            bool hasPhase,
            GameplaySessionContextSnapshot sessionContext,
            GameplayPhaseRuntimeSnapshot phaseRuntime)
        {
            if (!hasSession && !hasPhase)
            {
                return "both_empty";
            }

            if (hasSession && !hasPhase)
            {
                return "session_filled_phase_empty";
            }

            if (!hasSession && hasPhase)
            {
                return "phase_without_session";
            }

            return string.Equals(sessionContext.SessionSignature, phaseRuntime.SessionContext.SessionSignature, StringComparison.Ordinal)
                ? "linked"
                : "mismatch";
        }

        private static string DescribePhasePlayersRelation(
            bool hasPhase,
            bool hasPlayers,
            GameplayPhaseRuntimeSnapshot phaseRuntime,
            GameplayPhasePlayerParticipationSnapshot players)
        {
            if (!hasPhase && !hasPlayers)
            {
                return "both_empty";
            }

            if (hasPhase && !hasPlayers)
            {
                return "phase_filled_players_empty";
            }

            if (!hasPhase && hasPlayers)
            {
                return "players_without_phase";
            }

            return string.Equals(phaseRuntime.PhaseRuntimeSignature, players.PhaseRuntime.PhaseRuntimeSignature, StringComparison.Ordinal)
                ? "linked"
                : "mismatch";
        }

        private static string DescribePhaseRulesObjectivesRelation(
            bool hasPhase,
            bool hasRulesObjectives,
            GameplayPhaseRuntimeSnapshot phaseRuntime,
            GameplayPhaseRulesObjectivesSnapshot rulesObjectives)
        {
            if (!hasPhase && !hasRulesObjectives)
            {
                return "both_empty";
            }

            if (hasPhase && !hasRulesObjectives)
            {
                return "phase_filled_rulesObjectives_empty";
            }

            if (!hasPhase && hasRulesObjectives)
            {
                return "rulesObjectives_without_phase";
            }

            return string.Equals(phaseRuntime.PhaseRuntimeSignature, rulesObjectives.PhaseRuntime.PhaseRuntimeSignature, StringComparison.Ordinal)
                ? "linked"
                : "mismatch";
        }

        private static string DescribePhaseInitialStateRelation(
            bool hasPhase,
            bool hasRulesObjectives,
            bool hasInitialState,
            GameplayPhaseRuntimeSnapshot phaseRuntime,
            GameplayPhaseRulesObjectivesSnapshot rulesObjectives,
            GameplayPhaseInitialStateSnapshot initialState)
        {
            if (!hasPhase && !hasRulesObjectives && !hasInitialState)
            {
                return "both_empty";
            }

            if (hasPhase && !hasInitialState)
            {
                return "phase_filled_initialState_empty";
            }

            if (!hasPhase && hasInitialState)
            {
                return "initialState_without_phase";
            }

            if (!hasRulesObjectives && hasInitialState)
            {
                return "initialState_without_rulesObjectives";
            }

            bool phaseLinked = string.Equals(phaseRuntime.PhaseRuntimeSignature, initialState.PhaseRuntime.PhaseRuntimeSignature, StringComparison.Ordinal);
            bool rulesLinked = string.Equals(rulesObjectives.RulesSignature, initialState.RulesObjectives.RulesSignature, StringComparison.Ordinal);
            return phaseLinked && rulesLinked ? "linked" : "mismatch";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<null>" : value.Trim();
        }
    }
}
