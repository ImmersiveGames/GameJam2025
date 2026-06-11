using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Actors.Semantic.Participation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Policies;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using UnityEngine;
using PlayerActivityParticipantBinding = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipantBinding;
using PlayerActivityParticipationContext = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipationContext;
using PlayerSessionParticipantId = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.SessionParticipantId;
using PlayerSessionParticipationContext = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.SessionParticipationContext;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal interface IActivityEntryParticipantBindingRuntimeBridge
    {
        IReadOnlyList<SessionActivityActorMaterializationPlanEntry> GetActorMaterializationPlanEntries();
        bool TryBuildActivityParticipantBinding(
            ParticipantRequirement requirement,
            string source,
            string reason,
            out PlayerActivityParticipantBinding binding,
            out string resolutionReason);
        void LogActivityParticipationBindingSkipped(
            string activityId,
            SessionActivityIdentity identity,
            string requirementId,
            PlayerSessionParticipantId participantId,
            string skipReason,
            string source,
            string reason);
        bool TryGetSessionScopedPlayerActorForParticipant(
            SessionActivityIdentity identity,
            PlayerActivityParticipantBinding participant,
            out PlayerActorRuntimeHandle handle);
        IReadOnlyList<PlayerActorMaterializationRecord> ExecutePlayerActorMaterialization(
            PlayerActorMaterializationCommand command,
            SessionActivityIdentity identity);
        IReadOnlyList<PlayerActorParticipationEnterRecord> ExecutePlayerActorParticipationEnter(
            PlayerActorParticipationEnterCommand command,
            SessionActivityIdentity identity);
        IReadOnlyList<ActorResetResult> ExecuteActorReset(ActorResetCommand command, SessionActivityIdentity identity);
    }

    internal static class ActivityEntryParticipantBindingStage
    {
        public static ActivityEntryParticipantBindingResult Execute(
            ActivityEntryParticipantBindingCommand command,
            IActivityEntryRuntimeBridge endpoint,
            ActivitySetupInventory inventory,
            IActivityEntryParticipantBindingRuntimeBridge bridge,
            ActivityPlayerActorRegistry playerActorRegistry,
            IActivityEntryPlacementMarkerLookup placementMarkerLookup,
            ActivityParticipationRuntimeState activityParticipationRuntimeState,
            ActivityActorExitRuntimeState activityActorExitRuntimeState,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryParticipantBindingCommand is invalid.");
            }

            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (bridge == null)
            {
                throw new ArgumentNullException(nameof(bridge));
            }

            if (placementMarkerLookup == null)
            {
                throw new ArgumentNullException(nameof(placementMarkerLookup));
            }

            if (activityParticipationRuntimeState == null)
            {
                throw new ArgumentNullException(nameof(activityParticipationRuntimeState));
            }

            if (activityActorExitRuntimeState == null)
            {
                throw new ArgumentNullException(nameof(activityActorExitRuntimeState));
            }

            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity startedIdentity = BuildIdentity(command, SessionActivityStage.ActivityParticipantBindingStarted);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActivityParticipantBindingStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityParticipantBindingStarted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' participant binding stage started. participantOwnership='ActivityParticipationContext' sessionParticipationContext='required'.");
            endpoint.EmitSnapshot(
                snapshots,
                "activity_participant_binding_started",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' participant binding stage started. participantOwnership='ActivityParticipationContext'.");

            SessionActivityIdentity expectedInventoryIdentity = BuildIdentity(command, SessionActivityStage.ActivitySetupStarted);
            if (!inventory.IsValid || inventory.Identity.CycleKey != expectedInventoryIdentity.CycleKey)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActivityParticipantBindingFailed);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityParticipantBindingFailed);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' participant binding failed because ActivitySetupInventory is missing or foreign/stale.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_participant_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' participant binding failed because ActivitySetupInventory is missing or foreign/stale.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActivityEntryParticipantBindingStage][ParticipantBinding] Missing valid ActivitySetupInventory for activityId='{command.ActivityId}' entrySequence='{entrySequence}'.");
            }

            IReadOnlyList<ParticipantRequirement> participantRequirements = inventory.ParticipantRequirements;
            if (participantRequirements == null || participantRequirements.Count == 0)
            {
                PlayerSessionParticipationContext retainedSessionParticipationContext = ResolveSessionParticipationContextOrFail(
                    command,
                    activityParticipationRuntimeState,
                    endpoint,
                    facts,
                    snapshots);
                BeginPlayerActorActivityScope(playerActorRegistry, command.Identity);
                IReadOnlyList<PlayerActorIdentityRecord> activeActors = Array.Empty<PlayerActorIdentityRecord>();
                if (playerActorRegistry.TryGetIndexedActiveActorIdentities(out IReadOnlyList<PlayerActorIdentityRecord> resolvedActiveActors) &&
                    ActivityActorScopeCompatibilityPolicy.IsScopeCompatible(
                        playerActorRegistry.ActiveScopeIdentity,
                        command.Identity,
                        ActorScope.ActivityScoped) &&
                    resolvedActiveActors is { Count: > 0 })
                {
                    activeActors = resolvedActiveActors;
                }

                IReadOnlyList<PlayerActivityParticipantBinding> retainedExitBindings = activityActorExitRuntimeState.GetActivePlayerParticipantBindings() ??
                    Array.Empty<PlayerActivityParticipantBinding>();
                DebugUtility.Log(
                    typeof(ActivityEntryParticipantBindingStage),
                    $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipantRetainedBindingDiagnostics' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' participantRequirements='0' sessionParticipationContext='present' sessionParticipationRevision='{retainedSessionParticipationContext.Revision}' sessionParticipants='{retainedSessionParticipationContext.ParticipantCount}' activePlayerActorIdentities='{activeActors.Count}' playerExitBindings='{retainedExitBindings.Count}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                string retainedBindingSource = activeActors.Count > 0
                    ? "active_player_actor_identities"
                    : "player_exit_bindings";
                List<PlayerActivityParticipantBinding> retainedParticipants = new(activeActors.Count);
                List<ActivityEntryParticipantBindingResolvedRecord> retainedResolvedParticipants = new(activeActors.Count);
                for (int index = 0; index < activeActors.Count; index++)
                {
                    PlayerActorIdentityRecord activeActor = activeActors[index];
                    if (!activeActor.IsValid)
                    {
                        DebugUtility.Log(
                            typeof(ActivityEntryParticipantBindingStage),
                            $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipantRetainedBindingRejected' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' reason='invalid_active_actor_identity' participantId='{FormatSessionParticipantId(activeActor.ParticipantId)}' playerSlotId='{activeActor.PlayerSlotId}' actorId='{activeActor.ActorId}' source='{command.Source}' reason='{command.Reason}'.",
                            DebugUtility.Colors.Warning);
                        continue;
                    }

                    if (!activeActor.ParticipantBinding.IsValid)
                    {
                        DebugUtility.Log(
                            typeof(ActivityEntryParticipantBindingStage),
                            $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipantRetainedBindingRejected' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' reason='invalid_active_actor_binding' participantId='{FormatSessionParticipantId(activeActor.ParticipantId)}' playerSlotId='{activeActor.PlayerSlotId}' actorId='{activeActor.ActorId}' actorScope='{activeActor.ParticipantBinding.ActorScope}' source='{command.Source}' reason='{command.Reason}'.",
                            DebugUtility.Colors.Warning);
                        continue;
                    }

                    if (activeActor.ParticipantBinding is { RequiresPlayerActor: false, RequiresPlayerInput: false })
                    {
                        DebugUtility.Log(
                            typeof(ActivityEntryParticipantBindingStage),
                            $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipantRetainedBindingRejected' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' reason='actor_binding' requirementId='{activeActor.ParticipantBinding.RequirementId}' participantId='{FormatSessionParticipantId(activeActor.ParticipantId)}' playerSlotId='{activeActor.PlayerSlotId}' actorId='{activeActor.ActorId}' actorScope='{activeActor.ParticipantBinding.ActorScope}' source='{command.Source}' reason='{command.Reason}'.",
                            DebugUtility.Colors.Warning);
                        continue;
                    }

                    PlayerActivityParticipantBinding retainedParticipant = activeActor.ParticipantBinding;
                    retainedParticipants.Add(retainedParticipant);
                    retainedResolvedParticipants.Add(new ActivityEntryParticipantBindingResolvedRecord(
                        retainedParticipant.RequirementId.Value,
                        ActivityParticipantRequirementKind.ControllablePlayer,
                        retainedParticipant,
                        required: true));
                }

                if (retainedParticipants.Count == 0 && activeActors.Count == 0 && retainedExitBindings.Count > 0)
                {
                    for (int index = 0; index < retainedExitBindings.Count; index++)
                    {
                        PlayerActivityParticipantBinding retainedExitBinding = retainedExitBindings[index];
                        if (!retainedExitBinding.IsValid)
                        {
                            DebugUtility.Log(
                                typeof(ActivityEntryParticipantBindingStage),
                                $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipantRetainedBindingRejected' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' reason='invalid_binding' participantId='{FormatSessionParticipantId(retainedExitBinding.ParticipantId)}' playerSlotId='{retainedExitBinding.PlayerSlotId}' actorId='{retainedExitBinding.ActorId}' actorScope='{retainedExitBinding.ActorScope}' source='{command.Source}' reason='{command.Reason}'.",
                                DebugUtility.Colors.Warning);
                            continue;
                        }

                        if (retainedExitBinding is { RequiresPlayerActor: false, RequiresPlayerInput: false })
                        {
                            DebugUtility.Log(
                                typeof(ActivityEntryParticipantBindingStage),
                                $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipantRetainedBindingRejected' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' reason='actor_binding' requirementId='{retainedExitBinding.RequirementId}' participantId='{FormatSessionParticipantId(retainedExitBinding.ParticipantId)}' playerSlotId='{retainedExitBinding.PlayerSlotId}' actorId='{retainedExitBinding.ActorId}' actorScope='{retainedExitBinding.ActorScope}' source='{command.Source}' reason='{command.Reason}'.",
                                DebugUtility.Colors.Warning);
                            continue;
                        }

                        if (retainedExitBinding.ActorScope != ActorScope.SessionScoped)
                        {
                            DebugUtility.Log(
                                typeof(ActivityEntryParticipantBindingStage),
                                $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipantRetainedBindingRejected' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' reason='non_session_scoped_binding' requirementId='{retainedExitBinding.RequirementId}' participantId='{FormatSessionParticipantId(retainedExitBinding.ParticipantId)}' playerSlotId='{retainedExitBinding.PlayerSlotId}' actorId='{retainedExitBinding.ActorId}' actorScope='{retainedExitBinding.ActorScope}' source='{command.Source}' reason='{command.Reason}'.",
                                DebugUtility.Colors.Warning);
                            continue;
                        }

                        if (!bridge.TryGetSessionScopedPlayerActorForParticipant(command.Identity, retainedExitBinding, out PlayerActorRuntimeHandle retainedHandle) &&
                            !playerActorRegistry.TryGetIndexedActiveHandleByParticipant(retainedExitBinding.ParticipantId, out retainedHandle))
                        {
                            DebugUtility.Log(
                                typeof(ActivityEntryParticipantBindingStage),
                                $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipantRetainedBindingRejected' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' reason='session_scoped_handle_missing' requirementId='{retainedExitBinding.RequirementId}' participantId='{FormatSessionParticipantId(retainedExitBinding.ParticipantId)}' playerSlotId='{retainedExitBinding.PlayerSlotId}' actorId='{retainedExitBinding.ActorId}' actorScope='{retainedExitBinding.ActorScope}' source='{command.Source}' reason='{command.Reason}'.",
                                DebugUtility.Colors.Warning);
                            continue;
                        }

                        PlayerActorRuntimeHandle reboundHandle = RebindRetainedPlayerActorHandleForCurrentActivityOrFail(
                            command.Identity,
                            retainedExitBinding,
                            retainedHandle,
                            "retained_no_requirements");
                        TryRegisterRetainedPlayerActorHandle(
                            playerActorRegistry,
                            command.Identity,
                            reboundHandle,
                            command.ActivityId,
                            command.Source,
                            command.Reason,
                            "ActivityParticipantRetainedBindingChosen");
                        PlayerActivityParticipantBinding promotedBinding = reboundHandle.ParticipantBinding;
                        retainedParticipants.Add(promotedBinding);
                        retainedResolvedParticipants.Add(new ActivityEntryParticipantBindingResolvedRecord(
                            promotedBinding.RequirementId.Value,
                            ActivityParticipantRequirementKind.ControllablePlayer,
                            promotedBinding,
                            required: true));
                    }
                }

                if (retainedParticipants.Count > 0)
                {
                    DebugUtility.Log(
                        typeof(ActivityEntryParticipantBindingStage),
                        $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipantRetainedBindingChosen' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' retainedBindingSource='{retainedBindingSource}' sessionParticipationContext='present' sessionParticipationRevision='{retainedSessionParticipationContext.Revision}' sessionParticipants='{retainedSessionParticipationContext.ParticipantCount}' activePlayerActorIdentities='{activeActors.Count}' playerExitBindings='{retainedExitBindings.Count}' resolvedParticipants='{retainedParticipants.Count}' finalStatus='ResolvedFromRetainedSessionScopedActor' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Success);
                    SessionActivityIdentity completedAfterRetentionIdentity = BuildIdentity(command, SessionActivityStage.ActivityParticipantBindingCompleted);
                    StoreActivityParticipationBoundary(
                        new PlayerActivityParticipationContext(
                            completedAfterRetentionIdentity,
                            retainedParticipants,
                            command.Source,
                            command.Reason),
                        activityParticipationRuntimeState,
                        activityActorExitRuntimeState,
                        retainedSessionParticipationContext);
                    endpoint.SetCurrentIdentity(completedAfterRetentionIdentity, SessionActivityStage.ActivityParticipantBindingCompleted);
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantBindingCompleted,
                        completedAfterRetentionIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' participant binding completed resolved='{retainedParticipants.Count}' skipped='0' totalRequirements='0' status='ResolvedFromRetainedSessionScopedActor'.");
                    endpoint.EmitSnapshot(
                        snapshots,
                        "activity_participant_binding_completed",
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' participant binding completed resolved='{retainedParticipants.Count}' skipped='0' totalRequirements='0' status='ResolvedFromRetainedSessionScopedActor'.");
                    return new ActivityEntryParticipantBindingResult(
                        completedAfterRetentionIdentity,
                        totalRequirements: 0,
                        requiredRequirements: 0,
                        resolvedRequirements: retainedParticipants.Count,
                        skippedRequirements: 0,
                        requiredResolvedRequirements: 0,
                        resolvedParticipants: retainedResolvedParticipants);
                }

                SessionActivityIdentity skippedIdentity = BuildIdentity(command, SessionActivityStage.ActivityParticipantBindingSkippedNoRequirements);
                DebugUtility.Log(
                    typeof(ActivityEntryParticipantBindingStage),
                    $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipantRetainedBindingSkipped' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' participantRequirements='0' sessionParticipationContext='not_resolved' activePlayerActorIdentities='{activeActors.Count}' playerExitBindings='{retainedExitBindings.Count}' finalStatus='SkippedNoRequirements' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Warning);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivityParticipantBindingSkippedNoRequirements);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindingSkippedNoRequirements,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' participant binding skipped because no participant requirements were declared. inventoryId='{inventory.InventoryId}' totalRequirements='0' routeSessionParticipantPreparationConsumed='false'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_participant_binding_skipped_no_requirements",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' participant binding skipped because no participant requirements were declared totalRequirements='0'.");

                SessionActivityIdentity completedAfterSkipIdentity = BuildIdentity(command, SessionActivityStage.ActivityParticipantBindingCompleted);
                StoreActivityParticipationBoundary(
                    new PlayerActivityParticipationContext(
                        completedAfterSkipIdentity,
                        Array.Empty<PlayerActivityParticipantBinding>(),
                        command.Source,
                        command.Reason),
                    activityParticipationRuntimeState,
                    activityActorExitRuntimeState,
                    null);
                endpoint.SetCurrentIdentity(completedAfterSkipIdentity, SessionActivityStage.ActivityParticipantBindingCompleted);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindingCompleted,
                    completedAfterSkipIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' participant binding completed resolved='0' skipped='0' totalRequirements='0' status='SkippedNoRequirements'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_participant_binding_completed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' participant binding completed resolved='0' skipped='0' totalRequirements='0' status='SkippedNoRequirements'.");
                return new ActivityEntryParticipantBindingResult(
                    completedAfterSkipIdentity,
                    totalRequirements: 0,
                    requiredRequirements: 0,
                    resolvedRequirements: 0,
                    skippedRequirements: 0,
                    requiredResolvedRequirements: 0,
                    resolvedParticipants: Array.Empty<ActivityEntryParticipantBindingResolvedRecord>());
            }

            PlayerSessionParticipationContext sessionParticipationContext = ResolveSessionParticipationContextOrFail(
                command,
                activityParticipationRuntimeState,
                endpoint,
                facts,
                snapshots);
            Dictionary<PlayerSessionParticipantId, SessionActivityActorMaterializationPlanEntry> materializationPlanByParticipantId =
                BuildMaterializationPlanMap(bridge.GetActorMaterializationPlanEntries());

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityParticipantBindingResolutionStarted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' participant binding resolution started participantOwnership='ActivityParticipationContext' sessionParticipationRevision='{sessionParticipationContext.Revision}' sessionParticipants='{sessionParticipationContext.ParticipantCount}' materializationPlanEntries='{materializationPlanByParticipantId.Count}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "activity_participant_binding_resolution_started",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' participant binding resolution started sessionParticipants='{sessionParticipationContext.ParticipantCount}' materializationPlanEntries='{materializationPlanByParticipantId.Count}'.");

            int resolvedCount = 0;
            int skippedCount = 0;
            int requiredRequirementCount = 0;
            int requiredResolvedCount = 0;
            List<ActivityEntryParticipantBindingResolvedRecord> resolvedParticipants = new(participantRequirements.Count);
            List<ActivityParticipantBindCommand> bindCommands = new(participantRequirements.Count);
            List<ActivityParticipantMaterializationCommand> materializationCommands = new(participantRequirements.Count);
            List<ActivityParticipantPlacementCommand> placementCommands = new(participantRequirements.Count);
            List<ActivityParticipantResetCommand> resetCommands = new(participantRequirements.Count);
            List<PlayerActivityParticipantBinding> activityParticipantBindings = new(participantRequirements.Count);

            for (int index = 0; index < participantRequirements.Count; index++)
            {
                ParticipantRequirement requirement = participantRequirements[index];
                if (!requirement.IsValid)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActivityParticipantBindingFailed);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityParticipantBindingFailed);
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantBindingFailed,
                        failedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' participant binding failed invalid participant requirement index='{index}'.");
                    endpoint.EmitSnapshot(
                        snapshots,
                        "activity_participant_binding_failed",
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' participant binding failed invalid participant requirement index='{index}'.");
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActivityEntryParticipantBindingStage][ParticipantBinding] Invalid ParticipantRequirement at index='{index}' activityId='{command.ActivityId}' entrySequence='{entrySequence}'.");
                }

                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantRequirementDeclared,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' participant requirement declared requirementId='{requirement.Requirement.RequirementId}' participantKind='{requirement.ParticipantKind}' participantId='{requirement.ParticipantId}' expectedSessionRole='{requirement.ExpectedSessionRole}' requiredness='{requirement.Requirement.Requiredness}' participantOwnership='ActivityParticipationContext' activityOwnership='true' status='Declared'.");

                PlayerSessionParticipantId requestedParticipantId = requirement.SessionParticipantId;
                bool requirementRequired = requirement.Requirement.IsRequired;
                if (requirementRequired)
                {
                    requiredRequirementCount += 1;
                }

                if (!bridge.TryBuildActivityParticipantBinding(
                        requirement,
                        command.Source,
                        command.Reason,
                        out PlayerActivityParticipantBinding activityParticipantBinding,
                        out string activityParticipationResolutionReason))
                {
                    if (!requirementRequired)
                    {
                        skippedCount += 1;
                            bridge.LogActivityParticipationBindingSkipped(
                            command.ActivityId,
                            startedIdentity,
                            requirement.Requirement.RequirementId,
                            requestedParticipantId,
                            activityParticipationResolutionReason,
                            command.Source,
                            command.Reason);
                        endpoint.EmitFact(
                            facts,
                            SessionActivityFactKind.ActivityParticipantBindingResolved,
                            startedIdentity,
                            command.Source,
                            command.Reason,
                            $"'{command.ActivityId}' optional activity participation binding skipped requirementId='{requirement.Requirement.RequirementId}' participantKind='{requirement.ParticipantKind}' requestedParticipantId='{FormatSessionParticipantId(requestedParticipantId)}' skipReason='{activityParticipationResolutionReason}' sessionParticipationContext='present' status='OptionalActivityParticipationBindingSkipped'.");
                        continue;
                    }

                    SessionActivityIdentity missingSessionParticipantIdentity = BuildIdentity(command, SessionActivityStage.ActivityParticipantBindingFailed);
                    endpoint.SetCurrentIdentity(missingSessionParticipantIdentity, SessionActivityStage.ActivityParticipantBindingFailed);
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantBindingFailed,
                        missingSessionParticipantIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' required activity participant binding failed requirementId='{requirement.Requirement.RequirementId}' participantKind='{requirement.ParticipantKind}' requestedParticipantId='{FormatSessionParticipantId(requestedParticipantId)}' sessionParticipationContext='present' resolutionReason='{activityParticipationResolutionReason}' error='activity_session_participant_binding_missing'.");
                    endpoint.EmitSnapshot(
                        snapshots,
                        "activity_participant_binding_failed",
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' required activity participant binding failed requirementId='{requirement.Requirement.RequirementId}' resolutionReason='{activityParticipationResolutionReason}'.");
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActivityEntryParticipantBindingStage][ParticipantBinding] Required activity participant binding missing requirementId='{requirement.Requirement.RequirementId}' activityId='{command.ActivityId}' entrySequence='{entrySequence}' requestedParticipantId='{FormatSessionParticipantId(requestedParticipantId)}' resolutionReason='{activityParticipationResolutionReason}'.");
                }

                if (activityParticipantBinding.RequiresPlayerActor)
                {
                    try
                    {
                        ResolveMaterializationPlanEntryForActivityParticipantOrFail(
                            command.ActivityId,
                            activityParticipantBinding,
                            materializationPlanByParticipantId,
                            "binding_validation");
                    }
                    catch (Exception exception)
                    {
                        if (!requirementRequired)
                        {
                            skippedCount += 1;
                            bridge.LogActivityParticipationBindingSkipped(
                                command.ActivityId,
                                startedIdentity,
                                requirement.Requirement.RequirementId,
                                requestedParticipantId,
                                exception.Message,
                                command.Source,
                                command.Reason);
                            endpoint.EmitFact(
                                facts,
                                SessionActivityFactKind.ActivityParticipantBindingResolved,
                                startedIdentity,
                                command.Source,
                                command.Reason,
                                $"'{command.ActivityId}' optional activity participant materialization plan skipped requirementId='{requirement.Requirement.RequirementId}' participantId='{activityParticipantBinding.ParticipantId}' playerSlotId='{activityParticipantBinding.PlayerSlotId}' skipReason='{exception.Message}' status='OptionalMaterializationPlanMissingSkipped'.");
                            continue;
                        }

                        SessionActivityIdentity missingMaterializationPlanIdentity = BuildIdentity(command, SessionActivityStage.ActivityParticipantBindingFailed);
                        endpoint.SetCurrentIdentity(missingMaterializationPlanIdentity, SessionActivityStage.ActivityParticipantBindingFailed);
                        endpoint.EmitFact(
                            facts,
                            SessionActivityFactKind.ActivityParticipantBindingFailed,
                            missingMaterializationPlanIdentity,
                            command.Source,
                            command.Reason,
                            $"'{command.ActivityId}' required activity participant materialization plan missing requirementId='{requirement.Requirement.RequirementId}' participantId='{activityParticipantBinding.ParticipantId}' playerSlotId='{activityParticipantBinding.PlayerSlotId}' actorDefinitionId='{activityParticipantBinding.ActorDefinitionId}' routeOperationId='{sessionParticipationContext.RouteOperationId}' error='missing_activity_participant_materialization_plan'.");
                        endpoint.EmitSnapshot(
                            snapshots,
                            "activity_participant_binding_failed",
                            command.Source,
                            command.Reason,
                            $"'{command.ActivityId}' required activity participant materialization plan missing requirementId='{requirement.Requirement.RequirementId}' participantId='{activityParticipantBinding.ParticipantId}'.");
                        throw new InvalidOperationException(
                            $"missing_activity_participant_materialization_plan: activityId='{command.ActivityId}' participantId='{activityParticipantBinding.ParticipantId}' playerSlotId='{activityParticipantBinding.PlayerSlotId}' actorDefinitionId='{activityParticipantBinding.ActorDefinitionId}' routeOperationId='{sessionParticipationContext.RouteOperationId}'.");
                    }
                }

                activityParticipantBindings.Add(activityParticipantBinding);
                resolvedCount += 1;
                if (requirementRequired)
                {
                    requiredResolvedCount += 1;
                }

                resolvedParticipants.Add(new ActivityEntryParticipantBindingResolvedRecord(
                    requirement.Requirement.RequirementId,
                    requirement.ParticipantKind,
                    activityParticipantBinding,
                    requirementRequired));
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindingResolved,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' participant requirement resolved requirementId='{requirement.Requirement.RequirementId}' participantKind='{requirement.ParticipantKind}' requestedParticipantId='{FormatSessionParticipantId(requestedParticipantId)}' participantId='{activityParticipantBinding.ParticipantId}' role='{activityParticipantBinding.Role}' playerSlotId='{activityParticipantBinding.PlayerSlotId}' actorDefinitionId='{activityParticipantBinding.ActorDefinitionId}' actorId='{activityParticipantBinding.ActorId}' requiredness='{requirement.Requirement.Requiredness}' participantOwnership='ActivityParticipationContext' activityOwnership='true' status='ResolvedNominally' resolutionReason='{activityParticipationResolutionReason}'.");

                ActivityParticipantBindCommand bindCommand = new(
                    startedIdentity,
                    requirement.Requirement.RequirementId,
                    requirement.ParticipantKind,
                    requestedParticipantId,
                    activityParticipantBinding,
                    command.Source,
                    command.Reason);
                ActivityParticipantMaterializationCommand materializationCommand = new(
                    startedIdentity,
                    requirement.Requirement.RequirementId,
                    requirement.ParticipantKind,
                    activityParticipantBinding,
                    ActivityParticipantMaterializationNeedKind.EnsureRouteSessionParticipantAvailable,
                    command.Source,
                    command.Reason);
                ActivityParticipantPlacementCommand placementCommand = new(
                    startedIdentity,
                    requirement.Requirement.RequirementId,
                    activityParticipantBinding,
                    requirement.PlacementRequirementId,
                    command.Source,
                    command.Reason);
                ActivityParticipantResetCommand resetCommand = new(
                    startedIdentity,
                    requirement.Requirement.RequirementId,
                    activityParticipantBinding,
                    requirement.PlacementRequirementId,
                    BuildDefaultParticipantResetGroups(),
                    command.Source,
                    command.Reason);

                if (!bindCommand.IsValid || !materializationCommand.IsValid || !placementCommand.IsValid || !resetCommand.IsValid)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActivityParticipantBindingFailed);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityParticipantBindingFailed);
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantBindingFailed,
                        failedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' participant command plan failed invalid command requirementId='{requirement.Requirement.RequirementId}' participantId='{activityParticipantBinding.ParticipantId}' playerSlotId='{activityParticipantBinding.PlayerSlotId}'.");
                    endpoint.EmitSnapshot(
                        snapshots,
                        "activity_participant_binding_failed",
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' participant command plan failed invalid command requirementId='{requirement.Requirement.RequirementId}'.");
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActivityEntryParticipantBindingStage][ParticipantBinding] Invalid participant command plan requirementId='{requirement.Requirement.RequirementId}' activityId='{command.ActivityId}' entrySequence='{entrySequence}'.");
                }

                bindCommands.Add(bindCommand);
                materializationCommands.Add(materializationCommand);
                placementCommands.Add(placementCommand);
                resetCommands.Add(resetCommand);
            }

            if (bindCommands.Count > 0)
            {
                EmitParticipantCommandPlan(
                    command.ActivityId,
                    command,
                    endpoint,
                    bridge,
                    playerActorRegistry,
                    placementMarkerLookup,
                    facts,
                    snapshots,
                    startedIdentity,
                    materializationPlanByParticipantId,
                    bindCommands,
                    materializationCommands,
                    placementCommands,
                    resetCommands);
            }

            SessionActivityIdentity completedIdentity = BuildIdentity(command, SessionActivityStage.ActivityParticipantBindingCompleted);
            StoreActivityParticipationBoundary(
                new PlayerActivityParticipationContext(
                    completedIdentity,
                    activityParticipantBindings,
                    command.Source,
                    command.Reason),
                activityParticipationRuntimeState,
                activityActorExitRuntimeState,
                sessionParticipationContext);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActivityParticipantBindingCompleted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityParticipantBindingCompleted,
                completedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' participant binding completed resolved='{resolvedCount}' skipped='{skippedCount}' totalRequirements='{participantRequirements.Count}' participantOwnership='ActivityParticipationContext' activityOwnership='true' status='ResolvedNominally'.");
            endpoint.EmitSnapshot(
                snapshots,
                "activity_participant_binding_completed",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' participant binding completed resolved='{resolvedCount}' skipped='{skippedCount}' totalRequirements='{participantRequirements.Count}'.");
            return new ActivityEntryParticipantBindingResult(
                completedIdentity,
                participantRequirements.Count,
                requiredRequirementCount,
                resolvedCount,
                skippedCount,
                requiredResolvedCount,
                resolvedParticipants);
        }

        private static SessionActivityIdentity BuildIdentity(
            ActivityEntryParticipantBindingCommand command,
            SessionActivityStage stage)
        {
            return new SessionActivityIdentity(
                command.Identity.PipelineId,
                command.Identity.SessionId,
                command.ActivityId,
                command.ActivityOrdinal,
                command.Identity.EntrySequence,
                stage,
                command.Source);
        }

        private static void EmitParticipantCommandPlan(
            string activityId,
            ActivityEntryParticipantBindingCommand command,
            IActivityEntryRuntimeBridge endpoint,
            IActivityEntryParticipantBindingRuntimeBridge bridge,
            ActivityPlayerActorRegistry playerActorRegistry,
            IActivityEntryPlacementMarkerLookup placementMarkerLookup,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityIdentity identity,
            Dictionary<PlayerSessionParticipantId, SessionActivityActorMaterializationPlanEntry> materializationPlanByParticipantId,
            IReadOnlyList<ActivityParticipantBindCommand> bindCommands,
            IReadOnlyList<ActivityParticipantMaterializationCommand> materializationCommands,
            IReadOnlyList<ActivityParticipantPlacementCommand> placementCommands,
            IReadOnlyList<ActivityParticipantResetCommand> resetCommands)
        {
            ActivityParticipantCommandPlan plan = new(
                identity,
                bindCommands,
                materializationCommands,
                placementCommands,
                resetCommands,
                command.Source,
                command.Reason);

            if (!plan.IsValid || !plan.HasCommands)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindingFailed,
                    identity,
                    command.Source,
                    command.Reason,
                    $"'{activityId}' participant command plan failed invalid plan.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActivityEntryParticipantBindingStage][ParticipantBinding] Invalid participant command plan activityId='{activityId}' entrySequence='{identity.EntrySequence}'.");
            }

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityParticipantCommandPlanReady,
                identity,
                command.Source,
                command.Reason,
                $"'{activityId}' participant command plan ready totalCommands='{plan.TotalCommandCount}' bind='{bindCommands.Count}' materialization='{materializationCommands.Count}' placement='{placementCommands.Count}' reset='{resetCommands.Count}' participantOwnership='ActivityParticipationContext' activityOwnership='true' adapterExecution='true' commandOwner='ActivityEntryPipeline' status='AdaptersConnected'.");
            endpoint.EmitSnapshot(
                snapshots,
                "activity_participant_command_plan_ready",
                command.Source,
                command.Reason,
                $"'{activityId}' participant command plan ready totalCommands='{plan.TotalCommandCount}' adapterExecution='true' commandOwner='ActivityEntryPipeline'.");
            for (int index = 0; index < bindCommands.Count; index++)
            {
                ActivityParticipantBindCommand bindCommand = bindCommands[index];
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindCommandIssued,
                    bindCommand.Identity,
                    bindCommand.Source,
                    bindCommand.Reason,
                    $"'{activityId}' participant bind command issued requirementId='{bindCommand.RequirementId}' requestedParticipantId='{FormatSessionParticipantId(bindCommand.RequestedParticipantId)}' participantId='{bindCommand.ParticipantBinding.ParticipantId}' role='{bindCommand.ParticipantBinding.Role}' playerSlotId='{bindCommand.ParticipantBinding.PlayerSlotId}' actorDefinitionId='{bindCommand.ParticipantBinding.ActorDefinitionId}' actorId='{bindCommand.ParticipantBinding.ActorId}' participantOwnership='ActivityParticipationContext' activityOwnership='true' adapterExecution='true' commandOwner='ActivityEntryPipeline'.");
            }

            for (int index = 0; index < materializationCommands.Count; index++)
            {
                ActivityParticipantMaterializationCommand materializationCommand = materializationCommands[index];
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantMaterializationCommandIssued,
                    materializationCommand.Identity,
                    materializationCommand.Source,
                    materializationCommand.Reason,
                    $"'{activityId}' participant materialization command issued requirementId='{materializationCommand.RequirementId}' participantId='{materializationCommand.ParticipantBinding.ParticipantId}' role='{materializationCommand.ParticipantBinding.Role}' playerSlotId='{materializationCommand.ParticipantBinding.PlayerSlotId}' actorDefinitionId='{materializationCommand.ParticipantBinding.ActorDefinitionId}' actorId='{materializationCommand.ParticipantBinding.ActorId}' participantKind='{materializationCommand.ParticipantKind}' needKind='{materializationCommand.NeedKind}' participantOwnership='ActivityParticipationContext' activityOwnership='true' adapterExecution='true' commandOwner='ActivityEntryPipeline'.");
            }

            for (int index = 0; index < placementCommands.Count; index++)
            {
                ActivityParticipantPlacementCommand placementCommand = placementCommands[index];
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantPlacementCommandIssued,
                    placementCommand.Identity,
                    placementCommand.Source,
                    placementCommand.Reason,
                    $"'{activityId}' participant placement command issued requirementId='{placementCommand.RequirementId}' participantId='{placementCommand.ParticipantBinding.ParticipantId}' role='{placementCommand.ParticipantBinding.Role}' playerSlotId='{placementCommand.ParticipantBinding.PlayerSlotId}' actorDefinitionId='{placementCommand.ParticipantBinding.ActorDefinitionId}' actorId='{placementCommand.ParticipantBinding.ActorId}' placementRequirementId='{(string.IsNullOrWhiteSpace(placementCommand.PlacementRequirementId) ? "<none>" : placementCommand.PlacementRequirementId)}' placementScope='ActivityLocal' participantOwnership='ActivityParticipationContext' activityOwnership='true' adapterExecution='true' commandOwner='ActivityEntryPipeline'.");
            }

            for (int index = 0; index < resetCommands.Count; index++)
            {
                ActivityParticipantResetCommand resetCommand = resetCommands[index];
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantResetCommandIssued,
                    resetCommand.Identity,
                    resetCommand.Source,
                    resetCommand.Reason,
                    $"'{activityId}' participant reset command issued requirementId='{resetCommand.RequirementId}' participantId='{resetCommand.ParticipantBinding.ParticipantId}' role='{resetCommand.ParticipantBinding.Role}' playerSlotId='{resetCommand.ParticipantBinding.PlayerSlotId}' actorDefinitionId='{resetCommand.ParticipantBinding.ActorDefinitionId}' actorId='{resetCommand.ParticipantBinding.ActorId}' placementRequirementId='{(string.IsNullOrWhiteSpace(resetCommand.PlacementRequirementId) ? "<none>" : resetCommand.PlacementRequirementId)}' resetGroups='{FormatActivityStateResetGroups(resetCommand.ResetGroups)}' participantOwnership='ActivityParticipationContext' activityOwnership='true' adapterExecution='true' commandOwner='ActivityEntryPipeline'.");
            }

            ExecuteParticipantCommandPlan(activityId, command, endpoint, bridge, playerActorRegistry, placementMarkerLookup, facts, snapshots, identity, plan, materializationPlanByParticipantId);
        }

        private static void ExecuteParticipantCommandPlan(
            string activityId,
            ActivityEntryParticipantBindingCommand command,
            IActivityEntryRuntimeBridge endpoint,
            IActivityEntryParticipantBindingRuntimeBridge bridge,
            ActivityPlayerActorRegistry playerActorRegistry,
            IActivityEntryPlacementMarkerLookup placementMarkerLookup,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityIdentity identity,
            ActivityParticipantCommandPlan plan,
            Dictionary<PlayerSessionParticipantId, SessionActivityActorMaterializationPlanEntry> materializationPlanByParticipantId)
        {
            try
            {
                BeginPlayerActorActivityScope(playerActorRegistry, identity);
                Dictionary<PlayerSessionParticipantId, PlayerActorIdentityRecord> ensuredActorsByActivityParticipant = new();

                for (int index = 0; index < plan.MaterializationCommands.Count; index++)
                {
                    ActivityParticipantMaterializationCommand materializationCommand = plan.MaterializationCommands[index];
                    PlayerActorIdentityRecord actorIdentity = EnsureParticipantMaterialized(
                        activityId,
                        identity,
                        materializationCommand,
                        bridge,
                        playerActorRegistry,
                        materializationPlanByParticipantId,
                        command.Source,
                        command.Reason);
                    if (materializationCommand.ParticipantBinding.ParticipantId.IsValid)
                    {
                        ensuredActorsByActivityParticipant[materializationCommand.ParticipantBinding.ParticipantId] = actorIdentity;
                    }
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantMaterialized,
                        identity,
                        command.Source,
                        command.Reason,
                        $"'{activityId}' participant materialization applied requirementId='{materializationCommand.RequirementId}' participantId='{materializationCommand.ParticipantBinding.ParticipantId}' role='{materializationCommand.ParticipantBinding.Role}' playerSlotId='{materializationCommand.ParticipantBinding.PlayerSlotId}' actorDefinitionId='{materializationCommand.ParticipantBinding.ActorDefinitionId}' actorId='{materializationCommand.ParticipantBinding.ActorId}' needKind='{materializationCommand.NeedKind}' adapterExecution='true' commandOwner='ActivityEntryPipeline' participantOwnership='ActivityParticipationContext' activityOwnership='true'.");
                }

                for (int index = 0; index < plan.BindCommands.Count; index++)
                {
                    ActivityParticipantBindCommand bindCommand = plan.BindCommands[index];
                    PlayerActorIdentityRecord actorIdentity = EnsureResolvedActorIdentityForActivityParticipantOrFail(bindCommand.ParticipantBinding, ensuredActorsByActivityParticipant, activityId, "bind");
                    PlayerActorParticipationEnterCommand enterCommand = new(identity, new[] { actorIdentity }, bindCommand.Source, bindCommand.Reason);
                    IReadOnlyList<PlayerActorParticipationEnterRecord> records = bridge.ExecutePlayerActorParticipationEnter(enterCommand, identity);
                    if (records.Count != 1 || !records[0].IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Invalid participant bind apply record for participantId='{bindCommand.ParticipantBinding.ParticipantId}' requirementId='{bindCommand.RequirementId}'.");
                    }

                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantBindApplied,
                        identity,
                        command.Source,
                        command.Reason,
                        $"'{activityId}' participant bind applied requirementId='{bindCommand.RequirementId}' participantId='{bindCommand.ParticipantBinding.ParticipantId}' role='{bindCommand.ParticipantBinding.Role}' playerSlotId='{bindCommand.ParticipantBinding.PlayerSlotId}' actorDefinitionId='{bindCommand.ParticipantBinding.ActorDefinitionId}' actorId='{bindCommand.ParticipantBinding.ActorId}' adapterExecution='true' commandOwner='ActivityEntryPipeline' participantOwnership='ActivityParticipationContext' activityOwnership='true'.");
                }

                for (int index = 0; index < plan.PlacementCommands.Count; index++)
                {
                    ActivityParticipantPlacementCommand placementCommand = plan.PlacementCommands[index];
                    PlayerActorIdentityRecord actorIdentity = EnsureResolvedActorIdentityForActivityParticipantOrFail(placementCommand.ParticipantBinding, ensuredActorsByActivityParticipant, activityId, "placement");
                    SessionActivityActorMaterializationPlanEntry materializationPlanEntry =
                        ResolveMaterializationPlanEntryForActivityParticipantOrFail(activityId, placementCommand.ParticipantBinding, materializationPlanByParticipantId, "placement");

                    string placementId = ResolvePlacementIdForCommand(placementCommand, materializationPlanEntry);
                    ResolvePlacementPlanForEntry(
                        activityId,
                        identity,
                        placementMarkerLookup,
                        playerActorRegistry,
                        materializationPlanEntry,
                        placementId,
                        placementCommand.RequirementId,
                        placementCommand.ParticipantBinding,
                        "placement",
                        out bool placementDeclared,
                        out bool placementRequired,
                        out bool placementOptional,
                        out bool hasPlacement,
                        out Vector3 placementPosition,
                        out Vector3 placementEuler);

                    ActorResetTargetRef placementTarget = new(
                        BuildActorResetActorRef(identity, actorIdentity, activityId, playerActorRegistry, "placement"),
                        new[] { ActorResetGroup.Placement },
                        placementId,
                        placementDeclared,
                        placementRequired,
                        placementOptional,
                        hasPlacement,
                        placementPosition,
                        placementEuler);
                    ActorResetCommand placementResetCommand = new(identity, new[] { placementTarget }, placementCommand.Source, placementCommand.Reason);
                    IReadOnlyList<ActorResetResult> placementRecords = bridge.ExecuteActorReset(placementResetCommand, identity);
                    if (placementRecords.Count != 1 || !placementRecords[0].IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Invalid participant placement apply record for participantId='{placementCommand.ParticipantBinding.ParticipantId}' requirementId='{placementCommand.RequirementId}'.");
                    }

                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantPlacementApplied,
                        identity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' participant placement applied requirementId='{placementCommand.RequirementId}' participantId='{placementCommand.ParticipantBinding.ParticipantId}' role='{placementCommand.ParticipantBinding.Role}' playerSlotId='{placementCommand.ParticipantBinding.PlayerSlotId}' actorDefinitionId='{placementCommand.ParticipantBinding.ActorDefinitionId}' actorId='{placementCommand.ParticipantBinding.ActorId}' placementRequirementId='{(string.IsNullOrWhiteSpace(placementCommand.PlacementRequirementId) ? "<none>" : placementCommand.PlacementRequirementId)}' appliedGroups='{placementRecords[0].AppliedGroups.Count}' skippedGroups='{placementRecords[0].SkippedGroups.Count}' adapterExecution='true' commandOwner='ActivityEntryPipeline' participantOwnership='ActivityParticipationContext' activityOwnership='true'.");
                    DebugUtility.Log(typeof(ActivityEntryParticipantBindingStage),
                        $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipantPlacementApplied' activityId='{command.ActivityId}' entrySequence='{identity.EntrySequence}' requirementId='{placementCommand.RequirementId}' actorId='{placementCommand.ParticipantBinding.ActorId}' actorScope='{placementCommand.ParticipantBinding.ActorScope}' playerSlotId='{placementCommand.ParticipantBinding.PlayerSlotId}' placementRequirementId='{(string.IsNullOrWhiteSpace(placementCommand.PlacementRequirementId) ? "<none>" : placementCommand.PlacementRequirementId)}' appliedGroups='{placementRecords[0].AppliedGroups.Count}' skippedGroups='{placementRecords[0].SkippedGroups.Count}' owner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Success);
                }

                for (int index = 0; index < plan.ResetCommands.Count; index++)
                {
                    ActivityParticipantResetCommand resetCommand = plan.ResetCommands[index];
                    PlayerActorIdentityRecord actorIdentity = EnsureResolvedActorIdentityForActivityParticipantOrFail(
                        resetCommand.ParticipantBinding,
                        ensuredActorsByActivityParticipant,
                        activityId,
                        "reset");
                    SessionActivityActorMaterializationPlanEntry materializationPlanEntry =
                        ResolveMaterializationPlanEntryForActivityParticipantOrFail(activityId, resetCommand.ParticipantBinding, materializationPlanByParticipantId, "reset");

                    string placementId = ResolvePlacementIdForResetCommand(resetCommand, materializationPlanEntry);
                    ResolvePlacementPlanForEntry(
                        activityId,
                        identity,
                        placementMarkerLookup,
                        playerActorRegistry,
                        materializationPlanEntry,
                        placementId,
                        resetCommand.RequirementId,
                        resetCommand.ParticipantBinding,
                        "reset",
                        out bool placementDeclared,
                        out bool placementRequired,
                        out bool placementOptional,
                        out bool hasPlacement,
                        out Vector3 placementPosition,
                        out Vector3 placementEuler);

                    ActorResetTargetRef resetTarget = new(
                        BuildActorResetActorRef(identity, actorIdentity, resetCommand.ParticipantBinding, activityId, playerActorRegistry, "reset"),
                        MapResetGroupsOrFail(resetCommand.ResetGroups),
                        placementId,
                        placementDeclared,
                        placementRequired,
                        placementOptional,
                        hasPlacement,
                        placementPosition,
                        placementEuler);
                    ActorResetCommand actorResetCommand = new(identity, new[] { resetTarget }, resetCommand.Source, resetCommand.Reason);
                    IReadOnlyList<ActorResetResult> resetRecords = bridge.ExecuteActorReset(actorResetCommand, identity);
                    if (resetRecords.Count != 1 || !resetRecords[0].IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Invalid participant reset apply record for participantId='{resetCommand.ParticipantBinding.ParticipantId}' playerSlotId='{resetCommand.ParticipantBinding.PlayerSlotId}' requirementId='{resetCommand.RequirementId}'.");
                    }
                    ValidateRequiredResetGroupsOrFail(resetCommand, resetRecords[0]);

                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantResetApplied,
                        identity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' participant reset applied requirementId='{resetCommand.RequirementId}' participantId='{resetCommand.ParticipantBinding.ParticipantId}' role='{resetCommand.ParticipantBinding.Role}' playerSlotId='{resetCommand.ParticipantBinding.PlayerSlotId}' actorDefinitionId='{resetCommand.ParticipantBinding.ActorDefinitionId}' actorId='{resetCommand.ParticipantBinding.ActorId}' placementRequirementId='{(string.IsNullOrWhiteSpace(resetCommand.PlacementRequirementId) ? "<none>" : resetCommand.PlacementRequirementId)}' resetGroups='{FormatActivityStateResetGroups(resetCommand.ResetGroups)}' appliedGroups='{resetRecords[0].AppliedGroups.Count}' skippedGroups='{resetRecords[0].SkippedGroups.Count}' adapterExecution='true' commandOwner='ActivityEntryPipeline' participantOwnership='ActivityParticipationContext' activityOwnership='true'.");
                    DebugUtility.Log(typeof(ActivityEntryParticipantBindingStage),
                        $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipantResetApplied' activityId='{command.ActivityId}' entrySequence='{identity.EntrySequence}' requirementId='{resetCommand.RequirementId}' actorId='{resetCommand.ParticipantBinding.ActorId}' actorScope='{resetCommand.ParticipantBinding.ActorScope}' playerSlotId='{resetCommand.ParticipantBinding.PlayerSlotId}' resetGroups='{FormatActivityStateResetGroups(resetCommand.ResetGroups)}' appliedGroups='{resetRecords[0].AppliedGroups.Count}' skippedGroups='{resetRecords[0].SkippedGroups.Count}' owner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Success);
                }
            }
            catch (Exception exception)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantSetupFailed,
                    identity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' participant setup failed commandOwner='ActivityEntryPipeline' participantOwnership='ActivityParticipationContext' activityOwnership='true' error='{exception.Message}'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_participant_setup_failed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' participant setup failed error='{exception.Message}'.");
                throw;
            }
        }

        private static IReadOnlyList<ActivityStateResetGroup> BuildDefaultParticipantResetGroups()
        {
            return new[]
            {
                ActivityStateResetGroup.Placement,
                ActivityStateResetGroup.ActivityParticipation,
            };
        }

        private static Dictionary<PlayerSessionParticipantId, SessionActivityActorMaterializationPlanEntry> BuildMaterializationPlanMap(IReadOnlyList<SessionActivityActorMaterializationPlanEntry> materializationPlanEntries)
        {
            Dictionary<PlayerSessionParticipantId, SessionActivityActorMaterializationPlanEntry> map = new();
            if (materializationPlanEntries == null || materializationPlanEntries.Count == 0)
            {
                return map;
            }

            for (int index = 0; index < materializationPlanEntries.Count; index++)
            {
                SessionActivityActorMaterializationPlanEntry entry = materializationPlanEntries[index];
                if (entry.IsValid && !map.ContainsKey(entry.ParticipantId))
                {
                    map.Add(entry.ParticipantId, entry);
                }
            }

            return map;
        }

        private static PlayerActorIdentityRecord EnsureParticipantMaterialized(
            string activityId,
            SessionActivityIdentity identity,
            ActivityParticipantMaterializationCommand command,
            IActivityEntryParticipantBindingRuntimeBridge bridge,
            ActivityPlayerActorRegistry playerActorRegistry,
            Dictionary<PlayerSessionParticipantId, SessionActivityActorMaterializationPlanEntry> materializationPlanByParticipantId,
            string source,
            string reason)
        {
            PlayerActivityParticipantBinding participant = command.ParticipantBinding;
            if (!participant.IsValid)
            {
                throw new InvalidOperationException(
                    $"activity_participant_materialization_binding_invalid: activityId='{activityId}' entrySequence='{identity.EntrySequence}' requirementId='{command.RequirementId}'.");
            }

            string playerSlotId = participant.PlayerSlotId.IsValid ? Normalize(participant.PlayerSlotId.Value) : string.Empty;
            string sessionParticipantId = participant.ParticipantId.IsValid ? Normalize(participant.ParticipantId.Value) : string.Empty;
            string actorDefinitionId = participant.ActorDefinitionId.IsValid ? Normalize(participant.ActorDefinitionId.Value) : string.Empty;
            string actorId = participant.ActorId.IsValid ? Normalize(participant.ActorId.Value) : string.Empty;
            if (string.IsNullOrWhiteSpace(playerSlotId) || string.IsNullOrWhiteSpace(sessionParticipantId) || string.IsNullOrWhiteSpace(actorDefinitionId) || string.IsNullOrWhiteSpace(actorId))
            {
                throw new InvalidOperationException(
                    $"activity_participant_materialization_identity_invalid: activityId='{activityId}' entrySequence='{identity.EntrySequence}' requirementId='{command.RequirementId}' participantId='{sessionParticipantId}' playerSlotId='{playerSlotId}' actorDefinitionId='{actorDefinitionId}' actorId='{actorId}'.");
            }

            bool routeScopedRetentionAllowed = participant.ActorScope == ActorScope.RouteScoped;
            bool sessionScopedRetentionAllowed = participant.ActorScope == ActorScope.SessionScoped;
            PlayerActorRuntimeHandle retainedHandle = default;
            bool hasRetainedHandle =
                (routeScopedRetentionAllowed &&
                 playerActorRegistry.TryGetRouteScopedHandleByParticipant(participant.ParticipantId, out retainedHandle)) ||
                (sessionScopedRetentionAllowed &&
                 (bridge.TryGetSessionScopedPlayerActorForParticipant(identity, participant, out retainedHandle) ||
                  playerActorRegistry.TryGetIndexedActiveHandleByParticipant(participant.ParticipantId, out retainedHandle)));

            if (hasRetainedHandle)
            {
                GameObject retainedInstance = retainedHandle.Instance;
                if (retainedInstance == null)
                {
                    throw new InvalidOperationException($"Retained participant instance is null participantId='{sessionParticipantId}' playerSlotId='{playerSlotId}'.");
                }

                PlayerActorIdentity identityComponent = retainedInstance.GetComponent<PlayerActorIdentity>();
                if (identityComponent == null)
                {
                    throw new InvalidOperationException($"Retained participant is missing PlayerActorIdentity component participantId='{sessionParticipantId}' playerSlotId='{playerSlotId}'.");
                }

                PlayerActorIdentityRecord reboundIdentity = BuildParticipantActorIdentity(identity, participant);
                identityComponent.Bind(identity, reboundIdentity);
                EnsurePlayerRuntimeActorIdentityBoundOrFail(retainedInstance, identity, participant.ParticipantId, "retained_rebind");
                Actor retainedActor = retainedInstance.GetComponent<Actor>();
                if (retainedActor == null)
                {
                    throw new InvalidOperationException($"Retained participant is missing Actor component participantId='{sessionParticipantId}' playerSlotId='{playerSlotId}'.");
                }

                PlayerActorRuntimeHandle reboundHandle = new(reboundIdentity, retainedInstance, retainedActor);
                TryRegisterRetainedPlayerActorHandle(
                    playerActorRegistry,
                    identity,
                    reboundHandle,
                    activityId,
                    source,
                    reason,
                    "ActivityParticipantActorMaterializationRetained");
                DebugUtility.Log(typeof(ActivityEntryParticipantBindingStage),
                    $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipantActorMaterializationRetained' activityId='{activityId}' entrySequence='{identity.EntrySequence}' requirementId='{command.RequirementId}' actorId='{actorId}' actorScope='{participant.ActorScope}' playerSlotId='{playerSlotId}' playerActorId='{reboundIdentity.PlayerActorId}' materializationPolicy='{participant.MaterializationPolicy}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Success);
                return reboundIdentity;
            }

            SessionActivityActorMaterializationPlanEntry materializationPlanEntry =
                ResolveMaterializationPlanEntryForActivityParticipantOrFail(activityId, participant, materializationPlanByParticipantId, "materialization");
            if (materializationPlanEntry.Prefab == null)
            {
                throw new InvalidOperationException(
                    $"missing_activity_participant_materialization_prefab: participantId='{sessionParticipantId}' playerSlotId='{playerSlotId}' actorDefinitionId='{actorDefinitionId}' activityId='{activityId}' operation='materialization' requirementId='{command.RequirementId}'.");
            }

            Vector3 localPosition = materializationPlanEntry.PlacementMode == ActorPlacementMode.FixedTransform
                ? materializationPlanEntry.LocalPosition
                : Vector3.zero;
            Vector3 localEuler = materializationPlanEntry.PlacementMode == ActorPlacementMode.FixedTransform
                ? materializationPlanEntry.LocalEulerAngles
                : Vector3.zero;
            PlayerActorIdentityRecord actorIdentity = BuildParticipantActorIdentity(identity, participant);
            PlayerActorEntryPlan plan = new(actorIdentity, materializationPlanEntry.Prefab, localPosition, localEuler);
            PlayerActorMaterializationCommand playerMaterializationCommand = new(identity, new[] { plan }, source, reason);
            IReadOnlyList<PlayerActorMaterializationRecord> records = bridge.ExecutePlayerActorMaterialization(playerMaterializationCommand, identity);
            if (records.Count != 1 || !records[0].IsValid)
            {
                throw new InvalidOperationException(
                    $"Materialization adapter returned invalid record participantId='{sessionParticipantId}' playerSlotId='{playerSlotId}' requirementId='{command.RequirementId}'.");
            }

            EnsurePlayerRuntimeActorIdentityBoundOrFail(records[0].Instance, identity, participant.ParticipantId, "materialization");
            playerActorRegistry.IndexActiveHandle(records[0].RuntimeHandle);
            if (ActivityActorScopeCompatibilityPolicy.ShouldTrackInRouteIndex(
                    identity,
                    records[0].RuntimeHandle,
                    nameof(SessionActivityPipeline),
                    "RegisterMaterializedPlayerActor"))
            {
                playerActorRegistry.IndexRouteScopedHandle(records[0].RuntimeHandle);
            }
            DebugUtility.Log(typeof(ActivityEntryParticipantBindingStage),
                $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipantActorMaterialized' activityId='{activityId}' entrySequence='{identity.EntrySequence}' requirementId='{command.RequirementId}' actorId='{actorId}' actorScope='{participant.ActorScope}' playerSlotId='{playerSlotId}' playerActorId='{records[0].ActorIdentity.PlayerActorId}' materializationPolicy='{participant.MaterializationPolicy}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Success);
            return records[0].ActorIdentity;
        }

        private static PlayerActorRuntimeHandle RebindRetainedPlayerActorHandleForCurrentActivityOrFail(
            SessionActivityIdentity identity,
            PlayerActivityParticipantBinding participant,
            PlayerActorRuntimeHandle retainedHandle,
            string operation)
        {
            if (!identity.IsValid || !participant.IsValid || !retainedHandle.IsValid)
            {
                throw new InvalidOperationException($"Cannot rebind retained player actor handle for operation='{Normalize(operation)}'.");
            }

            GameObject retainedInstance = retainedHandle.Instance;
            if (retainedInstance == null)
            {
                throw new InvalidOperationException($"Retained participant instance is null participantId='{participant.ParticipantId}' playerSlotId='{participant.PlayerSlotId}'.");
            }

            PlayerActorIdentity identityComponent = retainedInstance.GetComponent<PlayerActorIdentity>();
            if (identityComponent == null)
            {
                throw new InvalidOperationException($"Retained participant is missing PlayerActorIdentity component participantId='{participant.ParticipantId}' playerSlotId='{participant.PlayerSlotId}'.");
            }

            PlayerActorIdentityRecord reboundIdentity = BuildParticipantActorIdentity(identity, participant);
            identityComponent.Bind(identity, reboundIdentity);
            EnsurePlayerRuntimeActorIdentityBoundOrFail(retainedInstance, identity, participant.ParticipantId, operation);

            Actor retainedActor = retainedInstance.GetComponent<Actor>();
            if (retainedActor == null)
            {
                throw new InvalidOperationException($"Retained participant is missing Actor component participantId='{participant.ParticipantId}' playerSlotId='{participant.PlayerSlotId}'.");
            }

            return new PlayerActorRuntimeHandle(reboundIdentity, retainedInstance, retainedActor);
        }

        private static void EnsurePlayerRuntimeActorIdentityBoundOrFail(
            GameObject actorInstance,
            SessionActivityIdentity identity,
            PlayerSessionParticipantId participantId,
            string operation)
        {
            if (actorInstance == null)
            {
                throw new InvalidOperationException(
                    $"player_actor_runtime_identity_missing_after_{operation}: participantId='{participantId}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' reason='actor_instance_null'.");
            }

            Actor runtimeActor = actorInstance.GetComponent<Actor>();
            if (runtimeActor == null)
            {
                throw new InvalidOperationException(
                    $"player_actor_runtime_identity_missing_after_{operation}: participantId='{participantId}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' reason='runtime_actor_missing'.");
            }

            ActorInstanceRuntimeId actorInstanceRuntimeId = ActorInstanceRuntimeId.FromScopedRuntimeActorIdentity(
                identity,
                runtimeActor.ActorId,
                runtimeActor.ActorScopeMetadata,
                runtimeActor.ActorScopeMetadata.ToString());
            if (!actorInstanceRuntimeId.IsValid)
            {
                throw new InvalidOperationException(
                    $"player_actor_runtime_identity_missing_after_{operation}: participantId='{participantId}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' reason='runtime_actor_instance_runtime_id_invalid'.");
            }

            runtimeActor.SetRuntimeActorInstanceId(actorInstanceRuntimeId);
            if (!runtimeActor.RuntimeActorInstanceId.IsValid ||
                runtimeActor.RuntimeActorInstanceId != actorInstanceRuntimeId)
            {
                throw new InvalidOperationException(
                    $"player_actor_runtime_identity_missing_after_{operation}: participantId='{participantId}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' reason='runtime_actor_instance_id_not_bound'.");
            }
        }

        private static PlayerActorIdentityRecord BuildParticipantActorIdentity(SessionActivityIdentity identity, PlayerActivityParticipantBinding participant)
        {
            PlayerActorId playerActorId = PlayerActorIdentityRecord.BuildPlayerActorId(identity, participant.ActorId);
            if (!identity.IsValid || !participant.IsValid || !playerActorId.IsValid)
            {
                throw new InvalidOperationException("Cannot build participant actor identity with invalid ActivityParticipantBinding.");
            }

            return new PlayerActorIdentityRecord(identity, participant, playerActorId);
        }

        private static SessionActivityActorMaterializationPlanEntry ResolveMaterializationPlanEntryForActivityParticipantOrFail(
            string activityId,
            PlayerActivityParticipantBinding participant,
            Dictionary<PlayerSessionParticipantId, SessionActivityActorMaterializationPlanEntry> materializationPlanByParticipantId,
            string operation)
        {
            if (!participant.IsValid || !participant.ParticipantId.IsValid)
            {
                throw new InvalidOperationException(
                    $"missing_activity_participant_materialization_plan: activityId='{activityId}' operation='{operation}' reason='participant_binding_invalid'.");
            }

            PlayerSessionParticipantId sessionParticipantId = participant.ParticipantId;
            if (materializationPlanByParticipantId != null &&
                materializationPlanByParticipantId.TryGetValue(sessionParticipantId, out SessionActivityActorMaterializationPlanEntry byParticipant) &&
                byParticipant.IsValid)
            {
                return byParticipant;
            }

            string actorDefinitionId = participant.ActorDefinitionId.IsValid ? Normalize(participant.ActorDefinitionId.Value) : string.Empty;
            string playerSlotId = participant.PlayerSlotId.IsValid ? Normalize(participant.PlayerSlotId.Value) : string.Empty;
            throw new InvalidOperationException(
                $"missing_activity_participant_materialization_plan: activityId='{activityId}' participantId='{sessionParticipantId}' playerSlotId='{playerSlotId}' actorDefinitionId='{actorDefinitionId}' operation='{operation}' resolutionKey='SessionParticipantId'.");
        }

        private static string ResolvePlacementIdForCommand(
            ActivityParticipantPlacementCommand placementCommand,
            SessionActivityActorMaterializationPlanEntry materializationPlanEntry)
        {
            string commandPlacementId = placementCommand.IsValid ? Normalize(placementCommand.PlacementRequirementId) : string.Empty;
            if (!string.IsNullOrWhiteSpace(commandPlacementId))
            {
                return commandPlacementId;
            }

            return Normalize(materializationPlanEntry.PlacementId);
        }

        private static string ResolvePlacementIdForResetCommand(
            ActivityParticipantResetCommand resetCommand,
            SessionActivityActorMaterializationPlanEntry materializationPlanEntry)
        {
            string commandPlacementId = Normalize(resetCommand.PlacementRequirementId);
            if (!string.IsNullOrWhiteSpace(commandPlacementId))
            {
                return commandPlacementId;
            }

            return Normalize(materializationPlanEntry.PlacementId);
        }

        private static void ResolvePlacementPlanForEntry(
            string activityId,
            SessionActivityIdentity identity,
            IActivityEntryPlacementMarkerLookup placementMarkerLookup,
            ActivityPlayerActorRegistry playerActorRegistry,
            SessionActivityActorMaterializationPlanEntry materializationPlanEntry,
            string placementId,
            string requirementId,
            PlayerActivityParticipantBinding participantBinding,
            string operation,
            out bool placementDeclared,
            out bool placementRequired,
            out bool placementOptional,
            out bool hasPlacement,
            out Vector3 placementPosition,
            out Vector3 placementEuler)
        {
            ResolvePlacementPlanFromDefinition(
                materializationPlanEntry,
                out placementDeclared,
                out placementRequired,
                out placementOptional,
                out hasPlacement,
                out placementPosition,
                out placementEuler);

            if (materializationPlanEntry.PlacementMode != ActorPlacementMode.SceneMarker)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(placementId))
            {
                if (placementRequired)
                {
                    throw new InvalidOperationException(
                        $"invalid_required_placement: activityId='{activityId}' entrySequence='{identity.EntrySequence}' requirementId='{requirementId}' participantId='{participantBinding.ParticipantId}' actorId='{participantBinding.ActorId}' operation='{operation}' reason='scene_marker_placement_id_missing'.");
                }

                return;
            }

            if (!placementMarkerLookup.TryResolvePlacementMarker(identity, placementId, out Vector3 markerPosition, out Vector3 markerEuler, out string resolutionReason))
            {
                if (placementRequired)
                {
                    throw new InvalidOperationException(
                        $"invalid_required_placement: activityId='{activityId}' entrySequence='{identity.EntrySequence}' requirementId='{requirementId}' participantId='{participantBinding.ParticipantId}' actorId='{participantBinding.ActorId}' placementId='{placementId}' operation='{operation}' reason='{resolutionReason}'.");
                }

                return;
            }

            hasPlacement = true;
            if (!playerActorRegistry.TryGetActiveHandleByParticipant(participantBinding.ParticipantId, out PlayerActorRuntimeHandle handle) ||
                !handle.IsValid ||
                handle.Instance == null)
            {
                throw new InvalidOperationException(
                    $"invalid_required_placement: activityId='{activityId}' entrySequence='{identity.EntrySequence}' requirementId='{requirementId}' participantId='{participantBinding.ParticipantId}' actorId='{participantBinding.ActorId}' placementId='{placementId}' operation='{operation}' reason='actor_handle_missing_for_placement_space_resolution'.");
            }

            Transform parent = handle.Instance.transform.parent;
            placementPosition = parent == null ? markerPosition : parent.InverseTransformPoint(markerPosition);
            Quaternion markerRotation = Quaternion.Euler(markerEuler);
            Quaternion localRotation = parent == null ? markerRotation : Quaternion.Inverse(parent.rotation) * markerRotation;
            placementEuler = localRotation.eulerAngles;
        }

        private static void ResolvePlacementPlanFromDefinition(
            SessionActivityActorMaterializationPlanEntry materializationPlanEntry,
            out bool placementDeclared,
            out bool placementRequired,
            out bool placementOptional,
            out bool hasPlacement,
            out Vector3 placementPosition,
            out Vector3 placementEuler)
        {
            placementDeclared = materializationPlanEntry.PlacementMode != ActorPlacementMode.None;
            hasPlacement = materializationPlanEntry.PlacementMode == ActorPlacementMode.FixedTransform;
            placementRequired =
                materializationPlanEntry.PlacementMode == ActorPlacementMode.FixedTransform ||
                materializationPlanEntry.PlacementMode == ActorPlacementMode.SceneMarker;
            placementOptional = placementDeclared && !placementRequired;
            placementPosition = hasPlacement ? materializationPlanEntry.LocalPosition : Vector3.zero;
            placementEuler = hasPlacement ? materializationPlanEntry.LocalEulerAngles : Vector3.zero;
        }

        private static IReadOnlyList<ActorResetGroup> MapResetGroupsOrFail(IReadOnlyList<ActivityStateResetGroup> groups)
        {
            if (groups == null || groups.Count == 0)
            {
                throw new InvalidOperationException("Participant reset command requires at least one reset group.");
            }

            List<ActorResetGroup> mapped = new(groups.Count);
            for (int index = 0; index < groups.Count; index++)
            {
                mapped.Add(MapResetGroupOrFail(groups[index]));
            }

            return mapped;
        }

        private static ActorResetGroup MapResetGroupOrFail(ActivityStateResetGroup group)
        {
            return group switch
            {
                ActivityStateResetGroup.Placement => ActorResetGroup.Placement,
                ActivityStateResetGroup.ActivityParticipation => ActorResetGroup.ActivityParticipation,
                ActivityStateResetGroup.RuntimeTransient => ActorResetGroup.MovementTransient,
                _ => throw new InvalidOperationException($"Unsupported participant reset group mapping '{group}'."),
            };
        }

        private static PlayerActorIdentityRecord EnsureResolvedActorIdentityForActivityParticipantOrFail(
            PlayerActivityParticipantBinding participantBinding,
            Dictionary<PlayerSessionParticipantId, PlayerActorIdentityRecord> ensuredActorsByActivityParticipant,
            string activityId,
            string operation)
        {
            if (!participantBinding.IsValid || !participantBinding.ParticipantId.IsValid)
            {
                throw new InvalidOperationException(
                    $"Activity participant binding is invalid for operation='{operation}' activityId='{activityId}'.");
            }

            PlayerSessionParticipantId participantId = participantBinding.ParticipantId;
            if (ensuredActorsByActivityParticipant == null || !ensuredActorsByActivityParticipant.TryGetValue(participantId, out PlayerActorIdentityRecord identity) || !identity.IsValid)
            {
                throw new InvalidOperationException(
                    $"Activity participant '{participantId}' is not available for operation='{operation}' activityId='{activityId}' playerSlotId='{participantBinding.PlayerSlotId}' actorDefinitionId='{participantBinding.ActorDefinitionId}' actorId='{participantBinding.ActorId}'.");
            }

            return identity;
        }

        private static void BeginPlayerActorActivityScope(
            ActivityPlayerActorRegistry playerActorRegistry,
            SessionActivityIdentity identity)
        {
            if (playerActorRegistry == null)
            {
                throw new ArgumentNullException(nameof(playerActorRegistry));
            }

            if (!identity.IsValid)
            {
                throw new InvalidOperationException("ActivityPlayerActorRegistry requires valid activity scope identity.");
            }

            IReadOnlyList<PlayerActorRuntimeHandle> activeHandles = playerActorRegistry.GetIndexedActiveHandles();
            playerActorRegistry.SetActiveScopeIdentity(identity);
            playerActorRegistry.ClearActiveIndexes();
            for (int index = 0; index < activeHandles.Count; index++)
            {
                PlayerActorRuntimeHandle handle = activeHandles[index];
                if (ActivityActorScopeCompatibilityPolicy.ShouldRetainActiveHandleOnActivityScopeBegin(
                        identity,
                        handle,
                        nameof(SessionActivityPipeline),
                        "BeginPlayerActorActivityScope"))
                {
                    PlayerActorRuntimeHandle scopedHandle = RebindRetainedPlayerActorHandleForCurrentActivityOrFail(
                        identity,
                        handle.ParticipantBinding,
                        handle,
                        "activity_scope_begin");
                    playerActorRegistry.IndexActiveHandle(scopedHandle);
                }
            }
        }

        private static void TryRegisterRetainedPlayerActorHandle(
            ActivityPlayerActorRegistry playerActorRegistry,
            SessionActivityIdentity identity,
            PlayerActorRuntimeHandle handle,
            string activityId,
            string source,
            string reason,
            string eventName)
        {
            if (playerActorRegistry == null)
            {
                throw new ArgumentNullException(nameof(playerActorRegistry));
            }

            if (!handle.IsValid)
            {
                throw new InvalidOperationException("Cannot register invalid retained player actor handle.");
            }

            PlayerActorIdentityRecord actorIdentity = handle.ActorIdentity;
            if (playerActorRegistry.TryGetIndexedActiveHandleByParticipant(actorIdentity.ParticipantId, out PlayerActorRuntimeHandle activeHandle))
            {
                bool isCurrentActivityScope = activeHandle.ActorIdentity.Identity.CycleKey.Equals(identity.CycleKey);
                if (isCurrentActivityScope && activeHandle.Instance == handle.Instance)
                {
                    DebugUtility.Log(
                        typeof(ActivityEntryParticipantBindingStage),
                        $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='{eventName}RegistryRegistrationSkipped' activityId='{activityId}' entrySequence='{identity.EntrySequence}' participantId='{actorIdentity.ParticipantId}' activeEntrySequence='{activeHandle.ActorIdentity.Identity.EntrySequence}' playerSlotId='{actorIdentity.PlayerSlotId}' playerActorId='{actorIdentity.PlayerActorId}' actorId='{actorIdentity.ActorId}' actorScope='{actorIdentity.ParticipantBinding.ActorScope}' skipReason='already_indexed_retained_player_actor' source='{source}' reason='{reason}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (!isCurrentActivityScope)
                {
                    DebugUtility.Log(
                        typeof(ActivityEntryParticipantBindingStage),
                        $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='retained_player_actor_promoted_to_current_activity_scope' activityId='{activityId}' activeActivityId='{identity.ActivityId}' activeEntrySequence='{identity.EntrySequence}' previousActivityId='{activeHandle.ActorIdentity.Identity.ActivityId}' previousEntrySequence='{activeHandle.ActorIdentity.Identity.EntrySequence}' participantId='{actorIdentity.ParticipantId}' playerSlotId='{actorIdentity.PlayerSlotId}' playerActorId='{actorIdentity.PlayerActorId}' actorId='{actorIdentity.ActorId}' actorScope='{actorIdentity.ParticipantBinding.ActorScope}' source='{source}' reason='{reason}'.",
                        DebugUtility.Colors.Info);
                }

                DebugUtility.Log(
                    typeof(ActivityEntryParticipantBindingStage),
                    $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='{eventName}RegistryRegistrationRetained' activityId='{activityId}' entrySequence='{identity.EntrySequence}' participantId='{actorIdentity.ParticipantId}' playerSlotId='{actorIdentity.PlayerSlotId}' playerActorId='{actorIdentity.PlayerActorId}' actorId='{actorIdentity.ActorId}' actorScope='{actorIdentity.ParticipantBinding.ActorScope}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
            }

            playerActorRegistry.IndexActiveHandle(handle);
            if (ActivityActorScopeCompatibilityPolicy.ShouldTrackInRouteIndex(
                    identity,
                    handle,
                    nameof(SessionActivityPipeline),
                    "RegisterRetainedPlayerActorParticipation"))
            {
                playerActorRegistry.IndexRouteScopedHandle(handle);
            }
        }

        private static ActorResetActorRef BuildActorResetActorRef(
            SessionActivityIdentity identity,
            PlayerActorIdentityRecord actorIdentity,
            string activityId,
            ActivityPlayerActorRegistry playerActorRegistry,
            string operation)
        {
            if (!actorIdentity.IsValid)
            {
                throw new InvalidOperationException("Cannot build ActorResetActorRef from invalid PlayerActorIdentityRecord.");
            }

            if (!TryResolvePlayerActorHandleForParticipant(playerActorRegistry, actorIdentity.ParticipantId, out PlayerActorRuntimeHandle handle) ||
                !handle.IsValid)
            {
                throw new InvalidOperationException(
                    $"Actor reset {operation} requires active player actor instance. activityId='{activityId}' playerSlotId='{actorIdentity.PlayerSlotId}' playerActorId='{actorIdentity.PlayerActorId}'.");
            }

            GameObject actorInstance = handle.Instance;
            PlayerActorIdentityRecord observedIdentity = handle.ActorIdentity;
            Actor runtimeActor = actorInstance.GetComponent<Actor>();
            if (runtimeActor == null || !runtimeActor.RuntimeActorInstanceId.IsValid || string.IsNullOrWhiteSpace(runtimeActor.ActorId))
            {
                throw new InvalidOperationException(
                    $"Actor reset {operation} requires valid runtime actor identity. activityId='{activityId}' playerSlotId='{actorIdentity.PlayerSlotId}' playerActorId='{actorIdentity.PlayerActorId}'.");
            }

            return new ActorResetActorRef(
                identity,
                new ActorId(runtimeActor.ActorId),
                runtimeActor.RuntimeActorInstanceId,
                ActorKind.Player,
                observedIdentity.PlayerActorId,
                observedIdentity.PlayerSlotId);
        }

        private static ActorResetActorRef BuildActorResetActorRef(
            SessionActivityIdentity identity,
            PlayerActorIdentityRecord actorIdentity,
            PlayerActivityParticipantBinding participantBinding,
            string activityId,
            ActivityPlayerActorRegistry playerActorRegistry,
            string operation)
        {
            if (!actorIdentity.IsValid)
            {
                throw new InvalidOperationException("Cannot build ActorResetActorRef from invalid PlayerActorIdentityRecord.");
            }

            if (!participantBinding.IsValid)
            {
                throw new InvalidOperationException("Cannot build ActorResetActorRef from invalid ActivityParticipantBinding.");
            }

            if (!TryResolvePlayerActorHandleForParticipant(playerActorRegistry, actorIdentity.ParticipantId, out PlayerActorRuntimeHandle handle) ||
                !handle.IsValid)
            {
                throw new InvalidOperationException(
                    $"Actor reset {operation} requires active player actor instance. activityId='{activityId}' participantId='{participantBinding.ParticipantId}' playerSlotId='{participantBinding.PlayerSlotId}' actorDefinitionId='{participantBinding.ActorDefinitionId}' actorId='{participantBinding.ActorId}' playerActorId='{actorIdentity.PlayerActorId}'.");
            }

            GameObject actorInstance = handle.Instance;
            PlayerActorIdentityRecord observedIdentity = handle.ActorIdentity;
            Actor runtimeActor = actorInstance.GetComponent<Actor>();
            if (runtimeActor == null || !runtimeActor.RuntimeActorInstanceId.IsValid || string.IsNullOrWhiteSpace(runtimeActor.ActorId))
            {
                throw new InvalidOperationException(
                    $"Actor reset {operation} requires valid runtime actor identity. activityId='{activityId}' participantId='{participantBinding.ParticipantId}' playerSlotId='{participantBinding.PlayerSlotId}' actorDefinitionId='{participantBinding.ActorDefinitionId}' actorId='{participantBinding.ActorId}' playerActorId='{actorIdentity.PlayerActorId}'.");
            }

            return new ActorResetActorRef(
                identity,
                new ActorId(runtimeActor.ActorId),
                runtimeActor.RuntimeActorInstanceId,
                ActorKind.Player,
                observedIdentity.PlayerActorId,
                observedIdentity.PlayerSlotId);
        }

        private static void ValidateRequiredResetGroupsOrFail(
            ActivityParticipantResetCommand resetCommand,
            ActorResetResult record)
        {
            if (record.SkippedGroups == null || record.SkippedGroups.Count == 0)
            {
                return;
            }

            if (record.SkippedGroupReasons == null || record.SkippedGroupReasons.Count == 0)
            {
                throw new InvalidOperationException(
                    $"required_reset_group_failed: requirementId='{resetCommand.RequirementId}' participantId='{resetCommand.ParticipantBinding.ParticipantId}' playerSlotId='{resetCommand.ParticipantBinding.PlayerSlotId}' skippedGroups='{record.SkippedGroups.Count}' reason='missing_skip_reason'.");
            }

            for (int index = 0; index < record.SkippedGroupReasons.Count; index++)
            {
                ActorResetSkippedGroupReason reason = record.SkippedGroupReasons[index];
                if (!reason.IsValid)
                {
                    continue;
                }

                if (string.Equals(reason.ReasonCode, "optional_placement_missing", StringComparison.Ordinal))
                {
                    continue;
                }

                throw new InvalidOperationException(
                    $"required_reset_group_failed: requirementId='{resetCommand.RequirementId}' participantId='{resetCommand.ParticipantBinding.ParticipantId}' playerSlotId='{resetCommand.ParticipantBinding.PlayerSlotId}' group='{reason.Group}' reason='{reason.ReasonCode}'.");
            }
        }

        private static void StoreActivityParticipationBoundary(
            PlayerActivityParticipationContext context,
            ActivityParticipationRuntimeState activityParticipationRuntimeState,
            ActivityActorExitRuntimeState activityActorExitRuntimeState,
            PlayerSessionParticipationContext sessionParticipationContext)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (activityParticipationRuntimeState == null)
            {
                throw new ArgumentNullException(nameof(activityParticipationRuntimeState));
            }

            if (activityActorExitRuntimeState == null)
            {
                throw new ArgumentNullException(nameof(activityActorExitRuntimeState));
            }

            activityParticipationRuntimeState.StoreCurrentParticipationContext(context);
            activityActorExitRuntimeState.StoreActivityParticipationExitCorrelation(context);

            string sessionParticipationState = sessionParticipationContext != null && sessionParticipationContext.IsValid
                ? "present"
                : "absent";
            int sessionRevision = sessionParticipationContext?.Revision ?? 0;
            int sessionParticipants = sessionParticipationContext?.ParticipantCount ?? 0;
            int activityParticipants = context.ParticipantCount;
            string participationState = context.HasParticipants ? "present" : "empty";
            string bindings = FormatActivityParticipantBindings(context.Participants ?? Array.Empty<PlayerActivityParticipantBinding>());

            DebugUtility.Log(
                typeof(ActivityEntryParticipantBindingStage),
                $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipationContextPrepared' pipelineId='{context.SessionActivityIdentity.PipelineId}' sessionStateId='{context.SessionActivityIdentity.SessionId}' activityId='{context.SessionActivityIdentity.ActivityId}' entrySequence='{context.SessionActivityIdentity.EntrySequence}' stage='{context.SessionActivityIdentity.Stage}' sessionParticipationContext='{sessionParticipationState}' sessionParticipationRevision='{sessionRevision}' sessionParticipants='{sessionParticipants}' activityParticipationContext='{(context.IsValid ? "present" : "invalid")}' activityParticipants='{activityParticipants}' participationState='{participationState}' currentStateOwner='ActivityParticipationRuntimeState' exitCorrelationOwner='ActivityActorExitRuntimeState' materializationOwner='ActivityEntryPipeline' inputBindingOwner='ActivityEntryPipeline' bindings=\"{bindings}\" source='{context.Source}' reason='{context.Reason}'.",
                context.IsValid ? DebugUtility.Colors.Info : DebugUtility.Colors.Warning);
        }

        private static PlayerSessionParticipationContext ResolveSessionParticipationContextOrFail(
            ActivityEntryParticipantBindingCommand command,
            ActivityParticipationRuntimeState activityParticipationRuntimeState,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (activityParticipationRuntimeState == null)
            {
                throw new ArgumentNullException(nameof(activityParticipationRuntimeState));
            }

            PlayerSessionParticipationContext context = activityParticipationRuntimeState.CurrentSessionParticipationContext;
            string sessionStateId = command.Identity.SessionId;
            int entrySequence = command.Identity.EntrySequence;

            if (context == null || !context.IsValid)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActivityParticipantBindingFailed);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityParticipantBindingFailed);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' participant binding failed missing SessionParticipationContext sessionStateId='{sessionStateId}'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_participant_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' participant binding failed missing SessionParticipationContext.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionActivityPipeline][ParticipantBinding] Missing valid SessionParticipationContext activityId='{command.ActivityId}' entrySequence='{entrySequence}' sessionStateId='{sessionStateId}'.");
            }

            if (context.HasSessionId && !string.Equals(context.SessionId, sessionStateId, StringComparison.Ordinal))
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActivityParticipantBindingFailed);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityParticipantBindingFailed);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' participant binding failed foreign SessionParticipationContext sessionStateId='{sessionStateId}' contextSessionId='{context.SessionId}'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_participant_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' participant binding failed foreign SessionParticipationContext.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionActivityPipeline][ParticipantBinding] Foreign SessionParticipationContext activityId='{command.ActivityId}' entrySequence='{entrySequence}' sessionStateId='{sessionStateId}' contextSessionId='{context.SessionId}'.");
            }

            return context;
        }

        private static string FormatActivityParticipantBindings(IReadOnlyList<PlayerActivityParticipantBinding> participants)
        {
            if (participants == null || participants.Count == 0)
            {
                return "<none>";
            }

            List<string> segments = new(participants.Count);
            for (int index = 0; index < participants.Count; index++)
            {
                PlayerActivityParticipantBinding participant = participants[index];
                segments.Add($"requirementId='{participant.RequirementId}' participantId='{participant.ParticipantId}' role='{participant.Role}' playerSlotId='{participant.PlayerSlotId}' actorDefinitionId='{participant.ActorDefinitionId}' actorId='{participant.ActorId}' materializationPolicy='{participant.MaterializationPolicy}' requiresPlayerInput='{participant.RequiresPlayerInput}'");
            }

            return string.Join(" | ", segments);
        }

        private static string FormatActivityStateResetGroups(IReadOnlyList<ActivityStateResetGroup> resetGroups)
        {
            if (resetGroups == null || resetGroups.Count == 0)
            {
                return "<none>";
            }

            return string.Join(",", resetGroups);
        }

        private static bool TryResolvePlayerActorHandleForParticipant(
            ActivityPlayerActorRegistry playerActorRegistry,
            PlayerSessionParticipantId participantId,
            out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (playerActorRegistry == null || !participantId.IsValid)
            {
                return false;
            }

            if (playerActorRegistry.TryGetActiveHandleByParticipant(participantId, out handle))
            {
                return true;
            }

            return playerActorRegistry.TryGetRouteScopedHandleByParticipant(participantId, out handle);
        }

        private static string FormatSessionParticipantId(PlayerSessionParticipantId participantId)
        {
            return participantId.IsValid ? participantId.ToString() : "<none>";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
