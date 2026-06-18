using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;

namespace _ImmersiveGames.NewScripts.Actors.Impact.Runtime
{
    public interface IActorImpactEffectEventStream
    {
        void Publish(ActorImpactEffectEvent impactEffectEvent);

        IDisposable Subscribe(Action<ActorImpactEffectEvent> handler);

        IDisposable Subscribe(
            ActorInstanceRuntimeId impactActorInstanceRuntimeId,
            Action<ActorImpactEffectEvent> handler);
    }
}
