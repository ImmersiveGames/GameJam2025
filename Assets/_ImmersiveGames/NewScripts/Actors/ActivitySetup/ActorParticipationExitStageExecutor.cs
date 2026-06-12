using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.ActivitySetup
{
    public readonly struct ActorParticipationExitCommand
    {
        public ActorParticipationExitCommand(
            SessionActivityIdentity identity,
            string activityId,
            int entrySequence,
            ActorInventoryFeedResult inventoryFeed,
            string source,
            string reason)
        {
            Identity = identity;
            ActivityId = Normalize(activityId);
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            InventoryFeed = inventoryFeed;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ActivityId { get; }
        public int EntrySequence { get; }
        public ActorInventoryFeedResult InventoryFeed { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            EntrySequence > 0 &&
            InventoryFeed.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public enum ActorParticipationExitActorOutcome
    {
        Unknown = 0,
        Exited = 1,
        Skipped = 2,
        Failed = 3,
    }

    public readonly struct ActorParticipationExitActorResult
    {
        public ActorParticipationExitActorResult(
            ActorParticipationExitActorOutcome outcome,
            ActorInstanceRecord instance,
            ActorParticipationRecord participation,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId,
            string reasonCode,
            string skipOrFailureKind)
        {
            Outcome = outcome;
            Instance = instance;
            Participation = participation;
            PlayerActorId = playerActorId;
            PlayerSlotId = playerSlotId;
            ReasonCode = Normalize(reasonCode);
            SkipOrFailureKind = Normalize(skipOrFailureKind);
        }

        public ActorParticipationExitActorOutcome Outcome { get; }
        public ActorInstanceRecord Instance { get; }
        public ActorParticipationRecord Participation { get; }
        public PlayerActorId PlayerActorId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public string ReasonCode { get; }
        public string SkipOrFailureKind { get; }
        public bool IsExited => Outcome == ActorParticipationExitActorOutcome.Exited;
        public bool IsSkipped => Outcome == ActorParticipationExitActorOutcome.Skipped;
        public bool IsFailed => Outcome == ActorParticipationExitActorOutcome.Failed;
        public bool HasResolvedPlayerIdentity => PlayerActorId.IsValid && PlayerSlotId.IsValid;
        public bool IsValid =>
            Outcome != ActorParticipationExitActorOutcome.Unknown &&
            Instance.IsValid &&
            Participation.IsValid &&
            !string.IsNullOrWhiteSpace(ReasonCode);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorParticipationExitResult
    {
        public ActorParticipationExitResult(
            SessionActivityIdentity identity,
            IReadOnlyList<ActorParticipationExitActorResult> actorResults,
            int total,
            int exited,
            int skipped,
            int failed,
            string source,
            string reason)
        {
            Identity = identity;
            ActorResults = actorResults ?? Array.Empty<ActorParticipationExitActorResult>();
            Total = total < 0 ? 0 : total;
            Exited = exited < 0 ? 0 : exited;
            Skipped = skipped < 0 ? 0 : skipped;
            Failed = failed < 0 ? 0 : failed;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public IReadOnlyList<ActorParticipationExitActorResult> ActorResults { get; }
        public int Total { get; }
        public int Exited { get; }
        public int Skipped { get; }
        public int Failed { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => Identity.IsValid && Total >= 0 && Exited >= 0 && Skipped >= 0 && Failed >= 0;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public sealed class ActorParticipationExitStageExecutor
    {
        public ActorParticipationExitResult Execute(
            ActorParticipationExitCommand command,
            IReadOnlyCollection<ActorInstanceRuntimeId> activeParticipationActorIds)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActorParticipationExitCommand is invalid.");
            }

            Dictionary<ActorInstanceRuntimeId, ActorInstanceRecord> instancesByRuntimeId = BuildActorInstanceIndex(command.InventoryFeed.ActorInstances);
            List<ActorParticipationExitActorResult> actorResults = new();
            int total = 0;
            int exited = 0;
            int skipped = 0;
            int failed = 0;

            IReadOnlyList<ActorParticipationRecord> participations = command.InventoryFeed.ActorParticipations;
            for (int index = 0; index < participations.Count; index++)
            {
                var participation = participations[index];
                if (!participation.IsValid || !participation.ParticipatesInCurrentEntry)
                {
                    continue;
                }

                if (!instancesByRuntimeId.TryGetValue(participation.ActorInstanceRuntimeId, out var instance) || !instance.IsValid)
                {
                    continue;
                }

                total += 1;
                if (!IsEligibleFromPolicy(command.ActivityId, participation, out string eligibilityReason))
                {
                    skipped += 1;
                    actorResults.Add(new ActorParticipationExitActorResult(
                        ActorParticipationExitActorOutcome.Skipped,
                        instance,
                        participation,
                        default,
                        default,
                        eligibilityReason,
                        "policy"));
                    continue;
                }

                var runtimeActorInstanceId = instance.ActorInstanceRuntimeId;
                if (!runtimeActorInstanceId.IsValid)
                {
                    skipped += 1;
                    actorResults.Add(new ActorParticipationExitActorResult(
                        ActorParticipationExitActorOutcome.Skipped,
                        instance,
                        participation,
                        default,
                        default,
                        "actor_instance_runtime_id_invalid",
                        "runtime_id"));
                    continue;
                }

                if (!ContainsActorInstanceRuntimeId(activeParticipationActorIds, runtimeActorInstanceId))
                {
                    skipped += 1;
                    actorResults.Add(new ActorParticipationExitActorResult(
                        ActorParticipationExitActorOutcome.Skipped,
                        instance,
                        participation,
                        default,
                        default,
                        "actor_participation_not_active",
                        "not_active"));
                    continue;
                }

                PlayerActorId playerActorId = default;
                PlayerSlotId playerSlotId = default;
                bool hasPlayerIdentityComponent = HasPlayerIdentityComponent(instance);
                if (hasPlayerIdentityComponent &&
                    !TryResolvePlayerIdentityFromInstance(instance, out playerActorId, out playerSlotId, out string playerIdentityFailureReason))
                {
                    failed += 1;
                    actorResults.Add(new ActorParticipationExitActorResult(
                        ActorParticipationExitActorOutcome.Failed,
                        instance,
                        participation,
                        default,
                        default,
                        playerIdentityFailureReason,
                        "player_identity"));
                    continue;
                }

                exited += 1;
                actorResults.Add(new ActorParticipationExitActorResult(
                    ActorParticipationExitActorOutcome.Exited,
                    instance,
                    participation,
                    hasPlayerIdentityComponent ? playerActorId : default,
                    hasPlayerIdentityComponent ? playerSlotId : default,
                    "exited",
                    string.Empty));
            }

            return new ActorParticipationExitResult(
                command.Identity,
                actorResults,
                total,
                exited,
                skipped,
                failed,
                command.Source,
                command.Reason);
        }

        private static bool HasPlayerIdentityComponent(ActorInstanceRecord instance)
        {
            return instance.IsValid &&
                instance.ActorRoot != null &&
                instance.ActorRoot.GetComponent<PlayerActorIdentity>() != null;
        }

        private static bool TryResolvePlayerIdentityFromInstance(
            ActorInstanceRecord instance,
            out PlayerActorId playerActorId,
            out PlayerSlotId playerSlotId,
            out string failureReason)
        {
            playerActorId = default;
            playerSlotId = default;
            failureReason = "player_identity_missing_in_actor_participation_record";

            if (!instance.IsValid || instance.ActorRoot == null)
            {
                return false;
            }

            var identity = instance.ActorRoot.GetComponent<PlayerActorIdentity>();
            if (identity == null || !identity.IsValid)
            {
                return false;
            }

            playerActorId = identity.PlayerActorId;
            playerSlotId = identity.PlayerSlotId;
            if (!playerActorId.IsValid || !playerSlotId.IsValid)
            {
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        private static Dictionary<ActorInstanceRuntimeId, ActorInstanceRecord> BuildActorInstanceIndex(IReadOnlyList<ActorInstanceRecord> instances)
        {
            Dictionary<ActorInstanceRuntimeId, ActorInstanceRecord> byRuntimeId = new();
            if (instances == null)
            {
                return byRuntimeId;
            }

            for (int index = 0; index < instances.Count; index++)
            {
                var instance = instances[index];
                if (!instance.IsValid)
                {
                    continue;
                }

                byRuntimeId[instance.ActorInstanceRuntimeId] = instance;
            }

            return byRuntimeId;
        }

        private static bool IsEligibleFromPolicy(
            string activityId,
            ActorParticipationRecord participation,
            out string reasonCode)
        {
            if (!participation.IsValid)
            {
                reasonCode = "participation_record_invalid";
                return false;
            }

            if (!participation.ParticipatesInCurrentEntry)
            {
                reasonCode = "entry_not_participating";
                return false;
            }

            switch (participation.Policy)
            {
                case ActorParticipationRecord.ActorParticipationPolicy.AllActivitiesInRoute:
                    reasonCode = "eligible";
                    return true;
                case ActorParticipationRecord.ActorParticipationPolicy.ExplicitActivityIds:
                    {
                        IReadOnlyList<string> activityIds = participation.ExplicitActivityIds;
                        for (int index = 0; index < activityIds.Count; index++)
                        {
                            if (string.Equals(Normalize(activityIds[index]), Normalize(activityId), StringComparison.Ordinal))
                            {
                                reasonCode = "eligible";
                                return true;
                            }
                        }

                        reasonCode = "activity_not_listed";
                        return false;
                    }
                case ActorParticipationRecord.ActorParticipationPolicy.None:
                default:
                    reasonCode = "policy_disabled";
                    return false;
            }
        }

        private static bool ContainsActorInstanceRuntimeId(
            IReadOnlyCollection<ActorInstanceRuntimeId> activeParticipationActorIds,
            ActorInstanceRuntimeId actorInstanceRuntimeId)
        {
            if (activeParticipationActorIds == null || !actorInstanceRuntimeId.IsValid)
            {
                return false;
            }

            foreach (var current in activeParticipationActorIds)
            {
                if (current == actorInstanceRuntimeId)
                {
                    return true;
                }
            }

            return false;
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
