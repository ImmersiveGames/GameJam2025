using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Actors.Projectile.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using UnityEngine;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityPermissionScanner : IActivityCapabilityScanner
    {
        private const string ModuleId = "SessionActivity.PermissionTarget";
        public string ScannerId => "activity_capability_permission_scanner.v1";
        public int Order => 300;

        public ActivityCapabilityScanResult Scan(ActivityCapabilityScanContext context)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("Activity capability scan context is invalid.");
            }

            var inventoryId = context.InventoryId;
            List<ActivityCapabilityOwnerDescriptor> owners = new();
            List<ActivityCapabilityDescriptor> capabilities = new();
            List<ActivityPermissionReceiverContribution> contributions = new();
            HashSet<string> ownerKeys = new(StringComparer.Ordinal);
            HashSet<string> capabilityKeys = new(StringComparer.Ordinal);

            for (int index = 0; index < context.ActorTargets.Count; index++)
            {
                var target = context.ActorTargets[index];
                if (!target.IsValid)
                {
                    continue;
                }

                var surface = target.CapabilitySurface;
                if (surface == null)
                {
                    throw new InvalidOperationException(
                        $"ActivityCapabilityPermissionScanner requires ActorCapabilitySurface actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}'.");
                }

                var movementEndpoint = surface.ActorMovementEndpoint;
                var projectileFireEndpoint = surface.ActorProjectileFireEndpoint;
                var directPermissionReceiver = surface.ActorPermissionReceiver;
                if (movementEndpoint == null && projectileFireEndpoint == null && directPermissionReceiver == null)
                {
                    continue;
                }

                if (!TryResolvePlayerIdentity(target, out var playerActorId, out var playerSlotId))
                {
                    var endpointComponent = movementEndpoint as Component ?? projectileFireEndpoint as Component;
                    string unresolvedComponentPath = endpointComponent != null
                        ? ActivityCapabilityTransformPathUtility.BuildTransformPath(endpointComponent.transform)
                        : string.Empty;
                    DebugUtility.LogWarning(typeof(ActivityCapabilityPermissionScanner),
                        $"event='PermissionTargetIdentityUnresolved' reason='player_identity_missing' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}' capabilityKind='{ActivityCapabilityKind.PermissionTarget}' componentPath='{unresolvedComponentPath}' source='{context.Source}' activityId='{context.Identity.ActivityId}' entrySequence='{context.Identity.EntrySequence}'.");
                    continue;
                }

                string ownerPath = ActivityCapabilityTransformPathUtility.BuildTransformPath(target.ActorRoot.transform);
                string ownerId = BuildPermissionOwnerId(
                    inventoryId,
                    ActivityCapabilityOwnerKind.Actor,
                    new ActorId(target.ActorId),
                    target.ActorInstanceRuntimeId);

                if (ownerKeys.Add(ownerId))
                {
                    owners.Add(new ActivityCapabilityOwnerDescriptor(
                        ActivityCapabilityOwnerKind.Actor,
                        ownerId,
                        ownerPath,
                        target.SourceSceneName,
                        target.Source,
                        context.Source));
                }

                int targetContributionCount = contributions.Count;
                ActivityCapabilityPermissionReceiverIdentity receiverIdentity = new(
                    context.Identity.PipelineId,
                    context.Identity.SessionId,
                    context.Identity.ActivityId,
                    context.Identity.EntrySequence,
                    new ActorId(target.ActorId),
                    target.ActorInstanceRuntimeId,
                    playerActorId,
                    playerSlotId);

                if (movementEndpoint != null)
                {
                    AppendMovementPermissionReceiverContribution(
                        contributions,
                        capabilityKeys,
                        inventoryId,
                        ownerId,
                        context.Identity,
                        receiverIdentity,
                        movementEndpoint as Component,
                        movementEndpoint,
                        context.Source,
                        context.Reason);
                }

                if (projectileFireEndpoint != null)
                {
                    AppendProjectileFirePermissionReceiverContribution(
                        contributions,
                        capabilityKeys,
                        inventoryId,
                        ownerId,
                        context.Identity,
                        receiverIdentity,
                        projectileFireEndpoint as Component,
                        projectileFireEndpoint,
                        context.Source,
                        context.Reason);
                }

                if (contributions.Count != targetContributionCount ||
                    !ActorLifetimePolicyRuntime.IsRetainedAcrossActivity(target.ActorScope))
                {
                    continue;
                }

                AppendRetainedPlayerPermissionReceiverContribution(
                    contributions,
                    capabilityKeys,
                    inventoryId,
                    ownerId,
                    context.Identity,
                    receiverIdentity,
                    directPermissionReceiver,
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

        private static string BuildPermissionOwnerId(
            ActivityCapabilityInventoryId inventoryId,
            ActivityCapabilityOwnerKind ownerKind,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId)
        {
            string actorIdToken = actorId.IsValid ? actorId.Value : string.Empty;
            string actorInstanceToken = actorInstanceRuntimeId.IsValid
                ? actorInstanceRuntimeId.Value
                : "actor.instance.unbound";
            return $"{inventoryId.Signature}|ownerKind={ownerKind}|actorId={actorIdToken}|actorInstance={actorInstanceToken}";
        }

        private static string BuildPermissionCapabilityId(
            ActivityCapabilityInventoryId inventoryId,
            string ownerId,
            ActivityCapabilityPermissionId permissionId,
            ActivityCapabilityPermissionReceiverId receiverId)
        {
            return $"{inventoryId.Signature}|ownerId={ownerId}|capabilityKind={ActivityCapabilityKind.PermissionTarget}|moduleId={ModuleId}|permissionId={permissionId}|receiverId={receiverId}";
        }

        private static void AppendMovementPermissionReceiverContribution(
            List<ActivityPermissionReceiverContribution> contributions,
            HashSet<string> capabilityKeys,
            ActivityCapabilityInventoryId inventoryId,
            string ownerId,
            SessionActivityIdentity identity,
            ActivityCapabilityPermissionReceiverIdentity receiverIdentity,
            Component endpointComponent,
            IActorMovementEndpoint movementEndpoint,
            string source,
            string reason)
        {
            string componentPath = endpointComponent != null
                ? ActivityCapabilityTransformPathUtility.BuildTransformPath(endpointComponent.transform)
                : string.Empty;
            var receiverId = PlayerMovementPermissionReceiver.CreateReceiverId(receiverIdentity);
            string capabilityId = BuildPermissionCapabilityId(
                inventoryId,
                ownerId,
                ActivityCapabilityPermissionId.ActivityGameplayControl,
                receiverId);

            if (!capabilityKeys.Add(capabilityId))
            {
                return;
            }

            IActivityPermissionReceiverProvider receiverProvider = new PlayerMovementPermissionReceiverProvider(
                movementEndpoint,
                receiverId,
                receiverIdentity.ActorId,
                receiverIdentity.ActorInstanceRuntimeId,
                receiverIdentity.PlayerActorId,
                receiverIdentity.PlayerSlotId);

            contributions.Add(new ActivityPermissionReceiverContribution(
                identity,
                capabilityId,
                ownerId,
                receiverIdentity.ActorId,
                receiverIdentity.ActorInstanceRuntimeId,
                receiverIdentity.PlayerActorId,
                receiverIdentity.PlayerSlotId,
                ActivityCapabilityPermissionId.ActivityGameplayControl,
                receiverIdentity,
                receiverId,
                componentPath,
                receiverProvider,
                source,
                reason));
        }

        private static void AppendProjectileFirePermissionReceiverContribution(
            List<ActivityPermissionReceiverContribution> contributions,
            HashSet<string> capabilityKeys,
            ActivityCapabilityInventoryId inventoryId,
            string ownerId,
            SessionActivityIdentity identity,
            ActivityCapabilityPermissionReceiverIdentity receiverIdentity,
            Component endpointComponent,
            IActorProjectileFireEndpoint projectileFireEndpoint,
            string source,
            string reason)
        {
            string componentPath = endpointComponent != null
                ? ActivityCapabilityTransformPathUtility.BuildTransformPath(endpointComponent.transform)
                : string.Empty;
            var receiverId = ActorProjectileFirePermissionReceiver.CreateReceiverId(receiverIdentity);
            string capabilityId = BuildPermissionCapabilityId(
                inventoryId,
                ownerId,
                ActivityCapabilityPermissionId.ActivityGameplayControl,
                receiverId);

            if (!capabilityKeys.Add(capabilityId))
            {
                return;
            }

            IActivityPermissionReceiverProvider receiverProvider = new ActorProjectileFirePermissionReceiverProvider(
                projectileFireEndpoint,
                receiverId,
                receiverIdentity.ActorId,
                receiverIdentity.ActorInstanceRuntimeId,
                receiverIdentity.PlayerActorId,
                receiverIdentity.PlayerSlotId);

            contributions.Add(new ActivityPermissionReceiverContribution(
                identity,
                capabilityId,
                ownerId,
                receiverIdentity.ActorId,
                receiverIdentity.ActorInstanceRuntimeId,
                receiverIdentity.PlayerActorId,
                receiverIdentity.PlayerSlotId,
                ActivityCapabilityPermissionId.ActivityGameplayControl,
                receiverIdentity,
                receiverId,
                componentPath,
                receiverProvider,
                source,
                reason));
        }

        private static void AppendRetainedPlayerPermissionReceiverContribution(
            List<ActivityPermissionReceiverContribution> contributions,
            HashSet<string> capabilityKeys,
            ActivityCapabilityInventoryId inventoryId,
            string ownerId,
            SessionActivityIdentity identity,
            ActivityCapabilityPermissionReceiverIdentity receiverIdentity,
            IActorPermissionReceiver directPermissionReceiver,
            string source,
            string reason)
        {
            if (directPermissionReceiver == null)
            {
                return;
            }

            var receiverId = directPermissionReceiver.ReceiverId;
            if (!receiverId.IsValid)
            {
                return;
            }

            var receiverComponent = directPermissionReceiver as Component;
            string componentPath = receiverComponent != null
                ? ActivityCapabilityTransformPathUtility.BuildTransformPath(receiverComponent.transform)
                : string.Empty;
            string capabilityId = BuildPermissionCapabilityId(
                inventoryId,
                ownerId,
                ActivityCapabilityPermissionId.ActivityGameplayControl,
                receiverId);

            if (!capabilityKeys.Add(capabilityId))
            {
                return;
            }

            IActivityPermissionReceiverProvider receiverProvider = new ExistingPermissionReceiverProvider(directPermissionReceiver);
            contributions.Add(new ActivityPermissionReceiverContribution(
                identity,
                capabilityId,
                ownerId,
                receiverIdentity.ActorId,
                receiverIdentity.ActorInstanceRuntimeId,
                receiverIdentity.PlayerActorId,
                receiverIdentity.PlayerSlotId,
                ActivityCapabilityPermissionId.ActivityGameplayControl,
                receiverIdentity,
                receiverId,
                componentPath,
                receiverProvider,
                source,
                reason));

            DebugUtility.LogVerbose(typeof(ActivityCapabilityPermissionScanner),
                $"event='retained_player_permission_receiver_contribution_resolved' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' actorId='{receiverIdentity.ActorId}' actorInstanceRuntimeId='{receiverIdentity.ActorInstanceRuntimeId}' playerActorId='{receiverIdentity.PlayerActorId}' playerSlotId='{receiverIdentity.PlayerSlotId}' receiverId='{receiverId}' source='{source}' reason='{reason}'.");
        }

        private static bool TryResolvePlayerIdentity(
            ActorScanTarget target,
            out PlayerActorId playerActorId,
            out PlayerSlotId playerSlotId)
        {
            playerActorId = default;
            playerSlotId = default;

            if (target.RuntimeActor == null)
            {
                return false;
            }

            var playerIdentity = target.RuntimeActor.GetComponent<PlayerActorIdentity>();
            if (playerIdentity == null || !playerIdentity.IsValid)
            {
                return false;
            }

            playerActorId = playerIdentity.PlayerActorId;
            playerSlotId = playerIdentity.PlayerSlotId;
            return playerActorId.IsValid && playerSlotId.IsValid;
        }

        private sealed class ExistingPermissionReceiverProvider : IActivityPermissionReceiverProvider
        {
            private readonly IActivityCapabilityPermissionReceiver _receiver;

            public ExistingPermissionReceiverProvider(IActivityCapabilityPermissionReceiver receiver)
            {
                _receiver = receiver;
            }

            public ActivityCapabilityPermissionReceiverId ReceiverId => _receiver?.ReceiverId ?? default;

            public bool TryCreateReceiver(out IActivityCapabilityPermissionReceiver receiver)
            {
                receiver = _receiver;
                return receiver != null;
            }
        }
    }
}
