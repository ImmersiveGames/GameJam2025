using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
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
        public bool Required { get; }
        public bool PlacementDeclared { get; }
        public bool PlacementRequired { get; }
        public bool PlacementOptional { get; }
        public bool HasPlacement { get; }
        public Vector3 PlacementPosition { get; }
        public Vector3 PlacementEulerAngles { get; }
        public IReadOnlyList<ActorResetGroup> ResetGroups => DeriveResetGroups(ResetReferences);
        public string Source { get; }
        public string Reason { get; }

        public bool HasResetGroups => ResetGroups is { Count: > 0 };

        public bool IsValid
        {
            get
            {
                if (!Identity.IsValid || string.IsNullOrWhiteSpace(RequirementId) || !ParticipantBinding.IsValid || !ActorIdentity.IsValid || ResetReferences == null || string.IsNullOrWhiteSpace(Source))
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
            return $"identity='{Identity}', requirementId='{RequirementId}', participantId='{ParticipantBinding.ParticipantId}', role='{ParticipantBinding.Role}', playerSlotId='{ParticipantBinding.PlayerSlotId}', actorDefinitionId='{ParticipantBinding.ActorDefinitionId}', actorId='{ParticipantBinding.ActorId}', placementRequirementId='{(string.IsNullOrWhiteSpace(PlacementRequirementId) ? "<none>" : PlacementRequirementId)}', resetGroups='{FormatResetGroups(ResetGroups)}', participantOwnership='ActivityParticipationContext', activityOwnership='true', source='{Source}', reason='{Reason}'";
        }

        private static IReadOnlyList<ActorResetGroup> DeriveResetGroups(IReadOnlyList<ActorCapabilityResetEndpointReference> resetReferences)
        {
            if (resetReferences == null || resetReferences.Count == 0)
            {
                return Array.Empty<ActorResetGroup>();
            }

            List<ActorResetGroup> groups = new();
            HashSet<ActorResetGroup> unique = new();
            for (int referenceIndex = 0; referenceIndex < resetReferences.Count; referenceIndex++)
            {
                var resetReference = resetReferences[referenceIndex];
                if (resetReference == null || !resetReference.IsValid || resetReference.SupportedGroups == null)
                {
                    continue;
                }

                for (int groupIndex = 0; groupIndex < resetReference.SupportedGroups.Length; groupIndex++)
                {
                    var group = resetReference.SupportedGroups[groupIndex];
                    if (group == ActorResetGroup.Unknown || !unique.Add(group))
                    {
                        continue;
                    }

                    groups.Add(group);
                }
            }

            return groups;
        }

        private static string FormatResetGroups(IReadOnlyList<ActorResetGroup> resetGroups)
        {
            if (resetGroups == null || resetGroups.Count == 0)
            {
                return "<none>";
            }

            return string.Join(",", resetGroups);
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
