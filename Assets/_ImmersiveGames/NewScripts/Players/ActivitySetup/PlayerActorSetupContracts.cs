using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Players.ActivitySetup
{
    public enum PlayerSelectionSnapshotSource
    {
        Unknown = 0,
        ExplicitPayload = 1,
        MvpDefaultFromPlayerPreparation = 2,
    }

    public enum PlayerActorReadyStage
    {
        Unknown = 0,
        MaterializedOnly = 1,
        RetainedForActivity = 2,
    }

    public readonly struct PlayerSelectionEntry
    {
        public PlayerSelectionEntry(string playerId, bool required)
        {
            PlayerId = Normalize(playerId);
            Required = required;
        }

        public string PlayerId { get; }
        public bool Required { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(PlayerId);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct PlayerSelectionSnapshot
    {
        public PlayerSelectionSnapshot(
            SessionActivityIdentity identity,
            IReadOnlyList<PlayerSelectionEntry> entries,
            PlayerSelectionSnapshotSource selectionSource,
            int ignoredOptionalPlayersCount,
            string source,
            string reason)
        {
            Identity = identity;
            Entries = entries ?? Array.Empty<PlayerSelectionEntry>();
            SelectionSource = selectionSource;
            IgnoredOptionalPlayersCount = ignoredOptionalPlayersCount < 0 ? 0 : ignoredOptionalPlayersCount;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public IReadOnlyList<PlayerSelectionEntry> Entries { get; }
        public PlayerSelectionSnapshotSource SelectionSource { get; }
        public int IgnoredOptionalPlayersCount { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid =>
            Identity.IsValid &&
            Entries != null &&
            SelectionSource != PlayerSelectionSnapshotSource.Unknown;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct PlayerActorIdentityRecord
    {
        public PlayerActorIdentityRecord(
            SessionActivityIdentity identity,
            string playerId,
            string playerActorId)
        {
            Identity = identity;
            PlayerId = Normalize(playerId);
            PlayerActorId = Normalize(playerActorId);
        }

        public SessionActivityIdentity Identity { get; }
        public string PlayerId { get; }
        public string PlayerActorId { get; }

        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(PlayerId) && !string.IsNullOrWhiteSpace(PlayerActorId);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct PlayerActorEntryPlan
    {
        public PlayerActorEntryPlan(
            PlayerActorIdentityRecord actorIdentity,
            GameObject prefab,
            Vector3 localPosition,
            Vector3 localEulerAngles)
        {
            ActorIdentity = actorIdentity;
            Prefab = prefab;
            LocalPosition = localPosition;
            LocalEulerAngles = localEulerAngles;
        }

        public PlayerActorIdentityRecord ActorIdentity { get; }
        public GameObject Prefab { get; }
        public Vector3 LocalPosition { get; }
        public Vector3 LocalEulerAngles { get; }
        public bool IsValid => ActorIdentity.IsValid && Prefab != null;
    }

    public readonly struct PlayerActorResetPlan
    {
        public PlayerActorResetPlan(
            PlayerActorIdentityRecord actorIdentity,
            IReadOnlyList<PlayerActorResetGroup> groups,
            bool placementDeclared,
            bool placementRequired,
            bool placementOptional,
            bool hasPlacement,
            Vector3 placementLocalPosition,
            Vector3 placementLocalEulerAngles)
        {
            ActorIdentity = actorIdentity;
            Groups = groups ?? Array.Empty<PlayerActorResetGroup>();
            PlacementDeclared = placementDeclared;
            PlacementRequired = placementRequired;
            PlacementOptional = placementOptional;
            HasPlacement = hasPlacement;
            PlacementLocalPosition = placementLocalPosition;
            PlacementLocalEulerAngles = placementLocalEulerAngles;
        }

        public PlayerActorIdentityRecord ActorIdentity { get; }
        public IReadOnlyList<PlayerActorResetGroup> Groups { get; }
        public bool PlacementDeclared { get; }
        public bool PlacementRequired { get; }
        public bool PlacementOptional { get; }
        public bool HasPlacement { get; }
        public Vector3 PlacementLocalPosition { get; }
        public Vector3 PlacementLocalEulerAngles { get; }
        public bool IsValid => ActorIdentity.IsValid && Groups != null && Groups.Count > 0;
    }

    public enum PlayerActorResetGroup
    {
        Unknown = 0,
        Placement = 1,
        ActivityParticipation = 2,
        MovementTransient = 3,
    }

    public readonly struct PlayerActorResetContext
    {
        public PlayerActorResetContext(
            SessionActivityIdentity pipelineIdentity,
            PlayerActorIdentityRecord actorIdentity,
            PlayerActorResetGroup group,
            bool hasPlacement,
            bool placementRequired,
            bool placementOptional,
            bool placementDeclared,
            Vector3 placementLocalPosition,
            Vector3 placementLocalEulerAngles,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            ActorIdentity = actorIdentity;
            Group = group;
            HasPlacement = hasPlacement;
            PlacementRequired = placementRequired;
            PlacementOptional = placementOptional;
            PlacementDeclared = placementDeclared;
            PlacementLocalPosition = placementLocalPosition;
            PlacementLocalEulerAngles = placementLocalEulerAngles;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public PlayerActorIdentityRecord ActorIdentity { get; }
        public PlayerActorResetGroup Group { get; }
        public bool HasPlacement { get; }
        public bool PlacementRequired { get; }
        public bool PlacementOptional { get; }
        public bool PlacementDeclared { get; }
        public Vector3 PlacementLocalPosition { get; }
        public Vector3 PlacementLocalEulerAngles { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => PipelineIdentity.IsValid && ActorIdentity.IsValid && Group != PlayerActorResetGroup.Unknown;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public interface IPlayerActorResetEndpoint
    {
        bool Supports(PlayerActorResetGroup group);
        void ApplyReset(PlayerActorResetContext context);
    }

    public readonly struct PlayerActorResetCommand
    {
        public PlayerActorResetCommand(
            SessionActivityIdentity pipelineIdentity,
            IReadOnlyList<PlayerActorResetPlan> plans,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            Plans = plans ?? Array.Empty<PlayerActorResetPlan>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public IReadOnlyList<PlayerActorResetPlan> Plans { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => PipelineIdentity.IsValid && Plans != null;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct PlayerActorResetAppliedRecord
    {
        public PlayerActorResetAppliedRecord(
            PlayerActorIdentityRecord actorIdentity,
            IReadOnlyList<PlayerActorResetGroup> appliedGroups,
            IReadOnlyList<PlayerActorResetGroup> skippedGroups,
            IReadOnlyList<PlayerActorResetSkippedGroupReason> skippedGroupReasons)
        {
            ActorIdentity = actorIdentity;
            AppliedGroups = appliedGroups ?? Array.Empty<PlayerActorResetGroup>();
            SkippedGroups = skippedGroups ?? Array.Empty<PlayerActorResetGroup>();
            SkippedGroupReasons = skippedGroupReasons ?? Array.Empty<PlayerActorResetSkippedGroupReason>();
        }

        public PlayerActorIdentityRecord ActorIdentity { get; }
        public IReadOnlyList<PlayerActorResetGroup> AppliedGroups { get; }
        public IReadOnlyList<PlayerActorResetGroup> SkippedGroups { get; }
        public IReadOnlyList<PlayerActorResetSkippedGroupReason> SkippedGroupReasons { get; }
        public bool IsValid => ActorIdentity.IsValid && AppliedGroups != null && SkippedGroups != null && SkippedGroupReasons != null;
    }

    public readonly struct PlayerActorResetSkippedGroupReason
    {
        public PlayerActorResetSkippedGroupReason(PlayerActorResetGroup group, string reasonCode)
        {
            Group = group;
            ReasonCode = Normalize(reasonCode);
        }

        public PlayerActorResetGroup Group { get; }
        public string ReasonCode { get; }
        public bool IsValid => Group != PlayerActorResetGroup.Unknown && !string.IsNullOrWhiteSpace(ReasonCode);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct PlayerActorActivityParticipationPlan
    {
        public PlayerActorActivityParticipationPlan(PlayerActorIdentityRecord actorIdentity)
        {
            ActorIdentity = actorIdentity;
        }

        public PlayerActorIdentityRecord ActorIdentity { get; }
        public bool IsValid => ActorIdentity.IsValid;
    }

    public readonly struct PlayerActorMaterializationCommand
    {
        public PlayerActorMaterializationCommand(
            SessionActivityIdentity pipelineIdentity,
            IReadOnlyList<PlayerActorEntryPlan> entries,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            Entries = entries ?? Array.Empty<PlayerActorEntryPlan>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public IReadOnlyList<PlayerActorEntryPlan> Entries { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => PipelineIdentity.IsValid && Entries != null;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct PlayerActorMaterializationRecord
    {
        public PlayerActorMaterializationRecord(
            PlayerActorIdentityRecord actorIdentity,
            GameObject instance)
        {
            ActorIdentity = actorIdentity;
            Instance = instance;
        }

        public PlayerActorIdentityRecord ActorIdentity { get; }
        public GameObject Instance { get; }
        public bool IsValid => ActorIdentity.IsValid && Instance != null;
    }

    public interface IPlayerActorMaterializationAdapter
    {
        IReadOnlyList<PlayerActorMaterializationRecord> Execute(PlayerActorMaterializationCommand command, SessionActivityIdentity activeIdentity);
    }

    public interface IPlayerActorResetAdapter
    {
        IReadOnlyList<PlayerActorResetAppliedRecord> Execute(
            PlayerActorResetCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry);
    }

    public readonly struct PlayerActorParticipationExitCommand
    {
        public PlayerActorParticipationExitCommand(
            SessionActivityIdentity pipelineIdentity,
            IReadOnlyList<PlayerActorIdentityRecord> actors,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            Actors = actors ?? Array.Empty<PlayerActorIdentityRecord>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public IReadOnlyList<PlayerActorIdentityRecord> Actors { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => PipelineIdentity.IsValid && Actors != null;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct PlayerActorParticipationExitRecord
    {
        public PlayerActorParticipationExitRecord(
            PlayerActorIdentityRecord actorIdentity,
            bool exited,
            bool retainedForRoute)
        {
            ActorIdentity = actorIdentity;
            Exited = exited;
            RetainedForRoute = retainedForRoute;
        }

        public PlayerActorIdentityRecord ActorIdentity { get; }
        public bool Exited { get; }
        public bool RetainedForRoute { get; }
        public bool IsValid => ActorIdentity.IsValid && Exited && RetainedForRoute;
    }

    public interface IPlayerActorParticipationAdapter
    {
        IReadOnlyList<PlayerActorParticipationExitRecord> Execute(
            PlayerActorParticipationExitCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry);

        IReadOnlyList<PlayerActorParticipationEnterRecord> Execute(
            PlayerActorParticipationEnterCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry);
    }

    public readonly struct PlayerActorParticipationEnterCommand
    {
        public PlayerActorParticipationEnterCommand(
            SessionActivityIdentity pipelineIdentity,
            IReadOnlyList<PlayerActorIdentityRecord> actors,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            Actors = actors ?? Array.Empty<PlayerActorIdentityRecord>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public IReadOnlyList<PlayerActorIdentityRecord> Actors { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => PipelineIdentity.IsValid && Actors != null;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct PlayerActorParticipationEnterRecord
    {
        public PlayerActorParticipationEnterRecord(
            PlayerActorIdentityRecord actorIdentity,
            bool entered,
            bool retainedForRoute)
        {
            ActorIdentity = actorIdentity;
            Entered = entered;
            RetainedForRoute = retainedForRoute;
        }

        public PlayerActorIdentityRecord ActorIdentity { get; }
        public bool Entered { get; }
        public bool RetainedForRoute { get; }
        public bool IsValid => ActorIdentity.IsValid && Entered && RetainedForRoute;
    }
}
