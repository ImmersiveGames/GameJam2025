using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Capabilities.Reset
{
    public readonly struct ActorResetContext
    {
        public ActorResetContext(
            SessionActivityIdentity pipelineIdentity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorKind actorKind,
            ActivityResetIntent resetIntent,
            ActivityResetStateProfileKind stateProfileKind,
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
            ResetIntent = resetIntent;
            StateProfileKind = stateProfileKind;
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
        public ActivityResetIntent ResetIntent { get; }
        public ActivityResetStateProfileKind StateProfileKind { get; }
        public string PlacementId { get; }
        public bool HasPlacement { get; }
        public bool PlacementRequired { get; }
        public bool PlacementOptional { get; }
        public bool PlacementDeclared { get; }
        public Vector3 PlacementPosition { get; }
        public Vector3 PlacementEulerAngles { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            PipelineIdentity.IsValid &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            ActorKind != global::_ImmersiveGames.NewScripts.Actors.Foundation.ActorKind.Unknown &&
            ResetIntent != ActivityResetIntent.Unknown &&
            StateProfileKind != ActivityResetStateProfileKind.Unknown;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public interface IActorResetEndpoint
    {
    }

    public interface IActorPlacementResetEndpoint : IActorResetEndpoint
    {
    }

    public interface IActorEntryInitializeResetEndpoint : IActorResetEndpoint
    {
        void ApplyEntryInitializeReset(ActorResetContext context);
    }

    public interface IActorRuntimeLocalResetEndpoint : IActorResetEndpoint
    {
        void ApplyRuntimeLocalReset(ActorResetContext context);
    }

    public interface IActorRuntimeActivityResetEndpoint : IActorResetEndpoint
    {
        void ApplyRuntimeActivityReset(ActorResetContext context);
    }

    public interface IActorRuntimeActivityTransitionResetEndpoint : IActorResetEndpoint
    {
        void ApplyRuntimeActivityTransitionReset(ActorResetContext context);
    }

    public interface IActorRuntimeRouteTransitionResetEndpoint : IActorResetEndpoint
    {
        void ApplyRuntimeRouteTransitionReset(ActorResetContext context);
    }

    public readonly struct ActorResetSkippedReferenceReason
    {
        public ActorResetSkippedReferenceReason(string capabilityId, string reasonCode)
        {
            CapabilityId = Normalize(capabilityId);
            ReasonCode = Normalize(reasonCode);
        }

        public string CapabilityId { get; }
        public string ReasonCode { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(CapabilityId) && !string.IsNullOrWhiteSpace(ReasonCode);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorResetResult
    {
        public ActorResetResult(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorKind actorKind,
            int appliedReferenceCount,
            int skippedReferenceCount,
            IReadOnlyList<ActorResetSkippedReferenceReason> skippedReferenceReasons)
        {
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ActorKind = actorKind;
            AppliedReferenceCount = Math.Max(0, appliedReferenceCount);
            SkippedReferenceCount = Math.Max(0, skippedReferenceCount);
            SkippedReferenceReasons = skippedReferenceReasons ?? Array.Empty<ActorResetSkippedReferenceReason>();
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorKind ActorKind { get; }
        public int AppliedReferenceCount { get; }
        public int SkippedReferenceCount { get; }
        public IReadOnlyList<ActorResetSkippedReferenceReason> SkippedReferenceReasons { get; }
        public bool IsValid => ActorId.IsValid && ActorInstanceRuntimeId.IsValid && ActorKind != global::_ImmersiveGames.NewScripts.Actors.Foundation.ActorKind.Unknown && AppliedReferenceCount >= 0 && SkippedReferenceCount >= 0 && SkippedReferenceReasons != null;
    }

    public interface IActorResetAdapter
    {
        IReadOnlyList<ActorResetResult> Execute(
            ActivityParticipantResetCommand command,
            SessionActivityIdentity activeIdentity);
    }
}
