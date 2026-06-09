using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RuntimeSpawnedActor : Actor, IPoolableObject
    {
        [SerializeField] private string defaultActorId = "actor.runtime.spawned";
        [SerializeField] private ActorRole defaultActorRole = ActorRole.RuntimeSpawnedActor;
        [SerializeField] private ActorScope defaultActorScope = ActorScope.ActivityScoped;

        private ActorId _runtimeActorId;
        private ActorRole _runtimeActorRole;
        private ActorScope _runtimeActorScope;
        private ActorParticipationRecord.ActorParticipationPolicy _runtimeParticipationPolicy;

        public override ActorId ActorIdValue => _runtimeActorId.IsValid ? _runtimeActorId : new ActorId(Normalize(defaultActorId));
        public override ActorRole ActorRoleMetadata => _runtimeActorRole == ActorRole.Unknown ? defaultActorRole : _runtimeActorRole;
        public override ActorScope ActorScopeMetadata => _runtimeActorScope == ActorScope.Unknown ? defaultActorScope : _runtimeActorScope;
        public override ActorParticipationRecord.ActorParticipationPolicy ActorParticipationPolicy => _runtimeParticipationPolicy;

        public void BindRuntimeMetadata(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorRole actorRole,
            ActorScope actorScope,
            ActorParticipationRecord.ActorParticipationPolicy participationPolicy,
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

            _runtimeActorId = actorId;
            _runtimeActorRole = actorRole;
            _runtimeActorScope = actorScope;
            _runtimeParticipationPolicy = participationPolicy;
            SetRuntimeActorInstanceId(actorInstanceRuntimeId);
        }

        public override void ValidateLocalConfigurationOrThrow(string source)
        {
            string origin = ResolveOrigin(source, nameof(RuntimeSpawnedActor), name);
            if (!ActorIdValue.IsValid)
            {
                throw new InvalidOperationException($"{origin} requires defaultActorId or runtime actor binding.");
            }

            if (ActorRoleMetadata == ActorRole.Unknown)
            {
                throw new InvalidOperationException($"{origin} requires explicit ActorRole.");
            }

            if (ActorScopeMetadata == ActorScope.Unknown)
            {
                throw new InvalidOperationException($"{origin} requires explicit ActorScope.");
            }

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
            // O adapter de spawn é o owner do bind runtime. O pool apenas reativa a instância.
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
            defaultActorId = Normalize(defaultActorId);
        }

        private void ClearRuntimeMetadata()
        {
            _runtimeActorId = default;
            _runtimeActorRole = ActorRole.Unknown;
            _runtimeActorScope = ActorScope.Unknown;
            _runtimeParticipationPolicy = ActorParticipationRecord.ActorParticipationPolicy.None;
            SetRuntimeActorInstanceId(default);
        }
    }
}
