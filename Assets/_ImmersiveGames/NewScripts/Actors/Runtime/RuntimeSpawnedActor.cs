using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RuntimeSpawnedActor : Actor, IPoolableObject
    {
        private ActorId runtimeActorId;
        private ActorRole runtimeActorRole;
        private ActorScope runtimeActorScope;
        private ActorParticipationRecord.ActorParticipationPolicy runtimeParticipationPolicy;
        private RuntimeSpawnOriginMetadata runtimeSpawnOrigin;

        public bool IsRuntimeMetadataBound =>
            runtimeActorId.IsValid &&
            RuntimeActorInstanceId.IsValid &&
            runtimeActorRole != ActorRole.Unknown &&
            runtimeActorScope != ActorScope.Unknown &&
            runtimeSpawnOrigin.IsValid;

        public override ActorId ActorIdValue => runtimeActorId;
        public override ActorRole ActorRoleMetadata => runtimeActorRole;
        public override ActorScope ActorScopeMetadata => runtimeActorScope;
        public override ActorParticipationRecord.ActorParticipationPolicy ActorParticipationPolicy => runtimeParticipationPolicy;
        public RuntimeSpawnOriginMetadata SpawnOrigin => runtimeSpawnOrigin;
        public bool HasSpawnOrigin => runtimeSpawnOrigin.IsValid;
        public ActorId OwnerActorId => runtimeSpawnOrigin.OwnerActorId;
        public ActorInstanceRuntimeId OwnerActorInstanceRuntimeId => runtimeSpawnOrigin.OwnerActorInstanceRuntimeId;
        public RuntimeSpawnProfileId SpawnProfileId => runtimeSpawnOrigin.SpawnProfileId;
        public PoolDefinitionAsset OriginPoolDefinition => runtimeSpawnOrigin.PoolDefinition;


        public void BindRuntimeMetadata(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorRole actorRole,
            ActorScope actorScope,
            ActorParticipationRecord.ActorParticipationPolicy participationPolicy,
            RuntimeSpawnOriginMetadata spawnOrigin,
            string source)
        {
            string origin = ResolveOrigin(source, nameof(RuntimeSpawnedActor), name);
            if (!actorId.IsValid)
            {
                throw new InvalidOperationException($"{origin} cannot bind invalid ActorId.");
            }

            if (!actorInstanceRuntimeId.IsValid)
            {
                throw new InvalidOperationException($"{origin} cannot bind invalid ActorInstanceRuntimeId.");
            }

            if (actorRole == ActorRole.Unknown)
            {
                throw new InvalidOperationException($"{origin} cannot bind unknown ActorRole.");
            }

            if (actorScope == ActorScope.Unknown)
            {
                throw new InvalidOperationException($"{origin} cannot bind unknown ActorScope.");
            }

            if (!spawnOrigin.IsValid)
            {
                throw new InvalidOperationException($"{origin} cannot bind invalid RuntimeSpawnOriginMetadata.");
            }

            runtimeActorId = actorId;
            runtimeActorRole = actorRole;
            runtimeActorScope = actorScope;
            runtimeParticipationPolicy = participationPolicy;
            runtimeSpawnOrigin = spawnOrigin;
            SetRuntimeActorInstanceId(actorInstanceRuntimeId);

            DebugUtility.Log(
                typeof(RuntimeSpawnedActor),
                $"[OBS][ActorProjectileFire] event='RuntimeSpawnedActorMetadataBound' actorId='{runtimeActorId}' actorInstanceRuntimeId='{RuntimeActorInstanceId}' actorRole='{runtimeActorRole}' actorScope='{runtimeActorScope}' ownerActorId='{runtimeSpawnOrigin.OwnerActorId}' ownerActorInstanceRuntimeId='{runtimeSpawnOrigin.OwnerActorInstanceRuntimeId}' spawnProfileId='{runtimeSpawnOrigin.SpawnProfileId}' originPoolDefinition='{runtimeSpawnOrigin.PoolDefinitionName}' commandSequence='{runtimeSpawnOrigin.CommandSequence}' instanceName='{name}' activeSelf='{gameObject.activeSelf}' activeInHierarchy='{gameObject.activeInHierarchy}' source='{Normalize(source)}' reason='runtime_spawned_actor_metadata_bound'.",
                DebugUtility.Colors.Info);
        }

        public override void ValidateLocalConfigurationOrThrow(string source)
        {
            string origin = ResolveOrigin(source, nameof(RuntimeSpawnedActor), name);
            if (CapabilitySurface == null)
            {
                throw new InvalidOperationException($"{origin} requires ActorCapabilitySurface.");
            }
        }

        public void OnPoolCreated()
        {
            ClearRuntimeMetadata();
        }

        public void OnPoolRent()
        {
            // O pool apenas aluga a instância. A identidade runtime é aplicada pelo adapter de spawn logo após o rent.
        }

        public void OnPoolReturn()
        {
            ClearRuntimeMetadata();
        }

        public void OnPoolDestroyed()
        {
            ClearRuntimeMetadata();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private void ClearRuntimeMetadata()
        {
            runtimeActorId = default;
            runtimeActorRole = ActorRole.Unknown;
            runtimeActorScope = ActorScope.Unknown;
            runtimeParticipationPolicy = ActorParticipationRecord.ActorParticipationPolicy.None;
            runtimeSpawnOrigin = default;
            SetRuntimeActorInstanceId(default);
        }
    }
}
