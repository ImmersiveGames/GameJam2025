using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Players.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityCameraTargetScanner : IActivityCapabilityScanner
    {
        private const string ModuleId = "SessionActivity.CameraTarget";

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

            for (int index = 0; index < context.PlayerActorTargets.Count; index++)
            {
                ActivityCapabilityPlayerActorScanTarget target = context.PlayerActorTargets[index];
                if (!target.IsValid)
                {
                    continue;
                }

                string ownerPath = BuildTransformPath(target.ActorRoot.transform);
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

                PlayerCameraEndpoint[] endpoints = target.ActorRoot.GetComponentsInChildren<PlayerCameraEndpoint>(true);
                for (int endpointIndex = 0; endpointIndex < endpoints.Length; endpointIndex++)
                {
                    PlayerCameraEndpoint endpoint = endpoints[endpointIndex];
                    if (endpoint == null || !endpoint.HasValidTargets)
                    {
                        continue;
                    }

                    string componentPath = BuildTransformPath(endpoint.transform);
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
                            new ActivityCapabilityPolicyEntry("playerActorId", target.PlayerActorId),
                            new ActivityCapabilityPolicyEntry("playerSlotId", target.PlayerSlotId),
                        },
                        source: context.Source));

                    runtimeReferences.Add(new ActivityCameraTargetReference(
                        capabilityId,
                        ownerId,
                        target.PlayerActorId,
                        target.PlayerSlotId,
                        componentPath,
                        endpoint.FollowTarget,
                        endpoint.LookAtTarget));
                }
            }

            return new ActivityCapabilityScanResult(ScannerId, owners, capabilities, runtimeReferences, context.Source, context.Reason);
        }

        private static string BuildTransformPath(UnityEngine.Transform target)
        {
            if (target == null)
            {
                return "<null>";
            }

            string path = target.name;
            UnityEngine.Transform current = target.parent;
            while (current != null)
            {
                path = $"{current.name}/{path}";
                current = current.parent;
            }

            return path;
        }
    }
}
