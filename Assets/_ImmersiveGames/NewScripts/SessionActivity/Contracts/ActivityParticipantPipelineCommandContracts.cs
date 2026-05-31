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
            string requestedParticipantId,
            string resolvedParticipantId,
            string roleId,
            string source,
            string reason)
        {
            Identity = identity;
            RequirementId = Normalize(requirementId);
            ParticipantKind = participantKind;
            RequestedParticipantId = Normalize(requestedParticipantId);
            ResolvedParticipantId = Normalize(resolvedParticipantId);
            RoleId = Normalize(roleId);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string RequirementId { get; }
        public ActivityParticipantRequirementKind ParticipantKind { get; }
        public string RequestedParticipantId { get; }
        public string ResolvedParticipantId { get; }
        public string RoleId { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(RequirementId) &&
            ParticipantKind != ActivityParticipantRequirementKind.Unknown &&
            !string.IsNullOrWhiteSpace(ResolvedParticipantId) &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', requirementId='{RequirementId}', participantKind='{ParticipantKind}', requestedParticipantId='{(string.IsNullOrWhiteSpace(RequestedParticipantId) ? "<none>" : RequestedParticipantId)}', resolvedParticipantId='{ResolvedParticipantId}', roleId='{RoleId}', participantOwnership='RouteSession', activityOwnership='false', source='{Source}', reason='{Reason}'";
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
            ResolvedParticipantId = participantBinding.PlayerSlotId.IsValid
                ? participantBinding.PlayerSlotId.Value
                : string.Empty;
            NeedKind = needKind;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string RequirementId { get; }
        public ActivityParticipantRequirementKind ParticipantKind { get; }

        // Chave técnica transitória para stages legados de placement/reset/binding.
        // A fonte canônica da materialização é ParticipantBinding.
        public string ResolvedParticipantId { get; }

        public ActivityParticipantBinding ParticipantBinding { get; }
        public ActivityParticipantMaterializationNeedKind NeedKind { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(RequirementId) &&
            ParticipantKind != ActivityParticipantRequirementKind.Unknown &&
            ParticipantBinding.IsValid &&
            !string.IsNullOrWhiteSpace(ResolvedParticipantId) &&
            NeedKind != ActivityParticipantMaterializationNeedKind.Unknown &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', requirementId='{RequirementId}', participantKind='{ParticipantKind}', participantId='{ParticipantBinding.ParticipantId}', playerSlotId='{ParticipantBinding.PlayerSlotId}', actorDefinitionId='{ParticipantBinding.ActorDefinitionId}', actorId='{ParticipantBinding.ActorId}', materializationPolicy='{ParticipantBinding.MaterializationPolicy}', technicalKey='{ResolvedParticipantId}', needKind='{NeedKind}', participantOwnership='ActivityParticipationContext', activityOwnership='true', source='{Source}', reason='{Reason}'";
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
            string resolvedParticipantId,
            string placementRequirementId,
            string source,
            string reason)
        {
            Identity = identity;
            RequirementId = Normalize(requirementId);
            ResolvedParticipantId = Normalize(resolvedParticipantId);
            PlacementRequirementId = Normalize(placementRequirementId);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string RequirementId { get; }
        public string ResolvedParticipantId { get; }
        public string PlacementRequirementId { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(RequirementId) &&
            !string.IsNullOrWhiteSpace(ResolvedParticipantId) &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', requirementId='{RequirementId}', resolvedParticipantId='{ResolvedParticipantId}', placementRequirementId='{(string.IsNullOrWhiteSpace(PlacementRequirementId) ? "<none>" : PlacementRequirementId)}', placementScope='ActivityLocal', participantOwnership='RouteSession', activityOwnership='false', source='{Source}', reason='{Reason}'";
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
            string resolvedParticipantId,
            IReadOnlyList<ActivityStateResetGroup> resetGroups,
            string source,
            string reason)
        {
            Identity = identity;
            RequirementId = Normalize(requirementId);
            ResolvedParticipantId = Normalize(resolvedParticipantId);
            ResetGroups = resetGroups ?? Array.Empty<ActivityStateResetGroup>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string RequirementId { get; }
        public string ResolvedParticipantId { get; }
        public IReadOnlyList<ActivityStateResetGroup> ResetGroups { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasResetGroups => ResetGroups != null && ResetGroups.Count > 0;

        public bool IsValid
        {
            get
            {
                if (!Identity.IsValid || string.IsNullOrWhiteSpace(RequirementId) || string.IsNullOrWhiteSpace(ResolvedParticipantId) || ResetGroups == null || ResetGroups.Count == 0 || string.IsNullOrWhiteSpace(Source))
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
            return $"identity='{Identity}', requirementId='{RequirementId}', resolvedParticipantId='{ResolvedParticipantId}', resetGroups='{FormatResetGroups(ResetGroups)}', participantOwnership='RouteSession', activityOwnership='false', source='{Source}', reason='{Reason}'";
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
            return $"identity='{Identity}', totalCommands='{TotalCommandCount}', bind='{BindCommands.Count}', materialization='{MaterializationCommands.Count}', placement='{PlacementCommands.Count}', reset='{ResetCommands.Count}', participantOwnership='RouteSession', activityOwnership='false', source='{Source}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
