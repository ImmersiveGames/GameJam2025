using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    public interface IActorAttributeUiBindingRequestProvider
    {
        IReadOnlyList<ActorAttributeUiBindingRequestEntry> GetRequests();
    }
}
