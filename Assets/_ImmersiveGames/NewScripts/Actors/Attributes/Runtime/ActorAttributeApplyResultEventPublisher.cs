using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    public static class ActorAttributeApplyResultEventPublisher
    {
        public static bool TryPublish(
            IActorAttributeEventStream eventStream,
            ActorId actorId,
            ActorAttributeApplyResult result,
            string source,
            string reason)
        {
            if (eventStream == null || !actorId.IsValid || !result.HasFact)
            {
                return false;
            }

            var fact = result.Fact;
            if (!fact.Changed)
            {
                return true;
            }

            eventStream.Publish(
                new ActorAttributeChangedEvent(
                    actorId,
                    fact.ActorInstanceRuntimeId,
                    fact.AttributeId,
                    fact.Operation,
                    fact.PreviousValue,
                    fact.NewValue,
                    fact.MinValue,
                    fact.MaxValue,
                    fact.Clamped,
                    fact.Source,
                    fact.Reason));

            PublishThresholdFacts(eventStream, actorId, result.ThresholdFacts);
            return true;
        }

        private static void PublishThresholdFacts(
            IActorAttributeEventStream eventStream,
            ActorId actorId,
            IReadOnlyList<ActorAttributeThresholdCrossedFact> thresholdFacts)
        {
            if (eventStream == null || !actorId.IsValid || thresholdFacts == null || thresholdFacts.Count == 0)
            {
                return;
            }

            for (int index = 0; index < thresholdFacts.Count; index++)
            {
                var thresholdFact = thresholdFacts[index];
                if (!thresholdFact.IsValid)
                {
                    continue;
                }

                eventStream.Publish(
                    new ActorAttributeThresholdCrossedEvent(
                        actorId,
                        thresholdFact.ActorInstanceRuntimeId,
                        thresholdFact.AttributeId,
                        thresholdFact.Operation,
                        thresholdFact.ThresholdId,
                        thresholdFact.PresetKind,
                        thresholdFact.Direction,
                        thresholdFact.ThresholdNormalizedValue,
                        thresholdFact.PreviousValue,
                        thresholdFact.CurrentValue,
                        thresholdFact.MinValue,
                        thresholdFact.MaxValue,
                        thresholdFact.PreviousNormalizedValue,
                        thresholdFact.CurrentNormalizedValue,
                        thresholdFact.Source,
                        thresholdFact.Reason));
            }
        }
    }
}
