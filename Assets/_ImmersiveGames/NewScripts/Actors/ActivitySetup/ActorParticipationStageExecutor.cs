using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.ActivitySetup
{
    public readonly struct ActorParticipationCommand
    {
        public ActorParticipationCommand(
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

    public enum ActorParticipationActorOutcome
    {
        Unknown = 0,
        Entered = 1,
        Skipped = 2,
        Failed = 3,
    }

    public readonly struct ActorParticipationActorResult
    {
        public ActorParticipationActorResult(
            ActorParticipationActorOutcome outcome,
            ActorInstanceRecord instance,
            ActorParticipationRecord participation,
            string reasonCode,
            string skipKind)
        {
            Outcome = outcome;
            Instance = instance;
            Participation = participation;
            ReasonCode = Normalize(reasonCode);
            SkipKind = Normalize(skipKind);
        }

        public ActorParticipationActorOutcome Outcome { get; }
        public ActorInstanceRecord Instance { get; }
        public ActorParticipationRecord Participation { get; }
        public string ReasonCode { get; }
        public string SkipKind { get; }
        public bool IsEntered => Outcome == ActorParticipationActorOutcome.Entered;
        public bool IsSkipped => Outcome == ActorParticipationActorOutcome.Skipped;
        public bool IsFailed => Outcome == ActorParticipationActorOutcome.Failed;
        public bool IsValid =>
            Outcome != ActorParticipationActorOutcome.Unknown &&
            Instance.IsValid &&
            Participation.IsValid &&
            !string.IsNullOrWhiteSpace(ReasonCode);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorParticipationReadinessEvaluation
    {
        public ActorParticipationReadinessEvaluation(
            bool isReady,
            bool isFailure,
            string reasonCode)
        {
            IsReady = isReady;
            IsFailure = isFailure;
            ReasonCode = Normalize(reasonCode);
        }

        public bool IsReady { get; }
        public bool IsFailure { get; }
        public string ReasonCode { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(ReasonCode);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public interface IActorParticipationReadinessPolicy
    {
        ActorParticipationReadinessEvaluation Evaluate(
            SessionActivityIdentity identity,
            ActorInstanceRecord instance);
    }

    public readonly struct ActorParticipationResult
    {
        public ActorParticipationResult(
            SessionActivityIdentity identity,
            IReadOnlyList<ActorParticipationActorResult> actorResults,
            int total,
            int entered,
            int skipped,
            int failed,
            string source,
            string reason)
        {
            Identity = identity;
            ActorResults = actorResults ?? Array.Empty<ActorParticipationActorResult>();
            Total = total < 0 ? 0 : total;
            Entered = entered < 0 ? 0 : entered;
            Skipped = skipped < 0 ? 0 : skipped;
            Failed = failed < 0 ? 0 : failed;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public IReadOnlyList<ActorParticipationActorResult> ActorResults { get; }
        public int Total { get; }
        public int Entered { get; }
        public int Skipped { get; }
        public int Failed { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => Identity.IsValid && Total >= 0 && Entered >= 0 && Skipped >= 0 && Failed >= 0;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public sealed class ActorParticipationStageExecutor
    {
        private readonly IActorParticipationReadinessPolicy _readinessPolicy;

        public ActorParticipationStageExecutor(IActorParticipationReadinessPolicy readinessPolicy)
        {
            _readinessPolicy = readinessPolicy ?? throw new InvalidOperationException("ActorParticipationStageExecutor requires non-null readiness policy.");
        }

        public ActorParticipationResult Execute(ActorParticipationCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActorParticipationCommand is invalid.");
            }

            Dictionary<ActorInstanceRuntimeId, ActorInstanceRecord> instancesByRuntimeId = BuildActorInstanceIndex(command.InventoryFeed.ActorInstances);
            List<ActorParticipationActorResult> actorResults = new();
            int total = 0;
            int entered = 0;
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
                    actorResults.Add(new ActorParticipationActorResult(
                        ActorParticipationActorOutcome.Skipped,
                        instance,
                        participation,
                        eligibilityReason,
                        "policy"));
                    continue;
                }

                var readiness = _readinessPolicy.Evaluate(command.Identity, instance);
                if (!readiness.IsReady)
                {
                    if (readiness.IsFailure)
                    {
                        failed += 1;
                        actorResults.Add(new ActorParticipationActorResult(
                            ActorParticipationActorOutcome.Failed,
                            instance,
                            participation,
                            readiness.ReasonCode,
                            "readiness_required"));
                        continue;
                    }

                    skipped += 1;
                    actorResults.Add(new ActorParticipationActorResult(
                        ActorParticipationActorOutcome.Skipped,
                        instance,
                        participation,
                        readiness.ReasonCode,
                        "readiness_optional"));
                    continue;
                }

                entered += 1;
                actorResults.Add(new ActorParticipationActorResult(
                    ActorParticipationActorOutcome.Entered,
                    instance,
                    participation,
                    "ready",
                    string.Empty));
            }

            return new ActorParticipationResult(
                command.Identity,
                actorResults,
                total,
                entered,
                skipped,
                failed,
                command.Source,
                command.Reason);
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

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
