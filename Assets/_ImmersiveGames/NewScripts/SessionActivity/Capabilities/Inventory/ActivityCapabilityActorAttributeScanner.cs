using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;

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

                if (target.CapabilitySurface == null)
                {
                    throw new InvalidOperationException(
                        $"ActivityCapabilityActorAttributeScanner requires ActorCapabilitySurface actorId='{target.ActorId}' actorInstanceId='{target.ActorInstanceId.Value}'.");
                }

                ActorAttributeEndpoint endpoint = target.CapabilitySurface.AttributeEndpoint;
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

                string componentPath = ActivityCapabilityTransformPathUtility.BuildTransformPath(endpoint.transform);
                string capabilityId = ActivityCapabilityInventoryId.DeriveCapabilityId(
                    inventoryId,
                    ownerId,
                    ActivityCapabilityKind.AttributeEndpoint,
                    ModuleId,
                    componentPath);

                if (!capabilityKeys.Add(capabilityId))
                {
                    continue;
                }

                capabilities.Add(new ActivityCapabilityDescriptor(
                    capabilityId,
                    ActivityCapabilityKind.AttributeEndpoint,
                    ModuleId,
                    ownerId,
                    componentPath,
                    endpoint.GetType().FullName ?? endpoint.GetType().Name,
                    required: true,
                    priority: 125,
                    policyMetadata: new[]
                    {
                        new ActivityCapabilityPolicyEntry("actorId", target.ActorId),
                        new ActivityCapabilityPolicyEntry("actorKind", ResolveActorKindLabel(target)),
                        new ActivityCapabilityPolicyEntry("actorRole", target.ActorRole.ToString()),
                        new ActivityCapabilityPolicyEntry("actorScope", target.ActorScope.ToString()),
                    },
                    source: context.Source));

                runtimeReferences.Add(new ActorAttributeEndpointReference(
                    capabilityId,
                    ownerId,
                    target.ActorInstanceId,
                    target.ActorId,
                    target.ActorKind,
                    target.ActorRole,
                    target.ActorScope,
                    componentPath,
                    endpoint));
            }

            return new ActivityCapabilityScanResult(ScannerId, owners, capabilities, runtimeReferences, context.Source, context.Reason);
        }

        private static ActivityCapabilityOwnerKind ResolveOwnerKind(ActorScanTarget target)
        {
            return target.IsValid
                ? ActivityCapabilityOwnerKind.Actor
                : ActivityCapabilityOwnerKind.Unsupported;
        }

        private static string ResolveActorKindLabel(ActorScanTarget target)
        {
            if (target.ActorRole == ActorRole.PrimaryPlayer || target.ActorRole == ActorRole.SupportingPlayer)
            {
                return "Player";
            }

            if (target.ActorRole == ActorRole.SceneAuthoredNonPlayer)
            {
                return "NonPlayer";
            }

            return target.RuntimeActor != null ? target.RuntimeActor.GetType().Name : target.ActorKind.ToString();
        }
    }
}
