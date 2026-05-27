using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;

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
            List<IActivityCapabilityRuntimeReference> runtimeReferences = new();
            HashSet<string> ownerKeys = new(StringComparer.Ordinal);
            HashSet<string> capabilityKeys = new(StringComparer.Ordinal);

            for (int index = 0; index < context.PlayerActorTargets.Count; index++)
            {
                ActivityCapabilityPlayerActorScanTarget target = context.PlayerActorTargets[index];
                if (!target.IsValid)
                {
                    continue;
                }

                string ownerPath = ActivityCapabilityTransformPathUtility.BuildTransformPath(target.ActorRoot.transform);
                string ownerId = ActivityCapabilityInventoryId.DeriveOwnerId(
                    inventoryId,
                    ActivityCapabilityOwnerKind.PlayerActor,
                    ownerPath,
                    target.PlayerActorId);

                if (ownerKeys.Add(ownerId))
                {
                    owners.Add(new ActivityCapabilityOwnerDescriptor(
                        ActivityCapabilityOwnerKind.PlayerActor,
                        ownerId,
                        ownerPath,
                        target.SourceScene,
                        target.SourceContent,
                        context.Source));
                }

                ActorPresentationEndpoint[] endpoints = target.ActorRoot.GetComponentsInChildren<ActorPresentationEndpoint>(true);
                for (int endpointIndex = 0; endpointIndex < endpoints.Length; endpointIndex++)
                {
                    ActorPresentationEndpoint endpoint = endpoints[endpointIndex];
                    if (endpoint == null)
                    {
                        continue;
                    }

                    string componentPath = ActivityCapabilityTransformPathUtility.BuildTransformPath(endpoint.transform);
                    string capabilityId = ActivityCapabilityInventoryId.DeriveCapabilityId(
                        inventoryId,
                        ownerId,
                        ActivityCapabilityKind.PresentationEndpoint,
                        ModuleId,
                        componentPath);

                    if (!capabilityKeys.Add(capabilityId))
                    {
                        continue;
                    }

                    ActorPresentationProfileAsset profile = endpoint.Profile;
                    bool required = profile != null && profile.IsRequired;

                    capabilities.Add(new ActivityCapabilityDescriptor(
                        capabilityId,
                        ActivityCapabilityKind.PresentationEndpoint,
                        ModuleId,
                        ownerId,
                        componentPath,
                        endpoint.GetType().FullName ?? endpoint.GetType().Name,
                        required,
                        priority: 130,
                        policyMetadata: BuildPolicyMetadata(target, endpoint, profile),
                        source: context.Source));

                    runtimeReferences.Add(new ActorPresentationEndpointReference(
                        capabilityId,
                        ownerId,
                        target.PlayerActorId,
                        target.ActorRoot.GetInstanceID(),
                        componentPath,
                        endpoint));
                }
            }

            return new ActivityCapabilityScanResult(ScannerId, owners, capabilities, runtimeReferences, context.Source, context.Reason);
        }

        private static IReadOnlyList<ActivityCapabilityPolicyEntry> BuildPolicyMetadata(
            ActivityCapabilityPlayerActorScanTarget target,
            ActorPresentationEndpoint endpoint,
            ActorPresentationProfileAsset profile)
        {
            string profileId = profile != null ? profile.ProfileId : string.Empty;
            string requiredness = profile != null
                ? profile.Requiredness.ToString()
                : ActorPresentationRequiredness.Unknown.ToString();
            string releasePolicy = profile != null
                ? profile.ReleasePolicy.ToString()
                : ActorPresentationReleasePolicy.Unknown.ToString();
            string endpointId = endpoint != null ? endpoint.EndpointId : string.Empty;

            return new[]
            {
                new ActivityCapabilityPolicyEntry("playerActorId", target.PlayerActorId),
                new ActivityCapabilityPolicyEntry("playerSlotId", target.PlayerSlotId),
                new ActivityCapabilityPolicyEntry("endpointId", endpointId),
                new ActivityCapabilityPolicyEntry("profileId", profileId),
                new ActivityCapabilityPolicyEntry("requiredness", requiredness),
                new ActivityCapabilityPolicyEntry("releasePolicy", releasePolicy),
            };
        }
    }
}
