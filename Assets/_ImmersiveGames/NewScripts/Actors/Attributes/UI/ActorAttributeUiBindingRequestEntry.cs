using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    [Serializable]
    public readonly struct ActorAttributeUiBindingRequestEntry
    {
        public ActorAttributeUiBindingRequestEntry(
            ActorAttributeUiBindingRequest request,
            IActorAttributeUiSink sink)
        {
            Request = request;
            Sink = sink;
        }

        public ActorAttributeUiBindingRequest Request { get; }
        public IActorAttributeUiSink Sink { get; }

        public bool IsValid =>
            Request.IsValid &&
            Sink is { IsReady: true };

        public string GetInvalidReason()
        {
            if (!Request.IsValid)
            {
                return Request.GetInvalidReason();
            }

            if (Sink == null)
            {
                return "sink_missing";
            }

            if (!Sink.IsReady)
            {
                return "sink_not_ready";
            }

            return string.Empty;
        }
    }
}
