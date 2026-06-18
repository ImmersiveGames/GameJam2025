using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using static _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages.ActivityEntryObjectSetupStageUtility;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntrySetupInventoryStage
    {
        public static void Execute(
            ActivityEntryObjectSetupCommand command,
            ActivitySetupInventoryBuilder builder,
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
                var failedIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActivitySetupInventoryBuildFailed);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivitySetupInventoryBuildFailed);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivitySetupInventoryBuildFailed,
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

            // Validação passiva removida: o builder já produz inventário válido ou falha.
            // Checks obrigatórios restantes pertencem aos stages consumidores.
        }
    }
}
