using System.Collections.Generic;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityObjectLifecycleContributionKind
    {
        Unknown = 0,
        Reset = 1,
        Snapshot = 2,
        SnapshotRestore = 3,
        Release = 4
    }

    public readonly struct ActivityObjectLifecycleContributionContext
    {
        public ActivityObjectLifecycleContributionContext(
            SessionActivityIdentity identity,
            string targetId,
            string roleId,
            ActivityObjectContributorKind contributorKind,
            ActivitySetupRequirementRequiredness requiredness,
            string source,
            string reason)
        {
            Identity = identity;
            TargetId = targetId.TrimToEmpty();
            RoleId = roleId.TrimToEmpty();
            ContributorKind = contributorKind;
            Requiredness = requiredness;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public string TargetId { get; }
        public string RoleId { get; }
        public ActivityObjectContributorKind ContributorKind { get; }
        public ActivitySetupRequirementRequiredness Requiredness { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(TargetId) &&
            ContributorKind != ActivityObjectContributorKind.Unknown &&
            Requiredness != ActivitySetupRequirementRequiredness.Unknown &&
            !string.IsNullOrWhiteSpace(Source);
    }

    public interface IActivityObjectLifecycleContribution
    {
        ActivityObjectLifecycleContributionKind Kind { get; }
        string ContributionId { get; }
        int Priority { get; }
        bool IsValid { get; }
    }

    public interface IActivityObjectResetContribution : IActivityObjectLifecycleContribution
    {
        IActivityObjectResetEndpoint ResetEndpoint { get; }
    }

    public interface IActivityObjectSnapshotContribution : IActivityObjectLifecycleContribution
    {
        IActivityObjectSnapshotProvider SnapshotProvider { get; }
    }

    public interface IActivityObjectSnapshotRestoreContribution : IActivityObjectLifecycleContribution
    {
        IActivityObjectSnapshotRestoreEndpoint RestoreEndpoint { get; }
    }

    public interface IActivityObjectReleaseContribution : IActivityObjectLifecycleContribution
    {
        IActivityObjectReleaseEndpoint ReleaseEndpoint { get; }
    }

    public interface IActivityObjectLifecycleContributionProvider
    {
        void CollectActivityObjectLifecycleContributions(
            ActivityObjectLifecycleContributionContext context,
            IList<IActivityObjectLifecycleContribution> contributions);
    }

    public readonly struct ActivityObjectResetContribution : IActivityObjectResetContribution
    {
        public ActivityObjectResetContribution(
            string contributionId,
            int priority,
            IActivityObjectResetEndpoint resetEndpoint)
        {
            ContributionId = contributionId.TrimToEmpty();
            Priority = priority;
            ResetEndpoint = resetEndpoint;
        }

        public ActivityObjectLifecycleContributionKind Kind => ActivityObjectLifecycleContributionKind.Reset;
        public string ContributionId { get; }
        public int Priority { get; }
        public IActivityObjectResetEndpoint ResetEndpoint { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ContributionId) && ResetEndpoint != null;
    }

    public readonly struct ActivityObjectSnapshotContribution : IActivityObjectSnapshotContribution
    {
        public ActivityObjectSnapshotContribution(
            string contributionId,
            int priority,
            IActivityObjectSnapshotProvider snapshotProvider)
        {
            ContributionId = contributionId.TrimToEmpty();
            Priority = priority;
            SnapshotProvider = snapshotProvider;
        }

        public ActivityObjectLifecycleContributionKind Kind => ActivityObjectLifecycleContributionKind.Snapshot;
        public string ContributionId { get; }
        public int Priority { get; }
        public IActivityObjectSnapshotProvider SnapshotProvider { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ContributionId) && SnapshotProvider != null;
    }

    public readonly struct ActivityObjectSnapshotRestoreContribution : IActivityObjectSnapshotRestoreContribution
    {
        public ActivityObjectSnapshotRestoreContribution(
            string contributionId,
            int priority,
            IActivityObjectSnapshotRestoreEndpoint restoreEndpoint)
        {
            ContributionId = contributionId.TrimToEmpty();
            Priority = priority;
            RestoreEndpoint = restoreEndpoint;
        }

        public ActivityObjectLifecycleContributionKind Kind => ActivityObjectLifecycleContributionKind.SnapshotRestore;
        public string ContributionId { get; }
        public int Priority { get; }
        public IActivityObjectSnapshotRestoreEndpoint RestoreEndpoint { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ContributionId) && RestoreEndpoint != null;
    }

    public readonly struct ActivityObjectReleaseContribution : IActivityObjectReleaseContribution
    {
        public ActivityObjectReleaseContribution(
            string contributionId,
            int priority,
            IActivityObjectReleaseEndpoint releaseEndpoint)
        {
            ContributionId = contributionId.TrimToEmpty();
            Priority = priority;
            ReleaseEndpoint = releaseEndpoint;
        }

        public ActivityObjectLifecycleContributionKind Kind => ActivityObjectLifecycleContributionKind.Release;
        public string ContributionId { get; }
        public int Priority { get; }
        public IActivityObjectReleaseEndpoint ReleaseEndpoint { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ContributionId) && ReleaseEndpoint != null;
    }
}
