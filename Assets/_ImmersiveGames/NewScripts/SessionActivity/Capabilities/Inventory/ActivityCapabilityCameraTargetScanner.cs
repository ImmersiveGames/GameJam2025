using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityCameraTargetScanner : IActivityCapabilityScanner
    {
        private const string ModuleId = "SessionActivity.CameraTarget";
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
                        $"[OBS][ActivityCapabilityCameraTargetScanner] event='CameraTargetIdentityUnresolved' reason='player_identity_missing' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceId.Value}' capabilityKind='{ActivityCapabilityKind.CameraTarget}' componentPath='{unresolvedComponentPath}' source='{context.Source}' activityId='{context.Identity.ActivityId}' entrySequence='{context.Identity.EntrySequence}'.");
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

                Component cameraComponent = endpoint as Component;
                string componentPath = cameraComponent != null
                    ? ActivityCapabilityTransformPathUtility.BuildTransformPath(cameraComponent.transform)
                    : string.Empty;
                string componentType = endpoint.GetType().FullName ?? endpoint.GetType().Name;
                string capabilityId = ActivityCapabilityInventoryId.DeriveCapabilityId(
                    inventoryId,
                    ownerId,
                    ActivityCapabilityKind.CameraTarget,
                    ModuleId,
                    componentPath);

                if (!capabilityKeys.Add(capabilityId))
                {
                    continue;
                }

                capabilities.Add(new ActivityCapabilityDescriptor(
                    capabilityId,
                    ActivityCapabilityKind.CameraTarget,
                    ModuleId,
                    ownerId,
                    componentPath,
                    componentType,
                    required: true,
                    priority: 120,
                    policyMetadata: new[]
                    {
                        new ActivityCapabilityPolicyEntry("actorId", playerIdentity.ActorId.Value),
                        new ActivityCapabilityPolicyEntry("playerActorId", playerIdentity.PlayerActorId.Value),
                        new ActivityCapabilityPolicyEntry("playerSlotId", playerIdentity.PlayerSlotId.Value),
                        new ActivityCapabilityPolicyEntry("actorRole", target.ActorRole.ToString()),
                    },
                    source: context.Source));

                runtimeReferences.Add(new ActivityCameraTargetReference(
                    capabilityId,
                    ownerId,
                    playerIdentity.ActorId,
                    playerIdentity.PlayerActorId,
                    playerIdentity.PlayerSlotId,
                    componentPath,
                    endpoint.FollowTarget,
                    endpoint.LookAtTarget));
            }

            return new ActivityCapabilityScanResult(ScannerId, owners, capabilities, runtimeReferences, context.Source, context.Reason);
        }
    }
}
