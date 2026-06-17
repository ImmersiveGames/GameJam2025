using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityParticipantMaterializationNeedKind
    {
        Unknown = 0,
        EnsureRouteSessionParticipantAvailable = 1,
    }

    public readonly struct ActivityParticipantBindCommand
    {
        public ActivityParticipantBindCommand(
            SessionActivityIdentity identity,
            string requirementId,
            ActivityParticipantRequirementKind participantKind,
            SessionParticipantId requestedParticipantId,
            ActivityParticipantBinding participantBinding,
            string source,
            string reason)
        {
            Identity = identity;
            RequirementId = Normalize(requirementId);
            ParticipantKind = participantKind;
            RequestedParticipantId = requestedParticipantId;
            ParticipantBinding = participantBinding;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string RequirementId { get; }
        public ActivityParticipantRequirementKind ParticipantKind { get; }
        public SessionParticipantId RequestedParticipantId { get; }
        public ActivityParticipantBinding ParticipantBinding { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(RequirementId) &&
            ParticipantKind != ActivityParticipantRequirementKind.Unknown &&
            ParticipantBinding.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', requirementId='{RequirementId}', participantKind='{ParticipantKind}', requestedParticipantId='{FormatRequestedParticipantId(RequestedParticipantId)}', participantId='{ParticipantBinding.ParticipantId}', role='{ParticipantBinding.Role}', playerSlotId='{ParticipantBinding.PlayerSlotId}', actorDefinitionId='{ParticipantBinding.ActorDefinitionId}', actorId='{ParticipantBinding.ActorId}', participantOwnership='ActivityParticipationContext', activityOwnership='true', source='{Source}', reason='{Reason}'";
        }

        private static string FormatRequestedParticipantId(SessionParticipantId participantId)
        {
            return participantId.IsValid ? participantId.ToString() : "<none>";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityParticipantMaterializationCommand
    {
        public ActivityParticipantMaterializationCommand(
            SessionActivityIdentity identity,
            string requirementId,
            ActivityParticipantRequirementKind participantKind,
            ActivityParticipantBinding participantBinding,
            ActivityParticipantMaterializationNeedKind needKind,
            string source,
            string reason)
        {
            Identity = identity;
            RequirementId = Normalize(requirementId);
            ParticipantKind = participantKind;
            ParticipantBinding = participantBinding;
            NeedKind = needKind;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string RequirementId { get; }
        public ActivityParticipantRequirementKind ParticipantKind { get; }

        public ActivityParticipantBinding ParticipantBinding { get; }
        public ActivityParticipantMaterializationNeedKind NeedKind { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(RequirementId) &&
            ParticipantKind != ActivityParticipantRequirementKind.Unknown &&
            ParticipantBinding.IsValid &&
            NeedKind != ActivityParticipantMaterializationNeedKind.Unknown &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', requirementId='{RequirementId}', participantKind='{ParticipantKind}', participantId='{ParticipantBinding.ParticipantId}', role='{ParticipantBinding.Role}', playerSlotId='{ParticipantBinding.PlayerSlotId}', actorDefinitionId='{ParticipantBinding.ActorDefinitionId}', actorId='{ParticipantBinding.ActorId}', materializationPolicy='{ParticipantBinding.MaterializationPolicy}', needKind='{NeedKind}', participantOwnership='ActivityParticipationContext', activityOwnership='true', source='{Source}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityParticipantResetCommand
    {
        public ActivityParticipantResetCommand(
            SessionActivityIdentity identity,
            string requirementId,
            ActivityParticipantBinding participantBinding,
            string placementRequirementId,
            PlayerActorIdentityRecord actorIdentity,
            IReadOnlyList<ActorCapabilityResetEndpointReference> resetReferences,
            ActivityResetScopePlan resetScopePlan,
            bool required,
            bool placementDeclared,
            bool placementRequired,
            bool placementOptional,
            bool hasPlacement,
            Vector3 placementPosition,
            Vector3 placementEulerAngles,
            string source,
            string reason)
        {
            Identity = identity;
            RequirementId = Normalize(requirementId);
            ParticipantBinding = participantBinding;
            PlacementRequirementId = Normalize(placementRequirementId);
            ActorIdentity = actorIdentity;
            ResetReferences = resetReferences ?? Array.Empty<ActorCapabilityResetEndpointReference>();
            ResetScopePlan = resetScopePlan;
            Required = required;
            PlacementDeclared = placementDeclared;
            PlacementRequired = placementRequired;
            PlacementOptional = placementOptional;
            HasPlacement = hasPlacement;
            PlacementPosition = placementPosition;
            PlacementEulerAngles = placementEulerAngles;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string RequirementId { get; }
        public ActivityParticipantBinding ParticipantBinding { get; }
        public string PlacementRequirementId { get; }
        public PlayerActorIdentityRecord ActorIdentity { get; }
        public IReadOnlyList<ActorCapabilityResetEndpointReference> ResetReferences { get; }
        public ActivityResetScopePlan ResetScopePlan { get; }
        public ActivityResetIntent ResetIntent => ResetScopePlan.ResetIntent;
        public ActivityResetStateProfileKind StateProfileKind => ResetScopePlan.StateProfileKind;
        public bool Required { get; }
        public bool PlacementDeclared { get; }
        public bool PlacementRequired { get; }
        public bool PlacementOptional { get; }
        public bool HasPlacement { get; }
        public Vector3 PlacementPosition { get; }
        public Vector3 PlacementEulerAngles { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid
        {
            get
            {
                if (!Identity.IsValid || string.IsNullOrWhiteSpace(RequirementId) || !ParticipantBinding.IsValid || !ActorIdentity.IsValid || ResetReferences == null || !ResetScopePlan.IsValid || string.IsNullOrWhiteSpace(Source))
                {
                    return false;
                }

                for (int index = 0; index < ResetReferences.Count; index++)
                {
                    if (ResetReferences[index] == null || !ResetReferences[index].IsValid)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override string ToString()
        {
            int resetReferenceCount = ResetReferences?.Count ?? 0;
            return $"identity='{Identity}', requirementId='{RequirementId}', participantId='{ParticipantBinding.ParticipantId}', role='{ParticipantBinding.Role}', playerSlotId='{ParticipantBinding.PlayerSlotId}', actorDefinitionId='{ParticipantBinding.ActorDefinitionId}', actorId='{ParticipantBinding.ActorId}', placementRequirementId='{(string.IsNullOrWhiteSpace(PlacementRequirementId) ? "<none>" : PlacementRequirementId)}', resetIntent='{ResetIntent}', resetStateProfile='{StateProfileKind}', resetDescriptor='endpoint_inventory', descriptorMode='endpoint_inventory', resetReferenceCount='{resetReferenceCount}', participantOwnership='ActivityParticipationContext', activityOwnership='true', source='{Source}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityParticipantCommandPlan
    {
        public ActivityParticipantCommandPlan(
            SessionActivityIdentity identity,
            IReadOnlyList<ActivityParticipantBindCommand> bindCommands,
            IReadOnlyList<ActivityParticipantMaterializationCommand> materializationCommands,
            string source,
            string reason)
        {
            Identity = identity;
            BindCommands = bindCommands ?? Array.Empty<ActivityParticipantBindCommand>();
            MaterializationCommands = materializationCommands ?? Array.Empty<ActivityParticipantMaterializationCommand>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public IReadOnlyList<ActivityParticipantBindCommand> BindCommands { get; }
        public IReadOnlyList<ActivityParticipantMaterializationCommand> MaterializationCommands { get; }
        public string Source { get; }
        public string Reason { get; }

        public int TotalCommandCount =>
            BindCommands.Count +
            MaterializationCommands.Count;

        public bool HasCommands => TotalCommandCount > 0;

        public bool IsValid =>
            Identity.IsValid &&
            BindCommands != null &&
            MaterializationCommands != null &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', totalCommands='{TotalCommandCount}', bind='{BindCommands.Count}', materialization='{MaterializationCommands.Count}', participantOwnership='ActivityParticipationContext', activityOwnership='true', source='{Source}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
