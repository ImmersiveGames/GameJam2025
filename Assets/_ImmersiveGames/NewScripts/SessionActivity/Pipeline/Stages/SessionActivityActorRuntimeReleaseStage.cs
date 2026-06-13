using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class SessionActivityActorRuntimeReleaseStage
    {
        private const string Owner = "SessionActivityActorRuntimeReleaseStage";
        private const string MacroLifecycleOwner = "SessionActivityPipeline";

        public static void ReleaseSessionScopedActorsForSession(
            SessionActivityIdentity identity,
            SessionActorRuntimeStore sessionActorRuntimeStore,
            SessionActivityRuntimeState runtimeState,
            List<SessionActivityFact> facts,
            string source,
            string reason)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("SessionActivityActorRuntimeReleaseStage requires valid identity.");
            }

            sessionActorRuntimeStore = sessionActorRuntimeStore ?? throw new ArgumentNullException(nameof(sessionActorRuntimeStore));
            runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            facts ??= new List<SessionActivityFact>();

            IReadOnlyList<SessionActorRuntimeEntry> entries = sessionActorRuntimeStore.GetEntriesForSession(identity);
            ReleaseSessionScopedActors(identity, entries, sessionActorRuntimeStore, runtimeState, facts, source, reason, emitFacts: true);
        }

        public static void ReleaseAllSessionScopedActors(
            SessionActivityIdentity identity,
            SessionActorRuntimeStore sessionActorRuntimeStore,
            string source,
            string reason)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("SessionActivityActorRuntimeReleaseStage requires valid identity.");
            }

            sessionActorRuntimeStore = sessionActorRuntimeStore ?? throw new ArgumentNullException(nameof(sessionActorRuntimeStore));

            IReadOnlyList<SessionActorRuntimeEntry> entries = sessionActorRuntimeStore.GetAllEntries();
            ReleaseSessionScopedActors(identity, entries, sessionActorRuntimeStore, null, null, source, reason, emitFacts: false);
        }

        public static void ReleaseIndexedRouteScopedPlayerActors(
            ActivityPlayerActorRegistry activityPlayerActorRegistry,
            string source,
            string reason)
        {
            activityPlayerActorRegistry = activityPlayerActorRegistry ?? throw new ArgumentNullException(nameof(activityPlayerActorRegistry));

            IReadOnlyList<PlayerActorRuntimeHandle> handles = activityPlayerActorRegistry.GetIndexedRouteScopedHandles();
            for (int index = 0; index < handles.Count; index++)
            {
                var handle = handles[index];
                if (!handle.IsValid || handle.Instance == null)
                {
                    continue;
                }

                Object.Destroy(handle.Instance);
                DebugUtility.LogVerbose(typeof(SessionActivityActorRuntimeReleaseStage),
                    $"event='ActorLifetimeReleased' owner='{Owner}' macroLifecycleOwner='{MacroLifecycleOwner}' activityId='{Normalize(handle.ActorIdentity.Identity.ActivityId)}' entrySequence='{handle.ActorIdentity.Identity.EntrySequence}' trigger='RouteScopedIndexReset' actorId='{handle.ActorId}' actorInstanceRuntimeId='{handle.ActorInstanceRuntimeId}' actorScope='RouteScoped' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                    DebugUtility.Colors.Success);
            }
        }

        private static void ReleaseSessionScopedActors(
            SessionActivityIdentity identity,
            IReadOnlyList<SessionActorRuntimeEntry> entries,
            SessionActorRuntimeStore sessionActorRuntimeStore,
            SessionActivityRuntimeState runtimeState,
            List<SessionActivityFact> facts,
            string source,
            string reason,
            bool emitFacts)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (!entry.IsValid)
                {
                    continue;
                }

                DebugUtility.LogVerbose(typeof(SessionActivityActorRuntimeReleaseStage),
                    $"event='ActorLifetimeDecisionResolved' owner='{Owner}' macroLifecycleOwner='{MacroLifecycleOwner}' activityId='{Normalize(identity.ActivityId)}' entrySequence='{identity.EntrySequence}' trigger='SessionReset' actorId='{entry.ActorId}' actorInstanceRuntimeId='{entry.ActorInstanceRuntimeId}' actorScope='{entry.ActorScope}' decision='Release' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                    DebugUtility.Colors.Info);
                if (emitFacts)
                {
                    EmitFact(
                        facts,
                        runtimeState,
                        SessionActivityFactKind.ActorLifetimeDecisionResolved,
                        identity,
                        source,
                        reason,
                        $"Actor lifetime decision resolved actorId='{entry.ActorId}' actorInstanceRuntimeId='{entry.ActorInstanceRuntimeId}' actorScope='{entry.ActorScope}' trigger='SessionReset' decision='Release'.");
                }

                if (entry.Instance != null)
                {
                    Object.Destroy(entry.Instance);
                }

                sessionActorRuntimeStore.Remove(entry.ActorInstanceRuntimeId);
                DebugUtility.LogVerbose(typeof(SessionActivityActorRuntimeReleaseStage),
                    $"event='ActorLifetimeReleased' owner='{Owner}' macroLifecycleOwner='{MacroLifecycleOwner}' activityId='{Normalize(identity.ActivityId)}' entrySequence='{identity.EntrySequence}' trigger='SessionReset' actorId='{entry.ActorId}' actorInstanceRuntimeId='{entry.ActorInstanceRuntimeId}' actorScope='{entry.ActorScope}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                    DebugUtility.Colors.Success);
                if (emitFacts)
                {
                    EmitFact(
                        facts,
                        runtimeState,
                        SessionActivityFactKind.ActorLifetimeReleased,
                        identity,
                        source,
                        reason,
                        $"Actor lifetime released actorId='{entry.ActorId}' actorInstanceRuntimeId='{entry.ActorInstanceRuntimeId}' actorScope='{entry.ActorScope}' trigger='SessionReset'.");
                }
            }
        }

        private static void EmitFact(
            List<SessionActivityFact> facts,
            SessionActivityRuntimeState runtimeState,
            SessionActivityFactKind kind,
            SessionActivityIdentity identity,
            string source,
            string reason,
            string message)
        {
            SessionActivityFact fact = new(kind, identity, source, reason, message, default);
            if (!fact.IsValid)
            {
                throw new InvalidOperationException($"Cannot emit invalid actor runtime release fact '{kind}'.");
            }

            facts.Add(fact);
            runtimeState.AppendFact(fact);
            runtimeState.AppendTrace($"fact='{fact.Kind}' stage='{fact.Identity.Stage}' entrySequence='{fact.Identity.EntrySequence}' activity='{fact.Identity.ActivityId}' executionState='{runtimeState.CurrentExecutionState}' message=\"{fact.Message}\"");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
