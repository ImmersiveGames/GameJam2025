using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext
{
    public readonly struct GameplayPhaseRuntimeMaterializedEvent : IEvent
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

    public readonly struct PhaseCompleted : IEvent
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
                "[OBS][GameplaySessionFlow][PhaseRuntime] owner='GameplayPhaseRuntimeService' executor='GameplayPhaseRuntimeService' role='technical-fine-runtime'.");
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
                    $"[OBS][GameplaySessionFlow][PhaseRuntime] PhaseRuntimeUpdated owner='GameplayPhaseRuntimeService' sessionSignature='{snapshot.SessionContext.SessionSignature}' phaseId='{(snapshot.PhaseDefinitionRef != null ? snapshot.PhaseDefinitionRef.PhaseId : PhaseDefinitionId.None)}' phaseRef='{(snapshot.PhaseDefinitionRef != null ? snapshot.PhaseDefinitionRef.name : "<none>")}' contentCount='{snapshot.ContentEntryCount}' playerCount='{snapshot.PlayerEntryCount}' phaseSignature='{snapshot.PhaseRuntimeSignature}'.",
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
                $"[OBS][GameplaySessionFlow][PhaseRuntime] PhaseRuntimeCleared owner='GameplayPhaseRuntimeService' keepLast='true' lastPhaseSignature='{Normalize(lastSignature)}' reason='{normalizedReason}'.",
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
                "[OBS][GameplaySessionFlow][Participation] owner='GameplayParticipationFlowService' executor='GameplayParticipationFlowService' role='semantic-roster-owner'.",
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
                $"[OBS][GameplaySessionFlow][Participation] ParticipationUpdated owner='GameplayParticipationFlowService' executor='GameplayParticipationFlowService' source='{source}' sessionSignature='{snapshot.SessionSignature}' phaseSignature='{snapshot.PhaseSignature}' participantCount='{snapshot.ParticipantCount}' primaryId='{snapshot.PrimaryParticipantId}' readinessState='{snapshot.Readiness.State}' readinessCanEnter='{snapshot.Readiness.CanEnterGameplay}' signature='{snapshot.Signature}'.",
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

}

