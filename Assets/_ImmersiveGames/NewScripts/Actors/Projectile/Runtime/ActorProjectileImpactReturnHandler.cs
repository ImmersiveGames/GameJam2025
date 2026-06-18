using System;
using _ImmersiveGames.NewScripts.Actors.Impact.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Runtime
{
    public sealed class ActorProjectileImpactReturnHandler : IActorImpactReturnHandler
    {
        private readonly ActorProjectileSpawnRuntimeState _spawnRuntimeState;
        private readonly RuntimeSpawnedActor _spawnedActor;

        public ActorProjectileImpactReturnHandler(
            ActorProjectileSpawnRuntimeState spawnRuntimeState,
            RuntimeSpawnedActor spawnedActor)
        {
            _spawnRuntimeState = spawnRuntimeState;
            _spawnedActor = spawnedActor;
        }

        public bool IsConfigured =>
            _spawnRuntimeState != null &&
            _spawnedActor != null &&
            _spawnedActor.IsRuntimeMetadataBound;

        public bool TryRequestReturn(
            ActorImpactResult impactResult,
            string source,
            string reason,
            out string outcomeReason)
        {
            string normalizedSource = source.TrimToEmpty();
            string normalizedReason = reason.TrimToEmpty();

            DebugUtility.LogVerbose(
                typeof(ActorProjectileImpactReturnHandler),
                $"event='ActorProjectileImpactReturnRequested' impactActorId='{impactResult.Intent.ImpactActorId}' ownerActorId='{impactResult.Intent.OwnerActorId}' targetActorId='{impactResult.Intent.TargetActorId}' spawnedActorId='{ResolveSpawnedActorId()}' spawnedActorInstanceRuntimeId='{ResolveSpawnedActorInstanceRuntimeId()}' source='{normalizedSource}' reason='{normalizedReason}'",
                DebugUtility.Colors.Info);

            if (!impactResult.Registered || !impactResult.HasIntent)
            {
                outcomeReason = "impact_result_not_registered";
                LogRejected(impactResult, outcomeReason, normalizedSource, normalizedReason);
                return false;
            }

            if (_spawnRuntimeState == null)
            {
                outcomeReason = "spawn_runtime_state_missing";
                LogRejected(impactResult, outcomeReason, normalizedSource, normalizedReason);
                return false;
            }

            if (_spawnedActor == null)
            {
                outcomeReason = "spawned_runtime_actor_missing";
                LogRejected(impactResult, outcomeReason, normalizedSource, normalizedReason);
                return false;
            }

            if (!_spawnRuntimeState.TryReturnSpawnedRuntimeObject(
                    _spawnedActor,
                    normalizedSource,
                    normalizedReason,
                    "impact_registered",
                    out outcomeReason))
            {
                LogRejected(impactResult, outcomeReason, normalizedSource, normalizedReason);
                return false;
            }

            DebugUtility.LogVerbose(
                typeof(ActorProjectileImpactReturnHandler),
                $"event='ActorProjectileImpactReturnAccepted' impactActorId='{impactResult.Intent.ImpactActorId}' ownerActorId='{impactResult.Intent.OwnerActorId}' targetActorId='{impactResult.Intent.TargetActorId}' spawnedActorId='{impactResult.Intent.ImpactActorId}' spawnedActorInstanceRuntimeId='{impactResult.Intent.ImpactActorInstanceRuntimeId}' outcomeReason='{outcomeReason.TrimToEmpty()}' source='{normalizedSource}' reason='{normalizedReason}'",
                DebugUtility.Colors.Success);
            return true;
        }

        private void LogRejected(
            ActorImpactResult impactResult,
            string outcomeReason,
            string source,
            string reason)
        {
            DebugUtility.LogVerbose(
                typeof(ActorProjectileImpactReturnHandler),
                $"event='ActorProjectileImpactReturnRejected' impactActorId='{impactResult.Intent.ImpactActorId}' ownerActorId='{impactResult.Intent.OwnerActorId}' targetActorId='{impactResult.Intent.TargetActorId}' spawnedActorId='{ResolveSpawnedActorId()}' spawnedActorInstanceRuntimeId='{ResolveSpawnedActorInstanceRuntimeId()}' outcomeReason='{outcomeReason.TrimToEmpty()}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Warning);
        }

        private string ResolveSpawnedActorId()
        {
            return _spawnedActor == null ? string.Empty : _spawnedActor.ActorIdValue.ToString();
        }

        private string ResolveSpawnedActorInstanceRuntimeId()
        {
            return _spawnedActor == null ? string.Empty : _spawnedActor.RuntimeActorInstanceId.ToString();
        }
}
}
