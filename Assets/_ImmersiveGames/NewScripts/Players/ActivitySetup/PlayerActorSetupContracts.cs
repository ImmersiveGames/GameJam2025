using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Players.ActivitySetup
{
    public readonly struct PlayerActorIdentityRecord
    {
        public PlayerActorIdentityRecord(
            SessionActivityIdentity identity,
            string playerSlotId,
            string playerActorId)
        {
            Identity = identity;
            PlayerSlotId = Normalize(playerSlotId);
            PlayerActorId = Normalize(playerActorId);
        }

        public SessionActivityIdentity Identity { get; }
        public string PlayerSlotId { get; }
        public string PlayerActorId { get; }

        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(PlayerSlotId) && !string.IsNullOrWhiteSpace(PlayerActorId);

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
            string placementId,
            bool placementDeclared,
            bool placementRequired,
            bool placementOptional,
            bool hasPlacement,
            Vector3 placementPosition,
            Vector3 placementEulerAngles)
        {
            ActorIdentity = actorIdentity;
            Groups = groups ?? Array.Empty<PlayerActorResetGroup>();
            PlacementId = Normalize(placementId);
            PlacementDeclared = placementDeclared;
            PlacementRequired = placementRequired;
            PlacementOptional = placementOptional;
            HasPlacement = hasPlacement;
            PlacementPosition = placementPosition;
            PlacementEulerAngles = placementEulerAngles;
        }

        public PlayerActorIdentityRecord ActorIdentity { get; }
        public IReadOnlyList<PlayerActorResetGroup> Groups { get; }
        public string PlacementId { get; }
        public bool PlacementDeclared { get; }
        public bool PlacementRequired { get; }
        public bool PlacementOptional { get; }
        public bool HasPlacement { get; }
        public Vector3 PlacementPosition { get; }
        public Vector3 PlacementEulerAngles { get; }
        public bool IsValid => ActorIdentity.IsValid && Groups != null && Groups.Count > 0;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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
            string placementId,
            bool hasPlacement,
            bool placementRequired,
            bool placementOptional,
            bool placementDeclared,
            Vector3 placementPosition,
            Vector3 placementEulerAngles,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            ActorIdentity = actorIdentity;
            Group = group;
            PlacementId = Normalize(placementId);
            HasPlacement = hasPlacement;
            PlacementRequired = placementRequired;
            PlacementOptional = placementOptional;
            PlacementDeclared = placementDeclared;
            PlacementPosition = placementPosition;
            PlacementEulerAngles = placementEulerAngles;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public PlayerActorIdentityRecord ActorIdentity { get; }
        public PlayerActorResetGroup Group { get; }
        public string PlacementId { get; }
        public bool HasPlacement { get; }
        public bool PlacementRequired { get; }
        public bool PlacementOptional { get; }
        public bool PlacementDeclared { get; }
        public Vector3 PlacementPosition { get; }
        public Vector3 PlacementEulerAngles { get; }
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

    public readonly struct PlayerInputBindingRequirement
    {
        public PlayerInputBindingRequirement(
            SessionActivityIdentity identity,
            string requirementId,
            string playerSlotId,
            string playerActorId,
            string playerDefinitionId,
            bool required,
            string source,
            string reason)
        {
            Identity = identity;
            RequirementId = Normalize(requirementId);
            PlayerSlotId = Normalize(playerSlotId);
            PlayerActorId = Normalize(playerActorId);
            PlayerDefinitionId = Normalize(playerDefinitionId);
            Required = required;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string RequirementId { get; }
        public string PlayerSlotId { get; }
        public string PlayerActorId { get; }
        public string PlayerDefinitionId { get; }
        public bool Required { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(RequirementId) &&
            !string.IsNullOrWhiteSpace(PlayerSlotId) &&
            !string.IsNullOrWhiteSpace(PlayerActorId) &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct PlayerInputBindingCommand
    {
        public PlayerInputBindingCommand(
            SessionActivityIdentity pipelineIdentity,
            IReadOnlyList<PlayerInputBindingRequirement> requirements,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            Requirements = requirements ?? Array.Empty<PlayerInputBindingRequirement>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public IReadOnlyList<PlayerInputBindingRequirement> Requirements { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => PipelineIdentity.IsValid && Requirements != null && !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct PlayerInputBindingRecord
    {
        public PlayerInputBindingRecord(
            PlayerInputBindingRequirement requirement,
            bool bound,
            string observedInputId)
        {
            Requirement = requirement;
            Bound = bound;
            ObservedInputId = Normalize(observedInputId);
        }

        public PlayerInputBindingRequirement Requirement { get; }
        public bool Bound { get; }
        public string ObservedInputId { get; }
        public bool IsValid => Requirement.IsValid && Bound && !string.IsNullOrWhiteSpace(ObservedInputId);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public interface IPlayerInputBindingAdapter
    {
        IReadOnlyList<PlayerInputBindingRecord> Execute(
            PlayerInputBindingCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry);
    }

    public readonly struct MovementBindingRequirement
    {
        public MovementBindingRequirement(
            SessionActivityIdentity identity,
            string requirementId,
            string playerSlotId,
            string playerActorId,
            bool required,
            string source,
            string reason)
        {
            Identity = identity;
            RequirementId = Normalize(requirementId);
            PlayerSlotId = Normalize(playerSlotId);
            PlayerActorId = Normalize(playerActorId);
            Required = required;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string RequirementId { get; }
        public string PlayerSlotId { get; }
        public string PlayerActorId { get; }
        public bool Required { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(RequirementId) &&
            !string.IsNullOrWhiteSpace(PlayerSlotId) &&
            !string.IsNullOrWhiteSpace(PlayerActorId) &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct MovementBindingCommand
    {
        public MovementBindingCommand(
            SessionActivityIdentity pipelineIdentity,
            IReadOnlyList<MovementBindingRequirement> requirements,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            Requirements = requirements ?? Array.Empty<MovementBindingRequirement>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public IReadOnlyList<MovementBindingRequirement> Requirements { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => PipelineIdentity.IsValid && Requirements != null && !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct MovementBindingRecord
    {
        public MovementBindingRecord(
            MovementBindingRequirement requirement,
            bool bound,
            string observedEndpoint)
        {
            Requirement = requirement;
            Bound = bound;
            ObservedEndpoint = Normalize(observedEndpoint);
        }

        public MovementBindingRequirement Requirement { get; }
        public bool Bound { get; }
        public string ObservedEndpoint { get; }
        public bool IsValid => Requirement.IsValid && Bound && !string.IsNullOrWhiteSpace(ObservedEndpoint);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public interface IMovementBindingAdapter
    {
        IReadOnlyList<MovementBindingRecord> Execute(
            MovementBindingCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry);
    }

    public readonly struct MovementControlCommand
    {
        public MovementControlCommand(
            SessionActivityIdentity pipelineIdentity,
            IReadOnlyList<PlayerActorIdentityRecord> actors,
            bool enable,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            Actors = actors ?? Array.Empty<PlayerActorIdentityRecord>();
            Enable = enable;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public IReadOnlyList<PlayerActorIdentityRecord> Actors { get; }
        public bool Enable { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => PipelineIdentity.IsValid && Actors != null && !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct MovementControlRecord
    {
        public MovementControlRecord(
            PlayerActorIdentityRecord actorIdentity,
            bool enabled,
            string observedEndpoint)
        {
            ActorIdentity = actorIdentity;
            Enabled = enabled;
            ObservedEndpoint = Normalize(observedEndpoint);
        }

        public PlayerActorIdentityRecord ActorIdentity { get; }
        public bool Enabled { get; }
        public string ObservedEndpoint { get; }
        public bool IsValid => ActorIdentity.IsValid && !string.IsNullOrWhiteSpace(ObservedEndpoint);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public interface IPlayerMovementControlAdapter
    {
        IReadOnlyList<MovementControlRecord> Execute(
            MovementControlCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry);
    }
}
