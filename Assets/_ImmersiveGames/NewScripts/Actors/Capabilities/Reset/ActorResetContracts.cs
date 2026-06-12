using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Capabilities.Reset
{
    public enum ActorResetGroup
    {
        Unknown = 0,
        Placement = 1,
        ActivityParticipation = 2,
        MovementTransient = 3,
        SpawnedRuntimeObjects = 4,
    }

    public readonly struct ActorResetContext
    {
        public ActorResetContext(
            SessionActivityIdentity pipelineIdentity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorKind actorKind,
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
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ActorKind = actorKind;
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
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorKind ActorKind { get; }
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

        public bool IsValid => PipelineIdentity.IsValid && ActorId.IsValid && ActorInstanceRuntimeId.IsValid && ActorKind != global::_ImmersiveGames.NewScripts.Actors.Foundation.ActorKind.Unknown && Group != ActorResetGroup.Unknown;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public interface IActorResetEndpoint
    {
        bool Supports(ActorResetGroup group);
        void ApplyReset(ActorResetContext context);
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
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorKind actorKind,
            IReadOnlyList<ActorResetGroup> appliedGroups,
            IReadOnlyList<ActorResetGroup> skippedGroups,
            IReadOnlyList<ActorResetSkippedGroupReason> skippedGroupReasons)
        {
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ActorKind = actorKind;
            AppliedGroups = appliedGroups ?? Array.Empty<ActorResetGroup>();
            SkippedGroups = skippedGroups ?? Array.Empty<ActorResetGroup>();
            SkippedGroupReasons = skippedGroupReasons ?? Array.Empty<ActorResetSkippedGroupReason>();
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorKind ActorKind { get; }
        public IReadOnlyList<ActorResetGroup> AppliedGroups { get; }
        public IReadOnlyList<ActorResetGroup> SkippedGroups { get; }
        public IReadOnlyList<ActorResetSkippedGroupReason> SkippedGroupReasons { get; }
        public bool IsValid => ActorId.IsValid && ActorInstanceRuntimeId.IsValid && ActorKind != global::_ImmersiveGames.NewScripts.Actors.Foundation.ActorKind.Unknown && AppliedGroups != null && SkippedGroups != null && SkippedGroupReasons != null;
    }

    public interface IActorResetAdapter
    {
        IReadOnlyList<ActorResetResult> Execute(
            ActivityParticipantResetCommand command,
            SessionActivityIdentity activeIdentity);
    }
}
