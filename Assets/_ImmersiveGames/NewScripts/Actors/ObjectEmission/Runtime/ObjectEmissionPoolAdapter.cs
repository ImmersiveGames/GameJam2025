using System;
using _ImmersiveGames.NewScripts.Actors.ObjectEmission.Authoring;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.ObjectEmission.Runtime
{
    public sealed class ObjectEmissionPoolAdapter
    {
        private IPoolService _poolService;

        public void AttachPoolService(IPoolService poolService)
        {
            _poolService = poolService;
        }

        public void PrepareObjectEmissionPool(in ActorObjectEmissionCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ObjectEmissionPoolAdapter requires a valid ActorObjectEmissionCommand.");
            }

            ActorObjectEmissionProfile profile = command.Profile ?? throw new InvalidOperationException("ObjectEmissionPoolAdapter requires non-null ActorObjectEmissionProfile.");
            PoolDefinitionAsset poolDefinition = profile.PoolDefinition ?? throw new InvalidOperationException($"ObjectEmissionPoolAdapter requires pool definition for profileId='{profile.ProfileId}'.");

            DebugUtility.Log(
                typeof(ObjectEmissionPoolAdapter),
                $"[OBS][ObjectEmissionPool] event='ObjectEmissionPoolProfileResolved' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' profileId='{profile.ProfileId}' poolDefinition='{poolDefinition.name}' poolLabel='{poolDefinition.PoolLabel}' emissionOriginPolicy='{profile.EmissionOriginPolicy}' objectSpeed='{profile.ObjectSpeed:0.###}' objectLifetime='{profile.ObjectLifetime:0.###}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (_poolService == null)
            {
                DebugUtility.Log(
                    typeof(ObjectEmissionPoolAdapter),
                    $"[OBS][ObjectEmissionPool] event='ObjectEmissionPoolRegistrationSkipped' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' profileId='{profile.ProfileId}' poolDefinition='{poolDefinition.name}' reason='pool_service_unavailable' source='{command.Source}' commandSource='{command.SourceId}' commandReason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _poolService.EnsureRegistered(poolDefinition);
            DebugUtility.Log(
                typeof(ObjectEmissionPoolAdapter),
                $"[OBS][ObjectEmissionPool] event='ObjectEmissionPoolRegistered' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' profileId='{profile.ProfileId}' poolDefinition='{poolDefinition.name}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            if (!poolDefinition.Prewarm)
            {
                DebugUtility.Log(
                    typeof(ObjectEmissionPoolAdapter),
                    $"[OBS][ObjectEmissionPool] event='ObjectEmissionPoolPrewarmSkipped' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' profileId='{profile.ProfileId}' poolDefinition='{poolDefinition.name}' reason='prewarm_disabled_by_pool_definition' source='{command.Source}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.Log(
                typeof(ObjectEmissionPoolAdapter),
                $"[OBS][ObjectEmissionPool] event='ObjectEmissionPoolPrewarmRequested' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' profileId='{profile.ProfileId}' poolDefinition='{poolDefinition.name}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
            _poolService.Prewarm(poolDefinition);
            DebugUtility.Log(
                typeof(ObjectEmissionPoolAdapter),
                $"[OBS][ObjectEmissionPool] event='ObjectEmissionPoolPrewarmCompleted' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' profileId='{profile.ProfileId}' poolDefinition='{poolDefinition.name}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
        }
    }
}
