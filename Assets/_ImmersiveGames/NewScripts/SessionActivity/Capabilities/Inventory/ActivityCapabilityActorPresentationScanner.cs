using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityActorPresentationScanner : IActivityCapabilityScanner
    {
        private const string ModuleId = "Actors.Presentation";

        public string ScannerId => "activity_capability_actor_presentation_scanner.v1";
        public int Order => 310;

        public ActivityCapabilityScanResult Scan(ActivityCapabilityScanContext context)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("Activity capability scan context is invalid.");
            }

            ActivityCapabilityInventoryId inventoryId = context.InventoryId;
            List<ActivityCapabilityOwnerDescriptor> owners = new();
            List<ActivityCapabilityDescriptor> capabilities = new();
            List<ActorPresentationSetupContribution> presentationContributions = new();
            HashSet<string> ownerKeys = new(StringComparer.Ordinal);
            HashSet<string> contributionKeys = new(StringComparer.Ordinal);

            for (int index = 0; index < context.ActorTargets.Count; index++)
            {
                ActorScanTarget target = context.ActorTargets[index];
                if (!target.IsValid)
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

                if (target.CapabilitySurface == null)
                {
                    throw new InvalidOperationException(
                        $"ActivityCapabilityActorPresentationScanner requires ActorCapabilitySurface actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}'.");
                }

                ActorPresentationEndpoint endpoint = target.CapabilitySurface.PresentationEndpoint;
                if (endpoint == null)
                {
                    continue;
                }

                if (!ActorPresentationSetupContributionBuilder.TryBuild(
                        context.Identity,
                        target,
                        endpoint,
                        context.Source,
                        context.Reason,
                        out ActorPresentationSetupContribution contribution))
                {
                    continue;
                }

                string contributionKey = $"{contribution.ActorInstanceRuntimeId}|{contribution.ComponentPath}";
                if (!contributionKeys.Add(contributionKey))
                {
                    continue;
                }

                presentationContributions.Add(contribution);
            }

            return new ActivityCapabilityScanResult(
                ScannerId,
                owners,
                capabilities,
                Array.Empty<IActivityCapabilityRuntimeReference>(),
                Array.Empty<ActorCameraBindingContribution>(),
                Array.Empty<ActorAttributeSetupContribution>(),
                presentationContributions,
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
