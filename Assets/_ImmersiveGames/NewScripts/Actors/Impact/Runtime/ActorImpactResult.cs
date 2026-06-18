using System;

namespace _ImmersiveGames.NewScripts.Actors.Impact.Runtime
{
    [Serializable]
    public readonly struct ActorImpactResult
    {
        private ActorImpactResult(
            ActorImpactOutcome outcome,
            ActorImpactIntent intent,
            ActorImpactTarget target,
            bool hasIntent,
            bool hasTarget,
            string reason)
        {
            Outcome = outcome;
            Intent = intent;
            Target = target;
            HasIntent = hasIntent;
            HasTarget = hasTarget;
            Reason = Normalize(reason);
        }

        public ActorImpactOutcome Outcome { get; }
        public ActorImpactIntent Intent { get; }
        public ActorImpactTarget Target { get; }
        public bool HasIntent { get; }
        public bool HasTarget { get; }
        public string Reason { get; }

        public bool Registered => Outcome == ActorImpactOutcome.Registered;
        public bool Rejected => Outcome == ActorImpactOutcome.Rejected;
        public bool Failed => Outcome == ActorImpactOutcome.Failed;
        public bool HasResolvedTargetActor => HasTarget && Target.HasTargetActor;

        public static ActorImpactResult RegisteredResult(
            ActorImpactIntent intent,
            ActorImpactTarget target)
        {
            return new ActorImpactResult(
                ActorImpactOutcome.Registered,
                intent,
                target,
                true,
                target.HasTargetObject || target.HasTargetCollider || target.HasTargetActor,
                string.Empty);
        }

        public static ActorImpactResult Reject(
            string reason)
        {
            return new ActorImpactResult(
                ActorImpactOutcome.Rejected,
                default,
                default,
                false,
                false,
                reason);
        }

        public static ActorImpactResult Reject(
            ActorImpactIntent intent,
            ActorImpactTarget target,
            string reason)
        {
            return new ActorImpactResult(
                ActorImpactOutcome.Rejected,
                intent,
                target,
                intent.IsValid,
                target.HasTargetObject || target.HasTargetCollider || target.HasTargetActor,
                reason);
        }

        public static ActorImpactResult Fail(
            ActorImpactIntent intent,
            ActorImpactTarget target,
            string reason)
        {
            return new ActorImpactResult(
                ActorImpactOutcome.Failed,
                intent,
                target,
                intent.IsValid,
                target.HasTargetObject || target.HasTargetCollider || target.HasTargetActor,
                reason);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
