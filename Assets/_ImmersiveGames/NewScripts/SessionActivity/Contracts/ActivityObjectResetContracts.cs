using _ImmersiveGames.NewScripts.UnityUtils;
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
            string resetDescriptorMetadata,
            ActivityResetScopePlan resetScopePlan,
            string source,
            string reason)
        {
            Identity = identity;
            TargetId = targetId.TrimToEmpty();
            RoleId = roleId.TrimToEmpty();
            ContributorKind = contributorKind;
            Requiredness = requiredness;
            ResetDescriptorMetadata = NormalizeResetDescriptorMetadata(resetDescriptorMetadata);
            ResetScopePlan = resetScopePlan;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public string TargetId { get; }
        public string RoleId { get; }
        public ActivityObjectContributorKind ContributorKind { get; }
        public ActivitySetupRequirementRequiredness Requiredness { get; }
        public string ResetDescriptorMetadata { get; }
        public ActivityResetScopePlan ResetScopePlan { get; }
        public ActivityResetIntent ResetIntent => ResetScopePlan.ResetIntent;
        public ActivityResetStateProfileKind StateProfileKind => ResetScopePlan.StateProfileKind;
        public string Source { get; }
        public string Reason { get; }

        public bool IsRequired => Requiredness == ActivitySetupRequirementRequiredness.Required;
        public bool IsOptional => Requiredness == ActivitySetupRequirementRequiredness.Optional;

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(TargetId) &&
            ContributorKind != ActivityObjectContributorKind.Unknown &&
            Requiredness != ActivitySetupRequirementRequiredness.Unknown &&
            ResetScopePlan.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', targetId='{TargetId}', roleId='{(string.IsNullOrWhiteSpace(RoleId) ? "<none>" : RoleId)}', contributorKind='{ContributorKind}', requiredness='{Requiredness}', resetIntent='{ResetIntent}', resetStateProfile='{StateProfileKind}', resetDescriptor='{ResetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report', source='{Source}', reason='{Reason}'";
        }
private static string NormalizeResetDescriptorMetadata(string value)
        {
            return value.TrimToOrDefault("<none>");
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
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
            Message = message.TrimToEmpty();
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
}

    public interface IActivityObjectResetEndpoint
    {
    }

    public interface IActivityObjectEntryInitializeResetEndpoint : IActivityObjectResetEndpoint
    {
        ActivityObjectResetResult ApplyEntryInitializeReset(ActivityObjectResetCommand command);
    }

    public interface IActivityObjectRuntimeLocalResetEndpoint : IActivityObjectResetEndpoint
    {
        ActivityObjectResetResult ApplyRuntimeLocalReset(ActivityObjectResetCommand command);
    }

    public interface IActivityObjectRuntimeActivityResetEndpoint : IActivityObjectResetEndpoint
    {
        ActivityObjectResetResult ApplyRuntimeActivityReset(ActivityObjectResetCommand command);
    }

    public interface IActivityObjectRuntimeActivityTransitionResetEndpoint : IActivityObjectResetEndpoint
    {
        ActivityObjectResetResult ApplyRuntimeActivityTransitionReset(ActivityObjectResetCommand command);
    }

    public interface IActivityObjectRuntimeRouteTransitionResetEndpoint : IActivityObjectResetEndpoint
    {
        ActivityObjectResetResult ApplyRuntimeRouteTransitionReset(ActivityObjectResetCommand command);
    }
}
