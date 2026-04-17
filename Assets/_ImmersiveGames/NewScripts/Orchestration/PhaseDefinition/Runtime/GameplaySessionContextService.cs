using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    public readonly struct GameplayPhaseRuntimeMaterializedEvent : _ImmersiveGames.NewScripts.Core.Events.IEvent
    {
        public GameplayPhaseRuntimeMaterializedEvent(GameplayPhaseRuntimeSnapshot runtime, string source, int phaseLocalEntrySequence, string entrySignature = "")
        {
            Runtime = runtime;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            PhaseLocalEntrySequence = phaseLocalEntrySequence < 0 ? 0 : phaseLocalEntrySequence;
            EntrySignature = string.IsNullOrWhiteSpace(entrySignature) ? string.Empty : entrySignature.Trim();
        }

        public GameplayPhaseRuntimeSnapshot Runtime { get; }
        public string Source { get; }
        public int PhaseLocalEntrySequence { get; }
        public string EntrySignature { get; }
    }

    public readonly struct PhaseCompleted : _ImmersiveGames.NewScripts.Core.Events.IEvent
    {
        public PhaseCompleted(
            GameplayPhaseRuntimeSnapshot phaseRuntime,
            RunEndIntent runEndIntent,
            GameRunOutcome runOutcome,
            string source,
            int phaseLocalEntrySequence,
            string entrySignature)
        {
            PhaseRuntime = phaseRuntime;
            RunEndIntent = runEndIntent;
            RunOutcome = runOutcome;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            PhaseLocalEntrySequence = phaseLocalEntrySequence < 0 ? 0 : phaseLocalEntrySequence;
            EntrySignature = string.IsNullOrWhiteSpace(entrySignature) ? string.Empty : entrySignature.Trim();
        }

        public GameplayPhaseRuntimeSnapshot PhaseRuntime { get; }
        public RunEndIntent RunEndIntent { get; }
        public GameRunOutcome RunOutcome { get; }
        public string Source { get; }
        public int PhaseLocalEntrySequence { get; }
        public string EntrySignature { get; }

        public PhaseDefinitionAsset PhaseDefinitionRef => PhaseRuntime.PhaseDefinitionRef;
        public string PhaseSignature => PhaseRuntime.PhaseRuntimeSignature;
        public bool IsValid =>
            PhaseRuntime.IsValid &&
            PhaseDefinitionRef != null &&
            PhaseDefinitionRef.PhaseId.IsValid &&
            !string.IsNullOrWhiteSpace(RunEndIntent.Signature) &&
            !string.IsNullOrWhiteSpace(RunEndIntent.SceneName) &&
            RunOutcome != GameRunOutcome.Unknown &&
            !string.IsNullOrWhiteSpace(Source) &&
            PhaseLocalEntrySequence >= 0;
    }

    public readonly struct GameplayPhaseRuntimeSnapshot
    {
        public static GameplayPhaseRuntimeSnapshot FromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            if (evt.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseRuntimeSnapshot),
                    "[FATAL][H1][GameplaySessionFlow] PhaseDefinitionSelectedEvent requires a valid phaseDefinitionRef to build the phase runtime snapshot.");
            }

            GameplaySessionContextSnapshot sessionContext = GameplaySessionContextSnapshot.FromPhaseDefinitionSelectedEvent(evt);
            PhaseDefinitionAsset phaseDefinitionRef = evt.PhaseDefinitionRef;

            int contentEntryCount = phaseDefinitionRef.Content != null && phaseDefinitionRef.Content.entries != null ? phaseDefinitionRef.Content.entries.Count : 0;
            int playerEntryCount = phaseDefinitionRef.Players != null && phaseDefinitionRef.Players.entries != null ? phaseDefinitionRef.Players.entries.Count : 0;
            // A eligibilidade do Intro nao vem mais do asset autoral; a resolucao real acontece no host operacional.
            bool hasIntroStage = true;

            return new GameplayPhaseRuntimeSnapshot(
                sessionContext,
                IntroStageSession.Empty,
                phaseDefinitionRef,
                contentEntryCount,
                playerEntryCount,
                hasIntroStage);
        }

        public IntroStageSession CreateIntroStageSession(
            string localContentId,
            string reason,
            int selectionVersion,
            int phaseLocalEntrySequence,
            string phaseSignature,
            string entrySignature = "")
        {
            if (PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseRuntimeSnapshot),
                    "[FATAL][H1][GameplaySessionFlow] Cannot materialize intro session from an invalid phase runtime snapshot.");
            }

            string normalizedContentId = string.IsNullOrWhiteSpace(localContentId)
                ? PhaseDefinitionId.BuildCanonicalIntroContentId(PhaseDefinitionRef.PhaseId)
                : localContentId.Trim();
            return new IntroStageSession(
                PhaseDefinitionRef,
                normalizedContentId,
                reason,
                selectionVersion,
                phaseLocalEntrySequence,
                phaseSignature,
                HasIntroStage,
                entrySignature);
        }

        public GameplayPhaseRuntimeSnapshot(
            GameplaySessionContextSnapshot sessionContext,
            IntroStageSession levelSession,
            bool hasIntroStage)
        {
            SessionContext = sessionContext;
            IntroStageSession = levelSession;
            PhaseDefinitionRef = null;
            ContentEntryCount = 0;
            PlayerEntryCount = 0;
            HasIntroStage = hasIntroStage;
            PhaseRuntimeSignature = BuildPhaseRuntimeSignature(sessionContext, levelSession);
        }

        public GameplayPhaseRuntimeSnapshot(
            GameplaySessionContextSnapshot sessionContext,
            IntroStageSession levelSession,
            PhaseDefinitionAsset phaseDefinitionRef,
            int contentEntryCount,
            int playerEntryCount,
            bool hasIntroStage)
        {
            SessionContext = sessionContext;
            IntroStageSession = levelSession;
            PhaseDefinitionRef = phaseDefinitionRef;
            ContentEntryCount = contentEntryCount < 0 ? 0 : contentEntryCount;
            PlayerEntryCount = playerEntryCount < 0 ? 0 : playerEntryCount;
            HasIntroStage = hasIntroStage;
            PhaseRuntimeSignature = BuildPhaseRuntimeSignature(sessionContext, levelSession, phaseDefinitionRef, ContentEntryCount, PlayerEntryCount);
        }

        public GameplaySessionContextSnapshot SessionContext { get; }
        public IntroStageSession IntroStageSession { get; }
        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public int ContentEntryCount { get; }
        public int PlayerEntryCount { get; }
        public bool HasIntroStage { get; }
        public string PhaseRuntimeSignature { get; }

        public bool IsValid =>
            SessionContext.IsValid &&
            ((IntroStageSession.IsValid && PhaseDefinitionRef == null) || (PhaseDefinitionRef != null && PhaseDefinitionRef.PhaseId.IsValid));
        public bool HasPhaseDefinitionRef => PhaseDefinitionRef != null;
        public bool HasPhaseRuntimeSignature => !string.IsNullOrWhiteSpace(PhaseRuntimeSignature);

        public static GameplayPhaseRuntimeSnapshot Empty => new(
            GameplaySessionContextSnapshot.Empty,
            IntroStageSession.Empty,
            null,
            0,
            0,
            false);

        public override string ToString()
        {
            string phaseName = PhaseDefinitionRef != null ? PhaseDefinitionRef.name : "<none>";
            return $"sessionContext='{SessionContext}', phaseRef='{phaseName}', contentCount='{ContentEntryCount}', playerCount='{PlayerEntryCount}', introStage='{HasIntroStage}', phaseRuntimeSignature='{(string.IsNullOrWhiteSpace(PhaseRuntimeSignature) ? "<none>" : PhaseRuntimeSignature)}'";
        }

        private static string BuildPhaseRuntimeSignature(
            GameplaySessionContextSnapshot sessionContext,
            IntroStageSession introStageSession)
        {
            string sessionSignature = sessionContext.HasSessionSignature ? sessionContext.SessionSignature : "<no-session>";
            string sessionIntroSignature = string.IsNullOrWhiteSpace(introStageSession.SessionSignature) ? "<no-session>" : introStageSession.SessionSignature;
            return $"{sessionSignature}|{sessionIntroSignature}";
        }

        private static string BuildPhaseRuntimeSignature(
            GameplaySessionContextSnapshot sessionContext,
            IntroStageSession introStageSession,
            PhaseDefinitionAsset phaseDefinitionRef,
            int contentEntryCount,
            int playerEntryCount)
        {
            if (phaseDefinitionRef == null)
            {
                return BuildPhaseRuntimeSignature(sessionContext, introStageSession);
            }

            string sessionSignature = sessionContext.HasSessionSignature ? sessionContext.SessionSignature : "<no-session>";
            string phaseId = phaseDefinitionRef.PhaseId.IsValid ? phaseDefinitionRef.PhaseId.Value : "<no-phase>";
            return $"{sessionSignature}|{phaseId}|content:{contentEntryCount}|players:{playerEntryCount}";
        }
    }

    public interface IGameplayPhaseRuntimeService
    {
        GameplayPhaseRuntimeSnapshot Current { get; }
        bool TryGetCurrent(out GameplayPhaseRuntimeSnapshot snapshot);
        bool TryGetLast(out GameplayPhaseRuntimeSnapshot snapshot);
        GameplayPhaseRuntimeSnapshot Update(GameplayPhaseRuntimeSnapshot snapshot);
        GameplayPhaseRuntimeSnapshot UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt);
        void Clear(string reason = null);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayPhaseRuntimeService : IGameplayPhaseRuntimeService, IDisposable
    {
        private readonly object _sync = new();
        private GameplayPhaseRuntimeSnapshot _current = GameplayPhaseRuntimeSnapshot.Empty;
        private GameplayPhaseRuntimeSnapshot _last = GameplayPhaseRuntimeSnapshot.Empty;

        public GameplayPhaseRuntimeService()
        {
            DebugUtility.LogVerbose<GameplayPhaseRuntimeService>(
                "[OBS][GameplaySessionFlow][PhaseRuntime] GameplayPhaseRuntimeService registrado como owner do runtime da fase.");
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

        public GameplayPhaseRuntimeSnapshot UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            return Update(GameplayPhaseRuntimeSnapshot.FromPhaseDefinitionSelectedEvent(evt));
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
                $"[OBS][GameplaySessionFlow][PhaseRuntime] PhaseRuntimeUpdated sessionSignature='{snapshot.SessionContext.SessionSignature}' phaseId='{(snapshot.PhaseDefinitionRef != null ? snapshot.PhaseDefinitionRef.PhaseId : PhaseDefinitionId.None)}' phaseRef='{(snapshot.PhaseDefinitionRef != null ? snapshot.PhaseDefinitionRef.name : "<none>")}' contentCount='{snapshot.ContentEntryCount}' playerCount='{snapshot.PlayerEntryCount}' phaseSignature='{snapshot.PhaseRuntimeSignature}'.",
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
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }

    public enum GameplayPhasePlayerParticipationMode
    {
        None = 0,
        SoloCanonical = 1,
        ConfiguredRoster = 2
    }

    public readonly struct GameplayPhasePlayerParticipationSnapshot
    {
        public static GameplayPhasePlayerParticipationSnapshot FromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            return FromPhaseDefinitionSelectedEvent(evt, GameplayPhaseRuntimeSnapshot.FromPhaseDefinitionSelectedEvent(evt));
        }

        internal static GameplayPhasePlayerParticipationSnapshot FromPhaseDefinitionSelectedEvent(
            PhaseDefinitionSelectedEvent evt,
            GameplayPhaseRuntimeSnapshot phaseRuntime)
        {
            if (evt.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhasePlayerParticipationSnapshot),
                    "[FATAL][H1][GameplaySessionFlow] PhaseDefinitionSelectedEvent requires a valid phaseDefinitionRef to build the players snapshot.");
            }

            PhaseDefinitionAsset.PhasePlayersBlock playersBlock = evt.PhaseDefinitionRef.Players;
            if (playersBlock == null || playersBlock.entries == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhasePlayerParticipationSnapshot),
                    $"[FATAL][H1][GameplaySessionFlow] PhaseDefinition phaseId='{evt.PhaseId}' has no valid players block.");
            }

            int participantCount = 0;
            string primaryParticipantId = string.Empty;
            string primaryParticipantLabel = string.Empty;
            GameplayPhasePlayerParticipationMode participationMode = GameplayPhasePlayerParticipationMode.ConfiguredRoster;

            for (int i = 0; i < playersBlock.entries.Count; i++)
            {
                PhaseDefinitionAsset.PhasePlayerEntry entry = playersBlock.entries[i];
                if (entry == null)
                {
                    continue;
                }

                participantCount++;
                if (string.IsNullOrWhiteSpace(primaryParticipantId))
                {
                    primaryParticipantId = string.IsNullOrWhiteSpace(entry.localId) ? $"player_{i + 1}" : entry.localId.Trim();
                    primaryParticipantLabel = entry.role.ToString();
                }

                if (entry.role == PhaseDefinitionAsset.PhasePlayerRole.Local)
                {
                    primaryParticipantId = string.IsNullOrWhiteSpace(entry.localId) ? $"player_{i + 1}" : entry.localId.Trim();
                    primaryParticipantLabel = "Player";
                    participationMode = participantCount == 1 ? GameplayPhasePlayerParticipationMode.SoloCanonical : GameplayPhasePlayerParticipationMode.ConfiguredRoster;
                    break;
                }
            }

            if (participantCount <= 0)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhasePlayerParticipationSnapshot),
                    $"[FATAL][H1][GameplaySessionFlow] PhaseDefinition phaseId='{evt.PhaseId}' has an empty players block.");
            }

            if (string.IsNullOrWhiteSpace(primaryParticipantId))
            {
                primaryParticipantId = "primary_player";
                primaryParticipantLabel = "Player";
            }

            if (participantCount == 1)
            {
                participationMode = GameplayPhasePlayerParticipationMode.SoloCanonical;
            }

            return new GameplayPhasePlayerParticipationSnapshot(
                phaseRuntime,
                participationMode,
                participantCount,
                primaryParticipantId,
                primaryParticipantLabel);
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
        public bool IsValid => PhaseRuntime.IsValid && HasParticipants && !string.IsNullOrWhiteSpace(PrimaryParticipantId);

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
        GameplayPhasePlayerParticipationSnapshot UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt);
        void Clear(string reason = null);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayPhasePlayerParticipationService : IGameplayPhasePlayerParticipationService, IDisposable
    {
        private readonly object _sync = new();
        private GameplayPhasePlayerParticipationSnapshot _current = GameplayPhasePlayerParticipationSnapshot.Empty;
        private GameplayPhasePlayerParticipationSnapshot _last = GameplayPhasePlayerParticipationSnapshot.Empty;

        public GameplayPhasePlayerParticipationService()
        {
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

        public GameplayPhasePlayerParticipationSnapshot UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            return Update(GameplayPhasePlayerParticipationSnapshot.FromPhaseDefinitionSelectedEvent(evt));
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

            string sessionState = hasSession ? DescribeSessionContext(sessionContext) : "missing";
            string phaseState = hasPhase ? DescribePhaseRuntime(phaseRuntime) : "missing";
            string playersState = hasPlayers ? DescribePhasePlayers(players) : "missing";
            string relation = DescribeSessionPhaseRelation(hasSession, hasPhase, sessionContext, phaseRuntime);
            string playersRelation = DescribePhasePlayersRelation(hasPhase, hasPlayers, phaseRuntime, players);

            DebugUtility.Log(typeof(GameplaySessionFlowSmokeReporter),
                $"[OBS][GameplaySessionFlow][Smoke] stage='{normalizedStage}' reason='{normalizedReason}' sessionContext='{sessionState}' phaseRuntime='{phaseState}' players='{playersState}' relation='{relation}' playersRelation='{playersRelation}'.",
                DebugUtility.Colors.Info);

            return hasSession || hasPhase || hasPlayers;
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

        private static string DescribeSessionContext(GameplaySessionContextSnapshot snapshot)
        {
            return snapshot.IsValid
                ? $"filled signature='{snapshot.SessionSignature}' phaseId='{snapshot.PhaseId}' routeId='{snapshot.MacroRouteId}' v='{snapshot.SelectionVersion}'"
                : "empty";
        }

        private static string DescribePhaseRuntime(GameplayPhaseRuntimeSnapshot snapshot)
        {
            return snapshot.IsValid
                ? $"filled signature='{snapshot.PhaseRuntimeSignature}' sessionSignature='{snapshot.SessionContext.SessionSignature}' phaseRef='{(snapshot.PhaseDefinitionRef != null ? snapshot.PhaseDefinitionRef.name : "<none>")}' contentCount='{snapshot.ContentEntryCount}' playerCount='{snapshot.PlayerEntryCount}'"
                : "empty";
        }

        private static string DescribePhasePlayers(GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            return snapshot.IsValid
                ? $"filled participationSignature='{snapshot.ParticipationSignature}' phaseSignature='{snapshot.PhaseRuntime.PhaseRuntimeSignature}' mode='{snapshot.ParticipationMode}' count='{snapshot.ParticipatingPlayerCount}' primaryId='{snapshot.PrimaryParticipantId}'"
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<null>" : value.Trim();
        }
    }
}
