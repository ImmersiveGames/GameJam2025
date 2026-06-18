using System;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Presentation.Runtime
{
    /// <summary>
    /// Resolve o plano de ActorPresentation a partir de dados autorais e containers explícitos.
    /// Não decide lifecycle e não executa side-effects Unity.
    /// </summary>
    public sealed class ActorPresentationPlanResolver
    {
        public const string ReasonResolved = "actor_presentation_plan_resolved";
        public const string ReasonProfileMissing = "actor_presentation_profile_missing";
        public const string ReasonProfileInvalid = "actor_presentation_profile_invalid";
        public const string ReasonEndpointMissing = "actor_presentation_endpoint_missing";
        public const string ReasonRequiredIdentityMissing = "actor_presentation_required_identity_missing";
        public const string ReasonRequiredVisualPrefabMissing = "actor_presentation_required_visual_prefab_missing";
        public const string ReasonOptionalVisualPrefabMissing = "actor_presentation_optional_visual_prefab_missing";
        public const string ReasonContainerResolutionFailed = "actor_presentation_container_resolution_failed";
        public const string ReasonPrimaryContainerMissing = "actor_presentation_primary_container_missing";
        public const string ReasonPrimaryContainerSkippedOptional = "actor_presentation_primary_container_skipped_optional";

        private readonly ActorPresentationContainerResolver _containerResolver;

        public ActorPresentationPlanResolver()
            : this(new ActorPresentationContainerResolver()) { }

        public ActorPresentationPlanResolver(ActorPresentationContainerResolver containerResolver)
        {
            _containerResolver = containerResolver ?? throw new ArgumentNullException(nameof(containerResolver));
        }

        public ActorPresentationPlanResolutionResult Resolve(
            ActorPresentationProfileAsset profile,
            ActorPresentationEndpoint endpoint,
            string activityIdentity,
            string actorId,
            string actorKind,
            string source,
            string reason)
        {
            string origin = source.TrimToOrDefault(nameof(ActorPresentationPlanResolver));

            if (string.IsNullOrWhiteSpace(activityIdentity) ||
                string.IsNullOrWhiteSpace(actorId) ||
                string.IsNullOrWhiteSpace(actorKind))
            {
                return ActorPresentationPlanResolutionResult.Failed(
                    ReasonRequiredIdentityMissing,
                    $"{origin} requires activityIdentity, actorId and actorKind.");
            }

            if (profile == null)
            {
                return ActorPresentationPlanResolutionResult.Failed(
                    ReasonProfileMissing,
                    $"{origin} requires ActorPresentationProfileAsset.");
            }

            try
            {
                profile.ValidateOrThrow(origin);
            }
            catch (Exception exception)
            {
                return ActorPresentationPlanResolutionResult.Failed(
                    ReasonProfileInvalid,
                    exception.Message);
            }

            if (profile.VisualPrefab == null)
            {
                if (profile.IsOptional)
                {
                    return ActorPresentationPlanResolutionResult.SkippedOptional(
                        ReasonOptionalVisualPrefabMissing,
                        $"{origin} skipped optional ActorPresentation because visualPrefab is missing.");
                }

                return ActorPresentationPlanResolutionResult.Failed(
                    ReasonRequiredVisualPrefabMissing,
                    $"{origin} requires visualPrefab for required ActorPresentation.");
            }

            if (endpoint == null)
            {
                return ActorPresentationPlanResolutionResult.Failed(
                    ReasonEndpointMissing,
                    $"{origin} requires ActorPresentationEndpoint.");
            }

            var containerResult = _containerResolver.Resolve(
                endpoint,
                profile.SlotRequirements,
                origin,
                reason);

            if (containerResult.IsFailed)
            {
                return ActorPresentationPlanResolutionResult.Failed(
                    ReasonContainerResolutionFailed,
                    $"{origin} failed container resolution: {containerResult.ReasonCode} - {containerResult.Message}");
            }

            var plan = new ActorPresentationResolvedPlan(
                activityIdentity,
                actorId,
                actorKind,
                profile.ProfileId,
                profile.Requiredness,
                profile.ReleasePolicy,
                profile.ResetPolicy,
                profile.VariationPolicy,
                profile.VariationSeed,
                profile.VisualPrefab,
                profile.PrimarySlotKind,
                profile.PrimarySlotId,
                containerResult.Bindings,
                nameof(ActorPresentationPlanResolver),
                ReasonResolved);

            if (!plan.TryGetPrimarySlot(out _))
            {
                if (profile.IsOptional)
                {
                    return ActorPresentationPlanResolutionResult.SkippedOptional(
                        ReasonPrimaryContainerSkippedOptional,
                        $"{origin} skipped optional ActorPresentation because primary container was not resolved.");
                }

                return ActorPresentationPlanResolutionResult.Failed(
                    ReasonPrimaryContainerMissing,
                    $"{origin} requires resolved primary container '{profile.PrimarySlotKind}:{profile.PrimarySlotId}'.");
            }

            return ActorPresentationPlanResolutionResult.Success(
                plan,
                $"{origin} resolved ActorPresentation plan.");
        }
    }
}
