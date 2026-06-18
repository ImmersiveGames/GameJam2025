using System;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Contracts;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Projectile.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using UnityEngine;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerActor : Actor, IActorPlacementResetEndpoint, IActorEntryInitializeResetEndpoint, IActorRuntimeLocalResetEndpoint, IActorRuntimeActivityResetEndpoint, IActorRuntimeActivityTransitionResetEndpoint, IActorRuntimeRouteTransitionResetEndpoint, IActorResetContributionProvider
    {
        [Header("Reset")]
        [SerializeField] private ActivityResetBoundaryEligibility resetBoundaryEligibility = ActivityResetBoundaryEligibility.RuntimeAll;

        private ActorId _runtimeActorId;
        private ActorScope _runtimeActorScope;
        private ActorParticipationRecord.ActorParticipationPolicy _runtimeParticipationPolicy;
        private bool _hasInitialStatePlacementProfile;
        private Vector3 _initialStatePlacementPosition;
        private Vector3 _initialStatePlacementEulerAngles;
        private bool _hasRuntimeActivityStatePlacementProfile;
        private Vector3 _runtimeActivityStatePlacementPosition;
        private Vector3 _runtimeActivityStatePlacementEulerAngles;

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

            if (CapabilitySurface.TryGetEndpoint<ActorProjectileFireEndpoint>(out var projectileFireEndpoint))
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

        public void ApplyEntryInitializeReset(ActorResetContext context)
        {
            ApplyPlacementFromCommandContext(
                context,
                nameof(ActivityResetStateProfileKind.InitialState),
                true,
                true);
        }

        public void ApplyRuntimeLocalReset(ActorResetContext context)
        {
            EnsurePlacementContext(context, nameof(ApplyRuntimeLocalReset));

            if (_hasInitialStatePlacementProfile)
            {
                ApplyPlacementProfile(
                    context,
                    _initialStatePlacementPosition,
                    _initialStatePlacementEulerAngles,
                    nameof(ActivityResetStateProfileKind.RuntimeLocalState),
                    nameof(ActivityResetStateProfileKind.InitialState),
                    true);
                return;
            }

            if (context.PlacementRequired)
            {
                throw new InvalidOperationException(
                    $"PlayerActor runtime local placement reset requires a cached InitialState profile. actorId='{context.ActorId}' resetIntent='{context.ResetIntent}' resetStateProfile='{context.StateProfileKind}'.");
            }

            LogPlacementProfileSkipped(
                context,
                nameof(ActivityResetStateProfileKind.RuntimeLocalState),
                "missing_initial_state_profile",
                "runtime_local_reset_has_no_cached_placement_profile");
        }

        public void ApplyRuntimeActivityReset(ActorResetContext context)
        {
            if (context.HasPlacement)
            {
                ApplyPlacementFromCommandContext(
                    context,
                    nameof(ActivityResetStateProfileKind.RuntimeActivityState),
                    false,
                    true);
                return;
            }

            if (_hasRuntimeActivityStatePlacementProfile)
            {
                ApplyPlacementProfile(
                    context,
                    _runtimeActivityStatePlacementPosition,
                    _runtimeActivityStatePlacementEulerAngles,
                    nameof(ActivityResetStateProfileKind.RuntimeActivityState),
                    nameof(ActivityResetStateProfileKind.RuntimeActivityState),
                    true);
                return;
            }

            if (_hasInitialStatePlacementProfile)
            {
                ApplyPlacementProfile(
                    context,
                    _initialStatePlacementPosition,
                    _initialStatePlacementEulerAngles,
                    nameof(ActivityResetStateProfileKind.RuntimeActivityState),
                    nameof(ActivityResetStateProfileKind.InitialState),
                    true);
                return;
            }

            ApplyPlacementFromCommandContext(
                context,
                nameof(ActivityResetStateProfileKind.RuntimeActivityState),
                false,
                true);
        }

        public void ApplyRuntimeActivityTransitionReset(ActorResetContext context)
        {
            EnsurePlacementContext(context, nameof(ApplyRuntimeActivityTransitionReset));
            LogPlacementProfileSkipped(
                context,
                nameof(ActivityResetStateProfileKind.RuntimeActivityTransitionState),
                "transition_profile_no_placement",
                "runtime_activity_transition_does_not_apply_player_placement");
        }

        public void ApplyRuntimeRouteTransitionReset(ActorResetContext context)
        {
            EnsurePlacementContext(context, nameof(ApplyRuntimeRouteTransitionReset));
            LogPlacementProfileSkipped(
                context,
                nameof(ActivityResetStateProfileKind.RuntimeRouteTransitionState),
                "route_transition_profile_no_placement",
                "runtime_route_transition_does_not_apply_player_placement");
        }

        private void ApplyPlacementFromCommandContext(
            ActorResetContext context,
            string placementProfileKind,
            bool captureInitialStateProfile,
            bool captureRuntimeActivityProfile)
        {
            EnsurePlacementContext(context, placementProfileKind);

            if (context is { PlacementRequired: true, HasPlacement: false })
            {
                throw new InvalidOperationException(
                    $"Placement reset is required but missing for actorId='{context.ActorId}' resetIntent='{context.ResetIntent}' resetStateProfile='{context.StateProfileKind}' placementProfileKind='{placementProfileKind}'.");
            }

            if (!context.HasPlacement)
            {
                LogPlacementProfileSkipped(
                    context,
                    placementProfileKind,
                    "command_context_missing_placement",
                    "placement_context_not_available");
                return;
            }

            ApplyPlacementProfile(
                context,
                context.PlacementPosition,
                context.PlacementEulerAngles,
                placementProfileKind,
                "command_context",
                true);

            if (captureInitialStateProfile)
            {
                _hasInitialStatePlacementProfile = true;
                _initialStatePlacementPosition = context.PlacementPosition;
                _initialStatePlacementEulerAngles = context.PlacementEulerAngles;
            }

            if (captureRuntimeActivityProfile)
            {
                _hasRuntimeActivityStatePlacementProfile = true;
                _runtimeActivityStatePlacementPosition = context.PlacementPosition;
                _runtimeActivityStatePlacementEulerAngles = context.PlacementEulerAngles;
            }
        }

        private void ApplyPlacementProfile(
            ActorResetContext context,
            Vector3 placementPosition,
            Vector3 placementEulerAngles,
            string placementProfileKind,
            string placementProfileSource,
            bool placementApplied)
        {
            EnsurePlacementContext(context, placementProfileKind);

            transform.localPosition = placementPosition;
            transform.localRotation = Quaternion.Euler(placementEulerAngles);

            DebugUtility.LogVerbose(typeof(PlayerActor),
                $"event='PlacementStateProfileApplied' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}' resetIntent='{context.ResetIntent}' resetStateProfile='{context.StateProfileKind}' placementProfileKind='{placementProfileKind}' placementProfileSource='{placementProfileSource}' placementApplied='{placementApplied}' placementPosition='{placementPosition}' placementEulerAngles='{placementEulerAngles}' source='{context.Source}' reason='{context.Reason}'.",
                DebugUtility.Colors.Info, this);
        }

        private void LogPlacementProfileSkipped(
            ActorResetContext context,
            string placementProfileKind,
            string placementProfileSource,
            string reason)
        {
            EnsurePlacementContext(context, placementProfileKind);
            DebugUtility.LogVerbose(typeof(PlayerActor),
                $"event='PlacementStateProfileSkipped' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}' resetIntent='{context.ResetIntent}' resetStateProfile='{context.StateProfileKind}' placementProfileKind='{placementProfileKind}' placementProfileSource='{placementProfileSource}' placementApplied='False' reason='{reason}' source='{context.Source}' reasonDetail='{context.Reason}'.",
                DebugUtility.Colors.Info, this);
        }

        private static void EnsurePlacementContext(ActorResetContext context, string operation)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException($"PlayerActor received invalid reset context for operation='{operation}'.");
            }

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
            public ActivityResetBoundaryEligibility ResetBoundaryEligibility { get; }
            public bool IsValid => Descriptor.IsValid;
        }
    }
}
