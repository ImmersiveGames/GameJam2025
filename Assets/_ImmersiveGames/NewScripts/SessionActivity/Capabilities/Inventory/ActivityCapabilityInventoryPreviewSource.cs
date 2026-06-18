using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    internal sealed class ActivityCapabilityInventoryPreviewSource : IActivityCapabilityInventoryPreviewSource
    {
        private readonly ActivityObjectCapabilityScanTargetAdapter _objectTargetAdapter;
        private readonly ActivityObjectCapabilityScanner _objectScanner;
        private readonly ActivityCapabilityActorLifecycleScanner _actorLifecycleScanner;
        private readonly ActivityCapabilityInventoryBuilder _inventoryBuilder;

        public ActivityCapabilityInventoryPreviewSource()
        {
            _objectTargetAdapter = new ActivityObjectCapabilityScanTargetAdapter();
            _objectScanner = new ActivityObjectCapabilityScanner();
            _actorLifecycleScanner = new ActivityCapabilityActorLifecycleScanner();

            var scannerRegistry = new ActivityCapabilityScannerRegistry();
            scannerRegistry.Register(_objectScanner);
            scannerRegistry.Register(_actorLifecycleScanner);
            scannerRegistry.Register(new ActivityCapabilityPermissionScanner());
            scannerRegistry.Register(new ActivityCapabilityActorPresentationScanner());
            scannerRegistry.Register(new ActivityCapabilityActorAttributeScanner());
            scannerRegistry.Register(new ActivityCapabilityCameraTargetScanner());

            _inventoryBuilder = new ActivityCapabilityInventoryBuilder(scannerRegistry);
        }

        public string ActivityObjectScannerId => _objectScanner.ScannerId;
        public string ActorLifecycleScannerId => _actorLifecycleScanner.ScannerId;

        public ActivityCapabilityInventoryBuildResult BuildForEntry(
            SessionActivityIdentity identity,
            ActivityObjectContributorDiscoveryResult activityObjectDiscovery,
            IReadOnlyList<ActorScanTarget> actorTargets,
            string source,
            string reason)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("ActivityCapabilityInventoryPreviewSource requires a valid SessionActivityIdentity.");
            }

            IReadOnlyList<ActivityObjectCapabilityScanTarget> objectTargets = Array.Empty<ActivityObjectCapabilityScanTarget>();
            int unresolvedReportCount = 0;
            if (activityObjectDiscovery.IsValid)
            {
                var objectAdaptation = _objectTargetAdapter.Adapt(
                    activityObjectDiscovery,
                    source,
                    reason);
                objectTargets = objectAdaptation.Targets;
                unresolvedReportCount = objectAdaptation.UnresolvedReports.Count;
            }

            ActivityCapabilityScanContext scanContext = new(
                identity,
                objectTargets,
                actorTargets ?? Array.Empty<ActorScanTarget>(),
                source,
                reason);

            var inventory = _inventoryBuilder.Build(
                scanContext,
                out IReadOnlyList<ActorCameraBindingContribution> cameraBindingContributions,
                out IReadOnlyList<ActorAttributeSetupContribution> attributeSetupContributions,
                out IReadOnlyList<ActorPresentationSetupContribution> presentationSetupContributions,
                out IReadOnlyList<ActivityPermissionReceiverContribution> permissionReceiverContributions);
            CollectLifecycleCapabilitySummaries(
                inventory,
                out int activityObjectLifecycleCapabilityCount,
                out string activityObjectLifecycleCapabilityKindsSummary,
                out int actorLifecycleCapabilityCount,
                out string actorLifecycleCapabilityKindsSummary);

            return new ActivityCapabilityInventoryBuildResult(
                inventory,
                cameraBindingContributions,
                attributeSetupContributions,
                presentationSetupContributions,
                permissionReceiverContributions,
                activityObjectLifecycleCapabilityCount,
                activityObjectLifecycleCapabilityKindsSummary,
                actorLifecycleCapabilityCount,
                actorLifecycleCapabilityKindsSummary,
                objectTargets.Count,
                unresolvedReportCount,
                source,
                reason);
        }

        private static void CollectLifecycleCapabilitySummaries(
            ActivityCapabilityInventory inventory,
            out int activityObjectLifecycleCapabilityCount,
            out string activityObjectLifecycleCapabilityKindsSummary,
            out int actorLifecycleCapabilityCount,
            out string actorLifecycleCapabilityKindsSummary)
        {
            List<ActivityCapabilityDescriptor> activityObjectLifecycleCapabilities = new();
            List<ActivityCapabilityDescriptor> actorLifecycleCapabilities = new();
            if (inventory.IsValid && inventory.Capabilities.Count > 0 && inventory.Owners.Count > 0)
            {
                Dictionary<string, ActivityCapabilityOwnerKind> ownerKindsById = new(StringComparer.Ordinal);
                for (int index = 0; index < inventory.Owners.Count; index++)
                {
                    var owner = inventory.Owners[index];
                    if (!owner.IsValid || string.IsNullOrWhiteSpace(owner.OwnerId))
                    {
                        continue;
                    }

                    if (!ownerKindsById.ContainsKey(owner.OwnerId))
                    {
                        ownerKindsById.Add(owner.OwnerId, owner.OwnerKind);
                    }
                }

                for (int index = 0; index < inventory.Capabilities.Count; index++)
                {
                    var capability = inventory.Capabilities[index];
                    if (!IsLifecycleCapability(capability.CapabilityKind) ||
                        string.IsNullOrWhiteSpace(capability.OwnerId) ||
                        !ownerKindsById.TryGetValue(capability.OwnerId, out var ownerKind))
                    {
                        continue;
                    }

                    if (IsActivityObjectLifecycleOwnerKind(ownerKind))
                    {
                        activityObjectLifecycleCapabilities.Add(capability);
                    }
                    else if (IsActorLifecycleOwnerKind(ownerKind))
                    {
                        actorLifecycleCapabilities.Add(capability);
                    }
                }
            }

            activityObjectLifecycleCapabilityCount = activityObjectLifecycleCapabilities.Count;
            activityObjectLifecycleCapabilityKindsSummary = FormatCapabilityKindsSummary(activityObjectLifecycleCapabilities);
            actorLifecycleCapabilityCount = actorLifecycleCapabilities.Count;
            actorLifecycleCapabilityKindsSummary = FormatCapabilityKindsSummary(actorLifecycleCapabilities);
        }

        private static bool IsLifecycleCapability(ActivityCapabilityKind kind)
        {
            return kind == ActivityCapabilityKind.ResetEndpoint ||
                kind == ActivityCapabilityKind.SnapshotProvider ||
                kind == ActivityCapabilityKind.SnapshotRestoreEndpoint ||
                kind == ActivityCapabilityKind.ReleaseEndpoint;
        }

        private static bool IsActivityObjectLifecycleOwnerKind(ActivityCapabilityOwnerKind ownerKind)
        {
            return ownerKind == ActivityCapabilityOwnerKind.ActivityObject ||
                ownerKind == ActivityCapabilityOwnerKind.SceneContributor;
        }

        private static bool IsActorLifecycleOwnerKind(ActivityCapabilityOwnerKind ownerKind)
        {
            return ownerKind == ActivityCapabilityOwnerKind.Actor ||
                ownerKind == ActivityCapabilityOwnerKind.RuntimeSpawnedActor;
        }

        private static string FormatCapabilityKindsSummary(IReadOnlyList<ActivityCapabilityDescriptor> capabilities)
        {
            if (capabilities == null || capabilities.Count == 0)
            {
                return "<none>";
            }

            Dictionary<ActivityCapabilityKind, int> countsByKind = new();
            for (int index = 0; index < capabilities.Count; index++)
            {
                var kind = capabilities[index].CapabilityKind;
                countsByKind.TryGetValue(kind, out int count);
                countsByKind[kind] = count + 1;
            }

            List<ActivityCapabilityKind> kinds = new(countsByKind.Keys);
            kinds.Sort();
            List<string> segments = new(kinds.Count);
            for (int index = 0; index < kinds.Count; index++)
            {
                var kind = kinds[index];
                segments.Add($"{kind}:{countsByKind[kind]}");
            }

            return string.Join(",", segments);
        }
    }
}
