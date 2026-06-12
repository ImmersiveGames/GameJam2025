using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityActorAttributeScanner : IActivityCapabilityScanner
    {
        private const string ModuleId = "Actors.Attributes";

        public string ScannerId => "activity_capability_actor_attribute_scanner.v1";
        public int Order => 315;

        public ActivityCapabilityScanResult Scan(ActivityCapabilityScanContext context)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("Activity capability scan context is invalid.");
            }

            var inventoryId = context.InventoryId;
            List<ActivityCapabilityOwnerDescriptor> owners = new();
            List<ActorAttributeSetupContribution> attributeContributions = new();
            HashSet<string> ownerKeys = new(StringComparer.Ordinal);
            HashSet<string> contributionKeys = new(StringComparer.Ordinal);

            for (int index = 0; index < context.ActorTargets.Count; index++)
            {
                var target = context.ActorTargets[index];
                if (!target.IsValid)
                {
                    continue;
                }

                if (target.CapabilitySurface == null)
                {
                    throw new InvalidOperationException(
                        $"ActivityCapabilityActorAttributeScanner requires ActorCapabilitySurface actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}'.");
                }

                var endpoint = target.CapabilitySurface.AttributeEndpoint;
                if (endpoint == null)
                {
                    continue;
                }

                string ownerPath = ActivityCapabilityTransformPathUtility.BuildTransformPath(target.ActorRoot.transform);
                string ownerId = ActivityCapabilityInventoryId.DeriveOwnerId(
                    inventoryId,
                    ResolveOwnerKind(target),
                    ownerPath,
                    target.ActorId);

                if (ownerKeys.Add(ownerId))
                {
                    owners.Add(new ActivityCapabilityOwnerDescriptor(
                        ResolveOwnerKind(target),
                        ownerId,
                        ownerPath,
                        target.SourceSceneName,
                        target.Source,
                        context.Source));
                }

                if (!ActorAttributeSetupContributionBuilder.TryBuild(
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

                attributeContributions.Add(contribution);
            }

            return new ActivityCapabilityScanResult(
                ScannerId,
                owners,
                Array.Empty<ActivityCapabilityDescriptor>(),
                Array.Empty<IActivityCapabilityRuntimeReference>(),
                Array.Empty<ActorCameraBindingContribution>(),
                attributeContributions,
                Array.Empty<ActorPresentationSetupContribution>(),
                Array.Empty<ActivityPermissionReceiverContribution>(),
                context.Source,
                context.Reason);
        }

        private static ActivityCapabilityOwnerKind ResolveOwnerKind(ActorScanTarget target)
        {
            return target.IsValid
                ? ActivityCapabilityOwnerKind.Actor
                : ActivityCapabilityOwnerKind.Unsupported;
        }
    }
}
