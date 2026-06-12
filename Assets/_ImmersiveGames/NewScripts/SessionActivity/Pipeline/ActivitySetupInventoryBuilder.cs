using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    internal sealed class ActivitySetupInventoryBuilder
    {
        public ActivitySetupInventoryBuildResult Build(ActivityObjectSetupInventoryPlan plan)
        {
            if (!plan.IsValid)
            {
                return new ActivitySetupInventoryBuildResult(
                    ActivitySetupInventoryBuildResultKind.Failed,
                    default,
                    plan.Source,
                    plan.Reason,
                    "ActivitySetupInventory build context is invalid.");
            }

            var requirements = plan.SetupRequirements;

            string inventoryId = $"{plan.ActivityId}|{plan.Identity.EntrySequence}|activity_setup_inventory";

            ActivitySetupInventory inventory = new(
                plan.Identity,
                inventoryId,
                BuildParticipantRequirements(plan, requirements?.ParticipantRequirements),
                BuildObjectEntryRequirements(plan, requirements?.ObjectEntryRequirements),
                BuildSceneContributorRequirements(plan, requirements?.SceneContributorRequirements),
                BuildPlacementRequirements(plan, requirements?.PlacementRequirements),
                BuildCameraBindingRequirements(plan, requirements?.CameraBindingRequirements),
                BuildInteractionBindingRequirements(plan, requirements?.InteractionBindingRequirements),
                BuildHudBindingRequirements(plan, requirements?.HudBindingRequirements),
                BuildWarmupRequirements(plan, requirements?.WarmupRequirements),
                BuildStateResetRequirements(plan, requirements?.StateResetRequirements),
                BuildReleaseRequirements(plan, requirements?.ReleaseRequirements),
                plan.Source,
                plan.Reason);

            if (!inventory.IsValid)
            {
                return new ActivitySetupInventoryBuildResult(
                    ActivitySetupInventoryBuildResultKind.Failed,
                    inventory,
                    plan.Source,
                    plan.Reason,
                    $"ActivitySetupInventory built invalid inventory for activityId='{plan.ActivityId}' entrySequence='{plan.Identity.EntrySequence}'.");
            }

            var kind = inventory.HasRequirements
                ? ActivitySetupInventoryBuildResultKind.Built
                : ActivitySetupInventoryBuildResultKind.SkippedNoRequirements;

            string message = inventory.HasRequirements
                ? $"ActivitySetupInventory built requirements='{inventory.TotalRequirementCount}'."
                : "ActivitySetupInventory skipped because no setup requirements were declared.";

            return new ActivitySetupInventoryBuildResult(kind, inventory, plan.Source, plan.Reason, message);
        }

        private static IReadOnlyList<ParticipantRequirement> BuildParticipantRequirements(ActivityObjectSetupInventoryPlan plan, IReadOnlyList<ActivityParticipantRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<ParticipantRequirement>();
            }

            List<ParticipantRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index] ?? throw new InvalidOperationException($"Participant requirement at index '{index}' cannot be null.");
                var requirement = BuildBaseRequirement(plan, ActivitySetupSubplanKind.Participant, entry.RequirementId, entry.Requiredness);
                requirements.Add(new ParticipantRequirement(requirement, entry.ParticipantKind, entry.SessionParticipantId, entry.ExpectedSessionRole, entry.PlacementRequirementId));
            }

            return requirements;
        }

        private static IReadOnlyList<ObjectEntryRequirement> BuildObjectEntryRequirements(ActivityObjectSetupInventoryPlan plan, IReadOnlyList<ActivityObjectEntryRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<ObjectEntryRequirement>();
            }

            List<ObjectEntryRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index] ?? throw new InvalidOperationException($"Object entry requirement at index '{index}' cannot be null.");
                var requirement = BuildBaseRequirement(plan, ActivitySetupSubplanKind.ObjectEntry, entry.RequirementId, entry.Requiredness);
                requirements.Add(new ObjectEntryRequirement(requirement, entry.ObjectEntryKind, entry.ObjectId, entry.ObjectTypeId, entry.PlacementRequirementId));
            }

            return requirements;
        }

        private static IReadOnlyList<SceneContributorRequirement> BuildSceneContributorRequirements(ActivityObjectSetupInventoryPlan plan, IReadOnlyList<ActivitySceneContributorRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<SceneContributorRequirement>();
            }

            List<SceneContributorRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index] ?? throw new InvalidOperationException($"Scene contributor requirement at index '{index}' cannot be null.");
                var requirement = BuildBaseRequirement(plan, ActivitySetupSubplanKind.SceneContributor, entry.RequirementId, entry.Requiredness);
                requirements.Add(new SceneContributorRequirement(requirement, entry.ContributorId, entry.ContributorRole, entry.SceneName));
            }

            return requirements;
        }

        private static IReadOnlyList<PlacementRequirement> BuildPlacementRequirements(ActivityObjectSetupInventoryPlan plan, IReadOnlyList<ActivityPlacementRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<PlacementRequirement>();
            }

            List<PlacementRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index] ?? throw new InvalidOperationException($"Placement requirement at index '{index}' cannot be null.");
                var requirement = BuildBaseRequirement(plan, ActivitySetupSubplanKind.Placement, entry.RequirementId, entry.Requiredness);
                requirements.Add(new PlacementRequirement(requirement, entry.PlacementKind, entry.TargetId, entry.MarkerId, entry.SceneName));
            }

            return requirements;
        }

        private static IReadOnlyList<CameraBindingRequirement> BuildCameraBindingRequirements(ActivityObjectSetupInventoryPlan plan, IReadOnlyList<ActivityCameraBindingRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<CameraBindingRequirement>();
            }

            List<CameraBindingRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index] ?? throw new InvalidOperationException($"Camera binding requirement at index '{index}' cannot be null.");
                var requirement = BuildBaseRequirement(plan, ActivitySetupSubplanKind.CameraBinding, entry.RequirementId, entry.Requiredness);
                requirements.Add(new CameraBindingRequirement(requirement, entry.CameraBindingKind, entry.BindingId, entry.TargetId, entry.ProfileId));
            }

            return requirements;
        }

        private static IReadOnlyList<InteractionBindingRequirement> BuildInteractionBindingRequirements(ActivityObjectSetupInventoryPlan plan, IReadOnlyList<ActivityInteractionBindingRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<InteractionBindingRequirement>();
            }

            List<InteractionBindingRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index] ?? throw new InvalidOperationException($"Interaction binding requirement at index '{index}' cannot be null.");
                var requirement = BuildBaseRequirement(plan, ActivitySetupSubplanKind.InteractionBinding, entry.RequirementId, entry.Requiredness);
                requirements.Add(new InteractionBindingRequirement(requirement, entry.InteractionBindingKind, entry.BindingId, entry.TargetId, entry.ProfileId));
            }

            return requirements;
        }

        private static IReadOnlyList<HudBindingRequirement> BuildHudBindingRequirements(ActivityObjectSetupInventoryPlan plan, IReadOnlyList<ActivityHudBindingRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<HudBindingRequirement>();
            }

            List<HudBindingRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index] ?? throw new InvalidOperationException($"Hud binding requirement at index '{index}' cannot be null.");
                var requirement = BuildBaseRequirement(plan, ActivitySetupSubplanKind.HudBinding, entry.RequirementId, entry.Requiredness);
                requirements.Add(new HudBindingRequirement(requirement, entry.HudBindingKind, entry.BindingId, entry.TargetId, entry.ProfileId));
            }

            return requirements;
        }

        private static IReadOnlyList<WarmupRequirement> BuildWarmupRequirements(ActivityObjectSetupInventoryPlan plan, IReadOnlyList<ActivityWarmupRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<WarmupRequirement>();
            }

            List<WarmupRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index] ?? throw new InvalidOperationException($"Warmup requirement at index '{index}' cannot be null.");
                var requirement = BuildBaseRequirement(plan, ActivitySetupSubplanKind.Warmup, entry.RequirementId, entry.Requiredness);
                requirements.Add(new WarmupRequirement(requirement, entry.WarmupKind, entry.TargetId, entry.ProfileId));
            }

            return requirements;
        }

        private static IReadOnlyList<StateResetRequirement> BuildStateResetRequirements(ActivityObjectSetupInventoryPlan plan, IReadOnlyList<ActivityStateResetRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<StateResetRequirement>();
            }

            List<StateResetRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index] ?? throw new InvalidOperationException($"State reset requirement at index '{index}' cannot be null.");
                var requirement = BuildBaseRequirement(plan, ActivitySetupSubplanKind.StateReset, entry.RequirementId, entry.Requiredness);
                requirements.Add(new StateResetRequirement(requirement, entry.TargetId, entry.ResetGroups));
            }

            return requirements;
        }

        private static IReadOnlyList<ReleaseRequirement> BuildReleaseRequirements(ActivityObjectSetupInventoryPlan plan, IReadOnlyList<ActivityReleaseRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<ReleaseRequirement>();
            }

            List<ReleaseRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index] ?? throw new InvalidOperationException($"Release requirement at index '{index}' cannot be null.");
                var requirement = BuildBaseRequirement(plan, ActivitySetupSubplanKind.Release, entry.RequirementId, entry.Requiredness);
                requirements.Add(new ReleaseRequirement(requirement, entry.ReleaseKind, entry.TargetId, entry.PolicyId));
            }

            return requirements;
        }

        private static ActivitySetupRequirement BuildBaseRequirement(
            ActivityObjectSetupInventoryPlan plan,
            ActivitySetupSubplanKind subplanKind,
            string requirementId,
            ActivitySetupRequirementRequiredness requiredness)
        {
            return new ActivitySetupRequirement(
                plan.Identity,
                subplanKind,
                requirementId,
                requiredness,
                ActivitySetupRequirementStatus.Planned,
                plan.Source,
                plan.Reason);
        }
    }

    internal sealed class ActivitySetupInventoryValidator
    {
        public ActivitySetupInventoryValidationResult Validate(ActivitySetupInventory inventory, string source, string reason)
        {
            List<string> errors = new();
            List<string> skippedRequirementIds = new();

            if (!inventory.IsValid)
            {
                errors.Add("ActivitySetupInventory is invalid.");
                return new ActivitySetupInventoryValidationResult(
                    ActivitySetupInventoryValidationResultKind.Failed,
                    inventory,
                    errors,
                    skippedRequirementIds,
                    source,
                    reason,
                    "ActivitySetupInventory validation failed because inventory is invalid.");
            }

            ValidateRequirements(inventory.ParticipantRequirements, errors);
            ValidateRequirements(inventory.ObjectEntryRequirements, errors);
            ValidateRequirements(inventory.SceneContributorRequirements, errors);
            ValidateRequirements(inventory.PlacementRequirements, errors);
            ValidateRequirements(inventory.CameraBindingRequirements, errors);
            ValidateRequirements(inventory.InteractionBindingRequirements, errors);
            ValidateRequirements(inventory.HudBindingRequirements, errors);
            ValidateRequirements(inventory.WarmupRequirements, errors);
            ValidateRequirements(inventory.StateResetRequirements, errors);
            ValidateRequirements(inventory.ReleaseRequirements, errors);

            if (!inventory.HasRequirements)
            {
                skippedRequirementIds.Add("<empty_inventory>");
            }

            if (errors.Count > 0)
            {
                return new ActivitySetupInventoryValidationResult(
                    ActivitySetupInventoryValidationResultKind.Failed,
                    inventory,
                    errors,
                    skippedRequirementIds,
                    source,
                    reason,
                    $"ActivitySetupInventory validation failed errors='{errors.Count}'.");
            }

            var kind = skippedRequirementIds.Count > 0
                ? ActivitySetupInventoryValidationResultKind.ValidWithSkips
                : ActivitySetupInventoryValidationResultKind.Valid;

            return new ActivitySetupInventoryValidationResult(
                kind,
                inventory,
                errors,
                skippedRequirementIds,
                source,
                reason,
                $"ActivitySetupInventory validation succeeded requirements='{inventory.TotalRequirementCount}' skipped='{skippedRequirementIds.Count}'.");
        }

        private static void ValidateRequirements(IReadOnlyList<ParticipantRequirement> requirements, List<string> errors)
        {
            for (int index = 0; index < requirements.Count; index++)
            {
                if (!requirements[index].IsValid)
                {
                    errors.Add($"ParticipantRequirements[{index}] is invalid.");
                }
            }
        }

        private static void ValidateRequirements(IReadOnlyList<ObjectEntryRequirement> requirements, List<string> errors)
        {
            for (int index = 0; index < requirements.Count; index++)
            {
                if (!requirements[index].IsValid)
                {
                    errors.Add($"ObjectEntryRequirements[{index}] is invalid.");
                }
            }
        }

        private static void ValidateRequirements(IReadOnlyList<SceneContributorRequirement> requirements, List<string> errors)
        {
            for (int index = 0; index < requirements.Count; index++)
            {
                if (!requirements[index].IsValid)
                {
                    errors.Add($"SceneContributorRequirements[{index}] is invalid.");
                }
            }
        }

        private static void ValidateRequirements(IReadOnlyList<PlacementRequirement> requirements, List<string> errors)
        {
            for (int index = 0; index < requirements.Count; index++)
            {
                if (!requirements[index].IsValid)
                {
                    errors.Add($"PlacementRequirements[{index}] is invalid.");
                }
            }
        }

        private static void ValidateRequirements(IReadOnlyList<CameraBindingRequirement> requirements, List<string> errors)
        {
            for (int index = 0; index < requirements.Count; index++)
            {
                if (!requirements[index].IsValid)
                {
                    errors.Add($"CameraBindingRequirements[{index}] is invalid.");
                }
            }
        }

        private static void ValidateRequirements(IReadOnlyList<InteractionBindingRequirement> requirements, List<string> errors)
        {
            for (int index = 0; index < requirements.Count; index++)
            {
                if (!requirements[index].IsValid)
                {
                    errors.Add($"InteractionBindingRequirements[{index}] is invalid.");
                }
            }
        }

        private static void ValidateRequirements(IReadOnlyList<HudBindingRequirement> requirements, List<string> errors)
        {
            for (int index = 0; index < requirements.Count; index++)
            {
                if (!requirements[index].IsValid)
                {
                    errors.Add($"HudBindingRequirements[{index}] is invalid.");
                }
            }
        }

        private static void ValidateRequirements(IReadOnlyList<WarmupRequirement> requirements, List<string> errors)
        {
            for (int index = 0; index < requirements.Count; index++)
            {
                if (!requirements[index].IsValid)
                {
                    errors.Add($"WarmupRequirements[{index}] is invalid.");
                }
            }
        }

        private static void ValidateRequirements(IReadOnlyList<StateResetRequirement> requirements, List<string> errors)
        {
            for (int index = 0; index < requirements.Count; index++)
            {
                if (!requirements[index].IsValid)
                {
                    errors.Add($"StateResetRequirements[{index}] is invalid.");
                }
            }
        }

        private static void ValidateRequirements(IReadOnlyList<ReleaseRequirement> requirements, List<string> errors)
        {
            for (int index = 0; index < requirements.Count; index++)
            {
                if (!requirements[index].IsValid)
                {
                    errors.Add($"ReleaseRequirements[{index}] is invalid.");
                }
            }
        }
    }
}
