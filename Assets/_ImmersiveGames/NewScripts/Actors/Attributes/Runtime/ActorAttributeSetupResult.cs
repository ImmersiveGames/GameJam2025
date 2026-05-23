using System;
namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeSetupResult
    {
        public ActorAttributeSetupOutcome Outcome { get; }
        public string ActorInstanceId { get; }
        public int AttributeCount { get; }
        public string Reason { get; }

        public bool Succeeded => Outcome == ActorAttributeSetupOutcome.Ready || Outcome == ActorAttributeSetupOutcome.SkippedNoContent;
        public bool IsReady => Outcome == ActorAttributeSetupOutcome.Ready;
        public bool IsSkippedNoContent => Outcome == ActorAttributeSetupOutcome.SkippedNoContent;
        public bool Failed => Outcome == ActorAttributeSetupOutcome.Failed;

        private ActorAttributeSetupResult(
            ActorAttributeSetupOutcome outcome,
            string actorInstanceId,
            int attributeCount,
            string reason)
        {
            Outcome = outcome;
            ActorInstanceId = actorInstanceId ?? string.Empty;
            AttributeCount = attributeCount;
            Reason = reason ?? string.Empty;
        }

        public static ActorAttributeSetupResult Ready(string actorInstanceId, int attributeCount)
        {
            return new ActorAttributeSetupResult(
                ActorAttributeSetupOutcome.Ready,
                actorInstanceId,
                attributeCount,
                string.Empty);
        }

        public static ActorAttributeSetupResult SkippedNoContent(string actorInstanceId, string reason)
        {
            return new ActorAttributeSetupResult(
                ActorAttributeSetupOutcome.SkippedNoContent,
                actorInstanceId,
                0,
                reason);
        }

        public static ActorAttributeSetupResult Fail(string actorInstanceId, string reason)
        {
            return new ActorAttributeSetupResult(
                ActorAttributeSetupOutcome.Failed,
                actorInstanceId,
                0,
                reason);
        }
    }
}
