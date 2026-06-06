using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Projectile.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityProjectileEmitterScanner : IActivityCapabilityScanner
    {
        private const string ModuleId = "Actors.Projectile";
        private readonly IPlayerActorCapabilityIdentityResolver _identityResolver;

        public ActivityCapabilityProjectileEmitterScanner(IPlayerActorCapabilityIdentityResolver identityResolver)
        {
            _identityResolver = identityResolver ?? throw new InvalidOperationException("ActivityCapabilityProjectileEmitterScanner requires non-null identity resolver.");
        }

        public string ScannerId => "activity_capability_projectile_emitter_scanner.v1";
        public int Order => 318;

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
                        $"ActivityCapabilityProjectileEmitterScanner requires ActorCapabilitySurface actorId='{target.ActorId}' actorInstanceId='{target.ActorInstanceId.Value}'.");
                }

                IActorProjectileEmitterEndpoint endpoint = surface.ActorProjectileEmitterEndpoint;
                bool required = target.ActorSourceKind == ActorSourceKind.PlayerParticipation;
                if (endpoint == null)
                {
                    if (required)
                    {
                        Debug.LogWarning(
                            $"[OBS][ActivityCapabilityProjectileEmitterScanner] event='ProjectileEmitterCapabilityValidationFailed' reason='required_endpoint_missing' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceId.Value}' capabilityKind='{ActivityCapabilityKind.ProjectileEmitter}' required='true' source='{context.Source}' activityId='{context.Identity.ActivityId}' entrySequence='{context.Identity.EntrySequence}'.");
                        throw new InvalidOperationException(
                            $"ActivityCapabilityProjectileEmitterScanner required projectile emitter endpoint missing for actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceId.Value}'.");
                    }

                    Debug.Log(
                        $"[OBS][ActivityCapabilityProjectileEmitterScanner] event='ProjectileEmitterCapabilitySkipped' reason='optional_endpoint_missing' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceId.Value}' capabilityKind='{ActivityCapabilityKind.ProjectileEmitter}' required='false' source='{context.Source}' activityId='{context.Identity.ActivityId}' entrySequence='{context.Identity.EntrySequence}'.");
                    continue;
                }

                if (!_identityResolver.TryResolve(target, out PlayerActorCapabilityIdentity playerIdentity))
                {
                    if (required)
                    {
                        Debug.LogWarning(
                            $"[OBS][ActivityCapabilityProjectileEmitterScanner] event='ProjectileEmitterCapabilityValidationFailed' reason='player_identity_missing' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceId.Value}' capabilityKind='{ActivityCapabilityKind.ProjectileEmitter}' required='true' source='{context.Source}' activityId='{context.Identity.ActivityId}' entrySequence='{context.Identity.EntrySequence}'.");
                        throw new InvalidOperationException($"ActivityCapabilityProjectileEmitterScanner requires player identity for required projectile emitter actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceId.Value}'.");
                    }

                    Debug.Log(
                        $"[OBS][ActivityCapabilityProjectileEmitterScanner] event='ProjectileEmitterCapabilitySkipped' reason='player_identity_missing' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceId.Value}' capabilityKind='{ActivityCapabilityKind.ProjectileEmitter}' required='false' source='{context.Source}' activityId='{context.Identity.ActivityId}' entrySequence='{context.Identity.EntrySequence}'.");
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

                Component endpointComponent = endpoint as Component;
                string componentPath = endpointComponent != null
                    ? ActivityCapabilityTransformPathUtility.BuildTransformPath(endpointComponent.transform)
                    : string.Empty;
                string componentType = endpoint.GetType().FullName ?? endpoint.GetType().Name;
                string capabilityId = ActivityCapabilityInventoryId.DeriveCapabilityId(
                    inventoryId,
                    ownerId,
                    ActivityCapabilityKind.ProjectileEmitter,
                    ModuleId,
                    componentPath);

                if (!capabilityKeys.Add(capabilityId))
                {
                    continue;
                }

                Debug.Log(
                    $"[OBS][ActivityCapabilityProjectileEmitterScanner] event='ProjectileEmitterCapabilityObserved' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceId.Value}' capabilityId='{capabilityId}' capabilityKind='{ActivityCapabilityKind.ProjectileEmitter}' required='{required}' componentPath='{componentPath}' componentType='{componentType}' source='{context.Source}' activityId='{context.Identity.ActivityId}' entrySequence='{context.Identity.EntrySequence}'.");

                capabilities.Add(new ActivityCapabilityDescriptor(
                    capabilityId,
                    ActivityCapabilityKind.ProjectileEmitter,
                    ModuleId,
                    ownerId,
                    componentPath,
                    componentType,
                    required,
                    priority: 140,
                    policyMetadata: new[]
                    {
                        new ActivityCapabilityPolicyEntry("actorId", target.ActorId),
                        new ActivityCapabilityPolicyEntry("actorInstanceRuntimeId", target.ActorInstanceId.Value),
                        new ActivityCapabilityPolicyEntry("actorKind", target.ActorKind.ToString()),
                        new ActivityCapabilityPolicyEntry("actorRole", target.ActorRole.ToString()),
                        new ActivityCapabilityPolicyEntry("actorScope", target.ActorScope.ToString()),
                        new ActivityCapabilityPolicyEntry("actorSourceKind", target.ActorSourceKind.ToString()),
                        new ActivityCapabilityPolicyEntry("participationPolicy", target.ParticipationPolicy),
                    },
                    source: context.Source));

                runtimeReferences.Add(new ActorProjectileEmitterEndpointReference(
                    capabilityId,
                    ownerId,
                    target.ActorInstanceId,
                    target.ActorId,
                    target.ActorKind,
                    target.ActorRole,
                    target.ActorScope,
                    componentPath,
                    endpoint));

                if (endpoint is not ActorProjectileEmitterEndpoint projectileEndpoint)
                {
                    throw new InvalidOperationException($"ActivityCapabilityProjectileEmitterScanner requires ActorProjectileEmitterEndpoint concrete component for actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceId.Value}'.");
                }

                string receiverId = $"projectile.fire.receiver|pipeline={context.Identity.PipelineId}|session={context.Identity.SessionId}|activity={context.Identity.ActivityId}|entry={context.Identity.EntrySequence}|actorInstance={target.ActorInstanceId.Value}|slot={playerIdentity.PlayerSlotId.Value}";
                ActorProjectileFirePermissionReceiver receiver = new(
                    projectileEndpoint,
                    receiverId,
                    playerIdentity.ActorId,
                    playerIdentity.ActorInstanceRuntimeId,
                    playerIdentity.PlayerActorId,
                    playerIdentity.PlayerSlotId);

                string permissionCapabilityId = ActivityCapabilityInventoryId.DeriveCapabilityId(
                    inventoryId,
                    ownerId,
                    ActivityCapabilityKind.PermissionTarget,
                    $"{ModuleId}.FirePermission",
                    componentPath);

                capabilities.Add(new ActivityCapabilityDescriptor(
                    permissionCapabilityId,
                    ActivityCapabilityKind.PermissionTarget,
                    $"{ModuleId}.FirePermission",
                    ownerId,
                    componentPath,
                    receiver.GetType().FullName ?? receiver.GetType().Name,
                    required,
                    priority: 141,
                    policyMetadata: new[]
                    {
                        new ActivityCapabilityPolicyEntry("permissionId", ActivityCapabilityPermissionIds.ActivityGameplayControl),
                        new ActivityCapabilityPolicyEntry("actorId", playerIdentity.ActorId.Value),
                        new ActivityCapabilityPolicyEntry("actorInstanceRuntimeId", playerIdentity.ActorInstanceRuntimeId.Value),
                        new ActivityCapabilityPolicyEntry("playerActorId", playerIdentity.PlayerActorId.Value),
                        new ActivityCapabilityPolicyEntry("playerSlotId", playerIdentity.PlayerSlotId.Value),
                        new ActivityCapabilityPolicyEntry("actorRole", target.ActorRole.ToString()),
                    },
                    source: context.Source));

                runtimeReferences.Add(new ActivityCapabilityPermissionReceiverReference(
                    permissionCapabilityId,
                    ownerId,
                    playerIdentity.ActorId,
                    playerIdentity.ActorInstanceRuntimeId,
                    playerIdentity.PlayerActorId,
                    playerIdentity.PlayerSlotId,
                    componentPath,
                    ActivityCapabilityPermissionId.ActivityGameplayControl,
                    new ActivityCapabilityPermissionReceiverIdentity(
                        context.Identity.PipelineId,
                        context.Identity.SessionId,
                        context.Identity.ActivityId,
                        context.Identity.EntrySequence,
                        playerIdentity.ActorId,
                        playerIdentity.ActorInstanceRuntimeId,
                        playerIdentity.PlayerActorId,
                        playerIdentity.PlayerSlotId),
                    receiver));

                Debug.Log(
                    $"[OBS][ActivityCapabilityProjectileEmitterScanner] event='ProjectileEmitterCapabilityValidationPassed' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceId.Value}' capabilityId='{capabilityId}' capabilityKind='{ActivityCapabilityKind.ProjectileEmitter}' required='{required}' source='{context.Source}' activityId='{context.Identity.ActivityId}' entrySequence='{context.Identity.EntrySequence}'.");
            }

            return new ActivityCapabilityScanResult(ScannerId, owners, capabilities, runtimeReferences, context.Source, context.Reason);
        }
    }
}
