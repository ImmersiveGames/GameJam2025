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
                requirements.Add(new StateResetRequirement(requirement, entry.TargetId));
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

}
