using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
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
            logSink.LogEntryOwnerEvent(
                "ActivityEntryObjectContributorDiscoveryStarted",
                discoveryIdentity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryObjectSetupStages' entryPipelineOwner='ActivityEntryPipeline' block='object_contributor_discovery'");

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
                    "owner='ActivityEntryObjectSetupStages' entryPipelineOwner='ActivityEntryPipeline' block='object_contributor_discovery' reason='no_content_loaded_set'");
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
                        $"'{command.Identity.ActivityId}' contributor discovered contentProfileId='{report.ContentProfileId}' sceneName='{report.SceneName}' targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetGroups='{FormatActivityStateResetGroups(report.SupportedResetGroups)}' releaseKinds='{FormatReleaseKinds(report.SupportedReleaseKinds)}'.");
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
                    $"owner='ActivityEntryObjectSetupStages' entryPipelineOwner='ActivityEntryPipeline' block='object_contributor_discovery' discovered='{reports.Count}'");
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
                    $"owner='ActivityEntryObjectSetupStages' entryPipelineOwner='ActivityEntryPipeline' block='object_contributor_discovery' error='{exception.Message}'");
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
                        contributor.SupportedResetGroups,
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

        private static string FormatActivityStateResetGroups(IReadOnlyList<ActivityStateResetGroup> resetGroups)
        {
            if (resetGroups == null || resetGroups.Count == 0)
            {
                return "<none>";
            }

            return string.Join(",", resetGroups);
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

    internal static class ActivityEntrySetupInventoryStage
    {
        public static void Execute(
            ActivityEntryObjectSetupCommand command,
            ActivitySetupInventoryBuilder builder,
            ActivitySetupInventoryValidator validator,
            IActivityEntryRuntimeBridge endpoint,
            ActivityEntryInventoryRuntimeState inventoryState,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            int entrySequence = command.Identity.EntrySequence;
            var buildStartedIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActivitySetupInventoryBuildStarted);
            endpoint.SetCurrentIdentity(buildStartedIdentity, SessionActivityStage.ActivitySetupInventoryBuildStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivitySetupInventoryBuildStarted,
                buildStartedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' activity setup inventory build started.");
            endpoint.EmitSnapshot(
                snapshots,
                "activity_setup_inventory_build_started",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' activity setup inventory build started.");

            var buildResult = builder.Build(command.Plan);
            if (buildResult.IsFailed || !buildResult.IsValid)
            {
                var failedIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActivitySetupInventoryValidationFailed);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivitySetupInventoryValidationFailed);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivitySetupInventoryValidationFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity setup inventory build failed. message='{buildResult.Message}'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_setup_inventory_build_failed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity setup inventory build failed. message='{buildResult.Message}'.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActivityEntryPipeline][ActivitySetupInventory] Build failed activityId='{command.ActivityId}' entrySequence='{entrySequence}' message='{buildResult.Message}'.");
            }

            inventoryState.SetCurrentActivitySetupInventory(buildResult.Inventory);

            if (buildResult.IsSkipped)
            {
                var skippedIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActivitySetupInventorySkippedNoRequirements);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivitySetupInventorySkippedNoRequirements);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivitySetupInventorySkippedNoRequirements,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity setup inventory skipped because no requirements were declared. inventoryId='{buildResult.Inventory.InventoryId}'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_setup_inventory_skipped_no_requirements",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity setup inventory skipped because no requirements were declared.");
            }
            else
            {
                var builtIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActivitySetupInventoryBuilt);
                endpoint.SetCurrentIdentity(builtIdentity, SessionActivityStage.ActivitySetupInventoryBuilt);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivitySetupInventoryBuilt,
                    builtIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity setup inventory built inventoryId='{buildResult.Inventory.InventoryId}' totalRequirements='{buildResult.Inventory.TotalRequirementCount}'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_setup_inventory_built",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity setup inventory built totalRequirements='{buildResult.Inventory.TotalRequirementCount}'.");
            }

            var validationResult = validator.Validate(buildResult.Inventory, command.Source, command.Reason);
            if (validationResult.IsFailed || !validationResult.IsValid)
            {
                var failedIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActivitySetupInventoryValidationFailed);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivitySetupInventoryValidationFailed);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivitySetupInventoryValidationFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity setup inventory validation failed errors='{validationResult.Errors.Count}' message='{validationResult.Message}'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_setup_inventory_validation_failed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity setup inventory validation failed errors='{validationResult.Errors.Count}'.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActivityEntryPipeline][ActivitySetupInventory] Validation failed activityId='{command.ActivityId}' entrySequence='{entrySequence}' errors='{string.Join(" | ", validationResult.Errors)}'.");
            }

            var validatedIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActivitySetupInventoryValidated);
            endpoint.SetCurrentIdentity(validatedIdentity, SessionActivityStage.ActivitySetupInventoryValidated);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivitySetupInventoryValidated,
                validatedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' activity setup inventory validated inventoryId='{validationResult.Inventory.InventoryId}' totalRequirements='{validationResult.Inventory.TotalRequirementCount}' skipped='{validationResult.SkippedRequirementIds.Count}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "activity_setup_inventory_validated",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' activity setup inventory validated totalRequirements='{validationResult.Inventory.TotalRequirementCount}' skipped='{validationResult.SkippedRequirementIds.Count}'.");
        }
    }

    internal static class ActivityEntryObjectSnapshotContractValidationStage
    {
        public static void Execute(
            ActivityEntryObjectSetupCommand command,
            ActivityContentLoadedSet loadedSet,
            ActivityObjectContributorDiscoveryResult discoveryResult,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            int entrySequence = command.Identity.EntrySequence;
            var validationIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActivitySetupStarted);
            endpoint.SetCurrentIdentity(validationIdentity, SessionActivityStage.ActivitySetupStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotContractValidationStarted,
                validationIdentity,
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity object snapshot contract validation started.");

            if (!IsDiscoveryResultForCurrentEntryForIdentity(discoveryResult, command.Identity, entrySequence, validationIdentity) ||
                discoveryResult.Reports.Count == 0)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotContractValidationCompleted,
                    validationIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity object snapshot contract validation completed validationStarted='true' validatedCount='0' skippedCount='0' failedCount='0' targetIds='<none>' providerPaths='<none>' restoreEndpointPaths='<none>' targetTransformPaths='<none>' mismatchReason='<none>'.");
                return;
            }

            int validatedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;
            string mismatchReason = "<none>";
            HashSet<string> targetIds = new(StringComparer.Ordinal);
            HashSet<string> providerPaths = new(StringComparer.Ordinal);
            HashSet<string> restoreEndpointPaths = new(StringComparer.Ordinal);
            HashSet<string> targetTransformPaths = new(StringComparer.Ordinal);

            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                var report = discoveryResult.Reports[reportIndex];
                if (!IsReportForCurrentEntryForIdentity(report, command.Identity, entrySequence, validationIdentity))
                {
                    continue;
                }

                targetIds.Add(report.TargetId);
                bool required = report.Requiredness == ActivitySetupRequirementRequiredness.Required;
                var targetObject = ResolveContributorObjectOrFailForActivityId(loadedSet, command.Identity.ActivityId, report);
                IActivityObjectSnapshotProvider[] providers = ResolveObjectSnapshotProviders(targetObject);
                IActivityObjectSnapshotRestoreEndpoint[] restoreEndpoints = ResolveObjectSnapshotRestoreEndpoints(targetObject);

                bool providerFound = TryResolveSupportingSnapshotProvider(report.TargetId, providers, out var provider);
                bool restoreFound = TryResolveSupportingSnapshotRestoreEndpoint(report.TargetId, restoreEndpoints, out var restoreEndpoint);

                string providerPath = "<none>";
                string providerTargetTransformPath = "<none>";
                string providerFailureReason = providerFound ? "<none>" : "snapshot_provider_missing";
                if (providerFound && provider is IActivityObjectSnapshotProviderContractView providerView)
                {
                    bool providerValid = providerView.TryDescribeContract(report.TargetId, out providerPath, out providerTargetTransformPath, out providerFailureReason);
                    if (!providerValid && string.IsNullOrWhiteSpace(providerFailureReason))
                    {
                        providerFailureReason = "snapshot_provider_contract_invalid";
                    }
                }
                else if (providerFound)
                {
                    providerFailureReason = "snapshot_provider_contract_view_missing";
                }

                string restorePath = "<none>";
                string restoreTargetTransformPath = "<none>";
                string restoreFailureReason = restoreFound ? "<none>" : "snapshot_restore_endpoint_missing";
                if (restoreFound && restoreEndpoint is IActivityObjectSnapshotRestoreEndpointContractView restoreView)
                {
                    bool restoreValid = restoreView.TryDescribeContract(report.TargetId, out restorePath, out restoreTargetTransformPath, out restoreFailureReason);
                    if (!restoreValid && string.IsNullOrWhiteSpace(restoreFailureReason))
                    {
                        restoreFailureReason = "snapshot_restore_contract_invalid";
                    }
                }
                else if (restoreFound)
                {
                    restoreFailureReason = "snapshot_restore_contract_view_missing";
                }

                providerPaths.Add(string.IsNullOrWhiteSpace(providerPath) ? "<none>" : providerPath);
                restoreEndpointPaths.Add(string.IsNullOrWhiteSpace(restorePath) ? "<none>" : restorePath);
                if (!string.IsNullOrWhiteSpace(providerTargetTransformPath) && !string.Equals(providerTargetTransformPath, "<none>", StringComparison.Ordinal))
                {
                    targetTransformPaths.Add(providerTargetTransformPath);
                }

                if (!string.IsNullOrWhiteSpace(restoreTargetTransformPath) && !string.Equals(restoreTargetTransformPath, "<none>", StringComparison.Ordinal))
                {
                    targetTransformPaths.Add(restoreTargetTransformPath);
                }

                bool providerContractValid = providerFound && string.Equals(providerFailureReason, "resolved", StringComparison.Ordinal);
                bool restoreContractValid = restoreFound && string.Equals(restoreFailureReason, "resolved", StringComparison.Ordinal);
                bool transformMismatch = providerContractValid &&
                                         restoreContractValid &&
                                         !string.Equals(providerTargetTransformPath, restoreTargetTransformPath, StringComparison.Ordinal);
                string failureReason = transformMismatch
                    ? "snapshot_restore_target_transform_mismatch"
                    : ResolveSnapshotContractFailureReason(providerFailureReason, restoreFailureReason, providerFound, restoreFound);

                bool hasDeclaredSnapshotCapability = providerFound || restoreFound;
                bool hasNoSnapshotCapability = !providerFound && !restoreFound;
                bool contractValid = providerContractValid && restoreContractValid && !transformMismatch;

                if (hasDeclaredSnapshotCapability && contractValid)
                {
                    validatedCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotContractValidated,
                        validationIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.Identity.ActivityId}' activity object snapshot contract validated targetId='{report.TargetId}' requiredness='{report.Requiredness}' providerPath='{providerPath}' restoreEndpointPath='{restorePath}' targetTransformPath='{providerTargetTransformPath}'.");
                    continue;
                }

                if (hasNoSnapshotCapability && !required)
                {
                    skippedCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotContractSkippedOptional,
                        validationIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.Identity.ActivityId}' activity object snapshot contract skipped optional targetId='{report.TargetId}' requiredness='{report.Requiredness}' reason='snapshot_capability_not_declared_optional' providerPath='{providerPath}' restoreEndpointPath='{restorePath}' targetTransformPath='<none>'.");
                    continue;
                }

                failedCount += 1;
                if (transformMismatch)
                {
                    mismatchReason = "snapshot_restore_target_transform_mismatch";
                }
                else if (!string.Equals(failureReason, "<none>", StringComparison.Ordinal))
                {
                    mismatchReason = failureReason;
                }

                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotContractFailed,
                    validationIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity object snapshot contract failed targetId='{report.TargetId}' requiredness='{report.Requiredness}' reason='{failureReason}' providerPath='{providerPath}' restoreEndpointPath='{restorePath}' providerTargetTransformPath='{providerTargetTransformPath}' restoreTargetTransformPath='{restoreTargetTransformPath}'.");
                throw new InvalidOperationException(
                    $"snapshot_contract_validation_failed: activityId='{command.Identity.ActivityId}' targetId='{report.TargetId}' reason='{failureReason}' providerPath='{providerPath}' restoreEndpointPath='{restorePath}' providerTargetTransformPath='{providerTargetTransformPath}' restoreTargetTransformPath='{restoreTargetTransformPath}'.");
            }

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotContractValidationCompleted,
                validationIdentity,
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity object snapshot contract validation completed validationStarted='true' validatedCount='{validatedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' targetIds='{JoinValues(targetIds)}' providerPaths='{JoinValues(providerPaths)}' restoreEndpointPaths='{JoinValues(restoreEndpointPaths)}' targetTransformPaths='{JoinValues(targetTransformPaths)}' mismatchReason='{mismatchReason}'.");
        }
    }

    internal static class ActivityEntryCapabilityInventoryPreviewStage
    {
        public static ActivityCapabilityInventoryBuildResult Execute(
            ActivityEntryObjectSetupCommand command,
            ActivityObjectContributorDiscoveryResult discoveryResult,
            IReadOnlyList<ActorScanTarget> actorTargets,
            ActivityCapabilityInventoryCoordinator coordinator,
            IActivityEntryRuntimeBridge endpoint,
            ActivityEntryInventoryRuntimeState inventoryState,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            int entrySequence = command.Identity.EntrySequence;
            var previewIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActivitySetupStarted);
            endpoint.SetCurrentIdentity(previewIdentity, SessionActivityStage.ActivitySetupStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityCapabilityInventoryPreviewStarted,
                previewIdentity,
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity capability inventory preview started scannerId='{coordinator.ActivityObjectScannerId}'.");
            EmitEntryCapabilityInventoryLog(
                SessionActivityFactKind.ActivityCapabilityInventoryPreviewStarted,
                previewIdentity,
                $"'{command.Identity.ActivityId}' activity capability inventory preview started scannerId='{coordinator.ActivityObjectScannerId}'.");

            bool hasDiscoveryForCurrentEntry = IsDiscoveryResultForCurrentEntryForIdentity(discoveryResult, command.Identity, entrySequence, previewIdentity);
            bool hasActorTargets = actorTargets is { Count: > 0 };
            if (!hasDiscoveryForCurrentEntry && !hasActorTargets)
            {
                inventoryState.ClearCurrentActivityCapabilityInventoryPreview();
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityCapabilityInventoryPreviewSkippedNoDiscovery,
                    previewIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity capability inventory preview skipped reason='no_capability_sources' entrySequence='{entrySequence}' scannerId='{coordinator.ActivityObjectScannerId}'.");
                EmitEntryCapabilityInventoryLog(
                    SessionActivityFactKind.ActivityCapabilityInventoryPreviewSkippedNoDiscovery,
                    previewIdentity,
                    $"'{command.Identity.ActivityId}' activity capability inventory preview skipped reason='no_capability_sources' entrySequence='{entrySequence}' scannerId='{coordinator.ActivityObjectScannerId}'.");
                return default;
            }

            var buildResult = coordinator.BuildForEntry(
                previewIdentity,
                hasDiscoveryForCurrentEntry ? discoveryResult : default,
                actorTargets,
                command.Source,
                command.Reason);
            var inventory = buildResult.Inventory;
            var validationResult = buildResult.Validation;
            inventoryState.SetCurrentActivityCameraBindingContributions(buildResult.CameraBindingContributions);
            inventoryState.SetCurrentActivityAttributeSetupContributions(buildResult.AttributeSetupContributions);
            inventoryState.SetCurrentActivityPresentationSetupContributions(buildResult.PresentationSetupContributions);
            inventoryState.SetCurrentActivityPermissionReceiverContributions(buildResult.PermissionReceiverContributions);
            string capabilityKindsSummary = FormatCapabilityKindsSummary(inventory.Capabilities);
            inventoryState.SetCurrentActivityCapabilityInventoryPreview(inventory, validationResult);
            string validationIssueCodes = FormatValidationIssueCodes(validationResult.Issues);
            string activityObjectLifecycleCapabilityKinds = buildResult.ActivityObjectLifecycleCapabilityKindsSummary;
            string actorLifecycleCapabilityKinds = buildResult.ActorLifecycleCapabilityKindsSummary;

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityCapabilityInventoryValidationStarted,
                previewIdentity,
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity capability inventory validation started entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' scannerId='{coordinator.ActivityObjectScannerId}'.");
            EmitEntryCapabilityInventoryLog(
                SessionActivityFactKind.ActivityCapabilityInventoryValidationStarted,
                previewIdentity,
                $"'{command.Identity.ActivityId}' activity capability inventory validation started entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' scannerId='{coordinator.ActivityObjectScannerId}'.");

            var validationOutcomeKind = validationResult.Status switch
            {
                ActivityCapabilityInventoryValidationStatus.Passed => SessionActivityFactKind.ActivityCapabilityInventoryValidationPassed,
                ActivityCapabilityInventoryValidationStatus.PassedWithWarnings => SessionActivityFactKind.ActivityCapabilityInventoryValidationWarning,
                ActivityCapabilityInventoryValidationStatus.FailedPassive => SessionActivityFactKind.ActivityCapabilityInventoryValidationFailedPassive,
                _ => SessionActivityFactKind.ActivityCapabilityInventoryValidationWarning,
            };

            string validationOutcomeMessage =
                $"'{command.Identity.ActivityId}' activity capability inventory validation outcome status='{validationResult.Status}' entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{coordinator.ActivityObjectScannerId}'.";
            endpoint.EmitFact(
                facts,
                validationOutcomeKind,
                previewIdentity,
                command.Source,
                command.Reason,
                validationOutcomeMessage);
            EmitEntryCapabilityInventoryLog(validationOutcomeKind, previewIdentity, validationOutcomeMessage);

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityCapabilityInventoryValidationCompleted,
                previewIdentity,
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity capability inventory validation completed status='{validationResult.Status}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{coordinator.ActivityObjectScannerId}'.");
            EmitEntryCapabilityInventoryLog(
                SessionActivityFactKind.ActivityCapabilityInventoryValidationCompleted,
                previewIdentity,
                $"'{command.Identity.ActivityId}' activity capability inventory validation completed status='{validationResult.Status}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{coordinator.ActivityObjectScannerId}'.");

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityCapabilityInventoryPreviewObserved,
                previewIdentity,
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity capability inventory preview observed entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' capabilityKinds='{capabilityKindsSummary}' activityObjectLifecycleCapabilityCount='{buildResult.ActivityObjectLifecycleCapabilityCount}' activityObjectLifecycleCapabilityKinds='{activityObjectLifecycleCapabilityKinds}' actorLifecycleCapabilityCount='{buildResult.ActorLifecycleCapabilityCount}' actorLifecycleCapabilityKinds='{actorLifecycleCapabilityKinds}' unresolvedReports='{buildResult.UnresolvedReportCount}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{coordinator.ActivityObjectScannerId}' actorScannerId='{coordinator.ActorLifecycleScannerId}'.");
            EmitEntryCapabilityInventoryLog(
                SessionActivityFactKind.ActivityCapabilityInventoryPreviewObserved,
                previewIdentity,
                $"'{command.Identity.ActivityId}' activity capability inventory preview observed entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' capabilityKinds='{capabilityKindsSummary}' activityObjectLifecycleCapabilityCount='{buildResult.ActivityObjectLifecycleCapabilityCount}' activityObjectLifecycleCapabilityKinds='{activityObjectLifecycleCapabilityKinds}' actorLifecycleCapabilityCount='{buildResult.ActorLifecycleCapabilityCount}' actorLifecycleCapabilityKinds='{actorLifecycleCapabilityKinds}' unresolvedReports='{buildResult.UnresolvedReportCount}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{coordinator.ActivityObjectScannerId}' actorScannerId='{coordinator.ActorLifecycleScannerId}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "activity_capability_inventory_preview_observed",
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity capability inventory preview observed entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' capabilityKinds='{capabilityKindsSummary}' activityObjectLifecycleCapabilityCount='{buildResult.ActivityObjectLifecycleCapabilityCount}' activityObjectLifecycleCapabilityKinds='{activityObjectLifecycleCapabilityKinds}' actorLifecycleCapabilityCount='{buildResult.ActorLifecycleCapabilityCount}' actorLifecycleCapabilityKinds='{actorLifecycleCapabilityKinds}' unresolvedReports='{buildResult.UnresolvedReportCount}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{coordinator.ActivityObjectScannerId}' actorScannerId='{coordinator.ActorLifecycleScannerId}'.");

            return buildResult;
        }

        private static void EmitEntryCapabilityInventoryLog(
            SessionActivityFactKind kind,
            SessionActivityIdentity identity,
            string message)
        {
            Debug.Log(
                $"[OBS][ActivityEntryPipeline][CapabilityInventoryPreview] fact='{kind}' stage='{identity.Stage}' entrySequence='{identity.EntrySequence}' activity='{identity.ActivityId}' owner='ActivityEntryObjectSetupStages' entryPipelineOwner='ActivityEntryPipeline' block='capability_inventory_preview' message=\"{message}\"");
        }
    }

    internal static class ActivityEntryObjectResetStage
    {
        public static void Execute(
            ActivityEntryObjectSetupCommand command,
            ActivityObjectContributorDiscoveryResult discoveryResult,
            ActivityCapabilityInventory inventory,
            ActivityCapabilityInventoryValidationResult validation,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            var resetIdentity = command.Identity;
            int entrySequence = resetIdentity.EntrySequence;
            endpoint.SetCurrentIdentity(resetIdentity, SessionActivityStage.ActivitySetupStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ObjectResetStarted,
                resetIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' object reset started.");
            endpoint.EmitSnapshot(
                snapshots,
                "object_reset_started",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' object reset started.");

            if (!IsDiscoveryResultForCurrentEntryForIdentity(discoveryResult, resetIdentity, entrySequence, resetIdentity))
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ObjectResetCompleted,
                    resetIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.NoCommands}' completionReason='no_contributors_current_entry'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "object_reset_completed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.NoCommands}' completionReason='no_contributors_current_entry'.");
                return;
            }

            if (discoveryResult.Reports.Count == 0)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ObjectResetCompleted,
                    resetIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.NoCommands}' completionReason='no_reports_for_current_entry'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "object_reset_completed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.NoCommands}' completionReason='no_reports_for_current_entry'.");
                return;
            }

            int commandCount = 0;
            int appliedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;
            int noSupportedGroupsCount = 0;
            int reportEvaluatedCount = 0;
            bool hasRequiredContributor = HasRequiredResetContributor(discoveryResult, resetIdentity);
            bool hasValidResetInventory =
                inventory.IsValid &&
                validation.IsValid &&
                string.Equals(inventory.Id.PipelineId, resetIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.SessionStateId, resetIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.ActivityId, resetIdentity.ActivityId, StringComparison.Ordinal) &&
                inventory.Id.EntrySequence == resetIdentity.EntrySequence;

            if (!hasValidResetInventory)
            {
                if (hasRequiredContributor)
                {
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectResetFailed,
                        resetIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' object reset failed reason='required_reset_inventory_missing_or_invalid' entrySequence='{entrySequence}' inventoryValid='{inventory.IsValid.ToString().ToLowerInvariant()}' validationValid='{validation.IsValid.ToString().ToLowerInvariant()}'.");
                    throw new InvalidOperationException(
                        $"required_reset_inventory_missing_or_invalid: activityId='{command.ActivityId}' entrySequence='{entrySequence}'.");
                }

                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ObjectResetCompleted,
                    resetIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.InventoryInvalidOrStale}' completionReason='inventory_stale_or_invalid'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "object_reset_completed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.InventoryInvalidOrStale}' completionReason='inventory_stale_or_invalid'.");
                return;
            }

            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                var report = discoveryResult.Reports[reportIndex];
                if (!IsReportForCurrentEntryForIdentity(report, resetIdentity, entrySequence, resetIdentity))
                {
                    continue;
                }

                reportEvaluatedCount += 1;
                if (report.SupportedResetGroups == null || report.SupportedResetGroups.Count == 0)
                {
                    skippedCount += 1;
                    noSupportedGroupsCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectResetSkippedOptional,
                        resetIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' object reset skipped targetId='{report.TargetId}' reason='no_supported_reset_groups'.");
                    continue;
                }

                IActivityObjectResetEndpoint[] endpoints = ResolveObjectResetEndpointsFromInventory(inventory, report);
                for (int groupIndex = 0; groupIndex < report.SupportedResetGroups.Count; groupIndex++)
                {
                    var resetGroup = report.SupportedResetGroups[groupIndex];
                    if (resetGroup == ActivityStateResetGroup.Unknown)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{command.ActivityId}' reset group cannot be Unknown targetId='{report.TargetId}'.");
                    }

                    ActivityObjectResetCommand resetCommand = new(
                        resetIdentity,
                        report.TargetId,
                        report.RoleId,
                        report.ContributorKind,
                        report.Requiredness,
                        resetGroup,
                        command.Source,
                        command.Reason);
                    if (!resetCommand.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{command.ActivityId}' produced invalid object reset command targetId='{report.TargetId}' resetGroup='{resetGroup}'.");
                    }

                    commandCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectResetCommandIssued,
                        resetIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' object reset command issued targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetGroup='{resetGroup}'.");

                    var result = ExecuteObjectResetCommand(resetCommand, endpoints);
                    if (!IsObjectResetResultForCurrentEntry(result, resetIdentity, entrySequence, resetIdentity))
                    {
                        failedCount += 1;
                        endpoint.EmitFact(
                            facts,
                            SessionActivityFactKind.ObjectResetFailed,
                            resetIdentity,
                            command.Source,
                            command.Reason,
                        $"'{command.ActivityId}' object reset failed targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='stale_or_foreign_reset_result'.");
                        throw new InvalidOperationException(
                            $"stale_or_foreign_reset_result: activityId='{command.ActivityId}' targetId='{report.TargetId}' resetGroup='{resetGroup}'.");
                    }

                    if (result.IsApplied)
                    {
                        appliedCount += 1;
                        endpoint.EmitFact(
                            facts,
                            SessionActivityFactKind.ObjectResetApplied,
                            resetIdentity,
                            command.Source,
                            command.Reason,
                            $"'{command.ActivityId}' object reset applied targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetGroup='{resetGroup}'.");
                        continue;
                    }

                    if (result.IsSkippedOptional)
                    {
                        skippedCount += 1;
                        endpoint.EmitFact(
                            facts,
                            SessionActivityFactKind.ObjectResetSkippedOptional,
                            resetIdentity,
                            command.Source,
                            command.Reason,
                            $"'{command.ActivityId}' object reset skipped optional targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='{result.Message}'.");
                        continue;
                    }

                    failedCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectResetFailed,
                        resetIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' object reset failed targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='{result.Message}'.");
                    throw new InvalidOperationException(
                        $"object_reset_failed: activityId='{command.ActivityId}' targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='{result.Message}'.");
                }
            }

            ActivityResetCompletionKind finalCompletionKind;
            string finalCompletionReason;
            if (appliedCount > 0)
            {
                finalCompletionKind = ActivityResetCompletionKind.Applied;
                finalCompletionReason = "applied";
            }
            else if (commandCount <= 0 && reportEvaluatedCount <= 0)
            {
                finalCompletionKind = ActivityResetCompletionKind.NoCommands;
                finalCompletionReason = "no_reports_for_current_entry";
            }
            else if (commandCount <= 0 && noSupportedGroupsCount > 0)
            {
                finalCompletionKind = ActivityResetCompletionKind.NoApplicableGroups;
                finalCompletionReason = "no_supported_groups";
            }
            else if (commandCount <= 0)
            {
                finalCompletionKind = ActivityResetCompletionKind.NoCommands;
                finalCompletionReason = "no_applicable_reset_groups";
            }
            else if (skippedCount > 0)
            {
                finalCompletionKind = ActivityResetCompletionKind.SkippedOptional;
                finalCompletionReason = "skipped_optional";
            }
            else
            {
                finalCompletionKind = ActivityResetCompletionKind.NoCommands;
                finalCompletionReason = "no_commands";
            }

                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ObjectResetCompleted,
                    resetIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' object reset completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' completionKind='{finalCompletionKind}' completionReason='{finalCompletionReason}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "object_reset_completed",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' object reset completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' completionKind='{finalCompletionKind}' completionReason='{finalCompletionReason}'.");
        }
    }

    internal static class ActivityEntryObjectSnapshotRestoreStage
    {
        private const string RouteActivitySnapshotSchemaId = "progression.route_activity.object_snapshot.v1";
        private const string TransformSnapshotSchemaId = "activity_object.transform_snapshot.v1";
        private const string WorldTransformCoordinateSpace = "world_transform";

        public static void Execute(
            ActivityEntryObjectSetupCommand command,
            ActivityObjectContributorDiscoveryResult discoveryResult,
            ActivityCapabilityInventory inventory,
            ActivityCapabilityInventoryValidationResult validation,
            IActivityEntryRuntimeBridge endpoint,
            ActivityEntryObjectSnapshotRestorePayloadContext loadedSnapshotPayloadContext,
            List<SessionActivityFact> facts)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            int entrySequence = command.Identity.EntrySequence;
            var restoreIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActivitySetupStarted);
            endpoint.SetCurrentIdentity(restoreIdentity, SessionActivityStage.ActivitySetupStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotRestoreStarted,
                restoreIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' activity object snapshot restore started.");

            if (!loadedSnapshotPayloadContext.HasPayload)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoPayload,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore skipped reason='no_loaded_payload'.");
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore completed payloadAvailable='false' payloadKind='<none>' canonicalPayload='<none>' recordCount='0' matchedRecordCount='0' matchedTargetCount='0' restoredCount='0' restoreFailed='false'.");
                return;
            }

            var loadedPayload = loadedSnapshotPayloadContext.Payload;

            if (!IsLoadedSnapshotPayloadForCurrentActivity(loadedPayload, command.Identity.SessionId, command.ActivityId))
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore failed reason='payload_foreign_or_stale' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' payloadSessionStateId='{loadedPayload.SessionStateId}' payloadActivityId='{loadedPayload.ActivityId}' payloadSourceEntrySequence='{loadedPayload.SourceEntrySequence}' recordCount='{loadedPayload.RecordCount}'.");
                throw new InvalidOperationException(
                    $"payload_foreign_or_stale: activityId='{command.ActivityId}' entrySequence='{entrySequence}' payloadSessionStateId='{loadedPayload.SessionStateId}' payloadActivityId='{loadedPayload.ActivityId}' payloadSourceEntrySequence='{loadedPayload.SourceEntrySequence}'.");
            }

            if (!TryBuildActivityObjectTransformPayloadByTargetId(
                    loadedPayload.CapabilitySnapshotEnvelope,
                    out Dictionary<string, ActivityObjectTransformSnapshotPayload> payloadByTargetId,
                    out int matchedRecordCount,
                    out string failureReason,
                    out string failureDetail))
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore failed reason='{failureReason}' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' detail='{failureDetail}'.");
                throw new InvalidOperationException(
                    $"{failureReason}: activityId='{command.ActivityId}' entrySequence='{entrySequence}' detail='{failureDetail}'.");
            }

            if (matchedRecordCount == 0)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoMatchingTarget,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore skipped reason='payload_has_no_activity_object_transform_records' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' matchedRecordCount='0'.");
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore completed payloadAvailable='true' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' matchedRecordCount='0' matchedTargetCount='0' restoredCount='0' restoreFailed='false'.");
                return;
            }

            if (!IsDiscoveryResultForCurrentEntryForIdentity(discoveryResult, command.Identity, entrySequence, restoreIdentity) ||
                discoveryResult.Reports.Count == 0)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoMatchingTarget,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore skipped reason='payload_has_no_matching_target_for_entry' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' matchedRecordCount='{matchedRecordCount}'.");
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore completed payloadAvailable='true' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' matchedRecordCount='{matchedRecordCount}' matchedTargetCount='0' restoredCount='0' restoreFailed='false'.");
                return;
            }

            int matchedTargetCount = 0;
            int restoredCount = 0;
            bool restoreFailed = false;
            HashSet<string> matchedTargetIds = new(StringComparer.Ordinal);
            bool hasValidRestoreInventory =
                inventory.IsValid &&
                validation.IsValid &&
                string.Equals(inventory.Id.PipelineId, restoreIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.SessionStateId, restoreIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.ActivityId, restoreIdentity.ActivityId, StringComparison.Ordinal) &&
                inventory.Id.EntrySequence == restoreIdentity.EntrySequence;

            if (!hasValidRestoreInventory)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore failed reason='restore_inventory_missing_or_invalid' entrySequence='{entrySequence}' inventoryValid='{inventory.IsValid.ToString().ToLowerInvariant()}' validationValid='{validation.IsValid.ToString().ToLowerInvariant()}'.");
                throw new InvalidOperationException(
                    $"restore_inventory_missing_or_invalid: activityId='{command.ActivityId}' entrySequence='{entrySequence}'.");
            }

            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                var report = discoveryResult.Reports[reportIndex];
                if (!IsReportForCurrentEntryForIdentity(report, restoreIdentity, entrySequence, restoreIdentity))
                {
                    continue;
                }

                if (!payloadByTargetId.TryGetValue(report.TargetId, out var payloadObject) || !payloadObject.IsValid)
                {
                    continue;
                }

                matchedTargetCount += 1;
                matchedTargetIds.Add(report.TargetId);
                IActivityObjectSnapshotRestoreEndpoint[] endpoints = ResolveObjectSnapshotRestoreEndpointsFromInventory(inventory, report);
                ActivityObjectSnapshotRestoreCommand restoreCommand = new(
                    restoreIdentity,
                    report.TargetId,
                    ActivityObjectSnapshotCoordinateSpace.WorldTransform,
                    payloadObject.PositionX,
                    payloadObject.PositionY,
                    payloadObject.PositionZ,
                    payloadObject.RotationX,
                    payloadObject.RotationY,
                    payloadObject.RotationZ,
                    payloadObject.RotationW,
                    payloadObject.ScaleX,
                    payloadObject.ScaleY,
                    payloadObject.ScaleZ,
                    command.Source,
                    command.Reason);

                var result = ExecuteObjectSnapshotRestoreCommand(restoreCommand, endpoints, report);
                if (!IsObjectSnapshotRestoreResultForCurrentEntry(result, restoreIdentity, entrySequence, restoreIdentity))
                {
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                        restoreIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' activity object snapshot restore failed reason='restore_result_invalid_or_failed_required' targetId='{report.TargetId}' detail='{result.Detail}'.");
                    throw new InvalidOperationException(
                        $"restore_result_invalid_or_failed_required: activityId='{command.ActivityId}' targetId='{report.TargetId}' detail='{result.Detail}'.");
                }

                if (result.IsRestored)
                {
                    restoredCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotRestoreApplied,
                        restoreIdentity,
                        command.Source,
                        command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore applied targetId='{report.TargetId}' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' payloadSchemaId='{TransformSnapshotSchemaId}' coordinateSpace='{ToCoordinateSpaceToken(restoreCommand.CoordinateSpace)}' payloadPosition='({payloadObject.PositionX:0.###},{payloadObject.PositionY:0.###},{payloadObject.PositionZ:0.###})' beforePosition='({result.BeforePositionX:0.###},{result.BeforePositionY:0.###},{result.BeforePositionZ:0.###})' afterPosition='({result.AfterPositionX:0.###},{result.AfterPositionY:0.###},{result.AfterPositionZ:0.###})' restoreVerified='{result.RestoreVerified.ToString().ToLowerInvariant()}' hasTransformPayload='true' detail='{result.Detail}'.");
                    continue;
                }

                if (result.IsSkippedOptional)
                {
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoEndpointOptional,
                        restoreIdentity,
                        command.Source,
                        command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore skipped optional targetId='{report.TargetId}' reason='{result.Detail}'.");
                    continue;
                }

                restoreFailed = true;
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore failed reason='restore_endpoint_missing_required' targetId='{report.TargetId}' detail='{result.Detail}'.");
                throw new InvalidOperationException(
                    $"restore_endpoint_missing_required: activityId='{command.ActivityId}' targetId='{report.TargetId}' detail='{result.Detail}'.");
            }

            if (matchedTargetCount == 0)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoMatchingTarget,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore skipped reason='payload_has_no_matching_target_for_entry' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' matchedRecordCount='{matchedRecordCount}'.");
            }

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted,
                restoreIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' activity object snapshot restore completed payloadAvailable='true' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' matchedRecordCount='{matchedRecordCount}' matchedTargetCount='{matchedTargetCount}' restoredCount='{restoredCount}' targetIds='{JoinValues(matchedTargetIds)}' appliedTargetIds='{JoinValues(matchedTargetIds)}' failedTargetIds='<none>' coordinateSpace='world_transform' restoreVerified='{(!restoreFailed && restoredCount == matchedTargetCount).ToString().ToLowerInvariant()}' restoreFailed='{restoreFailed.ToString().ToLowerInvariant()}'.");
        }

        private static bool IsLoadedSnapshotPayloadForCurrentActivity(
            LoadedRouteActivitySnapshotPayload loadedPayload,
            string sessionId,
            string activityId)
        {
            return loadedPayload.IsValid &&
                   string.Equals(loadedPayload.SchemaId, RouteActivitySnapshotSchemaId, StringComparison.Ordinal) &&
                   string.Equals(loadedPayload.SessionStateId, sessionId, StringComparison.Ordinal) &&
                   string.Equals(loadedPayload.ActivityId, activityId, StringComparison.Ordinal) &&
                   loadedPayload.SourceEntrySequence > 0;
        }

        private static bool TryBuildActivityObjectTransformPayloadByTargetId(
            ActivityCapabilitySnapshotEnvelope envelope,
            out Dictionary<string, ActivityObjectTransformSnapshotPayload> payloadByTargetId,
            out int matchedRecordCount,
            out string failureReason,
            out string failureDetail)
        {
            payloadByTargetId = new Dictionary<string, ActivityObjectTransformSnapshotPayload>(StringComparer.Ordinal);
            matchedRecordCount = 0;
            failureReason = string.Empty;
            failureDetail = string.Empty;

            if (!envelope.IsValid || envelope.Records == null)
            {
                failureReason = "capability_snapshot_envelope_invalid";
                failureDetail = "Envelope missing or invalid.";
                return false;
            }

            for (int index = 0; index < envelope.Records.Count; index++)
            {
                var record = envelope.Records[index];
                if (!IsActivityObjectTransformRecord(record))
                {
                    continue;
                }

                matchedRecordCount += 1;
                string targetId = Normalize(record.OwnerId);
                if (string.IsNullOrWhiteSpace(targetId))
                {
                    failureReason = "activity_object_snapshot_record_owner_missing";
                    failureDetail = $"recordIndex='{index}'";
                    return false;
                }

                ActivityObjectTransformSnapshotPayloadDto dto;
                try
                {
                    dto = JsonUtility.FromJson<ActivityObjectTransformSnapshotPayloadDto>(record.Payload);
                }
                catch (Exception ex)
                {
                    failureReason = "activity_object_snapshot_record_payload_invalid_json";
                    failureDetail = $"recordIndex='{index}' ownerId='{targetId}' exception='{ex.GetType().Name}:{Normalize(ex.Message)}'";
                    return false;
                }

                if (dto == null)
                {
                    failureReason = "activity_object_snapshot_record_payload_invalid_json";
                    failureDetail = $"recordIndex='{index}' ownerId='{targetId}'";
                    return false;
                }

                string payloadTargetId = Normalize(dto.targetId);
                if (!string.IsNullOrWhiteSpace(payloadTargetId) && !string.Equals(payloadTargetId, targetId, StringComparison.Ordinal))
                {
                    failureReason = "activity_object_snapshot_record_target_mismatch";
                    failureDetail = $"recordIndex='{index}' ownerId='{targetId}' payloadTargetId='{payloadTargetId}'";
                    return false;
                }

                string coordinateSpace = Normalize(dto.coordinateSpace);
                if (!string.Equals(coordinateSpace, WorldTransformCoordinateSpace, StringComparison.Ordinal))
                {
                    failureReason = "activity_object_snapshot_record_coordinate_space_unsupported";
                    failureDetail = $"recordIndex='{index}' ownerId='{targetId}' coordinateSpace='{coordinateSpace}'";
                    return false;
                }

                if (payloadByTargetId.ContainsKey(targetId))
                {
                    failureReason = "activity_object_snapshot_record_duplicate_target";
                    failureDetail = $"recordIndex='{index}' ownerId='{targetId}'";
                    return false;
                }

                payloadByTargetId.Add(
                    targetId,
                    new ActivityObjectTransformSnapshotPayload(
                        targetId,
                        dto.position.x,
                        dto.position.y,
                        dto.position.z,
                        dto.rotation.x,
                        dto.rotation.y,
                        dto.rotation.z,
                        dto.rotation.w,
                        dto.scale.x,
                        dto.scale.y,
                        dto.scale.z));
            }

            return true;
        }

        private static bool IsActivityObjectTransformRecord(ActivityCapabilitySnapshotRecord record)
        {
            return record.OwnerKind == ActivityCapabilitySnapshotOwnerKind.ActivityObject &&
                string.Equals(record.PayloadSchemaId, TransformSnapshotSchemaId, StringComparison.Ordinal) &&
                record is { PayloadSchemaVersion: > 0, PayloadFormat: ActivityCapabilitySnapshotPayloadFormat.Json } &&
                !string.IsNullOrWhiteSpace(record.Payload);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private readonly struct ActivityObjectTransformSnapshotPayload
        {
            public ActivityObjectTransformSnapshotPayload(
                string targetId,
                float positionX,
                float positionY,
                float positionZ,
                float rotationX,
                float rotationY,
                float rotationZ,
                float rotationW,
                float scaleX,
                float scaleY,
                float scaleZ)
            {
                TargetId = Normalize(targetId);
                PositionX = positionX;
                PositionY = positionY;
                PositionZ = positionZ;
                RotationX = rotationX;
                RotationY = rotationY;
                RotationZ = rotationZ;
                RotationW = rotationW;
                ScaleX = scaleX;
                ScaleY = scaleY;
                ScaleZ = scaleZ;
            }

            public string TargetId { get; }
            public float PositionX { get; }
            public float PositionY { get; }
            public float PositionZ { get; }
            public float RotationX { get; }
            public float RotationY { get; }
            public float RotationZ { get; }
            public float RotationW { get; }
            public float ScaleX { get; }
            public float ScaleY { get; }
            public float ScaleZ { get; }
            public bool IsValid => !string.IsNullOrWhiteSpace(TargetId);
        }

        [Serializable]
        private sealed class ActivityObjectTransformSnapshotPayloadDto
        {
            public string targetId;
            public string contentProfileId;
            public string coordinateSpace;
            public Vector3Dto position;
            public QuaternionDto rotation;
            public Vector3Dto scale;
        }

        [Serializable]
        private struct Vector3Dto
        {
            public float x;
            public float y;
            public float z;
        }

        [Serializable]
        private struct QuaternionDto
        {
            public float x;
            public float y;
            public float z;
            public float w;
        }
    }

    internal static class ActivityEntryObjectSetupStageUtility
    {
        public static SessionActivityIdentity BuildIdentityFromCommandIdentity(
            SessionActivityIdentity identity,
            SessionActivityStage stage)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("SessionActivityIdentity is invalid.");
            }

            return new SessionActivityIdentity(
                identity.PipelineId,
                identity.SessionId,
                identity.ActivityId,
                identity.ActivityOrdinal,
                identity.EntrySequence,
                stage,
                identity.Source);
        }

        public static bool HasLoadedSetForCurrentEntry(
            ActivityContentLoadedSet loadedSet,
            SessionActivityIdentity identity,
            int entrySequence)
        {
            return loadedSet is { IsValid: true, Identity: { Stage: SessionActivityStage.ActivityContentLoadedSetReady } } &&
                string.Equals(loadedSet.Identity.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(loadedSet.Identity.SessionId, identity.SessionId, StringComparison.Ordinal) &&
                string.Equals(loadedSet.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                loadedSet.Identity.ActivityOrdinal == identity.ActivityOrdinal &&
                loadedSet.Identity.EntrySequence == entrySequence;
        }

        public static bool IsDiscoveryResultForCurrentEntryForIdentity(
            ActivityObjectContributorDiscoveryResult result,
            SessionActivityIdentity identity,
            int entrySequence,
            SessionActivityIdentity currentIdentity)
        {
            return result is { IsValid: true, Identity: { IsValid: true } } &&
                string.Equals(result.Identity.PipelineId, currentIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(result.Identity.SessionId, currentIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(result.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                result.Identity.ActivityOrdinal == identity.ActivityOrdinal &&
                result.Identity.EntrySequence == entrySequence;
        }

        public static bool IsReportForCurrentEntryForIdentity(
            ActivityObjectContributionReport report,
            SessionActivityIdentity identity,
            int entrySequence,
            SessionActivityIdentity currentIdentity)
        {
            return report is { IsValid: true, Identity: { IsValid: true } } &&
                string.Equals(report.Identity.PipelineId, currentIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(report.Identity.SessionId, currentIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(report.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                report.Identity.ActivityOrdinal == identity.ActivityOrdinal &&
                report.Identity.EntrySequence == entrySequence;
        }

        public static GameObject ResolveContributorObjectOrFailForActivityId(
            ActivityContentLoadedSet loadedSet,
            string activityId,
            ActivityObjectContributionReport report)
        {
            for (int sceneIndex = 0; sceneIndex < loadedSet.Scenes.Count; sceneIndex++)
            {
                var sceneRecord = loadedSet.Scenes[sceneIndex];
                if (!sceneRecord.IsValid || !string.Equals(sceneRecord.SceneName, report.SceneName, StringComparison.Ordinal))
                {
                    continue;
                }

                var scene = SceneManager.GetSceneByName(sceneRecord.SceneName);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
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

                        if (!string.Equals(contributor.TargetId, report.TargetId, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        return contributor.gameObject;
                    }
                }
            }

            throw new InvalidOperationException(
                $"Activity '{activityId}' could not resolve contributor object for targetId='{report.TargetId}' sceneName='{report.SceneName}'.");
        }

        public static bool IsDiscoveryResultForCurrentEntry(
            ActivityObjectContributorDiscoveryResult result,
            SessionActivityIdentity identity,
            int entrySequence)
        {
            return result is { IsValid: true, Identity: { IsValid: true } } &&
                string.Equals(result.Identity.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(result.Identity.SessionId, identity.SessionId, StringComparison.Ordinal) &&
                string.Equals(result.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                result.Identity.ActivityOrdinal == identity.ActivityOrdinal &&
                result.Identity.EntrySequence == entrySequence;
        }

        public static bool IsReportForCurrentEntry(
            ActivityObjectContributionReport report,
            SessionActivityIdentity identity,
            int entrySequence)
        {
            return report is { IsValid: true, Identity: { IsValid: true } } &&
                string.Equals(report.Identity.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(report.Identity.SessionId, identity.SessionId, StringComparison.Ordinal) &&
                string.Equals(report.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                report.Identity.ActivityOrdinal == identity.ActivityOrdinal &&
                report.Identity.EntrySequence == entrySequence;
        }

        public static bool HasRequiredResetContributor(
            ActivityObjectContributorDiscoveryResult discoveryResult,
            SessionActivityIdentity identity)
        {
            if (!discoveryResult.IsValid || discoveryResult.Reports == null)
            {
                return false;
            }

            for (int index = 0; index < discoveryResult.Reports.Count; index++)
            {
                var report = discoveryResult.Reports[index];
                if (!report.IsValid)
                {
                    continue;
                }

                if (!string.Equals(report.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) ||
                    report.Identity.ActivityOrdinal != identity.ActivityOrdinal ||
                    report.Identity.EntrySequence != identity.EntrySequence)
                {
                    continue;
                }

                if (report is { Requiredness: ActivitySetupRequirementRequiredness.Required, SupportedResetGroups: { Count: > 0 } })
                {
                    return true;
                }
            }

            return false;
        }

        public static IActivityObjectResetEndpoint[] ResolveObjectResetEndpointsFromInventory(
            ActivityCapabilityInventory inventory,
            ActivityObjectContributionReport report)
        {
            if (!inventory.IsValid || !report.IsValid)
            {
                return Array.Empty<IActivityObjectResetEndpoint>();
            }

            List<IActivityObjectResetEndpoint> endpoints = new();
            HashSet<IActivityObjectResetEndpoint> unique = new();
            for (int index = 0; index < inventory.Capabilities.Count; index++)
            {
                var capability = inventory.Capabilities[index];
                if (capability.CapabilityKind != ActivityCapabilityKind.ResetEndpoint)
                {
                    continue;
                }

                if (!TryGetPolicyValue(capability.PolicyMetadata, "targetId", out string targetId) ||
                    !string.Equals(targetId, report.TargetId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!inventory.TryGetRuntimeReference(capability.CapabilityId, out ActivityObjectResetEndpointReference runtimeReference) ||
                    runtimeReference.Endpoint == null)
                {
                    continue;
                }

                if (unique.Add(runtimeReference.Endpoint))
                {
                    endpoints.Add(runtimeReference.Endpoint);
                }
            }

            return endpoints.ToArray();
        }

        public static IActivityObjectSnapshotRestoreEndpoint[] ResolveObjectSnapshotRestoreEndpointsFromInventory(
            ActivityCapabilityInventory inventory,
            ActivityObjectContributionReport report)
        {
            if (!inventory.IsValid || !report.IsValid)
            {
                return Array.Empty<IActivityObjectSnapshotRestoreEndpoint>();
            }

            List<IActivityObjectSnapshotRestoreEndpoint> endpoints = new();
            HashSet<IActivityObjectSnapshotRestoreEndpoint> unique = new();
            for (int index = 0; index < inventory.Capabilities.Count; index++)
            {
                var capability = inventory.Capabilities[index];
                if (capability.CapabilityKind != ActivityCapabilityKind.SnapshotRestoreEndpoint)
                {
                    continue;
                }

                if (!TryGetPolicyValue(capability.PolicyMetadata, "targetId", out string targetId) ||
                    !string.Equals(targetId, report.TargetId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!inventory.TryGetRuntimeReference(capability.CapabilityId, out ActivityObjectSnapshotRestoreEndpointReference runtimeReference) ||
                    runtimeReference.Endpoint == null)
                {
                    continue;
                }

                if (unique.Add(runtimeReference.Endpoint))
                {
                    endpoints.Add(runtimeReference.Endpoint);
                }
            }

            return endpoints.ToArray();
        }

        public static ActivityObjectResetResult ExecuteObjectResetCommand(
            ActivityObjectResetCommand command,
            IActivityObjectResetEndpoint[] endpoints)
        {
            if (endpoints == null || endpoints.Length == 0)
            {
                if (command.IsRequired)
                {
                    return new ActivityObjectResetResult(
                        ActivityObjectResetResultKind.Failed,
                        command,
                        command.Source,
                        command.Reason,
                        "required_reset_endpoint_missing");
                }

                return new ActivityObjectResetResult(
                    ActivityObjectResetResultKind.SkippedOptional,
                    command,
                    command.Source,
                    command.Reason,
                    "optional_reset_endpoint_missing");
            }

            bool hasSupportingEndpoint = false;
            for (int index = 0; index < endpoints.Length; index++)
            {
                var endpoint = endpoints[index];
                if (endpoint == null || !endpoint.Supports(command.ResetGroup))
                {
                    continue;
                }

                hasSupportingEndpoint = true;
                var result = endpoint.ApplyReset(command);
                if (!result.IsValid)
                {
                    return new ActivityObjectResetResult(
                        ActivityObjectResetResultKind.Failed,
                        command,
                        command.Source,
                        command.Reason,
                        "invalid_reset_result");
                }

                return result;
            }

            if (command.IsRequired)
            {
                return new ActivityObjectResetResult(
                    ActivityObjectResetResultKind.Failed,
                    command,
                    command.Source,
                    command.Reason,
                    hasSupportingEndpoint ? "required_reset_not_applied" : "required_reset_group_not_supported");
            }

            return new ActivityObjectResetResult(
                ActivityObjectResetResultKind.SkippedOptional,
                command,
                command.Source,
                command.Reason,
                hasSupportingEndpoint ? "optional_reset_not_applied" : "optional_reset_group_not_supported");
        }

        public static bool IsObjectResetResultForCurrentEntry(
            ActivityObjectResetResult result,
            SessionActivityIdentity identity,
            int entrySequence,
            SessionActivityIdentity currentIdentity)
        {
            var resultIdentity = result.Command.Identity;
            return result.IsValid &&
                   resultIdentity.IsValid &&
                   string.Equals(resultIdentity.PipelineId, currentIdentity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(resultIdentity.SessionId, currentIdentity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(resultIdentity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                   resultIdentity.ActivityOrdinal == identity.ActivityOrdinal &&
                   resultIdentity.EntrySequence == entrySequence &&
                   !string.IsNullOrWhiteSpace(result.Command.TargetId) &&
                   result.Command.ResetGroup != ActivityStateResetGroup.Unknown;
        }

        public static IActivityObjectSnapshotProvider[] ResolveObjectSnapshotProviders(GameObject targetObject)
        {
            IReadOnlyList<IActivityObjectLifecycleContribution> contributions = ResolveObjectLifecycleContributions(targetObject);
            if (contributions.Count == 0)
            {
                return Array.Empty<IActivityObjectSnapshotProvider>();
            }

            List<IActivityObjectSnapshotProvider> providers = new();
            HashSet<IActivityObjectSnapshotProvider> unique = new();
            for (int index = 0; index < contributions.Count; index++)
            {
                if (contributions[index] is not IActivityObjectSnapshotContribution snapshotContribution ||
                    snapshotContribution.SnapshotProvider == null)
                {
                    continue;
                }

                if (unique.Add(snapshotContribution.SnapshotProvider))
                {
                    providers.Add(snapshotContribution.SnapshotProvider);
                }
            }

            return providers.ToArray();
        }

        public static IActivityObjectSnapshotRestoreEndpoint[] ResolveObjectSnapshotRestoreEndpoints(GameObject targetObject)
        {
            IReadOnlyList<IActivityObjectLifecycleContribution> contributions = ResolveObjectLifecycleContributions(targetObject);
            if (contributions.Count == 0)
            {
                return Array.Empty<IActivityObjectSnapshotRestoreEndpoint>();
            }

            List<IActivityObjectSnapshotRestoreEndpoint> endpoints = new();
            HashSet<IActivityObjectSnapshotRestoreEndpoint> unique = new();
            for (int index = 0; index < contributions.Count; index++)
            {
                if (contributions[index] is not IActivityObjectSnapshotRestoreContribution restoreContribution ||
                    restoreContribution.RestoreEndpoint == null)
                {
                    continue;
                }

                if (unique.Add(restoreContribution.RestoreEndpoint))
                {
                    endpoints.Add(restoreContribution.RestoreEndpoint);
                }
            }

            return endpoints.ToArray();
        }

        private static IReadOnlyList<IActivityObjectLifecycleContribution> ResolveObjectLifecycleContributions(GameObject targetObject)
        {
            if (targetObject == null)
            {
                return Array.Empty<IActivityObjectLifecycleContribution>();
            }

            var contributor = targetObject.GetComponent<ActivityObjectContributor>();
            if (contributor == null || !contributor.IsValid)
            {
                return Array.Empty<IActivityObjectLifecycleContribution>();
            }

            bool includeChildren = contributor.IncludeChildrenForEndpointDiscovery;
            MonoBehaviour[] behaviours = includeChildren
                ? targetObject.GetComponentsInChildren<MonoBehaviour>(true)
                : targetObject.GetComponents<MonoBehaviour>();
            ActivityObjectLifecycleContributionContext context = new(
                default,
                contributor.TargetId,
                contributor.RoleId,
                contributor.ContributorKind,
                contributor.DefaultRequiredness,
                nameof(ActivityEntryObjectSetupStageUtility),
                "activity_object_lifecycle_contribution_lookup");
            List<IActivityObjectLifecycleContribution> contributions = new();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IActivityObjectLifecycleContributionProvider provider)
                {
                    provider.CollectActivityObjectLifecycleContributions(context, contributions);
                }
            }

            return contributions;
        }

        public static bool TryResolveSupportingSnapshotProvider(
            string targetId,
            IActivityObjectSnapshotProvider[] providers,
            out IActivityObjectSnapshotProvider resolvedProvider)
        {
            resolvedProvider = null;
            if (providers == null || providers.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < providers.Length; index++)
            {
                var provider = providers[index];
                if (provider == null || !provider.Supports(targetId))
                {
                    continue;
                }

                resolvedProvider = provider;
                return true;
            }

            return false;
        }

        public static bool TryResolveSupportingSnapshotRestoreEndpoint(
            string targetId,
            IActivityObjectSnapshotRestoreEndpoint[] endpoints,
            out IActivityObjectSnapshotRestoreEndpoint resolvedEndpoint)
        {
            resolvedEndpoint = null;
            if (endpoints == null || endpoints.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < endpoints.Length; index++)
            {
                var endpoint = endpoints[index];
                if (endpoint == null || !endpoint.Supports(targetId))
                {
                    continue;
                }

                resolvedEndpoint = endpoint;
                return true;
            }

            return false;
        }

        public static string ResolveSnapshotContractFailureReason(
            string providerFailureReason,
            string restoreFailureReason,
            bool providerFound,
            bool restoreFound)
        {
            if (!providerFound && !restoreFound)
            {
                return "snapshot_provider_and_restore_endpoint_missing";
            }

            if (!providerFound)
            {
                return "snapshot_provider_missing";
            }

            if (!restoreFound)
            {
                return "snapshot_restore_endpoint_missing";
            }

            if (!string.Equals(providerFailureReason, "resolved", StringComparison.Ordinal))
            {
                return string.IsNullOrWhiteSpace(providerFailureReason) ? "snapshot_provider_contract_invalid" : providerFailureReason;
            }

            if (!string.Equals(restoreFailureReason, "resolved", StringComparison.Ordinal))
            {
                return string.IsNullOrWhiteSpace(restoreFailureReason) ? "snapshot_restore_contract_invalid" : restoreFailureReason;
            }

            return "<none>";
        }

        public static ActivityObjectSnapshotRestoreResult ExecuteObjectSnapshotRestoreCommand(
            ActivityObjectSnapshotRestoreCommand command,
            IActivityObjectSnapshotRestoreEndpoint[] endpoints,
            ActivityObjectContributionReport report)
        {
            bool isRequired = report.Requiredness == ActivitySetupRequirementRequiredness.Required;
            if (endpoints == null || endpoints.Length == 0)
            {
                return new ActivityObjectSnapshotRestoreResult(
                    isRequired ? ActivityObjectSnapshotRestoreResultKind.Failed : ActivityObjectSnapshotRestoreResultKind.SkippedOptional,
                    command,
                    restoreVerified: false,
                    beforePositionX: 0f,
                    beforePositionY: 0f,
                    beforePositionZ: 0f,
                    afterPositionX: 0f,
                    afterPositionY: 0f,
                    afterPositionZ: 0f,
                    command.Source,
                    command.Reason,
                    isRequired ? "restore_endpoint_missing_required" : "target_has_no_restore_endpoint_optional");
            }

            for (int index = 0; index < endpoints.Length; index++)
            {
                var endpoint = endpoints[index];
                if (endpoint == null || !endpoint.Supports(command.TargetId))
                {
                    continue;
                }

                var result = endpoint.ApplyRestore(command);
                if (!result.IsValid)
                {
                    return new ActivityObjectSnapshotRestoreResult(
                        ActivityObjectSnapshotRestoreResultKind.Failed,
                        command,
                        restoreVerified: false,
                        beforePositionX: 0f,
                        beforePositionY: 0f,
                        beforePositionZ: 0f,
                        afterPositionX: 0f,
                        afterPositionY: 0f,
                        afterPositionZ: 0f,
                        command.Source,
                        command.Reason,
                        "restore_result_invalid_or_failed_required");
                }

                return result;
            }

            return new ActivityObjectSnapshotRestoreResult(
                isRequired ? ActivityObjectSnapshotRestoreResultKind.Failed : ActivityObjectSnapshotRestoreResultKind.SkippedOptional,
                command,
                restoreVerified: false,
                beforePositionX: 0f,
                beforePositionY: 0f,
                beforePositionZ: 0f,
                afterPositionX: 0f,
                afterPositionY: 0f,
                afterPositionZ: 0f,
                command.Source,
                command.Reason,
                isRequired ? "restore_endpoint_missing_required" : "target_has_no_restore_endpoint_optional");
        }

        public static bool IsObjectSnapshotRestoreResultForCurrentEntry(
            ActivityObjectSnapshotRestoreResult result,
            SessionActivityIdentity identity,
            int entrySequence,
            SessionActivityIdentity currentIdentity)
        {
            var command = result.Command;
            return result.IsValid &&
                   command.Identity.IsValid &&
                   string.Equals(command.Identity.PipelineId, currentIdentity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(command.Identity.SessionId, currentIdentity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(command.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                   command.Identity.ActivityOrdinal == identity.ActivityOrdinal &&
                   command.Identity.EntrySequence == entrySequence &&
                   !string.IsNullOrWhiteSpace(command.TargetId);
        }

        public static string FormatCapabilityKindsSummary(IReadOnlyList<ActivityCapabilityDescriptor> capabilities)
        {
            if (capabilities == null || capabilities.Count == 0)
            {
                return "<none>";
            }

            Dictionary<ActivityCapabilityKind, int> countsByKind = new();
            for (int index = 0; index < capabilities.Count; index++)
            {
                var kind = capabilities[index].CapabilityKind;
                countsByKind.TryGetValue(kind, out int count);
                countsByKind[kind] = count + 1;
            }

            List<ActivityCapabilityKind> kinds = new(countsByKind.Keys);
            kinds.Sort();
            List<string> segments = new(kinds.Count);
            for (int index = 0; index < kinds.Count; index++)
            {
                var kind = kinds[index];
                segments.Add($"{kind}:{countsByKind[kind]}");
            }

            return string.Join(",", segments);
        }

        public static string FormatValidationIssueCodes(IReadOnlyList<ActivityCapabilityInventoryValidationIssue> issues)
        {
            if (issues == null || issues.Count == 0)
            {
                return "<none>";
            }

            Dictionary<string, int> countsByCode = new(StringComparer.Ordinal);
            for (int index = 0; index < issues.Count; index++)
            {
                string code = string.IsNullOrWhiteSpace(issues[index].Code) ? "unknown" : issues[index].Code;
                countsByCode.TryGetValue(code, out int count);
                countsByCode[code] = count + 1;
            }

            List<string> codes = new(countsByCode.Keys);
            codes.Sort(StringComparer.Ordinal);
            List<string> segments = new(codes.Count);
            for (int index = 0; index < codes.Count; index++)
            {
                string code = codes[index];
                segments.Add($"{code}:{countsByCode[code]}");
            }

            return string.Join(",", segments);
        }

        public static string ToCoordinateSpaceToken(ActivityObjectSnapshotCoordinateSpace coordinateSpace)
        {
            return coordinateSpace == ActivityObjectSnapshotCoordinateSpace.WorldTransform
                ? "world_transform"
                : "unknown";
        }

        public static string JoinValues(HashSet<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return "<none>";
            }

            List<string> ordered = new(values);
            ordered.Sort(StringComparer.Ordinal);
            return string.Join(",", ordered);
        }

        private static bool TryGetPolicyValue(IReadOnlyList<ActivityCapabilityPolicyEntry> metadata, string key, out string value)
        {
            value = string.Empty;
            if (metadata == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            for (int index = 0; index < metadata.Count; index++)
            {
                var entry = metadata[index];
                if (string.Equals(entry.Key, key, StringComparison.Ordinal))
                {
                    value = entry.Value;
                    return !string.IsNullOrWhiteSpace(value);
                }
            }

            return false;
        }
    }
}
