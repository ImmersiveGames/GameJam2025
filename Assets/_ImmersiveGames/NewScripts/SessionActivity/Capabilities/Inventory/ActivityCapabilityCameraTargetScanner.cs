using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityCameraTargetScanner : IActivityCapabilityScanner
    {
        private readonly IPlayerActorCapabilityIdentityResolver _identityResolver;

        public ActivityCapabilityCameraTargetScanner(IPlayerActorCapabilityIdentityResolver identityResolver)
        {
            _identityResolver = identityResolver ?? throw new InvalidOperationException("ActivityCapabilityCameraTargetScanner requires non-null identity resolver.");
        }

        public string ScannerId => "activity_capability_camera_target_scanner.v1";
        public int Order => 320;

        public ActivityCapabilityScanResult Scan(ActivityCapabilityScanContext context)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("Activity capability scan context is invalid.");
            }

            ActivityCapabilityInventoryId inventoryId = context.InventoryId;
            List<ActivityCapabilityOwnerDescriptor> owners = new();
            List<ActorCameraBindingContribution> cameraContributions = new();
            HashSet<string> ownerKeys = new(StringComparer.Ordinal);
            HashSet<string> contributionKeys = new(StringComparer.Ordinal);

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
                        $"ActivityCapabilityCameraTargetScanner requires ActorCapabilitySurface actorId='{target.ActorId}' actorInstanceId='{target.ActorInstanceId.Value}'.");
                }

                IActorCameraTargetEndpoint endpoint = surface.ActorCameraTargetEndpoint;
                if (endpoint == null || !endpoint.HasValidTargets)
                {
                    continue;
                }

                if (!_identityResolver.TryResolve(target, out PlayerActorCapabilityIdentity playerIdentity))
                {
                    Component endpointComponent = endpoint as Component;
                    string unresolvedComponentPath = endpointComponent != null
                        ? ActivityCapabilityTransformPathUtility.BuildTransformPath(endpointComponent.transform)
                        : string.Empty;
                    Debug.LogWarning(
                        $"[OBS][ActivityCapabilityCameraTargetScanner] event='CameraBindingContributionIdentityUnresolved' reason='player_identity_missing' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceId.Value}' bindingKind='CameraBindingContribution' componentPath='{unresolvedComponentPath}' source='{context.Source}' activityId='{context.Identity.ActivityId}' entrySequence='{context.Identity.EntrySequence}'.");
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

                if (!ActorCameraBindingContributionBuilder.TryBuild(
                        context.Identity,
                        target,
                        playerIdentity,
                        endpoint,
                        context.Source,
                        context.Reason,
                        out ActorCameraBindingContribution contribution))
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
    }
}
