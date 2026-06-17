using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    public interface IActorAttributeEventStream
    {
        void Publish(ActorAttributeChangedEvent changedEvent);

        IDisposable Subscribe(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            Action<ActorAttributeChangedEvent> handler);
    }
}
