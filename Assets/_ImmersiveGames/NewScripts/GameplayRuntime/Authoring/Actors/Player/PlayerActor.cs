using System;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Contracts;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Projectile.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerActor : Actor, IActorResetEndpoint, IActorResetContributionProvider
    {
        private static readonly ActorResetGroup[] PlacementResetGroups =
        {
            ActorResetGroup.Placement,
        };

        [Header("Reset")]
        [SerializeField] private ActivityResetBoundaryEligibility resetBoundaryEligibility = ActivityResetBoundaryEligibility.All;

        private ActorId _runtimeActorId;
        private ActorScope _runtimeActorScope;
        private ActorParticipationRecord.ActorParticipationPolicy _runtimeParticipationPolicy;

        public override ActorId ActorIdValue => _runtimeActorId;
        public override ActorRole ActorRoleMetadata => ActorRole.PrimaryPlayer;
        public override ActorScope ActorScopeMetadata => _runtimeActorScope;
        public override ActorParticipationRecord.ActorParticipationPolicy ActorParticipationPolicy => _runtimeParticipationPolicy;

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

            _runtimeActorId = actorId;
            _runtimeActorScope = actorScope;
            _runtimeParticipationPolicy = participationPolicy;
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

            if (CapabilitySurface.TryGetEndpoint<ActorProjectileFireEndpoint>(out ActorProjectileFireEndpoint projectileFireEndpoint))
            {
                projectileFireEndpoint.ValidateLocalConfigurationOrThrow($"{origin}/{nameof(ActorCapabilitySurface)}.{nameof(ActorCapabilitySurface.ActorProjectileFireEndpoint)}");
            }
        }

        public bool TryCreateResetContribution(
            ActorCapabilityContributionContext context,
            out IActorResetContribution contribution)
        {
            if (!context.IsValid)
            {
                contribution = null;
                return false;
            }

            contribution = new PlayerActorPlacementResetContribution(context, resetBoundaryEligibility);
            return true;
        }

        public bool Supports(ActorResetGroup group)
        {
            return group == ActorResetGroup.Placement;
        }

        public void ApplyReset(ActorResetContext context)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("PlayerActor received invalid reset context.");
            }

            if (context.Group != ActorResetGroup.Placement)
            {
                throw new InvalidOperationException(
                    $"PlayerActor received unsupported reset group='{context.Group}' for actorId='{context.ActorId}'.");
            }

            if (context is { PlacementRequired: true, HasPlacement: false })
            {
                throw new InvalidOperationException($"Placement reset is required but missing for actorId='{context.ActorId}'.");
            }

            if (!context.HasPlacement)
            {
                return;
            }

            transform.localPosition = context.PlacementPosition;
            transform.localRotation = Quaternion.Euler(context.PlacementEulerAngles);
        }

        private readonly struct PlayerActorPlacementResetContribution : IActorResetContribution
        {
            public PlayerActorPlacementResetContribution(ActorCapabilityContributionContext context, ActivityResetBoundaryEligibility resetBoundaryEligibility)
            {
                Descriptor = new ActorCapabilityContributionDescriptor(
                    new ActorCapabilityId("actor.capability.player.placement"),
                    ActorCapabilityContributionPhase.Reset,
                    ActorCapabilityContributionRequirement.Optional,
                    context.ActorId,
                    context.ActorInstanceRuntimeId,
                    context.ActorKind,
                    context.ActorRole,
                    context.ActorScope,
                    context.ComponentPath,
                    nameof(PlayerActor),
                    "player_actor_placement_reset_contribution");
                ResetBoundaryEligibility = resetBoundaryEligibility;
            }

            public ActorCapabilityContributionDescriptor Descriptor { get; }
            public ActorResetGroup[] SupportedGroups => PlacementResetGroups;
            public ActivityResetBoundaryEligibility ResetBoundaryEligibility { get; }
            public bool IsValid => Descriptor.IsValid && SupportedGroups is { Length: > 0 };
        }
    }
}
