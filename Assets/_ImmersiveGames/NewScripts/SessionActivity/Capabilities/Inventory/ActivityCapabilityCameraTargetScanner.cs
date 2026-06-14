using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using UnityEngine;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityCameraTargetScanner : IActivityCapabilityScanner
    {
        public string ScannerId => "activity_capability_camera_target_scanner.v1";
        public int Order => 320;

        public ActivityCapabilityScanResult Scan(ActivityCapabilityScanContext context)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("Activity capability scan context is invalid.");
            }

            var inventoryId = context.InventoryId;
            List<ActivityCapabilityOwnerDescriptor> owners = new();
            List<ActorCameraBindingContribution> cameraContributions = new();
            HashSet<string> ownerKeys = new(StringComparer.Ordinal);
            HashSet<string> contributionKeys = new(StringComparer.Ordinal);

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
                        $"ActivityCapabilityCameraTargetScanner requires ActorCapabilitySurface actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}'.");
                }

                var endpoint = surface.ActorCameraTargetEndpoint;
                if (endpoint == null || !endpoint.HasValidTargets)
                {
                    continue;
                }

                if (!TryResolvePlayerIdentity(target, out var playerActorId, out var playerSlotId))
                {
                    var endpointComponent = endpoint as Component;
                    string unresolvedComponentPath = endpointComponent != null
                        ? ActivityCapabilityTransformPathUtility.BuildTransformPath(endpointComponent.transform)
                        : string.Empty;
                    DebugUtility.LogWarning(typeof(ActivityCapabilityCameraTargetScanner), 
                        $"event='CameraBindingContributionIdentityUnresolved' reason='player_identity_missing' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}' bindingKind='CameraBindingContribution' componentPath='{unresolvedComponentPath}' source='{context.Source}' activityId='{context.Identity.ActivityId}' entrySequence='{context.Identity.EntrySequence}'.");
                    continue;
                }

                string ownerPath = ActivityCapabilityTransformPathUtility.BuildTransformPath(target.ActorRoot.transform);
                string ownerId = ActivityCapabilityInventoryId.DeriveOwnerId(
                    inventoryId,
                    ActivityCapabilityOwnerKind.Actor,
                    ownerPath,
                    target.ActorId);

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

                if (!ActorCameraBindingContributionBuilder.TryBuild(
                        context.Identity,
                        target,
                        endpoint,
                        context.Source,
                        context.Reason,
                        out var contribution))
                {
                    continue;
                }

                string contributionKey = $"{contribution.ActorInstanceRuntimeId}|{contribution.ComponentPath}";
                if (!contributionKeys.Add(contributionKey))
                {
                    continue;
                }

                cameraContributions.Add(contribution);
            }

            return new ActivityCapabilityScanResult(
                ScannerId,
                owners,
                Array.Empty<ActivityCapabilityDescriptor>(),
                Array.Empty<IActivityCapabilityRuntimeReference>(),
                cameraContributions,
                Array.Empty<ActorAttributeSetupContribution>(),
                Array.Empty<ActorPresentationSetupContribution>(),
                Array.Empty<ActivityPermissionReceiverContribution>(),
                context.Source,
                context.Reason);
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
    }
}
