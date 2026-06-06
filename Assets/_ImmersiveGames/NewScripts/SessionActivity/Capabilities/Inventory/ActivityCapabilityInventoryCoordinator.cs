using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityCapabilityInventoryBuildResult
    {
        public ActivityCapabilityInventoryBuildResult(
            ActivityCapabilityInventory inventory,
            ActivityCapabilityInventoryValidationResult validation,
            int objectTargetCount,
            int unresolvedReportCount,
            string source,
            string reason)
        {
            Inventory = inventory;
            Validation = validation;
            ObjectTargetCount = objectTargetCount < 0 ? 0 : objectTargetCount;
            UnresolvedReportCount = unresolvedReportCount < 0 ? 0 : unresolvedReportCount;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActivityCapabilityInventory Inventory { get; }
        public ActivityCapabilityInventoryValidationResult Validation { get; }
        public int ObjectTargetCount { get; }
        public int UnresolvedReportCount { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => Inventory.IsValid && Validation.IsValid;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class ActivityCapabilityInventoryCoordinator
    {
        private readonly ActivityObjectCapabilityScanTargetAdapter _objectTargetAdapter;
        private readonly ActivityObjectCapabilityScanner _objectScanner;
        private readonly ActivityCapabilityInventoryBuilder _inventoryBuilder;
        private readonly ActivityCapabilityInventoryValidator _inventoryValidator;

        public ActivityCapabilityInventoryCoordinator()
        {
            _objectTargetAdapter = new ActivityObjectCapabilityScanTargetAdapter();
            _objectScanner = new ActivityObjectCapabilityScanner();
            IPlayerActorCapabilityIdentityResolver playerIdentityResolver = new PlayerActorCapabilityIdentityResolver();

            ActivityCapabilityScannerRegistry scannerRegistry = new ActivityCapabilityScannerRegistry();
            scannerRegistry.Register(_objectScanner);
            scannerRegistry.Register(new ActivityCapabilityPermissionScanner(playerIdentityResolver));
            scannerRegistry.Register(new ActivityCapabilityActorPresentationScanner());
            scannerRegistry.Register(new ActivityCapabilityActorAttributeScanner());
            scannerRegistry.Register(new ActivityCapabilityProjectileEmitterScanner(playerIdentityResolver));
            scannerRegistry.Register(new ActivityCapabilityCameraTargetScanner(playerIdentityResolver));

            _inventoryBuilder = new ActivityCapabilityInventoryBuilder(scannerRegistry);
            _inventoryValidator = new ActivityCapabilityInventoryValidator();
        }

        public string ActivityObjectScannerId => _objectScanner.ScannerId;

        public ActivityCapabilityInventoryBuildResult BuildForEntry(
            SessionActivityIdentity identity,
            ActivityObjectContributorDiscoveryResult activityObjectDiscovery,
            IReadOnlyList<ActorScanTarget> actorTargets,
            string source,
            string reason)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("ActivityCapabilityInventoryCoordinator requires a valid SessionActivityIdentity.");
            }

            IReadOnlyList<ActivityObjectCapabilityScanTarget> objectTargets = Array.Empty<ActivityObjectCapabilityScanTarget>();
            int unresolvedReportCount = 0;
            if (activityObjectDiscovery.IsValid)
            {
                ActivityObjectCapabilityScanTargetAdaptationResult objectAdaptation = _objectTargetAdapter.Adapt(
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

            ActivityCapabilityInventory inventory = _inventoryBuilder.Build(scanContext);
            ActivityCapabilityInventoryValidationResult validation = _inventoryValidator.Validate(inventory, source, reason);
            return new ActivityCapabilityInventoryBuildResult(
                inventory,
                validation,
                objectTargets.Count,
                unresolvedReportCount,
                source,
                reason);
        }
    }
}
