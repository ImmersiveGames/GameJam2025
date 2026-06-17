using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeSetupResult
    {
        public ActorAttributeSetupOutcome Outcome { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public int AttributeCount { get; }
        public string Reason { get; }

        public bool Succeeded => Outcome == ActorAttributeSetupOutcome.Ready || Outcome == ActorAttributeSetupOutcome.SkippedNoContent;
        public bool IsReady => Outcome == ActorAttributeSetupOutcome.Ready;
        public bool IsSkippedNoContent => Outcome == ActorAttributeSetupOutcome.SkippedNoContent;
        public bool Failed => Outcome == ActorAttributeSetupOutcome.Failed;

        private ActorAttributeSetupResult(
            ActorAttributeSetupOutcome outcome,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            int attributeCount,
            string reason)
        {
            Outcome = outcome;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            AttributeCount = attributeCount;
            Reason = reason ?? string.Empty;
        }

        public static ActorAttributeSetupResult Ready(ActorInstanceRuntimeId actorInstanceRuntimeId, int attributeCount)
        {
            return new ActorAttributeSetupResult(
                ActorAttributeSetupOutcome.Ready,
                actorInstanceRuntimeId,
                attributeCount,
                string.Empty);
        }

        public static ActorAttributeSetupResult SkippedNoContent(ActorInstanceRuntimeId actorInstanceRuntimeId, string reason)
        {
            return new ActorAttributeSetupResult(
                ActorAttributeSetupOutcome.SkippedNoContent,
                actorInstanceRuntimeId,
                0,
                reason);
        }

        public static ActorAttributeSetupResult Fail(ActorInstanceRuntimeId actorInstanceRuntimeId, string reason)
        {
            return new ActorAttributeSetupResult(
                ActorAttributeSetupOutcome.Failed,
                actorInstanceRuntimeId,
                0,
                reason);
        }
    }
}
