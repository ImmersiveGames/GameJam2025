using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryPoolPreparationStage
    {
        public static void Execute(
            SessionActivityIdentity identity,
            ActorInventoryFeedResult actorInventoryFeed,
            IPoolService poolService,
            string source,
            string reason)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryPoolPreparationStage requires a valid session activity identity.");
            }

            if (!actorInventoryFeed.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryPoolPreparationStage requires a valid actor inventory feed result.");
            }

            poolService = poolService ?? throw new ArgumentNullException(nameof(poolService));

            IReadOnlyList<ResolvedPoolDependency> resolvedDependencies = CollectDependencies(
                actorInventoryFeed.ActorInstances,
                out int providerCount);

            DebugUtility.LogVerbose(
                typeof(ActivityEntryPoolPreparationStage),
                $"event='ActivityEntryPoolPreparationStarted' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' providerCount='{providerCount}' resolvedPoolCount='{resolvedDependencies.Count}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);

            for (int index = 0; index < resolvedDependencies.Count; index++)
            {
                ResolvedPoolDependency dependency = resolvedDependencies[index];
                PoolDefinitionAsset poolDefinition = dependency.PoolDefinition;

                DebugUtility.LogVerbose(
                    typeof(ActivityEntryPoolPreparationStage),
                    $"event='ActivityEntryPoolDependencyResolved' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' providerType='{dependency.ProviderType}' poolDefinition='{poolDefinition.name}' poolLabel='{Normalize(poolDefinition.PoolLabel)}' registrationMode='{poolDefinition.RegistrationMode}' prewarm='{poolDefinition.Prewarm}' initialSize='{poolDefinition.InitialSize}' lifetimeScope='{poolDefinition.LifetimeScope}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                    DebugUtility.Colors.Info);

                if (poolDefinition.RegistrationMode != PoolRegistrationMode.ActivityEntry)
                {
                    DebugUtility.LogVerbose(
                        typeof(ActivityEntryPoolPreparationStage),
                        $"event='ActivityEntryPoolPreparationSkipped' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' providerType='{dependency.ProviderType}' poolDefinition='{poolDefinition.name}' poolLabel='{Normalize(poolDefinition.PoolLabel)}' registrationMode='{poolDefinition.RegistrationMode}' reason='timing_not_activity_entry' source='{Normalize(source)}' reasonText='{Normalize(reason)}'.",
                        DebugUtility.Colors.Info);
                    continue;
                }

                poolService.EnsureRegistered(poolDefinition);

                DebugUtility.LogVerbose(
                    typeof(ActivityEntryPoolPreparationStage),
                    $"event='ActivityEntryPoolPrepared' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' providerType='{dependency.ProviderType}' poolDefinition='{poolDefinition.name}' poolLabel='{Normalize(poolDefinition.PoolLabel)}' registrationMode='{poolDefinition.RegistrationMode}' prewarm='{poolDefinition.Prewarm}' initialSize='{poolDefinition.InitialSize}' lifetimeScope='{poolDefinition.LifetimeScope}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                    DebugUtility.Colors.Success);
            }

            DebugUtility.LogVerbose(
                typeof(ActivityEntryPoolPreparationStage),
                $"event='ActivityEntryPoolPreparationCompleted' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' providerCount='{providerCount}' resolvedPoolCount='{resolvedDependencies.Count}' preparedPoolCount='{CountPrepared(resolvedDependencies)}' skippedPoolCount='{CountSkipped(resolvedDependencies)}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Success);
        }

        private static IReadOnlyList<ResolvedPoolDependency> CollectDependencies(
            IReadOnlyList<ActorInstanceRecord> actorInstances,
            out int providerCount)
        {
            providerCount = 0;
            if (actorInstances == null || actorInstances.Count == 0)
            {
                return Array.Empty<ResolvedPoolDependency>();
            }

            List<ResolvedPoolDependency> dependencies = new();
            HashSet<int> uniquePoolDefinitionIds = new();

            for (int actorIndex = 0; actorIndex < actorInstances.Count; actorIndex++)
            {
                ActorInstanceRecord actorInstance = actorInstances[actorIndex];
                if (!actorInstance.IsValid || actorInstance.CapabilitySurface == null)
                {
                    continue;
                }

                IReadOnlyList<IActorRuntimePoolDependencyProvider> providers =
                    actorInstance.CapabilitySurface.GetContributionProviders<IActorRuntimePoolDependencyProvider>();
                if (providers == null || providers.Count == 0)
                {
                    continue;
                }

                for (int providerIndex = 0; providerIndex < providers.Count; providerIndex++)
                {
                    IActorRuntimePoolDependencyProvider provider = providers[providerIndex];
                    if (provider == null)
                    {
                        continue;
                    }

                    providerCount++;
                    string providerType = provider.GetType().FullName ?? provider.GetType().Name;
                    IReadOnlyList<PoolDefinitionAsset> poolDefinitions = provider.RuntimePoolDefinitions;
                    if (poolDefinitions == null)
                    {
                        throw new InvalidOperationException(
                            $"ActivityEntryPoolPreparationStage provider returned null pool definition list. providerType='{providerType}' actorId='{actorInstance.ActorId}' actorInstanceRuntimeId='{actorInstance.ActorInstanceRuntimeId}'.");
                    }

                    for (int poolIndex = 0; poolIndex < poolDefinitions.Count; poolIndex++)
                    {
                        PoolDefinitionAsset poolDefinition = poolDefinitions[poolIndex];
                        if (poolDefinition == null)
                        {
                            throw new InvalidOperationException(
                                $"ActivityEntryPoolPreparationStage provider returned null pool definition. providerType='{providerType}' actorId='{actorInstance.ActorId}' actorInstanceRuntimeId='{actorInstance.ActorInstanceRuntimeId}'.");
                        }

                        int poolDefinitionId = poolDefinition.GetInstanceID();
                        if (!uniquePoolDefinitionIds.Add(poolDefinitionId))
                        {
                            continue;
                        }

                        dependencies.Add(new ResolvedPoolDependency(providerType, poolDefinition));
                    }
                }
            }

            return dependencies;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static int CountPrepared(IReadOnlyList<ResolvedPoolDependency> dependencies)
        {
            int prepared = 0;
            for (int index = 0; index < dependencies.Count; index++)
            {
                if (dependencies[index].PoolDefinition.RegistrationMode == PoolRegistrationMode.ActivityEntry)
                {
                    prepared++;
                }
            }

            return prepared;
        }

        private static int CountSkipped(IReadOnlyList<ResolvedPoolDependency> dependencies)
        {
            int skipped = 0;
            for (int index = 0; index < dependencies.Count; index++)
            {
                if (dependencies[index].PoolDefinition.RegistrationMode != PoolRegistrationMode.ActivityEntry)
                {
                    skipped++;
                }
            }

            return skipped;
        }

        private readonly struct ResolvedPoolDependency
        {
            public ResolvedPoolDependency(string providerType, PoolDefinitionAsset poolDefinition)
            {
                ProviderType = Normalize(providerType);
                PoolDefinition = poolDefinition;
            }

            public string ProviderType { get; }
            public PoolDefinitionAsset PoolDefinition { get; }
        }
    }
}
