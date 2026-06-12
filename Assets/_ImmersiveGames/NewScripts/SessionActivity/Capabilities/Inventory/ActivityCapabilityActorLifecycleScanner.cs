using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Contracts;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityActorLifecycleScanner : IActivityCapabilityScanner
    {
        private const string ModuleId = "Actors.Lifecycle";

        public string ScannerId => "activity_capability_actor_lifecycle_scanner.v1";
        public int Order => 240;

        public ActivityCapabilityScanResult Scan(ActivityCapabilityScanContext context)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("Activity capability scan context is invalid.");
            }

            var inventoryId = context.InventoryId;
            List<ActivityCapabilityOwnerDescriptor> owners = new();
            List<ActivityCapabilityDescriptor> capabilities = new();
            List<IActivityCapabilityRuntimeReference> runtimeReferences = new();
            HashSet<string> ownerKeys = new(StringComparer.Ordinal);
            HashSet<string> capabilityKeys = new(StringComparer.Ordinal);

            for (int index = 0; index < context.ActorTargets.Count; index++)
            {
                var target = context.ActorTargets[index];
                if (!target.IsValid)
                {
                    continue;
                }

                var surface = target.CapabilitySurface;
                if (surface == null)
                {
                    throw new InvalidOperationException(
                        $"ActivityCapabilityActorLifecycleScanner requires ActorCapabilitySurface actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}'.");
                }

                surface.RefreshFromLocalActorRoot();

                string ownerPath = !string.IsNullOrWhiteSpace(target.ComponentBasePath)
                    ? target.ComponentBasePath
                    : ActivityCapabilityTransformPathUtility.BuildTransformPath(target.ActorRoot.transform);
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

                AppendResetContributions(
                    inventoryId,
                    ownerId,
                    target,
                    surface.ResetContributionProviders,
                    context,
                    capabilities,
                    runtimeReferences,
                    capabilityKeys);

                AppendSnapshotContributions(
                    inventoryId,
                    ownerId,
                    target,
                    surface.SnapshotContributionProviders,
                    context,
                    capabilities,
                    runtimeReferences,
                    capabilityKeys);

                AppendRestoreContributions(
                    inventoryId,
                    ownerId,
                    target,
                    surface.RestoreContributionProviders,
                    context,
                    capabilities,
                    runtimeReferences,
                    capabilityKeys);

                AppendReleaseContributions(
                    inventoryId,
                    ownerId,
                    target,
                    surface.ReleaseContributionProviders,
                    context,
                    capabilities,
                    runtimeReferences,
                    capabilityKeys);
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

        private static void AppendResetContributions(
            ActivityCapabilityInventoryId inventoryId,
            string ownerId,
            ActorScanTarget target,
            IReadOnlyList<IActorResetContributionProvider> providers,
            ActivityCapabilityScanContext context,
            List<ActivityCapabilityDescriptor> capabilities,
            List<IActivityCapabilityRuntimeReference> runtimeReferences,
            HashSet<string> capabilityKeys)
        {
            if (providers == null || providers.Count == 0)
            {
                return;
            }

            for (int index = 0; index < providers.Count; index++)
            {
                var provider = providers[index];
                if (provider == null)
                {
                    continue;
                }

                string providerTypeName = provider.GetType().FullName ?? provider.GetType().Name;
                var contributionContext = BuildContributionContext(target, provider, context, "reset");
                if (!provider.TryCreateResetContribution(contributionContext, out var contribution))
                {
                    continue;
                }

                if (contribution == null || !contribution.IsValid)
                {
                    throw new InvalidOperationException(
                        $"ActivityCapabilityActorLifecycleScanner found invalid reset contribution provider='{provider.GetType().Name}' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}'.");
                }

                if (provider is not IActorResetEndpoint endpoint)
                {
                    throw new InvalidOperationException(
                        $"ActivityCapabilityActorLifecycleScanner requires IActorResetEndpoint for reset provider='{provider.GetType().Name}' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}'.");
                }

                AppendCapability(
                    inventoryId,
                    ownerId,
                    target,
                    provider,
                    contribution,
                    ActivityCapabilityKind.ResetEndpoint,
                    capabilityKeys,
                    capabilities,
                    runtimeReferences,
                    providerTypeName,
                    new ActorCapabilityResetEndpointReference(
                        ActivityCapabilityInventoryId.DeriveCapabilityId(
                            inventoryId,
                            ownerId,
                            ActivityCapabilityKind.ResetEndpoint,
                            ModuleId,
                            $"{contributionContext.ComponentPath}|providerType={providerTypeName}"),
                        ownerId,
                        new ActorId(target.ActorId),
                        target.ActorInstanceRuntimeId,
                        contributionContext.ComponentPath,
                        providerTypeName,
                        endpoint,
                        contribution));
            }
        }

        private static void AppendSnapshotContributions(
            ActivityCapabilityInventoryId inventoryId,
            string ownerId,
            ActorScanTarget target,
            IReadOnlyList<IActorSnapshotContributionProvider> providers,
            ActivityCapabilityScanContext context,
            List<ActivityCapabilityDescriptor> capabilities,
            List<IActivityCapabilityRuntimeReference> runtimeReferences,
            HashSet<string> capabilityKeys)
        {
            if (providers == null || providers.Count == 0)
            {
                return;
            }

            for (int index = 0; index < providers.Count; index++)
            {
                var provider = providers[index];
                if (provider == null)
                {
                    continue;
                }

                var contributionContext = BuildContributionContext(target, provider, context, "snapshot");
                if (!provider.TryCreateSnapshotContribution(contributionContext, out var contribution))
                {
                    continue;
                }

                if (contribution == null || !contribution.IsValid)
                {
                    throw new InvalidOperationException(
                        $"ActivityCapabilityActorLifecycleScanner found invalid snapshot contribution provider='{provider.GetType().Name}' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}'.");
                }

                if (contribution.SnapshotEndpoint == null)
                {
                    throw new InvalidOperationException(
                        $"ActivityCapabilityActorLifecycleScanner requires SnapshotEndpoint for provider='{provider.GetType().Name}' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}'.");
                }

                AppendCapability(
                    inventoryId,
                    ownerId,
                    target,
                    provider,
                    contribution,
                    ActivityCapabilityKind.SnapshotProvider,
                    capabilityKeys,
                    capabilities,
                    runtimeReferences,
                    string.Empty,
                    new ActorCapabilitySnapshotContributionReference(
                        ActivityCapabilityInventoryId.DeriveCapabilityId(
                            inventoryId,
                            ownerId,
                            ActivityCapabilityKind.SnapshotProvider,
                            ModuleId,
                            contributionContext.ComponentPath),
                        ownerId,
                        new ActorId(target.ActorId),
                        target.ActorInstanceRuntimeId,
                        contributionContext.ComponentPath,
                        contribution));
            }
        }

        private static void AppendRestoreContributions(
            ActivityCapabilityInventoryId inventoryId,
            string ownerId,
            ActorScanTarget target,
            IReadOnlyList<IActorRestoreContributionProvider> providers,
            ActivityCapabilityScanContext context,
            List<ActivityCapabilityDescriptor> capabilities,
            List<IActivityCapabilityRuntimeReference> runtimeReferences,
            HashSet<string> capabilityKeys)
        {
            if (providers == null || providers.Count == 0)
            {
                return;
            }

            for (int index = 0; index < providers.Count; index++)
            {
                var provider = providers[index];
                if (provider == null)
                {
                    continue;
                }

                var contributionContext = BuildContributionContext(target, provider, context, "restore");
                if (!provider.TryCreateRestoreContribution(contributionContext, out var contribution))
                {
                    continue;
                }

                if (contribution == null || !contribution.IsValid)
                {
                    throw new InvalidOperationException(
                        $"ActivityCapabilityActorLifecycleScanner found invalid restore contribution provider='{provider.GetType().Name}' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}'.");
                }

                if (contribution.RestoreEndpoint == null)
                {
                    throw new InvalidOperationException(
                        $"ActivityCapabilityActorLifecycleScanner requires RestoreEndpoint for provider='{provider.GetType().Name}' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}'.");
                }

                AppendCapability(
                    inventoryId,
                    ownerId,
                    target,
                    provider,
                    contribution,
                    ActivityCapabilityKind.SnapshotRestoreEndpoint,
                    capabilityKeys,
                    capabilities,
                    runtimeReferences,
                    string.Empty,
                    new ActorCapabilitySnapshotRestoreContributionReference(
                        ActivityCapabilityInventoryId.DeriveCapabilityId(
                            inventoryId,
                            ownerId,
                            ActivityCapabilityKind.SnapshotRestoreEndpoint,
                            ModuleId,
                            contributionContext.ComponentPath),
                        ownerId,
                        new ActorId(target.ActorId),
                        target.ActorInstanceRuntimeId,
                        contributionContext.ComponentPath,
                        contribution));
            }
        }

        private static void AppendReleaseContributions(
            ActivityCapabilityInventoryId inventoryId,
            string ownerId,
            ActorScanTarget target,
            IReadOnlyList<IActorReleaseContributionProvider> providers,
            ActivityCapabilityScanContext context,
            List<ActivityCapabilityDescriptor> capabilities,
            List<IActivityCapabilityRuntimeReference> runtimeReferences,
            HashSet<string> capabilityKeys)
        {
            if (providers == null || providers.Count == 0)
            {
                return;
            }

            for (int index = 0; index < providers.Count; index++)
            {
                var provider = providers[index];
                if (provider == null)
                {
                    continue;
                }

                var contributionContext = BuildContributionContext(target, provider, context, "release");
                if (!provider.TryCreateReleaseContribution(contributionContext, out var contribution))
                {
                    continue;
                }

                if (contribution == null || !contribution.IsValid)
                {
                    throw new InvalidOperationException(
                        $"ActivityCapabilityActorLifecycleScanner found invalid release contribution provider='{provider.GetType().Name}' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}'.");
                }

                if (contribution.ReleaseEndpoint == null)
                {
                    throw new InvalidOperationException(
                        $"ActivityCapabilityActorLifecycleScanner requires ReleaseEndpoint for provider='{provider.GetType().Name}' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId.Value}'.");
                }

                AppendCapability(
                    inventoryId,
                    ownerId,
                    target,
                    provider,
                    contribution,
                    ActivityCapabilityKind.ReleaseEndpoint,
                    capabilityKeys,
                    capabilities,
                    runtimeReferences,
                    string.Empty,
                    new ActorCapabilityReleaseEndpointReference(
                        ActivityCapabilityInventoryId.DeriveCapabilityId(
                            inventoryId,
                            ownerId,
                            ActivityCapabilityKind.ReleaseEndpoint,
                            ModuleId,
                            contributionContext.ComponentPath),
                        ownerId,
                        new ActorId(target.ActorId),
                        target.ActorInstanceRuntimeId,
                        contributionContext.ComponentPath,
                        contribution));
            }
        }

        private static void AppendCapability(
            ActivityCapabilityInventoryId inventoryId,
            string ownerId,
            ActorScanTarget target,
            object provider,
            IActorCapabilityContribution contribution,
            ActivityCapabilityKind capabilityKind,
            HashSet<string> capabilityKeys,
            List<ActivityCapabilityDescriptor> capabilities,
            List<IActivityCapabilityRuntimeReference> runtimeReferences,
            string capabilityIdentitySuffix,
            IActivityCapabilityRuntimeReference runtimeReference)
        {
            string providerComponentPath = provider is Component component
                ? ActivityCapabilityTransformPathUtility.BuildTransformPath(component.transform)
                : target.ComponentBasePath;
            string capabilityId = ActivityCapabilityInventoryId.DeriveCapabilityId(
                inventoryId,
                ownerId,
                capabilityKind,
                ModuleId,
                string.IsNullOrWhiteSpace(capabilityIdentitySuffix)
                    ? providerComponentPath
                    : $"{providerComponentPath}|providerType={(string.IsNullOrWhiteSpace(capabilityIdentitySuffix) ? string.Empty : capabilityIdentitySuffix.Trim())}");
            if (!capabilityKeys.Add(capabilityId))
            {
                return;
            }

            capabilities.Add(new ActivityCapabilityDescriptor(
                capabilityId,
                capabilityKind,
                ModuleId,
                ownerId,
                providerComponentPath,
                provider.GetType().FullName ?? provider.GetType().Name,
                contribution.Descriptor.Requirement == ActorCapabilityContributionRequirement.Required,
                0,
                BuildPolicyMetadata(target, provider, contribution, providerComponentPath),
                contribution.Descriptor.Source));

            if (runtimeReference is { IsValid: true })
            {
                runtimeReferences.Add(runtimeReference);
            }
        }

        private static ActorCapabilityContributionContext BuildContributionContext(
            ActorScanTarget target,
            object provider,
            ActivityCapabilityScanContext context,
            string operation)
        {
            string componentPath = provider is Component component
                ? ActivityCapabilityTransformPathUtility.BuildTransformPath(component.transform)
                : target.ComponentBasePath;

            return new ActorCapabilityContributionContext(
                context.Identity,
                new ActorId(target.ActorId),
                target.ActorInstanceRuntimeId,
                target.ActorKind,
                target.ActorRole,
                target.ActorScope,
                componentPath,
                context.Source,
                $"{context.Reason}:{operation}");
        }

        private static IReadOnlyList<ActivityCapabilityPolicyEntry> BuildPolicyMetadata(
            ActorScanTarget target,
            object provider,
            IActorCapabilityContribution contribution,
            string componentPath)
        {
            List<ActivityCapabilityPolicyEntry> metadata = new(16)
            {
                new("actorId", target.ActorId),
                new("actorInstanceRuntimeId", target.ActorInstanceRuntimeId.Value),
                new("actorKind", target.ActorKind.ToString()),
                new("actorRole", target.ActorRole.ToString()),
                new("actorScope", target.ActorScope.ToString()),
                new("actorSourceKind", target.ActorSourceKind.ToString()),
                new("actorSourceScene", target.SourceSceneName),
                new("actorRootPath", target.ActorRoot != null ? ActivityCapabilityTransformPathUtility.BuildTransformPath(target.ActorRoot.transform) : string.Empty),
                new("componentPath", componentPath),
                new("providerType", provider.GetType().FullName ?? provider.GetType().Name),
                new("capabilityPhase", contribution.Descriptor.Phase.ToString()),
                new("capabilityRequirement", contribution.Descriptor.Requirement.ToString()),
                new("capabilitySource", contribution.Descriptor.Source),
            };

            if (contribution is IActorResetContribution { SupportedGroups: not null } resetContribution)
            {
                for (int index = 0; index < resetContribution.SupportedGroups.Length; index++)
                {
                    metadata.Add(new($"supportedResetGroup[{index}]", resetContribution.SupportedGroups[index].ToString()));
                }
            }

            if (contribution is IActorSnapshotContribution snapshotContribution)
            {
                metadata.Add(new("schemaId", snapshotContribution.SchemaId));
                metadata.Add(new("schemaVersion", snapshotContribution.SchemaVersion.ToString()));
            }

            if (contribution is IActorRestoreContribution restoreContribution)
            {
                metadata.Add(new("schemaId", restoreContribution.SchemaId));
                metadata.Add(new("schemaVersion", restoreContribution.SchemaVersion.ToString()));
            }

            return metadata;
        }

        private static ActivityCapabilityOwnerKind ResolveOwnerKind(ActorScanTarget target)
        {
            if (target.RuntimeActor is RuntimeSpawnedActor || target.ActorRole == ActorRole.RuntimeSpawnedActor)
            {
                return ActivityCapabilityOwnerKind.RuntimeSpawnedActor;
            }

            if (target.ActorKind == ActorKind.Player)
            {
                return ActivityCapabilityOwnerKind.PlayerActor;
            }

            if (target.ActorKind == ActorKind.Actor)
            {
                return ActivityCapabilityOwnerKind.Actor;
            }

            return ActivityCapabilityOwnerKind.Unsupported;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class ActorCapabilityResetEndpointReference : IActivityCapabilityRuntimeReference
    {
        public ActorCapabilityResetEndpointReference(
            string capabilityId,
            string ownerId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string componentPath,
            string providerType,
            IActorResetEndpoint endpoint,
            IActorResetContribution contribution)
        {
            TypedCapabilityId = new ActorCapabilityId(capabilityId);
            CapabilityId = TypedCapabilityId.Value;
            OwnerId = Normalize(ownerId);
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            TargetId = actorId.Value;
            ComponentPath = Normalize(componentPath);
            ProviderType = Normalize(providerType);
            Endpoint = endpoint;
            Contribution = contribution;
        }

        public string CapabilityId { get; }
        public ActorCapabilityId TypedCapabilityId { get; }
        public string OwnerId { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public string TargetId { get; }
        public string ComponentPath { get; }
        public string ProviderType { get; }
        public IActorResetEndpoint Endpoint { get; }
        public IActorResetContribution Contribution { get; }
        public ActorResetGroup[] SupportedGroups => Contribution?.SupportedGroups ?? Array.Empty<ActorResetGroup>();
        public bool IsValid =>
            TypedCapabilityId.IsValid &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            !string.IsNullOrWhiteSpace(ProviderType) &&
            Endpoint != null &&
            Contribution != null;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public sealed class ActorCapabilitySnapshotContributionReference : IActivityCapabilityRuntimeReference
    {
        public ActorCapabilitySnapshotContributionReference(
            string capabilityId,
            string ownerId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string componentPath,
            IActorSnapshotContribution contribution)
        {
            TypedCapabilityId = new ActorCapabilityId(capabilityId);
            CapabilityId = TypedCapabilityId.Value;
            OwnerId = Normalize(ownerId);
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            TargetId = actorId.Value;
            ComponentPath = Normalize(componentPath);
            Contribution = contribution;
        }

        public string CapabilityId { get; }
        public ActorCapabilityId TypedCapabilityId { get; }
        public string OwnerId { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public string TargetId { get; }
        public string ComponentPath { get; }
        public IActorSnapshotContribution Contribution { get; }
        public IActorCapabilitySnapshotEndpoint SnapshotEndpoint => Contribution?.SnapshotEndpoint;
        public string SchemaId => Contribution?.SchemaId ?? string.Empty;
        public int SchemaVersion => Contribution?.SchemaVersion ?? 0;
        public bool IsValid => TypedCapabilityId.IsValid && ActorId.IsValid && ActorInstanceRuntimeId.IsValid && Contribution != null && SnapshotEndpoint != null;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public sealed class ActorCapabilitySnapshotRestoreContributionReference : IActivityCapabilityRuntimeReference
    {
        public ActorCapabilitySnapshotRestoreContributionReference(
            string capabilityId,
            string ownerId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string componentPath,
            IActorRestoreContribution contribution)
        {
            TypedCapabilityId = new ActorCapabilityId(capabilityId);
            CapabilityId = TypedCapabilityId.Value;
            OwnerId = Normalize(ownerId);
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            TargetId = actorId.Value;
            ComponentPath = Normalize(componentPath);
            Contribution = contribution;
        }

        public string CapabilityId { get; }
        public ActorCapabilityId TypedCapabilityId { get; }
        public string OwnerId { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public string TargetId { get; }
        public string ComponentPath { get; }
        public IActorRestoreContribution Contribution { get; }
        public IActorCapabilityRestoreEndpoint RestoreEndpoint => Contribution?.RestoreEndpoint;
        public string SchemaId => Contribution?.SchemaId ?? string.Empty;
        public int SchemaVersion => Contribution?.SchemaVersion ?? 0;
        public bool IsValid => TypedCapabilityId.IsValid && ActorId.IsValid && ActorInstanceRuntimeId.IsValid && Contribution != null && RestoreEndpoint != null;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public sealed class ActorCapabilityReleaseEndpointReference : IActivityCapabilityRuntimeReference
    {
        public ActorCapabilityReleaseEndpointReference(
            string capabilityId,
            string ownerId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string componentPath,
            IActorReleaseContribution contribution)
        {
            TypedCapabilityId = new ActorCapabilityId(capabilityId);
            CapabilityId = TypedCapabilityId.Value;
            OwnerId = Normalize(ownerId);
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            TargetId = actorId.Value;
            ComponentPath = Normalize(componentPath);
            Contribution = contribution;
        }

        public string CapabilityId { get; }
        public ActorCapabilityId TypedCapabilityId { get; }
        public string OwnerId { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public string TargetId { get; }
        public string ComponentPath { get; }
        public IActorReleaseContribution Contribution { get; }
        public IActorCapabilityReleaseEndpoint ReleaseEndpoint => Contribution?.ReleaseEndpoint;
        public bool IsValid => TypedCapabilityId.IsValid && ActorId.IsValid && ActorInstanceRuntimeId.IsValid && Contribution != null && ReleaseEndpoint != null;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
