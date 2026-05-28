using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityPermissionScanner : IActivityCapabilityScanner
    {
        private const string ModuleId = "SessionActivity.PermissionMovement";

        public string ScannerId => "activity_capability_permission_scanner.v1";
        public int Order => 300;

        public ActivityCapabilityScanResult Scan(ActivityCapabilityScanContext context)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("Activity capability scan context is invalid.");
            }

            ActivityCapabilityInventoryId inventoryId = context.InventoryId;
            List<ActivityCapabilityOwnerDescriptor> owners = new();
            List<ActivityCapabilityDescriptor> capabilities = new();
            List<IActivityCapabilityRuntimeReference> runtimeReferences = new();
            HashSet<string> ownerKeys = new(StringComparer.Ordinal);
            HashSet<string> capabilityKeys = new(StringComparer.Ordinal);

            for (int index = 0; index < context.ActorTargets.Count; index++)
            {
                ActorScanTarget target = context.ActorTargets[index];
                if (!target.IsValid)
                {
                    continue;
                }

                ActorCapabilitySurface surface = target.CapabilitySurface;
                if (surface == null)
                {
                    throw new InvalidOperationException(
                        $"ActivityCapabilityPermissionScanner requires ActorCapabilitySurface actorId='{target.ActorId}' actorInstanceId='{target.ActorInstanceId.Value}'.");
                }

                IActorMovementEndpoint movementEndpoint = surface.ActorMovementEndpoint;
                if (movementEndpoint == null)
                {
                    continue;
                }

                if (!TryResolvePlayerIdentity(target, out string playerActorId, out string playerSlotId))
                {
                    Component movementComponent = movementEndpoint as Component;
                    string unresolvedComponentPath = movementComponent != null
                        ? ActivityCapabilityTransformPathUtility.BuildTransformPath(movementComponent.transform)
                        : string.Empty;
                    Debug.LogWarning(
                        $"[OBS][ActivityCapabilityPermissionScanner] event='PermissionTargetIdentityUnresolved' reason='player_identity_missing' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceId.Value}' capabilityKind='{ActivityCapabilityKind.PermissionTarget}' componentPath='{unresolvedComponentPath}' source='{context.Source}' activityId='{context.Identity.ActivityId}' entrySequence='{context.Identity.EntrySequence}'.");
                    continue;
                }

                string ownerPath = ActivityCapabilityTransformPathUtility.BuildTransformPath(target.ActorRoot.transform);
                string ownerId = ActivityCapabilityInventoryId.DeriveOwnerId(
                    inventoryId,
                    ActivityCapabilityOwnerKind.PlayerActor,
                    ownerPath,
                    target.ActorId);

                if (ownerKeys.Add(ownerId))
                {
                    owners.Add(new ActivityCapabilityOwnerDescriptor(
                        ActivityCapabilityOwnerKind.PlayerActor,
                        ownerId,
                        ownerPath,
                        target.SourceSceneName,
                        target.Source,
                        context.Source));
                }

                Component endpointComponent = movementEndpoint as Component;
                string componentPath = endpointComponent != null
                    ? ActivityCapabilityTransformPathUtility.BuildTransformPath(endpointComponent.transform)
                    : string.Empty;
                string componentType = movementEndpoint.GetType().FullName ?? movementEndpoint.GetType().Name;
                string capabilityId = ActivityCapabilityInventoryId.DeriveCapabilityId(
                    inventoryId,
                    ownerId,
                    ActivityCapabilityKind.PermissionTarget,
                    ModuleId,
                    componentPath);

                if (!capabilityKeys.Add(capabilityId))
                {
                    continue;
                }

                string permissionToken = ActivityCapabilityPermissionIds.ActivityGameplayControl;
                ActivityCapabilityPermissionReceiverIdentity receiverIdentity = new(
                    context.Identity.PipelineId,
                    context.Identity.SessionId,
                    context.Identity.ActivityId,
                    context.Identity.EntrySequence,
                    playerActorId,
                    playerSlotId);
                string receiverId = PlayerMovementPermissionReceiver.CreateReceiverId(
                    receiverIdentity);

                IActorPermissionReceiver receiver = surface.ActorPermissionReceiver;
                if (receiver == null)
                {
                    receiver = new PlayerMovementPermissionReceiver(
                        movementEndpoint,
                        receiverId,
                        playerActorId,
                        playerSlotId);
                }

                capabilities.Add(new ActivityCapabilityDescriptor(
                    capabilityId,
                    ActivityCapabilityKind.PermissionTarget,
                    ModuleId,
                    ownerId,
                    componentPath,
                    componentType,
                    required: true,
                    priority: 100,
                    policyMetadata: new[]
                    {
                        new ActivityCapabilityPolicyEntry("permissionId", permissionToken),
                        new ActivityCapabilityPolicyEntry("playerActorId", playerActorId),
                        new ActivityCapabilityPolicyEntry("playerSlotId", playerSlotId),
                        new ActivityCapabilityPolicyEntry("actorRole", target.ActorRole.ToString()),
                    },
                    source: context.Source));

                runtimeReferences.Add(new ActivityCapabilityPermissionReceiverReference(
                    capabilityId,
                    ownerId,
                    playerActorId,
                    componentPath,
                    permissionToken,
                    receiverIdentity,
                    receiver));
            }

            return new ActivityCapabilityScanResult(ScannerId, owners, capabilities, runtimeReferences, context.Source, context.Reason);
        }

        private static bool TryResolvePlayerIdentity(ActorScanTarget target, out string playerActorId, out string playerSlotId)
        {
            playerActorId = string.Empty;
            playerSlotId = string.Empty;

            PlayerActorIdentity identity = target.ActorRoot != null ? target.ActorRoot.GetComponent<PlayerActorIdentity>() : null;
            if (identity == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(identity.PlayerActorId))
            {
                playerActorId = identity.PlayerActorId.Trim();
            }

            if (!string.IsNullOrWhiteSpace(identity.PlayerSlotId))
            {
                playerSlotId = identity.PlayerSlotId.Trim();
            }

            return !string.IsNullOrWhiteSpace(playerActorId) &&
                   !string.IsNullOrWhiteSpace(playerSlotId);
        }
    }

}
