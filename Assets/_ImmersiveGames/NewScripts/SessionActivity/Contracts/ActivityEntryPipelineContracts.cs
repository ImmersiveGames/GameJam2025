using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public readonly struct ActivityEntryCommand
    {
        public ActivityEntryCommand(
            SessionActivityIdentity identity,
            SessionActivityDefinition definition,
            string source,
            string reason)
        {
            Identity = identity;
            Definition = definition;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public SessionActivityDefinition Definition { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Definition.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            string.Equals(Identity.ActivityId, Definition.ActivityId, StringComparison.Ordinal) &&
            Identity.ActivityOrdinal == Definition.ActivityOrdinal &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryResult
    {
        public ActivityEntryResult(
            bool accepted,
            SessionActivityIdentity identity,
            string reason)
        {
            Accepted = accepted;
            Identity = identity;
            Reason = Normalize(reason);
        }

        public bool Accepted { get; }
        public SessionActivityIdentity Identity { get; }
        public string Reason { get; }

        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryContentLoadCommand
    {
        public ActivityEntryContentLoadCommand(
            SessionActivityIdentity identity,
            SessionActivityDefinition definition,
            string source,
            string reason)
        {
            Identity = identity;
            Definition = definition;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public SessionActivityDefinition Definition { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => Identity.IsValid && Definition.IsValid && !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryContentLoadCompletionCommand
    {
        public ActivityEntryContentLoadCompletionCommand(
            SessionActivityIdentity activeIdentity,
            SessionActivityDefinition definition,
            SessionActivityPendingOperation operation,
            string source,
            string reason)
        {
            ActiveIdentity = activeIdentity;
            Definition = definition;
            Operation = operation;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity ActiveIdentity { get; }
        public SessionActivityDefinition Definition { get; }
        public SessionActivityPendingOperation Operation { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => ActiveIdentity.IsValid && Definition.IsValid && Operation.IsValid && !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryContentLoadFailureCommand
    {
        public ActivityEntryContentLoadFailureCommand(
            SessionActivityIdentity activeIdentity,
            SessionActivityDefinition definition,
            SessionActivityPendingOperation operation,
            string source,
            string reason,
            string error)
        {
            ActiveIdentity = activeIdentity;
            Definition = definition;
            Operation = operation;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Error = Normalize(error);
        }

        public SessionActivityIdentity ActiveIdentity { get; }
        public SessionActivityDefinition Definition { get; }
        public SessionActivityPendingOperation Operation { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Error { get; }
        public bool IsValid => ActiveIdentity.IsValid && Definition.IsValid && Operation.IsValid && !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryContentLoadResult
    {
        public ActivityEntryContentLoadResult(
            bool shouldContinueEntry,
            bool pendingOperationIssued,
            string reason)
        {
            ShouldContinueEntry = shouldContinueEntry;
            PendingOperationIssued = pendingOperationIssued;
            Reason = Normalize(reason);
        }

        public bool ShouldContinueEntry { get; }
        public bool PendingOperationIssued { get; }
        public string Reason { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryObjectSetupCommand
    {
        public ActivityEntryObjectSetupCommand(
            SessionActivityIdentity identity,
            SessionActivityDefinition definition,
            string source,
            string reason)
        {
            Identity = identity;
            Definition = definition;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public SessionActivityDefinition Definition { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Definition.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            string.Equals(Identity.ActivityId, Definition.ActivityId, StringComparison.Ordinal) &&
            Identity.ActivityOrdinal == Definition.ActivityOrdinal &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryObjectSetupResult
    {
        public ActivityEntryObjectSetupResult(
            bool completed,
            SessionActivityIdentity identity,
            string reason)
        {
            Completed = completed;
            Identity = identity;
            Reason = Normalize(reason);
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public string Reason { get; }

        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }


    public readonly struct ActivityEntryActorPresentationSetupCommand
    {
        public ActivityEntryActorPresentationSetupCommand(
            SessionActivityIdentity identity,
            SessionActivityDefinition definition,
            string source,
            string reason)
        {
            Identity = identity;
            Definition = definition;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public SessionActivityDefinition Definition { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Definition.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            string.Equals(Identity.ActivityId, Definition.ActivityId, StringComparison.Ordinal) &&
            Identity.ActivityOrdinal == Definition.ActivityOrdinal &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryActorPresentationSetupResult
    {
        public ActivityEntryActorPresentationSetupResult(
            bool completed,
            SessionActivityIdentity identity,
            int total,
            int resolved,
            int materialized,
            int retained,
            int skipped,
            string reason)
        {
            Completed = completed;
            Identity = identity;
            Total = total < 0 ? 0 : total;
            Resolved = resolved < 0 ? 0 : resolved;
            Materialized = materialized < 0 ? 0 : materialized;
            Retained = retained < 0 ? 0 : retained;
            Skipped = skipped < 0 ? 0 : skipped;
            Reason = Normalize(reason);
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public int Total { get; }
        public int Resolved { get; }
        public int Materialized { get; }
        public int Retained { get; }
        public int Skipped { get; }
        public string Reason { get; }

        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IActivityEntryRuntimeEndpoint : IActivityEntryPipelineBoundary
    {
        SessionActivityIdentity BuildIdentity(SessionActivityDefinition definition, SessionActivityStage stage, int entrySequence);
        void SetCurrentIdentity(SessionActivityIdentity identity, SessionActivityStage stage);
        void EmitFact(
            List<SessionActivityFact> emittedFacts,
            SessionActivityFactKind kind,
            SessionActivityIdentity identity,
            string source,
            string reason,
            string message);
        void EmitSnapshot(
            List<SessionActivitySnapshot> emittedSnapshots,
            string snapshotKind,
            string source,
            string reason,
            string message);
        void SetCurrentActivityContentLoadedSet(ActivityContentLoadedSet loadedSet);
        SessionActivityPendingOperation BuildActivityContentPendingOperation(
            SessionActivityDefinition definition,
            int entrySequence,
            ActivityContentSceneLoadCommand command);
        void SetPendingOperation(SessionActivityPendingOperation operation);
        void RunActivityContentOperation(
            SessionActivityPendingOperation operation,
            ActivityContentSceneLoadCommand command);

        void LogEntryOwnerEvent(
            string eventName,
            SessionActivityIdentity identity,
            string source,
            string reason,
            string detail = "");

        void LogPhaseBoundary(
            string phaseName,
            SessionActivityIdentity identity,
            string source,
            string reason,
            bool completed = false,
            string detail = "");

        void ClearCurrentActivityContentLoadedSet();
        void ClearCurrentActivityObjectContributorDiscoveryResult();
        void ClearCurrentActivitySetupInventory();
        void ClearCurrentActorInventoryFeedResult();
    }

    // Bridge transitoria SA-3B0: expõe apenas state técnico canônico e actor scan targets
    // enquanto o subfluxo é transferido do SessionActivityPipeline para o ActivityEntryPipeline.
    public interface IActivityEntryObjectSetupRuntimeBridge
    {
        ActivityContentLoadedSet GetCurrentActivityContentLoadedSet();
        ActivityObjectContributorDiscoveryResult GetCurrentActivityObjectContributorDiscoveryResult();
        void SetCurrentActivityObjectContributorDiscoveryResult(ActivityObjectContributorDiscoveryResult result);
        void SetCurrentActivitySetupInventory(ActivitySetupInventory inventory);
        void SetCurrentActivityCapabilityInventoryPreview(
            ActivityCapabilityInventory inventory,
            ActivityCapabilityInventoryValidationResult validation);
        void ClearCurrentActivityCapabilityInventoryPreview();
    }

    public interface IActivityEntryActorInventoryRuntimeBridge
    {
        ActivityNonPlayerActorRegistry GetActivitySceneActorRegistry();
        ActivityPlayerActorRegistry GetActivityPlayerActorRegistry();
        IReadOnlyList<PlayerActorIdentityRecord> ResolvePlayerActorCapabilityTargetsForCurrentEntry(SessionActivityIdentity identity);
        ActorInventoryFeedResult GetCurrentActorInventoryFeedResult();
        void SetCurrentActorInventoryFeedResult(ActorInventoryFeedResult result);
        void ClearCurrentActorInventoryFeedResult();
    }



    public interface IActivityEntryActorPresentationRuntimeBridge
    {
        ActivityCapabilityInventory GetCurrentActivityCapabilityInventoryPreview();
        bool TryGetActiveActorPresentationHandle(
            ActorPresentationEndpointReference presentationReference,
            out ActorPresentationRuntimeHandle handle);
        void StoreActiveActorPresentationHandle(
            SessionActivityIdentity identity,
            ActorPresentationEndpointReference presentationReference,
            ActorPresentationRuntimeHandle handle);
        void SyncActiveActorPresentationHandle(
            SessionActivityIdentity identity,
            ActorPresentationEndpointReference presentationReference,
            ActorPresentationRuntimeHandle handle);
        void ReleaseActorPresentationBeforeRematerialization(
            ActivityEntryActorPresentationSetupCommand command,
            ActorInstanceId actorInstanceRuntimeId,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
    }

    public interface IActivityEntryPipeline
    {
        Task<ActivityEntryResult> ExecuteAsync(ActivityEntryCommand command, CancellationToken cancellationToken = default);
        ActivityEntryContentLoadResult BeginContentLoad(
            ActivityEntryContentLoadCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryContentLoadResult CompleteContentLoad(
            ActivityEntryContentLoadCompletionCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryObjectSetupResult ExecuteSetupInfrastructure(
            ActivityEntryObjectSetupCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryObjectSetupResult ExecuteCapabilityObjectSetup(
            ActivityEntryObjectSetupCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryActorPresentationSetupResult ExecuteActorPresentationSetup(
            ActivityEntryActorPresentationSetupCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        void FailContentLoad(ActivityEntryContentLoadFailureCommand command, List<SessionActivityFact> facts);
        void ResetState();
    }
}
