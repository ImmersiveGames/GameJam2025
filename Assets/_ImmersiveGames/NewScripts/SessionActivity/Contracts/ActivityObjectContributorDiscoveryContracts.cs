using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityObjectContributorKind
    {
        Unknown = 0,
        SceneObject = 1,
        RuntimeObject = 2,
        AdapterProxy = 3,
    }

    public readonly struct ActivityObjectContributionReport
    {
        public ActivityObjectContributionReport(
            SessionActivityIdentity identity,
            string contentProfileId,
            SceneKeyAsset sceneKey,
            string sceneName,
            string targetId,
            string roleId,
            ActivityObjectContributorKind contributorKind,
            ActivitySetupRequirementRequiredness requiredness,
            ActivityResetBoundaryEligibility resetBoundaryEligibility,
            IReadOnlyList<ActivityReleaseRequirementKind> supportedReleaseKinds,
            string source,
            string reason)
        {
            Identity = identity;
            PipelineId = identity.PipelineId.TrimToEmpty();
            SessionStateId = identity.SessionId.TrimToEmpty();
            ActivityId = identity.ActivityId.TrimToEmpty();
            ActivityOrdinal = identity.ActivityOrdinal;
            EntrySequence = identity.EntrySequence;
            ContentProfileId = contentProfileId.TrimToEmpty();
            SceneKey = sceneKey;
            SceneName = sceneName.TrimToEmpty();
            TargetId = targetId.TrimToEmpty();
            RoleId = roleId.TrimToEmpty();
            ContributorKind = contributorKind;
            Requiredness = requiredness;
            ResetBoundaryEligibility = resetBoundaryEligibility;
            SupportedReleaseKinds = supportedReleaseKinds ?? Array.Empty<ActivityReleaseRequirementKind>();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public string PipelineId { get; }
        public string SessionStateId { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public int EntrySequence { get; }
        public string ContentProfileId { get; }
        public SceneKeyAsset SceneKey { get; }
        public string SceneName { get; }
        public string TargetId { get; }
        public string RoleId { get; }
        public ActivityObjectContributorKind ContributorKind { get; }
        public ActivitySetupRequirementRequiredness Requiredness { get; }
        public ActivityResetBoundaryEligibility ResetBoundaryEligibility { get; }
        public IReadOnlyList<ActivityReleaseRequirementKind> SupportedReleaseKinds { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasSceneKey => SceneKey != null;

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            EntrySequence > 0 &&
            !string.IsNullOrWhiteSpace(ContentProfileId) &&
            !string.IsNullOrWhiteSpace(SceneName) &&
            !string.IsNullOrWhiteSpace(TargetId) &&
            ContributorKind != ActivityObjectContributorKind.Unknown &&
            Requiredness != ActivitySetupRequirementRequiredness.Unknown &&
            SupportedReleaseKinds != null &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', contentProfileId='{ContentProfileId}', sceneKey='{(HasSceneKey ? SceneKey.name : "<none>")}', sceneName='{SceneName}', targetId='{TargetId}', roleId='{(string.IsNullOrWhiteSpace(RoleId) ? "<none>" : RoleId)}', contributorKind='{ContributorKind}', requiredness='{Requiredness}', resetBoundaryEligibility='{ActivityResetBoundaryEligibilityFormatter.Format(ResetBoundaryEligibility)}', resetDescriptor='endpoint_inventory', descriptorMode='endpoint_inventory', releaseKinds='{SupportedReleaseKinds.Count}', source='{Source}', reason='{Reason}'";
        }
}

    public readonly struct ActivityObjectContributorDiscoveryResult
    {
        public ActivityObjectContributorDiscoveryResult(
            SessionActivityIdentity identity,
            string contentProfileId,
            IReadOnlyList<ActivityObjectContributionReport> reports,
            string source,
            string reason,
            string message)
        {
            Identity = identity;
            ContentProfileId = contentProfileId.TrimToEmpty();
            Reports = reports ?? Array.Empty<ActivityObjectContributionReport>();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
            Message = message.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public string ContentProfileId { get; }
        public IReadOnlyList<ActivityObjectContributionReport> Reports { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(ContentProfileId) &&
            Reports != null &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', contentProfileId='{ContentProfileId}', reports='{Reports.Count}', source='{Source}', reason='{Reason}', message='{Message}'";
        }
}
}
