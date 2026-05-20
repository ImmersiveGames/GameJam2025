using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    internal readonly struct ActivitySetupInventoryBuildContext
    {
        public ActivitySetupInventoryBuildContext(
            SessionActivityDefinition definition,
            SessionActivityIdentity identity,
            ActivityContentLoadedSet loadedSet,
            string source,
            string reason)
        {
            Definition = definition;
            Identity = identity;
            LoadedSet = loadedSet;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityDefinition Definition { get; }
        public SessionActivityIdentity Identity { get; }
        public ActivityContentLoadedSet LoadedSet { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => Definition.IsValid && Identity.IsValid && Identity.Stage == SessionActivityStage.ActivitySetupStarted && !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    internal sealed class ActivitySetupInventoryBuilder
    {
        public ActivitySetupInventoryBuildResult Build(ActivitySetupInventoryBuildContext context)
        {
            if (!context.IsValid)
            {
                return new ActivitySetupInventoryBuildResult(
                    ActivitySetupInventoryBuildResultKind.Failed,
                    default,
                    context.Source,
                    context.Reason,
                    "ActivitySetupInventory build context is invalid.");
            }

            ActivitySetupRequirementsAuthoring requirements = context.Definition.ActivityContentProfile != null
                ? context.Definition.ActivityContentProfile.SetupRequirements
                : null;

            string inventoryId = $"{context.Definition.ActivityId}|{context.Identity.EntrySequence}|activity_setup_inventory";

            ActivitySetupInventory inventory = new(
                context.Identity,
                inventoryId,
                BuildParticipantRequirements(context, requirements?.ParticipantRequirements),
                BuildObjectEntryRequirements(context, requirements?.ObjectEntryRequirements),
                BuildSceneContributorRequirements(context, requirements?.SceneContributorRequirements),
                BuildPlacementRequirements(context, requirements?.PlacementRequirements),
                BuildCameraBindingRequirements(context, requirements?.CameraBindingRequirements),
                BuildInteractionBindingRequirements(context, requirements?.InteractionBindingRequirements),
                BuildHudBindingRequirements(context, requirements?.HudBindingRequirements),
                BuildWarmupRequirements(context, requirements?.WarmupRequirements),
                BuildStateResetRequirements(context, requirements?.StateResetRequirements),
                BuildReleaseRequirements(context, requirements?.ReleaseRequirements),
                context.Source,
                context.Reason);

            if (!inventory.IsValid)
            {
                return new ActivitySetupInventoryBuildResult(
                    ActivitySetupInventoryBuildResultKind.Failed,
                    inventory,
                    context.Source,
                    context.Reason,
                    $"ActivitySetupInventory built invalid inventory for activityId='{context.Definition.ActivityId}' entrySequence='{context.Identity.EntrySequence}'.");
            }

            ActivitySetupInventoryBuildResultKind kind = inventory.HasRequirements
                ? ActivitySetupInventoryBuildResultKind.Built
                : ActivitySetupInventoryBuildResultKind.SkippedNoRequirements;

            string message = inventory.HasRequirements
                ? $"ActivitySetupInventory built requirements='{inventory.TotalRequirementCount}'."
                : "ActivitySetupInventory skipped because no setup requirements were declared.";

            return new ActivitySetupInventoryBuildResult(kind, inventory, context.Source, context.Reason, message);
        }

        private static IReadOnlyList<ParticipantRequirement> BuildParticipantRequirements(ActivitySetupInventoryBuildContext context, IReadOnlyList<ActivityParticipantRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<ParticipantRequirement>();
            }

            List<ParticipantRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                ActivityParticipantRequirementAuthoring entry = entries[index] ?? throw new InvalidOperationException($"Participant requirement at index '{index}' cannot be null.");
                ActivitySetupRequirement requirement = BuildBaseRequirement(context, ActivitySetupSubplanKind.Participant, entry.RequirementId, entry.Requiredness);
                requirements.Add(new ParticipantRequirement(requirement, entry.ParticipantKind, entry.ParticipantId, entry.RoleId, entry.PlacementRequirementId));
            }

            return requirements;
        }

        private static IReadOnlyList<ObjectEntryRequirement> BuildObjectEntryRequirements(ActivitySetupInventoryBuildContext context, IReadOnlyList<ActivityObjectEntryRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<ObjectEntryRequirement>();
            }

            List<ObjectEntryRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                ActivityObjectEntryRequirementAuthoring entry = entries[index] ?? throw new InvalidOperationException($"Object entry requirement at index '{index}' cannot be null.");
                ActivitySetupRequirement requirement = BuildBaseRequirement(context, ActivitySetupSubplanKind.ObjectEntry, entry.RequirementId, entry.Requiredness);
                requirements.Add(new ObjectEntryRequirement(requirement, entry.ObjectEntryKind, entry.ObjectId, entry.ObjectTypeId, entry.PlacementRequirementId));
            }

            return requirements;
        }

        private static IReadOnlyList<SceneContributorRequirement> BuildSceneContributorRequirements(ActivitySetupInventoryBuildContext context, IReadOnlyList<ActivitySceneContributorRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<SceneContributorRequirement>();
            }

            List<SceneContributorRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                ActivitySceneContributorRequirementAuthoring entry = entries[index] ?? throw new InvalidOperationException($"Scene contributor requirement at index '{index}' cannot be null.");
                ActivitySetupRequirement requirement = BuildBaseRequirement(context, ActivitySetupSubplanKind.SceneContributor, entry.RequirementId, entry.Requiredness);
                requirements.Add(new SceneContributorRequirement(requirement, entry.ContributorId, entry.ContributorRole, entry.SceneName));
            }

            return requirements;
        }

        private static IReadOnlyList<PlacementRequirement> BuildPlacementRequirements(ActivitySetupInventoryBuildContext context, IReadOnlyList<ActivityPlacementRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<PlacementRequirement>();
            }

            List<PlacementRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                ActivityPlacementRequirementAuthoring entry = entries[index] ?? throw new InvalidOperationException($"Placement requirement at index '{index}' cannot be null.");
                ActivitySetupRequirement requirement = BuildBaseRequirement(context, ActivitySetupSubplanKind.Placement, entry.RequirementId, entry.Requiredness);
                requirements.Add(new PlacementRequirement(requirement, entry.PlacementKind, entry.TargetId, entry.MarkerId, entry.SceneName));
            }

            return requirements;
        }

        private static IReadOnlyList<CameraBindingRequirement> BuildCameraBindingRequirements(ActivitySetupInventoryBuildContext context, IReadOnlyList<ActivityCameraBindingRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<CameraBindingRequirement>();
            }

            List<CameraBindingRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                ActivityCameraBindingRequirementAuthoring entry = entries[index] ?? throw new InvalidOperationException($"Camera binding requirement at index '{index}' cannot be null.");
                ActivitySetupRequirement requirement = BuildBaseRequirement(context, ActivitySetupSubplanKind.CameraBinding, entry.RequirementId, entry.Requiredness);
                requirements.Add(new CameraBindingRequirement(requirement, entry.CameraBindingKind, entry.BindingId, entry.TargetId, entry.ProfileId));
            }

            return requirements;
        }

        private static IReadOnlyList<InteractionBindingRequirement> BuildInteractionBindingRequirements(ActivitySetupInventoryBuildContext context, IReadOnlyList<ActivityInteractionBindingRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<InteractionBindingRequirement>();
            }

            List<InteractionBindingRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                ActivityInteractionBindingRequirementAuthoring entry = entries[index] ?? throw new InvalidOperationException($"Interaction binding requirement at index '{index}' cannot be null.");
                ActivitySetupRequirement requirement = BuildBaseRequirement(context, ActivitySetupSubplanKind.InteractionBinding, entry.RequirementId, entry.Requiredness);
                requirements.Add(new InteractionBindingRequirement(requirement, entry.InteractionBindingKind, entry.BindingId, entry.TargetId, entry.ProfileId));
            }

            return requirements;
        }

        private static IReadOnlyList<HudBindingRequirement> BuildHudBindingRequirements(ActivitySetupInventoryBuildContext context, IReadOnlyList<ActivityHudBindingRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<HudBindingRequirement>();
            }

            List<HudBindingRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                ActivityHudBindingRequirementAuthoring entry = entries[index] ?? throw new InvalidOperationException($"Hud binding requirement at index '{index}' cannot be null.");
                ActivitySetupRequirement requirement = BuildBaseRequirement(context, ActivitySetupSubplanKind.HudBinding, entry.RequirementId, entry.Requiredness);
                requirements.Add(new HudBindingRequirement(requirement, entry.HudBindingKind, entry.BindingId, entry.TargetId, entry.ProfileId));
            }

            return requirements;
        }

        private static IReadOnlyList<WarmupRequirement> BuildWarmupRequirements(ActivitySetupInventoryBuildContext context, IReadOnlyList<ActivityWarmupRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<WarmupRequirement>();
            }

            List<WarmupRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                ActivityWarmupRequirementAuthoring entry = entries[index] ?? throw new InvalidOperationException($"Warmup requirement at index '{index}' cannot be null.");
                ActivitySetupRequirement requirement = BuildBaseRequirement(context, ActivitySetupSubplanKind.Warmup, entry.RequirementId, entry.Requiredness);
                requirements.Add(new WarmupRequirement(requirement, entry.WarmupKind, entry.TargetId, entry.ProfileId));
            }

            return requirements;
        }

        private static IReadOnlyList<StateResetRequirement> BuildStateResetRequirements(ActivitySetupInventoryBuildContext context, IReadOnlyList<ActivityStateResetRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<StateResetRequirement>();
            }

            List<StateResetRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                ActivityStateResetRequirementAuthoring entry = entries[index] ?? throw new InvalidOperationException($"State reset requirement at index '{index}' cannot be null.");
                ActivitySetupRequirement requirement = BuildBaseRequirement(context, ActivitySetupSubplanKind.StateReset, entry.RequirementId, entry.Requiredness);
                requirements.Add(new StateResetRequirement(requirement, entry.TargetId, entry.ResetGroups));
            }

            return requirements;
        }

        private static IReadOnlyList<ReleaseRequirement> BuildReleaseRequirements(ActivitySetupInventoryBuildContext context, IReadOnlyList<ActivityReleaseRequirementAuthoring> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<ReleaseRequirement>();
            }

            List<ReleaseRequirement> requirements = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                ActivityReleaseRequirementAuthoring entry = entries[index] ?? throw new InvalidOperationException($"Release requirement at index '{index}' cannot be null.");
                ActivitySetupRequirement requirement = BuildBaseRequirement(context, ActivitySetupSubplanKind.Release, entry.RequirementId, entry.Requiredness);
                requirements.Add(new ReleaseRequirement(requirement, entry.ReleaseKind, entry.TargetId, entry.PolicyId));
            }

            return requirements;
        }

        private static ActivitySetupRequirement BuildBaseRequirement(
            ActivitySetupInventoryBuildContext context,
            ActivitySetupSubplanKind subplanKind,
            string requirementId,
            ActivitySetupRequirementRequiredness requiredness)
        {
            return new ActivitySetupRequirement(
                context.Identity,
                subplanKind,
                requirementId,
                requiredness,
                ActivitySetupRequirementStatus.Planned,
                context.Source,
                context.Reason);
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

            ActivitySetupInventoryValidationResultKind kind = skippedRequirementIds.Count > 0
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
