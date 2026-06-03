namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityObjectResetResultKind
    {
        Unknown = 0,
        Applied = 1,
        SkippedOptional = 2,
        Failed = 3,
        RejectedStaleOrForeign = 4,
    }

    public readonly struct ActivityObjectResetCommand
    {
        public ActivityObjectResetCommand(
            SessionActivityIdentity identity,
            string targetId,
            string roleId,
            ActivityObjectContributorKind contributorKind,
            ActivitySetupRequirementRequiredness requiredness,
            ActivityStateResetGroup resetGroup,
            string source,
            string reason)
        {
            Identity = identity;
            TargetId = Normalize(targetId);
            RoleId = Normalize(roleId);
            ContributorKind = contributorKind;
            Requiredness = requiredness;
            ResetGroup = resetGroup;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string TargetId { get; }
        public string RoleId { get; }
        public ActivityObjectContributorKind ContributorKind { get; }
        public ActivitySetupRequirementRequiredness Requiredness { get; }
        public ActivityStateResetGroup ResetGroup { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsRequired => Requiredness == ActivitySetupRequirementRequiredness.Required;
        public bool IsOptional => Requiredness == ActivitySetupRequirementRequiredness.Optional;

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(TargetId) &&
            ContributorKind != ActivityObjectContributorKind.Unknown &&
            Requiredness != ActivitySetupRequirementRequiredness.Unknown &&
            ResetGroup != ActivityStateResetGroup.Unknown &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', targetId='{TargetId}', roleId='{(string.IsNullOrWhiteSpace(RoleId) ? "<none>" : RoleId)}', contributorKind='{ContributorKind}', requiredness='{Requiredness}', resetGroup='{ResetGroup}', source='{Source}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityObjectResetResult
    {
        public ActivityObjectResetResult(
            ActivityObjectResetResultKind kind,
            ActivityObjectResetCommand command,
            string source,
            string reason,
            string message)
        {
            Kind = kind;
            Command = command;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ActivityObjectResetResultKind Kind { get; }
        public ActivityObjectResetCommand Command { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsApplied => Kind == ActivityObjectResetResultKind.Applied;
        public bool IsSkippedOptional => Kind == ActivityObjectResetResultKind.SkippedOptional;
        public bool IsFailed => Kind == ActivityObjectResetResultKind.Failed;
        public bool IsRejectedStaleOrForeign => Kind == ActivityObjectResetResultKind.RejectedStaleOrForeign;

        public bool IsValid =>
            Kind != ActivityObjectResetResultKind.Unknown &&
            Command.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"kind='{Kind}', command='{Command}', source='{Source}', reason='{Reason}', message='{Message}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IActivityObjectResetEndpoint
    {
        bool Supports(ActivityStateResetGroup resetGroup);
        ActivityObjectResetResult ApplyReset(ActivityObjectResetCommand command);
    }
}
