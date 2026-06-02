using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerActor : Actor
    {
        private ActorId runtimeActorId;
        private ActorScope runtimeActorScope;
        private ActorParticipationRecord.ActorParticipationPolicy runtimeParticipationPolicy;

        public override ActorId ActorIdValue => runtimeActorId;
        public override ActorRole ActorRoleMetadata => ActorRole.PrimaryPlayer;
        public override ActorScope ActorScopeMetadata => runtimeActorScope;
        public override ActorParticipationRecord.ActorParticipationPolicy ActorParticipationPolicy => runtimeParticipationPolicy;

        public void BindRuntimeMetadata(
            ActorId actorId,
            ActorScope actorScope,
            ActorParticipationRecord.ActorParticipationPolicy participationPolicy,
            string source)
        {
            string origin = ResolveOrigin(source, nameof(PlayerActor), name);
            if (!actorId.IsValid)
            {
                throw new InvalidOperationException($"{origin} cannot bind invalid ActorId.");
            }

            if (actorScope == ActorScope.Unknown)
            {
                throw new InvalidOperationException($"{origin} cannot bind unknown ActorScope.");
            }

            if (!Enum.IsDefined(typeof(ActorParticipationRecord.ActorParticipationPolicy), participationPolicy) ||
                participationPolicy == ActorParticipationRecord.ActorParticipationPolicy.None)
            {
                throw new InvalidOperationException($"{origin} cannot bind empty ActorParticipationPolicy.");
            }

            runtimeActorId = actorId;
            runtimeActorScope = actorScope;
            runtimeParticipationPolicy = participationPolicy;
        }

        public override void ValidateLocalConfigurationOrThrow(string source)
        {
            string origin = ResolveOrigin(source, nameof(PlayerActor), name);
            if (!ActorIdValue.IsValid)
            {
                throw new InvalidOperationException($"{origin} requires runtime ActorId binding from ActivityParticipantBinding.");
            }

            if (ActorScopeMetadata == ActorScope.Unknown)
            {
                throw new InvalidOperationException($"{origin} requires runtime ActorScope binding from ActivityParticipantBinding.");
            }

            if (!Enum.IsDefined(typeof(ActorParticipationRecord.ActorParticipationPolicy), ActorParticipationPolicy) ||
                ActorParticipationPolicy == ActorParticipationRecord.ActorParticipationPolicy.None)
            {
                throw new InvalidOperationException($"{origin} requires runtime ActorParticipationPolicy binding from ActivityParticipantBinding.");
            }

            if (CapabilitySurface == null)
            {
                throw new InvalidOperationException($"{origin} requires ActorCapabilitySurface.");
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
        }
    }
}
