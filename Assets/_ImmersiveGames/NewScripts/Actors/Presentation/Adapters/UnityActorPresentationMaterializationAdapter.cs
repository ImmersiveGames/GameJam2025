using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Presentation.Adapters
{
    public sealed class UnityActorPresentationMaterializationAdapter : IActorPresentationMaterializationAdapter
    {
        public const string ReasonInvalidMaterializationCommand = "actor_presentation_invalid_materialization_command";
        public const string ReasonOptionalVisualPrefabMissing = "actor_presentation_optional_visual_prefab_missing";
        public const string ReasonRequiredVisualPrefabMissing = "actor_presentation_required_visual_prefab_missing";
        public const string ReasonPrimarySlotMissing = "actor_presentation_primary_slot_missing";
        public const string ReasonPrimaryContainerMissing = "actor_presentation_primary_container_missing";
        public const string ReasonMaterialized = "actor_presentation_materialized";

        public const string ReasonInvalidReleaseCommand = "actor_presentation_invalid_release_command";
        public const string ReasonReleased = "actor_presentation_released";
        public const string ReasonReleaseKeptBound = "actor_presentation_release_kept_bound";

        public ActorPresentationResult Materialize(ActorPresentationMaterializationCommand command)
        {
            if (!command.IsValid)
            {
                return ActorPresentationResult.Failed(
                    ReasonInvalidMaterializationCommand,
                    "ActorPresentation materialization command is invalid.");
            }

            ActorPresentationResolvedPlan plan = command.ResolvedPlan;

            if (plan.VisualPrefab == null)
            {
                if (plan.IsOptional)
                {
                    return ActorPresentationResult.SkippedOptional(
                        plan,
                        ReasonOptionalVisualPrefabMissing,
                        "Optional ActorPresentation visualPrefab is missing.");
                }

                return ActorPresentationResult.Failed(
                    ReasonRequiredVisualPrefabMissing,
                    "Required ActorPresentation visualPrefab is missing.");
            }

            if (!plan.HasPrimarySlot)
            {
                return ActorPresentationResult.Failed(
                    ReasonPrimarySlotMissing,
                    "ActorPresentation plan requires explicit primary slot to materialize visualPrefab.");
            }

            if (!plan.TryGetPrimarySlot(out ActorPresentationSlotBinding primarySlot))
            {
                return ActorPresentationResult.Failed(
                    ReasonPrimaryContainerMissing,
                    $"ActorPresentation plan could not resolve primary container '{plan.PrimarySlotKind}:{plan.PrimarySlotId}'.");
            }

            GameObject instance = Object.Instantiate(plan.VisualPrefab, primarySlot.Container, false);
            instance.name = $"{plan.VisualPrefab.name}::{plan.ActorId}::Presentation";
            Transform instanceTransform = instance.transform;
            instanceTransform.localPosition = Vector3.zero;
            instanceTransform.localRotation = Quaternion.identity;
            instanceTransform.localScale = Vector3.one;

            ActorPresentationRuntimeHandle handle = new ActorPresentationRuntimeHandle(
                plan,
                instance,
                nameof(UnityActorPresentationMaterializationAdapter),
                ReasonMaterialized);

            ActorPresentationReadyFact readyFact = new ActorPresentationReadyFact(
                handle,
                nameof(UnityActorPresentationMaterializationAdapter),
                ReasonMaterialized);

            return ActorPresentationResult.Materialized(
                readyFact,
                "ActorPresentation visual prefab materialized.");
        }

        public ActorPresentationResult Release(ActorPresentationReleaseCommand command)
        {
            if (!command.IsValid)
            {
                return ActorPresentationResult.Failed(
                    ReasonInvalidReleaseCommand,
                    "ActorPresentation release command is invalid.");
            }

            ActorPresentationRuntimeHandle handle = command.RuntimeHandle;

            if (handle.ResolvedPlan.ReleasePolicy == ActorPresentationReleasePolicy.KeepBound)
            {
                ActorPresentationReleasedFact keptFact = new ActorPresentationReleasedFact(
                    handle,
                    nameof(UnityActorPresentationMaterializationAdapter),
                    ReasonReleaseKeptBound);

                return ActorPresentationResult.Released(
                    keptFact,
                    "ActorPresentation kept bound by release policy.");
            }

            if (handle.PresentationInstance != null)
            {
                Object.Destroy(handle.PresentationInstance);
            }

            ActorPresentationReleasedFact releasedFact = new ActorPresentationReleasedFact(
                handle,
                nameof(UnityActorPresentationMaterializationAdapter),
                ReasonReleased);

            return ActorPresentationResult.Released(
                releasedFact,
                "ActorPresentation instance released.");
        }
    }
}
