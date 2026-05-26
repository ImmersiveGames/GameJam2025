using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityObjectCapabilityScanner : IActivityCapabilityScanner
    {
        private const string ModuleId = "SessionActivity.ActivityObject";
        public string ScannerId => "activity_object_capability_scanner.v1";
        public int Order => 200;

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

            for (int index = 0; index < context.ActivityObjectTargets.Count; index++)
            {
                ActivityObjectCapabilityScanTarget target = context.ActivityObjectTargets[index];
                if (!target.IsValid)
                {
                    continue;
                }

                ActivityCapabilityOwnerKind ownerKind = ResolveOwnerKind(target.Contribution.ContributorKind);
                string ownerPath = target.HasTargetObjectPath
                    ? target.TargetObjectPath
                    : BuildTransformPath(target.TargetObject.transform);
                string ownerSource = target.Contribution.TargetId;
                string ownerId = ActivityCapabilityInventoryId.DeriveOwnerId(inventoryId, ownerKind, ownerPath, ownerSource);

                if (ownerKeys.Add(ownerId))
                {
                    owners.Add(new ActivityCapabilityOwnerDescriptor(
                        ownerKind,
                        ownerId,
                        ownerPath,
                        target.Contribution.SceneName,
                        target.Contribution.ContentProfileId,
                        context.Source));
                }

                MonoBehaviour[] behaviours = ResolveBehaviours(target);
                for (int behaviourIndex = 0; behaviourIndex < behaviours.Length; behaviourIndex++)
                {
                    MonoBehaviour behaviour = behaviours[behaviourIndex];
                    if (behaviour == null)
                    {
                        continue;
                    }

                    string componentPath = BuildTransformPath(behaviour.transform);
                    string componentType = behaviour.GetType().FullName ?? behaviour.GetType().Name;
                    bool required = target.Contribution.Requiredness == ActivitySetupRequirementRequiredness.Required;
                    IReadOnlyList<ActivityCapabilityPolicyEntry> metadata = BuildPolicyMetadata(target);

                    TryAppendCapability(capabilities, runtimeReferences, capabilityKeys, inventoryId, ownerId, target.Contribution.TargetId, ActivityCapabilityKind.ResetEndpoint, componentPath, componentType, required, 100, metadata, context.Source, behaviour, behaviour is IActivityObjectResetEndpoint);
                    TryAppendCapability(capabilities, runtimeReferences, capabilityKeys, inventoryId, ownerId, target.Contribution.TargetId, ActivityCapabilityKind.SnapshotProvider, componentPath, componentType, required, 200, metadata, context.Source, behaviour, behaviour is IActivityObjectSnapshotProvider);
                    TryAppendCapability(capabilities, runtimeReferences, capabilityKeys, inventoryId, ownerId, target.Contribution.TargetId, ActivityCapabilityKind.SnapshotRestoreEndpoint, componentPath, componentType, required, 300, metadata, context.Source, behaviour, behaviour is IActivityObjectSnapshotRestoreEndpoint);
                    TryAppendCapability(capabilities, runtimeReferences, capabilityKeys, inventoryId, ownerId, target.Contribution.TargetId, ActivityCapabilityKind.ReleaseEndpoint, componentPath, componentType, required, 400, metadata, context.Source, behaviour, behaviour is IActivityObjectReleaseEndpoint);
                }
            }

            return new ActivityCapabilityScanResult(ScannerId, owners, capabilities, runtimeReferences, context.Source, context.Reason);
        }

        private static void TryAppendCapability(
            List<ActivityCapabilityDescriptor> capabilities,
            List<IActivityCapabilityRuntimeReference> runtimeReferences,
            HashSet<string> capabilityKeys,
            ActivityCapabilityInventoryId inventoryId,
            string ownerId,
            string targetId,
            ActivityCapabilityKind capabilityKind,
            string componentPath,
            string componentType,
            bool required,
            int priority,
            IReadOnlyList<ActivityCapabilityPolicyEntry> metadata,
            string source,
            MonoBehaviour behaviour,
            bool supportsKind)
        {
            if (!supportsKind)
            {
                return;
            }

            string capabilityId = ActivityCapabilityInventoryId.DeriveCapabilityId(inventoryId, ownerId, capabilityKind, ModuleId, componentPath);
            if (!capabilityKeys.Add(capabilityId))
            {
                return;
            }

            capabilities.Add(new ActivityCapabilityDescriptor(
                capabilityId,
                capabilityKind,
                ModuleId,
                ownerId,
                componentPath,
                componentType,
                required,
                priority,
                metadata,
                source));

            if (capabilityKind == ActivityCapabilityKind.ResetEndpoint &&
                behaviour is IActivityObjectResetEndpoint endpoint)
            {
                runtimeReferences.Add(new ActivityObjectResetEndpointReference(
                    capabilityId,
                    ownerId,
                    targetId,
                    componentPath,
                    endpoint));
            }

            if (capabilityKind == ActivityCapabilityKind.SnapshotProvider &&
                behaviour is IActivityObjectSnapshotProvider provider)
            {
                runtimeReferences.Add(new ActivityObjectSnapshotProviderReference(
                    capabilityId,
                    ownerId,
                    targetId,
                    componentPath,
                    provider));
            }

            if (capabilityKind == ActivityCapabilityKind.SnapshotRestoreEndpoint &&
                behaviour is IActivityObjectSnapshotRestoreEndpoint restoreEndpoint)
            {
                runtimeReferences.Add(new ActivityObjectSnapshotRestoreEndpointReference(
                    capabilityId,
                    ownerId,
                    targetId,
                    componentPath,
                    restoreEndpoint));
            }

            if (capabilityKind == ActivityCapabilityKind.ReleaseEndpoint &&
                behaviour is IActivityObjectReleaseEndpoint releaseEndpoint)
            {
                runtimeReferences.Add(new ActivityObjectReleaseEndpointReference(
                    capabilityId,
                    ownerId,
                    targetId,
                    componentPath,
                    releaseEndpoint));
            }
        }

        private static MonoBehaviour[] ResolveBehaviours(ActivityObjectCapabilityScanTarget target)
        {
            return target.IncludeChildrenForEndpointDiscovery
                ? target.TargetObject.GetComponentsInChildren<MonoBehaviour>(true)
                : target.TargetObject.GetComponents<MonoBehaviour>();
        }

        private static ActivityCapabilityOwnerKind ResolveOwnerKind(ActivityObjectContributorKind contributorKind)
        {
            return contributorKind == ActivityObjectContributorKind.AdapterProxy
                ? ActivityCapabilityOwnerKind.SceneContributor
                : ActivityCapabilityOwnerKind.ActivityObject;
        }

        private static IReadOnlyList<ActivityCapabilityPolicyEntry> BuildPolicyMetadata(ActivityObjectCapabilityScanTarget target)
        {
            ActivityObjectContributionReport contribution = target.Contribution;
            List<ActivityCapabilityPolicyEntry> metadata = new(8)
            {
                new("targetId", contribution.TargetId),
                new("roleId", contribution.RoleId),
                new("contributorKind", contribution.ContributorKind.ToString()),
                new("requiredness", contribution.Requiredness.ToString()),
                new("includeChildrenForEndpointDiscovery", target.IncludeChildrenForEndpointDiscovery ? "true" : "false"),
            };

            for (int index = 0; index < contribution.SupportedResetGroups.Count; index++)
            {
                metadata.Add(new($"supportedResetGroup[{index}]", contribution.SupportedResetGroups[index].ToString()));
            }

            for (int index = 0; index < contribution.SupportedReleaseKinds.Count; index++)
            {
                metadata.Add(new($"supportedReleaseKind[{index}]", contribution.SupportedReleaseKinds[index].ToString()));
            }

            return metadata;
        }

        private static string BuildTransformPath(Transform target)
        {
            if (target == null)
            {
                return "<null>";
            }

            string path = target.name;
            Transform current = target.parent;
            while (current != null)
            {
                path = $"{current.name}/{path}";
                current = current.parent;
            }

            return path;
        }
    }
}
