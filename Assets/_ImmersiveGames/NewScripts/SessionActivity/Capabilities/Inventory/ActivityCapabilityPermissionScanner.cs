using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Actors.Projectile.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityPermissionScanner : IActivityCapabilityScanner
    {
        private const string ModuleId = "SessionActivity.PermissionTarget";
        private const string MovementReceiverKind = "movement";
        private const string ProjectileFireReceiverKind = "projectile_fire";
        private const string RetainedPlayerReceiverKind = "retained_player";
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

                if (!_identityResolver.TryResolve(target, out var playerIdentity))
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

                int targetContributionCount = contributions.Count;
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

                if (projectileFireEndpoint != null)
                {
                    AppendProjectileFirePermissionReceiverContribution(
                        contributions,
                        capabilityKeys,
                        inventoryId,
                        ownerId,
                        context.Identity,
                        playerIdentity,
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
                    playerIdentity,
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

        private static string BuildCapabilityPath(
            string receiverKind,
            string componentPath,
            ActivityCapabilityPermissionReceiverIdentity receiverIdentity)
        {
            string normalizedReceiverKind = string.IsNullOrWhiteSpace(receiverKind) ? "permission" : receiverKind.Trim();
            string normalizedComponentPath = string.IsNullOrWhiteSpace(componentPath) ? string.Empty : componentPath.Trim();
            string actorInstanceToken = receiverIdentity.ActorInstanceRuntimeId.IsValid
                ? receiverIdentity.ActorInstanceRuntimeId.Value
                : "actor.instance.unbound";
            string slotToken = receiverIdentity.PlayerSlotId.IsValid
                ? receiverIdentity.PlayerSlotId.Value
                : "slot.unbound";
            if (!string.IsNullOrWhiteSpace(normalizedComponentPath))
            {
                return $"{normalizedReceiverKind}|componentPath={normalizedComponentPath}|actorInstance={actorInstanceToken}|slot={slotToken}";
            }

            return $"{normalizedReceiverKind}|actorInstance={actorInstanceToken}|slot={slotToken}";
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
            string capabilityPath = BuildCapabilityPath(MovementReceiverKind, componentPath, receiverIdentity);
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

        private static void AppendProjectileFirePermissionReceiverContribution(
            List<ActivityPermissionReceiverContribution> contributions,
            HashSet<string> capabilityKeys,
            ActivityCapabilityInventoryId inventoryId,
            string ownerId,
            SessionActivityIdentity identity,
            PlayerActorCapabilityIdentity playerIdentity,
            ActivityCapabilityPermissionReceiverIdentity receiverIdentity,
            Component endpointComponent,
            IActorProjectileFireEndpoint projectileFireEndpoint,
            string source,
            string reason)
        {
            string componentPath = endpointComponent != null
                ? ActivityCapabilityTransformPathUtility.BuildTransformPath(endpointComponent.transform)
                : string.Empty;
            string capabilityPath = BuildCapabilityPath(ProjectileFireReceiverKind, componentPath, receiverIdentity);
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

            string receiverId = ActorProjectileFirePermissionReceiver.CreateReceiverId(receiverIdentity);
            IActivityPermissionReceiverProvider receiverProvider = new ActorProjectileFirePermissionReceiverProvider(
                projectileFireEndpoint,
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

        private static void AppendRetainedPlayerPermissionReceiverContribution(
            List<ActivityPermissionReceiverContribution> contributions,
            HashSet<string> capabilityKeys,
            ActivityCapabilityInventoryId inventoryId,
            string ownerId,
            SessionActivityIdentity identity,
            PlayerActorCapabilityIdentity playerIdentity,
            ActivityCapabilityPermissionReceiverIdentity receiverIdentity,
            IActorPermissionReceiver directPermissionReceiver,
            string source,
            string reason)
        {
            if (directPermissionReceiver == null)
            {
                return;
            }

            string receiverId = directPermissionReceiver.ReceiverId;
            if (string.IsNullOrWhiteSpace(receiverId))
            {
                return;
            }

            var receiverComponent = directPermissionReceiver as Component;
            string componentPath = receiverComponent != null
                ? ActivityCapabilityTransformPathUtility.BuildTransformPath(receiverComponent.transform)
                : string.Empty;
            string capabilityPath = BuildCapabilityPath(RetainedPlayerReceiverKind, componentPath, receiverIdentity);
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

            IActivityPermissionReceiverProvider receiverProvider = new ExistingPermissionReceiverProvider(directPermissionReceiver);
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

            DebugUtility.LogVerbose(typeof(ActivityCapabilityPermissionScanner), 
                $"event='retained_player_permission_receiver_contribution_resolved' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' actorId='{playerIdentity.ActorId}' actorInstanceRuntimeId='{playerIdentity.ActorInstanceRuntimeId}' playerActorId='{playerIdentity.PlayerActorId}' playerSlotId='{playerIdentity.PlayerSlotId}' receiverId='{receiverId}' source='{source}' reason='{reason}'.");
        }

        private sealed class ExistingPermissionReceiverProvider : IActivityPermissionReceiverProvider
        {
            private readonly IActivityCapabilityPermissionReceiver _receiver;

            public ExistingPermissionReceiverProvider(IActivityCapabilityPermissionReceiver receiver)
            {
                _receiver = receiver;
            }

            public string ReceiverId => _receiver?.ReceiverId ?? string.Empty;

            public bool TryCreateReceiver(out IActivityCapabilityPermissionReceiver receiver)
            {
                receiver = _receiver;
                return receiver != null;
            }
        }
    }
}
