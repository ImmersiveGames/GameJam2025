using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.Actors.Capabilities.Reset
{
    public sealed class ActorResetAdapter : IActorResetAdapter
    {
        public ActorResetAdapter() { }

        public IReadOnlyList<ActorResetResult> Execute(
            ActivityParticipantResetCommand command,
            SessionActivityIdentity activeIdentity)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityParticipantResetCommand is invalid.");
            }

            if (!activeIdentity.IsValid)
            {
                throw new InvalidOperationException("Active identity is invalid for participant actor reset.");
            }

            if (!IsSameActivityCycle(command.Identity, activeIdentity))
            {
                throw new InvalidOperationException("stale_or_foreign_participant_actor_reset_command: command identity does not match active identity.");
            }

            var actorId = command.ActorIdentity.ActorId;
            var actorKind = ActorKind.Player;
            int appliedReferenceCount = 0;
            int skippedReferenceCount = 0;
            List<ActorResetSkippedReferenceReason> skippedReferenceReasons = new();
            ActorInstanceRuntimeId expectedActorInstanceRuntimeId = default;
            ActorInstanceRuntimeId actorInstanceRuntimeId = default;
            bool actorCaptured = false;

            for (int referenceIndex = 0; referenceIndex < command.ResetReferences.Count; referenceIndex++)
            {
                var resetReference = command.ResetReferences[referenceIndex];
                if (resetReference == null || !resetReference.IsValid)
                {
                    throw new InvalidOperationException($"Invalid actor reset inventory reference at index '{referenceIndex}' requirementId='{command.RequirementId}'.");
                }

                if (!actorCaptured)
                {
                    expectedActorInstanceRuntimeId = resetReference.ActorInstanceRuntimeId;
                    actorInstanceRuntimeId = resetReference.ActorInstanceRuntimeId;
                    actorCaptured = true;
                }

                if (resetReference.ActorId != command.ActorIdentity.ActorId)
                {
                    throw new InvalidOperationException(
                        $"stale_or_foreign_actor_reset_inventory_reference: capabilityId='{resetReference.CapabilityId}' actorId='{resetReference.ActorId}' expectedActorId='{command.ActorIdentity.ActorId}'.");
                }

                if (resetReference.ActorInstanceRuntimeId != expectedActorInstanceRuntimeId)
                {
                    throw new InvalidOperationException(
                        $"stale_or_foreign_actor_reset_inventory_reference: capabilityId='{resetReference.CapabilityId}' actorId='{resetReference.ActorId}' actorInstanceRuntimeId='{resetReference.ActorInstanceRuntimeId}' expectedActorInstanceRuntimeId='{expectedActorInstanceRuntimeId}'.");
                }

                ActorResetContext context = new(
                    activeIdentity,
                    actorId,
                    actorInstanceRuntimeId,
                    actorKind,
                    command.ResetIntent,
                    command.StateProfileKind,
                    command.PlacementRequirementId,
                    command.HasPlacement,
                    command.PlacementRequired,
                    command.PlacementOptional,
                    command.PlacementDeclared,
                    command.PlacementPosition,
                    command.PlacementEulerAngles,
                    command.Source,
                    command.Reason);

                if (resetReference.Endpoint is IActorPlacementResetEndpoint)
                {
                    if (command is { PlacementRequired: true, HasPlacement: false } && string.IsNullOrWhiteSpace(command.PlacementRequirementId))
                    {
                        throw new InvalidOperationException($"invalid_required_placement: actorId='{command.ActorIdentity.ActorId}'.");
                    }

                    if (!command.PlacementDeclared)
                    {
                        skippedReferenceCount += 1;
                        skippedReferenceReasons.Add(new ActorResetSkippedReferenceReason(resetReference.CapabilityId, "no_placement_declared"));

                        continue;
                    }

                    if (command is { PlacementOptional: true, HasPlacement: false })
                    {
                        skippedReferenceCount += 1;
                        skippedReferenceReasons.Add(new ActorResetSkippedReferenceReason(resetReference.CapabilityId, "optional_placement_missing"));

                        continue;
                    }

                    if (command is { HasPlacement: true, PlacementRequired: false })
                    {
                        skippedReferenceCount += 1;
                        skippedReferenceReasons.Add(new ActorResetSkippedReferenceReason(resetReference.CapabilityId, "placement_not_required"));

                        continue;
                    }
                }

                string resetHandler = ApplyResetByIntent(resetReference.Endpoint, command.ResetIntent, context);
                DebugUtility.LogVerbose(typeof(ActorResetAdapter),
                    $"event='ActorResetEndpointAppliedFromInventory' actorId='{resetReference.ActorId}' actorInstanceRuntimeId='{actorInstanceRuntimeId}' providerType='{resetReference.ProviderType}' resetIntent='{command.ResetIntent}' resetStateProfile='{command.StateProfileKind}' resetHandler='{resetHandler}' resetDescriptor='endpoint_inventory' runtimeBoundaryEligibility='{ActivityResetBoundaryEligibilityFormatter.Format(resetReference.ResetBoundaryEligibility)}' executionMode='intent_handler_per_reference' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                appliedReferenceCount += 1;
            }

            if (!actorCaptured)
            {
                throw new InvalidOperationException("ActivityParticipantResetCommand did not resolve any actor reset reference.");
            }

            return new[]
            {
                new ActorResetResult(actorId, actorInstanceRuntimeId, actorKind, appliedReferenceCount, skippedReferenceCount, skippedReferenceReasons)
            };
        }

        private static string ApplyResetByIntent(
            IActorResetEndpoint endpoint,
            ActivityResetIntent resetIntent,
            ActorResetContext context)
        {
            if (resetIntent == ActivityResetIntent.EntryInitialize && endpoint is IActorEntryInitializeResetEndpoint entryInitialize)
            {
                entryInitialize.ApplyEntryInitializeReset(context);
                return nameof(IActorEntryInitializeResetEndpoint);
            }

            if (resetIntent == ActivityResetIntent.RuntimeLocalReset && endpoint is IActorRuntimeLocalResetEndpoint runtimeLocal)
            {
                runtimeLocal.ApplyRuntimeLocalReset(context);
                return nameof(IActorRuntimeLocalResetEndpoint);
            }

            if (resetIntent == ActivityResetIntent.RuntimeActivityReset && endpoint is IActorRuntimeActivityResetEndpoint runtimeActivity)
            {
                runtimeActivity.ApplyRuntimeActivityReset(context);
                return nameof(IActorRuntimeActivityResetEndpoint);
            }

            if (resetIntent == ActivityResetIntent.RuntimeActivityTransitionReset && endpoint is IActorRuntimeActivityTransitionResetEndpoint runtimeActivityTransition)
            {
                runtimeActivityTransition.ApplyRuntimeActivityTransitionReset(context);
                return nameof(IActorRuntimeActivityTransitionResetEndpoint);
            }

            if (resetIntent == ActivityResetIntent.RuntimeRouteTransitionReset && endpoint is IActorRuntimeRouteTransitionResetEndpoint runtimeRouteTransition)
            {
                runtimeRouteTransition.ApplyRuntimeRouteTransitionReset(context);
                return nameof(IActorRuntimeRouteTransitionResetEndpoint);
            }

            string endpointType = endpoint == null ? "<null>" : endpoint.GetType().Name;
            throw new InvalidOperationException(
                $"actor_reset_intent_handler_missing: endpointType='{endpointType}' resetIntent='{resetIntent}' resetStateProfile='{context.StateProfileKind}' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}'.");
        }

        private static bool IsSameActivityCycle(SessionActivityIdentity left, SessionActivityIdentity right)
        {
            return left.IsValid &&
                right.IsValid &&
                string.Equals(left.PipelineId, right.PipelineId, StringComparison.Ordinal) &&
                string.Equals(left.SessionId, right.SessionId, StringComparison.Ordinal) &&
                string.Equals(left.ActivityId, right.ActivityId, StringComparison.Ordinal) &&
                left.ActivityOrdinal == right.ActivityOrdinal &&
                left.EntrySequence == right.EntrySequence;
        }

    }
}
