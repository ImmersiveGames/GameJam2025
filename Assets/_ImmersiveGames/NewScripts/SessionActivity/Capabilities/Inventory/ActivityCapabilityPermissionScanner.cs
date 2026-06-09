using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityPermissionScanner : IActivityCapabilityScanner
    {
        private const string ModuleId = "SessionActivity.PermissionTarget";
        private const string MovementReceiverKind = "movement";
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
                        $"ActivityCapabilityPermissionScanner requires ActorCapabilitySurface actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}'.");
                }

                IActorMovementEndpoint movementEndpoint = surface.ActorMovementEndpoint;
                if (movementEndpoint == null)
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
                        $"[OBS][ActivityCapabilityPermissionScanner] event='PermissionTargetIdentityUnresolved' reason='player_identity_missing' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}' capabilityKind='{ActivityCapabilityKind.PermissionTarget}' componentPath='{unresolvedComponentPath}' source='{context.Source}' activityId='{context.Identity.ActivityId}' entrySequence='{context.Identity.EntrySequence}'.");
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

                AppendMovementPermissionReceiverContribution(
                    contributions,
                    capabilityKeys,
                    inventoryId,
                    ownerId,
                    context.Identity,
                    playerIdentity,
                    receiverIdentity,
                    movementEndpoint as Component,
                    movementEndpoint,
                    context.Source,
                    context.Reason);
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
            ActivityCapabilityPermissionReceiverIdentity receiverIdentity)
        {
            string normalizedComponentPath = string.IsNullOrWhiteSpace(componentPath) ? string.Empty : componentPath.Trim();
            string actorInstanceToken = receiverIdentity.ActorInstanceRuntimeId.IsValid
                ? receiverIdentity.ActorInstanceRuntimeId.Value
                : "actor.instance.unbound";
            string slotToken = receiverIdentity.PlayerSlotId.IsValid
                ? receiverIdentity.PlayerSlotId.Value
                : "slot.unbound";
            if (!string.IsNullOrWhiteSpace(normalizedComponentPath))
            {
                return $"{MovementReceiverKind}|componentPath={normalizedComponentPath}|actorInstance={actorInstanceToken}|slot={slotToken}";
            }

            return $"{MovementReceiverKind}|actorInstance={actorInstanceToken}|slot={slotToken}";
        }

        private static void AppendMovementPermissionReceiverContribution(
            List<ActivityPermissionReceiverContribution> contributions,
            HashSet<string> capabilityKeys,
            ActivityCapabilityInventoryId inventoryId,
            string ownerId,
            SessionActivityIdentity identity,
            PlayerActorCapabilityIdentity playerIdentity,
            ActivityCapabilityPermissionReceiverIdentity receiverIdentity,
            Component endpointComponent,
            IActorMovementEndpoint movementEndpoint,
            string source,
            string reason)
        {
            string componentPath = endpointComponent != null
                ? ActivityCapabilityTransformPathUtility.BuildTransformPath(endpointComponent.transform)
                : string.Empty;
            string capabilityPath = BuildCapabilityPath(componentPath, receiverIdentity);
            string capabilityId = ActivityCapabilityInventoryId.DeriveCapabilityId(
                inventoryId,
                ownerId,
                ActivityCapabilityKind.PermissionTarget,
                ModuleId,
                capabilityPath);

            if (!capabilityKeys.Add(capabilityId))
            {
                return;
            }

            string receiverId = PlayerMovementPermissionReceiver.CreateReceiverId(receiverIdentity);
            IActivityPermissionReceiverProvider receiverProvider = new PlayerMovementPermissionReceiverProvider(
                movementEndpoint,
                receiverId,
                playerIdentity.ActorId,
                playerIdentity.ActorInstanceRuntimeId,
                playerIdentity.PlayerActorId,
                playerIdentity.PlayerSlotId);

            contributions.Add(new ActivityPermissionReceiverContribution(
                identity,
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
                source,
                reason));
        }
    }
}
