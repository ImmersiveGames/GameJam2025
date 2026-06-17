using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    public sealed class EmptyActorAttributeUiBindingRequestProvider : IActorAttributeUiBindingRequestProvider
    {
        public IReadOnlyList<ActorAttributeUiBindingRequestEntry> GetRequests()
        {
            return Array.Empty<ActorAttributeUiBindingRequestEntry>();
        }
    }
}
