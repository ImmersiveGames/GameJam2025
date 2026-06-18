using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using static _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages.ActivityEntryObjectSetupStageUtility;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryObjectContributorDiscoveryStage
    {
        public static ActivityObjectContributorDiscoveryResult Execute(
            ActivityEntryObjectSetupCommand command,
            ActivityContentLoadedSet loadedSet,
            IActivityEntryFactRuntimeBridge factBridge,
            ActivityEntryLogSink logSink,
            ActivityEntryInventoryRuntimeState inventoryState,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            int entrySequence = command.Identity.EntrySequence;
            var discoveryIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActivitySetupStarted);
            factBridge.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectContributorDiscoveryStarted,
                discoveryIdentity,
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity object contributor discovery started.");
            factBridge.EmitSnapshot(
                snapshots,
                "activity_object_contributor_discovery_started",
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity object contributor discovery started.");
            if (!HasLoadedSetForCurrentEntry(loadedSet, command.Identity, entrySequence) || !loadedSet.HasScenes)
            {
                inventoryState.ClearCurrentActivityObjectContributorDiscoveryResult();
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorDiscoverySkippedNoContent,
                    discoveryIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity object contributor discovery skipped reason='no_content_loaded_set'.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "activity_object_contributor_discovery_skipped_no_content",
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity object contributor discovery skipped reason='no_content_loaded_set'.");
                logSink.LogEntryOwnerEvent(
                    "ActivityEntryObjectContributorDiscoverySkipped",
                    discoveryIdentity,
                    command.Source,
                    command.Reason,
                    "owner='ActivityEntryObjectContributorDiscoveryStage' entryPipelineOwner='ActivityEntryPipeline' block='object_contributor_discovery' reason='no_content_loaded_set'");
                return default;
            }

            try
            {
                List<ActivityObjectContributionReport> reports = new();
                for (int sceneIndex = 0; sceneIndex < loadedSet.Scenes.Count; sceneIndex++)
                {
                    var record = loadedSet.Scenes[sceneIndex];
                    if (!record.IsValid)
                    {
                        continue;
                    }

                    var scene = SceneManager.GetSceneByName(record.SceneName);
                    if (!scene.IsValid() || !scene.isLoaded)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{command.Identity.ActivityId}' content scene '{record.SceneName}' is not loaded for object contributor discovery.");
                    }

                    AppendContributorsFromSceneOrFail(reports, scene, loadedSet, record, command.Source, command.Reason);
                }

                ActivityObjectContributorDiscoveryResult result = new(
                    discoveryIdentity,
                    loadedSet.ContentProfileId,
                    reports,
                    command.Source,
                    command.Reason,
                    $"discovered='{reports.Count}' contentProfileId='{loadedSet.ContentProfileId}'");
                if (!result.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Activity '{command.Identity.ActivityId}' produced invalid ActivityObjectContributorDiscoveryResult.");
                }

                inventoryState.SetCurrentActivityObjectContributorDiscoveryResult(result);

                for (int reportIndex = 0; reportIndex < reports.Count; reportIndex++)
                {
                    var report = reports[reportIndex];
                    factBridge.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectContributorDiscovered,
                        discoveryIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.Identity.ActivityId}' contributor discovered contentProfileId='{report.ContentProfileId}' sceneName='{report.SceneName}' targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetBoundaryEligibility='{ActivityResetBoundaryEligibilityFormatter.Format(report.ResetBoundaryEligibility)}' resetDescriptor='endpoint_inventory' descriptorMode='endpoint_inventory' releaseKinds='{FormatReleaseKinds(report.SupportedReleaseKinds)}'.");
                }

                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorDiscoveryCompleted,
                    discoveryIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity object contributor discovery completed discovered='{reports.Count}' contentProfileId='{loadedSet.ContentProfileId}'.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "activity_object_contributor_discovery_completed",
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity object contributor discovery completed discovered='{reports.Count}' contentProfileId='{loadedSet.ContentProfileId}'.");
                logSink.LogEntryOwnerEvent(
                    "ActivityEntryObjectContributorDiscoveryCompleted",
                    discoveryIdentity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryObjectContributorDiscoveryStage' entryPipelineOwner='ActivityEntryPipeline' block='object_contributor_discovery' discovered='{reports.Count}'");
                return result;
            }
            catch (Exception exception)
            {
                inventoryState.ClearCurrentActivityObjectContributorDiscoveryResult();
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorDiscoveryFailed,
                    discoveryIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity object contributor discovery failed error='{exception.Message}'.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "activity_object_contributor_discovery_failed",
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity object contributor discovery failed error='{exception.Message}'.");
                logSink.LogEntryOwnerEvent(
                    "ActivityEntryObjectContributorDiscoveryFailed",
                    discoveryIdentity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryObjectContributorDiscoveryStage' entryPipelineOwner='ActivityEntryPipeline' block='object_contributor_discovery' error='{exception.Message}'");
                throw;
            }
        }

        private static void AppendContributorsFromSceneOrFail(
            List<ActivityObjectContributionReport> reports,
            Scene contentScene,
            ActivityContentLoadedSet loadedSet,
            ActivityContentLoadedSceneRecord record,
            string source,
            string reason)
        {
            GameObject[] roots = contentScene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                ActivityObjectContributor[] contributors = roots[rootIndex].GetComponentsInChildren<ActivityObjectContributor>(true);
                for (int contributorIndex = 0; contributorIndex < contributors.Length; contributorIndex++)
                {
                    var contributor = contributors[contributorIndex];
                    if (contributor == null)
                    {
                        continue;
                    }

                    contributor.ValidateOrThrow(
                        $"ActivityObjectContributorDiscovery:{contentScene.name}:{rootIndex}:{contributorIndex}");

                    ActivityObjectContributionReport report = new(
                        loadedSet.Identity,
                        loadedSet.ContentProfileId,
                        null,
                        contentScene.name,
                        contributor.TargetId,
                        contributor.RoleId,
                        contributor.ContributorKind,
                        contributor.DefaultRequiredness,
                        contributor.ResetBoundaryEligibility,
                        contributor.SupportedReleaseKinds,
                        source,
                        reason);

                    if (!report.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Invalid ActivityObjectContributionReport targetId='{contributor.TargetId}' scene='{contentScene.name}'.");
                    }

                    reports.Add(report);
                }
            }
        }

        private static string FormatReleaseKinds(IReadOnlyList<ActivityReleaseRequirementKind> releaseKinds)
        {
            if (releaseKinds == null || releaseKinds.Count == 0)
            {
                return "<none>";
            }

            return string.Join(",", releaseKinds);
        }
    }
}
