using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    public interface IActorAttributeEventStream
    {
        void Publish(ActorAttributeChangedEvent changedEvent);

        void Publish(ActorAttributeThresholdCrossedEvent thresholdCrossedEvent);

        IDisposable Subscribe(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            Action<ActorAttributeChangedEvent> handler);

        IDisposable SubscribeThreshold(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            Action<ActorAttributeThresholdCrossedEvent> handler);
    }
}
