using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using System.Collections.Generic;
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
                "[OBS][GameplaySessionFlow][PlayersLegacy] GameplayPhasePlayerParticipationService registrado como ponte de compatibilidade.");
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
                    $"[OBS][GameplaySessionFlow][PlayersLegacy] PlayersUpdated compatibilityOnly='true' phaseSignature='{snapshot.PhaseRuntime.PhaseRuntimeSignature}' participationMode='{snapshot.ParticipationMode}' participantCount='{snapshot.ParticipatingPlayerCount}' primaryId='{snapshot.PrimaryParticipantId}' participationSignature='{snapshot.ParticipationSignature}'.",
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
                $"[OBS][GameplaySessionFlow][PlayersLegacy] PlayersCleared keepLast='true' compatibilityOnly='true' lastParticipationSignature='{Normalize(lastSignature)}' reason='{normalizedReason}'.",
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

    public interface IGameplayParticipationFlowService
    {
        ParticipationSnapshot Current { get; }
        ParticipationReadinessSnapshot CurrentReadiness { get; }
        bool TryGetCurrent(out ParticipationSnapshot snapshot);
        bool TryGetCurrentReadiness(out ParticipationReadinessSnapshot readiness);
        bool TryGetLast(out ParticipationSnapshot snapshot);
        ParticipationSnapshot Update(ParticipationSnapshot snapshot);
        ParticipationSnapshot UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt);
        void Clear(string reason = null);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayParticipationFlowService :
        IGameplayParticipationFlowService,
        IGameplayPhasePlayerParticipationService,
        IDisposable
    {
        private readonly object _sync = new();
        private ParticipationSnapshot _current = ParticipationSnapshot.Empty;
        private ParticipationSnapshot _last = ParticipationSnapshot.Empty;
        private GameplayPhaseRuntimeSnapshot _currentPhaseRuntime = GameplayPhaseRuntimeSnapshot.Empty;
        private GameplayPhaseRuntimeSnapshot _lastPhaseRuntime = GameplayPhaseRuntimeSnapshot.Empty;

        public GameplayParticipationFlowService()
        {
            RegisterSelfInGlobalDi();

            DebugUtility.LogVerbose<GameplayParticipationFlowService>(
                "[OBS][GameplaySessionFlow][Participation] GameplayParticipationFlowService registrado como owner do roster semantico.",
                DebugUtility.Colors.Info);
        }

        public ParticipationSnapshot Current
        {
            get
            {
                lock (_sync)
                {
                    return _current;
                }
            }
        }

        public ParticipationReadinessSnapshot CurrentReadiness
        {
            get
            {
                lock (_sync)
                {
                    return _current.Readiness;
                }
            }
        }

        public bool TryGetCurrent(out ParticipationSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _current;
                return _current.IsValid;
            }
        }

        public bool TryGetCurrentReadiness(out ParticipationReadinessSnapshot readiness)
        {
            lock (_sync)
            {
                readiness = _current.Readiness;
                return _current.IsValid && readiness.IsValid;
            }
        }

        public bool TryGetLast(out ParticipationSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _last;
                return _last.IsValid;
            }
        }

        public ParticipationSnapshot Update(ParticipationSnapshot snapshot)
        {
            return UpdateInternal(snapshot, GameplayPhaseRuntimeSnapshot.Empty, source: "manual_update");
        }

        public ParticipationSnapshot UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            GameplayPhaseRuntimeSnapshot phaseRuntime = GameplayPhaseRuntimeSnapshot.FromPhaseDefinitionSelectedEvent(evt);
            return UpdateInternal(FromPhaseDefinitionSelectedEvent(evt), phaseRuntime, source: "phase_selected_event");
        }

        public void Clear(string reason = null)
        {
            string normalizedReason = Normalize(reason);
            string lastSignature;
            ParticipationSnapshot clearedSnapshot = ParticipationSnapshot.Empty;

            lock (_sync)
            {
                _last = _current;
                _lastPhaseRuntime = _currentPhaseRuntime;
                _current = clearedSnapshot;
                _currentPhaseRuntime = GameplayPhaseRuntimeSnapshot.Empty;
                lastSignature = _last.Signature.Value;
            }

            DebugUtility.Log<GameplayParticipationFlowService>(
                $"[OBS][GameplaySessionFlow][Participation] ParticipationCleared keepLast='true' lastParticipationSignature='{Normalize(lastSignature)}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            EventBus<ParticipationSnapshotChangedEvent>.Raise(
                new ParticipationSnapshotChangedEvent(
                    clearedSnapshot,
                    source: "GameplayParticipationFlowService.Clear",
                    reason: normalizedReason,
                    isCleared: true));
        }

        public void Dispose()
        {
        }

        GameplayPhasePlayerParticipationSnapshot IGameplayPhasePlayerParticipationService.Current
        {
            get
            {
                lock (_sync)
                {
                    return ToLegacySnapshot(_current, _currentPhaseRuntime);
                }
            }
        }

        bool IGameplayPhasePlayerParticipationService.TryGetCurrent(out GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            lock (_sync)
            {
                if (!_current.IsValid)
                {
                    snapshot = GameplayPhasePlayerParticipationSnapshot.Empty;
                    return false;
                }

                snapshot = ToLegacySnapshot(_current, _currentPhaseRuntime);
                return true;
            }
        }

        bool IGameplayPhasePlayerParticipationService.TryGetLast(out GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            lock (_sync)
            {
                if (!_last.IsValid)
                {
                    snapshot = GameplayPhasePlayerParticipationSnapshot.Empty;
                    return false;
                }

                snapshot = ToLegacySnapshot(_last, _lastPhaseRuntime);
                return true;
            }
        }

        GameplayPhasePlayerParticipationSnapshot IGameplayPhasePlayerParticipationService.Update(GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            HardFailFastH1.Trigger(typeof(GameplayParticipationFlowService),
                "[FATAL][H1][GameplaySessionFlow] Legacy player-participation write path is not owned by GameplayParticipationFlowService.");
            return GameplayPhasePlayerParticipationSnapshot.Empty;
        }

        GameplayPhasePlayerParticipationSnapshot IGameplayPhasePlayerParticipationService.UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            ParticipationSnapshot updated = UpdateFromPhaseDefinitionSelectedEvent(evt);
            return ToLegacySnapshot(updated, _currentPhaseRuntime);
        }

        void IGameplayPhasePlayerParticipationService.Clear(string reason)
        {
            Clear(reason);
        }

        private static ParticipationSnapshot FromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            if (evt.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayParticipationFlowService),
                    "[FATAL][H1][GameplaySessionFlow] PhaseDefinitionSelectedEvent requires a valid phaseDefinitionRef to build participation.");
            }

            string sessionSignature = GameplaySessionContextSnapshot.FromPhaseDefinitionSelectedEvent(evt).SessionSignature;
            string phaseSignature = GameplayPhaseRuntimeSnapshot.FromPhaseDefinitionSelectedEvent(evt).PhaseRuntimeSignature;

            ParticipantSnapshot[] participants = BuildParticipants(evt);
            bool hasParticipants = participants.Length > 0;
            ParticipantId primaryParticipantId = ResolveParticipantId(participants, participant => participant.IsPrimary);

            ParticipationReadinessSnapshot readiness = new(
                hasParticipants ? ParticipationReadinessState.Ready : ParticipationReadinessState.NoContent,
                hasParticipants ? "phase_players_derived" : "phase_players_empty",
                participants.Length,
                primaryParticipantId);

            return new ParticipationSnapshot(
                sessionSignature,
                phaseSignature,
                participants,
                readiness,
                ParticipationPublicationMode.SnapshotOnly);
        }

        private ParticipationSnapshot UpdateInternal(ParticipationSnapshot snapshot, GameplayPhaseRuntimeSnapshot phaseRuntime, string source)
        {
            lock (_sync)
            {
                if (!snapshot.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplayParticipationFlowService),
                        "[FATAL][H1][GameplaySessionFlow] Invalid participation snapshot received by participation owner.");
                }

                _lastPhaseRuntime = _currentPhaseRuntime;
                _currentPhaseRuntime = phaseRuntime;
                _last = _current;
                _current = snapshot;
            }

            DebugUtility.Log<GameplayParticipationFlowService>(
                $"[OBS][GameplaySessionFlow][Participation] ParticipationUpdated owner='GameplayParticipationFlowService' source='{source}' sessionSignature='{snapshot.SessionSignature}' phaseSignature='{snapshot.PhaseSignature}' participantCount='{snapshot.ParticipantCount}' primaryId='{snapshot.PrimaryParticipantId}' readinessState='{snapshot.Readiness.State}' readinessCanEnter='{snapshot.Readiness.CanEnterGameplay}' signature='{snapshot.Signature}'.",
                DebugUtility.Colors.Info);

            EventBus<ParticipationSnapshotChangedEvent>.Raise(
                new ParticipationSnapshotChangedEvent(
                    snapshot,
                    source: $"GameplayParticipationFlowService.{source}",
                    reason: source));

            return snapshot;
        }

        private static ParticipantSnapshot[] BuildParticipants(PhaseDefinitionSelectedEvent evt)
        {
            PhaseDefinitionAsset.PhasePlayersBlock playersBlock = evt.PhaseDefinitionRef.Players;
            if (playersBlock == null || playersBlock.entries == null || playersBlock.entries.Count == 0)
            {
                return Array.Empty<ParticipantSnapshot>();
            }

            var participants = new List<ParticipantSnapshot>(playersBlock.entries.Count);
            int primaryIndex = ResolvePrimaryIndex(playersBlock.entries);

            for (int index = 0; index < playersBlock.entries.Count; index += 1)
            {
                PhaseDefinitionAsset.PhasePlayerEntry entry = playersBlock.entries[index];
                if (entry == null)
                {
                    continue;
                }

                bool isPrimary = index == primaryIndex;
                bool isLocal = entry.role == PhaseDefinitionAsset.PhasePlayerRole.Local;
                ParticipantKind participantKind = ParticipantKind.Player;
                OwnershipKind ownershipKind = ResolveOwnershipKind(entry.role);
                BindingHint bindingHint = ResolveBindingHint(entry.role, isPrimary);
                string participantIdValue = ResolveParticipantIdValue(evt.PhaseId, entry, index);

                participants.Add(new ParticipantSnapshot(
                    new ParticipantId(participantIdValue),
                    participantKind,
                    ownershipKind,
                    bindingHint,
                    ParticipantLifecycleState.Expected,
                    isPrimary,
                    isLocal,
                    entry.localId));
            }

            return participants.ToArray();
        }

        private static int ResolvePrimaryIndex(IReadOnlyList<PhaseDefinitionAsset.PhasePlayerEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return -1;
            }

            for (int index = 0; index < entries.Count; index += 1)
            {
                PhaseDefinitionAsset.PhasePlayerEntry entry = entries[index];
                if (entry != null && entry.role == PhaseDefinitionAsset.PhasePlayerRole.Local)
                {
                    return index;
                }
            }

            return 0;
        }

        private static string ResolveParticipantIdValue(PhaseDefinitionId phaseId, PhaseDefinitionAsset.PhasePlayerEntry entry, int index)
        {
            if (entry != null && !string.IsNullOrWhiteSpace(entry.localId))
            {
                return entry.localId.Trim();
            }

            string phaseToken = phaseId.IsValid ? phaseId.Value : "<no-phase>";
            string roleToken = entry != null ? entry.role.ToString() : "Unknown";
            return $"{phaseToken}:participant:{roleToken}:{index + 1}";
        }

        private static OwnershipKind ResolveOwnershipKind(PhaseDefinitionAsset.PhasePlayerRole role)
        {
            switch (role)
            {
                case PhaseDefinitionAsset.PhasePlayerRole.Local:
                    return OwnershipKind.Local;
                case PhaseDefinitionAsset.PhasePlayerRole.Remote:
                    return OwnershipKind.Remote;
                case PhaseDefinitionAsset.PhasePlayerRole.Shared:
                    return OwnershipKind.Shared;
                case PhaseDefinitionAsset.PhasePlayerRole.Bot:
                    return OwnershipKind.Authoring;
                default:
                    return OwnershipKind.Unknown;
            }
        }

        private static BindingHint ResolveBindingHint(PhaseDefinitionAsset.PhasePlayerRole role, bool isPrimary)
        {
            switch (role)
            {
                case PhaseDefinitionAsset.PhasePlayerRole.Local:
                    return new BindingHint(isPrimary ? BindingHintKind.LocalPrimary : BindingHintKind.LocalSecondary);
                case PhaseDefinitionAsset.PhasePlayerRole.Remote:
                    return new BindingHint(BindingHintKind.Remote);
                case PhaseDefinitionAsset.PhasePlayerRole.Shared:
                    return new BindingHint(BindingHintKind.Shared);
                case PhaseDefinitionAsset.PhasePlayerRole.Bot:
                    return new BindingHint(BindingHintKind.Custom, "bot");
                default:
                    return BindingHint.None;
            }
        }

        private static ParticipantId ResolveParticipantId(ParticipantSnapshot[] participants, Func<ParticipantSnapshot, bool> predicate)
        {
            if (participants == null || predicate == null)
            {
                return ParticipantId.None;
            }

            for (int index = 0; index < participants.Length; index += 1)
            {
                ParticipantSnapshot participant = participants[index];
                if (participant.IsValid && predicate(participant))
                {
                    return participant.ParticipantId;
                }
            }

            return ParticipantId.None;
        }

        private static GameplayPhasePlayerParticipationSnapshot ToLegacySnapshot(ParticipationSnapshot snapshot, GameplayPhaseRuntimeSnapshot phaseRuntime)
        {
            if (!snapshot.IsValid || !phaseRuntime.IsValid)
            {
                return GameplayPhasePlayerParticipationSnapshot.Empty;
            }

            int participatingPlayerCount = snapshot.ParticipantCount;
            string primaryParticipantId = snapshot.PrimaryParticipantId.IsValid ? snapshot.PrimaryParticipantId.Value : string.Empty;
            string primaryParticipantLabel = "Player";

            if (snapshot.HasPrimaryParticipant)
            {
                for (int index = 0; index < snapshot.Participants.Length; index += 1)
                {
                    if (snapshot.Participants[index].IsPrimary)
                    {
                        primaryParticipantLabel = snapshot.Participants[index].Kind.ToString();
                        break;
                    }
                }
            }

            GameplayPhasePlayerParticipationMode participationMode =
                participatingPlayerCount == 1
                    ? GameplayPhasePlayerParticipationMode.SoloCanonical
                    : GameplayPhasePlayerParticipationMode.ConfiguredRoster;

            return new GameplayPhasePlayerParticipationSnapshot(
                phaseRuntime,
                participationMode,
                participatingPlayerCount,
                primaryParticipantId,
                primaryParticipantLabel);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }

        private void RegisterSelfInGlobalDi()
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayParticipationFlowService),
                    "[FATAL][H1][GameplaySessionFlow] DependencyManager.Provider unavailable while registering participation owner.");
            }

            RegisterOwnerBinding<GameplayParticipationFlowService>(this);
            RegisterOwnerBinding<IGameplayParticipationFlowService>(this);
            RegisterOwnerBinding<IGameplayPhasePlayerParticipationService>(this);
        }

        private static void RegisterOwnerBinding<T>(T instance)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                if (!ReferenceEquals(existing, instance))
                {
                    HardFailFastH1.Trigger(typeof(GameplayParticipationFlowService),
                        $"[FATAL][H1][GameplaySessionFlow] Conflicting global binding for '{typeof(T).Name}' while registering participation owner.");
                }

                return;
            }

            DependencyManager.Provider.RegisterGlobal<T>(instance);
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
            bool hasParticipation = TryResolveParticipation(out var participation);

            string sessionState = hasSession ? DescribeSessionContext(sessionContext) : "missing";
            string phaseState = hasPhase ? DescribePhaseRuntime(phaseRuntime) : "missing";
            string participationState = hasParticipation ? DescribeParticipation(participation) : "missing";
            string relation = DescribeSessionPhaseRelation(hasSession, hasPhase, sessionContext, phaseRuntime);
            string participationRelation = DescribeParticipationRelation(hasPhase, hasParticipation, phaseRuntime, participation);

            DebugUtility.Log(typeof(GameplaySessionFlowSmokeReporter),
                $"[OBS][GameplaySessionFlow][Smoke] stage='{normalizedStage}' reason='{normalizedReason}' sessionContext='{sessionState}' phaseRuntime='{phaseState}' participation='{participationState}' relation='{relation}' participationRelation='{participationRelation}'.",
                DebugUtility.Colors.Info);

            return hasSession || hasPhase || hasParticipation;
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

            if (TryResolveParticipationService(out var participationService))
            {
                participationService.Clear(normalizedReason);
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

        private static bool TryResolveParticipation(out ParticipationSnapshot snapshot)
        {
            snapshot = ParticipationSnapshot.Empty;
            return TryResolveParticipationService(out var service) && service.TryGetCurrent(out snapshot);
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

        private static bool TryResolveParticipationService(out IGameplayParticipationFlowService service)
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

        private static string DescribeParticipation(ParticipationSnapshot snapshot)
        {
            return snapshot.IsValid
                ? $"filled participationSignature='{snapshot.Signature}' phaseSignature='{snapshot.PhaseSignature}' readiness='{snapshot.Readiness.State}' count='{snapshot.ParticipantCount}' primaryId='{snapshot.PrimaryParticipantId}' localId='{snapshot.LocalParticipantId}'"
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

        private static string DescribeParticipationRelation(
            bool hasPhase,
            bool hasParticipation,
            GameplayPhaseRuntimeSnapshot phaseRuntime,
            ParticipationSnapshot participation)
        {
            if (!hasPhase && !hasParticipation)
            {
                return "both_empty";
            }

            if (hasPhase && !hasParticipation)
            {
                return "phase_filled_participation_empty";
            }

            if (!hasPhase && hasParticipation)
            {
                return "participation_without_phase";
            }

            return string.Equals(phaseRuntime.PhaseRuntimeSignature, participation.PhaseSignature, StringComparison.Ordinal)
                ? "linked"
                : "mismatch";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<null>" : value.Trim();
        }
    }
}
