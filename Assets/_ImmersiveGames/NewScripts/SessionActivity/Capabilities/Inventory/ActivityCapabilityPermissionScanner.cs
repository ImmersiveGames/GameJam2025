using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.ObjectEmission.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityPermissionScanner : IActivityCapabilityScanner
    {
        private const string ModuleId = "SessionActivity.PermissionTarget";
        private readonly IPlayerActorCapabilityIdentityResolver _identityResolver;

        public ActivityCapabilityPermissionScanner(IPlayerActorCapabilityIdentityResolver identityResolver)
        {
            _identityResolver = identityResolver ?? throw new InvalidOperationException("ActivityCapabilityPermissionScanner requires non-null identity resolver.");
        }

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
            List<ActivityPermissionReceiverContribution> contributions = new();
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
                IActorObjectEmitterEndpoint objectEmitterEndpoint = surface.ActorObjectEmitterEndpoint;
                if (movementEndpoint == null && objectEmitterEndpoint == null)
                {
                    continue;
                }

                if (!_identityResolver.TryResolve(target, out PlayerActorCapabilityIdentity playerIdentity))
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

                ActivityCapabilityPermissionReceiverIdentity receiverIdentity = new(
                    context.Identity.PipelineId,
                    context.Identity.SessionId,
                    context.Identity.ActivityId,
                    context.Identity.EntrySequence,
                    playerIdentity.ActorId,
                    playerIdentity.ActorInstanceRuntimeId,
                    playerIdentity.PlayerActorId,
                    playerIdentity.PlayerSlotId);

                if (movementEndpoint != null)
                {
                    Component endpointComponent = movementEndpoint as Component;
                    string componentPath = endpointComponent != null
                        ? ActivityCapabilityTransformPathUtility.BuildTransformPath(endpointComponent.transform)
                        : string.Empty;
                    string componentType = movementEndpoint.GetType().FullName ?? movementEndpoint.GetType().Name;
                    string capabilityPath = BuildCapabilityPath(componentPath, receiverIdentity, receiverKind: "movement");
                    string capabilityId = ActivityCapabilityInventoryId.DeriveCapabilityId(
                        inventoryId,
                        ownerId,
                        ActivityCapabilityKind.PermissionTarget,
                        ModuleId,
                        capabilityPath);

                    if (capabilityKeys.Add(capabilityId))
                    {
                        string permissionToken = ActivityCapabilityPermissionIds.ActivityGameplayControl;
                        string receiverId = PlayerMovementPermissionReceiver.CreateReceiverId(
                            receiverIdentity);

                        IActivityPermissionReceiverProvider receiverProvider = surface.ActorPermissionReceiver != null
                            ? new PlayerMovementPermissionReceiverProvider(
                                surface.ActorPermissionReceiver,
                                receiverId,
                                playerIdentity.ActorId,
                                playerIdentity.ActorInstanceRuntimeId,
                                playerIdentity.PlayerActorId,
                                playerIdentity.PlayerSlotId)
                            : new PlayerMovementPermissionReceiverProvider(
                                movementEndpoint,
                                receiverId,
                                playerIdentity.ActorId,
                                playerIdentity.ActorInstanceRuntimeId,
                                playerIdentity.PlayerActorId,
                                playerIdentity.PlayerSlotId);

                        capabilities.Add(new ActivityCapabilityDescriptor(
                            capabilityId,
                            ActivityCapabilityKind.PermissionTarget,
                            ModuleId,
                            ownerId,
                            componentPath,
                            componentType,
                            required: false,
                            priority: 100,
                            policyMetadata: new[]
                            {
                                new ActivityCapabilityPolicyEntry("permissionId", permissionToken),
                                new ActivityCapabilityPolicyEntry("actorId", playerIdentity.ActorId.Value),
                                new ActivityCapabilityPolicyEntry("actorInstanceRuntimeId", playerIdentity.ActorInstanceRuntimeId.Value),
                                new ActivityCapabilityPolicyEntry("playerActorId", playerIdentity.PlayerActorId.Value),
                                new ActivityCapabilityPolicyEntry("playerSlotId", playerIdentity.PlayerSlotId.Value),
                                new ActivityCapabilityPolicyEntry("actorRole", target.ActorRole.ToString()),
                            },
                            source: context.Source));

                        contributions.Add(new ActivityPermissionReceiverContribution(
                            context.Identity,
                            capabilityId,
                            ownerId,
                            playerIdentity.ActorId,
                            playerIdentity.ActorInstanceRuntimeId,
                            playerIdentity.PlayerActorId,
                            playerIdentity.PlayerSlotId,
                            ActivityCapabilityPermissionId.ActivityGameplayControl,
                            receiverIdentity,
                            receiverId,
                            componentPath,
                            receiverProvider,
                            context.Source,
                            context.Reason));
                    }
                }

                if (objectEmitterEndpoint != null)
                {
                    Component objectEmissionComponent = objectEmitterEndpoint as Component;
                    string objectEmissionComponentPath = objectEmissionComponent != null
                        ? ActivityCapabilityTransformPathUtility.BuildTransformPath(objectEmissionComponent.transform)
                        : string.Empty;
                    string objectEmissionCapabilityPath = BuildCapabilityPath(objectEmissionComponentPath, receiverIdentity, receiverKind: "object_emission");
                    string objectEmissionCapabilityId = ActivityCapabilityInventoryId.DeriveCapabilityId(
                        inventoryId,
                        ownerId,
                        ActivityCapabilityKind.PermissionTarget,
                        ModuleId,
                        objectEmissionCapabilityPath);

                    string objectEmissionReceiverId = ActorObjectEmissionPermissionReceiver.CreateReceiverId(receiverIdentity);
                    IActivityPermissionReceiverProvider objectEmissionReceiverProvider =
                        new ActorObjectEmissionPermissionReceiverProvider(
                            objectEmitterEndpoint,
                            objectEmissionReceiverId,
                            playerIdentity.ActorId,
                            playerIdentity.ActorInstanceRuntimeId,
                            playerIdentity.PlayerActorId,
                            playerIdentity.PlayerSlotId);

                    if (capabilityKeys.Add(objectEmissionCapabilityId))
                    {
                        contributions.Add(new ActivityPermissionReceiverContribution(
                            context.Identity,
                            objectEmissionCapabilityId,
                            ownerId,
                            playerIdentity.ActorId,
                            playerIdentity.ActorInstanceRuntimeId,
                            playerIdentity.PlayerActorId,
                            playerIdentity.PlayerSlotId,
                            ActivityCapabilityPermissionId.ActivityGameplayControl,
                            receiverIdentity,
                            objectEmissionReceiverId,
                            objectEmissionComponentPath,
                            objectEmissionReceiverProvider,
                            context.Source,
                            context.Reason));
                    }
                }
            }

            return new ActivityCapabilityScanResult(
                ScannerId,
                owners,
                capabilities,
                Array.Empty<IActivityCapabilityRuntimeReference>(),
                Array.Empty<ActorCameraBindingContribution>(),
                Array.Empty<ActorAttributeSetupContribution>(),
                Array.Empty<ActorPresentationSetupContribution>(),
                contributions,
                context.Source,
                context.Reason);
        }

        private static string BuildCapabilityPath(
            string componentPath,
            ActivityCapabilityPermissionReceiverIdentity receiverIdentity,
            string receiverKind)
        {
            string normalizedComponentPath = string.IsNullOrWhiteSpace(componentPath) ? string.Empty : componentPath.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedComponentPath))
            {
                return normalizedComponentPath;
            }

            string normalizedReceiverKind = string.IsNullOrWhiteSpace(receiverKind) ? "receiver" : receiverKind.Trim();
            string actorInstanceToken = receiverIdentity.ActorInstanceRuntimeId.IsValid
                ? receiverIdentity.ActorInstanceRuntimeId.Value
                : "actor.instance.unbound";
            string slotToken = receiverIdentity.PlayerSlotId.IsValid
                ? receiverIdentity.PlayerSlotId.Value
                : "slot.unbound";
            return $"{normalizedReceiverKind}|actorInstance={actorInstanceToken}|slot={slotToken}";
        }
    }

}
