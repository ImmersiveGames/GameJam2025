using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeReleaseResult
    {
        public ActorAttributeReleaseOutcome Outcome { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public int ReleasedAttributeCount { get; }
        public string Reason { get; }

        public bool Succeeded => Outcome == ActorAttributeReleaseOutcome.Released || Outcome == ActorAttributeReleaseOutcome.SkippedNoContent;
        public bool Released => Outcome == ActorAttributeReleaseOutcome.Released;
        public bool SkippedNoContent => Outcome == ActorAttributeReleaseOutcome.SkippedNoContent;
        public bool Rejected => Outcome == ActorAttributeReleaseOutcome.Rejected;
        public bool Failed => Outcome == ActorAttributeReleaseOutcome.Failed;

        private ActorAttributeReleaseResult(
            ActorAttributeReleaseOutcome outcome,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            int releasedAttributeCount,
            string reason)
        {
            Outcome = outcome;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ReleasedAttributeCount = releasedAttributeCount;
            Reason = reason ?? string.Empty;
        }

        public static ActorAttributeReleaseResult ReleasedResult(ActorInstanceRuntimeId actorInstanceRuntimeId, int releasedAttributeCount)
        {
            return new ActorAttributeReleaseResult(
                ActorAttributeReleaseOutcome.Released,
                actorInstanceRuntimeId,
                releasedAttributeCount,
                string.Empty);
        }

        public static ActorAttributeReleaseResult Skipped(ActorInstanceRuntimeId actorInstanceRuntimeId, string reason)
        {
            return new ActorAttributeReleaseResult(
                ActorAttributeReleaseOutcome.SkippedNoContent,
                actorInstanceRuntimeId,
                0,
                reason);
        }

        public static ActorAttributeReleaseResult Reject(ActorInstanceRuntimeId actorInstanceRuntimeId, string reason)
        {
            return new ActorAttributeReleaseResult(
                ActorAttributeReleaseOutcome.Rejected,
                actorInstanceRuntimeId,
                0,
                reason);
        }

        public static ActorAttributeReleaseResult Fail(ActorInstanceRuntimeId actorInstanceRuntimeId, string reason)
        {
            return new ActorAttributeReleaseResult(
                ActorAttributeReleaseOutcome.Failed,
                actorInstanceRuntimeId,
                0,
                reason);
        }
    }
}
