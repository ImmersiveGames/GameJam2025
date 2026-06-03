using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
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
            IActivityEntryIdentityRuntimeBridge identityBridge,
            IActivityEntryFactRuntimeBridge factBridge,
            IActivityEntryLogRuntimeBridge logBridge,
            IActivityEntryPreparationRuntimeBridge preparationBridge,
            IActivityEntryObjectSetupRuntimeBridge bridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity discoveryIdentity = identityBridge.BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            identityBridge.SetCurrentIdentity(discoveryIdentity, SessionActivityStage.ActivitySetupStarted);
            factBridge.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectContributorDiscoveryStarted,
                discoveryIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object contributor discovery started.");
            factBridge.EmitSnapshot(
                snapshots,
                "activity_object_contributor_discovery_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object contributor discovery started.");
            logBridge.LogEntryOwnerEvent(
                "ActivityEntryObjectContributorDiscoveryStarted",
                discoveryIdentity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' block='object_contributor_discovery'");

            if (!HasLoadedSetForCurrentEntry(loadedSet, definition, entrySequence, command.Identity) || !loadedSet.HasScenes)
            {
                preparationBridge.ClearCurrentActivityObjectContributorDiscoveryResult();
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorDiscoverySkippedNoContent,
                    discoveryIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor discovery skipped reason='no_content_loaded_set'.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "activity_object_contributor_discovery_skipped_no_content",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor discovery skipped reason='no_content_loaded_set'.");
                logBridge.LogEntryOwnerEvent(
                    "ActivityEntryObjectContributorDiscoverySkipped",
                    discoveryIdentity,
                    command.Source,
                    command.Reason,
                    "owner='ActivityEntryPipeline' block='object_contributor_discovery' reason='no_content_loaded_set'");
                return default;
            }

            try
            {
                List<ActivityObjectContributionReport> reports = new();
                for (int sceneIndex = 0; sceneIndex < loadedSet.Scenes.Count; sceneIndex++)
                {
                    ActivityContentLoadedSceneRecord record = loadedSet.Scenes[sceneIndex];
                    if (!record.IsValid)
                    {
                        continue;
                    }

                    Scene scene = SceneManager.GetSceneByName(record.SceneName);
                    if (!scene.IsValid() || !scene.isLoaded)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{definition.ActivityId}' content scene '{record.SceneName}' is not loaded for object contributor discovery.");
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
                        $"Activity '{definition.ActivityId}' produced invalid ActivityObjectContributorDiscoveryResult.");
                }

                bridge.SetCurrentActivityObjectContributorDiscoveryResult(result);

                for (int reportIndex = 0; reportIndex < reports.Count; reportIndex++)
                {
                    ActivityObjectContributionReport report = reports[reportIndex];
                    factBridge.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectContributorDiscovered,
                        discoveryIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' contributor discovered contentProfileId='{report.ContentProfileId}' sceneName='{report.SceneName}' targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetGroups='{FormatActivityStateResetGroups(report.SupportedResetGroups)}' releaseKinds='{FormatReleaseKinds(report.SupportedReleaseKinds)}'.");
                }

                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorDiscoveryCompleted,
                    discoveryIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor discovery completed discovered='{reports.Count}' contentProfileId='{loadedSet.ContentProfileId}'.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "activity_object_contributor_discovery_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor discovery completed discovered='{reports.Count}' contentProfileId='{loadedSet.ContentProfileId}'.");
                logBridge.LogEntryOwnerEvent(
                    "ActivityEntryObjectContributorDiscoveryCompleted",
                    discoveryIdentity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='object_contributor_discovery' discovered='{reports.Count}'");
                return result;
            }
            catch (Exception exception)
            {
                preparationBridge.ClearCurrentActivityObjectContributorDiscoveryResult();
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorDiscoveryFailed,
                    discoveryIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor discovery failed error='{exception.Message}'.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "activity_object_contributor_discovery_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor discovery failed error='{exception.Message}'.");
                logBridge.LogEntryOwnerEvent(
                    "ActivityEntryObjectContributorDiscoveryFailed",
                    discoveryIdentity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='object_contributor_discovery' error='{exception.Message}'");
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
                    ActivityObjectContributor contributor = contributors[contributorIndex];
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

        private static bool HasLoadedSetForCurrentEntry(
            ActivityContentLoadedSet loadedSet,
            SessionActivityDefinition definition,
            int entrySequence,
            SessionActivityIdentity identity)
        {
            return loadedSet.IsValid &&
                   loadedSet.Identity.Stage == SessionActivityStage.ActivityContentLoadedSetReady &&
                   string.Equals(loadedSet.Identity.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(loadedSet.Identity.SessionId, identity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(loadedSet.Identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   loadedSet.Identity.ActivityOrdinal == definition.ActivityOrdinal &&
                   loadedSet.Identity.EntrySequence == entrySequence;
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
            ActivityContentLoadedSet loadedSet,
            ActivitySetupInventoryBuilder builder,
            ActivitySetupInventoryValidator validator,
            IActivityEntryRuntimeBridge endpoint,
            IActivityEntryObjectSetupRuntimeBridge bridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity setupIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            SessionActivityIdentity buildStartedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActivitySetupInventoryBuildStarted, entrySequence);
            endpoint.SetCurrentIdentity(buildStartedIdentity, SessionActivityStage.ActivitySetupInventoryBuildStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivitySetupInventoryBuildStarted,
                buildStartedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity setup inventory build started.");
            endpoint.EmitSnapshot(
                snapshots,
                "activity_setup_inventory_build_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity setup inventory build started.");

            ActivitySetupInventoryBuildContext buildContext = new(
                definition,
                setupIdentity,
                loadedSet,
                command.Source,
                command.Reason);

            ActivitySetupInventoryBuildResult buildResult = builder.Build(buildContext);
            if (buildResult.IsFailed || !buildResult.IsValid)
            {
                SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActivitySetupInventoryValidationFailed, entrySequence);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivitySetupInventoryValidationFailed);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivitySetupInventoryValidationFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory build failed. message='{buildResult.Message}'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_setup_inventory_build_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory build failed. message='{buildResult.Message}'.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActivityEntryPipeline][ActivitySetupInventory] Build failed activityId='{definition.ActivityId}' entrySequence='{entrySequence}' message='{buildResult.Message}'.");
            }

            bridge.SetCurrentActivitySetupInventory(buildResult.Inventory);

            if (buildResult.IsSkipped)
            {
                SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActivitySetupInventorySkippedNoRequirements, entrySequence);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivitySetupInventorySkippedNoRequirements);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivitySetupInventorySkippedNoRequirements,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory skipped because no requirements were declared. inventoryId='{buildResult.Inventory.InventoryId}'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_setup_inventory_skipped_no_requirements",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory skipped because no requirements were declared.");
            }
            else
            {
                SessionActivityIdentity builtIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActivitySetupInventoryBuilt, entrySequence);
                endpoint.SetCurrentIdentity(builtIdentity, SessionActivityStage.ActivitySetupInventoryBuilt);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivitySetupInventoryBuilt,
                    builtIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory built inventoryId='{buildResult.Inventory.InventoryId}' totalRequirements='{buildResult.Inventory.TotalRequirementCount}'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_setup_inventory_built",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory built totalRequirements='{buildResult.Inventory.TotalRequirementCount}'.");
            }

            ActivitySetupInventoryValidationResult validationResult = validator.Validate(buildResult.Inventory, command.Source, command.Reason);
            if (validationResult.IsFailed || !validationResult.IsValid)
            {
                SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActivitySetupInventoryValidationFailed, entrySequence);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivitySetupInventoryValidationFailed);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivitySetupInventoryValidationFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory validation failed errors='{validationResult.Errors.Count}' message='{validationResult.Message}'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_setup_inventory_validation_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory validation failed errors='{validationResult.Errors.Count}'.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActivityEntryPipeline][ActivitySetupInventory] Validation failed activityId='{definition.ActivityId}' entrySequence='{entrySequence}' errors='{string.Join(" | ", validationResult.Errors)}'.");
            }

            SessionActivityIdentity validatedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActivitySetupInventoryValidated, entrySequence);
            endpoint.SetCurrentIdentity(validatedIdentity, SessionActivityStage.ActivitySetupInventoryValidated);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivitySetupInventoryValidated,
                validatedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity setup inventory validated inventoryId='{validationResult.Inventory.InventoryId}' totalRequirements='{validationResult.Inventory.TotalRequirementCount}' skipped='{validationResult.SkippedRequirementIds.Count}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "activity_setup_inventory_validated",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity setup inventory validated totalRequirements='{validationResult.Inventory.TotalRequirementCount}' skipped='{validationResult.SkippedRequirementIds.Count}'.");
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

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity validationIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            endpoint.SetCurrentIdentity(validationIdentity, SessionActivityStage.ActivitySetupStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotContractValidationStarted,
                validationIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot contract validation started.");

            if (!IsDiscoveryResultForCurrentEntry(discoveryResult, definition, entrySequence, validationIdentity) ||
                discoveryResult.Reports.Count == 0)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotContractValidationCompleted,
                    validationIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot contract validation completed validationStarted='true' validatedCount='0' skippedCount='0' failedCount='0' targetIds='<none>' providerPaths='<none>' restoreEndpointPaths='<none>' targetTransformPaths='<none>' mismatchReason='<none>'.");
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
                ActivityObjectContributionReport report = discoveryResult.Reports[reportIndex];
                if (!IsReportForCurrentEntry(report, definition, entrySequence, validationIdentity))
                {
                    continue;
                }

                targetIds.Add(report.TargetId);
                bool required = report.Requiredness == ActivitySetupRequirementRequiredness.Required;
                GameObject targetObject = ResolveContributorObjectOrFail(loadedSet, definition, report);
                IActivityObjectSnapshotProvider[] providers = ResolveObjectSnapshotProviders(targetObject);
                IActivityObjectSnapshotRestoreEndpoint[] restoreEndpoints = ResolveObjectSnapshotRestoreEndpoints(targetObject);

                bool providerFound = TryResolveSupportingSnapshotProvider(report.TargetId, providers, out IActivityObjectSnapshotProvider provider);
                bool restoreFound = TryResolveSupportingSnapshotRestoreEndpoint(report.TargetId, restoreEndpoints, out IActivityObjectSnapshotRestoreEndpoint restoreEndpoint);

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
                        $"'{definition.ActivityId}' activity object snapshot contract validated targetId='{report.TargetId}' requiredness='{report.Requiredness}' providerPath='{providerPath}' restoreEndpointPath='{restorePath}' targetTransformPath='{providerTargetTransformPath}'.");
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
                        $"'{definition.ActivityId}' activity object snapshot contract skipped optional targetId='{report.TargetId}' requiredness='{report.Requiredness}' reason='snapshot_capability_not_declared_optional' providerPath='{providerPath}' restoreEndpointPath='{restorePath}' targetTransformPath='<none>'.");
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
                    $"'{definition.ActivityId}' activity object snapshot contract failed targetId='{report.TargetId}' requiredness='{report.Requiredness}' reason='{failureReason}' providerPath='{providerPath}' restoreEndpointPath='{restorePath}' providerTargetTransformPath='{providerTargetTransformPath}' restoreTargetTransformPath='{restoreTargetTransformPath}'.");
                throw new InvalidOperationException(
                    $"snapshot_contract_validation_failed: activityId='{definition.ActivityId}' targetId='{report.TargetId}' reason='{failureReason}' providerPath='{providerPath}' restoreEndpointPath='{restorePath}' providerTargetTransformPath='{providerTargetTransformPath}' restoreTargetTransformPath='{restoreTargetTransformPath}'.");
            }

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotContractValidationCompleted,
                validationIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot contract validation completed validationStarted='true' validatedCount='{validatedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' targetIds='{JoinValues(targetIds)}' providerPaths='{JoinValues(providerPaths)}' restoreEndpointPaths='{JoinValues(restoreEndpointPaths)}' targetTransformPaths='{JoinValues(targetTransformPaths)}' mismatchReason='{mismatchReason}'.");
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
            IActivityEntryObjectSetupRuntimeBridge bridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity previewIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            endpoint.SetCurrentIdentity(previewIdentity, SessionActivityStage.ActivitySetupStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityCapabilityInventoryPreviewStarted,
                previewIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity capability inventory preview started scannerId='{coordinator.ActivityObjectScannerId}'.");
            EmitEntryCapabilityInventoryLog(
                SessionActivityFactKind.ActivityCapabilityInventoryPreviewStarted,
                previewIdentity,
                $"'{definition.ActivityId}' activity capability inventory preview started scannerId='{coordinator.ActivityObjectScannerId}'.");

            bool hasDiscoveryForCurrentEntry = IsDiscoveryResultForCurrentEntry(discoveryResult, definition, entrySequence, previewIdentity);
            bool hasActorTargets = actorTargets != null && actorTargets.Count > 0;
            if (!hasDiscoveryForCurrentEntry && !hasActorTargets)
            {
                bridge.ClearCurrentActivityCapabilityInventoryPreview();
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityCapabilityInventoryPreviewSkippedNoDiscovery,
                    previewIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity capability inventory preview skipped reason='no_capability_sources' entrySequence='{entrySequence}' scannerId='{coordinator.ActivityObjectScannerId}'.");
                EmitEntryCapabilityInventoryLog(
                    SessionActivityFactKind.ActivityCapabilityInventoryPreviewSkippedNoDiscovery,
                    previewIdentity,
                    $"'{definition.ActivityId}' activity capability inventory preview skipped reason='no_capability_sources' entrySequence='{entrySequence}' scannerId='{coordinator.ActivityObjectScannerId}'.");
                return default;
            }

            ActivityCapabilityInventoryBuildResult buildResult = coordinator.BuildForEntry(
                previewIdentity,
                hasDiscoveryForCurrentEntry ? discoveryResult : default,
                actorTargets,
                command.Source,
                command.Reason);
            ActivityCapabilityInventory inventory = buildResult.Inventory;
            ActivityCapabilityInventoryValidationResult validationResult = buildResult.Validation;
            string capabilityKindsSummary = FormatCapabilityKindsSummary(inventory.Capabilities);
            bridge.SetCurrentActivityCapabilityInventoryPreview(inventory, validationResult);
            string validationIssueCodes = FormatValidationIssueCodes(validationResult.Issues);

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityCapabilityInventoryValidationStarted,
                previewIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity capability inventory validation started entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' scannerId='{coordinator.ActivityObjectScannerId}'.");
            EmitEntryCapabilityInventoryLog(
                SessionActivityFactKind.ActivityCapabilityInventoryValidationStarted,
                previewIdentity,
                $"'{definition.ActivityId}' activity capability inventory validation started entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' scannerId='{coordinator.ActivityObjectScannerId}'.");

            SessionActivityFactKind validationOutcomeKind = validationResult.Status switch
            {
                ActivityCapabilityInventoryValidationStatus.Passed => SessionActivityFactKind.ActivityCapabilityInventoryValidationPassed,
                ActivityCapabilityInventoryValidationStatus.PassedWithWarnings => SessionActivityFactKind.ActivityCapabilityInventoryValidationWarning,
                ActivityCapabilityInventoryValidationStatus.FailedPassive => SessionActivityFactKind.ActivityCapabilityInventoryValidationFailedPassive,
                _ => SessionActivityFactKind.ActivityCapabilityInventoryValidationWarning,
            };

            string validationOutcomeMessage =
                $"'{definition.ActivityId}' activity capability inventory validation outcome status='{validationResult.Status}' entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{coordinator.ActivityObjectScannerId}'.";
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
                $"'{definition.ActivityId}' activity capability inventory validation completed status='{validationResult.Status}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{coordinator.ActivityObjectScannerId}'.");
            EmitEntryCapabilityInventoryLog(
                SessionActivityFactKind.ActivityCapabilityInventoryValidationCompleted,
                previewIdentity,
                $"'{definition.ActivityId}' activity capability inventory validation completed status='{validationResult.Status}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{coordinator.ActivityObjectScannerId}'.");

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityCapabilityInventoryPreviewObserved,
                previewIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity capability inventory preview observed entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' capabilityKinds='{capabilityKindsSummary}' unresolvedReports='{buildResult.UnresolvedReportCount}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{coordinator.ActivityObjectScannerId}'.");
            EmitEntryCapabilityInventoryLog(
                SessionActivityFactKind.ActivityCapabilityInventoryPreviewObserved,
                previewIdentity,
                $"'{definition.ActivityId}' activity capability inventory preview observed entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' capabilityKinds='{capabilityKindsSummary}' unresolvedReports='{buildResult.UnresolvedReportCount}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{coordinator.ActivityObjectScannerId}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "activity_capability_inventory_preview_observed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity capability inventory preview observed entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' capabilityKinds='{capabilityKindsSummary}' unresolvedReports='{buildResult.UnresolvedReportCount}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{coordinator.ActivityObjectScannerId}'.");

            return buildResult;
        }

        private static void EmitEntryCapabilityInventoryLog(
            SessionActivityFactKind kind,
            SessionActivityIdentity identity,
            string message)
        {
            Debug.Log(
                $"[OBS][ActivityEntryPipeline][CapabilityInventoryPreview] fact='{kind}' stage='{identity.Stage}' entrySequence='{identity.EntrySequence}' activity='{identity.ActivityId}' owner='ActivityEntryPipeline' block='capability_inventory_preview' message=\"{message}\"");
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

            SessionActivityDefinition definition = command.Definition;
            SessionActivityIdentity resetIdentity = command.Identity;
            int entrySequence = resetIdentity.EntrySequence;
            endpoint.SetCurrentIdentity(resetIdentity, SessionActivityStage.ActivitySetupStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ObjectResetStarted,
                resetIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' object reset started.");
            endpoint.EmitSnapshot(
                snapshots,
                "object_reset_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' object reset started.");

            if (!IsDiscoveryResultForCurrentEntry(discoveryResult, definition, entrySequence, resetIdentity))
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ObjectResetCompleted,
                    resetIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.NoCommands}' completionReason='no_contributors_current_entry'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "object_reset_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.NoCommands}' completionReason='no_contributors_current_entry'.");
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
                    $"'{definition.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.NoCommands}' completionReason='no_reports_for_current_entry'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "object_reset_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.NoCommands}' completionReason='no_reports_for_current_entry'.");
                return;
            }

            int commandCount = 0;
            int appliedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;
            int noSupportedGroupsCount = 0;
            int reportEvaluatedCount = 0;
            bool hasRequiredContributor = HasRequiredResetContributor(discoveryResult, definition, entrySequence);
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
                        $"'{definition.ActivityId}' object reset failed reason='required_reset_inventory_missing_or_invalid' entrySequence='{entrySequence}' inventoryValid='{inventory.IsValid.ToString().ToLowerInvariant()}' validationValid='{validation.IsValid.ToString().ToLowerInvariant()}'.");
                    throw new InvalidOperationException(
                        $"required_reset_inventory_missing_or_invalid: activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                }

                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ObjectResetCompleted,
                    resetIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.InventoryInvalidOrStale}' completionReason='inventory_stale_or_invalid'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "object_reset_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.InventoryInvalidOrStale}' completionReason='inventory_stale_or_invalid'.");
                return;
            }

            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                ActivityObjectContributionReport report = discoveryResult.Reports[reportIndex];
                if (!IsReportForCurrentEntry(report, definition, entrySequence, resetIdentity))
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
                        $"'{definition.ActivityId}' object reset skipped targetId='{report.TargetId}' reason='no_supported_reset_groups'.");
                    continue;
                }

                IActivityObjectResetEndpoint[] endpoints = ResolveObjectResetEndpointsFromInventory(inventory, report);
                for (int groupIndex = 0; groupIndex < report.SupportedResetGroups.Count; groupIndex++)
                {
                    ActivityStateResetGroup resetGroup = report.SupportedResetGroups[groupIndex];
                    if (resetGroup == ActivityStateResetGroup.Unknown)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{definition.ActivityId}' reset group cannot be Unknown targetId='{report.TargetId}'.");
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
                            $"Activity '{definition.ActivityId}' produced invalid object reset command targetId='{report.TargetId}' resetGroup='{resetGroup}'.");
                    }

                    commandCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectResetCommandIssued,
                        resetIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' object reset command issued targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetGroup='{resetGroup}'.");

                    ActivityObjectResetResult result = ExecuteObjectResetCommand(resetCommand, endpoints);
                    if (!IsObjectResetResultForCurrentEntry(result, definition, entrySequence, resetIdentity))
                    {
                        failedCount += 1;
                        endpoint.EmitFact(
                            facts,
                            SessionActivityFactKind.ObjectResetFailed,
                            resetIdentity,
                            command.Source,
                            command.Reason,
                            $"'{definition.ActivityId}' object reset failed targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='stale_or_foreign_reset_result'.");
                        throw new InvalidOperationException(
                            $"stale_or_foreign_reset_result: activityId='{definition.ActivityId}' targetId='{report.TargetId}' resetGroup='{resetGroup}'.");
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
                            $"'{definition.ActivityId}' object reset applied targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetGroup='{resetGroup}'.");
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
                            $"'{definition.ActivityId}' object reset skipped optional targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='{result.Message}'.");
                        continue;
                    }

                    failedCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectResetFailed,
                        resetIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' object reset failed targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='{result.Message}'.");
                    throw new InvalidOperationException(
                        $"object_reset_failed: activityId='{definition.ActivityId}' targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='{result.Message}'.");
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
                $"'{definition.ActivityId}' object reset completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' completionKind='{finalCompletionKind}' completionReason='{finalCompletionReason}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "object_reset_completed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' object reset completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' completionKind='{finalCompletionKind}' completionReason='{finalCompletionReason}'.");
        }
    }

    internal static class ActivityEntryObjectSnapshotRestoreStage
    {
        private const string RouteActivitySnapshotSchemaId = "progression.route_activity.object_snapshot.v1";

        public static void Execute(
            ActivityEntryObjectSetupCommand command,
            ActivityObjectContributorDiscoveryResult discoveryResult,
            ActivityCapabilityInventory inventory,
            ActivityCapabilityInventoryValidationResult validation,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity restoreIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            endpoint.SetCurrentIdentity(restoreIdentity, SessionActivityStage.ActivitySetupStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotRestoreStarted,
                restoreIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot restore started.");

            if (!TryResolveRouteLoadedSnapshotPayload(endpoint.SessionId, out LoadedSessionActivitySnapshotPayload loadedPayload, out string payloadFailureReason))
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoPayload,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot restore skipped reason='no_loaded_payload' failureReason='{payloadFailureReason}'.");
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot restore completed payloadAvailable='false' payloadObjectCount='0' matchedTargetCount='0' restoredCount='0' restoreFailed='false'.");
                return;
            }

            if (!IsLoadedSnapshotPayloadForCurrentActivity(loadedPayload, endpoint.SessionId, definition))
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot restore failed reason='payload_foreign_or_stale' payloadSessionStateId='{loadedPayload.SessionStateId}' payloadActivityId='{loadedPayload.ActivityId}' payloadSourceEntrySequence='{loadedPayload.SourceEntrySequence}'.");
                throw new InvalidOperationException(
                    $"payload_foreign_or_stale: activityId='{definition.ActivityId}' entrySequence='{entrySequence}' payloadSessionStateId='{loadedPayload.SessionStateId}' payloadActivityId='{loadedPayload.ActivityId}' payloadSourceEntrySequence='{loadedPayload.SourceEntrySequence}'.");
            }

            if (!IsDiscoveryResultForCurrentEntry(discoveryResult, definition, entrySequence, restoreIdentity) ||
                discoveryResult.Reports.Count == 0)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoMatchingTarget,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot restore skipped reason='payload_has_no_matching_target_for_entry' payloadObjectCount='{loadedPayload.Objects.Count}'.");
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot restore completed payloadAvailable='true' payloadObjectCount='{loadedPayload.Objects.Count}' matchedTargetCount='0' restoredCount='0' restoreFailed='false'.");
                return;
            }

            Dictionary<string, LoadedSessionActivitySnapshotPayloadObject> payloadByTargetId = BuildLoadedSnapshotPayloadByTargetId(loadedPayload.Objects);
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
                    $"'{definition.ActivityId}' activity object snapshot restore failed reason='restore_inventory_missing_or_invalid' entrySequence='{entrySequence}' inventoryValid='{inventory.IsValid.ToString().ToLowerInvariant()}' validationValid='{validation.IsValid.ToString().ToLowerInvariant()}'.");
                throw new InvalidOperationException(
                    $"restore_inventory_missing_or_invalid: activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                ActivityObjectContributionReport report = discoveryResult.Reports[reportIndex];
                if (!IsReportForCurrentEntry(report, definition, entrySequence, restoreIdentity))
                {
                    continue;
                }

                if (!payloadByTargetId.TryGetValue(report.TargetId, out LoadedSessionActivitySnapshotPayloadObject payloadObject) || !payloadObject.IsValid)
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

                ActivityObjectSnapshotRestoreResult result = ExecuteObjectSnapshotRestoreCommand(restoreCommand, endpoints, report);
                if (!IsObjectSnapshotRestoreResultForCurrentEntry(result, definition, entrySequence, restoreIdentity, endpoint))
                {
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                        restoreIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot restore failed reason='restore_result_invalid_or_failed_required' targetId='{report.TargetId}' detail='{result.Detail}'.");
                    throw new InvalidOperationException(
                        $"restore_result_invalid_or_failed_required: activityId='{definition.ActivityId}' targetId='{report.TargetId}' detail='{result.Detail}'.");
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
                        $"'{definition.ActivityId}' activity object snapshot restore applied targetId='{report.TargetId}' coordinateSpace='{ToCoordinateSpaceToken(restoreCommand.CoordinateSpace)}' payloadPosition='({payloadObject.PositionX:0.###},{payloadObject.PositionY:0.###},{payloadObject.PositionZ:0.###})' beforePosition='({result.BeforePositionX:0.###},{result.BeforePositionY:0.###},{result.BeforePositionZ:0.###})' afterPosition='({result.AfterPositionX:0.###},{result.AfterPositionY:0.###},{result.AfterPositionZ:0.###})' restoreVerified='{result.RestoreVerified.ToString().ToLowerInvariant()}' hasTransformPayload='true' detail='{result.Detail}'.");
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
                        $"'{definition.ActivityId}' activity object snapshot restore skipped optional targetId='{report.TargetId}' reason='{result.Detail}'.");
                    continue;
                }

                restoreFailed = true;
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot restore failed reason='restore_endpoint_missing_required' targetId='{report.TargetId}' detail='{result.Detail}'.");
                throw new InvalidOperationException(
                    $"restore_endpoint_missing_required: activityId='{definition.ActivityId}' targetId='{report.TargetId}' detail='{result.Detail}'.");
            }

            if (matchedTargetCount == 0)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoMatchingTarget,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot restore skipped reason='payload_has_no_matching_target_for_entry' payloadObjectCount='{loadedPayload.Objects.Count}'.");
            }

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted,
                restoreIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot restore completed payloadAvailable='true' payloadObjectCount='{loadedPayload.Objects.Count}' matchedTargetCount='{matchedTargetCount}' restoredCount='{restoredCount}' targetIds='{JoinValues(matchedTargetIds)}' appliedTargetIds='{JoinValues(matchedTargetIds)}' failedTargetIds='<none>' coordinateSpace='world_transform' restoreVerified='{(!restoreFailed && restoredCount == matchedTargetCount).ToString().ToLowerInvariant()}' restoreFailed='{restoreFailed.ToString().ToLowerInvariant()}'.");
        }

        private static bool TryResolveRouteLoadedSnapshotPayload(
            string sessionId,
            out LoadedSessionActivitySnapshotPayload loadedPayload,
            out string failureReason)
        {
            loadedPayload = default;
            if (!DependencyManager.Provider.TryGetGlobal<IRouteActivityLoadedSnapshotPayloadProvider>(out var payloadProvider) ||
                payloadProvider == null)
            {
                failureReason = "no_loaded_payload_provider";
                return false;
            }

            bool resolved = payloadProvider.TryGetPendingLoadedSnapshotPayload(sessionId, out loadedPayload, out failureReason);
            if (!resolved || !loadedPayload.IsValid)
            {
                loadedPayload = default;
                return false;
            }

            failureReason = "resolved";
            return true;
        }

        private static bool IsLoadedSnapshotPayloadForCurrentActivity(
            LoadedSessionActivitySnapshotPayload loadedPayload,
            string sessionId,
            SessionActivityDefinition definition)
        {
            return loadedPayload.IsValid &&
                   string.Equals(loadedPayload.SchemaId, RouteActivitySnapshotSchemaId, StringComparison.Ordinal) &&
                   string.Equals(loadedPayload.SessionStateId, sessionId, StringComparison.Ordinal) &&
                   string.Equals(loadedPayload.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   loadedPayload.SourceEntrySequence > 0;
        }
    }

    internal static class ActivityEntryObjectSetupStageUtility
    {
        public static bool IsDiscoveryResultForCurrentEntry(
            ActivityObjectContributorDiscoveryResult result,
            SessionActivityDefinition definition,
            int entrySequence,
            SessionActivityIdentity identity)
        {
            return result.IsValid &&
                   result.Identity.IsValid &&
                   string.Equals(result.Identity.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(result.Identity.SessionId, identity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(result.Identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   result.Identity.ActivityOrdinal == definition.ActivityOrdinal &&
                   result.Identity.EntrySequence == entrySequence;
        }

        public static bool IsReportForCurrentEntry(
            ActivityObjectContributionReport report,
            SessionActivityDefinition definition,
            int entrySequence,
            SessionActivityIdentity identity)
        {
            return report.IsValid &&
                   report.Identity.IsValid &&
                   string.Equals(report.Identity.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(report.Identity.SessionId, identity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(report.Identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   report.Identity.ActivityOrdinal == definition.ActivityOrdinal &&
                   report.Identity.EntrySequence == entrySequence;
        }

        public static bool HasRequiredResetContributor(
            ActivityObjectContributorDiscoveryResult discoveryResult,
            SessionActivityDefinition definition,
            int entrySequence)
        {
            if (!discoveryResult.IsValid || discoveryResult.Reports == null)
            {
                return false;
            }

            for (int index = 0; index < discoveryResult.Reports.Count; index++)
            {
                ActivityObjectContributionReport report = discoveryResult.Reports[index];
                if (!report.IsValid)
                {
                    continue;
                }

                if (!string.Equals(report.Identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) ||
                    report.Identity.ActivityOrdinal != definition.ActivityOrdinal ||
                    report.Identity.EntrySequence != entrySequence)
                {
                    continue;
                }

                if (report.Requiredness == ActivitySetupRequirementRequiredness.Required &&
                    report.SupportedResetGroups != null &&
                    report.SupportedResetGroups.Count > 0)
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
                ActivityCapabilityDescriptor capability = inventory.Capabilities[index];
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
                ActivityCapabilityDescriptor capability = inventory.Capabilities[index];
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
                IActivityObjectResetEndpoint endpoint = endpoints[index];
                if (endpoint == null || !endpoint.Supports(command.ResetGroup))
                {
                    continue;
                }

                hasSupportingEndpoint = true;
                ActivityObjectResetResult result = endpoint.ApplyReset(command);
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
            SessionActivityDefinition definition,
            int entrySequence,
            SessionActivityIdentity identity)
        {
            SessionActivityIdentity resultIdentity = result.Command.Identity;
            return result.IsValid &&
                   resultIdentity.IsValid &&
                   string.Equals(resultIdentity.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(resultIdentity.SessionId, identity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(resultIdentity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   resultIdentity.ActivityOrdinal == definition.ActivityOrdinal &&
                   resultIdentity.EntrySequence == entrySequence &&
                   !string.IsNullOrWhiteSpace(result.Command.TargetId) &&
                   result.Command.ResetGroup != ActivityStateResetGroup.Unknown;
        }

        public static GameObject ResolveContributorObjectOrFail(
            ActivityContentLoadedSet loadedSet,
            SessionActivityDefinition definition,
            ActivityObjectContributionReport report)
        {
            for (int sceneIndex = 0; sceneIndex < loadedSet.Scenes.Count; sceneIndex++)
            {
                ActivityContentLoadedSceneRecord sceneRecord = loadedSet.Scenes[sceneIndex];
                if (!sceneRecord.IsValid || !string.Equals(sceneRecord.SceneName, report.SceneName, StringComparison.Ordinal))
                {
                    continue;
                }

                Scene scene = SceneManager.GetSceneByName(sceneRecord.SceneName);
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
                        ActivityObjectContributor contributor = contributors[contributorIndex];
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
                $"Activity '{definition.ActivityId}' could not resolve contributor object for targetId='{report.TargetId}' sceneName='{report.SceneName}'.");
        }

        public static IActivityObjectSnapshotProvider[] ResolveObjectSnapshotProviders(GameObject targetObject)
        {
            if (targetObject == null)
            {
                return Array.Empty<IActivityObjectSnapshotProvider>();
            }

            List<IActivityObjectSnapshotProvider> providers = new();
            ActivityObjectContributor contributor = targetObject.GetComponent<ActivityObjectContributor>();
            bool includeChildren = contributor != null && contributor.IncludeChildrenForEndpointDiscovery;
            MonoBehaviour[] behaviours = includeChildren
                ? targetObject.GetComponentsInChildren<MonoBehaviour>(true)
                : targetObject.GetComponents<MonoBehaviour>();

            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IActivityObjectSnapshotProvider provider)
                {
                    providers.Add(provider);
                }
            }

            return providers.ToArray();
        }

        public static IActivityObjectSnapshotRestoreEndpoint[] ResolveObjectSnapshotRestoreEndpoints(GameObject targetObject)
        {
            if (targetObject == null)
            {
                return Array.Empty<IActivityObjectSnapshotRestoreEndpoint>();
            }

            List<IActivityObjectSnapshotRestoreEndpoint> endpoints = new();
            ActivityObjectContributor contributor = targetObject.GetComponent<ActivityObjectContributor>();
            bool includeChildren = contributor != null && contributor.IncludeChildrenForEndpointDiscovery;
            MonoBehaviour[] behaviours = includeChildren
                ? targetObject.GetComponentsInChildren<MonoBehaviour>(true)
                : targetObject.GetComponents<MonoBehaviour>();

            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IActivityObjectSnapshotRestoreEndpoint endpoint)
                {
                    endpoints.Add(endpoint);
                }
            }

            return endpoints.ToArray();
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
                IActivityObjectSnapshotProvider provider = providers[index];
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
                IActivityObjectSnapshotRestoreEndpoint endpoint = endpoints[index];
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
                IActivityObjectSnapshotRestoreEndpoint endpoint = endpoints[index];
                if (endpoint == null || !endpoint.Supports(command.TargetId))
                {
                    continue;
                }

                ActivityObjectSnapshotRestoreResult result = endpoint.ApplyRestore(command);
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
            SessionActivityDefinition definition,
            int entrySequence,
            SessionActivityIdentity identity,
            IActivityEntryRuntimeBridge endpoint)
        {
            ActivityObjectSnapshotRestoreCommand command = result.Command;
            return result.IsValid &&
                   command.Identity.IsValid &&
                   string.Equals(command.Identity.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(command.Identity.SessionId, identity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(command.Identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   command.Identity.ActivityOrdinal == definition.ActivityOrdinal &&
                   command.Identity.EntrySequence == entrySequence &&
                   !string.IsNullOrWhiteSpace(command.TargetId);
        }

        public static Dictionary<string, LoadedSessionActivitySnapshotPayloadObject> BuildLoadedSnapshotPayloadByTargetId(IReadOnlyList<LoadedSessionActivitySnapshotPayloadObject> objects)
        {
            Dictionary<string, LoadedSessionActivitySnapshotPayloadObject> byTargetId = new(StringComparer.Ordinal);
            if (objects == null)
            {
                return byTargetId;
            }

            for (int index = 0; index < objects.Count; index++)
            {
                LoadedSessionActivitySnapshotPayloadObject current = objects[index];
                if (!current.IsValid || string.IsNullOrWhiteSpace(current.TargetId))
                {
                    continue;
                }

                byTargetId[current.TargetId] = current;
            }

            return byTargetId;
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
                ActivityCapabilityKind kind = capabilities[index].CapabilityKind;
                countsByKind.TryGetValue(kind, out int count);
                countsByKind[kind] = count + 1;
            }

            List<ActivityCapabilityKind> kinds = new(countsByKind.Keys);
            kinds.Sort();
            List<string> segments = new(kinds.Count);
            for (int index = 0; index < kinds.Count; index++)
            {
                ActivityCapabilityKind kind = kinds[index];
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
                ActivityCapabilityPolicyEntry entry = metadata[index];
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
