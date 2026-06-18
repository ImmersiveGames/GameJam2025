using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Contracts;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal readonly struct ActorReleaseContributionStageCommand
    {
        public ActorReleaseContributionStageCommand(
            SessionActivityIdentity identity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorKind actorKind,
            ActorRole actorRole,
            ActorScope actorScope,
            ActorCapabilitySurface capabilitySurface,
            string componentBasePath,
            ActorLifetimeTrigger trigger,
            string source,
            string reason)
        {
            Identity = identity;
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ActorKind = actorKind == ActorKind.Unknown ? ResolveActorKind(actorRole) : actorKind;
            ActorRole = actorRole;
            ActorScope = actorScope;
            CapabilitySurface = capabilitySurface;
            ComponentBasePath = componentBasePath.TrimToEmpty();
            Trigger = trigger;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorKind ActorKind { get; }
        public ActorRole ActorRole { get; }
        public ActorScope ActorScope { get; }
        public ActorCapabilitySurface CapabilitySurface { get; }
        public string ComponentBasePath { get; }
        public ActorLifetimeTrigger Trigger { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            ActorKind != ActorKind.Unknown &&
            ActorRole != ActorRole.Unknown &&
            ActorScope != ActorScope.Unknown &&
            Trigger != ActorLifetimeTrigger.Unknown &&
            !string.IsNullOrWhiteSpace(Source);

        private static ActorKind ResolveActorKind(ActorRole actorRole)
        {
            return actorRole == ActorRole.PrimaryPlayer || actorRole == ActorRole.SupportingPlayer
                ? ActorKind.Player
                : ActorKind.Actor;
        }
}

    internal readonly struct ActorReleaseContributionStageResult
    {
        public ActorReleaseContributionStageResult(bool completed, int executed, int skipped, string reason)
        {
            Completed = completed;
            Executed = executed < 0 ? 0 : executed;
            Skipped = skipped < 0 ? 0 : skipped;
            Reason = reason.TrimToEmpty();
        }

        public bool Completed { get; }
        public int Executed { get; }
        public int Skipped { get; }
        public string Reason { get; }
        public bool IsValid => Completed && !string.IsNullOrWhiteSpace(Reason);
}

    internal static class ActorReleaseContributionStage
    {
        private const string Owner = "ActorReleaseContributionStage";
        private const string MacroLifecycleOwner = "SessionActivityPipeline";

        public static ActorReleaseContributionStageResult ExecuteOrFail(ActorReleaseContributionStageCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActorReleaseContributionStageCommand is invalid.");
            }

            IReadOnlyList<IActorReleaseContributionProvider> providers = command.CapabilitySurface?.ReleaseContributionProviders;
            if (providers == null || providers.Count == 0)
            {
                return new ActorReleaseContributionStageResult(true, 0, 0, "no_release_contribution_providers");
            }

            int executedCount = 0;
            int skippedCount = 0;
            for (int index = 0; index < providers.Count; index++)
            {
                IActorReleaseContributionProvider provider = providers[index];
                if (provider == null)
                {
                    skippedCount++;
                    continue;
                }

                ActorCapabilityContributionContext contributionContext = BuildContributionContext(command, provider);
                if (!provider.TryCreateReleaseContribution(contributionContext, out IActorReleaseContribution contribution))
                {
                    skippedCount++;
                    continue;
                }

                if (contribution == null || !contribution.IsValid || contribution.ReleaseEndpoint == null)
                {
                    throw new InvalidOperationException($"[FATAL][ActorReleaseContributionStage] Invalid release contribution provider='{provider.GetType().Name}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}'.");
                }

                if (!contribution.ReleaseEndpoint.TryRelease(contributionContext, out ActorCapabilityReleaseResult result) ||
                    !result.IsValid ||
                    !result.Released)
                {
                    string outcomeReason = result.IsValid ? result.OutcomeReason : "invalid_release_result";
                    DebugUtility.Log(
                        typeof(ActorReleaseContributionStage),
                        $"event='ActorReleaseContributionFailed' owner='{Owner}' macroLifecycleOwner='{MacroLifecycleOwner}' activityId='{command.Identity.ActivityId}' entrySequence='{command.Identity.EntrySequence}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' actorScope='{command.ActorScope}' trigger='{command.Trigger}' providerType='{provider.GetType().FullName ?? provider.GetType().Name}' outcomeReason='{outcomeReason.TrimToEmpty()}' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Error);
                    throw new InvalidOperationException($"[FATAL][ActorReleaseContributionStage] Release contribution failed actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' provider='{provider.GetType().Name}' reason='{outcomeReason}'.");
                }

                executedCount++;
            }

            DebugUtility.LogVerbose(
                typeof(ActorReleaseContributionStage),
                $"event='ActorReleaseContributionsCompleted' owner='{Owner}' macroLifecycleOwner='{MacroLifecycleOwner}' activityId='{command.Identity.ActivityId}' entrySequence='{command.Identity.EntrySequence}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' actorScope='{command.ActorScope}' trigger='{command.Trigger}' executed='{executedCount}' skipped='{skippedCount}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new ActorReleaseContributionStageResult(true, executedCount, skippedCount, "actor_release_contributions_completed");
        }

        private static ActorCapabilityContributionContext BuildContributionContext(
            ActorReleaseContributionStageCommand command,
            IActorReleaseContributionProvider provider)
        {
            string componentPath = provider is Component component
                ? BuildTransformPath(component.transform)
                : command.ComponentBasePath;

            if (string.IsNullOrWhiteSpace(componentPath))
            {
                componentPath = command.ActorId.Value;
            }

            return new ActorCapabilityContributionContext(
                command.Identity,
                command.ActorId,
                command.ActorInstanceRuntimeId,
                command.ActorKind,
                command.ActorRole,
                command.ActorScope,
                componentPath,
                command.Source,
                command.Reason);
        }

        private static string BuildTransformPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            string path = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
}
}
