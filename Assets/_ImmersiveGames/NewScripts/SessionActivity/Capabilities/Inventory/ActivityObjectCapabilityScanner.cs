using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
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
            List<IActivityObjectLifecycleContribution> lifecycleContributions = new(4);

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
                    : ActivityCapabilityTransformPathUtility.BuildTransformPath(target.TargetObject.transform);
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

                ActivityObjectLifecycleContributionContext lifecycleContext = new(
                    default,
                    target.Contribution.TargetId,
                    target.Contribution.RoleId,
                    target.Contribution.ContributorKind,
                    target.Contribution.Requiredness,
                    context.Source,
                    context.Reason);
                MonoBehaviour[] behaviours = ResolveBehaviours(target);
                for (int behaviourIndex = 0; behaviourIndex < behaviours.Length; behaviourIndex++)
                {
                    MonoBehaviour behaviour = behaviours[behaviourIndex];
                    if (behaviour is not IActivityObjectLifecycleContributionProvider provider)
                    {
                        continue;
                    }

                    lifecycleContributions.Clear();
                    provider.CollectActivityObjectLifecycleContributions(lifecycleContext, lifecycleContributions);
                    for (int contributionIndex = 0; contributionIndex < lifecycleContributions.Count; contributionIndex++)
                    {
                        TryAppendLifecycleContribution(
                            capabilities,
                            runtimeReferences,
                            capabilityKeys,
                            inventoryId,
                            ownerId,
                            target.Contribution.TargetId,
                            BuildPolicyMetadata(target),
                            target.Contribution.Requiredness == ActivitySetupRequirementRequiredness.Required,
                            context.Source,
                            behaviour,
                            lifecycleContributions[contributionIndex]);
                    }
                }
            }

            return new ActivityCapabilityScanResult(
                ScannerId,
                owners,
                capabilities,
                runtimeReferences,
                Array.Empty<ActorCameraBindingContribution>(),
                Array.Empty<ActorAttributeSetupContribution>(),
                Array.Empty<ActorPresentationSetupContribution>(),
                Array.Empty<ActivityPermissionReceiverContribution>(),
                context.Source,
                context.Reason);
        }

        private static void TryAppendLifecycleContribution(
            List<ActivityCapabilityDescriptor> capabilities,
            List<IActivityCapabilityRuntimeReference> runtimeReferences,
            HashSet<string> capabilityKeys,
            ActivityCapabilityInventoryId inventoryId,
            string ownerId,
            string targetId,
            IReadOnlyList<ActivityCapabilityPolicyEntry> metadata,
            bool required,
            string source,
            MonoBehaviour providerBehaviour,
            IActivityObjectLifecycleContribution contribution)
        {
            if (contribution == null || !contribution.IsValid || providerBehaviour == null)
            {
                return;
            }

            if (!TryResolveCapabilityKind(contribution, out ActivityCapabilityKind capabilityKind))
            {
                return;
            }

            string componentPath = ActivityCapabilityTransformPathUtility.BuildTransformPath(providerBehaviour.transform);
            string componentType = providerBehaviour.GetType().FullName ?? providerBehaviour.GetType().Name;
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
                contribution.Priority,
                metadata,
                source));

            if (contribution is IActivityObjectResetContribution resetContribution && resetContribution.ResetEndpoint != null)
            {
                runtimeReferences.Add(new ActivityObjectResetEndpointReference(
                    capabilityId,
                    ownerId,
                    targetId,
                    componentPath,
                    resetContribution.ResetEndpoint));
            }

            if (contribution is IActivityObjectSnapshotContribution snapshotContribution && snapshotContribution.SnapshotProvider != null)
            {
                runtimeReferences.Add(new ActivityObjectSnapshotProviderReference(
                    capabilityId,
                    ownerId,
                    targetId,
                    componentPath,
                    snapshotContribution.SnapshotProvider));
            }

            if (contribution is IActivityObjectSnapshotRestoreContribution restoreContribution && restoreContribution.RestoreEndpoint != null)
            {
                runtimeReferences.Add(new ActivityObjectSnapshotRestoreEndpointReference(
                    capabilityId,
                    ownerId,
                    targetId,
                    componentPath,
                    restoreContribution.RestoreEndpoint));
            }

            if (contribution is IActivityObjectReleaseContribution releaseContribution && releaseContribution.ReleaseEndpoint != null)
            {
                runtimeReferences.Add(new ActivityObjectReleaseEndpointReference(
                    capabilityId,
                    ownerId,
                    targetId,
                    componentPath,
                    releaseContribution.ReleaseEndpoint));
            }
        }

        private static bool TryResolveCapabilityKind(
            IActivityObjectLifecycleContribution contribution,
            out ActivityCapabilityKind capabilityKind)
        {
            switch (contribution.Kind)
            {
                case ActivityObjectLifecycleContributionKind.Reset:
                    capabilityKind = ActivityCapabilityKind.ResetEndpoint;
                    return true;
                case ActivityObjectLifecycleContributionKind.Snapshot:
                    capabilityKind = ActivityCapabilityKind.SnapshotProvider;
                    return true;
                case ActivityObjectLifecycleContributionKind.SnapshotRestore:
                    capabilityKind = ActivityCapabilityKind.SnapshotRestoreEndpoint;
                    return true;
                case ActivityObjectLifecycleContributionKind.Release:
                    capabilityKind = ActivityCapabilityKind.ReleaseEndpoint;
                    return true;
                default:
                    capabilityKind = ActivityCapabilityKind.Unknown;
                    return false;
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
                new("resetBoundaryEligibility", ActivityResetBoundaryEligibilityFormatter.Format(contribution.ResetBoundaryEligibility)),
                new("includeChildrenForEndpointDiscovery", target.IncludeChildrenForEndpointDiscovery ? "true" : "false"),
            };

            for (int index = 0; index < contribution.SupportedReleaseKinds.Count; index++)
            {
                metadata.Add(new($"supportedReleaseKind[{index}]", contribution.SupportedReleaseKinds[index].ToString()));
            }

            return metadata;
        }
    }
}
