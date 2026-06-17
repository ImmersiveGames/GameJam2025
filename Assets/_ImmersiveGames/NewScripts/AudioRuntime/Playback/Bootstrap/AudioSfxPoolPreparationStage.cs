using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.AudioRuntime.Playback.Bootstrap
{
    internal static class AudioSfxPoolPreparationStage
    {
        public static void Execute(
            AudioDefaultsAsset defaults,
            IPoolService poolService,
            string source,
            string reason)
        {
            if (defaults == null)
            {
                throw new InvalidOperationException("AudioSfxPoolPreparationStage requires AudioDefaultsAsset.");
            }

            poolService = poolService ?? throw new ArgumentNullException(nameof(poolService));

            IReadOnlyList<PoolDefinitionAsset> poolDefinitions = defaults.GlobalSfxVoicePoolDefinitions;
            if (poolDefinitions == null)
            {
                throw new InvalidOperationException("AudioSfxPoolPreparationStage requires a non-null global SFX voice pool catalog.");
            }

            HashSet<EntityId> uniquePoolDefinitionIds = new();
            int catalogCount = poolDefinitions.Count;
            int resolvedCount = 0;
            int preparedCount = 0;
            int skippedCount = 0;

            DebugUtility.LogVerbose(
                typeof(AudioSfxPoolPreparationStage),
                $"event='AudioSfxPoolPreparationStarted' catalogCount='{catalogCount}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);

            for (int index = 0; index < poolDefinitions.Count; index++)
            {
                PoolDefinitionAsset poolDefinition = poolDefinitions[index];
                if (poolDefinition == null)
                {
                    throw new InvalidOperationException($"AudioSfxPoolPreparationStage catalog returned null pool definition at index='{index}'.");
                }

                EntityId poolDefinitionId = poolDefinition.GetEntityId();
                if (!uniquePoolDefinitionIds.Add(poolDefinitionId))
                {
                    continue;
                }

                resolvedCount++;

                DebugUtility.LogVerbose(
                    typeof(AudioSfxPoolPreparationStage),
                    $"event='AudioSfxPoolDependencyResolved' poolDefinition='{poolDefinition.name}' poolLabel='{Normalize(poolDefinition.PoolLabel)}' registrationMode='{poolDefinition.RegistrationMode}' prewarm='{poolDefinition.Prewarm}' initialSize='{poolDefinition.InitialSize}' lifetimeScope='{poolDefinition.LifetimeScope}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                    DebugUtility.Colors.Info);

                if (poolDefinition.RegistrationMode != PoolRegistrationMode.GlobalBoot)
                {
                    skippedCount++;
                    DebugUtility.LogVerbose(
                        typeof(AudioSfxPoolPreparationStage),
                        $"event='AudioSfxPoolPreparationSkipped' poolDefinition='{poolDefinition.name}' poolLabel='{Normalize(poolDefinition.PoolLabel)}' registrationMode='{poolDefinition.RegistrationMode}' reason='timing_not_global_boot' source='{Normalize(source)}' reasonText='{Normalize(reason)}'.",
                        DebugUtility.Colors.Info);
                    continue;
                }

                poolService.EnsureRegistered(poolDefinition);
                preparedCount++;

                DebugUtility.LogVerbose(
                    typeof(AudioSfxPoolPreparationStage),
                    $"event='AudioSfxPoolPrepared' poolDefinition='{poolDefinition.name}' poolLabel='{Normalize(poolDefinition.PoolLabel)}' registrationMode='{poolDefinition.RegistrationMode}' prewarm='{poolDefinition.Prewarm}' initialSize='{poolDefinition.InitialSize}' lifetimeScope='{poolDefinition.LifetimeScope}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                    DebugUtility.Colors.Success);
            }

            DebugUtility.LogVerbose(
                typeof(AudioSfxPoolPreparationStage),
                $"event='AudioSfxPoolPreparationCompleted' catalogCount='{catalogCount}' resolvedPoolCount='{resolvedCount}' preparedPoolCount='{preparedCount}' skippedPoolCount='{skippedCount}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Success);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
