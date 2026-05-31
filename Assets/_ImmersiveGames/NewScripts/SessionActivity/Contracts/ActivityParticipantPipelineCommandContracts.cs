using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;

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

    public readonly struct ActivityParticipantPlacementCommand
    {
        public ActivityParticipantPlacementCommand(
            SessionActivityIdentity identity,
            string requirementId,
            ActivityParticipantBinding participantBinding,
            string placementRequirementId,
            string source,
            string reason)
        {
            Identity = identity;
            RequirementId = Normalize(requirementId);
            ParticipantBinding = participantBinding;
            PlacementRequirementId = Normalize(placementRequirementId);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string RequirementId { get; }
        public ActivityParticipantBinding ParticipantBinding { get; }
        public string PlacementRequirementId { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(RequirementId) &&
            ParticipantBinding.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', requirementId='{RequirementId}', participantId='{ParticipantBinding.ParticipantId}', role='{ParticipantBinding.Role}', playerSlotId='{ParticipantBinding.PlayerSlotId}', actorDefinitionId='{ParticipantBinding.ActorDefinitionId}', actorId='{ParticipantBinding.ActorId}', placementRequirementId='{(string.IsNullOrWhiteSpace(PlacementRequirementId) ? "<none>" : PlacementRequirementId)}', placementScope='ActivityLocal', participantOwnership='ActivityParticipationContext', activityOwnership='true', source='{Source}', reason='{Reason}'";
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
            IReadOnlyList<ActivityStateResetGroup> resetGroups,
            string source,
            string reason)
        {
            Identity = identity;
            RequirementId = Normalize(requirementId);
            ParticipantBinding = participantBinding;
            PlacementRequirementId = Normalize(placementRequirementId);
            ResetGroups = resetGroups ?? Array.Empty<ActivityStateResetGroup>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string RequirementId { get; }
        public ActivityParticipantBinding ParticipantBinding { get; }
        public string PlacementRequirementId { get; }
        public IReadOnlyList<ActivityStateResetGroup> ResetGroups { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasResetGroups => ResetGroups != null && ResetGroups.Count > 0;

        public bool IsValid
        {
            get
            {
                if (!Identity.IsValid || string.IsNullOrWhiteSpace(RequirementId) || !ParticipantBinding.IsValid || ResetGroups == null || ResetGroups.Count == 0 || string.IsNullOrWhiteSpace(Source))
                {
                    return false;
                }

                for (int index = 0; index < ResetGroups.Count; index++)
                {
                    if (ResetGroups[index] == ActivityStateResetGroup.Unknown)
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

        private static string FormatResetGroups(IReadOnlyList<ActivityStateResetGroup> resetGroups)
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
            IReadOnlyList<ActivityParticipantPlacementCommand> placementCommands,
            IReadOnlyList<ActivityParticipantResetCommand> resetCommands,
            string source,
            string reason)
        {
            Identity = identity;
            BindCommands = bindCommands ?? Array.Empty<ActivityParticipantBindCommand>();
            MaterializationCommands = materializationCommands ?? Array.Empty<ActivityParticipantMaterializationCommand>();
            PlacementCommands = placementCommands ?? Array.Empty<ActivityParticipantPlacementCommand>();
            ResetCommands = resetCommands ?? Array.Empty<ActivityParticipantResetCommand>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public IReadOnlyList<ActivityParticipantBindCommand> BindCommands { get; }
        public IReadOnlyList<ActivityParticipantMaterializationCommand> MaterializationCommands { get; }
        public IReadOnlyList<ActivityParticipantPlacementCommand> PlacementCommands { get; }
        public IReadOnlyList<ActivityParticipantResetCommand> ResetCommands { get; }
        public string Source { get; }
        public string Reason { get; }

        public int TotalCommandCount =>
            BindCommands.Count +
            MaterializationCommands.Count +
            PlacementCommands.Count +
            ResetCommands.Count;

        public bool HasCommands => TotalCommandCount > 0;

        public bool IsValid =>
            Identity.IsValid &&
            BindCommands != null &&
            MaterializationCommands != null &&
            PlacementCommands != null &&
            ResetCommands != null &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', totalCommands='{TotalCommandCount}', bind='{BindCommands.Count}', materialization='{MaterializationCommands.Count}', placement='{PlacementCommands.Count}', reset='{ResetCommands.Count}', participantOwnership='ActivityParticipationContext', activityOwnership='true', source='{Source}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
