using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility;
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
            bool hasIntroStage = phaseDefinitionRef.Intro != null && phaseDefinitionRef.Intro.hasIntroStage;

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
}

