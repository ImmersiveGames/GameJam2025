using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Presentation.Runtime
{
    public sealed class ActorPresentationContainerResolver
    {
        public const string ReasonNoRequirements = "actor_presentation_no_slot_requirements";
        public const string ReasonEndpointMissing = "actor_presentation_endpoint_missing";
        public const string ReasonRequirementsMissing = "actor_presentation_slot_requirements_missing";
        public const string ReasonInvalidRequirement = "actor_presentation_invalid_slot_requirement";
        public const string ReasonDuplicateRequirement = "actor_presentation_duplicate_slot_requirement";
        public const string ReasonInvalidContainer = "actor_presentation_invalid_container";
        public const string ReasonDuplicateContainer = "actor_presentation_duplicate_container";
        public const string ReasonRequiredContainerMissing = "actor_presentation_required_container_missing";
        public const string ReasonOptionalContainerMissing = "actor_presentation_optional_container_missing";

        public ActorPresentationContainerResolutionResult Resolve(
            ActorPresentationEndpoint endpoint,
            IReadOnlyList<ActorPresentationSlotRequirement> requirements,
            string source,
            string reason)
        {
            string origin = source.TrimToOrDefault(nameof(ActorPresentationContainerResolver));

            if (endpoint == null)
            {
                return ActorPresentationContainerResolutionResult.Failed(
                    ReasonEndpointMissing,
                    $"{origin} requires ActorPresentationEndpoint.");
            }

            if (requirements == null)
            {
                return ActorPresentationContainerResolutionResult.Failed(
                    ReasonRequirementsMissing,
                    $"{origin} requires slot requirements.");
            }

            if (requirements.Count == 0)
            {
                return ActorPresentationContainerResolutionResult.Success(
                    Array.Empty<ActorPresentationSlotBinding>(),
                    Array.Empty<ActorPresentationSkippedSlot>(),
                    $"{origin} has no ActorPresentation slot requirements.");
            }

            var containerIndexResult = BuildContainerIndex(
                endpoint,
                origin,
                out Dictionary<string, ActorPresentationContainer> containerIndex);

            if (containerIndexResult.IsFailed)
            {
                return containerIndexResult;
            }

            var bindings = new List<ActorPresentationSlotBinding>();
            var skippedOptionalSlots = new List<ActorPresentationSkippedSlot>();
            var observedRequirements = new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0; index < requirements.Count; index++)
            {
                var requirement = requirements[index];

                if (!requirement.IsValid)
                {
                    return ActorPresentationContainerResolutionResult.Failed(
                        ReasonInvalidRequirement,
                        $"{origin} has invalid slot requirement at index '{index}'.");
                }

                string requirementKey = BuildKey(requirement.SlotKind, requirement.SlotId);
                if (!observedRequirements.Add(requirementKey))
                {
                    return ActorPresentationContainerResolutionResult.Failed(
                        ReasonDuplicateRequirement,
                        $"{origin} has duplicate slot requirement '{requirementKey}'.");
                }

                if (containerIndex.TryGetValue(requirementKey, out var container))
                {
                    bindings.Add(container.ToBinding());
                    continue;
                }

                if (requirement.IsRequired)
                {
                    return ActorPresentationContainerResolutionResult.Failed(
                        ReasonRequiredContainerMissing,
                        $"{origin} could not resolve required ActorPresentation container '{requirementKey}'.");
                }

                skippedOptionalSlots.Add(new ActorPresentationSkippedSlot(
                    requirement,
                    ReasonOptionalContainerMissing,
                    $"{origin} skipped optional ActorPresentation container '{requirementKey}'."));
            }

            if (bindings.Count == 0 && skippedOptionalSlots.Count > 0)
            {
                return ActorPresentationContainerResolutionResult.SkippedOptional(
                    skippedOptionalSlots,
                    ReasonOptionalContainerMissing,
                    $"{origin} resolved no required bindings and skipped optional ActorPresentation containers.");
            }

            return ActorPresentationContainerResolutionResult.Success(
                bindings,
                skippedOptionalSlots,
                $"{origin} resolved ActorPresentation containers.");
        }

        private static ActorPresentationContainerResolutionResult BuildContainerIndex(
            ActorPresentationEndpoint endpoint,
            string origin,
            out Dictionary<string, ActorPresentationContainer> containerIndex)
        {
            containerIndex = new Dictionary<string, ActorPresentationContainer>(StringComparer.Ordinal);
            IReadOnlyList<ActorPresentationContainer> containers = endpoint.Containers;

            for (int index = 0; index < containers.Count; index++)
            {
                var container = containers[index];

                if (container == null || !container.IsValid)
                {
                    return ActorPresentationContainerResolutionResult.Failed(
                        ReasonInvalidContainer,
                        $"{origin} has invalid ActorPresentation container at index '{index}'.");
                }

                string containerKey = BuildKey(container.SlotKind, container.SlotId);
                if (containerIndex.ContainsKey(containerKey))
                {
                    return ActorPresentationContainerResolutionResult.Failed(
                        ReasonDuplicateContainer,
                        $"{origin} has duplicate ActorPresentation container '{containerKey}'.");
                }

                containerIndex.Add(containerKey, container);
            }

            return ActorPresentationContainerResolutionResult.Success(
                Array.Empty<ActorPresentationSlotBinding>(),
                Array.Empty<ActorPresentationSkippedSlot>(),
                $"{origin} indexed ActorPresentation containers.");
        }

        private static string BuildKey(ActorPresentationSlotKind slotKind, string slotId)
        {
            return $"{slotKind}:{slotId.TrimToEmpty()}";
        }
}
}
