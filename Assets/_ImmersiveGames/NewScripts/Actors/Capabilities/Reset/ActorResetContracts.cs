using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Capabilities.Reset
{
    public readonly struct ActorResetActorRef
    {
        public ActorResetActorRef(
            SessionActivityIdentity identity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorKind actorKind,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId)
        {
            Identity = identity;
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ActorKind = actorKind;
            PlayerActorId = playerActorId;
            PlayerSlotId = playerSlotId;
        }

        public SessionActivityIdentity Identity { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorKind ActorKind { get; }
        public PlayerActorId PlayerActorId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public bool IsPlayer => ActorKind == ActorKind.Player;
        public bool IsValid =>
            Identity.IsValid &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            ActorKind != ActorKind.Unknown &&
            (!IsPlayer || (PlayerActorId.IsValid && PlayerSlotId.IsValid));
    }

    public enum ActorResetGroup
    {
        Unknown = 0,
        Placement = 1,
        ActivityParticipation = 2,
        MovementTransient = 3,
    }

    public readonly struct ActorResetTargetRef
    {
        public ActorResetTargetRef(
            ActorResetActorRef actor,
            IReadOnlyList<ActorResetGroup> groups,
            string placementId,
            bool placementDeclared,
            bool placementRequired,
            bool placementOptional,
            bool hasPlacement,
            Vector3 placementPosition,
            Vector3 placementEulerAngles)
        {
            Actor = actor;
            Groups = groups ?? Array.Empty<ActorResetGroup>();
            PlacementId = Normalize(placementId);
            PlacementDeclared = placementDeclared;
            PlacementRequired = placementRequired;
            PlacementOptional = placementOptional;
            HasPlacement = hasPlacement;
            PlacementPosition = placementPosition;
            PlacementEulerAngles = placementEulerAngles;
        }

        public ActorResetActorRef Actor { get; }
        public IReadOnlyList<ActorResetGroup> Groups { get; }
        public string PlacementId { get; }
        public bool PlacementDeclared { get; }
        public bool PlacementRequired { get; }
        public bool PlacementOptional { get; }
        public bool HasPlacement { get; }
        public Vector3 PlacementPosition { get; }
        public Vector3 PlacementEulerAngles { get; }

        public bool IsValid => Actor.IsValid && Groups != null && Groups.Count > 0;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorResetContext
    {
        public ActorResetContext(
            SessionActivityIdentity pipelineIdentity,
            ActorResetActorRef actor,
            ActorResetGroup group,
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
            Actor = actor;
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
        public ActorResetActorRef Actor { get; }
        public ActorResetGroup Group { get; }
        public string PlacementId { get; }
        public bool HasPlacement { get; }
        public bool PlacementRequired { get; }
        public bool PlacementOptional { get; }
        public bool PlacementDeclared { get; }
        public Vector3 PlacementPosition { get; }
        public Vector3 PlacementEulerAngles { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => PipelineIdentity.IsValid && Actor.IsValid && Group != ActorResetGroup.Unknown;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public interface IActorResetEndpoint
    {
        bool Supports(ActorResetGroup group);
        void ApplyReset(ActorResetContext context);
    }

    public readonly struct ActorResetCommand
    {
        public ActorResetCommand(
            SessionActivityIdentity pipelineIdentity,
            IReadOnlyList<ActorResetTargetRef> targets,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            Targets = targets ?? Array.Empty<ActorResetTargetRef>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public IReadOnlyList<ActorResetTargetRef> Targets { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => PipelineIdentity.IsValid && Targets != null;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorResetSkippedGroupReason
    {
        public ActorResetSkippedGroupReason(ActorResetGroup group, string reasonCode)
        {
            Group = group;
            ReasonCode = Normalize(reasonCode);
        }

        public ActorResetGroup Group { get; }
        public string ReasonCode { get; }
        public bool IsValid => Group != ActorResetGroup.Unknown && !string.IsNullOrWhiteSpace(ReasonCode);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorResetResult
    {
        public ActorResetResult(
            ActorResetActorRef actor,
            IReadOnlyList<ActorResetGroup> appliedGroups,
            IReadOnlyList<ActorResetGroup> skippedGroups,
            IReadOnlyList<ActorResetSkippedGroupReason> skippedGroupReasons)
        {
            Actor = actor;
            AppliedGroups = appliedGroups ?? Array.Empty<ActorResetGroup>();
            SkippedGroups = skippedGroups ?? Array.Empty<ActorResetGroup>();
            SkippedGroupReasons = skippedGroupReasons ?? Array.Empty<ActorResetSkippedGroupReason>();
        }

        public ActorResetActorRef Actor { get; }
        public IReadOnlyList<ActorResetGroup> AppliedGroups { get; }
        public IReadOnlyList<ActorResetGroup> SkippedGroups { get; }
        public IReadOnlyList<ActorResetSkippedGroupReason> SkippedGroupReasons { get; }
        public bool IsValid => Actor.IsValid && AppliedGroups != null && SkippedGroups != null && SkippedGroupReasons != null;
    }

    public interface IActorResetAdapter
    {
        IReadOnlyList<ActorResetResult> Execute(
            ActorResetCommand command,
            SessionActivityIdentity activeIdentity);
    }
}
