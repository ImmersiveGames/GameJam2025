using System;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityObjectReleaseResultKind
    {
        Unknown = 0,
        Applied = 1,
        SkippedOptional = 2,
        Failed = 3,
        RejectedStaleOrForeign = 4,
    }

    public readonly struct ActivityObjectReleaseCommand
    {
        public ActivityObjectReleaseCommand(
            SessionActivityIdentity identity,
            string targetId,
            string roleId,
            ActivityObjectContributorKind contributorKind,
            ActivitySetupRequirementRequiredness requiredness,
            ActivityReleaseRequirementKind releaseKind,
            string source,
            string reason)
        {
            Identity = identity;
            PipelineId = Normalize(identity.PipelineId);
            SessionStateId = Normalize(identity.SessionId);
            ActivityId = Normalize(identity.ActivityId);
            ActivityOrdinal = identity.ActivityOrdinal;
            EntrySequence = identity.EntrySequence;
            TargetId = Normalize(targetId);
            RoleId = Normalize(roleId);
            ContributorKind = contributorKind;
            Requiredness = requiredness;
            ReleaseKind = releaseKind;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string PipelineId { get; }
        public string SessionStateId { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public int EntrySequence { get; }
        public string TargetId { get; }
        public string RoleId { get; }
        public ActivityObjectContributorKind ContributorKind { get; }
        public ActivitySetupRequirementRequiredness Requiredness { get; }
        public ActivityReleaseRequirementKind ReleaseKind { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsRequired => Requiredness == ActivitySetupRequirementRequiredness.Required;
        public bool IsOptional => Requiredness == ActivitySetupRequirementRequiredness.Optional;

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            EntrySequence > 0 &&
            !string.IsNullOrWhiteSpace(TargetId) &&
            ContributorKind != ActivityObjectContributorKind.Unknown &&
            Requiredness != ActivitySetupRequirementRequiredness.Unknown &&
            ReleaseKind != ActivityReleaseRequirementKind.Unknown &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', targetId='{TargetId}', roleId='{(string.IsNullOrWhiteSpace(RoleId) ? "<none>" : RoleId)}', contributorKind='{ContributorKind}', requiredness='{Requiredness}', releaseKind='{ReleaseKind}', source='{Source}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityObjectReleaseResult
    {
        public ActivityObjectReleaseResult(
            ActivityObjectReleaseResultKind kind,
            ActivityObjectReleaseCommand command,
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

        public ActivityObjectReleaseResultKind Kind { get; }
        public ActivityObjectReleaseCommand Command { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsApplied => Kind == ActivityObjectReleaseResultKind.Applied;
        public bool IsSkippedOptional => Kind == ActivityObjectReleaseResultKind.SkippedOptional;
        public bool IsFailed => Kind == ActivityObjectReleaseResultKind.Failed;
        public bool IsRejectedStaleOrForeign => Kind == ActivityObjectReleaseResultKind.RejectedStaleOrForeign;

        public bool IsValid =>
            Kind != ActivityObjectReleaseResultKind.Unknown &&
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

    public interface IActivityObjectReleaseEndpoint
    {
        bool Supports(ActivityReleaseRequirementKind releaseKind);
        ActivityObjectReleaseResult ApplyRelease(ActivityObjectReleaseCommand command);
    }
}
