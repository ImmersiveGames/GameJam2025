using System;
namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeReleaseResult
    {
        public ActorAttributeReleaseOutcome Outcome { get; }
        public string ActorInstanceId { get; }
        public int ReleasedAttributeCount { get; }
        public string Reason { get; }

        public bool Succeeded => Outcome == ActorAttributeReleaseOutcome.Released || Outcome == ActorAttributeReleaseOutcome.SkippedNoContent;
        public bool Released => Outcome == ActorAttributeReleaseOutcome.Released;
        public bool SkippedNoContent => Outcome == ActorAttributeReleaseOutcome.SkippedNoContent;
        public bool Rejected => Outcome == ActorAttributeReleaseOutcome.Rejected;
        public bool Failed => Outcome == ActorAttributeReleaseOutcome.Failed;

        private ActorAttributeReleaseResult(
            ActorAttributeReleaseOutcome outcome,
            string actorInstanceId,
            int releasedAttributeCount,
            string reason)
        {
            Outcome = outcome;
            ActorInstanceId = actorInstanceId ?? string.Empty;
            ReleasedAttributeCount = releasedAttributeCount;
            Reason = reason ?? string.Empty;
        }

        public static ActorAttributeReleaseResult ReleasedResult(string actorInstanceId, int releasedAttributeCount)
        {
            return new ActorAttributeReleaseResult(
                ActorAttributeReleaseOutcome.Released,
                actorInstanceId,
                releasedAttributeCount,
                string.Empty);
        }

        public static ActorAttributeReleaseResult Skipped(string actorInstanceId, string reason)
        {
            return new ActorAttributeReleaseResult(
                ActorAttributeReleaseOutcome.SkippedNoContent,
                actorInstanceId,
                0,
                reason);
        }

        public static ActorAttributeReleaseResult Reject(string actorInstanceId, string reason)
        {
            return new ActorAttributeReleaseResult(
                ActorAttributeReleaseOutcome.Rejected,
                actorInstanceId,
                0,
                reason);
        }

        public static ActorAttributeReleaseResult Fail(string actorInstanceId, string reason)
        {
            return new ActorAttributeReleaseResult(
                ActorAttributeReleaseOutcome.Failed,
                actorInstanceId,
                0,
                reason);
        }
    }
}
