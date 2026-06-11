using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityObjectLifecycleContributionKind
    {
        Unknown = 0,
        Reset = 1,
        Snapshot = 2,
        SnapshotRestore = 3,
        Release = 4,
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
            TargetId = Normalize(targetId);
            RoleId = Normalize(roleId);
            ContributorKind = contributorKind;
            Requiredness = requiredness;
            Source = Normalize(source);
            Reason = Normalize(reason);
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
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
            ContributionId = Normalize(contributionId);
            Priority = priority;
            ResetEndpoint = resetEndpoint;
        }

        public ActivityObjectLifecycleContributionKind Kind => ActivityObjectLifecycleContributionKind.Reset;
        public string ContributionId { get; }
        public int Priority { get; }
        public IActivityObjectResetEndpoint ResetEndpoint { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ContributionId) && ResetEndpoint != null;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityObjectSnapshotContribution : IActivityObjectSnapshotContribution
    {
        public ActivityObjectSnapshotContribution(
            string contributionId,
            int priority,
            IActivityObjectSnapshotProvider snapshotProvider)
        {
            ContributionId = Normalize(contributionId);
            Priority = priority;
            SnapshotProvider = snapshotProvider;
        }

        public ActivityObjectLifecycleContributionKind Kind => ActivityObjectLifecycleContributionKind.Snapshot;
        public string ContributionId { get; }
        public int Priority { get; }
        public IActivityObjectSnapshotProvider SnapshotProvider { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ContributionId) && SnapshotProvider != null;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityObjectSnapshotRestoreContribution : IActivityObjectSnapshotRestoreContribution
    {
        public ActivityObjectSnapshotRestoreContribution(
            string contributionId,
            int priority,
            IActivityObjectSnapshotRestoreEndpoint restoreEndpoint)
        {
            ContributionId = Normalize(contributionId);
            Priority = priority;
            RestoreEndpoint = restoreEndpoint;
        }

        public ActivityObjectLifecycleContributionKind Kind => ActivityObjectLifecycleContributionKind.SnapshotRestore;
        public string ContributionId { get; }
        public int Priority { get; }
        public IActivityObjectSnapshotRestoreEndpoint RestoreEndpoint { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ContributionId) && RestoreEndpoint != null;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityObjectReleaseContribution : IActivityObjectReleaseContribution
    {
        public ActivityObjectReleaseContribution(
            string contributionId,
            int priority,
            IActivityObjectReleaseEndpoint releaseEndpoint)
        {
            ContributionId = Normalize(contributionId);
            Priority = priority;
            ReleaseEndpoint = releaseEndpoint;
        }

        public ActivityObjectLifecycleContributionKind Kind => ActivityObjectLifecycleContributionKind.Release;
        public string ContributionId { get; }
        public int Priority { get; }
        public IActivityObjectReleaseEndpoint ReleaseEndpoint { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ContributionId) && ReleaseEndpoint != null;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
